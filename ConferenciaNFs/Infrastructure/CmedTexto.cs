using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ConferenciaNFs.Infrastructure;

public static partial class CmedTexto
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRegex();

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigitoRegex();

    public static string NormalizarCabecalho(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var simples = texto.Replace('\u00A0', ' ').Trim();
        var decomposto = simples.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);

        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                construtor.Append(caractere);
        }

        var semAcento = construtor.ToString().Normalize(NormalizationForm.FormC);
        return EspacosRegex().Replace(semAcento.ToUpperInvariant(), " ").Trim();
    }

    public static string RotuloPreco(string cabecalhoNormalizado, string prefixo)
    {
        if (!cabecalhoNormalizado.StartsWith(prefixo, StringComparison.Ordinal))
            return string.Empty;

        var resto = cabecalhoNormalizado[prefixo.Length..].Trim();
        resto = EspacosRegex().Replace(resto, " ");
        return resto.Replace(" %", "%").Trim();
    }

    public static string? NormalizarEan(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var digitos = NaoDigitoRegex().Replace(valor, string.Empty);
        return digitos.Length >= 8 ? digitos : null;
    }

    public static string Busca(params string?[] partes)
    {
        var texto = string.Join(' ', partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        return NormalizarCabecalho(texto).ToLowerInvariant();
    }
}
