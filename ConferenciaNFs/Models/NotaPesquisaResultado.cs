using ConferenciaNFs.Infrastructure;

namespace ConferenciaNFs.Models;

public sealed class NotaPesquisaResultado
{
    public string NumNota { get; init; } = string.Empty;
    public string ApelidoLoja { get; init; } = string.Empty;
    public string NomeForn { get; init; } = string.Empty;
    public string DataPrimeiraConferencia { get; init; } = string.Empty;
    public string StatusConferencia { get; init; } = string.Empty;
    public string Observacao { get; init; } = string.Empty;
    public int OcorrenciasTotais { get; init; }

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
}
