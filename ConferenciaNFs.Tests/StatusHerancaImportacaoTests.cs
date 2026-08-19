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
}
