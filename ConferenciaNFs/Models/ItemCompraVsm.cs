namespace ConferenciaNFs.Models;

/// <summary>
/// Item de uma compra lido do VSM (MySQL myouro.itens_compra). Somente leitura.
/// </summary>
public sealed class ItemCompraVsm
{
    public int CodCompra { get; set; }
    public int CodLoja { get; set; }
    public int Sequencia { get; set; }
    public string? NumNota { get; set; }
    public int CodProd { get; set; }
    public string? NomeProd { get; set; }
    public decimal QuantItemCompra { get; set; }
    public decimal ValorItemCompra { get; set; }
    public decimal ValorItemFabrica { get; set; }
    public decimal Desconto { get; set; }

    /// <summary>ICMS-ST unitário do VSM (<c>CUSTO_VALORST</c>).</summary>
    public decimal CustoValorSt { get; set; }

    /// <summary>FCP-ST unitário do VSM (<c>CUSTO_VALORFCPST</c>).</summary>
    public decimal CustoValorFcpSt { get; set; }

    /// <summary>Custo após formação de preços no VSM. Costuma ficar 0 até acertarem o item.</summary>
    public decimal CustoUnit { get; set; }

    /// <summary>
    /// Quando a foto no Postgres já gravou o custo (com ST na loja PR), usa esse valor
    /// em vez de recalcular só com valor − desconto.
    /// </summary>
    public decimal? CustoNotaInformado { get; set; }

    /// <summary>
    /// Preço de compra da NF já com desconto do item.
    /// Na loja 29 (PR) inclui ICMS-ST e FCP-ST.
    /// </summary>
    public decimal CustoNota =>
        CustoNotaInformado ?? CustoItemNota.Calcular(
            CodLoja,
            ValorItemCompra,
            Desconto,
            CustoValorSt,
            CustoValorFcpSt);

    public decimal PrecoVendaNovo { get; set; }
    public decimal QuantDevol { get; set; }
    public decimal QuantConferida { get; set; }
    public string? CodProdDistr { get; set; }
    public string? BarrasEan { get; set; }
    public string? BarrasEanTrib { get; set; }
    public string? NumLote { get; set; }
    public DateTime? DataValidade { get; set; }
}
