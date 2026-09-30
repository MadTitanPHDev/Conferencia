using ClosedXML.Excel;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class DevolucoesExportador
{
    public static void Exportar(
        string caminhoArquivo,
        IReadOnlyList<DevolucaoItem> notas,
        IReadOnlyDictionary<long, IReadOnlyList<DevolucaoItemPreNota>> itensPorDevolucao)
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add("devolucoes");

        var cabecalhos = new[]
        {
            "ID", "Loja", "Nota", "Data", "Valor", "Valor item", "Motivo", "Classe", "Fornecedor", "Observações"
        };

        for (var col = 0; col < cabecalhos.Length; col++)
            planilha.Cell(1, col + 1).Value = cabecalhos[col];

        var linha = 2;
        foreach (var nota in notas)
        {
            if (itensPorDevolucao.TryGetValue(nota.Id, out var lista) && lista.Count > 0)
            {
                foreach (var item in lista)
                    linha = EscreverLinha(planilha, linha, nota, item);
            }
            else
            {
                linha = EscreverLinha(planilha, linha, nota, null);
            }
        }

        planilha.Columns(1, cabecalhos.Length).AdjustToContents();
        workbook.SaveAs(caminhoArquivo);
    }

    private static int EscreverLinha(
        IXLWorksheet planilha,
        int linha,
        DevolucaoItem nota,
        DevolucaoItemPreNota? item)
    {
        planilha.Cell(linha, 1).Value = nota.Id;
        planilha.Cell(linha, 2).Value = nota.ApelidoLoja;
        planilha.Cell(linha, 3).Value = nota.NumNota;
        planilha.Cell(linha, 4).Value = nota.DataEmissao;

        planilha.Cell(linha, 5).Value = (double)nota.ValorNota;
        planilha.Cell(linha, 5).Style.NumberFormat.Format = "R$ #,##0.00";

        if (item is not null)
        {
            planilha.Cell(linha, 6).Value = (double)(item.Quantidade * item.CustoNota);
            planilha.Cell(linha, 6).Style.NumberFormat.Format = "R$ #,##0.00";
            planilha.Cell(linha, 7).Value = item.Motivo;
            planilha.Cell(linha, 8).Value = item.Classe;
        }

        planilha.Cell(linha, 9).Value = nota.NomeForn;
        planilha.Cell(linha, 10).Value = nota.Observacao;
        return linha + 1;
    }
}
