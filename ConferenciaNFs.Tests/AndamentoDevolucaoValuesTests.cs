using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class AndamentoDevolucaoValuesTests
{
    [Theory]
    [InlineData(AndamentoDevolucaoValues.EnviarXml, "Enviar XML")]
    [InlineData(AndamentoDevolucaoValues.AguardandoLoja, "Aguardando loja")]
    [InlineData(AndamentoDevolucaoValues.AguardandoDist, "Aguardando dist.")]
    public void ObterRotulo_UsaTextoDaPlanilha(string valor, string rotulo)
    {
        Assert.Equal(rotulo, AndamentoDevolucaoValues.ObterRotulo(valor));
    }

    [Fact]
    public void DeFiltro_Todos_NaoRestringe()
    {
        Assert.Null(AndamentoDevolucaoValues.DeFiltro(AndamentoDevolucaoValues.FiltroTodos));
    }
}
