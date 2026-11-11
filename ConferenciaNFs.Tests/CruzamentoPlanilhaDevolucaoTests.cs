using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class CruzamentoPlanilhaDevolucaoTests
{
    [Fact]
    public void CodLojaDaAba_LeONumeroEntreParenteses()
    {
        Assert.Equal(27, CruzamentoPlanilhaDevolucao.CodLojaDaAba("FLÓRIDA PAULISTA (27)"));
        Assert.Equal(1, CruzamentoPlanilhaDevolucao.CodLojaDaAba("BARÃO (1)"));
        Assert.Null(CruzamentoPlanilhaDevolucao.CodLojaDaAba("Sem codigo"));
    }

    [Fact]
    public void ChaveNumero_IgnoraTituloELoja()
    {
        Assert.Equal(string.Empty, CruzamentoPlanilhaDevolucao.ChaveNumero("CONTROLE DE DEVOLUÇÕES"));
        Assert.Equal(string.Empty, CruzamentoPlanilhaDevolucao.ChaveNumero("LOJA 6 - MARTINÓPOLIS"));
        Assert.Equal("7189135", CruzamentoPlanilhaDevolucao.ChaveNumero("7189135"));
        Assert.Equal("7189135", CruzamentoPlanilhaDevolucao.ChaveNumero("007189135"));
    }

    [Fact]
    public void Faltantes_SoApontaNumeroAusenteNaAbaDaLoja()
    {
        var planilha = CruzamentoPlanilhaDevolucao.IndexarAbas(
        [
            ("FLÓRIDA PAULISTA (27)", ["CONTROLE DE DEVOLUÇÕES", "7189135"]),
            ("BARÃO (1)", ["CONTROLE DE DEVOLUÇÕES"])
        ]);

        var faltam = CruzamentoPlanilhaDevolucao.Faltantes(
        [
            (1L, "7189135", "FLORIDA PA", "05/11/2026", "10/11/2026 14:00"),
            (2L, "80011", "BARAO", "02/11/2026", "10/11/2026 14:00"),
            (3L, "900", "BARAO", "01/10/2026", "15/10/2026 09:00")
        ],
        planilha);

        Assert.Single(faltam);
        Assert.Equal(2L, faltam[0].Id);
        Assert.Equal("80011", faltam[0].NumNota);
    }
}
