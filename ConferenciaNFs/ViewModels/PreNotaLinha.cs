using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class PreNotaLinha : ViewModelBase
{
    private bool _selecionado;
    private decimal _quantidadeDevolver;
    private string _motivo = string.Empty;
    private string _classe = string.Empty;

    public PreNotaLinha(ItemNotaSnapshot item)
    {
        Item = item;
        _quantidadeDevolver = item.QuantItemCompra;
    }

    public ItemNotaSnapshot Item { get; }

    public int Sequencia => Item.Sequencia;
    public int CodProd => Item.CodProd;
    public string NomeProd => string.IsNullOrWhiteSpace(Item.NomeProd) ? $"Produto {Item.CodProd}" : Item.NomeProd;
    public string BarrasEan => Item.BarrasEan;
    public decimal QuantidadeNota => Item.QuantItemCompra;
    public decimal CustoNota => Item.CustoNota;

    public bool Selecionado
    {
        get => _selecionado;
        set => SetProperty(ref _selecionado, value);
    }

    public decimal QuantidadeDevolver
    {
        get => _quantidadeDevolver;
        set => SetProperty(ref _quantidadeDevolver, value);
    }

    public string Motivo
    {
        get => _motivo;
        set => SetProperty(ref _motivo, value ?? string.Empty);
    }

    public string Classe
    {
        get => _classe;
        set => SetProperty(ref _classe, value ?? string.Empty);
    }

    public DevolucaoItemPreNota ParaGravacao() => new()
    {
        Sequencia = Sequencia,
        CodProd = CodProd,
        NomeProd = NomeProd,
        BarrasEan = BarrasEan,
        Quantidade = QuantidadeDevolver,
        CustoNota = CustoNota,
        Motivo = Motivo.Trim(),
        Classe = Classe.Trim()
    };
}
