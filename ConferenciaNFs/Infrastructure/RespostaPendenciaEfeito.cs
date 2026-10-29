using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Resposta do comprador. A pendência muda de status e permanece no banco.
/// </summary>
public static class RespostaPendenciaEfeito
{
    public static string StatusNota(bool ficarComDivergencia) =>
        ficarComDivergencia
            ? StatusConferenciaValues.Verde
            : StatusConferenciaValues.Vermelho;

    public static string StatusPendencia(bool ficarComDivergencia) =>
        ficarComDivergencia
            ? StatusPendenciaCompraValues.Ficou
            : StatusPendenciaCompraValues.Devolvida;

    public static bool EntraNaFilaDevolucao(bool ficarComDivergencia) =>
        !ficarComDivergencia;
}
