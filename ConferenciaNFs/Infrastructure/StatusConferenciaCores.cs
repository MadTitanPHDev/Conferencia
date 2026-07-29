using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class StatusConferenciaCores
{
    public const string Verde = "#92D050";
    public const string Amarelo = "#FFFF00";
    public const string Vermelho = "#FF0000";
    public const string Azul = "#2F75B5";
    public const string Laranja = "#FFC000";
    public const string Pendente = "#FFFFFF";

    public static string ObterCorHex(string status) => status switch
    {
        StatusConferenciaValues.Verde => Verde,
        StatusConferenciaValues.Amarelo => Amarelo,
        StatusConferenciaValues.Vermelho => Vermelho,
        StatusConferenciaValues.Azul => Azul,
        StatusConferenciaValues.Laranja => Laranja,
        StatusConferenciaValues.Pendente => Pendente,
        _ => Pendente
    };
}
