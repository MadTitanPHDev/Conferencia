using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class StatusCompraVsmTests
{
    [Fact]
    public void Aberta_MostraEmAberto()
    {
        Assert.Equal("Em aberto", StatusCompraVsm.Rotulo("A", ""));
        Assert.Equal("Em aberto", StatusCompraVsm.Rotulo(" a ", "S"));
    }

    [Fact]
    public void Finalizada_MostraFinalizada()
    {
        Assert.Equal("Finalizada", StatusCompraVsm.Rotulo("F", ""));
        Assert.Equal("Finalizada", StatusCompraVsm.Rotulo("F", "N"));
    }

    [Fact]
    public void FinalizadaReaberta_MostraReaberta()
    {
        Assert.Equal("Reaberta", StatusCompraVsm.Rotulo("F", "S"));
    }

    [Fact]
    public void Devolvido_MostraDevolvido()
    {
        Assert.Equal("Devolvido", StatusCompraVsm.Rotulo("D", ""));
        Assert.Equal("Devolvido", StatusCompraVsm.Rotulo(" d ", "S"));
    }

    [Fact]
    public void SemStatus_MostraTraco()
    {
        Assert.Equal("—", StatusCompraVsm.Rotulo("  ", null));
    }
}
