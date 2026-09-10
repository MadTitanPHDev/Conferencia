namespace ConferenciaNFs.Models;

/// <summary>
/// Cabecalho de compra lido do VSM (MySQL myouro.compras). Somente leitura.
/// </summary>
public sealed class CompraVsm
{
    public int CodCompra { get; set; }
    public int CodLoja { get; set; }
    public string NumNota { get; set; } = string.Empty;
    public DateTime? DataEmissao { get; set; }
    public DateTime? DataCompra { get; set; }
    public int CodForn { get; set; }
    public string? CnpjForn { get; set; }
    public decimal ValorNota { get; set; }
    public string? Status { get; set; }
    public string? NfeChaveAcesso { get; set; }
    public string? SerieNota { get; set; }
}
