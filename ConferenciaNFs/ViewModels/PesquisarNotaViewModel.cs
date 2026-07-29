using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class PesquisarNotaViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private string _numNotaPesquisa = string.Empty;
    private string _mensagemPesquisa = "Informe o numero da nota para consultar o status da primeira conferencia.";
    private bool _pesquisaRealizada;
    private bool _isPesquisando;

    public PesquisarNotaViewModel(NotaFiscalRepository repository)
    {
        _repository = repository;
        Resultados = new ObservableCollection<NotaPesquisaResultado>();
        PesquisarCommand = new AsyncRelayCommand(_ => PesquisarAsync(), _ => PodePesquisar);
        LimparCommand = new RelayCommand(Limpar);
    }

    private bool PodePesquisar =>
        !string.IsNullOrWhiteSpace(NumNotaPesquisa) && !IsPesquisando;

    public ObservableCollection<NotaPesquisaResultado> Resultados { get; }

    public string NumNotaPesquisa
    {
        get => _numNotaPesquisa;
        set
        {
            if (!SetProperty(ref _numNotaPesquisa, value))
                return;

            ((AsyncRelayCommand)PesquisarCommand).RaiseCanExecuteChanged();
        }
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

    private async Task PesquisarAsync()
    {
        var termo = NumNotaPesquisa.Trim();
        if (string.IsNullOrWhiteSpace(termo))
            return;

        try
        {
            IsPesquisando = true;
            MensagemPesquisa = "Buscando nota...";

            var resultados = await _repository.PesquisarPrimeiraConferenciaPorNumNotaAsync(termo);

            Resultados.Clear();
            foreach (var resultado in resultados)
                Resultados.Add(resultado);

            PesquisaRealizada = true;
            OnPropertyChanged(nameof(TemResultados));
            OnPropertyChanged(nameof(ExibirResultados));

            MensagemPesquisa = resultados.Count switch
            {
                0 => $"Nenhuma nota encontrada com o numero {termo}.",
                1 => $"Status da primeira conferencia da nota {termo}.",
                _ => $"{resultados.Count} notas distintas encontradas com o numero {termo}."
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
        NumNotaPesquisa = string.Empty;
        Resultados.Clear();
        PesquisaRealizada = false;
        OnPropertyChanged(nameof(TemResultados));
        OnPropertyChanged(nameof(ExibirResultados));
        MensagemPesquisa = "Informe o numero da nota para consultar o status da primeira conferencia.";
    }
}
