using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class StatusHerancaImportacaoTests
{
    [Theory]
    [InlineData(StatusConferenciaValues.Pendente, StatusConferenciaValues.Pendente)]
    [InlineData(StatusConferenciaValues.Amarelo, StatusConferenciaValues.Amarelo)]
    [InlineData(StatusConferenciaValues.Verde, StatusConferenciaValues.Laranja)]
    [InlineData(StatusConferenciaValues.Azul, StatusConferenciaValues.Laranja)]
    [InlineData(StatusConferenciaValues.Laranja, StatusConferenciaValues.Laranja)]
    public void Calcular_SemDevolucaoConcluida_AplicaRegras(string anterior, string esperado)
    {
        var (status, obs) = StatusHerancaImportacao.Calcular(anterior, "PBM", devolucaoConcluida: false);

        Assert.Equal(esperado, status);
        Assert.Equal("PBM", obs);
    }

    [Fact]
    public void Calcular_VermelhoSemDevolucaoConcluida_MantemVermelho()
    {
        var (status, _) = StatusHerancaImportacao.Calcular(
            StatusConferenciaValues.Vermelho,
            string.Empty,
            devolucaoConcluida: false);

        Assert.Equal(StatusConferenciaValues.Vermelho, status);
    }

    [Fact]
    public void Calcular_VermelhoComDevolucaoConcluida_ViraLaranja()
    {
        var (status, obs) = StatusHerancaImportacao.Calcular(
            StatusConferenciaValues.Vermelho,
            "ENCOMENDA",
            devolucaoConcluida: true);

        Assert.Equal(StatusConferenciaValues.Laranja, status);
        Assert.Equal("ENCOMENDA", obs);
    }

    [Theory]
    [InlineData("08/09/2026", "09/09/2026", true)]
    [InlineData("04/09/2026", "08/09/2026", true)]
    [InlineData("09/09/2026", "09/09/2026", false)]
    [InlineData("04/09/2026", "04/09/2026", false)]
    [InlineData("05/09/2026", "04/09/2026", false)]
    [InlineData("", "09/09/2026", false)]
    [InlineData("08/09/2026", "", false)]
    public void DeveMarcarLaranjaPorEmissao_UsaDiaConferenciaAnterior(
        string emissao,
        string diaAnterior,
        bool esperado)
    {
        Assert.Equal(esperado, StatusHerancaImportacao.DeveMarcarLaranjaPorEmissao(emissao, diaAnterior));
    }
}

public class NumNotaNormalizerTests
{
    [Theory]
    [InlineData("0123", "123")]
    [InlineData("123", "123")]
    [InlineData("000", "0")]
    [InlineData("  45 ", "45")]
    [InlineData("", "")]
    public void Normalizar_RemoveZerosAEsquerda(string entrada, string esperado)
    {
        Assert.Equal(esperado, NumNotaNormalizer.Normalizar(entrada));
    }
}
