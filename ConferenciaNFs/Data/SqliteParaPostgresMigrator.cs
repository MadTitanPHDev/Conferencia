using System.IO;
using ConferenciaNFs.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace ConferenciaNFs.Data;

/// <summary>
/// Migracao one-shot de database.db (SQLite) para PostgreSQL.
/// </summary>
public static class SqliteParaPostgresMigrator
{
    public sealed class ResultadoMigracao
    {
        public int LojasInseridas { get; init; }
        public int NotasInseridas { get; init; }
        public int NotasAtualizadas { get; init; }
    }

    public static async Task<ResultadoMigracao> MigrarAsync(
        string caminhoSqlite,
        string connectionStringPostgres,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(caminhoSqlite))
            throw new FileNotFoundException("Arquivo SQLite nao encontrado.", caminhoSqlite);

        if (string.IsNullOrWhiteSpace(connectionStringPostgres))
            throw new InvalidOperationException("ConnectionString do PostgreSQL nao configurada.");

        _ = new NotaFiscalRepository(connectionStringPostgres);

        var sqliteCs = new SqliteConnectionStringBuilder { DataSource = caminhoSqlite }.ConnectionString;

        await using var sqlite = new SqliteConnection(sqliteCs);
        await sqlite.OpenAsync(cancellationToken);

        await using var pg = new NpgsqlConnection(connectionStringPostgres);
        await pg.OpenAsync(cancellationToken);
        await using var tx = await pg.BeginTransactionAsync(cancellationToken);

        var lojasInseridas = 0;
        var notasInseridas = 0;
        var notasAtualizadas = 0;

        var lojasExistem = await sqlite.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='LojaOrdem'
            """) > 0;

        if (lojasExistem)
        {
            var lojas = (await sqlite.QueryAsync<LojaOrdem>("""
                SELECT ApelidoLoja, NumeroOrdem, NomeExibicao
                FROM LojaOrdem
                """)).ToList();

            foreach (var loja in lojas)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await pg.ExecuteAsync("""
                    INSERT INTO LojaOrdem (ApelidoLoja, NumeroOrdem, NomeExibicao)
                    VALUES (@ApelidoLoja, @NumeroOrdem, @NomeExibicao)
                    ON CONFLICT (ApelidoLoja) DO UPDATE SET
                        NumeroOrdem = EXCLUDED.NumeroOrdem,
                        NomeExibicao = EXCLUDED.NomeExibicao;
                    """, loja, tx);

                lojasInseridas++;
            }
        }

        var notasExistem = await sqlite.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='NotasFiscais'
            """) > 0;

        if (notasExistem)
        {
            var colunas = (await sqlite.QueryAsync<string>("""
                SELECT name FROM pragma_table_info('NotasFiscais')
                """)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var temObservacao = colunas.Contains("Observacao");
            var temDataEmissao = colunas.Contains("DataEmissao");
            var temDiaConferencia = colunas.Contains("DiaConferencia");

            var sqlSelect = $"""
                SELECT
                    ApelidoLoja,
                    NumNota,
                    NomeForn,
                    CnpjForn,
                    ValorNota,
                    DataCompra,
                    {(temDataEmissao ? "DataEmissao" : "'' AS DataEmissao")},
                    {(temDiaConferencia ? "DiaConferencia" : "DataCompra AS DiaConferencia")},
                    {(temObservacao ? "Observacao" : "'' AS Observacao")},
                    ChaveUnica,
                    StatusConferencia
                FROM NotasFiscais
                """;

            var notas = (await sqlite.QueryAsync<NotaFiscal>(sqlSelect)).ToList();

            foreach (var nota in notas)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(nota.DiaConferencia))
                    nota.DiaConferencia = nota.DataCompra ?? string.Empty;

                nota.Observacao ??= string.Empty;
                nota.DataEmissao ??= string.Empty;
                nota.StatusConferencia = string.IsNullOrWhiteSpace(nota.StatusConferencia)
                    ? StatusConferenciaValues.Pendente
                    : nota.StatusConferencia;

                nota.ChaveUnica = NotaFiscal.GerarChaveUnica(
                    nota.ApelidoLoja,
                    nota.NumNota,
                    nota.CnpjForn ?? string.Empty,
                    nota.DiaConferencia,
                    nota.DataCompra ?? string.Empty);

                var jaExistia = await pg.ExecuteScalarAsync<int?>("""
                    SELECT 1 FROM NotasFiscais WHERE ChaveUnica = @ChaveUnica LIMIT 1
                    """, new { nota.ChaveUnica }, tx) is not null;

                await pg.ExecuteAsync("""
                    INSERT INTO NotasFiscais (
                        ApelidoLoja, NumNota, NomeForn, CnpjForn,
                        ValorNota, DataCompra, DataEmissao, DiaConferencia,
                        Observacao, ChaveUnica, StatusConferencia
                    ) VALUES (
                        @ApelidoLoja, @NumNota, @NomeForn, @CnpjForn,
                        @ValorNota, @DataCompra, @DataEmissao, @DiaConferencia,
                        @Observacao, @ChaveUnica, @StatusConferencia
                    )
                    ON CONFLICT (ChaveUnica) DO UPDATE SET
                        ApelidoLoja = EXCLUDED.ApelidoLoja,
                        NomeForn = EXCLUDED.NomeForn,
                        ValorNota = EXCLUDED.ValorNota,
                        DataCompra = EXCLUDED.DataCompra,
                        DataEmissao = EXCLUDED.DataEmissao,
                        DiaConferencia = EXCLUDED.DiaConferencia,
                        Observacao = EXCLUDED.Observacao,
                        StatusConferencia = EXCLUDED.StatusConferencia;
                    """, nota, tx);

                if (jaExistia)
                    notasAtualizadas++;
                else
                    notasInseridas++;
            }
        }

        await tx.CommitAsync(cancellationToken);

        return new ResultadoMigracao
        {
            LojasInseridas = lojasInseridas,
            NotasInseridas = notasInseridas,
            NotasAtualizadas = notasAtualizadas
        };
    }
}
