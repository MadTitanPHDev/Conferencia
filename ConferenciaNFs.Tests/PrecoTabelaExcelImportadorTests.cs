using ClosedXML.Excel;
using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class PrecoTabelaExcelImportadorTests
{
    [Fact]
    public void Ler_SemCabecalho_MantemColunasFixas()
    {
        var caminho = Planilha(planilha =>
        {
            planilha.Cell(1, 1).Value = "7891234567890";
            planilha.Cell(1, 2).Value = "PRODUTO";
            planilha.Cell(1, 3).Value = 10.5;
            planilha.Cell(1, 4).Value = 11.2;
        });

        var itens = PrecoTabelaExcelImportador.Ler(caminho);

        Assert.Single(itens);
        Assert.Equal(10.5m, itens[0].PrecoPrimario);
        Assert.Equal(11.2m, itens[0].PrecoSecundario);
        Assert.Equal("", itens[0].Loja);
    }

    [Fact]
    public void Ler_ComCabecalho_SeparaPrecoPorLoja()
    {
        var caminho = Planilha(planilha =>
        {
            planilha.Cell(1, 1).Value = "Loja";
            planilha.Cell(1, 2).Value = "EAN";
            planilha.Cell(1, 3).Value = "Produto";
            planilha.Cell(1, 4).Value = "Preco";
            planilha.Cell(1, 5).Value = "Preco ST";
            planilha.Cell(2, 1).Value = "Quata";
            planilha.Cell(2, 2).Value = "7891234567890";
            planilha.Cell(2, 3).Value = "A";
            planilha.Cell(2, 4).Value = 9;
            planilha.Cell(2, 5).Value = 10;
            planilha.Cell(3, 1).Value = 30;
            planilha.Cell(3, 2).Value = "7891234567890";
            planilha.Cell(3, 3).Value = "A";
            planilha.Cell(3, 4).Value = 8;
            planilha.Cell(3, 5).Value = 9.5;
        });

        var itens = PrecoTabelaExcelImportador.Ler(caminho);

        Assert.Equal(2, itens.Count);
        Assert.Equal("QUATA", itens[0].Loja);
        Assert.Equal(9m, itens[0].PrecoPrimario);
        Assert.Equal("TUPA", itens[1].Loja);
        Assert.Equal(9.5m, itens[1].PrecoSecundario);
    }

    private static string Planilha(Action<IXLWorksheet> preencher)
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"precos-{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var planilha = workbook.AddWorksheet("Precos");
        preencher(planilha);
        workbook.SaveAs(caminho);
        return caminho;
    }
}
