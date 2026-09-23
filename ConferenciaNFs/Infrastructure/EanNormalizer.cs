namespace ConferenciaNFs.Infrastructure;

public static class EanNormalizer
{
    public static string Normalizar(string? ean)
    {
        if (string.IsNullOrWhiteSpace(ean))
            return string.Empty;

        var texto = ean.Trim();
        if (texto.EndsWith(".0", StringComparison.Ordinal))
            texto = texto[..^2];

        var digits = texto.Where(char.IsDigit).ToArray();
        return new string(digits);
    }

    public static bool EhValido(string? ean)
        => Normalizar(ean).Length >= 8;
}
