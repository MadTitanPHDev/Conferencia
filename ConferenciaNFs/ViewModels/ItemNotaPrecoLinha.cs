using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class ItemNotaPrecoLinha : ViewModelBase
{
    private string? _comparacaoResultado;
    private string _textoVsTabela = string.Empty;

    public ItemNotaPrecoLinha(ItemCompraVsm item)
    {
        Item = item;
    }

    public ItemCompraVsm Item { get; }

    public int CodProd => Item.CodProd;
    public string? BarrasEan => Item.BarrasEan;
    public string? BarrasEanTrib => Item.BarrasEanTrib;
    public string? NomeProd => Item.NomeProd;
    public decimal QuantItemCompra => Item.QuantItemCompra;
    public decimal CustoUnit => Item.CustoNota;
    public string? NumLote => Item.NumLote;
    public DateTime? DataValidade => Item.DataValidade;
    public decimal QuantConferida => Item.QuantConferida;
    public decimal QuantDevol => Item.QuantDevol;

    public string EanParaCruzar
    {
        get
        {
            var ean = EanNormalizer.Normalizar(BarrasEan);
            if (EanNormalizer.EhValido(ean))
                return ean;
            return EanNormalizer.Normalizar(BarrasEanTrib);
        }
    }

    public string? ComparacaoResultado
    {
        get => _comparacaoResultado;
        private set
        {
            if (!SetProperty(ref _comparacaoResultado, value))
                return;
            OnPropertyChanged(nameof(ComparacaoRotulo));
        }
    }

    public string ComparacaoRotulo => ComparacaoPrecoValores.ObterRotulo(ComparacaoResultado);

    public string TextoVsTabela
    {
        get => _textoVsTabela;
        private set => SetProperty(ref _textoVsTabela, value);
    }

    public void LimparComparacao()
    {
        ComparacaoResultado = null;
        TextoVsTabela = string.Empty;
    }

    public void AplicarSemEan()
    {
        ComparacaoResultado = ComparacaoPrecoValores.Ambar;
        TextoVsTabela = "Sem EAN";
    }

    public void AplicarForaDaPlanilha()
    {
        ComparacaoResultado = ComparacaoPrecoValores.Cinza;
        TextoVsTabela = "Fora da planilha";
    }

    public void AplicarTabela(PrecoTabelaItem tabela)
    {
        ComparacaoResultado = ComparacaoPrecoTabela.Classificar(CustoUnit, tabela.PrecoPrimario);
        var secundario = tabela.PrecoSecundario is > 0
            ? $" · 2o R$ {tabela.PrecoSecundario.Value:N2}"
            : string.Empty;
        TextoVsTabela = $"R$ {tabela.PrecoPrimario:N2} · {tabela.NomeImportacao}{secundario}";
    }
}
