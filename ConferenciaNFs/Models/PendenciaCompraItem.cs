namespace ConferenciaNFs.Models;

public sealed class PendenciaCompraItem
{
    public long PendenciaId { get; set; }
    public int Sequencia { get; set; }
    public int CodProd { get; set; }
    public string NomeProd { get; set; } = string.Empty;
    public string BarrasEan { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal CustoNota { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
