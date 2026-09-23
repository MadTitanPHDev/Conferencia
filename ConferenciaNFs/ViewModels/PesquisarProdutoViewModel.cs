using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.Views;

namespace ConferenciaNFs.ViewModels;

public sealed class PesquisarProdutoViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private readonly VsmComprasReader _reader;
    private string _eanPesquisa = string.Empty;
    private DateTime? _dataInicio = DateTime.Today.AddDays(-30);
    private string _custoMinimoTexto = string.Empty;
    private string _mensagemPesquisa = "Informe o EAN, a data inicial e, se quiser, o custo minimo.";
    private bool _pesquisaRealizada;
    private bool _isPesquisando;
    private ItemEanPesquisaResultado? _itemSelecionado;

    public PesquisarProdutoViewModel(NotaFiscalRepository repository, VsmComprasReader reader)
    {
        _repository = repository;
        _reader = reader;
        Resultados = new ObservableCollection<ItemEanPesquisaResultado>();
        PesquisarCommand = new AsyncRelayCommand(_ => PesquisarAsync(), _ => PodePesquisar);
        LimparCommand = new RelayCommand(Limpar);
        AbrirItensCommand = new RelayCommand(_ => AbrirItensSelecionado(), _ => ItemSelecionado is not null);
        CopiarNumeroNotaCommand = new RelayCommand(_ => CopiarNumeroNotaSelecionada(), _ => ItemSelecionado is not null);
    }

    private bool PodePesquisar =>
        VsmComprasReader.NormalizarEan(EanPesquisa).Length >= 8
        && DataInicio.HasValue
        && !IsPesquisando;

    public ObservableCollection<ItemEanPesquisaResultado> Resultados { get; }

    public string EanPesquisa
    {
        get => _eanPesquisa;
        set
        {
            if (!SetProperty(ref _eanPesquisa, value))
                return;

            ((AsyncRelayCommand)PesquisarCommand).RaiseCanExecuteChanged();
        }
    }

    public DateTime? DataInicio
    {
        get => _dataInicio;
        set
        {
            if (!SetProperty(ref _dataInicio, value))
                return;

            ((AsyncRelayCommand)PesquisarCommand).RaiseCanExecuteChanged();
        }
    }

    public string CustoMinimoTexto
    {
        get => _custoMinimoTexto;
        set => SetProperty(ref _custoMinimoTexto, value);
    }

    public string MensagemPesquisa
    {
        get => _mensagemPesquisa;
        private set => SetProperty(ref _mensagemPesquisa, value);
    }

    public bool PesquisaRealizada
    {
        get => _pesquisaRealizada;
        private set
        {
            if (!SetProperty(ref _pesquisaRealizada, value))
                return;

            OnPropertyChanged(nameof(ExibirResultados));
        }
    }

    public bool IsPesquisando
    {
        get => _isPesquisando;
        private set
        {
            if (!SetProperty(ref _isPesquisando, value))
                return;

            ((AsyncRelayCommand)PesquisarCommand).RaiseCanExecuteChanged();
        }
    }

    public ItemEanPesquisaResultado? ItemSelecionado
    {
        get => _itemSelecionado;
        set
        {
            if (!SetProperty(ref _itemSelecionado, value))
                return;

            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool ExibirResultados => PesquisaRealizada && Resultados.Count > 0;

    public ICommand PesquisarCommand { get; }
    public ICommand LimparCommand { get; }
    public ICommand AbrirItensCommand { get; }
    public ICommand CopiarNumeroNotaCommand { get; }

    public bool CopiarNumeroNotaSelecionada()
    {
        if (ItemSelecionado is null || string.IsNullOrWhiteSpace(ItemSelecionado.NumNota))
            return false;

        try
        {
            var numero = ItemSelecionado.NumNota.Trim();
            Clipboard.SetText(numero);
            MensagemPesquisa = $"Numero da nota {numero} copiado.";
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void AbrirItensSelecionado()
    {
        if (ItemSelecionado is null)
            return;

        var nota = new NotaFiscal
        {
            CodCompra = ItemSelecionado.CodCompra,
            NumNota = ItemSelecionado.NumNota,
            ApelidoLoja = ItemSelecionado.Loja,
            NomeForn = ItemSelecionado.NomeForn,
            ValorNota = ItemSelecionado.ValorNota,
            DataCompra = ItemSelecionado.DataCompra
        };

        var janela = new DetalheNotaWindow(_reader, nota, _repository)
        {
            Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                ?? Application.Current.MainWindow
        };

        janela.ShowDialog();
    }

    private async Task PesquisarAsync()
    {
        var ean = VsmComprasReader.NormalizarEan(EanPesquisa);
        if (ean.Length < 8 || !DataInicio.HasValue)
            return;

        if (!TentarLerCustoMinimo(CustoMinimoTexto, out var custoMinimo))
        {
            MessageBox.Show(
                "Custo minimo invalido. Use o formato 12,03 ou deixe em branco.",
                "Pesquisar produto",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            IsPesquisando = true;
            MensagemPesquisa = "Buscando produto no VSM...";

            var encontrados = await _reader.BuscarItensPorEanAsync(ean, DataInicio.Value, custoMinimo);
            var mapaLojas = await _repository.ObterMapaLojasVsmAsync();
            var resumoNotas = await _repository.ObterResumoPorCodCompraAsync(
                encontrados.Select(i => i.CodCompra).Distinct().ToList());

            Resultados.Clear();
            foreach (var item in encontrados)
            {
                mapaLojas.TryGetValue(item.CodLoja, out var loja);
                resumoNotas.TryGetValue(item.CodCompra, out var resumo);

                var apelido = !string.IsNullOrWhiteSpace(resumo.ApelidoLoja)
                    ? resumo.ApelidoLoja
                    : loja?.NomeExibicao ?? loja?.ApelidoLoja ?? $"Loja {item.CodLoja}";

                var nomeForn = !string.IsNullOrWhiteSpace(resumo.NomeForn)
                    ? resumo.NomeForn
                    : item.CnpjForn ?? string.Empty;

                Resultados.Add(new ItemEanPesquisaResultado
                {
                    CodCompra = item.CodCompra,
                    Loja = apelido,
                    DataCompra = item.DataCompra.HasValue
                        ? DataCompraParser.Formatar(item.DataCompra.Value)
                        : string.Empty,
                    NumNota = item.NumNota?.Trim() ?? string.Empty,
                    CodProd = item.CodProd,
                    NomeProd = item.NomeProd?.Trim() ?? string.Empty,
                    BarrasEan = item.BarrasEan?.Trim() ?? ean,
                    Quantidade = item.QuantItemCompra,
                    Custo = item.CustoUnit,
                    ValorNota = item.ValorNota,
                    NomeForn = nomeForn,
                    StatusConferencia = resumo.StatusConferencia ?? string.Empty
                });
            }

            PesquisaRealizada = true;
            OnPropertyChanged(nameof(ExibirResultados));
            ItemSelecionado = Resultados.FirstOrDefault();

            var filtroCusto = custoMinimo.HasValue
                ? $" e custo acima de R$ {custoMinimo.Value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}"
                : string.Empty;

            MensagemPesquisa = Resultados.Count switch
            {
                0 => $"Nenhuma nota com o EAN {ean} a partir de {DataCompraParser.Formatar(DataInicio.Value)}{filtroCusto}.",
                VsmComprasReader.LimiteBuscaEan =>
                    $"Mostrando as {VsmComprasReader.LimiteBuscaEan} notas mais recentes do EAN {ean}.",
                _ => $"{Resultados.Count} ocorrencia(s) do EAN {ean} a partir de {DataCompraParser.Formatar(DataInicio.Value)}{filtroCusto}."
            };
        }
        catch (Exception ex)
        {
            PesquisaRealizada = true;
            Resultados.Clear();
            OnPropertyChanged(nameof(ExibirResultados));
            MensagemPesquisa = "Erro ao pesquisar produto.";
            MessageBox.Show($"Erro ao pesquisar produto:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsPesquisando = false;
        }
    }

    private void Limpar(object? _)
    {
        EanPesquisa = string.Empty;
        CustoMinimoTexto = string.Empty;
        DataInicio = DateTime.Today.AddDays(-30);
        Resultados.Clear();
        PesquisaRealizada = false;
        ItemSelecionado = null;
        OnPropertyChanged(nameof(ExibirResultados));
        MensagemPesquisa = "Informe o EAN, a data inicial e, se quiser, o custo minimo.";
    }

    private static bool TentarLerCustoMinimo(string? texto, out decimal? valor)
    {
        valor = null;
        if (string.IsNullOrWhiteSpace(texto))
            return true;

        try
        {
            var lido = ValorMonetarioParser.Parse(texto);
            if (lido > 0)
                valor = lido;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
