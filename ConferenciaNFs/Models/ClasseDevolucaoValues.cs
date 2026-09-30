namespace ConferenciaNFs.Models;

public static class ClasseDevolucaoValues
{
    public const string Generico = "Generico";
    public const string Referencia = "Referencia";
    public const string Perfumaria = "Perfumaria";

    public static readonly IReadOnlyList<string> Todas =
    [
        Generico,
        Referencia,
        Perfumaria
    ];
}
