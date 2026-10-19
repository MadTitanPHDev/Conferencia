using ConferenciaNFs.Infrastructure;

namespace ConferenciaNFs.Models;

public sealed class NotaPesquisaResultado
{
    public long Id { get; init; }
    public long? DevolucaoId { get; init; }
    public string NumNota { get; init; } = string.Empty;
    public string ApelidoLoja { get; init; } = string.Empty;
    public string NomeForn { get; init; } = string.Empty;
    public string CnpjForn { get; init; } = string.Empty;
    public string DiaConferencia { get; init; } = string.Empty;
    public string DataEmissao { get; init; } = string.Empty;
    public string StatusConferencia { get; init; } = string.Empty;
    public string Observacao { get; init; } = string.Empty;
    public string StatusDevolucao { get; init; } = string.Empty;
    public string AndamentoDevolucao { get; init; } = string.Empty;
    public int OcorrenciasTotais { get; init; }
    public bool EhPrimeiraOcorrencia { get; init; }

    public string DataPrimeiraConferencia => DiaConferencia;

    public bool EstaNaFila => DevolucaoId is > 0;

    public string StatusRotulo => StatusConferencia switch
    {
        StatusConferenciaValues.Pendente => "Branco",
        StatusConferenciaValues.Verde => "Verde",
        StatusConferenciaValues.Amarelo => "Amarelo",
        StatusConferenciaValues.Vermelho => "Vermelho",
        StatusConferenciaValues.Laranja => "Laranja",
        StatusConferenciaValues.Azul => "Azul",
        _ => StatusConferencia
    };

    public string StatusDescricao => StatusConferenciaValues.ObterDescricao(StatusConferencia);

    public string CorHex => StatusConferenciaCores.ObterCorHex(StatusConferencia);

    public string TextoCorHex =>
        StatusConferencia is StatusConferenciaValues.Pendente or StatusConferenciaValues.Amarelo
            ? "#1F1F1F"
            : "#FFFFFF";

    public string TextoFila
    {
        get
        {
            if (!EstaNaFila)
                return string.Empty;

            var status = string.IsNullOrWhiteSpace(StatusDevolucao)
                ? "Na fila"
                : StatusDevolucaoValues.ObterRotulo(StatusDevolucao);
            var andamento = AndamentoDevolucaoValues.ObterRotulo(AndamentoDevolucao);
            return string.IsNullOrWhiteSpace(andamento) ? status : $"{status} · {andamento}";
        }
    }

    public string TextoOcorrencias =>
        OcorrenciasTotais <= 1
            ? "Uma ocorrencia no historico"
            : $"{OcorrenciasTotais} ocorrencias no historico";
}
