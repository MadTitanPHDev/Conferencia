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
            "ID", "Loja", "Nota", "Status", "Data conclusao", "Data emissao",
            "Valor", "Valor item", "Motivo", "Classe", "Fornecedor", "Observações"
        };

        for (var col = 0; col < cabecalhos.Length; col++)
            planilha.Cell(1, col + 1).Value = cabecalhos[col];

        var linha = 2;
        foreach (var nota in notas)
        {
            itensPorDevolucao.TryGetValue(nota.Id, out var lista);
            linha = EscreverLinha(planilha, linha, nota, lista);
        }

        planilha.Columns(1, cabecalhos.Length).AdjustToContents();
        workbook.SaveAs(caminhoArquivo);
    }

    private static int EscreverLinha(
        IXLWorksheet planilha,
        int linha,
        DevolucaoItem nota,
        IReadOnlyList<DevolucaoItemPreNota>? itens)
    {
        planilha.Cell(linha, 1).Value = nota.Id;
        planilha.Cell(linha, 2).Value = nota.ApelidoLoja;
        planilha.Cell(linha, 3).Value = nota.NumNota;
        planilha.Cell(linha, 4).Value = nota.StatusRotulo;
        planilha.Cell(linha, 5).Value = nota.DataConclusao ?? "";
        planilha.Cell(linha, 6).Value = nota.DataEmissao;

        planilha.Cell(linha, 7).Value = (double)nota.ValorNota;
        planilha.Cell(linha, 7).Style.NumberFormat.Format = "R$ #,##0.00";

        if (itens is { Count: > 0 })
        {
            planilha.Cell(linha, 8).Value = (double)itens.Sum(i => i.Quantidade * i.CustoNota);
            planilha.Cell(linha, 8).Style.NumberFormat.Format = "R$ #,##0.00";
            planilha.Cell(linha, 9).Value = string.Join("; ",
                itens.Select(i => i.Motivo).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct());
            planilha.Cell(linha, 10).Value = string.Join("; ",
                itens.Select(i => i.Classe).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct());
        }

        planilha.Cell(linha, 11).Value = nota.NomeForn;
        planilha.Cell(linha, 12).Value = nota.Observacao;
        return linha + 1;
    }
}
