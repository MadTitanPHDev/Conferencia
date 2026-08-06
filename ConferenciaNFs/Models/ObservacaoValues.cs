namespace ConferenciaNFs.Models;

public static class ObservacaoValues
{
    public const string Pbm = "PBM";
    public const string UsoEConsumo = "USO E CONSUMO";
    public const string Conveniencia = "CONVENIENCIA";
    public const string Bonificacao = "BONIFICACAO";
    public const string Encomenda = "ENCOMENDA";

    public static readonly IReadOnlyList<string> AtalhosRapidos =
    [
        Pbm,
        UsoEConsumo,
        Conveniencia,
        Bonificacao,
        Encomenda
    ];
}
