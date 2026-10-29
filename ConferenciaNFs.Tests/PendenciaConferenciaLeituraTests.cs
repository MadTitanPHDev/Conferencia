using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class PendenciaConferenciaLeituraTests
{
    [Theory]
    [InlineData("Aberta", true)]
    [InlineData("Cancelada", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ApareceNaLista_SoPendenciaAberta(string? status, bool esperado)
    {
        Assert.Equal(esperado, PendenciaConferenciaLeitura.ApareceNaLista(status));
    }

    [Theory]
    [InlineData("Ficou", true)]
    [InlineData("Devolvida", true)]
    [InlineData("Aberta", false)]
    [InlineData("Cancelada", false)]
    public void ApareceComoConcluida_SoRespostaGravada(string status, bool esperado)
    {
        Assert.Equal(esperado, PendenciaConferenciaLeitura.ApareceComoConcluida(status));
    }

    [Fact]
    public void JuntarObservacao_MantemAsDuas()
    {
        Assert.Equal("PBM | Preco divergente", PendenciaConferenciaLeitura.JuntarObservacao("PBM", "Preco divergente"));
        Assert.Equal("PBM", PendenciaConferenciaLeitura.JuntarObservacao("PBM", "PBM"));
        Assert.Equal("So a pendencia", PendenciaConferenciaLeitura.JuntarObservacao("  ", "So a pendencia"));
    }

    [Fact]
    public void Resposta_FicarVoltaVerdeSemFila()
    {
        Assert.Equal(StatusConferenciaValues.Verde, RespostaPendenciaEfeito.StatusNota(ficarComDivergencia: true));
        Assert.Equal(StatusPendenciaCompraValues.Ficou, RespostaPendenciaEfeito.StatusPendencia(ficarComDivergencia: true));
        Assert.False(RespostaPendenciaEfeito.EntraNaFilaDevolucao(ficarComDivergencia: true));
    }

    [Fact]
    public void Resposta_DevolverEntraNaFila()
    {
        Assert.Equal(StatusConferenciaValues.Vermelho, RespostaPendenciaEfeito.StatusNota(ficarComDivergencia: false));
        Assert.Equal(StatusPendenciaCompraValues.Devolvida, RespostaPendenciaEfeito.StatusPendencia(ficarComDivergencia: false));
        Assert.True(RespostaPendenciaEfeito.EntraNaFilaDevolucao(ficarComDivergencia: false));
    }

    [Fact]
    public void TextoItens_JuntaNomesNaOrdem()
    {
        var texto = PendenciaConferenciaLeitura.TextoItens(
        [
            new PendenciaCompraItem { Sequencia = 2, NomeProd = "Batom", CodProd = 2 },
            new PendenciaCompraItem { Sequencia = 1, NomeProd = "Creme", CodProd = 1 }
        ]);

        Assert.Equal("Creme · Batom", texto);
    }

    [Fact]
    public void CopiarDestino_VisivelSemPendenciaRecebeADaCopia()
    {
        var visivel = Nota(2, "26/10/2026");
        var copia = Nota(1, "24/10/2026");
        copia.DestinoPendencia = "Maria";
        copia.EnviadoPorPendencia = "Paulo";
        copia.ItensPendencia = "Creme";

        PendenciaConferenciaLeitura.CopiarDestinoParaVisivel(visivel, [copia, visivel]);

        Assert.Equal("Maria", visivel.DestinoPendencia);
        Assert.Equal("Paulo", visivel.EnviadoPorPendencia);
        Assert.Equal("Creme", visivel.ItensPendencia);
    }

    [Fact]
    public void CopiarDestino_AmareloSoDaConferenciaNaoGanhaDestino()
    {
        var visivel = Nota(2, "26/10/2026");
        var outra = Nota(9, "26/10/2026");
        outra.CodCompra = 99;
        outra.NumNota = "999";
        outra.DestinoPendencia = "Luara";

        PendenciaConferenciaLeitura.CopiarDestinoParaVisivel(visivel, [outra, visivel]);

        Assert.Equal(string.Empty, visivel.DestinoPendencia);
    }

    private static NotaFiscal Nota(long id, string dia) => new()
    {
        Id = id,
        ApelidoLoja = "2",
        NumNota = "100",
        CnpjForn = "1",
        DiaConferencia = dia,
        CodCompra = 10,
        StatusConferencia = StatusConferenciaValues.Amarelo
    };
}
