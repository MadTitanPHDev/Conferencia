using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.Views;

namespace ConferenciaNFs.ViewModels;

public sealed class PendenciasCompraViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private readonly VsmComprasReader? _reader;
    private PendenciaCompraResumo? _selecionada;
    private string _mensagem = "Carregando divergencias enviadas...";
    private bool _isCarregando;
    private bool _mostrarConcluidas;

    public PendenciasCompraViewModel(NotaFiscalRepository repository, VsmComprasReader? reader = null)
    {
        _repository = repository;
        _reader = reader;
        Pendencias = new ObservableCollection<PendenciaCompraResumo>();
        AtualizarCommand = new AsyncRelayCommand(_ => CarregarAsync());
        AbrirItensCommand = new AsyncRelayCommand(_ => AbrirItensAsync(), _ => Selecionada is not null && !IsCarregando);
        _ = CarregarAsync();
    }

    public ObservableCollection<PendenciaCompraResumo> Pendencias { get; }
    public ICommand AtualizarCommand { get; }
    public ICommand AbrirItensCommand { get; }
    public Action? AoFechar { get; set; }

    public PendenciaCompraResumo? Selecionada
    {
        get => _selecionada;
        set
        {
            if (!SetProperty(ref _selecionada, value))
                return;
            OnPropertyChanged(nameof(TextoItensSelecionada));
            AtualizarComandos();
        }
    }

    public bool MostrarConcluidas
    {
        get => _mostrarConcluidas;
        set
        {
            if (!SetProperty(ref _mostrarConcluidas, value))
                return;
            _ = CarregarAsync();
        }
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    public bool IsCarregando
    {
        get => _isCarregando;
        private set
        {
            if (!SetProperty(ref _isCarregando, value))
                return;
            AtualizarComandos();
        }
    }

    public string TextoItensSelecionada =>
        Selecionada is null || string.IsNullOrWhiteSpace(Selecionada.ItensTexto)
            ? string.Empty
            : Selecionada.ItensTexto;

    private async Task CarregarAsync()
    {
        try
        {
            IsCarregando = true;
            var pessoaId = AppSettingsStore.Instance.Data.PessoaIdNestePc;
            if (pessoaId <= 0)
            {
                Pendencias.Clear();
                Selecionada = null;
                Mensagem = "Escolha quem usa este PC no menu para ver as pendencias.";
                return;
            }

            var pessoas = await _repository.ListarPessoasAsync(somenteAtivos: false);
            var eu = pessoas.FirstOrDefault(p => p.Id == pessoaId);
            if (eu is null)
            {
                Pendencias.Clear();
                Selecionada = null;
                Mensagem = "Escolha quem usa este PC no menu para ver as pendencias.";
                return;
            }

            var verTodas = EquipeConferencia.EhMembro(eu.Nome);
            var lista = await _repository.ListarPendenciasAsync(
                MostrarConcluidas,
                verTodas ? null : eu.Id);
            var idAntes = Selecionada?.Id;
            Pendencias.Clear();
            foreach (var item in lista)
                Pendencias.Add(item);

            Selecionada = idAntes is long id
                ? Pendencias.FirstOrDefault(p => p.Id == id) ?? Pendencias.FirstOrDefault()
                : Pendencias.FirstOrDefault();

            var escopo = verTodas ? "de todos" : "enviadas para voce";
            Mensagem = MostrarConcluidas
                ? (Pendencias.Count == 0
                    ? "Nenhuma divergencia concluida. Respostas ficam gravadas e nao sao apagadas."
                    : $"{Pendencias.Count} divergencia(s) concluida(s) {escopo}.")
                : (Pendencias.Count == 0
                    ? (verTodas
                        ? "Nenhuma divergencia enviada em aberto."
                        : "Nenhuma pendencia enviada para voce.")
                    : $"{Pendencias.Count} divergencia(s) {escopo}.");
        }
        catch (Exception ex)
        {
            Mensagem = "Erro ao carregar divergencias.";
            MessageBox.Show($"Erro ao carregar divergencias:\n{ex.Message}", "Divergencias",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
            OnPropertyChanged(nameof(TextoItensSelecionada));
            AtualizarComandos();
        }
    }

    public Task AbrirItensDaSelecionadaAsync() => AbrirItensAsync();

    private async Task AbrirItensAsync()
    {
        if (Selecionada is null)
            return;

        try
        {
            var nota = await _repository.ObterNotaPorIdAsync(Selecionada.NotaFiscalId);
            if (nota is null)
            {
                MessageBox.Show("Nota nao encontrada no cadastro.", "Divergencias",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var janela = new DivergenciaPreNotaWindow(_repository, _reader, nota, Selecionada.Id)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current.MainWindow
            };

            if (janela.ShowDialog() == true && janela.Confirmou)
            {
                var aviso = janela.MensagemResultado;
                await CarregarAsync();
                if (!string.IsNullOrWhiteSpace(aviso))
                    Mensagem = aviso;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao abrir a pre-nota:\n{ex.Message}", "Divergencias",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AtualizarComandos()
    {
        ((AsyncRelayCommand)AbrirItensCommand).RaiseCanExecuteChanged();
    }
}
