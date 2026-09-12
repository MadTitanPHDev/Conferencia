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

        const string sql = """
            SELECT
                CODCOMPRA      AS CodCompra,
                CODLOJA        AS CodLoja,
                NUMNOTA        AS NumNota,
                DATAEMISSAO    AS DataEmissao,
                DATACOMPRA     AS DataCompra,
                CODFORN        AS CodForn,
                CNPJFORN       AS CnpjForn,
                VALORNOTA      AS ValorNota,
                STATUS         AS Status,
                NFECHAVEACESSO AS NfeChaveAcesso,
                SERIENOTA      AS SerieNota
            FROM compras
            WHERE DATACOMPRA >= @Inicio AND DATACOMPRA < @FimExclusivo
            """;

        await using var connection = CriarConexao();
        await connection.OpenAsync(cancellationToken);

        var registros = await connection.QueryAsync<CompraVsm>(new CommandDefinition(
            sql,
            new { Inicio = inicio.Date, FimExclusivo = fim.Date.AddDays(1) },
            cancellationToken: cancellationToken));

        return registros.AsList();
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
                CUSTOUNIT        AS CustoUnit,
                PRECOVENDANOVO   AS PrecoVendaNovo,
                QUANTDEVOL       AS QuantDevol,
                QUANTCONFERIDA   AS QuantConferida,
                CODPRODDISTR     AS CodProdDistr,
                BARRAS_EAN       AS BarrasEan,
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
                i.CUSTOUNIT        AS CustoUnit,
                i.BARRAS_EAN       AS BarrasEan
            FROM itens_compra i
            INNER JOIN compras c ON c.CODCOMPRA = i.CODCOMPRA
            WHERE (i.BARRAS_EAN = @Ean OR i.BARRAS_EANTRIB = @Ean)
              AND c.DATACOMPRA >= @DataInicio
              AND (@CustoMinimo IS NULL OR i.CUSTOUNIT > @CustoMinimo)
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
