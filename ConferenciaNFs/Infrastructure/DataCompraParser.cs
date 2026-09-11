using System.Globalization;

namespace ConferenciaNFs.Infrastructure;

public static class DataCompraParser
{
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] Formatos = ["dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "d/M/yy"];

    public const int MaxDiasIntervaloPadrao = 7;

    public static DateTime DiaPadraoAbertura() => DateTime.Today.AddDays(-1);

    public static string Formatar(DateTime data) =>
        data.ToString("dd/MM/yyyy", CulturaPtBr);

    public static string FormatarIntervalo(DateTime inicio, DateTime fim)
    {
        var a = inicio.Date;
        var b = fim.Date;
        if (a == b)
            return Formatar(a);

        return $"{Formatar(a)} a {Formatar(b)}";
    }

    public static bool IntervaloValido(DateTime? inicio, DateTime? fim, int maxDias = MaxDiasIntervaloPadrao)
    {
        if (!inicio.HasValue || !fim.HasValue)
            return false;

        var a = inicio.Value.Date;
        var b = fim.Value.Date;
        if (b < a)
            return false;

        return (b - a).Days + 1 <= maxDias;
    }

    /// <summary>
    /// Dias inclusivos de inicio ate fim. Falha se o fim vier antes do inicio ou se passar de maxDias.
    /// </summary>
    public static IReadOnlyList<DateTime> EnumerarDias(
        DateTime inicio,
        DateTime fim,
        int maxDias = MaxDiasIntervaloPadrao)
    {
        var a = inicio.Date;
        var b = fim.Date;
        if (b < a)
            throw new ArgumentException("A data final nao pode ser anterior a data inicial.", nameof(fim));

        var total = (b - a).Days + 1;
        if (total > maxDias)
            throw new ArgumentOutOfRangeException(
                nameof(fim),
                $"O intervalo nao pode ter mais de {maxDias} dia(s).");

        var dias = new List<DateTime>(total);
        for (var d = a; d <= b; d = d.AddDays(1))
            dias.Add(d);

        return dias;
    }

    public static IReadOnlyList<string> FormatarDias(IEnumerable<DateTime> dias) =>
        dias.Select(Formatar).ToList();

    public static DateTime? TentarConverter(string? dataCompra)
    {
        if (string.IsNullOrWhiteSpace(dataCompra))
            return null;

        if (DateTime.TryParseExact(dataCompra.Trim(), Formatos, CulturaPtBr, DateTimeStyles.None, out var data))
            return data;

        if (DateTime.TryParse(dataCompra, CulturaPtBr, DateTimeStyles.None, out data))
            return data;

        return null;
    }

    public static int Comparar(string? dataA, string? dataB)
    {
        var parseA = TentarConverter(dataA);
        var parseB = TentarConverter(dataB);

        if (parseA.HasValue && parseB.HasValue)
            return parseB.Value.CompareTo(parseA.Value);

        if (parseA.HasValue)
            return -1;

        if (parseB.HasValue)
            return 1;

        return string.Compare(dataB, dataA, StringComparison.Ordinal);
    }

    public static int CompararAscendente(string? dataA, string? dataB)
    {
        var parseA = TentarConverter(dataA);
        var parseB = TentarConverter(dataB);

        if (parseA.HasValue && parseB.HasValue)
            return parseA.Value.CompareTo(parseB.Value);

        if (parseA.HasValue)
            return -1;

        if (parseB.HasValue)
            return 1;

        return string.Compare(dataA, dataB, StringComparison.OrdinalIgnoreCase);
    }
}
