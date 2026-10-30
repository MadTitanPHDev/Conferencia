using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class InicioComWindowsTests
{
    [Fact]
    public void CaminhoParaIniciar_PastaCurrent_UsaOExecutavelEstavel()
    {
        var processo = Path.Combine("C:", "Users", "Loja", "AppData", "Local", "ConferenciaNFs", "current", "ConferenciaNFs.exe");

        var caminho = InicioComWindows.CaminhoParaIniciar(processo);

        Assert.Equal(
            Path.Combine("C:", "Users", "Loja", "AppData", "Local", "ConferenciaNFs", "ConferenciaNFs.exe"),
            caminho);
    }

    [Fact]
    public void CaminhoParaIniciar_ForaDoVelopack_MantemOProprioExecutavel()
    {
        var processo = Path.Combine("C:", "dev", "ConferenciaNFs.exe");

        Assert.Equal(processo, InicioComWindows.CaminhoParaIniciar(processo));
    }
}
