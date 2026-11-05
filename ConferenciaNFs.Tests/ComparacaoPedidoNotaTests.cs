using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class ComparacaoPedidoNotaTests
{
    [Fact]
    public void DescontoDaNota_Lognis_FechaEm44PorCento()
    {
        Assert.Equal(44m, ComparacaoPedidoNota.PercentualDesconto(187.19m, 82.36m));
    }

    [Fact]
    public void Exibir_ItemAtendido_MostraDescontoDaNota()
    {
        var tela = ComparacaoPedidoNota.Exibir(new ComparacaoProdutoPedido
        {
            CodProd = 159102,
            QtdNota = 1,
            ValorItem = 187.19m,
            DescontoNota = 82.36m,
            PrecoUnitarioNota = 104.83m,
            QtdAtendida = 1,
            QtdPedida = 1,
            PrecoUnitarioPedido = 104.83m,
            DescontoPedido = 0,
            TemPedidoNumero = 1
        });

        Assert.Equal("Atendido", tela.Situacao);
        Assert.Equal("1", tela.QtdNota);
        Assert.Equal("1", tela.QtdPedido);
        Assert.Equal("0", tela.DifQtd);
        Assert.Equal("44", tela.DescontoNota);
        Assert.Equal("0", tela.DescontoPedido);
        Assert.Equal("44", tela.DifDesconto);
        Assert.True(tela.DifDescontoAlerta);
        Assert.False(tela.DifQtdAlerta);
    }

    [Fact]
    public void Exibir_QuantidadeMaiorNaNota_FicaParcial()
    {
        var tela = ComparacaoPedidoNota.Exibir(new ComparacaoProdutoPedido
        {
            QtdNota = 2,
            ValorItem = 100,
            DescontoNota = 10,
            PrecoUnitarioNota = 45,
            QtdAtendida = 1,
            QtdPedida = 1,
            PrecoUnitarioPedido = 45,
            TemPedidoNumero = 1
        });

        Assert.Equal("Atendido parcialmente", tela.Situacao);
        Assert.Equal("10", tela.DescontoNota);
        Assert.True(tela.DifQtdAlerta);
    }

    [Fact]
    public void Exibir_SemPedido_AindaMostraDescontoDaNota()
    {
        var tela = ComparacaoPedidoNota.Exibir(new ComparacaoProdutoPedido
        {
            QtdNota = 1,
            ValorItem = 50,
            DescontoNota = 5,
            PrecoUnitarioNota = 45
        });

        Assert.Equal("Não solicitado", tela.Situacao);
        Assert.Equal("10", tela.DescontoNota);
        Assert.Equal("—", tela.DescontoPedido);
    }
}
