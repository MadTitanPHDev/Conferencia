namespace ConferenciaNFs.Models;

public sealed class DevolucaoObservacao
{
    public long Id { get; set; }
    public long DevolucaoId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string CriadaEm { get; set; } = string.Empty;

    public string Rotulo => string.IsNullOrWhiteSpace(CriadaEm)
        ? Texto
        : $"{CriadaEm} · {Texto}";
}
