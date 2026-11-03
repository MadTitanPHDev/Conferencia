using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Pendência aberta enviada ao comprador acompanha o status definido na conferência.
/// Amarelo pode manter ou criar a pendência. Qualquer outro status encerra as abertas.
/// </summary>
public static class PendenciaAcompanhaStatus
{
    public static bool DeveEncerrarAbertas(string? novoStatus)
        => !string.Equals(
            novoStatus?.Trim(),
            StatusConferenciaValues.Amarelo,
            StringComparison.OrdinalIgnoreCase);
}
