using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class AvisoPendenciaNovasTests
{
    [Fact]
    public void PrimeiraLeitura_NaoAvisaOQueJaEstavaAberto()
    {
        var conhecidas = new HashSet<long>();
        var jaLeu = false;

        var novas = AvisoPendenciaNovas.Separar([1L, 2L], conhecidas, id => id, ref jaLeu);

        Assert.Empty(novas);
        Assert.True(jaLeu);
        Assert.Equal([1L, 2L], conhecidas.Order());
    }

    [Fact]
    public void Depois_AvisaSoOIdNovo()
    {
        var conhecidas = new HashSet<long> { 1, 2 };
        var jaLeu = true;

        var novas = AvisoPendenciaNovas.Separar([2L, 3L], conhecidas, id => id, ref jaLeu);

        Assert.Equal([3L], novas);
        Assert.DoesNotContain(1L, conhecidas);
        Assert.Contains(3L, conhecidas);
    }
}
