using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class DevolucoesPainelTests
{
    [Fact]
    public void Calcular_SomaPendentesEDevolvidas()
    {
        var notas = new List<DevolucaoItem>
        {
            new()
            {
                Id = 1,
                ApelidoLoja = "BARAO",
                ValorNota = 100,
                StatusDevolucao = StatusDevolucaoValues.Pendente
            },
            new()
            {
                Id = 2,
                ApelidoLoja = "BARAO",
                ValorNota = 40,
                StatusDevolucao = StatusDevolucaoValues.Devolvida
            }
        };

        var itens = new Dictionary<long, IReadOnlyList<DevolucaoItemPreNota>>
        {
            [1] = [new DevolucaoItemPreNota { Classe = "Generico", Motivo = "Avaria" }]
        };

        var painel = DevolucoesPainel.Calcular(notas, itens);
        Assert.Equal(1, painel.Pendentes);
        Assert.Equal(100, painel.ValorPendentes);
        Assert.Equal(1, painel.Devolvidas);
        Assert.Equal(40, painel.ValorDevolvido);
        Assert.Contains("Generico: 1", painel.PorClasse);
        Assert.Contains("Avaria: 1", painel.PorMotivo);
    }

    [Fact]
    public void Calcular_AbsorvidoNaoContaComoDevolvida()
    {
        var notas = new List<DevolucaoItem>
        {
            new()
            {
                Id = 1,
                ApelidoLoja = "BARAO",
                ValorNota = 80,
                StatusDevolucao = StatusDevolucaoValues.Absorvido
            }
        };

        var painel = DevolucoesPainel.Calcular(notas, new Dictionary<long, IReadOnlyList<DevolucaoItemPreNota>>());
        Assert.Equal(0, painel.Pendentes);
        Assert.Equal(0, painel.Devolvidas);
        Assert.Equal(1, painel.Absorvidas);
        Assert.Equal(80, painel.ValorAbsorvido);
        Assert.Equal(0, painel.PerdeuPrazo);
    }

    [Fact]
    public void Calcular_ContaAndamentoNasPendentes()
    {
        var notas = new List<DevolucaoItem>
        {
            new()
            {
                StatusDevolucao = StatusDevolucaoValues.Pendente,
                Andamento = AndamentoDevolucaoValues.EnviarXml,
                ValorNota = 10
            },
            new()
            {
                StatusDevolucao = StatusDevolucaoValues.Pendente,
                Andamento = AndamentoDevolucaoValues.EnviarXml,
                ValorNota = 5
            },
            new()
            {
                StatusDevolucao = StatusDevolucaoValues.Devolvida,
                Andamento = AndamentoDevolucaoValues.EnviarXml,
                ValorNota = 99
            }
        };

        var painel = DevolucoesPainel.Calcular(notas, new Dictionary<long, IReadOnlyList<DevolucaoItemPreNota>>());
        Assert.Contains("Enviar XML: 2", painel.PorAndamento);
    }
}
