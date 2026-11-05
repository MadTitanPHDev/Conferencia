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
    private DateTime? _dataFim;
    private string _custoMinimoTexto = string.Empty;
    private string _mensagemPesquisa = "Informe o EAN e a data inicial. Loja, distribuidora, status e devolucao sao opcionais.";
    private bool _pesquisaRealizada;
    private bool _isPesquisando;
    private ItemEanPesquisaResultado? _itemSelecionado;
    private StatusPesquisaOpcao? _lojaSelecionada;
    private StatusPesquisaOpcao? _distribuidoraSelecionada;
    private StatusPesquisaOpcao? _statusSelecionado;
    private StatusPesquisaOpcao? _devolucaoSelecionada;

    public PesquisarProdutoViewModel(NotaFiscalRepository repository, VsmComprasReader reader)
    {
        _repository = repository;
        _reader = reader;
        Resultados = new ObservableCollection<ItemEanPesquisaResultado>();
        LojaOpcoes = new ObservableCollection<StatusPesquisaOpcao> { new() { Rotulo = "Todas" } };
        DistribuidoraOpcoes = new ObservableCollection<StatusPesquisaOpcao> { new() { Rotulo = "Todas" } };
        StatusOpcoes = new ObservableCollection<StatusPesquisaOpcao>(CriarStatusOpcoes());
        DevolucaoOpcoes = new ObservableCollection<StatusPesquisaOpcao>(CriarDevolucaoOpcoes());
        _lojaSelecionada = LojaOpcoes[0];
        _distribuidoraSelecionada = DistribuidoraOpcoes[0];
        _statusSelecionado = StatusOpcoes[0];
        _devolucaoSelecionada = DevolucaoOpcoes[0];
        PesquisarCommand = new AsyncRelayCommand(_ => PesquisarAsync(), _ => PodePesquisar);
        LimparCommand = new RelayCommand(Limpar);
        AbrirItensCommand = new AsyncRelayCommand(_ => AbrirItensSelecionadoAsync(), _ => ItemSelecionado is not null);
        CopiarNumeroNotaCommand = new RelayCommand(_ => CopiarNumeroNotaSelecionada(), _ => ItemSelecionado is not null);
        _ = CarregarOpcoesAsync();
    }

    private bool PodePesquisar =>
        VsmComprasReader.NormalizarEan(EanPesquisa).Length >= 8
        && DataInicio.HasValue
        && !IsPesquisando;

    public ObservableCollection<ItemEanPesquisaResultado> Resultados { get; }
    public ObservableCollection<StatusPesquisaOpcao> LojaOpcoes { get; }
    public ObservableCollection<StatusPesquisaOpcao> DistribuidoraOpcoes { get; }
    public ObservableCollection<StatusPesquisaOpcao> StatusOpcoes { get; }
    public ObservableCollection<StatusPesquisaOpcao> DevolucaoOpcoes { get; }

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

    public StatusPesquisaOpcao? LojaSelecionada
    {
        get => _lojaSelecionada;
        set => SetProperty(ref _lojaSelecionada, value);
    }

    public StatusPesquisaOpcao? DistribuidoraSelecionada
    {
        get => _distribuidoraSelecionada;
        set => SetProperty(ref _distribuidoraSelecionada, value);
    }

    public StatusPesquisaOpcao? StatusSelecionado
    {
        get => _statusSelecionado;
        set => SetProperty(ref _statusSelecionado, value);
    }

    public StatusPesquisaOpcao? DevolucaoSelecionada
    {
        get => _devolucaoSelecionada;
        set => SetProperty(ref _devolucaoSelecionada, value);
    }

    public DateTime? DataFim
    {
        get => _dataFim;
        set => SetProperty(ref _dataFim, value);
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

    public Task AbrirItensSelecionado() => AbrirItensSelecionadoAsync();

    private async Task AbrirItensSelecionadoAsync()
    {
        if (ItemSelecionado is null)
            return;

        var nota = ItemSelecionado.CodCompra > 0
            ? await _repository.ObterPorCodCompraAsync(ItemSelecionado.CodCompra)
            : null;

        nota ??= new NotaFiscal
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

        if (DataFim.HasValue && DataFim.Value.Date < DataInicio.Value.Date)
        {
            MessageBox.Show(
                "A data final nao pode ser anterior a data inicial.",
                "Pesquisar produto",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

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

            var encontrados = await _reader.BuscarItensPorEanAsync(
                ean,
                DataInicio.Value,
                custoMinimo,
                DataFim);
            var mapaLojas = await _repository.ObterMapaLojasVsmAsync();
            var codigos = encontrados.Select(i => i.CodCompra).Distinct().ToList();
            var resumoNotas = await _repository.ObterResumoPorCodCompraAsync(codigos);
            var devolucoes = await _repository.ObterItensDevolucaoPorCodCompraAsync(codigos);
            var devolucaoPorCompra = devolucoes
                .GroupBy(i => i.CodCompra)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ItemDevolucaoConsulta>)g.ToList());

            var lojaFiltro = LojaSelecionada?.Valor?.Trim() ?? string.Empty;
            var cnpjFiltro = DistribuidoraSelecionada?.Valor?.Trim() ?? string.Empty;
            var statusFiltro = StatusSelecionado?.Valor?.Trim() ?? string.Empty;
            var devolucaoFiltro = DevolucaoSelecionada?.Valor?.Trim() ?? string.Empty;
            var nomePorCnpj = DistribuidoraOpcoes
                .Where(o => o.Valor.Length > 0)
                .GroupBy(o => o.Valor, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().Rotulo, StringComparer.Ordinal);

            Resultados.Clear();
            foreach (var item in encontrados)
            {
                mapaLojas.TryGetValue(item.CodLoja, out var loja);
                resumoNotas.TryGetValue(item.CodCompra, out var resumo);

                var apelido = !string.IsNullOrWhiteSpace(resumo.ApelidoLoja)
                    ? resumo.ApelidoLoja
                    : loja?.NomeExibicao ?? loja?.ApelidoLoja ?? $"Loja {item.CodLoja}";

                var cnpjItem = CnpjNormalizer.Normalizar(item.CnpjForn);
                var nomeForn = nomePorCnpj.TryGetValue(cnpjItem, out var nomeCadastro) && !string.IsNullOrWhiteSpace(nomeCadastro)
                    ? nomeCadastro
                    : !string.IsNullOrWhiteSpace(resumo.NomeForn)
                        ? resumo.NomeForn
                        : item.CnpjForn ?? string.Empty;

                var status = resumo.StatusConferencia ?? string.Empty;
                var devolucaoRotulo = DevolucaoItemPesquisa.Resolver(
                    item.CodProd,
                    item.BarrasEan ?? ean,
                    devolucaoPorCompra.TryGetValue(item.CodCompra, out var daCompra) ? daCompra : []);

                var apelidoNota = resumo.ApelidoLoja ?? string.Empty;
                var apelidoMapa = loja?.ApelidoLoja ?? string.Empty;
                var lojaConfere = lojaFiltro.Length == 0
                    || string.Equals(apelidoNota, lojaFiltro, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(apelidoMapa, lojaFiltro, StringComparison.OrdinalIgnoreCase);
                var distribuidoraConfere = cnpjFiltro.Length == 0
                    || string.Equals(cnpjItem, cnpjFiltro, StringComparison.Ordinal);

                if (!lojaConfere
                    || !distribuidoraConfere
                    || !StatusConfere(status, statusFiltro)
                    || !DevolucaoItemPesquisa.ConfereFiltro(devolucaoRotulo, devolucaoFiltro))
                    continue;

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
                    StatusConferencia = status,
                    DevolucaoItemRotulo = devolucaoRotulo
                });
            }

            PesquisaRealizada = true;
            OnPropertyChanged(nameof(ExibirResultados));
            ItemSelecionado = Resultados.FirstOrDefault();

            var periodo = DataFim.HasValue
                ? $"{DataCompraParser.Formatar(DataInicio.Value)} a {DataCompraParser.Formatar(DataFim.Value)}"
                : $"a partir de {DataCompraParser.Formatar(DataInicio.Value)}";
            var filtroCusto = custoMinimo.HasValue
                ? $" e custo acima de R$ {custoMinimo.Value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}"
                : string.Empty;
            var avisoLimite = encontrados.Count == VsmComprasReader.LimiteBuscaEan
                ? $" A busca trouxe as {VsmComprasReader.LimiteBuscaEan} notas mais recentes."
                : string.Empty;

            MensagemPesquisa = Resultados.Count switch
            {
                0 => $"Nenhuma ocorrencia do EAN {ean} em {periodo}{filtroCusto}.{avisoLimite}",
                _ => $"{Resultados.Count} ocorrencia(s) do EAN {ean} em {periodo}{filtroCusto}.{avisoLimite}"
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
        LojaSelecionada = LojaOpcoes[0];
        DistribuidoraSelecionada = DistribuidoraOpcoes[0];
        StatusSelecionado = StatusOpcoes[0];
        DevolucaoSelecionada = DevolucaoOpcoes[0];
        CustoMinimoTexto = string.Empty;
        DataInicio = DateTime.Today.AddDays(-30);
        DataFim = null;
        Resultados.Clear();
        PesquisaRealizada = false;
        ItemSelecionado = null;
        OnPropertyChanged(nameof(ExibirResultados));
        MensagemPesquisa = "Informe o EAN e a data inicial. Loja, distribuidora, status e devolucao sao opcionais.";
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

    private async Task CarregarOpcoesAsync()
    {
        try
        {
            var lojas = await _repository.ObterLojasCadastradasAsync();
            var distribuidoras = await _repository.ObterDistribuidorasAsync();

            LojaOpcoes.Clear();
            LojaOpcoes.Add(new StatusPesquisaOpcao { Rotulo = "Todas" });
            foreach (var loja in lojas.Where(l => !string.IsNullOrWhiteSpace(l.ApelidoLoja)))
            {
                var nome = string.IsNullOrWhiteSpace(loja.NomeExibicao)
                    ? loja.ApelidoLoja.Trim()
                    : loja.NomeExibicao.Trim();
                LojaOpcoes.Add(new StatusPesquisaOpcao
                {
                    Valor = loja.ApelidoLoja.Trim(),
                    Rotulo = nome
                });
            }

            DistribuidoraOpcoes.Clear();
            DistribuidoraOpcoes.Add(new StatusPesquisaOpcao { Rotulo = "Todas" });
            foreach (var dist in distribuidoras)
            {
                var cnpj = CnpjNormalizer.Normalizar(dist.CnpjForn);
                if (cnpj.Length == 0)
                    continue;

                DistribuidoraOpcoes.Add(new StatusPesquisaOpcao
                {
                    Valor = cnpj,
                    Rotulo = string.IsNullOrWhiteSpace(dist.NomeForn) ? dist.CnpjForn.Trim() : dist.NomeForn.Trim()
                });
            }

            LojaSelecionada = LojaOpcoes[0];
            DistribuidoraSelecionada = DistribuidoraOpcoes[0];
        }
        catch (Exception ex)
        {
            MensagemPesquisa = "Nao foi possivel carregar lojas e distribuidoras.";
            MessageBox.Show(
                $"Erro ao carregar lojas e distribuidoras:\n{ex.Message}",
                "Pesquisar produto",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static bool StatusConfere(string? status, string filtro)
    {
        if (filtro.Length == 0)
            return true;

        return string.Equals(status?.Trim(), filtro, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<StatusPesquisaOpcao> CriarStatusOpcoes()
    {
        yield return new StatusPesquisaOpcao { Valor = string.Empty, Rotulo = "Todos os status" };

        foreach (var status in StatusConferenciaValues.Todos)
        {
            var rotulo = status == StatusConferenciaValues.Pendente ? "Branco" : status;
            yield return new StatusPesquisaOpcao
            {
                Valor = status,
                Rotulo = $"{rotulo} — {StatusConferenciaValues.ObterDescricao(status)}"
            };
        }
    }

    private static IEnumerable<StatusPesquisaOpcao> CriarDevolucaoOpcoes()
    {
        yield return new StatusPesquisaOpcao { Valor = string.Empty, Rotulo = "Todas" };
        yield return new StatusPesquisaOpcao
        {
            Valor = DevolucaoItemPesquisa.Devolvido,
            Rotulo = DevolucaoItemPesquisa.Devolvido
        };
        yield return new StatusPesquisaOpcao
        {
            Valor = DevolucaoItemPesquisa.NaFila,
            Rotulo = DevolucaoItemPesquisa.NaFila
        };
        yield return new StatusPesquisaOpcao
        {
            Valor = DevolucaoItemPesquisa.Nao,
            Rotulo = DevolucaoItemPesquisa.Nao
        };
    }
}
