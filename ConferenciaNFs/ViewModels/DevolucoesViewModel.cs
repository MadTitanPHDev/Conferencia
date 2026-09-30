using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.Views;
using Microsoft.Win32;

namespace ConferenciaNFs.ViewModels;

public sealed class DevolucoesViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private readonly List<DevolucaoItem> _todas = [];
    private Dictionary<long, IReadOnlyList<DevolucaoItemPreNota>> _itensPorDevolucao = [];
    private DevolucoesPainel _painel = new();
    private DevolucaoItem? _selecionada;
    private string _filtroTexto = string.Empty;
    private string _filtroModo = "Pendentes";
    private string _filtroAndamento = AndamentoDevolucaoValues.FiltroTodos;
    private string _andamentoSelecionado = AndamentoDevolucaoValues.AguardandoLoja;
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
            "Absorvidas",
            "Todas"
        ]);
        FiltrosAndamento = new ObservableCollection<string>(AndamentoDevolucaoValues.Filtros);

        AtualizarCommand = new AsyncRelayCommand(_ => CarregarAsync());
        AbrirItensCommand = new AsyncRelayCommand(_ => AbrirItensAsync(), _ => Selecionada is not null);
        MarcarDevolvidaCommand = new AsyncRelayCommand(
            _ => AbrirItensAsync(marcarDevolvida: true),
            _ => Selecionada?.PodeConcluir == true);
        MarcarPerdeuPrazoCommand = new AsyncRelayCommand(
            _ => ConcluirAsync(StatusDevolucaoValues.PerdeuPrazo),
            _ => Selecionada?.PodeConcluir == true);
        MarcarAbsorvidoCommand = new AsyncRelayCommand(
            _ => ConcluirAsync(StatusDevolucaoValues.Absorvido),
            _ => Selecionada?.PodeConcluir == true);
        SalvarObservacaoCommand = new AsyncRelayCommand(
            _ => SalvarObservacaoAsync(),
            _ => Selecionada is not null);
        SalvarAndamentoCommand = new AsyncRelayCommand(
            _ => SalvarAndamentoAsync(),
            _ => Selecionada?.PodeConcluir == true);
        SalvarPeriodoCommand = new AsyncRelayCommand(_ => SalvarPeriodoAsync());
        LimparPeriodoCommand = new AsyncRelayCommand(_ => LimparPeriodoAsync());
        ExportarExcelCommand = new AsyncRelayCommand(_ => ExportarExcelAsync(), _ => Devolucoes.Count > 0);

        _ = CarregarAsync();
    }

    public ObservableCollection<DevolucaoItem> Devolucoes { get; }
    public ObservableCollection<string> FiltrosModo { get; }
    public ObservableCollection<string> FiltrosAndamento { get; }
    public IReadOnlyList<AndamentoDevolucaoOpcao> Andamentos { get; } = AndamentoDevolucaoValues.Opcoes;

    public DevolucaoItem? Selecionada
    {
        get => _selecionada;
        set
        {
            if (!SetProperty(ref _selecionada, value))
                return;

            ObservacaoTexto = string.Empty;
            SincronizarAndamentoCombo(value);
            _ = CarregarDetalheSelecionadaAsync(value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public ObservableCollection<DevolucaoItemPreNota> ItensPreNota { get; } = [];
    public ObservableCollection<DevolucaoObservacao> HistoricoObservacoes { get; } = [];

    public string TextoItensPreNota =>
        ItensPreNota.Count == 0
            ? "Esta nota nao tem pre-nota (marcada no fluxo antigo)."
            : string.Join(" · ", ItensPreNota.Select(i =>
                $"{i.NomeProd} ({i.Quantidade:N3}) · {i.Motivo} · {i.Classe}"));

    public string TextoHistoricoObservacoes =>
        HistoricoObservacoes.Count == 0
            ? "Nenhum recado ainda."
            : DevolucaoObservacaoHistorico.Juntar(HistoricoObservacoes.ToList());

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

    public string FiltroAndamento
    {
        get => _filtroAndamento;
        set
        {
            if (!SetProperty(ref _filtroAndamento, value))
                return;

            AplicarFiltro();
        }
    }

    public string AndamentoSelecionado
    {
        get => _andamentoSelecionado;
        set => SetProperty(ref _andamentoSelecionado, value);
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

    public DevolucoesPainel Painel
    {
        get => _painel;
        private set
        {
            if (!SetProperty(ref _painel, value))
                return;
            OnPropertyChanged(nameof(TextoTopLojas));
            OnPropertyChanged(nameof(TextoPorAndamento));
            OnPropertyChanged(nameof(TextoPorClasse));
            OnPropertyChanged(nameof(TextoPorMotivo));
        }
    }

    public string TextoTopLojas =>
        Painel.TopLojas.Count == 0 ? "Sem pendencias" : string.Join("  ·  ", Painel.TopLojas);

    public string TextoPorAndamento =>
        Painel.PorAndamento.Count == 0 ? "Sem pendencias" : string.Join("  ·  ", Painel.PorAndamento);

    public string TextoPorClasse =>
        Painel.PorClasse.Count == 0 ? "Sem itens na pre-nota" : string.Join("  ·  ", Painel.PorClasse);

    public string TextoPorMotivo =>
        Painel.PorMotivo.Count == 0 ? "Sem itens na pre-nota" : string.Join("  ·  ", Painel.PorMotivo);

    public ICommand AtualizarCommand { get; }
    public ICommand ExportarExcelCommand { get; }
    public ICommand AbrirItensCommand { get; }
    public ICommand MarcarDevolvidaCommand { get; }
    public ICommand MarcarAbsorvidoCommand { get; }
    public ICommand MarcarPerdeuPrazoCommand { get; }
    public ICommand SalvarObservacaoCommand { get; }
    public ICommand SalvarAndamentoCommand { get; }
    public ICommand SalvarPeriodoCommand { get; }
    public ICommand LimparPeriodoCommand { get; }

    private async Task CarregarDetalheSelecionadaAsync(DevolucaoItem? item)
    {
        ItensPreNota.Clear();
        HistoricoObservacoes.Clear();
        OnPropertyChanged(nameof(TextoItensPreNota));
        OnPropertyChanged(nameof(TextoHistoricoObservacoes));
        if (item is null)
            return;

        try
        {
            var itens = await _repository.ObterItensPreNotaAsync(item.Id);
            foreach (var linha in itens)
                ItensPreNota.Add(linha);
        }
        catch
        {
            // Fila antiga sem tabela ainda nao deve quebrar a tela.
        }

        try
        {
            var recados = await _repository.ObterObservacoesDevolucaoAsync(item.Id);
            foreach (var recado in recados)
                HistoricoObservacoes.Add(recado);
        }
        catch
        {
            // Tabela nova: tela continua sem historico.
        }

        OnPropertyChanged(nameof(TextoItensPreNota));
        OnPropertyChanged(nameof(TextoHistoricoObservacoes));
    }

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
            _itensPorDevolucao = (await _repository.ObterItensPreNotaPorDevolucoesAsync(
                _todas.Select(d => d.Id).ToList())).ToDictionary(p => p.Key, p => p.Value);
            AplicarFiltro();
            AtualizarPainel();

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
                      + "A fila so recebe o que for marcado Vermelho/pre-nota a partir dessa data. "
                      + "Notas Vermelho antigas da conferencia nao voltam a entrar.\n\n"
                      + (RemoverPendentesAnteriores
                          ? "Pendencias anteriores a essa data serao removidas da fila."
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
            "Concluidas" => query.Where(d => StatusDevolucaoValues.EstaConcluido(d.StatusDevolucao)),
            "Absorvidas" => query.Where(d => d.StatusDevolucao == StatusDevolucaoValues.Absorvido),
            _ => query
        };

        var andamentoFiltro = AndamentoDevolucaoValues.DeFiltro(FiltroAndamento);
        if (andamentoFiltro is not null)
            query = query.Where(d => d.Andamento == andamentoFiltro);

        var termo = FiltroTexto.Trim();
        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(d =>
                d.NumNota.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || d.NomeForn.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || d.CnpjForn.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || d.ApelidoLoja.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || d.AndamentoRotulo.Contains(termo, StringComparison.OrdinalIgnoreCase));
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

        ((AsyncRelayCommand)ExportarExcelCommand).RaiseCanExecuteChanged();
    }

    private void AtualizarPainel()
    {
        Painel = DevolucoesPainel.Calcular(_todas, _itensPorDevolucao);
    }

    private async Task ExportarExcelAsync()
    {
        if (Devolucoes.Count == 0)
        {
            MessageBox.Show("Nao ha notas no filtro atual para exportar.", "Exportar",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Exportar devolucoes",
            Filter = "Planilha Excel (*.xlsx)|*.xlsx",
            FileName = $"devolucoes_export_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
            DefaultExt = ".xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsCarregando = true;
            var notas = Devolucoes.ToList();
            var itens = _itensPorDevolucao;
            var caminho = dialog.FileName;
            await Task.Run(() => DevolucoesExportador.Exportar(caminho, notas, itens));
            Mensagem = $"Exportacao concluida: {Path.GetFileName(caminho)}";
            MessageBox.Show($"Arquivo exportado:\n{caminho}", "Exportacao concluida",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao exportar:\n{ex.Message}", "Exportar",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    public Task AbrirItensDaSelecionadaAsync() => AbrirItensAsync(marcarDevolvida: false);

    private async Task AbrirItensAsync(bool marcarDevolvida = false)
    {
        if (Selecionada is null)
            return;

        try
        {
            var nota = await _repository.ObterNotaPorIdAsync(Selecionada.NotaFiscalId);
            if (nota is null)
            {
                MessageBox.Show("Nota nao encontrada no cadastro.", "Devolucoes",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var temItens = ItensPreNota.Count > 0
                || (await _repository.ObterItensNotaAsync(nota.Id)).Count > 0
                || (await _repository.ObterItensPreNotaAsync(Selecionada.Id)).Count > 0;

            if (!temItens)
            {
                if (marcarDevolvida && Selecionada.PodeConcluir)
                {
                    await ConcluirAsync(StatusDevolucaoValues.Devolvida);
                    return;
                }

                MessageBox.Show(
                    "Esta nota nao tem itens gravados (fluxo antigo). Use Marcar devolvida para concluir sem pre-nota.",
                    "Devolucoes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var janela = new PreNotaWindow(
                _repository,
                reader: null,
                nota,
                PreNotaModo.ConfirmacaoFila,
                Selecionada)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                    ?? Application.Current.MainWindow
            };

            if (janela.ShowDialog() == true && janela.Confirmou)
            {
                Mensagem = $"Nota {Selecionada.NumNota}: itens confirmados e marcada como devolvida.";
                await CarregarAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao abrir os itens:\n{ex.Message}", "Devolucoes",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            await TentarRegistrarRecadoAsync();
            await _repository.AtualizarStatusDevolucaoAsync(
                Selecionada.Id,
                status);

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

        if (string.IsNullOrWhiteSpace(ObservacaoTexto))
        {
            MessageBox.Show("Digite um recado para gravar no historico.", "Devolucoes",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await _repository.AdicionarObservacaoDevolucaoAsync(Selecionada.Id, ObservacaoTexto);
            ObservacaoTexto = string.Empty;
            await CarregarDetalheSelecionadaAsync(Selecionada);
            if (Selecionada is not null)
                Selecionada.Observacao = DevolucaoObservacaoHistorico.Juntar(HistoricoObservacoes.ToList());
            Mensagem = $"Recado gravado na nota {Selecionada!.NumNota}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar observacao:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task TentarRegistrarRecadoAsync()
    {
        if (Selecionada is null || string.IsNullOrWhiteSpace(ObservacaoTexto))
            return;

        await _repository.AdicionarObservacaoDevolucaoAsync(Selecionada.Id, ObservacaoTexto);
        ObservacaoTexto = string.Empty;
    }

    private void SincronizarAndamentoCombo(DevolucaoItem? item)
    {
        AndamentoSelecionado = string.IsNullOrWhiteSpace(item?.Andamento)
            || !AndamentoDevolucaoValues.EhValido(item.Andamento)
            ? AndamentoDevolucaoValues.AguardandoLoja
            : item.Andamento;
    }

    private async Task SalvarAndamentoAsync()
    {
        if (Selecionada is null || !Selecionada.PodeConcluir)
            return;

        if (!AndamentoDevolucaoValues.EhValido(AndamentoSelecionado))
        {
            MessageBox.Show("Selecione um andamento.", "Devolucoes",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Selecionada.Andamento == AndamentoSelecionado)
        {
            Mensagem = $"Andamento da nota {Selecionada.NumNota} ja era {Selecionada.AndamentoRotulo}.";
            return;
        }

        try
        {
            await _repository.AtualizarAndamentoAsync(Selecionada.Id, AndamentoSelecionado);
            Selecionada.Andamento = AndamentoSelecionado;
            Mensagem = $"Andamento da nota {Selecionada.NumNota}: {Selecionada.AndamentoRotulo}.";
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar andamento:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
            SincronizarAndamentoCombo(Selecionada);
        }
    }
}
