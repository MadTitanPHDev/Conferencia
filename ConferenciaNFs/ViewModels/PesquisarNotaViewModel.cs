using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class StatusPesquisaOpcao
{
    public string Valor { get; init; } = string.Empty;
    public string Rotulo { get; init; } = string.Empty;

    public override string ToString() => Rotulo;
}

public sealed class PesquisarNotaViewModel : ViewModelBase
{
    private const string MensagemInicial =
        "Busque por numero (com ou sem zeros), loja ou fornecedor. Cada ocorrencia aparece com o dia e o status.";

    private readonly NotaFiscalRepository _repository;
    private readonly Action<NotaPesquisaResultado>? _aoAbrirConferencia;
    private readonly Action<NotaPesquisaResultado>? _aoAbrirFila;
    private string _termoPesquisa = string.Empty;
    private string _filtroLoja = string.Empty;
    private StatusPesquisaOpcao? _statusSelecionado;
    private DateTime? _dataInicio;
    private DateTime? _dataFim;
    private string _mensagemPesquisa = MensagemInicial;
    private bool _pesquisaRealizada;
    private bool _isPesquisando;

    public PesquisarNotaViewModel(
        NotaFiscalRepository repository,
        Action<NotaPesquisaResultado>? aoAbrirConferencia = null,
        Action<NotaPesquisaResultado>? aoAbrirFila = null)
    {
        _repository = repository;
        _aoAbrirConferencia = aoAbrirConferencia;
        _aoAbrirFila = aoAbrirFila;
        Resultados = new ObservableCollection<NotaPesquisaResultado>();
        StatusOpcoes = new ObservableCollection<StatusPesquisaOpcao>(CriarStatusOpcoes());
        _statusSelecionado = StatusOpcoes[0];
        PesquisarCommand = new AsyncRelayCommand(_ => PesquisarAsync(), _ => PodePesquisar);
        LimparCommand = new RelayCommand(Limpar);
        AbrirConferenciaCommand = new RelayCommand(
            p => AbrirConferencia(p as NotaPesquisaResultado),
            p => p is NotaPesquisaResultado);
        AbrirFilaCommand = new RelayCommand(
            p => AbrirFila(p as NotaPesquisaResultado),
            p => p is NotaPesquisaResultado r && r.EstaNaFila);
    }

    private bool PodePesquisar => !IsPesquisando;

    public ObservableCollection<NotaPesquisaResultado> Resultados { get; }
    public ObservableCollection<StatusPesquisaOpcao> StatusOpcoes { get; }

    public string TermoPesquisa
    {
        get => _termoPesquisa;
        set => SetProperty(ref _termoPesquisa, value);
    }

    public string FiltroLoja
    {
        get => _filtroLoja;
        set => SetProperty(ref _filtroLoja, value);
    }

    public StatusPesquisaOpcao? StatusSelecionado
    {
        get => _statusSelecionado;
        set => SetProperty(ref _statusSelecionado, value);
    }

    public DateTime? DataInicio
    {
        get => _dataInicio;
        set => SetProperty(ref _dataInicio, value);
    }

    public DateTime? DataFim
    {
        get => _dataFim;
        set => SetProperty(ref _dataFim, value);
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
            OnPropertyChanged(nameof(TemResultados));
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

    public bool TemResultados => Resultados.Count > 0;

    public bool ExibirResultados => PesquisaRealizada && TemResultados;

    public ICommand PesquisarCommand { get; }
    public ICommand LimparCommand { get; }
    public ICommand AbrirConferenciaCommand { get; }
    public ICommand AbrirFilaCommand { get; }

    /// <summary>Compatibilidade com bindings antigos do campo de busca.</summary>
    public string NumNotaPesquisa
    {
        get => TermoPesquisa;
        set => TermoPesquisa = value;
    }

    private async Task PesquisarAsync()
    {
        var termo = TermoPesquisa.Trim();
        var loja = FiltroLoja.Trim();
        var status = StatusSelecionado?.Valor?.Trim() ?? string.Empty;
        var inicio = DataInicio;
        var fim = DataFim;

        if (inicio.HasValue ^ fim.HasValue)
        {
            MessageBox.Show(
                "Preencha as duas datas do periodo, ou deixe as duas em branco.",
                "Pesquisar nota",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (inicio.HasValue && fim.HasValue)
        {
            var a = inicio.Value.Date;
            var b = fim.Value.Date;
            if (b < a)
                (a, b) = (b, a);

            if ((b - a).Days + 1 > 366)
            {
                MessageBox.Show(
                    "O periodo da busca nao pode passar de 366 dias.",
                    "Pesquisar nota",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            inicio = a;
            fim = b;
        }

        if (termo.Length == 0 && loja.Length == 0 && status.Length == 0 && !inicio.HasValue)
        {
            MessageBox.Show(
                "Informe o numero, a loja, o fornecedor, o status ou um periodo.",
                "Pesquisar nota",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            IsPesquisando = true;
            MensagemPesquisa = "Buscando nota...";

            var resultados = await _repository.PesquisarNotasAsync(new NotaPesquisaFiltro
            {
                Termo = termo,
                ApelidoLoja = loja,
                StatusConferencia = status,
                DataInicio = inicio,
                DataFim = fim
            });

            Resultados.Clear();
            foreach (var resultado in resultados)
                Resultados.Add(resultado);

            PesquisaRealizada = true;
            OnPropertyChanged(nameof(TemResultados));
            OnPropertyChanged(nameof(ExibirResultados));

            MensagemPesquisa = resultados.Count switch
            {
                0 => "Nenhuma ocorrencia encontrada com esses filtros.",
                1 => "1 ocorrencia encontrada.",
                _ => $"{resultados.Count} ocorrencias encontradas."
            };
        }
        catch (Exception ex)
        {
            PesquisaRealizada = true;
            Resultados.Clear();
            OnPropertyChanged(nameof(TemResultados));
            OnPropertyChanged(nameof(ExibirResultados));
            MensagemPesquisa = "Erro ao pesquisar nota.";
            MessageBox.Show($"Erro ao pesquisar nota:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsPesquisando = false;
        }
    }

    private void Limpar(object? _)
    {
        TermoPesquisa = string.Empty;
        FiltroLoja = string.Empty;
        StatusSelecionado = StatusOpcoes[0];
        DataInicio = null;
        DataFim = null;
        Resultados.Clear();
        PesquisaRealizada = false;
        OnPropertyChanged(nameof(TemResultados));
        OnPropertyChanged(nameof(ExibirResultados));
        MensagemPesquisa = MensagemInicial;
    }

    private void AbrirConferencia(NotaPesquisaResultado? resultado)
    {
        if (resultado is null)
            return;

        _aoAbrirConferencia?.Invoke(resultado);
    }

    private void AbrirFila(NotaPesquisaResultado? resultado)
    {
        if (resultado is null || !resultado.EstaNaFila)
            return;

        _aoAbrirFila?.Invoke(resultado);
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
}
