using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class PendenciaAcompanhaStatusTests
{
    [Theory]
    [InlineData(StatusConferenciaValues.Pendente)]
    [InlineData(StatusConferenciaValues.Verde)]
    [InlineData(StatusConferenciaValues.Vermelho)]
    [InlineData(StatusConferenciaValues.Laranja)]
    [InlineData(StatusConferenciaValues.Azul)]
    public void OutroStatus_EncerraPendenciaAberta(string status)
    {
        Assert.True(PendenciaAcompanhaStatus.DeveEncerrarAbertas(status));
    }

    [Fact]
    public void Amarelo_MantemAPendenciaDoComprador()
    {
        Assert.False(PendenciaAcompanhaStatus.DeveEncerrarAbertas(StatusConferenciaValues.Amarelo));
    }

    [Theory]
    [InlineData(StatusConferenciaValues.Vermelho, false)]
    [InlineData(StatusConferenciaValues.Amarelo, false)]
    [InlineData(StatusConferenciaValues.Laranja, true)]
    public void DevolvidaPeloComprador_AindaPedePrint(string status, bool temDevolucao)
    {
        Assert.True(PendenciaAcompanhaStatus.AindaAguardaPrint(status, temDevolucao));
    }

    [Theory]
    [InlineData(StatusConferenciaValues.Verde, false)]
    [InlineData(StatusConferenciaValues.Verde, true)]
    [InlineData(StatusConferenciaValues.Laranja, false)]
    [InlineData(StatusConferenciaValues.Azul, false)]
    [InlineData(StatusConferenciaValues.Pendente, false)]
    public void ConferenciaDesistiuDaDevolucao_NaoPedePrint(string status, bool temDevolucao)
    {
        Assert.False(PendenciaAcompanhaStatus.AindaAguardaPrint(status, temDevolucao));
    }
}
