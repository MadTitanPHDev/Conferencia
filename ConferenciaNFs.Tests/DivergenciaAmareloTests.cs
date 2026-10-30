using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.ViewModels;
using Xunit;

namespace ConferenciaNFs.Tests;

public class DivergenciaAmareloTests
{
    [Fact]
    public void Cancelar_NaoPintaNemCriaPendencia()
    {
        Assert.False(EscolhaAmareloEfeito.DevePintar(EscolhaAmarelo.Cancelar));
        Assert.False(EscolhaAmareloEfeito.DeveCriarPendencia(EscolhaAmarelo.Cancelar));
    }

    [Fact]
    public void SomenteConferencia_PintaSemPendencia()
    {
        Assert.True(EscolhaAmareloEfeito.DevePintar(EscolhaAmarelo.SomenteConferencia));
        Assert.False(EscolhaAmareloEfeito.DeveCriarPendencia(EscolhaAmarelo.SomenteConferencia));
    }

    [Fact]
    public void EnviarComprador_PintaECriaPendencia()
    {
        Assert.True(EscolhaAmareloEfeito.DevePintar(EscolhaAmarelo.EnviarComprador));
        Assert.True(EscolhaAmareloEfeito.DeveCriarPendencia(EscolhaAmarelo.EnviarComprador));
    }

    [Fact]
    public void ValidarAtribuidos_SemCompradorAplicado_Recusa()
    {
        var linha = Linha();
        linha.Selecionado = true;
        Assert.NotNull(DivergenciaValidador.ValidarAtribuidos([linha]));
    }

    [Fact]
    public void ValidarAtribuidos_CompradorEMotivo_Aceita()
    {
        var linha = Linha();
        linha.DefinirComprador(4, "Hillary", "mais caro");
        linha.Quantidade = 1;
        Assert.Null(DivergenciaValidador.ValidarAtribuidos([linha]));
    }

    [Fact]
    public void Validar_SemPessoa_Recusa()
    {
        var linha = Linha();
        linha.Selecionado = true;
        Assert.NotNull(DivergenciaValidador.Validar([linha], pessoaId: 0));
    }

    [Fact]
    public void Validar_SemItem_Recusa()
    {
        Assert.NotNull(DivergenciaValidador.Validar([Linha()], pessoaId: 4));
    }

    [Fact]
    public void Validar_QuantidadeMaiorQueNota_Recusa()
    {
        var linha = Linha();
        linha.Selecionado = true;
        linha.Quantidade = 9;
        Assert.NotNull(DivergenciaValidador.Validar([linha], pessoaId: 4));
    }

    [Fact]
    public void Validar_PessoaEItem_AceitaSemObservacaoObrigatoria()
    {
        var linha = Linha();
        linha.Selecionado = true;
        linha.Quantidade = 1;
        Assert.Null(DivergenciaValidador.Validar([linha], pessoaId: 4));
    }

    [Fact]
    public void EanParaCopiar_UsaOCodigoValido()
    {
        var linha = new DivergenciaLinha(new ItemNotaSnapshot
        {
            BarrasEan = "7891234567890",
            BarrasEanTrib = "123"
        });

        Assert.Equal("7891234567890", linha.EanParaCopiar);
    }

    [Fact]
    public void TotalItem_MultiplicaQuantidadePeloPreco()
    {
        var linha = Linha();
        linha.Quantidade = 3;
        Assert.Equal(30m, linha.TotalItem);
    }

    private static DivergenciaLinha Linha() => new(new ItemNotaSnapshot
    {
        Sequencia = 1,
        CodProd = 10,
        NomeProd = "Creme",
        QuantItemCompra = 2,
        CustoNota = 10m
    });
}
