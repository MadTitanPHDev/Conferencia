using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Mesma compra VSM (<c>CodCompra</c>) que muda de dia: a linha antiga
/// deve ir para o dia novo, sem criar duplicata nem reaplicar herança.
/// </summary>
public static class RelocacaoNotaVsm
{
    public static (string Status, string Observacao) StatusAposMesclar(
        string? statusDestino,
        string? observacaoDestino,
        string? statusOrigem,
        string? observacaoOrigem)
    {
        var dest = (statusDestino ?? string.Empty).Trim();
        var orig = (statusOrigem ?? string.Empty).Trim();
        var obsDest = observacaoDestino?.Trim() ?? string.Empty;
        var obsOrig = observacaoOrigem?.Trim() ?? string.Empty;

        if (DestinoJaConferidoDeVerdade(dest))
            return (dest, obsDest);

        if (StatusHerancaImportacao.TemStatusConferido(orig))
            return (orig, obsOrig);

        if (string.IsNullOrWhiteSpace(dest))
            return (StatusConferenciaValues.Pendente, obsDest);

        return (dest, obsDest);
    }

    /// <summary>
    /// Laranja na linha nova costuma ser herança do dia anterior, não conferência nova.
    /// </summary>
    private static bool DestinoJaConferidoDeVerdade(string status) =>
        StatusHerancaImportacao.TemStatusConferido(status)
        && !string.Equals(status, StatusConferenciaValues.Laranja, StringComparison.OrdinalIgnoreCase);
}
