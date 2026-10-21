using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class RelocacaoNotaVsmTests
{
    [Theory]
    [InlineData("20/10/2026", "20/10/2026", true)]
    [InlineData("20/10/26", "20/10/2026", true)]
    [InlineData("19/10/2026", "20/10/2026", false)]
    public void MesmoDiaConferencia_SoPermiteRealocacaoDentroDoDia(
        string origem,
        string destino,
        bool esperado)
    {
        Assert.Equal(esperado, RelocacaoNotaVsm.MesmoDiaConferencia(origem, destino));
    }

    [Fact]
    public void StatusAposMesclar_DestinoPendente_LevaStatusDoDiaAnterior()
    {
        var (status, obs) = RelocacaoNotaVsm.StatusAposMesclar(
            StatusConferenciaValues.Pendente,
            "",
            StatusConferenciaValues.Verde,
            "PBM");

        Assert.Equal(StatusConferenciaValues.Verde, status);
        Assert.Equal("PBM", obs);
    }

    [Fact]
    public void StatusAposMesclar_DestinoLaranjaDeHeranca_LevaVerdeAnterior()
    {
        var (status, _) = RelocacaoNotaVsm.StatusAposMesclar(
            StatusConferenciaValues.Laranja,
            "",
            StatusConferenciaValues.Verde,
            "PBM");

        Assert.Equal(StatusConferenciaValues.Verde, status);
    }

    [Fact]
    public void StatusAposMesclar_DestinoAmarelo_PreservaConferenciaDoDiaNovo()
    {
        var (status, obs) = RelocacaoNotaVsm.StatusAposMesclar(
            StatusConferenciaValues.Amarelo,
            "FALAR",
            StatusConferenciaValues.Verde,
            "PBM");

        Assert.Equal(StatusConferenciaValues.Amarelo, status);
        Assert.Equal("FALAR", obs);
    }

    [Fact]
    public void StatusAposMesclar_OrigemPendente_MantemDestino()
    {
        var (status, _) = RelocacaoNotaVsm.StatusAposMesclar(
            StatusConferenciaValues.Laranja,
            "",
            StatusConferenciaValues.Pendente,
            "");

        Assert.Equal(StatusConferenciaValues.Laranja, status);
    }
}

public class StatusAutomaticoProtegidoTests
{
    [Theory]
    [InlineData(StatusConferenciaValues.Verde, StatusConferenciaValues.Laranja)]
    [InlineData(StatusConferenciaValues.Azul, StatusConferenciaValues.Laranja)]
    [InlineData(StatusConferenciaValues.Laranja, StatusConferenciaValues.Laranja)]
    [InlineData(StatusConferenciaValues.Amarelo, StatusConferenciaValues.Amarelo)]
    [InlineData(StatusConferenciaValues.Vermelho, StatusConferenciaValues.Vermelho)]
    [InlineData(StatusConferenciaValues.Pendente, StatusConferenciaValues.Pendente)]
    public void ReentradaEmNovoDia_SegueRegraDeHeranca(string anterior, string esperado)
    {
        var (status, _) = StatusHerancaImportacao.Calcular(anterior, "", devolucaoJaRegistrada: false);
        Assert.Equal(esperado, status);
    }

    [Fact]
    public void LaranjaManual_NaoPodeSerLimpoAutomaticamente()
    {
        Assert.False(OrigemStatusValues.PodeSerAlteradoAutomaticamente(OrigemStatusValues.Manual));
        Assert.True(OrigemStatusValues.PodeSerAlteradoAutomaticamente(OrigemStatusValues.Herdado));
    }

    [Fact]
    public void SabadoSegundaTerca_NaoSaoOMesmoDia()
    {
        Assert.False(RelocacaoNotaVsm.MesmoDiaConferencia("17/10/2026", "19/10/2026"));
        Assert.False(RelocacaoNotaVsm.MesmoDiaConferencia("19/10/2026", "20/10/2026"));
        Assert.True(RelocacaoNotaVsm.MesmoDiaConferencia("20/10/2026", "20/10/2026"));
    }
}
