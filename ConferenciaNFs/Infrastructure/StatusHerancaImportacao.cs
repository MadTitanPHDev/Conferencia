using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class StatusHerancaImportacao
{
    public static (string Status, string Observacao) Calcular(
        string? statusAnterior,
        string? observacaoAnterior,
        bool devolucaoConcluida)
    {
        var status = string.IsNullOrWhiteSpace(statusAnterior)
            ? StatusConferenciaValues.Pendente
            : statusAnterior.Trim();
        var observacao = observacaoAnterior?.Trim() ?? string.Empty;

        if (status == StatusConferenciaValues.Vermelho)
        {
            return devolucaoConcluida
                ? (StatusConferenciaValues.Laranja, observacao)
                : (StatusConferenciaValues.Vermelho, observacao);
        }

        var herdado = status switch
        {
            StatusConferenciaValues.Amarelo => StatusConferenciaValues.Amarelo,
            StatusConferenciaValues.Verde => StatusConferenciaValues.Laranja,
            StatusConferenciaValues.Azul => StatusConferenciaValues.Laranja,
            StatusConferenciaValues.Laranja => StatusConferenciaValues.Laranja,
            StatusConferenciaValues.Pendente => StatusConferenciaValues.Pendente,
            _ => StatusConferenciaValues.Pendente
        };

        return (herdado, observacao);
    }
}
