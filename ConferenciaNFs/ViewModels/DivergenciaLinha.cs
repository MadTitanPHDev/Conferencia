using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class DivergenciaLinha : ViewModelBase
{
    private bool _selecionado;
    private decimal _quantidade;

    public DivergenciaLinha(ItemNotaSnapshot item)
    {
        Item = item;
        _quantidade = item.QuantItemCompra;
    }

    public ItemNotaSnapshot Item { get; }

    public int Sequencia => Item.Sequencia;
    public int CodProd => Item.CodProd;
    public string NomeProd => string.IsNullOrWhiteSpace(Item.NomeProd) ? $"Produto {Item.CodProd}" : Item.NomeProd;
    public string BarrasEan => Item.BarrasEan;
    public decimal QuantidadeNota => Item.QuantItemCompra;
    public decimal CustoNota => Item.CustoNota;
    public decimal TotalItem => Quantidade * CustoNota;

    public string EanParaCopiar
    {
        get
        {
            var ean = EanNormalizer.Normalizar(BarrasEan);
            if (EanNormalizer.EhValido(ean))
                return ean;
            return EanNormalizer.Normalizar(Item.BarrasEanTrib);
        }
    }

    public bool Selecionado
    {
        get => _selecionado;
        set => SetProperty(ref _selecionado, value);
    }

    public decimal Quantidade
    {
        get => _quantidade;
        set
        {
            if (!SetProperty(ref _quantidade, value))
                return;
            OnPropertyChanged(nameof(TotalItem));
        }
    }

    public PendenciaCompraItem ParaGravacao() => new()
    {
        Sequencia = Sequencia,
        CodProd = CodProd,
        NomeProd = NomeProd,
        BarrasEan = BarrasEan,
        Quantidade = Quantidade,
        CustoNota = CustoNota
    };
}
