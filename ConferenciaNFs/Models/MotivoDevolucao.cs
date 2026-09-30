namespace ConferenciaNFs.Models;

public sealed class MotivoDevolucao
{
    public long Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
