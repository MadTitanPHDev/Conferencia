namespace ConferenciaNFs.Models;

public static class ComparacaoPrecoValores
{
    public const string Verde = "Verde";
    public const string AzulClaro = "AzulClaro";
    public const string Vermelho = "Vermelho";
    public const string Cinza = "Cinza";
    public const string Ambar = "Ambar";

    public const decimal ToleranciaReais = 0.02m;
    public const decimal FaixaAzulFator = 1.10m;

    public static string ObterRotulo(string? resultado) => resultado switch
    {
        Verde => "NF mais barata ou igual",
        AzulClaro => "Ate 10% mais cara",
        Vermelho => "NF mais cara",
        Cinza => "EAN fora da planilha",
        Ambar => "Sem EAN",
        _ => string.Empty
    };
}
