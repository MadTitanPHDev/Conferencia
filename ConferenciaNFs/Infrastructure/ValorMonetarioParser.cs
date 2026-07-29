using System.Globalization;

namespace ConferenciaNFs.Infrastructure;

public static class ValorMonetarioParser
{
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static decimal Parse(string? valorBruto)
    {
        if (string.IsNullOrWhiteSpace(valorBruto))
            return 0m;

        var normalizado = valorBruto.Trim().Trim('"').Trim();

        if (decimal.TryParse(normalizado, NumberStyles.Number, CulturaPtBr, out var resultado))
            return resultado;

        throw new FormatException($"Valor monetario invalido: \"{valorBruto}\".");
    }
}
