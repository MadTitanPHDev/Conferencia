using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class DevolucaoObservacaoHistoricoTests
{
    [Fact]
    public void Juntar_MaisRecentePrimeiro()
    {
        var linhas = new List<DevolucaoObservacao>
        {
            new() { Id = 1, CriadaEm = "28/09/2026 10:00", Texto = "primeiro" },
            new() { Id = 2, CriadaEm = "29/09/2026 16:00", Texto = "segundo" }
        };

        var texto = DevolucaoObservacaoHistorico.Juntar(linhas);
        Assert.StartsWith("29/09/2026 16:00 · segundo", texto);
        Assert.Contains("28/09/2026 10:00 · primeiro", texto);
    }

    [Fact]
    public void Juntar_ListaVazia_Vazio()
    {
        Assert.Equal(string.Empty, DevolucaoObservacaoHistorico.Juntar([]));
    }
}
