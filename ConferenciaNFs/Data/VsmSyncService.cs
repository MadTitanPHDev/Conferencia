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
        var resultado = await _repository.SincronizarComprasVsmAsync(compras, dia, cancellationToken);
        await _repository.ReconciliarFilaDevolucoesAsync(cancellationToken);
        return resultado;
    }

    /// <summary>
    /// Le o intervalo inteiro do VSM numa consulta so e grava dia a dia: o upsert exige um
    /// unico DiaConferencia por chamada, entao cada nota continua no dia em que entrou.
    /// </summary>
    public async Task<VsmSyncResult> SincronizarIntervaloAsync(
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default,
        IProgress<VsmSyncProgress>? progress = null)
    {
        var dias = DataCompraParser.EnumerarDias(inicio, fim).ToList();
        var totalEtapas = Math.Max(2, dias.Count + 2);

        progress?.Report(new VsmSyncProgress
        {
            Etapa = 0,
            Total = totalEtapas,
            Texto = "Lendo compras no VSM..."
        });

        var compras = await _reader.ListarPorIntervaloDataCompraAsync(inicio, fim, cancellationToken);

        var porDia = compras
            .Where(c => c.DataCompra.HasValue)
            .GroupBy(c => c.DataCompra!.Value.Date)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CompraVsm>)g.ToList());

        var total = new VsmSyncResult();
        var indice = 0;

        foreach (var dia in dias)
        {
            cancellationToken.ThrowIfCancellationRequested();
            indice++;

            progress?.Report(new VsmSyncProgress
            {
                Etapa = indice,
                Total = totalEtapas,
                Texto = $"Gravando {DataCompraParser.Formatar(dia)} ({indice}/{dias.Count})..."
            });

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

        progress?.Report(new VsmSyncProgress
        {
            Etapa = totalEtapas - 1,
            Total = totalEtapas,
            Texto = "Reconciliando fila de devoluções..."
        });

        await _repository.ReconciliarFilaDevolucoesAsync(cancellationToken);

        progress?.Report(new VsmSyncProgress
        {
            Etapa = totalEtapas,
            Total = totalEtapas,
            Texto = "Sincronização concluída."
        });

        return total;
    }
}
