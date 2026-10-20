using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Corrige duplicidades da mesma compra VSM (<c>CodCompra</c>) apenas dentro
/// do mesmo dia. Registros de dias anteriores permanecem como historico.
/// </summary>
public static class RelocacaoNotaVsm
{
    public static bool MesmoDiaConferencia(string? a, string? b)
    {
        var dataA = DataCompraParser.TentarConverter(a);
        var dataB = DataCompraParser.TentarConverter(b);
        if (dataA.HasValue && dataB.HasValue)
            return dataA.Value.Date == dataB.Value.Date;

        return string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

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
