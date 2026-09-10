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

    /// <summary>
    /// Sem o mesmo numero no historico: NF emitida antes do ultimo dia ja conferido
    /// e "de outro dia" (lote que a equipe ja fechou).
    /// </summary>
    public static bool DeveMarcarLaranjaPorEmissao(
        string? dataEmissao,
        string? diaConferenciaAnterior)
    {
        var emissao = DataCompraParser.TentarConverter(dataEmissao);
        var anterior = DataCompraParser.TentarConverter(diaConferenciaAnterior);
        if (!emissao.HasValue || !anterior.HasValue)
            return false;

        return emissao.Value.Date < anterior.Value.Date;
    }
}
