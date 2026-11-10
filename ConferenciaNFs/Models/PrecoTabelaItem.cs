namespace ConferenciaNFs.Models;

public sealed class PrecoTabelaItem
{
    public long ImportacaoId { get; set; }
    public string Ean { get; set; } = string.Empty;
    public string NomeProd { get; set; } = string.Empty;
    public decimal PrecoPrimario { get; set; }
    public decimal? PrecoSecundario { get; set; }
    public string Loja { get; set; } = string.Empty;
    public string NomeImportacao { get; set; } = string.Empty;
}
