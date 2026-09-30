namespace ConferenciaNFs.Infrastructure;

public static class NomeFornecedorEscolha
{
    public static bool EhRotuloCnpj(string? nome, string? cnpj)
    {
        var texto = nome?.Trim() ?? string.Empty;
        if (texto.Length == 0)
            return true;

        var cnpjDigitos = CnpjNormalizer.Normalizar(cnpj);
        var nomeDigitos = CnpjNormalizer.Normalizar(texto);
        if (cnpjDigitos.Length > 0 && nomeDigitos == cnpjDigitos)
            return true;

        return texto.All(c => char.IsDigit(c) || c is '.' or '/' or '-' or ' ');
    }

    public static string Escolher(IEnumerable<string?> candidatos, string? cnpj)
    {
        var validos = candidatos
            .Select(c => c?.Trim() ?? string.Empty)
            .Where(c => c.Length > 0 && !EhRotuloCnpj(c, cnpj))
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key.Length)
            .Select(g => g.First())
            .ToList();

        return validos.Count == 0 ? string.Empty : validos[0];
    }
}
