namespace ConferenciaNFs.Models;

public sealed class PendenciaAviso
{
    public long Id { get; set; }
    public string NumNota { get; set; } = string.Empty;
    public string ApelidoLoja { get; set; } = string.Empty;
    public string EnviadoPorNome { get; set; } = string.Empty;
    public string StatusPendencia { get; set; } = string.Empty;
}
