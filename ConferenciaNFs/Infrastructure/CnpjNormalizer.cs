namespace ConferenciaNFs.Infrastructure;

public static class CnpjNormalizer
{
    /// <summary>
    /// Mantem apenas digitos para comparar/cadastrar CNPJ de forma consistente.
    /// </summary>
    public static string Normalizar(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return string.Empty;

        return new string(cnpj.Where(char.IsDigit).ToArray());
    }

    public static bool SaoEquivalentes(string? a, string? b)
        => string.Equals(Normalizar(a), Normalizar(b), StringComparison.Ordinal);
}
