using ClosedXML.Excel;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class ConferenciaExportador
{
    private const int ColLoja = 1;
    private const int ColNumNota = 2;
    private const int ColDistribuidora = 3;
    private const int ColValorNota = 4;
    private const int ColObservacao = 5;
    private const int TotalColunas = 5;

    public static void Exportar(string caminhoArquivo, string apelidoLoja, IEnumerable<NotaFiscal> notas)
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add(SanitizarNomeAba(apelidoLoja));

        EscreverCabecalho(planilha);
        EscreverNotas(planilha, notas);

        planilha.Columns(1, TotalColunas).AdjustToContents();
        planilha.SheetView.FreezeRows(1);

        workbook.SaveAs(caminhoArquivo);
    }

    public static void ExportarTodas(string caminhoArquivo, string dataCompra, IEnumerable<NotaFiscal> notas)
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add(SanitizarNomeAba($"Conferencia {dataCompra}"));

        EscreverCabecalho(planilha);
        EscreverNotas(planilha, notas);

        planilha.Columns(1, TotalColunas).AdjustToContents();
        planilha.SheetView.FreezeRows(1);

        workbook.SaveAs(caminhoArquivo);
    }

    private static void EscreverNotas(IXLWorksheet planilha, IEnumerable<NotaFiscal> notas)
    {
        var linha = 2;
        foreach (var nota in notas)
        {
            planilha.Cell(linha, ColLoja).Value = nota.ApelidoLoja;
            planilha.Cell(linha, ColNumNota).Value = nota.NumNota;
            planilha.Cell(linha, ColDistribuidora).Value = nota.NomeForn;
            planilha.Cell(linha, ColValorNota).Value = (double)nota.ValorNota;
            planilha.Cell(linha, ColValorNota).Style.NumberFormat.Format = "R$ #,##0.00";
            planilha.Cell(linha, ColObservacao).Value = nota.Observacao;

            var corFundo = XLColor.FromHtml(StatusConferenciaCores.ObterCorHex(nota.StatusConferencia));
            planilha.Range(linha, 1, linha, TotalColunas).Style.Fill.BackgroundColor = corFundo;
            linha++;
        }
    }

    private static void EscreverCabecalho(IXLWorksheet planilha)
    {
        planilha.Cell(1, ColLoja).Value = "Loja";
        planilha.Cell(1, ColNumNota).Value = "Num Nota";
        planilha.Cell(1, ColDistribuidora).Value = "Distribuidora";
        planilha.Cell(1, ColValorNota).Value = "Valor Nota";
        planilha.Cell(1, ColObservacao).Value = "Observacao";

        var cabecalho = planilha.Range(1, 1, 1, TotalColunas);
        cabecalho.Style.Font.Bold = true;
        cabecalho.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0E0E0");
        cabecalho.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static string SanitizarNomeAba(string nome)
    {
        var invalidos = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        var sanitizado = new string(nome.Select(c => invalidos.Contains(c) ? '_' : c).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(sanitizado))
            sanitizado = "Conferencia";

        return sanitizado.Length > 31 ? sanitizado[..31] : sanitizado;
    }
}
