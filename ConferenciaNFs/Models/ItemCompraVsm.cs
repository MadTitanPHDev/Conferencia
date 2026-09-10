namespace ConferenciaNFs.Models;

/// <summary>
/// Item de uma compra lido do VSM (MySQL myouro.itens_compra). Somente leitura.
/// </summary>
public sealed class ItemCompraVsm
{
    public int CodCompra { get; set; }
    public int Sequencia { get; set; }
    public string? NumNota { get; set; }
    public int CodProd { get; set; }
    public string? NomeProd { get; set; }
    public decimal QuantItemCompra { get; set; }
    public decimal ValorItemCompra { get; set; }
    public decimal ValorItemFabrica { get; set; }
    public decimal CustoUnit { get; set; }
    public decimal PrecoVendaNovo { get; set; }
    public decimal QuantDevol { get; set; }
    public decimal QuantConferida { get; set; }
    public string? CodProdDistr { get; set; }
    public string? BarrasEan { get; set; }
    public string? NumLote { get; set; }
    public DateTime? DataValidade { get; set; }
}
