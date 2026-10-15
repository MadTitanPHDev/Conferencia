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

    /// <summary>
    /// Chave para cruzar NF x planilha: só dígitos, sem zeros à esquerda.
    /// 070341368572 e 70341368572 viram a mesma chave.
    /// </summary>
    public static string ChaveComparacao(string? ean)
    {
        var digits = Normalizar(ean);
        if (digits.Length == 0)
            return string.Empty;

        var semZeros = digits.TrimStart('0');
        return semZeros.Length == 0 ? string.Empty : semZeros;
    }

    public static bool EhValido(string? ean)
        => Normalizar(ean).Length >= 8;

    public static bool SaoEquivalentes(string? a, string? b)
    {
        var chaveA = ChaveComparacao(a);
        var chaveB = ChaveComparacao(b);
        return chaveA.Length > 0 && string.Equals(chaveA, chaveB, StringComparison.Ordinal);
    }
}
