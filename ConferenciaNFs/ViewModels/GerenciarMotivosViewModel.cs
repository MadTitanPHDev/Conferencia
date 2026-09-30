using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class GerenciarMotivosViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private MotivoDevolucao? _selecionado;
    private string _novoMotivo = string.Empty;
    private string _mensagem = "Motivos usados na pre-nota. Desativar esconde da lista, sem apagar o historico.";

    public GerenciarMotivosViewModel(NotaFiscalRepository repository)
    {
        _repository = repository;
        Motivos = new ObservableCollection<MotivoDevolucao>();
        AdicionarCommand = new AsyncRelayCommand(_ => AdicionarAsync(), _ => !string.IsNullOrWhiteSpace(NovoMotivo));
        DesativarCommand = new AsyncRelayCommand(_ => DesativarAsync(), _ => Selecionado is { Ativo: true });
        _ = CarregarAsync();
    }

    public ObservableCollection<MotivoDevolucao> Motivos { get; }
    public ICommand AdicionarCommand { get; }
    public ICommand DesativarCommand { get; }

    public MotivoDevolucao? Selecionado
    {
        get => _selecionado;
        set
        {
            if (!SetProperty(ref _selecionado, value))
                return;
            ((AsyncRelayCommand)DesativarCommand).RaiseCanExecuteChanged();
        }
    }

    public string NovoMotivo
    {
        get => _novoMotivo;
        set
        {
            if (!SetProperty(ref _novoMotivo, value))
                return;
            ((AsyncRelayCommand)AdicionarCommand).RaiseCanExecuteChanged();
        }
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    private async Task CarregarAsync()
    {
        var lista = await _repository.ListarMotivosDevolucaoAsync(somenteAtivos: false);
        Motivos.Clear();
        foreach (var item in lista)
            Motivos.Add(item);
    }

    private async Task AdicionarAsync()
    {
        try
        {
            await _repository.SalvarMotivoDevolucaoAsync(NovoMotivo);
            NovoMotivo = string.Empty;
            Mensagem = "Motivo cadastrado.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Motivos", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async Task DesativarAsync()
    {
        if (Selecionado is null)
            return;

        await _repository.DesativarMotivoDevolucaoAsync(Selecionado.Id);
        Mensagem = $"Motivo \"{Selecionado.Descricao}\" desativado.";
        await CarregarAsync();
    }
}
