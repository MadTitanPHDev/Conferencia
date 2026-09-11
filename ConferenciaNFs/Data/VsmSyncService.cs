using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Data;

/// <summary>
/// Sync do dia: MySQL compras → Postgres NotasFiscais. Nao apaga notas.
/// </summary>
public sealed class VsmSyncService
{
    private readonly NotaFiscalRepository _repository;
    private readonly VsmComprasReader _reader;

    public VsmSyncService(NotaFiscalRepository repository, VsmComprasReader reader)
    {
        _repository = repository;
        _reader = reader;
    }

    public VsmComprasReader Reader => _reader;

    public async Task<VsmSyncResult> SincronizarDiaAsync(
        DateTime dataCompra,
        CancellationToken cancellationToken = default)
    {
        var compras = await _reader.ListarPorDataCompraAsync(dataCompra, cancellationToken);
        var dia = DataCompraParser.Formatar(dataCompra);
        return await _repository.SincronizarComprasVsmAsync(compras, dia, cancellationToken);
    }

    public async Task<VsmSyncResult> SincronizarIntervaloAsync(
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default)
    {
        var total = new VsmSyncResult();
        foreach (var dia in DataCompraParser.EnumerarDias(inicio, fim))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parcial = await SincronizarDiaAsync(dia, cancellationToken);
            total.Lidas += parcial.Lidas;
            total.Inseridas += parcial.Inseridas;
            total.Atualizadas += parcial.Atualizadas;
            total.Ignoradas += parcial.Ignoradas;
            total.Relocadas += parcial.Relocadas;
        }

        return total;
    }
}
