using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ConferenciaNFs.Tests;

public class ImportacaoCsvTests
{
    private const string ArquivoReal = @"c:\Users\User\Desktop\Notas\2026\Julho\dados2.csv";
    private const string ArquivoAliasesAtualizados = @"c:\Users\User\Desktop\Notas\2026\Julho\dados 4.csv";

    [Fact]
    public void DetectarDelimitador_ArquivoReal_DeveSerPontoVirgula()
    {
        Assert.True(File.Exists(ArquivoReal));
        Assert.Equal(';', CsvDelimitadorDetector.Detectar(ArquivoReal));
    }

    [Fact]
    public async Task ImportarCsv_ArquivoReal_DeveInserirRegistros()
    {
        Assert.True(File.Exists(ArquivoReal));

        var dbPath = Path.Combine(Path.GetTempPath(), $"conferencia-test-{Guid.NewGuid():N}.db");
        var repository = new NotaFiscalRepository(dbPath);

        var inseridos = await repository.ImportarCsvAsync(ArquivoReal);

        Assert.True(inseridos > 0, $"Esperava registros inseridos, obteve {inseridos}.");

        var datas = await repository.ObterDatasDisponiveisAsync();
        Assert.NotEmpty(datas);

        var lojas = await repository.ObterPendenciasPorLojaAsync(datas[0]);
        Assert.NotEmpty(lojas);

        SqliteConnection.ClearAllPools();
        File.Delete(dbPath);
    }

    [Fact]
    public async Task ImportarCsv_ArquivoComAliasesAtualizados_DeveNormalizarLojas()
    {
        Assert.True(File.Exists(ArquivoAliasesAtualizados));

        var dbPath = Path.Combine(Path.GetTempPath(), $"conferencia-test-{Guid.NewGuid():N}.db");
        var repository = new NotaFiscalRepository(dbPath);

        var inseridos = await repository.ImportarCsvAsync(ArquivoAliasesAtualizados);

        Assert.True(inseridos > 0, $"Esperava registros inseridos, obteve {inseridos}.");

        var datas = await repository.ObterDatasDisponiveisAsync();
        var lojas = await repository.ObterPendenciasPorLojaAsync(datas[0]);

        Assert.Contains(lojas, loja => loja.ApelidoLoja == "MANOEL GOU" && loja.NumeroOrdem == 21);
        Assert.Contains(lojas, loja => loja.ApelidoLoja == "QUATA FILI" && loja.NumeroOrdem == 16);
        Assert.Contains(lojas, loja => loja.ApelidoLoja == "LUCELIA" && loja.NumeroOrdem == 20);
        Assert.Contains(lojas, loja => loja.ApelidoLoja == "LUCELIA CE" && loja.NumeroOrdem == 28);

        SqliteConnection.ClearAllPools();
        File.Delete(dbPath);
    }
}
