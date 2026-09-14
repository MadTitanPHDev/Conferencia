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

    /// <summary>
    /// Le o intervalo inteiro do VSM numa consulta so e grava dia a dia: o upsert exige um
    /// unico DiaConferencia por chamada, entao cada nota continua no dia em que entrou.
    /// </summary>
    public async Task<VsmSyncResult> SincronizarIntervaloAsync(
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default)
    {
        var dias = DataCompraParser.EnumerarDias(inicio, fim);
        var compras = await _reader.ListarPorIntervaloDataCompraAsync(inicio, fim, cancellationToken);

        var porDia = compras
            .Where(c => c.DataCompra.HasValue)
            .GroupBy(c => c.DataCompra!.Value.Date)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CompraVsm>)g.ToList());

        var total = new VsmSyncResult();

        foreach (var dia in dias)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!porDia.TryGetValue(dia.Date, out var comprasDoDia))
                comprasDoDia = [];

            var parcial = await _repository.SincronizarComprasVsmAsync(
                comprasDoDia,
                DataCompraParser.Formatar(dia),
                cancellationToken);

            total.Lidas += parcial.Lidas;
            total.Inseridas += parcial.Inseridas;
            total.Atualizadas += parcial.Atualizadas;
            total.Ignoradas += parcial.Ignoradas;
            total.Relocadas += parcial.Relocadas;
        }

        return total;
    }
}
