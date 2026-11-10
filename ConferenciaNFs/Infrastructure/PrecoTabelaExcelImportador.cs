using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class PrecoTabelaExcelImportador
{
    private enum Coluna
    {
        Ean,
        Nome,
        Preco,
        Preco2,
        Loja
    }

    public static IReadOnlyList<PrecoTabelaItem> Ler(string caminhoArquivo)
    {
        using var workbook = new XLWorkbook(caminhoArquivo);
        var planilha = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("A planilha nao tem abas.");

        var linhas = planilha.RowsUsed().ToList();
        if (linhas.Count == 0)
            return [];

        var cabecalho = MapearCabecalho(linhas[0]);
        var inicio = cabecalho is null ? 0 : 1;
        var colunas = cabecalho ?? new Dictionary<Coluna, int>
        {
            [Coluna.Ean] = 1,
            [Coluna.Nome] = 2,
            [Coluna.Preco] = 3,
            [Coluna.Preco2] = 4
        };

        var itens = new List<PrecoTabelaItem>();
        var vistos = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var i = inicio; i < linhas.Count; i++)
        {
            var row = linhas[i];
            var ean = EanNormalizer.Normalizar(CelulaTexto(row, ColunaDe(colunas, Coluna.Ean)));
            if (!EanNormalizer.EhValido(ean))
                continue;

            var chaveEan = EanNormalizer.ChaveComparacao(ean);
            if (chaveEan.Length == 0)
                continue;

            var preco1 = LerPreco(row, ColunaDe(colunas, Coluna.Preco));
            if (!preco1.HasValue || preco1.Value <= 0)
                continue;

            var loja = colunas.ContainsKey(Coluna.Loja)
                ? NormalizarLoja(CelulaTexto(row, colunas[Coluna.Loja]))
                : string.Empty;
            var item = new PrecoTabelaItem
            {
                Ean = ean,
                NomeProd = CelulaTexto(row, ColunaDe(colunas, Coluna.Nome)),
                PrecoPrimario = preco1.Value,
                PrecoSecundario = LerPreco(row, ColunaDe(colunas, Coluna.Preco2)),
                Loja = loja
            };

            var chave = chaveEan + "|" + loja;
            if (vistos.TryGetValue(chave, out var indice))
            {
                itens[indice] = item;
                continue;
            }

            vistos[chave] = itens.Count;
            itens.Add(item);
        }

        return itens;
    }

    public static string NormalizarLoja(string? texto)
    {
        var limpo = (texto ?? string.Empty).Trim();
        if (limpo.Length == 0)
            return string.Empty;

        if (int.TryParse(limpo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var codigo))
        {
            var peloCodigo = LojaVsmMapSeed.Lojas.FirstOrDefault(l => l.CodLoja == codigo);
            if (peloCodigo is not null)
                return peloCodigo.ApelidoLoja;
        }

        return LojaAliasNormalizer.Normalizar(limpo);
    }

    private static int ColunaDe(IReadOnlyDictionary<Coluna, int> colunas, Coluna coluna) =>
        colunas.TryGetValue(coluna, out var indice) ? indice : 0;

    private static Dictionary<Coluna, int>? MapearCabecalho(IXLRow row)
    {
        var mapa = new Dictionary<Coluna, int>();
        foreach (var cell in row.CellsUsed())
        {
            var chave = Simplificar(cell.GetFormattedString());
            if (chave.Length == 0)
                continue;

            if (!mapa.ContainsKey(Coluna.Ean) && EhEan(chave))
                mapa[Coluna.Ean] = cell.Address.ColumnNumber;
            else if (!mapa.ContainsKey(Coluna.Loja) && chave is "LOJA" or "FILIAL")
                mapa[Coluna.Loja] = cell.Address.ColumnNumber;
            else if (!mapa.ContainsKey(Coluna.Preco2) && EhPrecoSecundario(chave))
                mapa[Coluna.Preco2] = cell.Address.ColumnNumber;
            else if (!mapa.ContainsKey(Coluna.Preco) && EhPreco(chave))
                mapa[Coluna.Preco] = cell.Address.ColumnNumber;
            else if (!mapa.ContainsKey(Coluna.Nome) && chave is "NOME" or "PRODUTO" or "DESCRICAO")
                mapa[Coluna.Nome] = cell.Address.ColumnNumber;
        }

        if (!mapa.ContainsKey(Coluna.Ean) || !mapa.ContainsKey(Coluna.Preco))
            return null;

        return mapa;
    }

    private static bool EhEan(string chave) =>
        chave is "EAN" or "BARRAS" or "CODIGO" or "CODIGO DE BARRAS" or "CODIGOBARRAS";

    private static bool EhPreco(string chave) =>
        chave is "PRECO" or "PRECO PRIMARIO" or "COMPRA" or "VALOR" or "CUSTO";

    private static bool EhPrecoSecundario(string chave) =>
        chave.Contains("PRECO 2", StringComparison.Ordinal)
        || chave.Contains("PRECO2", StringComparison.Ordinal)
        || chave.Contains("SECUNDARIO", StringComparison.Ordinal)
        || chave.Contains("COM ST", StringComparison.Ordinal)
        || chave is "ST" or "PRECO ST";

    private static string Simplificar(string valor)
    {
        var texto = valor.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(texto.Length);
        foreach (var caractere in texto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(caractere);
        }

        return builder.ToString().Trim();
    }

    private static string CelulaTexto(IXLRow row, int coluna)
    {
        if (coluna <= 0)
            return string.Empty;

        var cell = row.Cell(coluna);
        if (cell.IsEmpty())
            return string.Empty;

        return cell.GetFormattedString().Trim();
    }

    private static decimal? LerPreco(IXLRow row, int coluna)
    {
        if (coluna <= 0)
            return null;

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
