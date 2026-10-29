using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class PessoaNomeTests
{
    [Theory]
    [InlineData("  Maria   Silva  ", "Maria Silva")]
    [InlineData("Luara", "Luara")]
    [InlineData("  ", "")]
    public void NormalizarExibicao_JuntaEspacos(string entrada, string esperado)
    {
        Assert.Equal(esperado, PessoaNome.NormalizarExibicao(entrada));
    }

    [Fact]
    public void Chave_IgnoraMaiusculasEEspacos()
    {
        Assert.Equal(PessoaNome.Chave("Maria"), PessoaNome.Chave("  maria "));
        Assert.Equal(PessoaNome.Chave("João"), PessoaNome.Chave("joão"));
    }

    [Theory]
    [InlineData("Maria", true)]
    [InlineData("Ana", true)]
    [InlineData("A", false)]
    [InlineData("  ", false)]
    [InlineData(null, false)]
    public void EhValido_ExigePeloMenosDoisCaracteres(string? nome, bool esperado)
    {
        Assert.Equal(esperado, PessoaNome.EhValido(nome));
    }
}
