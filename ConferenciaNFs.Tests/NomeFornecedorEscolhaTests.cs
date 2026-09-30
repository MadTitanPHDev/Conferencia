using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class NomeFornecedorEscolhaTests
{
    [Theory]
    [InlineData("", "123", true)]
    [InlineData("12345678000199", "12345678000199", true)]
    [InlineData("12.345.678/0001-99", "12345678000199", true)]
    [InlineData("Panpharma", "12345678000199", false)]
    public void EhRotuloCnpj_DetectaCnpjComoNome(string nome, string cnpj, bool esperado)
    {
        Assert.Equal(esperado, NomeFornecedorEscolha.EhRotuloCnpj(nome, cnpj));
    }

    [Fact]
    public void Escolher_PrefereNomeRealAoCnpj()
    {
        var nome = NomeFornecedorEscolha.Escolher(
            ["12345678000199", "Panpharma", "PANPHARMA", "12.345.678/0001-99"],
            "12345678000199");

        Assert.Equal("Panpharma", nome);
    }

    [Fact]
    public void Escolher_SemNomeReal_Vazio()
    {
        var nome = NomeFornecedorEscolha.Escolher(
            ["12345678000199", ""],
            "12345678000199");

        Assert.Equal(string.Empty, nome);
    }
}
