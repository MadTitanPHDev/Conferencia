using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class StatusHerancaImportacao
{
    public static (string Status, string Observacao) Calcular(
        string? statusAnterior,
        string? observacaoAnterior,
        bool devolucaoJaRegistrada)
    {
        _ = devolucaoJaRegistrada;

        var status = string.IsNullOrWhiteSpace(statusAnterior)
            ? StatusConferenciaValues.Pendente
            : statusAnterior.Trim();
        var observacao = observacaoAnterior?.Trim() ?? string.Empty;

        // Reentrada de outro dia: ja conferida vira laranja; ainda pendente vira amarelo.
        var herdado = status switch
        {
            StatusConferenciaValues.Amarelo => StatusConferenciaValues.Amarelo,
            StatusConferenciaValues.Verde => StatusConferenciaValues.Laranja,
            StatusConferenciaValues.Vermelho => StatusConferenciaValues.Laranja,
            StatusConferenciaValues.Azul => StatusConferenciaValues.Laranja,
            StatusConferenciaValues.Laranja => StatusConferenciaValues.Laranja,
            StatusConferenciaValues.Pendente => StatusConferenciaValues.Amarelo,
            _ => StatusConferenciaValues.Amarelo
        };

        return (herdado, observacao);
    }

    /// <summary>
    /// Pendente nao conta: a nota ainda nao foi conferida por um operador.
    /// Laranja so nasce de um status ja definido (Verde, Azul, Laranja, Amarelo, Vermelho).
    /// </summary>
    public static bool TemStatusConferido(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        return !string.Equals(
            status.Trim(),
            StatusConferenciaValues.Pendente,
            StringComparison.OrdinalIgnoreCase);
    }
}
