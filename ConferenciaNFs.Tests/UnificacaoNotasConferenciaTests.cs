using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Xunit;

namespace ConferenciaNFs.Tests;

public class UnificacaoNotasConferenciaTests
{
    [Fact]
    public void Unificar_DuasPendenteMesmoCodCompra_FicaAMaisNova()
    {
        var sabado = Nota(1, "1821", "24/10/2026", StatusConferenciaValues.Pendente, 1279122);
        var segunda = Nota(2, "1821", "26/10/2026", StatusConferenciaValues.Pendente, 1279122);

        var visiveis = UnificacaoNotasConferencia.UnificarMesmoStatus([sabado, segunda]);

        var unica = Assert.Single(visiveis);
        Assert.Equal(2, unica.Id);
        Assert.Equal("26/10/2026", unica.DiaConferencia);
    }

    [Fact]
    public void Unificar_DuasVerdeMesmoCodCompra_FicaAMaisNova()
    {
        var sabado = Nota(1, "8187916", "24/10/2026", StatusConferenciaValues.Verde, 1279138);
        var segunda = Nota(2, "8187916", "26/10/2026", StatusConferenciaValues.Verde, 1279138);

        var visiveis = UnificacaoNotasConferencia.UnificarMesmoStatus([sabado, segunda]);

        Assert.Equal(2, Assert.Single(visiveis).Id);
    }

    [Fact]
    public void Unificar_VerdeELaranja_MantemAsDuas()
    {
        var sabado = Nota(1, "7094197", "24/10/2026", StatusConferenciaValues.Verde, 1279146);
        var segunda = Nota(2, "7094197", "26/10/2026", StatusConferenciaValues.Laranja, 1279146);

        var visiveis = UnificacaoNotasConferencia.UnificarMesmoStatus([sabado, segunda]);

        Assert.Equal(2, visiveis.Count);
        Assert.Contains(visiveis, n => n.Id == 1);
        Assert.Contains(visiveis, n => n.Id == 2);
    }

    [Fact]
    public void Unificar_PendenteEVerde_MantemAsDuas()
    {
        var sabado = Nota(1, "300038", "24/10/2026", StatusConferenciaValues.Pendente, 1279155);
        var segunda = Nota(2, "300038", "26/10/2026", StatusConferenciaValues.Verde, 1279155);

        var visiveis = UnificacaoNotasConferencia.UnificarMesmoStatus([sabado, segunda]);

        Assert.Equal(2, visiveis.Count);
    }

    [Fact]
    public void Unificar_MesmoNumeroFornecedorDiferente_NaoJunta()
    {
        var a = Nota(1, "123", "24/10/2026", StatusConferenciaValues.Pendente, codCompra: null, cnpj: "111");
        var b = Nota(2, "123", "26/10/2026", StatusConferenciaValues.Pendente, codCompra: null, cnpj: "222");

        var visiveis = UnificacaoNotasConferencia.UnificarMesmoStatus([a, b]);

        Assert.Equal(2, visiveis.Count);
    }

    [Fact]
    public void Unificar_PreservaOrdemDasDemaisNotas()
    {
        var outra = Nota(10, "999", "24/10/2026", StatusConferenciaValues.Pendente, 1);
        var sabado = Nota(1, "1821", "24/10/2026", StatusConferenciaValues.Pendente, 1279122);
        var segunda = Nota(2, "1821", "26/10/2026", StatusConferenciaValues.Pendente, 1279122);

        var visiveis = UnificacaoNotasConferencia.UnificarMesmoStatus([outra, sabado, segunda]);

        Assert.Equal([10L, 2L], visiveis.Select(n => n.Id).ToArray());
    }

    [Fact]
    public void Unificar_NaoApagaNemMudaDiaDasLinhasOriginais()
    {
        var sabado = Nota(1, "1821", "24/10/2026", StatusConferenciaValues.Pendente, 1279122);
        var segunda = Nota(2, "1821", "26/10/2026", StatusConferenciaValues.Pendente, 1279122);
        var origem = new[] { sabado, segunda };

        UnificacaoNotasConferencia.UnificarMesmoStatus(origem);

        Assert.Equal("24/10/2026", sabado.DiaConferencia);
        Assert.Equal("26/10/2026", segunda.DiaConferencia);
        Assert.Equal(StatusConferenciaValues.Pendente, sabado.StatusConferencia);
        Assert.Equal(2, origem.Length);
    }

    [Fact]
    public void CopiasComMesmoStatus_DevolveSoAsPendenteDoGrupo()
    {
        var sabado = Nota(1, "1821", "24/10/2026", StatusConferenciaValues.Pendente, 1279122);
        var segunda = Nota(2, "1821", "26/10/2026", StatusConferenciaValues.Pendente, 1279122);
        var outra = Nota(3, "999", "26/10/2026", StatusConferenciaValues.Pendente, 9);

        var copias = UnificacaoNotasConferencia.CopiasComMesmoStatus(segunda, [sabado, segunda, outra]);

        var unica = Assert.Single(copias);
        Assert.Equal(1, unica.Id);
    }

    [Fact]
    public void ContarVisiveis_DuasPendente_ContaUma()
    {
        var sabado = Nota(1, "1821", "24/10/2026", StatusConferenciaValues.Pendente, 1279122);
        var segunda = Nota(2, "1821", "26/10/2026", StatusConferenciaValues.Pendente, 1279122);
        var outra = Nota(3, "999", "26/10/2026", StatusConferenciaValues.Verde, 9);

        var (total, pendencias) = UnificacaoNotasConferencia.ContarVisiveis([sabado, segunda, outra]);

        Assert.Equal(2, total);
        Assert.Equal(1, pendencias);
    }

    [Fact]
    public void AgruparPendenciasPorLoja_NaoDuplicaCardDaLoja()
    {
        var sabado = Nota(1, "1821", "24/10/2026", StatusConferenciaValues.Pendente, 1279122);
        sabado.NomeForn = "S&H";
        var segunda = Nota(2, "1821", "26/10/2026", StatusConferenciaValues.Pendente, 1279122);
        segunda.NomeForn = "S&H";
        var amarela = Nota(3, "300934", "24/10/2026", StatusConferenciaValues.Amarelo, 1279181);
        amarela.NomeForn = "Santa Cruz";
        var amarelaCopia = Nota(4, "300934", "26/10/2026", StatusConferenciaValues.Amarelo, 1279181);
        amarelaCopia.NomeForn = "Santa Cruz";

        var lojas = UnificacaoNotasConferencia.AgruparPendenciasPorLoja(
            [sabado, segunda, amarela, amarelaCopia],
            new Dictionary<string, LojaOrdem>(StringComparer.OrdinalIgnoreCase)
            {
                ["VELASQUES"] = new() { ApelidoLoja = "VELASQUES", NumeroOrdem = 2, NomeExibicao = "Velasques" }
            });

        var loja = Assert.Single(lojas);
        Assert.Equal(2, loja.Pendencias);
        Assert.Equal(1, loja.QtdAmarelo);
        Assert.Equal(1, loja.QtdBranco);
        Assert.Single(loja.AmarelosPorDistribuidora);
        Assert.Equal(1, loja.AmarelosPorDistribuidora[0].Quantidade);
        Assert.DoesNotContain(loja.NotasPendentes, n => loja.NotasPendentes.Count(x => x.NumNota == n.NumNota) > 1);
    }

    [Fact]
    public void MesmaIdentidade_PorChaveNfe()
    {
        var a = Nota(1, "1", "24/10/2026", StatusConferenciaValues.Pendente, codCompra: null, nfe: "ABC");
        var b = Nota(2, "99", "26/10/2026", StatusConferenciaValues.Pendente, codCompra: null, nfe: "ABC");

        Assert.True(UnificacaoNotasConferencia.MesmaIdentidade(a, b));
    }

    private static NotaFiscal Nota(
        long id,
        string num,
        string dia,
        string status,
        int? codCompra,
        string cnpj = "12345678000199",
        string nfe = "")
        => new()
        {
            Id = id,
            ApelidoLoja = "VELASQUES",
            NumNota = num,
            CnpjForn = cnpj,
            DiaConferencia = dia,
            StatusConferencia = status,
            CodCompra = codCompra,
            NfeChaveAcesso = nfe
        };
}
