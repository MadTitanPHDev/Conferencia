using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class GerenciarPessoasViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private Pessoa? _selecionada;
    private string _novoNome = string.Empty;
    private string _mensagem = "Cadastro compartilhado. Desativar esconde a pessoa das listas novas, sem apagar o nome.";

    public GerenciarPessoasViewModel(NotaFiscalRepository repository)
    {
        _repository = repository;
        Pessoas = new ObservableCollection<Pessoa>();
        AdicionarCommand = new AsyncRelayCommand(_ => AdicionarAsync(), _ => !string.IsNullOrWhiteSpace(NovoNome));
        DesativarCommand = new AsyncRelayCommand(_ => DesativarAsync(), _ => Selecionada is { Ativo: true });
        _ = CarregarAsync();
    }

    public ObservableCollection<Pessoa> Pessoas { get; }
    public ICommand AdicionarCommand { get; }
    public ICommand DesativarCommand { get; }

    public Pessoa? Selecionada
    {
        get => _selecionada;
        set
        {
            if (!SetProperty(ref _selecionada, value))
                return;
            ((AsyncRelayCommand)DesativarCommand).RaiseCanExecuteChanged();
        }
    }

    public string NovoNome
    {
        get => _novoNome;
        set
        {
            if (!SetProperty(ref _novoNome, value))
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
        var lista = await _repository.ListarPessoasAsync(somenteAtivos: false);
        Pessoas.Clear();
        foreach (var item in lista)
            Pessoas.Add(item);
    }

    private async Task AdicionarAsync()
    {
        try
        {
            await _repository.SalvarPessoaAsync(NovoNome);
            NovoNome = string.Empty;
            Mensagem = "Pessoa cadastrada.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Pessoas", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async Task DesativarAsync()
    {
        if (Selecionada is null)
            return;

        try
        {
            var nome = Selecionada.Nome;
            await _repository.DesativarPessoaAsync(Selecionada.Id);
            Mensagem = $"\"{nome}\" desativada. O nome permanece no cadastro.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Pessoas", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
