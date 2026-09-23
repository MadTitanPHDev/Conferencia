using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class DetalheNotaViewModel : ViewModelBase
{
    private readonly VsmComprasReader _reader;
    private readonly NotaFiscalRepository? _repository;
    private readonly NotaFiscal _nota;
    private bool _isCarregando;
    private bool _estaVazio = true;
    private bool _popupPrecosAberto;
    private string _mensagemStatus = "Carregando itens...";
    private ItemNotaPrecoLinha? _itemSelecionado;

    public DetalheNotaViewModel(
        VsmComprasReader reader,
        NotaFiscal nota,
        NotaFiscalRepository? repository = null)
    {
        _reader = reader;
        _repository = repository;
        _nota = nota;
        Itens = new ObservableCollection<ItemNotaPrecoLinha>();
        Importacoes = new ObservableCollection<ImportacaoPrecoOpcao>();
        AlternarPopupPrecosCommand = new AsyncRelayCommand(_ => AlternarPopupPrecosAsync(), _ => PodeCompararPrecos);
        CompararPrecosCommand = new AsyncRelayCommand(_ => CompararPrecosAsync(), _ => PodeCompararPrecos);
        _ = CarregarItensAsync();
    }

    public ObservableCollection<ItemNotaPrecoLinha> Itens { get; }
    public ObservableCollection<ImportacaoPrecoOpcao> Importacoes { get; }

    public bool PodeCompararPrecos => _repository is not null && !IsCarregando;

    public ItemNotaPrecoLinha? ItemSelecionado
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
        private set
        {
            if (!SetProperty(ref _isCarregando, value))
                return;
            OnPropertyChanged(nameof(PodeCompararPrecos));
            ((AsyncRelayCommand)AlternarPopupPrecosCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)CompararPrecosCommand).RaiseCanExecuteChanged();
        }
    }

    public bool EstaVazio
    {
        get => _estaVazio;
        private set => SetProperty(ref _estaVazio, value);
    }

    public bool PopupPrecosAberto
    {
        get => _popupPrecosAberto;
        set => SetProperty(ref _popupPrecosAberto, value);
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

    public ICommand AlternarPopupPrecosCommand { get; }
    public ICommand CompararPrecosCommand { get; }

    public bool CopiarEanItemSelecionado()
    {
        if (ItemSelecionado is null)
            return false;

        var ean = ItemSelecionado.EanParaCruzar;
        if (!EanNormalizer.EhValido(ean))
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

    private async Task AlternarPopupPrecosAsync()
    {
        if (_repository is null)
            return;

        if (PopupPrecosAberto)
        {
            PopupPrecosAberto = false;
            return;
        }

        try
        {
            var lista = await _repository.ListarPrecosImportacoesAsync();
            Importacoes.Clear();
            foreach (var item in lista)
            {
                var opcao = new ImportacaoPrecoOpcao(item);
                if (lista.Count == 1)
                    opcao.EstaSelecionada = true;
                Importacoes.Add(opcao);
            }

            PopupPrecosAberto = true;
            if (Importacoes.Count == 0)
                MensagemStatus = "Nenhuma tabela importada. Use Tabelas de preco no menu.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao listar tabelas de preco:\n{ex.Message}", "Buscar preco",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task CompararPrecosAsync()
    {
        if (_repository is null)
            return;

        var ids = Importacoes.Where(i => i.EstaSelecionada).Select(i => i.Id).ToList();
        if (ids.Count == 0)
        {
            MessageBox.Show("Selecione ao menos uma importacao.", "Buscar preco",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsCarregando = true;
            var eans = Itens.Select(i => i.EanParaCruzar).Where(EanNormalizer.EhValido).ToList();
            var mapa = await _repository.ObterPrecosPorEansAsync(ids, eans);

            var verde = 0;
            var azul = 0;
            var vermelho = 0;
            var cinza = 0;
            var ambar = 0;

            foreach (var linha in Itens)
            {
                if (!EanNormalizer.EhValido(linha.EanParaCruzar))
                {
                    linha.AplicarSemEan();
                    ambar++;
                    continue;
                }

                if (!mapa.TryGetValue(linha.EanParaCruzar, out var tabela))
                {
                    linha.AplicarForaDaPlanilha();
                    cinza++;
                    continue;
                }

                linha.AplicarTabela(tabela);
                switch (linha.ComparacaoResultado)
                {
                    case ComparacaoPrecoValores.Verde:
                        verde++;
                        break;
                    case ComparacaoPrecoValores.AzulClaro:
                        azul++;
                        break;
                    case ComparacaoPrecoValores.Vermelho:
                        vermelho++;
                        break;
                }
            }

            PopupPrecosAberto = false;
            MensagemStatus =
                $"Preco: {verde} verde · {azul} azul · {vermelho} vermelho · {cinza} fora · {ambar} sem EAN.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao comparar precos:\n{ex.Message}", "Buscar preco",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsCarregando = false;
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
                Itens.Add(new ItemNotaPrecoLinha(item));

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
