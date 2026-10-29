namespace ConferenciaNFs.Models;

public sealed class Pessoa
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public string CriadaEm { get; set; } = string.Empty;

    public override string ToString() => Nome;
}
