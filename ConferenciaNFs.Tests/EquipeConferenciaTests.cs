using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class EquipeConferenciaTests
{
    [Theory]
    [InlineData("Paulo", true)]
    [InlineData("  guilherme ", true)]
    [InlineData("João Gabriel", true)]
    [InlineData("Joao Gabriel", true)]
    [InlineData("Rafael", false)]
    [InlineData("Hillary", false)]
    [InlineData("", false)]
    public void EhMembro_SoEquipeDaConferencia(string nome, bool esperado)
    {
        Assert.Equal(esperado, EquipeConferencia.EhMembro(nome));
    }

    [Fact]
    public void RotuloResolucao_FicarViraAbsorvidaEDevolverViraDevolvida()
    {
        Assert.Equal("absorvida", EquipeConferencia.RotuloResolucao(StatusPendenciaCompraValues.Ficou));
        Assert.Equal("devolvida", EquipeConferencia.RotuloResolucao(StatusPendenciaCompraValues.Devolvida));
        Assert.Equal(string.Empty, EquipeConferencia.RotuloResolucao(StatusPendenciaCompraValues.Aberta));
    }
}
