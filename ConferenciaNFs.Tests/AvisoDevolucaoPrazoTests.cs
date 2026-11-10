using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class AvisoDevolucaoPrazoTests
{
    private static readonly DateTime Hoje = new(2026, 11, 10);

    [Fact]
    public void DeveReceber_SomenteLais()
    {
        Assert.True(AvisoDevolucaoPrazo.DeveReceber("Lais"));
        Assert.True(AvisoDevolucaoPrazo.DeveReceber("laís"));
        Assert.False(AvisoDevolucaoPrazo.DeveReceber("Paulo"));
    }

    [Fact]
    public void Selecionar_PrazoAcabandoESemRastreio_EntramNoAviso()
    {
        var itens = AvisoDevolucaoPrazo.Selecionar(
        [
            Nota(1, rastrear: true, dias: 30, emissao: Hoje.AddDays(-5)),
            Nota(2, rastrear: true, dias: 10, emissao: Hoje.AddDays(-5)),
            Nota(3, rastrear: false, dias: null, emissao: Hoje)
        ],
        Hoje);

        Assert.Equal(2, itens.Count);
        Assert.Contains(itens, i => i.Id == 2 && i.Texto.Contains("vence em 5", StringComparison.Ordinal));
        Assert.Contains(itens, i => i.Id == 3 && i.Texto.Contains("sem prazo rastreado", StringComparison.Ordinal));
    }

    [Fact]
    public void Selecionar_RepeteSoDepoisDeDoisDias()
    {
        var recente = Nota(4, rastrear: false, dias: null, emissao: Hoje, ultimo: "09/11/2026");
        var vencido = Nota(5, rastrear: false, dias: null, emissao: Hoje, ultimo: "08/11/2026");

        var itens = AvisoDevolucaoPrazo.Selecionar([recente, vencido], Hoje);

        Assert.Single(itens);
        Assert.Equal(5, itens[0].Id);
    }

    [Fact]
    public void Selecionar_ConcluidaNaoEntra()
    {
        var itens = AvisoDevolucaoPrazo.Selecionar(
        [
            Nota(6, rastrear: false, dias: null, emissao: Hoje, status: StatusDevolucaoValues.Devolvida)
        ],
        Hoje);

        Assert.Empty(itens);
    }

    private static DevolucaoParaAviso Nota(
        long id,
        bool rastrear,
        int? dias,
        DateTime emissao,
        string? ultimo = null,
        string status = StatusDevolucaoValues.Pendente) =>
        new(id, "100", "QUATA", status, rastrear, dias, emissao, ultimo);
}
