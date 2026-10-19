using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.ViewModels;
using Xunit;

namespace ConferenciaNFs.Tests;

public class CustoItemNotaTests
{
    [Fact]
    public void Calcular_LojaSp_IgnoraSt()
    {
        var custo = CustoItemNota.Calcular(
            codLoja: 1,
            valorItemCompra: 13.19m,
            desconto: 9.58m,
            custoValorSt: 2.38m,
            custoValorFcpSt: 0.10m);
        Assert.Equal(3.61m, custo);
    }

    [Fact]
    public void Calcular_LojaParana_SomaStEFcp()
    {
        var custo = CustoItemNota.Calcular(
            CustoItemNota.CodLojaParana,
            valorItemCompra: 13.19m,
            desconto: 9.58m,
            custoValorSt: 2.38m,
            custoValorFcpSt: 0.10m);
        Assert.Equal(6.09m, custo);
    }

    [Fact]
    public void ItemCompraVsm_LojaParana_CustoNotaIncluiSt()
    {
        var item = new ItemCompraVsm
        {
            CodLoja = CustoItemNota.CodLojaParana,
            ValorItemCompra = 13.19m,
            Desconto = 9.58m,
            CustoValorSt = 2.38m
        };
        Assert.Equal(5.99m, item.CustoNota);
        Assert.Equal(ComparacaoPrecoValores.Vermelho,
            ComparacaoPrecoTabela.Classificar(item.CustoNota, 3.61m));
    }

    [Fact]
    public void ItemNotaSnapshotMap_PreservaCustoComSt()
    {
        var vsm = new ItemCompraVsm
        {
            CodLoja = CustoItemNota.CodLojaParana,
            ValorItemCompra = 13.19m,
            Desconto = 9.58m,
            CustoValorSt = 2.38m
        };
        var foto = ItemNotaSnapshotMap.DeVsm(1, vsm);
        Assert.Equal(5.99m, foto.CustoNota);

        var deVolta = ItemNotaSnapshotMap.ParaVsm(foto);
        Assert.Equal(5.99m, deVolta.CustoNota);
        Assert.Equal(5.99m, new ItemNotaPrecoLinha(deVolta).CustoUnit);
    }
}
