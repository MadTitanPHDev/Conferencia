namespace ConferenciaNFs.Infrastructure;

public static class MotivoDevolucaoSeed
{
    public static readonly IReadOnlyList<string> Padrao =
    [
        "Avaria",
        "Desistência",
        "Divergência de preço",
        "Divergência de produto",
        "Divergência de quantidade",
        "Erro de digitação",
        "Erro de faturamento",
        "Falta",
        "Nota duplicada",
        "Sem pedido",
        "Sobra",
        "Validade"
    ];
}
