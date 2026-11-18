using System.Globalization;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Celulas de preco da CMED vem como texto com virgula decimal.
/// O asterisco marca isencao de ICMS e nao altera o valor.
/// </summary>
public static class CmedNumero
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static decimal? Converter(object? valor)
    {
        switch (valor)
        {
            case null:
                return null;
            case decimal d:
                return d;
            case double db:
                return (decimal)db;
            case float f:
                return (decimal)f;
            case int i:
                return i;
            case long l:
                return l;
            default:
                return Converter(valor.ToString());
        }
    }

    public static decimal? Converter(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var limpo = texto.Replace('\u00A0', ' ').Trim();
        limpo = limpo.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase);
        limpo = limpo.Replace("*", string.Empty).Trim();

        if (limpo.Length == 0 || limpo.Trim(' ', '-', '.', '—').Length == 0)
            return null;

        limpo = limpo.Replace(" ", string.Empty);

        if (limpo.Contains(','))
            limpo = limpo.Replace(".", string.Empty).Replace(',', '.');

        if (decimal.TryParse(limpo, NumberStyles.Number, CultureInfo.InvariantCulture, out var convertido))
            return convertido;

        return decimal.TryParse(limpo, NumberStyles.Number, PtBr, out convertido) ? convertido : null;
    }
}
