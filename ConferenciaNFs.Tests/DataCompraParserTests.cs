using ConferenciaNFs.Infrastructure;
using Xunit;

namespace ConferenciaNFs.Tests;

public class DataCompraParserTests
{
    [Fact]
    public void EnumerarDias_UmDia_RetornaSomenteAqueleDia()
    {
        var dia = new DateTime(2026, 9, 11);

        var dias = DataCompraParser.EnumerarDias(dia, dia);

        Assert.Equal([dia], dias);
        Assert.Equal(["11/09/2026"], DataCompraParser.FormatarDias(dias));
    }

    [Fact]
    public void EnumerarDias_SabadoAteSegunda_IncluiTresDias()
    {
        var sabado = new DateTime(2026, 9, 5);
        var segunda = new DateTime(2026, 9, 7);

        var dias = DataCompraParser.EnumerarDias(sabado, segunda);

        Assert.Equal(
            [sabado, new DateTime(2026, 9, 6), segunda],
            dias);
        Assert.Equal("05/09/2026 a 07/09/2026", DataCompraParser.FormatarIntervalo(sabado, segunda));
    }

    [Fact]
    public void EnumerarDias_FimAntesDoInicio_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            DataCompraParser.EnumerarDias(new DateTime(2026, 9, 8), new DateTime(2026, 9, 7)));
    }

    [Fact]
    public void EnumerarDias_AcimaDoLimite_Lanca()
    {
        var inicio = new DateTime(2026, 9, 1);
        var fim = inicio.AddDays(DataCompraParser.MaxDiasIntervaloPadrao);

        Assert.Throws<ArgumentOutOfRangeException>(() => DataCompraParser.EnumerarDias(inicio, fim));
    }

    [Theory]
    [InlineData("2026-09-11", "2026-09-11", true)]
    [InlineData("2026-09-05", "2026-09-07", true)]
    [InlineData("2026-09-08", "2026-09-07", false)]
    [InlineData("2026-09-01", "2026-09-08", false)]
    public void IntervaloValido_RespeitaOrdemELimite(string inicioIso, string fimIso, bool esperado)
    {
        var inicio = DateTime.Parse(inicioIso);
        var fim = DateTime.Parse(fimIso);

        Assert.Equal(esperado, DataCompraParser.IntervaloValido(inicio, fim));
    }

    [Fact]
    public void FormatarIntervalo_MesmoDia_NaoUsaAte()
    {
        var dia = new DateTime(2026, 9, 10);
        Assert.Equal("10/09/2026", DataCompraParser.FormatarIntervalo(dia, dia));
    }

    [Fact]
    public void VariantesTextuais_CobremTodasAsGrafiasQueOParserAceita()
    {
        var dia = new DateTime(2026, 9, 5);

        var variantes = DataCompraParser.VariantesTextuais([dia]);

        Assert.Contains("05/09/2026", variantes);
        Assert.Contains("5/9/2026", variantes);
        Assert.Contains("05/09/26", variantes);
        Assert.All(variantes, v => Assert.Equal(dia, DataCompraParser.TentarConverter(v)));
    }

    [Theory]
    [InlineData("2026-09-07", "2026-09-05")] // segunda -> sabado anterior
    [InlineData("2026-09-08", "2026-09-05")] // terca pos-feriado -> mesmo sabado
    [InlineData("2026-09-05", "2026-09-05")] // o proprio sabado
    [InlineData("2026-09-06", "2026-09-05")] // domingo
    [InlineData("2026-09-11", "2026-09-05")] // sexta -> sabado de 6 dias atras
    public void UltimoSabado_RecuaAteOSabadoMaisRecente(string hojeIso, string esperadoIso)
    {
        var sabado = DataCompraParser.UltimoSabado(DateTime.Parse(hojeIso));

        Assert.Equal(DateTime.Parse(esperadoIso), sabado);
        Assert.Equal(DayOfWeek.Saturday, sabado.DayOfWeek);
    }

    [Fact]
    public void UltimoSabado_AteHoje_CabeNoLimiteDoIntervalo()
    {
        for (var offset = 0; offset < 7; offset++)
        {
            var hoje = new DateTime(2026, 9, 5).AddDays(offset);
            Assert.True(DataCompraParser.IntervaloValido(DataCompraParser.UltimoSabado(hoje), hoje));
        }
    }

    [Fact]
    public void VariantesTextuais_NaoRepetemQuandoDiaTemDoisDigitos()
    {
        var dia = new DateTime(2026, 11, 12);

        var variantes = DataCompraParser.VariantesTextuais([dia]);

        Assert.Equal(["12/11/2026", "12/11/26"], variantes.Order());
    }
}
