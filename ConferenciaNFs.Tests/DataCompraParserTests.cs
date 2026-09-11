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
}
