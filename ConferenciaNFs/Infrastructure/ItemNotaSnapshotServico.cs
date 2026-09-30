using ConferenciaNFs.Data;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class ItemNotaSnapshotServico
{
    public static async Task<IReadOnlyList<ItemNotaSnapshot>> GarantirAsync(
        NotaFiscalRepository repository,
        VsmComprasReader? reader,
        NotaFiscal nota,
        CancellationToken cancellationToken = default)
    {
        var doVsm = await TentarLerVsmAsync(reader, nota, cancellationToken);
        if (doVsm.Count > 0)
        {
            await repository.SubstituirItensNotaAsync(nota.Id, doVsm, cancellationToken);
            return doVsm;
        }

        return await repository.ObterItensNotaAsync(nota.Id, cancellationToken);
    }

    private static async Task<IReadOnlyList<ItemNotaSnapshot>> TentarLerVsmAsync(
        VsmComprasReader? reader,
        NotaFiscal nota,
        CancellationToken cancellationToken)
    {
        if (reader is null || nota.CodCompra is not int cod || cod <= 0)
            return [];

        try
        {
            var itens = await reader.ListarItensPorCodCompraAsync(cod, cancellationToken);
            return itens.Select(i => ItemNotaSnapshotMap.DeVsm(nota.Id, i)).ToList();
        }
        catch
        {
            return [];
        }
    }
}
