namespace ConferenciaNFs.Infrastructure;

public static class NumNotaNormalizer
{
    /// <summary>
    /// Remove espacos e zeros a esquerda para cruzar CSV ("0123") com VSM ("123").
    /// </summary>
    public static string Normalizar(string? numNota)
    {
        if (string.IsNullOrWhiteSpace(numNota))
            return string.Empty;

        var semZeros = numNota.Trim().TrimStart('0');
        return semZeros.Length == 0 ? "0" : semZeros;
    }

    public static bool SaoEquivalentes(string? a, string? b)
        => string.Equals(Normalizar(a), Normalizar(b), StringComparison.OrdinalIgnoreCase);
}
