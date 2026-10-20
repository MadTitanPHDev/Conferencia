using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class RelocacaoNotaVsmTests
{
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
