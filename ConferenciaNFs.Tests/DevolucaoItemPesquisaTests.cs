using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class DevolucaoItemPesquisaTests
{
    [Fact]
    public void ItemNaPreNotaConcluida_ApareceDevolvido()
    {
        var rotulo = DevolucaoItemPesquisa.Resolver(10, "7891234567890",
        [
            Item(10, "7891234567890", StatusDevolucaoValues.Devolvida)
        ]);

        Assert.Equal(DevolucaoItemPesquisa.Devolvido, rotulo);
    }

    [Fact]
    public void ItemNaPreNotaAberta_ApareceNaFila()
    {
        var rotulo = DevolucaoItemPesquisa.Resolver(10, "7891234567890",
        [
            Item(10, "789", StatusDevolucaoValues.Pendente)
        ]);

        Assert.Equal(DevolucaoItemPesquisa.NaFila, rotulo);
    }

    [Fact]
    public void OutroItemDaNota_ApareceNao()
    {
        var rotulo = DevolucaoItemPesquisa.Resolver(99, "7890000000000",
        [
            Item(10, "7891234567890", StatusDevolucaoValues.Devolvida)
        ]);

        Assert.Equal(DevolucaoItemPesquisa.Nao, rotulo);
    }

    [Fact]
    public void NotaSemItens_ApareceNao()
    {
        var rotulo = DevolucaoItemPesquisa.Resolver(10, "7891234567890", []);

        Assert.Equal(DevolucaoItemPesquisa.Nao, rotulo);
    }

    [Fact]
    public void MesmoProdutoDevolvidoENaFila_PrefereDevolvido()
    {
        var rotulo = DevolucaoItemPesquisa.Resolver(10, "7891234567890",
        [
            Item(10, "7891234567890", StatusDevolucaoValues.Pendente),
            Item(10, "7891234567890", StatusDevolucaoValues.Devolvida)
        ]);

        Assert.Equal(DevolucaoItemPesquisa.Devolvido, rotulo);
    }

    [Fact]
    public void FiltroVazio_MostraTodos()
    {
        Assert.True(DevolucaoItemPesquisa.ConfereFiltro(DevolucaoItemPesquisa.Nao, "  "));
    }

    [Fact]
    public void FiltroDevolvido_EscondeQuemNaoFoi()
    {
        Assert.False(DevolucaoItemPesquisa.ConfereFiltro(
            DevolucaoItemPesquisa.Nao,
            DevolucaoItemPesquisa.Devolvido));
    }

    private static ItemDevolucaoConsulta Item(int codProd, string ean, string status) => new()
    {
        CodCompra = 1,
        CodProd = codProd,
        BarrasEan = ean,
        StatusDevolucao = status
    };
}
