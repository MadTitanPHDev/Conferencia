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
                COALESCE(c.REABERTA, '') AS Reaberta,
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
                i.CODCOMPRA        AS CodCompra,
                c.CODLOJA          AS CodLoja,
                i.SEQUENCIA        AS Sequencia,
                i.NUMNOTA          AS NumNota,
                i.CODPROD          AS CodProd,
                i.NOMEPROD         AS NomeProd,
                i.QUANTITEMCOMPRA  AS QuantItemCompra,
                i.VALORITEMCOMPRA  AS ValorItemCompra,
                i.VALORITEMFABRICA AS ValorItemFabrica,
                i.DESCONTO         AS Desconto,
                IFNULL(i.CUSTO_VALORST, 0)    AS CustoValorSt,
                IFNULL(i.CUSTO_VALORFCPST, 0) AS CustoValorFcpSt,
                i.CUSTOUNIT        AS CustoUnit,
                i.PRECOVENDANOVO   AS PrecoVendaNovo,
                i.QUANTDEVOL       AS QuantDevol,
                i.QUANTCONFERIDA   AS QuantConferida,
                i.CODPRODDISTR     AS CodProdDistr,
                i.BARRAS_EAN       AS BarrasEan,
                i.BARRAS_EANTRIB   AS BarrasEanTrib,
                i.NUMLOTE          AS NumLote,
                i.DATAVALIDADE     AS DataValidade
            FROM itens_compra i
            LEFT JOIN compras c ON c.CODCOMPRA = i.CODCOMPRA
            WHERE i.CODCOMPRA = @CodCompra
            ORDER BY i.SEQUENCIA
            """;

        await using var connection = CriarConexao();
        await connection.OpenAsync(cancellationToken);

        var registros = await connection.QueryAsync<ItemCompraVsm>(new CommandDefinition(
            sql,
            new { CodCompra = codCompra },
            cancellationToken: cancellationToken));

        return registros.AsList();
    }

    /// <summary>
    /// Pedidos da mesma loja na data de emissão ou de entrada, só com produtos desta nota.
    /// </summary>
    public async Task<IReadOnlyList<ComparacaoProdutoPedido>> ListarComparacaoPedidosAsync(
        int codCompra,
        CancellationToken cancellationToken = default)
    {
        if (codCompra <= 0)
            return [];

        const string sql = """
            SELECT
                i.CODPROD AS CodProd,
                SUM(i.QUANTITEMCOMPRA) AS QtdNota,
                SUM(i.VALORITEMCOMPRA) AS ValorItem,
                SUM(i.DESCONTO) AS DescontoNota,
                CASE
                    WHEN SUM(i.QUANTITEMCOMPRA) = 0 THEN 0
                    ELSE SUM(i.CUSTOUNIT * i.QUANTITEMCOMPRA) / SUM(i.QUANTITEMCOMPRA)
                END AS PrecoUnitarioNota,
                COALESCE(ped.QtdAtendida, 0) AS QtdAtendida,
                COALESCE(ped.QtdPedida, 0) AS QtdPedida,
                COALESCE(ped.PrecoUnitarioPedido, 0) AS PrecoUnitarioPedido,
                COALESCE(ped.DescontoPedido, 0) AS DescontoPedido,
                CASE WHEN ped.CodProd IS NULL THEN 0 ELSE 1 END AS TemPedidoNumero
            FROM itens_compra i
            INNER JOIN compras c ON c.CODCOMPRA = i.CODCOMPRA
            LEFT JOIN (
                SELECT
                    pi.CODPROD AS CodProd,
                    SUM(COALESCE(pi.QUANTATEND, 0)) AS QtdAtendida,
                    SUM(COALESCE(pi.QUANTPED, 0)) AS QtdPedida,
                    CASE
                        WHEN SUM(COALESCE(pi.QUANTATEND, pi.QUANTPED, 0)) = 0
                            THEN AVG(COALESCE(pi.VALORUNIT, 0))
                        ELSE SUM(COALESCE(pi.QUANTATEND, pi.QUANTPED, 0) * COALESCE(pi.VALORUNIT, 0))
                             / SUM(COALESCE(pi.QUANTATEND, pi.QUANTPED, 0))
                    END AS PrecoUnitarioPedido,
                    CASE
                        WHEN SUM(COALESCE(pi.QUANTATEND, pi.QUANTPED, 0)) = 0
                            THEN AVG(COALESCE(pi.PERCDESCFORN, 0))
                        ELSE SUM(COALESCE(pi.QUANTATEND, pi.QUANTPED, 0) * COALESCE(pi.PERCDESCFORN, 0))
                             / SUM(COALESCE(pi.QUANTATEND, pi.QUANTPED, 0))
                    END AS DescontoPedido
                FROM compras c2
                INNER JOIN pedido p
                    ON p.CODLOJA = c2.CODLOJA
                   AND p.DATAPEDIDO IN (c2.DATAEMISSAO, c2.DATACOMPRA)
                INNER JOIN pedidoitens pi ON pi.IDPEDIDO = p.IDPEDIDO
                WHERE c2.CODCOMPRA = @CodCompra
                  AND EXISTS (
                      SELECT 1
                      FROM itens_compra ix
                      WHERE ix.CODCOMPRA = c2.CODCOMPRA
                        AND ix.CODPROD = pi.CODPROD
                  )
                GROUP BY pi.CODPROD
            ) ped ON ped.CodProd = i.CODPROD
            WHERE i.CODCOMPRA = @CodCompra
            GROUP BY i.CODPROD, ped.CodProd, ped.QtdAtendida, ped.QtdPedida,
                     ped.PrecoUnitarioPedido, ped.DescontoPedido
            """;

        await using var connection = CriarConexao();
        await connection.OpenAsync(cancellationToken);

        var registros = await connection.QueryAsync<ComparacaoProdutoPedido>(new CommandDefinition(
            sql,
            new { CodCompra = codCompra },
            commandTimeout: 60,
            cancellationToken: cancellationToken));

        return registros.AsList();
    }

    public const int LimiteBuscaEan = 400;

    private static readonly string SqlCustoEntrada = $"""
        (i.VALORITEMCOMPRA - IFNULL(i.DESCONTO, 0)
         + CASE WHEN c.CODLOJA = {CustoItemNota.CodLojaParana}
                THEN IFNULL(i.CUSTO_VALORST, 0) + IFNULL(i.CUSTO_VALORFCPST, 0)
                ELSE 0 END)
        """;

    /// <summary>
    /// Notas em que o EAN aparece, a partir de uma data de compra. Nao grava nada no ERP.
    /// </summary>
    public async Task<IReadOnlyList<ItemEanPesquisaVsm>> BuscarItensPorEanAsync(
        string ean,
        DateTime dataInicio,
        decimal? custoMinimo,
        DateTime? dataFim = null,
        CancellationToken cancellationToken = default)
    {
        var eanNormalizado = NormalizarEan(ean);
        if (eanNormalizado.Length < 8)
            return [];

        var sql = $"""
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
                {SqlCustoEntrada} AS CustoUnit,
                i.BARRAS_EAN       AS BarrasEan
            FROM itens_compra i
            INNER JOIN compras c ON c.CODCOMPRA = i.CODCOMPRA
            WHERE (i.BARRAS_EAN = @Ean OR i.BARRAS_EANTRIB = @Ean)
              AND c.DATACOMPRA >= @DataInicio
              AND (@DataFim IS NULL OR c.DATACOMPRA < @DataFim)
              AND (@CustoMinimo IS NULL OR {SqlCustoEntrada} > @CustoMinimo)
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
                DataFim = dataFim?.Date.AddDays(1),
                CustoMinimo = custoMinimo,
                Limite = LimiteBuscaEan
            },
            commandTimeout: 90,
            cancellationToken: cancellationToken));

        return registros.AsList();
    }

    public async Task<IReadOnlyDictionary<int, (string Status, string Reaberta)>> ListarSituacaoPorCodCompraAsync(
        IReadOnlyCollection<int> codigos,
        CancellationToken cancellationToken = default)
    {
        var distintos = codigos.Where(c => c > 0).Distinct().ToArray();
        if (distintos.Length == 0)
            return new Dictionary<int, (string, string)>();

        await using var connection = CriarConexao();
        await connection.OpenAsync(cancellationToken);

        var mapa = new Dictionary<int, (string Status, string Reaberta)>();
        foreach (var lote in distintos.Chunk(400))
        {
            var linhas = await connection.QueryAsync<(int CodCompra, string? Status, string? Reaberta)>(
                new CommandDefinition(
                    """
                    SELECT CODCOMPRA AS CodCompra,
                           COALESCE(STATUS, '') AS Status,
                           COALESCE(REABERTA, '') AS Reaberta
                    FROM compras
                    WHERE CODCOMPRA IN @Codigos
                    """,
                    new { Codigos = lote },
                    commandTimeout: 60,
                    cancellationToken: cancellationToken));

            foreach (var linha in linhas)
                mapa[linha.CodCompra] = (linha.Status?.Trim() ?? string.Empty, linha.Reaberta?.Trim() ?? string.Empty);
        }

        return mapa;
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
