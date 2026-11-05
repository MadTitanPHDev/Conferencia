using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ConferenciaNFs.Infrastructure;

public sealed record NomeDistribuidoraPlanilha(string Cnpj, string Nome);

public sealed record ResultadoImportacaoDistribuidoras(
    int Preenchidos,
    int JaTinhamNome,
    int PrediletaMantida,
    int ForaDoCadastro);

public static class ImportacaoDistribuidoraNomes
{
    public const string CnpjPredileta = "16619378000108";
    public const string NomePredileta = "PREDILETA SAO PAULO DISTRIBUIDORA";

    public static IReadOnlyList<NomeDistribuidoraPlanilha> Ler(string conteudo)
    {
        var celulas = Regex.Matches(conteudo, @"<td[^>]*>(.*?)</td>", RegexOptions.Singleline | RegexOptions.IgnoreCase)
            .Select(m => WebUtility.HtmlDecode(Regex.Replace(m.Groups[1].Value, @"<[^>]+>", " ")))
            .Select(s => Regex.Replace(s, @"\s+", " ").Trim())
            .Where(s => s.Length > 0)
            .ToList();

        var itens = new List<NomeDistribuidoraPlanilha>();
        for (var i = 0; i < celulas.Count; i++)
        {
            var cnpj = SomenteDigitos(celulas[i]);
            if (cnpj.Length != 14 || i + 1 >= celulas.Count)
                continue;

            var nome = celulas[i + 1].Trim();
            if (nome.Length == 0 || SomenteDigitos(nome) == cnpj)
                continue;

            itens.Add(new NomeDistribuidoraPlanilha(cnpj, nome));
            i++;
        }

        return itens;
    }

    public static bool NomeRuim(string? nome, string cnpj)
    {
        var limpo = (nome ?? string.Empty).Trim();
        if (limpo.Length == 0)
            return true;

        var digitos = SomenteDigitos(limpo);
        return digitos.Length >= 11 || digitos == cnpj;
    }

    public static string? NomeParaGravar(string cnpj, string? nomeAtual, string? nomePlanilha)
    {
        var planilha = (nomePlanilha ?? string.Empty).Trim();
        if (planilha.Length == 0)
            return null;

        if (cnpj == CnpjPredileta)
        {
            if (Chave(nomeAtual).Contains("PREDILETA", StringComparison.Ordinal))
                return null;

            return NomePredileta;
        }

        if (!NomeRuim(nomeAtual, cnpj))
            return null;

        return planilha;
    }

    private static string Chave(string? nome)
    {
        var form = (nome ?? string.Empty).Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }

    public static string SomenteDigitos(string? texto) =>
        new((texto ?? string.Empty).Where(char.IsDigit).ToArray());
}
