using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.ViewModels;
using Xunit;

namespace ConferenciaNFs.Tests;

public class PreNotaValidadorTests
{
    [Fact]
    public void Validar_SemSelecao_Recusa()
    {
        var linhas = new[] { Linha() };
        Assert.NotNull(PreNotaValidador.Validar(linhas));
    }

    [Fact]
    public void Validar_ItemCompleto_Aceita()
    {
        var linha = Linha();
        linha.Selecionado = true;
        linha.QuantidadeDevolver = 1;
        linha.Motivo = "Avaria";
        linha.Classe = ClasseDevolucaoValues.Generico;
        Assert.Null(PreNotaValidador.Validar([linha]));
    }

    [Fact]
    public void Validar_QuantidadeMaiorQueNota_Recusa()
    {
        var linha = Linha();
        linha.Selecionado = true;
        linha.QuantidadeDevolver = 5;
        linha.Motivo = "Avaria";
        linha.Classe = ClasseDevolucaoValues.Generico;
        Assert.NotNull(PreNotaValidador.Validar([linha]));
    }

    [Fact]
    public void CustoNota_DescontaValorDoItem()
    {
        var item = new ItemCompraVsm
        {
            ValorItemCompra = 118.14m,
            Desconto = 23.63m
        };
        Assert.Equal(94.51m, item.CustoNota);
    }

    private static PreNotaLinha Linha() => new(new ItemNotaSnapshot
    {
        Sequencia = 1,
        CodProd = 10,
        NomeProd = "Teste",
        QuantItemCompra = 2,
        CustoNota = 10m
    });
}
