using System.Text.RegularExpressions;

namespace ConferenciaNFs.Infrastructure;

public readonly record struct NotaFaltaPlanilha(long Id, string NumNota, string ApelidoLoja);

/// <summary>
/// Cruza a nota devolvida pelo comprador com a coluna A da aba da loja.
/// </summary>
public static partial class CruzamentoPlanilhaDevolucao
{
    public static readonly DateTime InicioPeriodo = new(2026, 11, 1);
    public static readonly DateTime FimPeriodo = new(2026, 12, 31);

    [GeneratedRegex(@"\((\d+)\)\s*$")]
    private static partial Regex CodigoAba();

    public static string ChaveNumero(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var digitos = new string(texto.Where(char.IsDigit).ToArray());
        var semZeros = digitos.TrimStart('0');
        return semZeros.Length < 4 ? string.Empty : semZeros;
    }

    public static int? CodLojaDaAba(string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            return null;

        var encontrado = CodigoAba().Match(titulo.Trim());
        return encontrado.Success && int.TryParse(encontrado.Groups[1].Value, out var codigo)
            ? codigo
            : null;
    }

    public static int? CodLojaPorApelido(string? apelido)
    {
        var normalizado = LojaAliasNormalizer.Normalizar(apelido);
        if (normalizado.Length == 0)
            return null;

        // O numero entre parenteses na planilha e o da tela do VSM.
        // Nas lojas 2 a 5 esse numero nao e o CodLoja corrigido do app:
        // Rancharia esta na aba (5), nao na (2).
        var loja = LojaOrdemSeed.LojasPadrao.FirstOrDefault(item =>
            LojaAliasNormalizer.Normalizar(item.ApelidoLoja) == normalizado
            || LojaAliasNormalizer.Normalizar(item.NomeExibicao) == normalizado);
        return loja?.NumeroOrdem;
    }

    public static bool DentroDoPeriodo(string? diaConferencia, string? respondidaEm)
    {
        var data = DataCompraParser.TentarConverter(respondidaEm)
            ?? DataCompraParser.TentarConverter(diaConferencia);
        if (data is null)
            return false;

        var dia = data.Value.Date;
        return dia >= InicioPeriodo && dia <= FimPeriodo;
    }

    public static IReadOnlyDictionary<int, HashSet<string>> IndexarAbas(
        IEnumerable<(string Aba, IEnumerable<string> ColunaA)> abas)
    {
        var indice = new Dictionary<int, HashSet<string>>();
        foreach (var (aba, coluna) in abas)
        {
            var codigo = CodLojaDaAba(aba);
            if (codigo is null)
                continue;

            if (!indice.TryGetValue(codigo.Value, out var numeros))
            {
                numeros = new HashSet<string>(StringComparer.Ordinal);
                indice[codigo.Value] = numeros;
            }

            foreach (var celula in coluna)
            {
                var chave = ChaveNumero(celula);
                if (chave.Length > 0)
                    numeros.Add(chave);
            }
        }

        return indice;
    }

    public static IReadOnlyList<NotaFaltaPlanilha> Faltantes(
        IEnumerable<(long Id, string NumNota, string ApelidoLoja, string? DiaConferencia, string? RespondidaEm)> devolvidas,
        IReadOnlyDictionary<int, HashSet<string>> planilha)
    {
        var faltam = new List<NotaFaltaPlanilha>();
        foreach (var nota in devolvidas)
        {
            if (!DentroDoPeriodo(nota.DiaConferencia, nota.RespondidaEm))
                continue;

            var codigo = CodLojaPorApelido(nota.ApelidoLoja);
            var numero = ChaveNumero(nota.NumNota);
            if (codigo is null || numero.Length == 0)
                continue;

            if (planilha.TryGetValue(codigo.Value, out var numeros) && numeros.Contains(numero))
                continue;

            faltam.Add(new NotaFaltaPlanilha(nota.Id, nota.NumNota.Trim(), nota.ApelidoLoja.Trim()));
        }

        return faltam;
    }

    public const string NaPlanilha = "Verde";
    public const string ForaDaPlanilha = "Amarelo";

    public static string? Estado(
        string? numNota,
        string? apelidoLoja,
        string? diaConferencia,
        string? respondidaEm,
        IReadOnlyDictionary<int, HashSet<string>> planilha)
    {
        if (!DentroDoPeriodo(diaConferencia, respondidaEm))
            return null;

        var codigo = CodLojaPorApelido(apelidoLoja);
        var numero = ChaveNumero(numNota);
        if (codigo is null || numero.Length == 0)
            return null;

        return planilha.TryGetValue(codigo.Value, out var numeros) && numeros.Contains(numero)
            ? NaPlanilha
            : ForaDaPlanilha;
    }
}
