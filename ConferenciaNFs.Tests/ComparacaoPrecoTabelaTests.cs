using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class ComparacaoPrecoTabelaTests
{
    [Fact]
    public void Classificar_NfMaisBarataOuIgual_Verde()
    {
        Assert.Equal(ComparacaoPrecoValores.Verde, ComparacaoPrecoTabela.Classificar(10.00m, 10.00m));
        Assert.Equal(ComparacaoPrecoValores.Verde, ComparacaoPrecoTabela.Classificar(9.50m, 10.00m));
        Assert.Equal(ComparacaoPrecoValores.Verde, ComparacaoPrecoTabela.Classificar(10.02m, 10.00m));
    }

    [Fact]
    public void Classificar_AteDezPorCentoMaisCara_AzulClaro()
    {
        Assert.Equal(ComparacaoPrecoValores.AzulClaro, ComparacaoPrecoTabela.Classificar(10.50m, 10.00m));
        Assert.Equal(ComparacaoPrecoValores.AzulClaro, ComparacaoPrecoTabela.Classificar(11.00m, 10.00m));
    }

    [Fact]
    public void Classificar_MaisCaraQueDezPorCento_Vermelho()
    {
        Assert.Equal(ComparacaoPrecoValores.Vermelho, ComparacaoPrecoTabela.Classificar(11.20m, 10.00m));
    }

    [Fact]
    public void Classificar_SemTabela_Cinza()
    {
        Assert.Equal(ComparacaoPrecoValores.Cinza, ComparacaoPrecoTabela.Classificar(10.00m, null));
    }

    [Fact]
    public void EanNormalizer_RemovePontoZeroENaoDigitos()
    {
        Assert.Equal("7891234567890", EanNormalizer.Normalizar("7891234567890.0"));
        Assert.Equal("7891234567890", EanNormalizer.Normalizar(" 789 1234567890 "));
        Assert.False(EanNormalizer.EhValido("123"));
    }

    [Fact]
    public void EanNormalizer_IgnoraZerosAEsquerdaNaComparacao()
    {
        Assert.Equal("70341368572", EanNormalizer.ChaveComparacao("070341368572"));
        Assert.Equal("70341368572", EanNormalizer.ChaveComparacao("70341368572"));
        Assert.Equal("70341369111", EanNormalizer.ChaveComparacao("0070341369111"));
        Assert.Equal("70341369111", EanNormalizer.ChaveComparacao("70341369111"));
        Assert.True(EanNormalizer.SaoEquivalentes("070341368572", "70341368572"));
        Assert.True(EanNormalizer.SaoEquivalentes("0070341369111", "70341369111"));
        Assert.False(EanNormalizer.SaoEquivalentes("070341368572", "0070341369111"));
    }
}
