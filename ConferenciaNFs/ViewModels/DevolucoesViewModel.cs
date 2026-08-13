using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class DevolucoesViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private readonly List<DevolucaoItem> _todas = [];
    private DevolucaoItem? _selecionada;
    private string _filtroTexto = string.Empty;
    private string _filtroModo = "Pendentes";
    private string _observacaoTexto = string.Empty;
    private string _mensagem = string.Empty;
    private bool _isCarregando;
    private DateTime? _dataMinimaRastreio;
    private bool _removerPendentesAnteriores = true;

    public DevolucoesViewModel(NotaFiscalRepository repository)
    {
        _repository = repository;
        Devolucoes = new ObservableCollection<DevolucaoItem>();
        FiltrosModo = new ObservableCollection<string>(
        [
            "Pendentes",
            "Vencidas",
            "A vencer (7 dias)",
            "Sem prazo / nao rastreada",
            "Concluidas",
            "Todas"
        ]);

        AtualizarCommand = new AsyncRelayCommand(_ => CarregarAsync());
        MarcarDevolvidaCommand = new AsyncRelayCommand(
            _ => ConcluirAsync(StatusDevolucaoValues.Devolvida),
            _ => Selecionada?.PodeConcluir == true);
        MarcarPerdeuPrazoCommand = new AsyncRelayCommand(
            _ => ConcluirAsync(StatusDevolucaoValues.PerdeuPrazo),
            _ => Selecionada?.PodeConcluir == true);
        SalvarObservacaoCommand = new AsyncRelayCommand(
            _ => SalvarObservacaoAsync(),
            _ => Selecionada is not null);
        SalvarPeriodoCommand = new AsyncRelayCommand(_ => SalvarPeriodoAsync());
        LimparPeriodoCommand = new AsyncRelayCommand(_ => LimparPeriodoAsync());

        _ = CarregarAsync();
    }

    public ObservableCollection<DevolucaoItem> Devolucoes { get; }
    public ObservableCollection<string> FiltrosModo { get; }

    public DevolucaoItem? Selecionada
    {
        get => _selecionada;
        set
        {
            if (!SetProperty(ref _selecionada, value))
                return;

            ObservacaoTexto = value?.Observacao ?? string.Empty;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string FiltroTexto
    {
        get => _filtroTexto;
        set
        {
            if (!SetProperty(ref _filtroTexto, value))
                return;

            AplicarFiltro();
        }
    }

    public string FiltroModo
    {
        get => _filtroModo;
        set
        {
            if (!SetProperty(ref _filtroModo, value))
                return;

            AplicarFiltro();
        }
    }

    public string ObservacaoTexto
    {
        get => _observacaoTexto;
        set => SetProperty(ref _observacaoTexto, value);
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    public bool CopiarNumeroNotaSelecionada()
    {
        if (Selecionada is null || string.IsNullOrWhiteSpace(Selecionada.NumNota))
            return false;

        try
        {
            var numero = Selecionada.NumNota.Trim();
            Clipboard.SetText(numero);
            Mensagem = $"Numero da nota {numero} copiado.";
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsCarregando
    {
        get => _isCarregando;
        private set => SetProperty(ref _isCarregando, value);
    }

    public DateTime? DataMinimaRastreio
    {
        get => _dataMinimaRastreio;
        set => SetProperty(ref _dataMinimaRastreio, value);
    }

    public bool RemoverPendentesAnteriores
    {
        get => _removerPendentesAnteriores;
        set => SetProperty(ref _removerPendentesAnteriores, value);
    }

    public string TextoPeriodoAtual => DataMinimaRastreio.HasValue
        ? $"Rastreando a partir de {DataCompraParser.Formatar(DataMinimaRastreio.Value)}"
        : "Sem data minima (rastreia todas as devolucoes da fila)";

    public ICommand AtualizarCommand { get; }
    public ICommand MarcarDevolvidaCommand { get; }
    public ICommand MarcarPerdeuPrazoCommand { get; }
    public ICommand SalvarObservacaoCommand { get; }
    public ICommand SalvarPeriodoCommand { get; }
    public ICommand LimparPeriodoCommand { get; }

    private async Task CarregarAsync()
    {
        try
        {
            IsCarregando = true;
            DataMinimaRastreio = await _repository.ObterDataMinimaDevolucoesAsync();
            OnPropertyChanged(nameof(TextoPeriodoAtual));

            var lista = await _repository.ObterDevolucoesAsync();
            _todas.Clear();
            _todas.AddRange(lista);
            AplicarFiltro();

            var pendentes = _todas.Count(d => d.StatusDevolucao == StatusDevolucaoValues.Pendente);
            var vencidas = _todas.Count(d =>
                d.StatusDevolucao == StatusDevolucaoValues.Pendente
                && d.Urgencia == "Vencida");

            Mensagem = $"{TextoPeriodoAtual} · {_todas.Count} na fila · {pendentes} pendente(s)"
                       + (vencidas > 0 ? $" · {vencidas} vencida(s)" : string.Empty);
        }
        catch (Exception ex)
        {
            Mensagem = "Erro ao carregar devolucoes.";
            MessageBox.Show($"Erro ao carregar devolucoes:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private async Task SalvarPeriodoAsync()
    {
        try
        {
            var confirmar = MessageBox.Show(
                DataMinimaRastreio.HasValue
                    ? $"Definir rastreio de devolucoes a partir de {DataCompraParser.Formatar(DataMinimaRastreio.Value)}?\n\n"
                      + (RemoverPendentesAnteriores
                          ? "Pendencias anteriores a essa data serao removidas da fila (notas Vermelho antigas deixam de aparecer aqui)."
                          : "Pendencias anteriores apenas deixarao de aparecer na lista (permanecem no banco).")
                    : "Nenhuma data selecionada. Use \"Limpar periodo\" para rastrear todas, ou escolha uma data.",
                "Periodo de rastreio",
                DataMinimaRastreio.HasValue ? MessageBoxButton.YesNo : MessageBoxButton.OK,
                MessageBoxImage.Question);

            if (!DataMinimaRastreio.HasValue || confirmar != MessageBoxResult.Yes)
                return;

            await _repository.SalvarDataMinimaDevolucoesAsync(
                DataMinimaRastreio,
                RemoverPendentesAnteriores);

            Mensagem = $"Periodo salvo: a partir de {DataCompraParser.Formatar(DataMinimaRastreio.Value)}.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar periodo:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LimparPeriodoAsync()
    {
        var confirmar = MessageBox.Show(
            "Remover a data minima e voltar a listar todas as devolucoes da fila?",
            "Limpar periodo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmar != MessageBoxResult.Yes)
            return;

        try
        {
            DataMinimaRastreio = null;
            await _repository.SalvarDataMinimaDevolucoesAsync(null, removerPendentesAnteriores: false);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao limpar periodo:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AplicarFiltro()
    {
        IEnumerable<DevolucaoItem> query = _todas;

        query = FiltroModo switch
        {
            "Pendentes" => query.Where(d => d.StatusDevolucao == StatusDevolucaoValues.Pendente),
            "Vencidas" => query.Where(d =>
                d.StatusDevolucao == StatusDevolucaoValues.Pendente && d.Urgencia == "Vencida"),
            "A vencer (7 dias)" => query.Where(d =>
                d.StatusDevolucao == StatusDevolucaoValues.Pendente
                && d.DiasRestantes is >= 0 and <= 7),
            "Sem prazo / nao rastreada" => query.Where(d =>
                d.StatusDevolucao == StatusDevolucaoValues.Pendente
                && d.Urgencia is "SemRastreio" or "SemPrazo"),
            "Concluidas" => query.Where(d => d.StatusDevolucao != StatusDevolucaoValues.Pendente),
            _ => query
        };

        var termo = FiltroTexto.Trim();
        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(d =>
                d.NumNota.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || d.NomeForn.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || d.CnpjForn.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || d.ApelidoLoja.Contains(termo, StringComparison.OrdinalIgnoreCase));
        }

        var ordenado = query
            .OrderBy(d => d.StatusDevolucao == StatusDevolucaoValues.Pendente ? 0 : 1)
            .ThenBy(d => d.Urgencia switch
            {
                "Vencida" => 0,
                "Critica" => 1,
                "Atencao" => 2,
                "SemPrazo" => 3,
                "SemRastreio" => 4,
                _ => 5
            })
            .ThenBy(d => d.DiasRestantes ?? int.MaxValue)
            .ThenBy(d => d.NomeForn, StringComparer.OrdinalIgnoreCase);

        var idSelecionado = Selecionada?.Id;
        Devolucoes.Clear();
        foreach (var item in ordenado)
            Devolucoes.Add(item);

        Selecionada = idSelecionado is null
            ? Devolucoes.FirstOrDefault()
            : Devolucoes.FirstOrDefault(d => d.Id == idSelecionado) ?? Devolucoes.FirstOrDefault();
    }

    private async Task ConcluirAsync(string status)
    {
        if (Selecionada is null || !Selecionada.PodeConcluir)
            return;

        var rotulo = StatusDevolucaoValues.ObterRotulo(status);
        var confirmar = MessageBox.Show(
            $"Marcar a nota {Selecionada.NumNota} ({Selecionada.NomeForn}) como \"{rotulo}\"?",
            "Confirmar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmar != MessageBoxResult.Yes)
            return;

        try
        {
            await _repository.AtualizarStatusDevolucaoAsync(
                Selecionada.Id,
                status,
                ObservacaoTexto);

            Mensagem = $"Nota {Selecionada.NumNota}: {rotulo}.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao atualizar devolucao:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SalvarObservacaoAsync()
    {
        if (Selecionada is null)
            return;

        try
        {
            await _repository.AtualizarStatusDevolucaoAsync(
                Selecionada.Id,
                Selecionada.StatusDevolucao,
                ObservacaoTexto);

            Selecionada.Observacao = ObservacaoTexto.Trim();
            Mensagem = $"Observacao salva para a nota {Selecionada.NumNota}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar observacao:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
