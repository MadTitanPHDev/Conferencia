namespace ConferenciaNFs.Models;

public sealed class NotaPesquisaFiltro
{
    public string Termo { get; init; } = string.Empty;
    public DateTime? DataInicio { get; init; }
    public DateTime? DataFim { get; init; }
    public string? StatusConferencia { get; init; }
    public string? ApelidoLoja { get; init; }
    public int Limite { get; init; } = 200;
}
