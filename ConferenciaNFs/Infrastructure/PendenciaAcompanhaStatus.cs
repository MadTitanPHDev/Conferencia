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

    /// <summary>
    /// A resposta Devolvida pede print na planilha enquanto a conferencia ainda devolve a nota.
    /// Verde, azul e branco saem dessa espera. Laranja sem fila tambem sai.
    /// </summary>
    public static bool AindaAguardaPrint(string? statusConferencia, bool temDevolucao)
    {
        var status = statusConferencia?.Trim() ?? string.Empty;
        if (string.Equals(status, StatusConferenciaValues.Vermelho, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, StatusConferenciaValues.Amarelo, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals(status, StatusConferenciaValues.Laranja, StringComparison.OrdinalIgnoreCase)
            && temDevolucao;
    }
}
