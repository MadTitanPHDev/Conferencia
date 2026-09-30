using ConferenciaNFs.Models;
using Dapper;
using MySqlConnector;

namespace ConferenciaNFs.Data;

/// <summary>
/// Leitura das notas de compra no VSM (MySQL). Nao grava nada no ERP.
/// </summary>
public sealed class VsmComprasReader
{
    private readonly string _connectionString;

    public VsmComprasReader(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "MysqlConnectionString nao configurada. Edite app-settings.json na pasta do aplicativo.");

        _connectionString = connectionString.Trim();
    }

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(_connectionString);

    private const string SqlComprasBase = """
            SELECT
                c.CODCOMPRA      AS CodCompra,
                c.CODLOJA        AS CodLoja,
                c.NUMNOTA        AS NumNota,
                c.DATAEMISSAO    AS DataEmissao,
                c.DATACOMPRA     AS DataCompra,
                c.CODFORN        AS CodForn,
                c.CNPJFORN       AS CnpjForn,
                c.VALORNOTA      AS ValorNota,
                c.STATUS         AS Status,
                c.NFECHAVEACESSO AS NfeChaveAcesso,
                c.SERIENOTA      AS SerieNota
            FROM compras c
            WHERE c.DATACOMPRA >= @Inicio AND c.DATACOMPRA < @FimExclusivo
            """;

    public async Task TestarConexaoAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CriarConexao();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT 1",
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Notas cuja data de entrada no VSM (DATACOMPRA) e o dia informado.
    /// Todas as letras de STATUS entram.
    /// </summary>
    public Task<IReadOnlyList<CompraVsm>> ListarPorDataCompraAsync(
        DateTime dataCompra,
        CancellationToken cancellationToken = default)
        => ListarPorIntervaloDataCompraAsync(dataCompra, dataCompra, cancellationToken);

    /// <summary>
    /// Notas cuja DATACOMPRA cai no intervalo, inclusive nas pontas. Uma ida ao VSM cobre
    /// todos os dias; quem chama separa por dia depois.
    /// </summary>
    public async Task<IReadOnlyList<CompraVsm>> ListarPorIntervaloDataCompraAsync(
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default)
    {
        if (fim.Date < inicio.Date)
            throw new ArgumentException("A data final nao pode ser anterior a data inicial.", nameof(fim));

        await using var connection = CriarConexao();
        await connection.OpenAsync(cancellationToken);

        var parametros = new { Inicio = inicio.Date, FimExclusivo = fim.Date.AddDays(1) };

        var registros = (await connection.QueryAsync<CompraVsm>(new CommandDefinition(
            SqlComprasBase,
            parametros,
            commandTimeout: 90,
            cancellationToken: cancellationToken))).AsList();

        await PreencherNomesFornecedorAsync(connection, registros, cancellationToken);
        return registros;
    }

    private static async Task PreencherNomesFornecedorAsync(
        MySqlConnection connection,
        IReadOnlyList<CompraVsm> compras,
        CancellationToken cancellationToken)
    {
        var codigos = compras
            .Select(c => c.CodForn)
            .Where(c => c > 0)
            .Distinct()
            .ToArray();

        if (codigos.Length == 0)
            return;

        try
        {
            var nomes = await connection.QueryAsync<(int CodForn, string? Nome)>(new CommandDefinition(
                """
                SELECT CODFORN AS CodForn, NOME AS Nome
                FROM fornecedor
                WHERE CODFORN IN @Codigos
                """,
                new { Codigos = codigos },
                commandTimeout: 30,
                cancellationToken: cancellationToken));

            var mapa = nomes
                .Where(x => !string.IsNullOrWhiteSpace(x.Nome))
                .GroupBy(x => x.CodForn)
                .ToDictionary(g => g.Key, g => g.First().Nome!.Trim());

            foreach (var compra in compras)
            {
                if (mapa.TryGetValue(compra.CodForn, out var nome))
                    compra.NomeForn = nome;
            }
        }
        catch (Exception)
        {
            // Schema antigo ou tabela ausente: segue so com CNPJ da compra.
        }
    }

    /// <summary>
    /// Itens da nota no VSM. Nao grava nada no ERP.
    /// </summary>
    public async Task<IReadOnlyList<ItemCompraVsm>> ListarItensPorCodCompraAsync(
        int codCompra,
        CancellationToken cancellationToken = default)
    {
        if (codCompra <= 0)
            return [];

        const string sql = """
            SELECT
                CODCOMPRA        AS CodCompra,
                SEQUENCIA        AS Sequencia,
                NUMNOTA          AS NumNota,
                CODPROD          AS CodProd,
                NOMEPROD         AS NomeProd,
                QUANTITEMCOMPRA  AS QuantItemCompra,
                VALORITEMCOMPRA  AS ValorItemCompra,
                VALORITEMFABRICA AS ValorItemFabrica,
                DESCONTO         AS Desconto,
                CUSTOUNIT        AS CustoUnit,
                PRECOVENDANOVO   AS PrecoVendaNovo,
                QUANTDEVOL       AS QuantDevol,
                QUANTCONFERIDA   AS QuantConferida,
                CODPRODDISTR     AS CodProdDistr,
                BARRAS_EAN       AS BarrasEan,
                BARRAS_EANTRIB   AS BarrasEanTrib,
                NUMLOTE          AS NumLote,
                DATAVALIDADE     AS DataValidade
            FROM itens_compra
            WHERE CODCOMPRA = @CodCompra
            ORDER BY SEQUENCIA
            """;

        await using var connection = CriarConexao();
        await connection.OpenAsync(cancellationToken);

        var registros = await connection.QueryAsync<ItemCompraVsm>(new CommandDefinition(
            sql,
            new { CodCompra = codCompra },
            cancellationToken: cancellationToken));

        return registros.AsList();
    }

    public const int LimiteBuscaEan = 400;

    /// <summary>
    /// Notas em que o EAN aparece, a partir de uma data de compra. Nao grava nada no ERP.
    /// </summary>
    public async Task<IReadOnlyList<ItemEanPesquisaVsm>> BuscarItensPorEanAsync(
        string ean,
        DateTime dataInicio,
        decimal? custoMinimo,
        CancellationToken cancellationToken = default)
    {
        var eanNormalizado = NormalizarEan(ean);
        if (eanNormalizado.Length < 8)
            return [];

        const string sql = """
            SELECT
                i.CODCOMPRA        AS CodCompra,
                c.CODLOJA          AS CodLoja,
                i.NUMNOTA          AS NumNota,
                c.DATACOMPRA       AS DataCompra,
                c.CNPJFORN         AS CnpjForn,
                c.VALORNOTA        AS ValorNota,
                i.CODPROD          AS CodProd,
                i.NOMEPROD         AS NomeProd,
                i.QUANTITEMCOMPRA  AS QuantItemCompra,
                (i.VALORITEMCOMPRA - IFNULL(i.DESCONTO, 0)) AS CustoUnit,
                i.BARRAS_EAN       AS BarrasEan
            FROM itens_compra i
            INNER JOIN compras c ON c.CODCOMPRA = i.CODCOMPRA
            WHERE (i.BARRAS_EAN = @Ean OR i.BARRAS_EANTRIB = @Ean)
              AND c.DATACOMPRA >= @DataInicio
              AND (@CustoMinimo IS NULL OR (i.VALORITEMCOMPRA - IFNULL(i.DESCONTO, 0)) > @CustoMinimo)
            ORDER BY c.DATACOMPRA DESC, i.CODCOMPRA DESC
            LIMIT @Limite
            """;

        await using var connection = CriarConexao();
        await connection.OpenAsync(cancellationToken);

        var registros = await connection.QueryAsync<ItemEanPesquisaVsm>(new CommandDefinition(
            sql,
            new
            {
                Ean = eanNormalizado,
                DataInicio = dataInicio.Date,
                CustoMinimo = custoMinimo,
                Limite = LimiteBuscaEan
            },
            commandTimeout: 90,
            cancellationToken: cancellationToken));

        return registros.AsList();
    }

    public static string NormalizarEan(string? ean)
    {
        if (string.IsNullOrWhiteSpace(ean))
            return string.Empty;

        var chars = ean.Where(char.IsDigit).ToArray();
        return new string(chars);
    }

    private MySqlConnection CriarConexao() => new(_connectionString);
}
