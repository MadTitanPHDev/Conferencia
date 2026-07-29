using System.Globalization;

namespace ConferenciaNFs.Infrastructure;

public static class DataCompraParser
{
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] Formatos = ["dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "d/M/yy"];

    public static DateTime DiaPadraoAbertura() => DateTime.Today.AddDays(-1);

    public static string Formatar(DateTime data) =>
        data.ToString("dd/MM/yyyy", CulturaPtBr);

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
