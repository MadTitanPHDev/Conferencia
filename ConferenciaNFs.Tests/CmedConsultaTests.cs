using ClosedXML.Excel;
using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class CmedConsultaTests
{
    [Theory]
    [InlineData("37,48", 37.48)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("8224,28*", 8224.28)]
    [InlineData("R$ 27,44", 27.44)]
    public void Numero_LePrecoComVirgulaEAsterisco(string texto, decimal esperado)
    {
        Assert.Equal(esperado, CmedNumero.Converter(texto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("—")]
    public void Numero_VazioNaoViraPreco(string? texto)
    {
        Assert.Null(CmedNumero.Converter(texto));
    }

    [Fact]
    public void Cabecalho_RemoveAcentoEEspacoDuplo()
    {
        Assert.Equal("CODIGO GGREM", CmedTexto.NormalizarCabecalho("Código GGREM"));
        Assert.Equal("12% ALC", CmedTexto.RotuloPreco("PF 12 % ALC", "PF "));
        Assert.Equal("0742832304825", CmedTexto.NormalizarEan("0742832304825"));
    }

    [Theory]
    [InlineData("Tarja Preta")]
    [InlineData("Tarja Vermelha sob restrição")]
    [InlineData("Tarja Vermelha sob restrição (*)")]
    public void Tarja_PretaEVermelhaSobRestricaoSaoControladas(string tarja)
    {
        Assert.True(CmedTarja.EhControlado(tarja));
        Assert.Equal(CmedTarja.Controlado, CmedTarja.Rotulo(tarja));
    }

    [Fact]
    public void Tarja_VermelhaComumNaoEControlada()
    {
        Assert.False(CmedTarja.EhControlado("Tarja Vermelha"));
        Assert.Equal(CmedTarja.Receita, CmedTarja.Rotulo("Tarja Vermelha"));
        Assert.Equal(CmedTarja.VendaLivre, CmedTarja.Rotulo("Tarja Sem Tarja"));
        Assert.Equal(CmedTarja.NaoInformada, CmedTarja.Rotulo("- (*)"));
    }

    [Fact]
    public void Importador_UnePmcComPmvgEMantemAsterisco()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "cmed-teste-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        var pmc = Path.Combine(pasta, "lista_pmc_20261109_1.xlsx");
        var pmvg = Path.Combine(pasta, "lista_pmvg_20261109_1.xlsx");

        try
        {
            Gravar(pmc, "PMC", "8224,28*", "100,00", "Tarja Preta");
            Gravar(pmvg, "PMVG", "8224,28*", "80,00", "Tarja Preta");

            var resultado = CmedCatalogoImportador.Ler(pmc, pmvg);
            var produto = Assert.Single(resultado.Produtos);
            var precos = ConferenciaNFs.Models.CmedPrecos.Desserializar(produto.PrecosJson);

            Assert.Equal("ORENCIA", produto.Produto);
            Assert.Equal("BRISTOL", produto.Laboratorio);
            Assert.Equal(8224.28m, precos.Obter("PF", "18%"));
            Assert.Equal(100m, precos.Obter("PMC", "18%"));
            Assert.Equal(80m, precos.Obter("PMVG", "18%"));
            Assert.Equal("09/11/2026", resultado.Publicacao);
        }
        finally
        {
            Directory.Delete(pasta, true);
        }
    }

    [Fact]
    public void Download_LeOsLinksDaPaginaEUsaOFallback()
    {
        var html = """
            <a href="/anvisa/pt-br/assuntos/medicamentos/cmed/precos/arquivos/lista_pmc_20261201_1.xlsx/@@download/file">PMC</a>
            <a href="https://www.gov.br/anvisa/pt-br/assuntos/medicamentos/cmed/precos/arquivos/lista_pmvg_20261201_1.xlsx/@@download/file">PMVG</a>
            """;

        var fontes = CmedDownload.InterpretarPagina(html);

        Assert.Equal(
            "https://www.gov.br/anvisa/pt-br/assuntos/medicamentos/cmed/precos/arquivos/lista_pmc_20261201_1.xlsx/@@download/file",
            fontes.UrlPmc);
        Assert.EndsWith("lista_pmvg_20261201_1.xlsx/@@download/file", fontes.UrlPmvg);
        Assert.Equal("01/12/2026", fontes.Publicacao);
        Assert.Equal("lista_pmc_20261201_1.xlsx", CmedDownload.NomeArquivo(fontes.UrlPmc));

        var reserva = CmedDownload.InterpretarPagina("");
        Assert.Contains("lista_pmc_20261109_111901662.xlsx", reserva.UrlPmc);
        Assert.Contains("lista_pmvg_20261109_111901662.xlsx", reserva.UrlPmvg);
    }

    private static void Gravar(string caminho, string tipoSegundo, string pf, string segundo, string tarja)
    {
        using var livro = new XLWorkbook();
        var capa = livro.AddWorksheet("Cabecalho");
        capa.Cell(1, 1).Value = "Publicada em 09/11/2026 17h30min.";
        var aba = livro.AddWorksheet(tipoSegundo == "PMC" ? "Lista PMC" : "Lista PMVG");
        aba.Cell(1, 1).Value = "SUBSTÂNCIA";
        aba.Cell(1, 2).Value = "CNPJ";
        aba.Cell(1, 3).Value = "LABORATÓRIO";
        aba.Cell(1, 4).Value = "CÓDIGO GGREM";
        aba.Cell(1, 5).Value = "REGISTRO";
        aba.Cell(1, 6).Value = "EAN 1";
        aba.Cell(1, 7).Value = "PRODUTO";
        aba.Cell(1, 8).Value = "APRESENTAÇÃO";
        aba.Cell(1, 9).Value = "TARJA";
        aba.Cell(1, 10).Value = "PF 18 %";
        aba.Cell(1, 11).Value = tipoSegundo + " 18 %";
        aba.Cell(2, 1).Value = "ABATACEPTE";
        aba.Cell(2, 3).Value = "BRISTOL";
        aba.Cell(2, 5).Value = "1.0";
        aba.Cell(2, 6).Value = "7896016808197";
        aba.Cell(2, 7).Value = "ORENCIA";
        aba.Cell(2, 8).Value = "CX";
        aba.Cell(2, 9).Value = tarja;
        aba.Cell(2, 10).Value = pf;
        aba.Cell(2, 11).Value = segundo;
        livro.SaveAs(caminho);
    }
}
