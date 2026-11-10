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

    [Fact]
    public void Escolher_TabelaMaisRecente_PrevaleceSobreAMaisBarata()
    {
        var antiga = Item(1, 8.00m, loja: "");
        var nova = Item(2, 12.00m, loja: "");

        var escolhida = ComparacaoPrecoTabela.Escolher([antiga, nova], "QUATA");

        Assert.Equal(2, escolhida!.ImportacaoId);
        Assert.Equal(12.00m, escolhida.PrecoPrimario);
    }

    [Fact]
    public void Escolher_LojaDaPlanilha_SoQuandoExisteParaANota()
    {
        var geral = Item(5, 10.00m, loja: "");
        var quata = Item(5, 9.00m, loja: "QUATA");
        var tupa = Item(5, 7.00m, loja: "TUPA");

        Assert.Equal(9.00m, ComparacaoPrecoTabela.Escolher([geral, quata, tupa], "QUATA")!.PrecoPrimario);
        Assert.Equal(10.00m, ComparacaoPrecoTabela.Escolher([geral, quata, tupa], "BARAO")!.PrecoPrimario);
    }

    [Fact]
    public void Escolher_TabelaNovaDeOutraLoja_CaiNaAnteriorGeral()
    {
        var geralAntiga = Item(1, 10.00m, loja: "");
        var soQuata = Item(2, 4.00m, loja: "QUATA");

        var escolhida = ComparacaoPrecoTabela.Escolher([geralAntiga, soQuata], "BARAO");

        Assert.Equal(1, escolhida!.ImportacaoId);
    }

    [Fact]
    public void Descrever_MostraDiferencaEUsaPrecoComStNaLojaDoParana()
    {
        var tabela = Item(3, 10.00m, loja: "", secundario: 12.00m, nome: "HITS");

        var parana = ComparacaoPrecoTabela.Descrever(12.50m, tabela, notaComSt: true, avisoCusto: "", peloEanTributario: false);
        Assert.Equal(ComparacaoPrecoValores.AzulClaro, parana.Resultado);
        Assert.Contains("R$ 12,00", parana.Texto, StringComparison.Ordinal);
        Assert.Contains("+R$ 0,50 (+4,2%)", parana.Texto, StringComparison.Ordinal);
        Assert.Contains("com ST", parana.Texto, StringComparison.Ordinal);

        var outra = ComparacaoPrecoTabela.Descrever(10.00m, tabela, notaComSt: false, avisoCusto: "", peloEanTributario: false);
        Assert.Equal(ComparacaoPrecoValores.Verde, outra.Resultado);
        Assert.Contains("igual", outra.Texto, StringComparison.Ordinal);
        Assert.Contains("2o R$ 12,00", outra.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolverCusto_LinhaTotalUsaPrecoUnitarioDoVsm()
    {
        var custo = ComparacaoPrecoTabela.ResolverCusto(30.00m, 10.00m, 3);
        Assert.Equal(10.00m, custo.Valor);
        Assert.Contains("unit. VSM", custo.Aviso, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolverCusto_DivergenciaSemSerTotal_MantemOCalculadoEAvisa()
    {
        var custo = ComparacaoPrecoTabela.ResolverCusto(10.50m, 10.00m, 1);
        Assert.Equal(10.50m, custo.Valor);
        Assert.Contains("VSM R$ 10,00", custo.Aviso, StringComparison.Ordinal);
    }

    [Fact]
    public void Localizar_UsaEanTributarioQuandoOComercialNaoEstaNaTabela()
    {
        var mapa = new Dictionary<string, IReadOnlyList<PrecoTabelaItem>>(StringComparer.Ordinal)
        {
            ["7891234567895"] = [Item(1, 8.00m, loja: "")]
        };

        var achou = ComparacaoPrecoTabela.Localizar("7891234567890", "7891234567895", mapa, "QUATA");

        Assert.True(achou!.Value.PeloEanTributario);
        Assert.Equal(8.00m, achou.Value.Item.PrecoPrimario);
    }

    private static PrecoTabelaItem Item(
        long importacaoId,
        decimal preco,
        string loja,
        decimal? secundario = null,
        string nome = "TABELA") => new()
    {
        ImportacaoId = importacaoId,
        Ean = "7891234567890",
        PrecoPrimario = preco,
        PrecoSecundario = secundario,
        Loja = loja,
        NomeImportacao = nome
    };
}
