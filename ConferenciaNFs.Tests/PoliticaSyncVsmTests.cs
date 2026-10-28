using System.Text.Json;
using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class PoliticaSyncVsmTests
{
    [Fact]
    public void SemMysql_NaoSincronizaMesmoComCheckbox()
    {
        Assert.False(PoliticaSyncVsm.DeveSincronizar(mysqlConfigurado: false, nesteComputador: true));
    }

    [Fact]
    public void ComMysql_CheckboxDesligado_NaoSincronizaNemMostraBotao()
    {
        Assert.False(PoliticaSyncVsm.DeveSincronizar(mysqlConfigurado: true, nesteComputador: false));
    }

    [Fact]
    public void ComMysql_CheckboxLigado_Sincroniza()
    {
        Assert.True(PoliticaSyncVsm.DeveSincronizar(mysqlConfigurado: true, nesteComputador: true));
    }

    [Fact]
    public void AppSettings_SemChave_SincronizarVsmFicaDesligado()
    {
        var dados = JsonSerializer.Deserialize<AppSettings>("{}");

        Assert.NotNull(dados);
        Assert.False(dados.SincronizarVsm);
    }

    [Fact]
    public void AppSettings_ChaveTrue_SincronizarVsmLiga()
    {
        var dados = JsonSerializer.Deserialize<AppSettings>("""{"SincronizarVsm":true}""");

        Assert.NotNull(dados);
        Assert.True(dados.SincronizarVsm);
    }
}
