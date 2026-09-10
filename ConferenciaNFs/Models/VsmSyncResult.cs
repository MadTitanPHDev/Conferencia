namespace ConferenciaNFs.Models;

public sealed class VsmSyncResult
{
    public int Lidas { get; set; }
    public int Inseridas { get; set; }
    public int Atualizadas { get; set; }
    public int Ignoradas { get; set; }
    public int Relocadas { get; set; }
}
