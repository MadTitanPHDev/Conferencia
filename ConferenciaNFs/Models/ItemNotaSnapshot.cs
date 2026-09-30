namespace ConferenciaNFs.Models;

/// <summary>
/// Cópia do item da NF no Postgres (não depende do VSM depois de gravada).
/// </summary>
public sealed class ItemNotaSnapshot
{
    public long Id { get; set; }
    public long NotaFiscalId { get; set; }
    public int? CodCompra { get; set; }
    public int Sequencia { get; set; }
    public int CodProd { get; set; }
    public string NomeProd { get; set; } = string.Empty;
    public string BarrasEan { get; set; } = string.Empty;
    public string BarrasEanTrib { get; set; } = string.Empty;
    public decimal QuantItemCompra { get; set; }
    public decimal ValorItemCompra { get; set; }
    public decimal Desconto { get; set; }
    public decimal CustoNota { get; set; }
    public string? NumLote { get; set; }
    public string? DataValidade { get; set; }
    public string AtualizadoEm { get; set; } = string.Empty;
}
