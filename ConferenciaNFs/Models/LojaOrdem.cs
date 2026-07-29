namespace ConferenciaNFs.Models;

public sealed class LojaOrdem
{
    public string ApelidoLoja { get; set; } = string.Empty;
    public int NumeroOrdem { get; set; }
    public string NomeExibicao { get; set; } = string.Empty;
}

public sealed class LojaSemOrdem
{
    public string ApelidoLoja { get; set; } = string.Empty;
}
