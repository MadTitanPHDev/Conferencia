namespace ConferenciaNFs.Models;

/// <summary>
/// Mapa estavel CODLOJA (VSM) → apelido usado no ConferenciaNFs.
/// Independente da ordem de exibicao em LojaOrdem.
/// </summary>
public sealed class LojaVsmMap
{
    public int CodLoja { get; set; }
    public string ApelidoLoja { get; set; } = string.Empty;
    public string NomeExibicao { get; set; } = string.Empty;
    public string CnpjLoja { get; set; } = string.Empty;
}
