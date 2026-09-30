using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class DetalheNotaViewModel : ViewModelBase
{
    private readonly VsmComprasReader? _reader;
    private readonly NotaFiscalRepository? _repository;
    private readonly NotaFiscal _nota;
    private bool _isCarregando;
    private bool _estaVazio = true;
    private string _mensagemStatus = "Carregando itens...";
    private ItemNotaPrecoLinha? _itemSelecionado;

    public DetalheNotaViewModel(
        VsmComprasReader? reader,
        NotaFiscal nota,
        NotaFiscalRepository? repository = null)
    {
        _reader = reader;
        _repository = repository;
        _nota = nota;
        Itens = new ObservableCollection<ItemNotaPrecoLinha>();
        CompararPrecosCommand = new AsyncRelayCommand(_ => CompararPrecosAsync(), _ => PodeCompararPrecos);
        _ = CarregarItensAsync();
    }

    public ObservableCollection<ItemNotaPrecoLinha> Itens { get; }

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
            ((AsyncRelayCommand)CompararPrecosCommand).RaiseCanExecuteChanged();
        }
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

    private async Task CompararPrecosAsync()
    {
        if (_repository is null)
            return;

        try
        {
            IsCarregando = true;
            var tabelas = await _repository.ListarPrecosImportacoesAsync();
            if (tabelas.Count == 0)
            {
                MessageBox.Show(
                    "Nenhuma tabela importada. Use Tabelas de preco no menu.",
                    "Buscar preco",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var ids = tabelas.Select(t => t.Id).ToList();
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

            var rotuloTabelas = tabelas.Count == 1
                ? "1 tabela"
                : $"{tabelas.Count} tabelas";
            MensagemStatus =
                $"Preco ({rotuloTabelas}): {verde} verde · {azul} azul · {vermelho} vermelho · {cinza} fora · {ambar} sem EAN.";
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
            MensagemStatus = "Buscando itens...";

            IReadOnlyList<ItemCompraVsm> itens;
            var veioDoSnapshot = false;

            if (_repository is not null)
            {
                var foto = await ItemNotaSnapshotServico.GarantirAsync(_repository, _reader, _nota);
                itens = foto.Select(ItemNotaSnapshotMap.ParaVsm).ToList();
                veioDoSnapshot = _reader is null || foto.Count > 0;
            }
            else
            {
                var codCompra = _nota.CodCompra ?? 0;
                itens = _reader is null
                    ? []
                    : await _reader.ListarItensPorCodCompraAsync(codCompra);
            }

            Itens.Clear();
            foreach (var item in itens)
                Itens.Add(new ItemNotaPrecoLinha(item));

            EstaVazio = Itens.Count == 0;
            ItemSelecionado = Itens.FirstOrDefault();
            MensagemStatus = EstaVazio
                ? "Nenhum item encontrado para esta nota."
                : veioDoSnapshot && _reader is null
                    ? $"{Itens.Count} item(ns) da foto (VSM indisponivel)."
                    : $"{Itens.Count} item(ns) carregado(s).";
            OnPropertyChanged(nameof(TextoResumo));
        }
        catch (Exception ex)
        {
            EstaVazio = true;
            MensagemStatus = "Nao foi possivel carregar os itens.";
            MessageBox.Show(
                $"Erro ao buscar itens:\n{ex.Message}",
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
