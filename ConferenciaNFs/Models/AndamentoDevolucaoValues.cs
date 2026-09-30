namespace ConferenciaNFs.Models;

public sealed record AndamentoDevolucaoOpcao(string Valor, string Rotulo);

public static class AndamentoDevolucaoValues
{
    public const string EnviarXml = "EnviarXml";
    public const string AguardandoLoja = "AguardandoLoja";
    public const string AguardandoDist = "AguardandoDist";

    public const string FiltroTodos = "Todos os andamentos";

    public static readonly IReadOnlyList<string> Todos =
    [
        EnviarXml,
        AguardandoLoja,
        AguardandoDist
    ];

    public static readonly IReadOnlyList<AndamentoDevolucaoOpcao> Opcoes =
    [
        new(EnviarXml, "Enviar XML"),
        new(AguardandoLoja, "Aguardando loja"),
        new(AguardandoDist, "Aguardando dist.")
    ];

    public static readonly IReadOnlyList<string> Filtros =
    [
        FiltroTodos,
        "Enviar XML",
        "Aguardando loja",
        "Aguardando dist."
    ];

    public static string ObterRotulo(string andamento) => andamento switch
    {
        EnviarXml => "Enviar XML",
        AguardandoLoja => "Aguardando loja",
        AguardandoDist => "Aguardando dist.",
        _ => andamento
    };

    public static string? DeFiltro(string filtro) => filtro switch
    {
        "Enviar XML" => EnviarXml,
        "Aguardando loja" => AguardandoLoja,
        "Aguardando dist." => AguardandoDist,
        _ => null
    };

    public static bool EhValido(string andamento) =>
        andamento is EnviarXml or AguardandoLoja or AguardandoDist;
}
