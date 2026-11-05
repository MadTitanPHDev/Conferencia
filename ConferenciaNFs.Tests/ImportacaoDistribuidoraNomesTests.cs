using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class ImportacaoDistribuidoraNomesTests
{
    [Fact]
    public void Ler_PlanilhaHtml_LeCnpjENome()
    {
        const string html = """
            <td>CNPJ</td><td>Fornecedor</td>
            <td>00.136.766/0001-06</td><td>SOUSA E LINGIARDI LTDA</td>
            """;

        var itens = ImportacaoDistribuidoraNomes.Ler(html);

        Assert.Single(itens);
        Assert.Equal("00136766000106", itens[0].Cnpj);
        Assert.Equal("SOUSA E LINGIARDI LTDA", itens[0].Nome);
    }

    [Fact]
    public void NomeVazioOuCnpj_RecebeONomeDaPlanilha()
    {
        Assert.Equal("I9 DISTRIBUICAO", ImportacaoDistribuidoraNomes.NomeParaGravar("10962349000168", "", "I9 DISTRIBUICAO"));
        Assert.Equal("ALTHAIA", ImportacaoDistribuidoraNomes.NomeParaGravar("48344725000808", "48344725000808", "ALTHAIA"));
    }

    [Fact]
    public void NomeJaPreenchido_NaoETrocado()
    {
        Assert.Null(ImportacaoDistribuidoraNomes.NomeParaGravar("10962349000168", "I9 DISTRIBUICAO", "OUTRO NOME"));
    }

    [Fact]
    public void Cimed_ViraPredileta()
    {
        Assert.Equal(
            ImportacaoDistribuidoraNomes.NomePredileta,
            ImportacaoDistribuidoraNomes.NomeParaGravar(
                ImportacaoDistribuidoraNomes.CnpjPredileta,
                "CIMED & CO. S.A.",
                "CIMED & CO. S.A."));
    }

    [Fact]
    public void Predileta_PermaneceSemRegravar()
    {
        Assert.Null(ImportacaoDistribuidoraNomes.NomeParaGravar(
            ImportacaoDistribuidoraNomes.CnpjPredileta,
            "PREDILETA SAO PAULO DISTRIBUID",
            "CIMED & CO. S.A."));
    }
}
