using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class ItemNotaSnapshotMap
{
    public static ItemNotaSnapshot DeVsm(long notaFiscalId, ItemCompraVsm item)
    {
        return new ItemNotaSnapshot
        {
            NotaFiscalId = notaFiscalId,
            CodCompra = item.CodCompra > 0 ? item.CodCompra : null,
            Sequencia = item.Sequencia,
            CodProd = item.CodProd,
            NomeProd = item.NomeProd?.Trim() ?? string.Empty,
            BarrasEan = item.BarrasEan?.Trim() ?? string.Empty,
            BarrasEanTrib = item.BarrasEanTrib?.Trim() ?? string.Empty,
            QuantItemCompra = item.QuantItemCompra,
            ValorItemCompra = item.ValorItemCompra,
            Desconto = item.Desconto,
            CustoNota = item.CustoNota,
            NumLote = string.IsNullOrWhiteSpace(item.NumLote) ? null : item.NumLote.Trim(),
            DataValidade = item.DataValidade.HasValue
                ? DataCompraParser.Formatar(item.DataValidade.Value)
                : null
        };
    }

    public static ItemCompraVsm ParaVsm(ItemNotaSnapshot item)
    {
        return new ItemCompraVsm
        {
            CodCompra = item.CodCompra ?? 0,
            Sequencia = item.Sequencia,
            CodProd = item.CodProd,
            NomeProd = item.NomeProd,
            QuantItemCompra = item.QuantItemCompra,
            ValorItemCompra = item.ValorItemCompra,
            Desconto = item.Desconto,
            BarrasEan = item.BarrasEan,
            BarrasEanTrib = item.BarrasEanTrib,
            NumLote = item.NumLote,
            DataValidade = DataCompraParser.TentarConverter(item.DataValidade)
        };
    }
}
