namespace ConferenciaNFs.Models;

public sealed class LojaPendencia
{
    public string ApelidoLoja { get; set; } = string.Empty;
    public int Pendencias { get; set; }
    public int NumeroOrdem { get; set; } = 9999;
    public string NomeExibicao { get; set; } = string.Empty;

    public bool PossuiOrdemCadastrada => NumeroOrdem < 9999;

    public bool TemPendencias => Pendencias > 0;

    public string TituloCard => PossuiOrdemCadastrada
        ? $"{NumeroOrdem} - {NomeExibicao}"
        : ApelidoLoja;

    public string TextoStatusCard => TemPendencias
        ? "Clique para conferir"
        : "Conferida - clique para reabrir";
}
