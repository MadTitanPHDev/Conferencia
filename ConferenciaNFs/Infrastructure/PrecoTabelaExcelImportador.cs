using ClosedXML.Excel;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class PrecoTabelaExcelImportador
{
    public static IReadOnlyList<PrecoTabelaItem> Ler(string caminhoArquivo)
    {
        using var workbook = new XLWorkbook(caminhoArquivo);
        var planilha = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("A planilha nao tem abas.");

        var itens = new List<PrecoTabelaItem>();
        var vistos = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in planilha.RowsUsed())
        {
            var ean = EanNormalizer.Normalizar(CelulaTexto(row, 1));
            if (!EanNormalizer.EhValido(ean))
                continue;

            var preco1 = LerPreco(row, 3);
            if (!preco1.HasValue || preco1.Value <= 0)
                continue;

            if (!vistos.Add(ean))
            {
                var existente = itens.First(i => i.Ean == ean);
                existente.NomeProd = CelulaTexto(row, 2);
                existente.PrecoPrimario = preco1.Value;
                existente.PrecoSecundario = LerPreco(row, 4);
                continue;
            }

            itens.Add(new PrecoTabelaItem
            {
                Ean = ean,
                NomeProd = CelulaTexto(row, 2),
                PrecoPrimario = preco1.Value,
                PrecoSecundario = LerPreco(row, 4)
            });
        }

        return itens;
    }

    private static string CelulaTexto(IXLRow row, int coluna)
    {
        var cell = row.Cell(coluna);
        if (cell.IsEmpty())
            return string.Empty;

        return cell.GetFormattedString().Trim();
    }

    private static decimal? LerPreco(IXLRow row, int coluna)
    {
        var cell = row.Cell(coluna);
        if (cell.IsEmpty())
            return null;

        if (cell.DataType == XLDataType.Number)
            return Convert.ToDecimal(cell.GetDouble());

        var texto = cell.GetFormattedString();
        if (string.IsNullOrWhiteSpace(texto) || texto.Equals("nan", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var valor = ValorMonetarioParser.Parse(texto);
            return valor > 0 ? valor : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
