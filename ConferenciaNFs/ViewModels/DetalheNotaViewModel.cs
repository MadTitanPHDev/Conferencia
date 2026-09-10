using System.Collections.ObjectModel;
using System.Windows;
using ConferenciaNFs.Data;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class DetalheNotaViewModel : ViewModelBase
{
    private readonly VsmComprasReader _reader;
    private readonly NotaFiscal _nota;
    private bool _isCarregando;
    private bool _estaVazio = true;
    private string _mensagemStatus = "Carregando itens...";
    private ItemCompraVsm? _itemSelecionado;

    public DetalheNotaViewModel(VsmComprasReader reader, NotaFiscal nota)
    {
        _reader = reader;
        _nota = nota;
        Itens = new ObservableCollection<ItemCompraVsm>();
        _ = CarregarItensAsync();
    }

    public ObservableCollection<ItemCompraVsm> Itens { get; }

    public ItemCompraVsm? ItemSelecionado
    {
        get => _itemSelecionado;
        set => SetProperty(ref _itemSelecionado, value);
    }

    public string TituloNota =>
        $"Nota {_nota.NumNota} · {_nota.ApelidoLoja}";

    public string SubtituloNota
    {
        get
        {
            var fornecedor = string.IsNullOrWhiteSpace(_nota.NomeForn) ? _nota.CnpjForn : _nota.NomeForn;
            return $"{fornecedor} · R$ {_nota.ValorNota:N2}";
        }
    }

    public bool IsCarregando
    {
        get => _isCarregando;
        private set => SetProperty(ref _isCarregando, value);
    }

    public bool EstaVazio
    {
        get => _estaVazio;
        private set => SetProperty(ref _estaVazio, value);
    }

    public string MensagemStatus
    {
        get => _mensagemStatus;
        private set => SetProperty(ref _mensagemStatus, value);
    }

    public string TextoResumo
    {
        get
        {
            if (Itens.Count == 0)
                return "Nenhum item carregado. Duplo clique copia o EAN.";

            return $"{Itens.Count} item(ns) · nota R$ {_nota.ValorNota:N2} · Duplo clique copia o EAN.";
        }
    }

    public bool CopiarEanItemSelecionado()
    {
        if (ItemSelecionado is null)
            return false;

        var ean = ItemSelecionado.BarrasEan?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ean))
        {
            MensagemStatus = "Este item nao tem EAN para copiar.";
            return false;
        }

        try
        {
            Clipboard.SetText(ean);
            MensagemStatus = $"EAN {ean} copiado.";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task CarregarItensAsync()
    {
        try
        {
            IsCarregando = true;
            MensagemStatus = "Buscando itens no VSM...";

            var codCompra = _nota.CodCompra ?? 0;
            var itens = await _reader.ListarItensPorCodCompraAsync(codCompra);

            Itens.Clear();
            foreach (var item in itens)
                Itens.Add(item);

            EstaVazio = Itens.Count == 0;
            ItemSelecionado = Itens.FirstOrDefault();
            MensagemStatus = EstaVazio
                ? "Nenhum item encontrado para esta nota no VSM."
                : $"{Itens.Count} item(ns) carregado(s).";
            OnPropertyChanged(nameof(TextoResumo));
        }
        catch (Exception ex)
        {
            EstaVazio = true;
            MensagemStatus = "Nao foi possivel carregar os itens.";
            MessageBox.Show(
                $"Erro ao buscar itens no VSM:\n{ex.Message}",
                "Itens da nota",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            IsCarregando = false;
        }
    }
}
