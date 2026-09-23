namespace ConferenciaNFs.Models;

public sealed class PrecoTabelaImportacao
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string CriadaEm { get; set; } = string.Empty;
    public int QuantidadeItens { get; set; }
}
