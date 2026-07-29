using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class GerenciarLojasViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private LojaOrdem? _lojaSelecionada;
    private string? _apelidoSemOrdemSelecionado;
    private int _novoNumeroOrdem = 39;
    private string _novoNomeExibicao = string.Empty;
    private int _editarNumeroOrdem;
    private string _editarNomeExibicao = string.Empty;
    private string _mensagem = string.Empty;

    public GerenciarLojasViewModel(NotaFiscalRepository repository)
    {
        _repository = repository;
        LojasCadastradas = new ObservableCollection<LojaOrdem>();
        ApelidosSemOrdem = new ObservableCollection<string>();

        SalvarNovaLojaCommand = new AsyncRelayCommand(_ => SalvarNovaLojaAsync(), _ => PodeSalvarNovaLoja);
        SalvarEdicaoCommand = new AsyncRelayCommand(_ => SalvarEdicaoAsync(), _ => LojaSelecionada is not null);
        RemoverCadastroCommand = new AsyncRelayCommand(_ => RemoverCadastroAsync(), _ => LojaSelecionada is not null);
        AtualizarCommand = new AsyncRelayCommand(_ => CarregarAsync());

        _ = CarregarAsync();
    }

    public ObservableCollection<LojaOrdem> LojasCadastradas { get; }
    public ObservableCollection<string> ApelidosSemOrdem { get; }

    public LojaOrdem? LojaSelecionada
    {
        get => _lojaSelecionada;
        set
        {
            if (!SetProperty(ref _lojaSelecionada, value))
                return;

            if (value is not null)
            {
                EditarNumeroOrdem = value.NumeroOrdem;
                EditarNomeExibicao = value.NomeExibicao;
            }
        }
    }

    public string? ApelidoSemOrdemSelecionado
    {
        get => _apelidoSemOrdemSelecionado;
        set
        {
            if (!SetProperty(ref _apelidoSemOrdemSelecionado, value))
                return;

            NovoNomeExibicao = value ?? string.Empty;
        }
    }

    public int NovoNumeroOrdem
    {
        get => _novoNumeroOrdem;
        set => SetProperty(ref _novoNumeroOrdem, value);
    }

    public string NovoNomeExibicao
    {
        get => _novoNomeExibicao;
        set => SetProperty(ref _novoNomeExibicao, value);
    }

    public int EditarNumeroOrdem
    {
        get => _editarNumeroOrdem;
        set => SetProperty(ref _editarNumeroOrdem, value);
    }

    public string EditarNomeExibicao
    {
        get => _editarNomeExibicao;
        set => SetProperty(ref _editarNomeExibicao, value);
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    public ICommand SalvarNovaLojaCommand { get; }
    public ICommand SalvarEdicaoCommand { get; }
    public ICommand RemoverCadastroCommand { get; }
    public ICommand AtualizarCommand { get; }

    private bool PodeSalvarNovaLoja =>
        !string.IsNullOrWhiteSpace(ApelidoSemOrdemSelecionado)
        && NovoNumeroOrdem > 0
        && !string.IsNullOrWhiteSpace(NovoNomeExibicao);

    private async Task CarregarAsync()
    {
        try
        {
            var cadastradas = await _repository.ObterLojasCadastradasAsync();
            var semOrdem = await _repository.ObterApelidosSemOrdemAsync();

            LojasCadastradas.Clear();
            foreach (var loja in cadastradas)
                LojasCadastradas.Add(loja);

            ApelidosSemOrdem.Clear();
            foreach (var apelido in semOrdem)
                ApelidosSemOrdem.Add(apelido);

            if (LojasCadastradas.Count > 0)
                NovoNumeroOrdem = LojasCadastradas.Max(l => l.NumeroOrdem) + 1;

            LojaSelecionada = LojasCadastradas.FirstOrDefault();
            ApelidoSemOrdemSelecionado = ApelidosSemOrdem.FirstOrDefault();

            Mensagem = semOrdem.Count > 0
                ? $"{semOrdem.Count} loja(s) importada(s) ainda sem numero. Atribua abaixo."
                : "Todas as lojas importadas possuem numero de ordem.";
        }
        catch (Exception ex)
        {
            Mensagem = "Erro ao carregar lojas.";
            MessageBox.Show($"Erro ao carregar lojas:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SalvarNovaLojaAsync()
    {
        if (!PodeSalvarNovaLoja)
            return;

        try
        {
            await _repository.SalvarLojaOrdemAsync(new LojaOrdem
            {
                ApelidoLoja = ApelidoSemOrdemSelecionado!,
                NumeroOrdem = NovoNumeroOrdem,
                NomeExibicao = NovoNomeExibicao.Trim()
            });

            Mensagem = $"Loja {ApelidoSemOrdemSelecionado} cadastrada como {NovoNumeroOrdem}.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar loja:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SalvarEdicaoAsync()
    {
        if (LojaSelecionada is null)
            return;

        try
        {
            await _repository.SalvarLojaOrdemAsync(new LojaOrdem
            {
                ApelidoLoja = LojaSelecionada.ApelidoLoja,
                NumeroOrdem = EditarNumeroOrdem,
                NomeExibicao = EditarNomeExibicao.Trim()
            });

            Mensagem = $"Loja {LojaSelecionada.ApelidoLoja} atualizada.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao atualizar loja:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RemoverCadastroAsync()
    {
        if (LojaSelecionada is null)
            return;

        var confirmar = MessageBox.Show(
            $"Remover o cadastro da loja {LojaSelecionada.ApelidoLoja}?",
            "Confirmar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmar != MessageBoxResult.Yes)
            return;

        try
        {
            await _repository.RemoverLojaOrdemAsync(LojaSelecionada.ApelidoLoja);
            Mensagem = $"Cadastro removido para {LojaSelecionada.ApelidoLoja}.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao remover loja:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
