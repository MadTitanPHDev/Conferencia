namespace ConferenciaNFs.Models;

public sealed class VsmSyncProgress
{
    public int Etapa { get; init; }
    public int Total { get; init; }
    public string Texto { get; init; } = string.Empty;
}
