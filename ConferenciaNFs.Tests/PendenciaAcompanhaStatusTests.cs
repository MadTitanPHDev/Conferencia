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
}
