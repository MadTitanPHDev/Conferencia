using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.Views;
using Microsoft.Win32;

namespace ConferenciaNFs.ViewModels;

public sealed class DashboardViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan IntervaloSyncVsm = TimeSpan.FromMinutes(30);

    private readonly NotaFiscalRepository _repository;
    private readonly VsmSyncService? _syncVsm;
    private DispatcherTimer? _timerSyncVsm;
    private bool _isCarregando;
    private bool _estaSincronizando;
    private bool _estaVazio = true;
    private string _mensagemStatus = "Importe um CSV ou sincronize o VSM para comecar.";
    private DateTime? _dataInicio;
    private DateTime? _dataFim;
    private IReadOnlyList<string> _diasAtivos = [];
    private CancellationTokenSource? _ctsCarregamento;
    private bool _suspenderCarregamento;
    private bool _herancaStatusImportacao;
    private int _totalPendencias;
    private int _totalNotasNovas;
    private int _totalNotas;

    public DashboardViewModel(NotaFiscalRepository repository, VsmSyncService? syncVsm = null)
    {
        _repository = repository;
        _syncVsm = syncVsm;
        Lojas = new ObservableCollection<LojaPendencia>();

        (_dataInicio, _dataFim) = IntervaloDeAbertura();

        ImportarCsvCommand = new AsyncRelayCommand(_ => ImportarCsvAsync());
        SincronizarVsmCommand = new AsyncRelayCommand(_ => SincronizarVsmManualAsync(), _ => PodeSincronizarVsm);
        AtualizarCommand = new AsyncRelayCommand(_ => InicializarAsync());
        AbrirConferenciaCommand = new RelayCommand(AbrirConferencia, param => param is LojaPendencia);
        GerenciarLojasCommand = new RelayCommand(GerenciarLojas);
        GerenciarDistribuidorasCommand = new RelayCommand(GerenciarDistribuidoras);
        DevolucoesCommand = new RelayCommand(AbrirDevolucoes);
        PesquisarNotaCommand = new RelayCommand(PesquisarNota);
        PesquisarProdutoCommand = new RelayCommand(PesquisarProduto);
        ExportarTodasCommand = new AsyncRelayCommand(_ => ExportarTodasAsync(), _ => PodeExportarTodas);
        RecalcularHerancaDiaCommand = new AsyncRelayCommand(
            _ => RecalcularHerancaDiaAsync(),
            _ => PodeRecalcularHerancaDia);
        MigrarSqliteCommand = new AsyncRelayCommand(_ => MigrarSqliteAsync());
        PresetOntemCommand = new RelayCommand(_ => AplicarPresetOntem());
        PresetDesdeSabadoCommand = new RelayCommand(_ => AplicarPresetDesdeSabado());
        PresetEmAbertoCommand = new AsyncRelayCommand(_ => AplicarPresetEmAbertoAsync());

        _ = InicializarAsync();
    }

    private bool IntervaloPronto => DataCompraParser.IntervaloValido(DataInicio, DataFim);

    private bool PodeExportarTodas =>
        IntervaloPronto && !EstaVazio && !IsCarregando;

    private bool PodeRecalcularHerancaDia =>
        HerancaStatusImportacao && IntervaloPronto && !EstaVazio && !IsCarregando;

    public ObservableCollection<LojaPendencia> Lojas { get; }

    public DateTime? DataInicio
    {
        get => _dataInicio;
        set => AlterarIntervalo(value, _dataFim, inicioMudou: true);
    }

    public DateTime? DataFim
    {
        get => _dataFim;
        set => AlterarIntervalo(_dataInicio, value, inicioMudou: false);
    }

    public string DataSelecionadaTexto =>
        IntervaloPronto
            ? DataCompraParser.FormatarIntervalo(DataInicio!.Value, DataFim!.Value)
            : "Intervalo invalido";

    public string TituloPagina =>
        IntervaloPronto
            ? $"Conferencia {DataSelecionadaTexto} - {_totalPendencias} com pendencias. Total de notas novas {_totalNotasNovas} - Total de notas {_totalNotas}"
            : "Conferencia";

    public bool IsCarregando
    {
        get => _isCarregando;
        private set
        {
            if (!SetProperty(ref _isCarregando, value))
                return;

            AtualizarComandosDoDia();
        }
    }

    public bool EstaVazio
    {
        get => _estaVazio;
        private set
        {
            if (!SetProperty(ref _estaVazio, value))
                return;

            AtualizarComandosDoDia();
        }
    }

    public string MensagemStatus
    {
        get => _mensagemStatus;
        private set => SetProperty(ref _mensagemStatus, value);
    }

    public bool HerancaStatusImportacao
    {
        get => _herancaStatusImportacao;
        set
        {
            if (!SetProperty(ref _herancaStatusImportacao, value))
                return;

            AtualizarComandosDoDia();
            _ = SalvarHerancaStatusImportacaoAsync(value);
        }
    }

    public bool SyncVsmDisponivel => _syncVsm is not null;

    public ICommand ImportarCsvCommand { get; }
    public ICommand SincronizarVsmCommand { get; }
    public ICommand AtualizarCommand { get; }
    public ICommand AbrirConferenciaCommand { get; }
    public ICommand GerenciarLojasCommand { get; }
    public ICommand GerenciarDistribuidorasCommand { get; }
    public ICommand DevolucoesCommand { get; }
    public ICommand PesquisarNotaCommand { get; }
    public ICommand PesquisarProdutoCommand { get; }
    public ICommand ExportarTodasCommand { get; }
    public ICommand RecalcularHerancaDiaCommand { get; }
    public ICommand MigrarSqliteCommand { get; }
    public ICommand PresetOntemCommand { get; }
    public ICommand PresetDesdeSabadoCommand { get; }
    public ICommand PresetEmAbertoCommand { get; }

    /// <summary>
    /// Retoma o intervalo da sessao anterior, mas so enquanto ele ainda termina hoje.
    /// Virado o dia, o ponto de partida volta a ser o dia anterior.
    /// </summary>
    private static (DateTime inicio, DateTime fim) IntervaloDeAbertura()
    {
        var padrao = DataCompraParser.DiaPadraoAbertura();
        var dados = AppSettingsStore.Instance.Data;

        var inicio = DataCompraParser.TentarConverter(dados.UltimoIntervaloInicio);
        var fim = DataCompraParser.TentarConverter(dados.UltimoIntervaloFim);

        if (fim?.Date != DateTime.Today || !DataCompraParser.IntervaloValido(inicio, fim))
            return (padrao, padrao);

        return (inicio!.Value.Date, fim.Value.Date);
    }

    private void SalvarIntervaloEscolhido()
    {
        var dados = AppSettingsStore.Instance.Data;
        dados.UltimoIntervaloInicio = _dataInicio.HasValue
            ? DataCompraParser.Formatar(_dataInicio.Value)
            : string.Empty;
        dados.UltimoIntervaloFim = _dataFim.HasValue
            ? DataCompraParser.Formatar(_dataFim.Value)
            : string.Empty;

        AppSettingsStore.Instance.Salvar();
    }

    private void AplicarPresetOntem()
    {
        var ontem = DataCompraParser.DiaPadraoAbertura();
        DefinirIntervalo(ontem, ontem);
    }

    private void AplicarPresetDesdeSabado()
        => DefinirIntervalo(DataCompraParser.UltimoSabado(DateTime.Today), DateTime.Today);

    private async Task AplicarPresetEmAbertoAsync()
    {
        var hoje = DateTime.Today;
        var inicioJanela = hoje.AddDays(-(DataCompraParser.MaxDiasIntervaloPadrao - 1));

        var maisAntigo = await _repository.ObterDiaMaisAntigoComPendenciaAsync(inicioJanela, hoje);
        if (maisAntigo is null)
        {
            MensagemStatus =
                $"Nenhuma nota em aberto desde {DataCompraParser.Formatar(inicioJanela)}.";
            return;
        }

        DefinirIntervalo(maisAntigo.Value, hoje);
    }

    /// <summary>
    /// Troca as duas pontas de uma vez, para que um preset dispare uma unica recarga
    /// em vez das duas que sairiam ao mexer em "De" e depois em "Ate".
    /// </summary>
    private void DefinirIntervalo(DateTime inicio, DateTime fim)
    {
        var a = inicio.Date;
        var b = fim.Date < a ? a : fim.Date;

        var mudou = _dataInicio != a || _dataFim != b;
        _dataInicio = a;
        _dataFim = b;
        OnPropertyChanged(nameof(DataInicio));
        OnPropertyChanged(nameof(DataFim));
        OnPropertyChanged(nameof(DataSelecionadaTexto));
        OnPropertyChanged(nameof(TituloPagina));
        AtualizarComandosDoDia();

        if (_suspenderCarregamento)
            return;

        SalvarIntervaloEscolhido();

        // Mesmo sem mudanca de datas o preset recarrega: e o que o usuario espera do clique.
        _ = mudou ? AbrirDiaAsync() : ExecutarCargaAsync(exibirErroSync: false, apenasDiasQuentes: true);
    }

    private void AlterarIntervalo(DateTime? inicio, DateTime? fim, bool inicioMudou)
    {
        var a = inicio?.Date;
        var b = fim?.Date;
        if (a.HasValue && b.HasValue && b < a)
        {
            if (inicioMudou)
                b = a;
            else
                a = b;
        }

        var mudou = _dataInicio != a || _dataFim != b;
        _dataInicio = a;
        _dataFim = b;
        OnPropertyChanged(nameof(DataInicio));
        OnPropertyChanged(nameof(DataFim));
        OnPropertyChanged(nameof(DataSelecionadaTexto));
        OnPropertyChanged(nameof(TituloPagina));
        AtualizarComandosDoDia();

        if (!mudou || _suspenderCarregamento)
            return;

        SalvarIntervaloEscolhido();
        _ = AbrirDiaAsync();
    }

    private async Task InicializarAsync()
    {
        if (!DataInicio.HasValue || !DataFim.HasValue)
        {
            _suspenderCarregamento = true;
            var padrao = DataCompraParser.DiaPadraoAbertura();
            _dataInicio = padrao;
            _dataFim = padrao;
            OnPropertyChanged(nameof(DataInicio));
            OnPropertyChanged(nameof(DataFim));
            OnPropertyChanged(nameof(DataSelecionadaTexto));
            OnPropertyChanged(nameof(TituloPagina));
            _suspenderCarregamento = false;
        }

        await CarregarConfiguracoesAsync();
        await AbrirDiaAsync();
        IniciarTimerSyncVsm();
    }

    private bool PodeSincronizarVsm =>
        SyncVsmDisponivel && IntervaloPronto && !IsCarregando && !_estaSincronizando;

    private void IniciarTimerSyncVsm()
    {
        if (_syncVsm is null || _timerSyncVsm is not null)
            return;

        _timerSyncVsm = new DispatcherTimer { Interval = IntervaloSyncVsm };
        _timerSyncVsm.Tick += OnTimerSyncVsm;
        _timerSyncVsm.Start();
    }

    private void OnTimerSyncVsm(object? sender, EventArgs e)
        => _ = ExecutarCargaAsync(exibirErroSync: false, apenasDiasQuentes: true);

    private Task AbrirDiaAsync() => ExecutarCargaAsync(exibirErroSync: false);

    private Task SincronizarVsmManualAsync() => ExecutarCargaAsync(exibirErroSync: true);

    /// <summary>
    /// Sincroniza e recarrega o intervalo. Uma carga nova cancela a anterior para que
    /// a mais lenta nao sobrescreva a tela com o resultado de um intervalo ja trocado.
    /// </summary>
    private async Task ExecutarCargaAsync(bool exibirErroSync, bool apenasDiasQuentes = false)
    {
        using var cts = new CancellationTokenSource();
        CancelarCargaPendente();
        _ctsCarregamento = cts;

        try
        {
            await SincronizarVsmCoreAsync(exibirErroSync, apenasDiasQuentes, cts.Token);
            await CarregarLojasAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // O intervalo mudou durante a carga; a chamada mais recente assume.
        }
        finally
        {
            if (ReferenceEquals(_ctsCarregamento, cts))
                _ctsCarregamento = null;
        }
    }

    private void CancelarCargaPendente()
    {
        try
        {
            _ctsCarregamento?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // A carga anterior ja terminou por conta propria.
        }
    }

    private async Task SincronizarVsmCoreAsync(
        bool exibirErro,
        bool apenasDiasQuentes,
        CancellationToken cancellationToken)
    {
        if (_syncVsm is null || !IntervaloPronto || _estaSincronizando)
            return;

        var inicio = DataInicio!.Value;
        var fim = DataFim!.Value;

        if (apenasDiasQuentes)
        {
            // DATACOMPRA e a data em que a nota entrou no VSM, entao dias passados nao
            // recebem nota nova. A atualizacao automatica so reve hoje e ontem.
            var primeiroDiaQuente = DateTime.Today.AddDays(-1);
            if (inicio < primeiroDiaQuente)
                inicio = primeiroDiaQuente;

            if (fim < inicio)
                return;
        }

        var periodo = DataCompraParser.FormatarIntervalo(inicio, fim);

        try
        {
            _estaSincronizando = true;
            AtualizarComandosDoDia();
            MensagemStatus = $"Sincronizando notas do VSM em {periodo}...";

            var resultado = await _syncVsm.SincronizarIntervaloAsync(inicio, fim, cancellationToken);
            var detalheIgnoradas = resultado.Ignoradas > 0
                ? $" · {resultado.Ignoradas} ignorada(s)"
                : string.Empty;
            var detalheRelocadas = resultado.Relocadas > 0
                ? $" · {resultado.Relocadas} realocada(s)"
                : string.Empty;
            MensagemStatus =
                $"VSM {periodo}: {resultado.Inseridas} nova(s), " +
                $"{resultado.Atualizadas} atualizada(s), {resultado.Lidas} lida(s)" +
                $"{detalheIgnoradas}{detalheRelocadas}.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MensagemStatus = "Nao foi possivel sincronizar com o VSM.";
            if (exibirErro)
            {
                MessageBox.Show(
                    $"Erro ao sincronizar com o VSM:\n{ex.Message}\n\n" +
                    "O CSV continua disponivel como alternativa.",
                    "Sincronizacao VSM",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        finally
        {
            _estaSincronizando = false;
            AtualizarComandosDoDia();
        }
    }

    public void Dispose()
    {
        CancelarCargaPendente();

        if (_timerSyncVsm is null)
            return;

        _timerSyncVsm.Stop();
        _timerSyncVsm.Tick -= OnTimerSyncVsm;
        _timerSyncVsm = null;
    }

    private async Task CarregarConfiguracoesAsync()
    {
        try
        {
            _herancaStatusImportacao = await _repository.ObterHerancaStatusImportacaoAsync();
            OnPropertyChanged(nameof(HerancaStatusImportacao));
            AtualizarComandosDoDia();
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro ao carregar configuracoes.";
            MessageBox.Show($"Erro ao carregar configuracoes:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SalvarHerancaStatusImportacaoAsync(bool ativa)
    {
        try
        {
            await _repository.SalvarHerancaStatusImportacaoAsync(ativa);
            MensagemStatus = ativa
                ? "Heranca de status na importacao ativada."
                : "Heranca de status na importacao desativada.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar configuracao:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CarregarLojasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsCarregando = true;

            if (!IntervaloPronto)
            {
                Lojas.Clear();
                EstaVazio = true;
                _diasAtivos = [];
                AplicarResumoDia(new ResumoDiaConferencia());
                MensagemStatus = DataInicio.HasValue && DataFim.HasValue
                    ? $"Intervalo invalido. Maximo de {DataCompraParser.MaxDiasIntervaloPadrao} dia(s)."
                    : "Selecione a data inicial e a data final para conferir.";
                return;
            }

            var dias = await _repository.ResolverChavesIntervaloAsync(
                DataInicio!.Value,
                DataFim!.Value,
                cancellationToken);

            MensagemStatus = $"Carregando lojas de {DataSelecionadaTexto}...";

            var lojas = await _repository.ObterPendenciasPorLojaAsync(dias, cancellationToken);
            var resumo = await _repository.ObterResumoDiaAsync(dias, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            _diasAtivos = dias;
            Lojas.Clear();
            foreach (var loja in lojas)
                Lojas.Add(loja);

            EstaVazio = Lojas.Count == 0;
            AplicarResumoDia(resumo);
            MensagemStatus = EstaVazio
                ? $"Nenhuma loja com notas em {DataSelecionadaTexto}."
                : $"{Lojas.Count} loja(s) em {DataSelecionadaTexto}.";

            AtualizarComandosDoDia();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro ao carregar dados.";
            MessageBox.Show($"Erro ao carregar pendencias:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Uma carga cancelada nao libera a tela: quem a substituiu ainda esta rodando.
            if (!cancellationToken.IsCancellationRequested)
            {
                IsCarregando = false;
                AtualizarComandosDoDia();
            }
        }
    }

    private async Task ImportarCsvAsync()
    {
        if (!IntervaloPronto)
        {
            MessageBox.Show(
                "Selecione um intervalo valido (data inicial ate data final, no maximo " +
                $"{DataCompraParser.MaxDiasIntervaloPadrao} dias) antes de importar o CSV.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Selecione o arquivo CSV de notas fiscais",
            Filter = "Arquivos CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsCarregando = true;
            MensagemStatus = "Importando CSV...";

            var diaConferencia = DataCompraParser.Formatar(DataFim!.Value);
            var inseridos = await _repository.ImportarCsvAsync(dialog.FileName, diaConferencia);
            MensagemStatus = $"{inseridos} nota(s) nova(s) importada(s) para {diaConferencia}.";

            MessageBox.Show(
                $"{inseridos} nota(s) nova(s) importada(s) para o dia {diaConferencia}.\n" +
                "Todas as linhas do CSV ficam na data final do intervalo, independente da data de compra de cada nota.",
                "Importacao concluida",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await CarregarLojasAsync();
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro na importacao.";
            MessageBox.Show($"Erro ao importar CSV:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private void AbrirConferencia(object? parameter)
    {
        if (parameter is not LojaPendencia loja)
            return;

        if (_diasAtivos.Count == 0)
        {
            MessageBox.Show("Selecione um intervalo antes de abrir a conferencia.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var janela = new ConferenciaWindow(_repository, loja.ApelidoLoja, _diasAtivos, _syncVsm?.Reader)
        {
            Owner = Application.Current.MainWindow
        };

        janela.ShowDialog();
        _ = CarregarLojasAsync();
    }

    private async Task RecalcularHerancaDiaAsync()
    {
        if (!HerancaStatusImportacao)
        {
            MessageBox.Show(
                "Ative a heranca de status antes de recalcular.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_diasAtivos.Count == 0)
        {
            MessageBox.Show("Selecione um intervalo antes de recalcular.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirmar = MessageBox.Show(
            $"Recalcular status herdados das notas de {DataSelecionadaTexto}?\n\n" +
            "So herda quem ja foi conferido em outro dia (loja + nota + CNPJ, chave NFe ou CodCompra).\n" +
            "Verde/Azul no historico viram Laranja. Pendente no historico nao pinta nada.\n" +
            "Laranja sem conferencia anterior volta para branco.\n" +
            "Notas que o operador ja conferiu hoje (exceto Laranja automatico) permanecem.\n" +
            "Devolucoes nao serao alteradas.",
            "Recalcular heranca do dia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmar != MessageBoxResult.Yes)
            return;

        try
        {
            IsCarregando = true;
            MensagemStatus = "Recalculando status herdados...";

            var atualizadas = await _repository.RecalcularHerancaImportacaoIntervaloAsync(_diasAtivos);

            MensagemStatus = $"{atualizadas} nota(s) atualizada(s) em {DataSelecionadaTexto}.";
            MessageBox.Show(
                $"{atualizadas} nota(s) atualizada(s) com base no historico anterior.",
                "Recalculo concluido",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await CarregarLojasAsync();
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro ao recalcular heranca.";
            MessageBox.Show($"Erro ao recalcular:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private void AplicarResumoDia(ResumoDiaConferencia resumo)
    {
        _totalPendencias = resumo.Pendencias;
        _totalNotasNovas = resumo.NotasNovas;
        _totalNotas = resumo.TotalNotas;
        OnPropertyChanged(nameof(TituloPagina));
    }

    private void AtualizarComandosDoDia()
    {
        ((AsyncRelayCommand)ExportarTodasCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)RecalcularHerancaDiaCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)SincronizarVsmCommand).RaiseCanExecuteChanged();
    }

    private async Task ExportarTodasAsync()
    {
        if (_diasAtivos.Count == 0)
        {
            MessageBox.Show("Selecione um intervalo antes de exportar.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsCarregando = true;
            MensagemStatus = "Preparando exportacao total...";
            ((AsyncRelayCommand)ExportarTodasCommand).RaiseCanExecuteChanged();

            var notas = await _repository.ObterTodasNotasPorDataAsync(_diasAtivos);
            if (notas.Count == 0)
            {
                MessageBox.Show("Nao ha notas para exportar neste intervalo.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var nomeArquivo = $"{SanitizarNomeArquivo(DataSelecionadaTexto)}_conferencia_todas_lojas.xlsx";
            var dialog = new SaveFileDialog
            {
                Title = "Exportar conferencia de todas as lojas",
                Filter = "Planilha Excel (*.xlsx)|*.xlsx",
                FileName = nomeArquivo,
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            MensagemStatus = "Exportando todas as lojas...";
            var caminho = dialog.FileName;

            await Task.Run(() => ConferenciaExportador.ExportarTodas(caminho, DataSelecionadaTexto, notas));

            MensagemStatus = $"Exportacao total concluida: {Path.GetFileName(caminho)}";
            MessageBox.Show(
                $"{notas.Count} nota(s) de {Lojas.Count} loja(s) exportada(s):\n{caminho}",
                "Exportacao concluida",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro na exportacao total.";
            MessageBox.Show($"Erro ao exportar conferencia:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
            AtualizarComandosDoDia();
        }
    }

    private static string SanitizarNomeArquivo(string nome)
    {
        var sanitizado = Regex.Replace(nome.Trim(), @"[<>:""/\\|?*]", "_");
        return string.IsNullOrWhiteSpace(sanitizado) ? "dia" : sanitizado;
    }

    private void GerenciarLojas(object? parameter)
    {
        var janela = new GerenciarLojasWindow(_repository)
        {
            Owner = Application.Current.MainWindow
        };

        janela.ShowDialog();
        _ = CarregarLojasAsync();
    }

    private void GerenciarDistribuidoras(object? parameter)
    {
        var janela = new GerenciarDistribuidorasWindow(_repository)
        {
            Owner = Application.Current.MainWindow
        };

        janela.ShowDialog();
    }

    private void AbrirDevolucoes(object? parameter)
    {
        var janela = new DevolucoesWindow(_repository)
        {
            Owner = Application.Current.MainWindow
        };

        janela.ShowDialog();
    }

    private void PesquisarNota(object? parameter)
    {
        var janela = new PesquisarNotaWindow(_repository)
        {
            Owner = Application.Current.MainWindow
        };

        janela.ShowDialog();
    }

    private void PesquisarProduto(object? parameter)
    {
        if (_syncVsm is null)
        {
            MessageBox.Show(
                "A conexao com o VSM nao esta configurada.\nNao e possivel pesquisar produto por EAN.",
                "Pesquisar produto",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var janela = new PesquisarProdutoWindow(_repository, _syncVsm.Reader)
        {
            Owner = Application.Current.MainWindow
        };

        janela.ShowDialog();
    }

    private async Task MigrarSqliteAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecione o database.db antigo (SQLite)",
            Filter = "SQLite (*.db)|*.db|Todos os arquivos (*.*)|*.*",
            CheckFileExists = true,
            FileName = "database.db"
        };

        if (dialog.ShowDialog() != true)
            return;

        var confirmar = MessageBox.Show(
            "Isso copia lojas e notas do SQLite para o PostgreSQL.\n" +
            "Notas ja existentes (mesma chave) serao atualizadas com status/observacao do SQLite.\n\nContinuar?",
            "Migrar SQLite → PostgreSQL",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmar != MessageBoxResult.Yes)
            return;

        try
        {
            IsCarregando = true;
            MensagemStatus = "Migrando SQLite para PostgreSQL...";

            var resultado = await SqliteParaPostgresMigrator.MigrarAsync(
                dialog.FileName,
                _repository.ConnectionString);

            MensagemStatus =
                $"Migracao concluida: {resultado.NotasInseridas} nota(s) nova(s), " +
                $"{resultado.NotasAtualizadas} atualizada(s), {resultado.LojasInseridas} loja(s).";

            MessageBox.Show(
                $"Migracao concluida.\n\n" +
                $"Notas novas: {resultado.NotasInseridas}\n" +
                $"Notas atualizadas: {resultado.NotasAtualizadas}\n" +
                $"Lojas processadas: {resultado.LojasInseridas}",
                "Migracao concluida",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await CarregarLojasAsync();
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro na migracao SQLite → PostgreSQL.";
            MessageBox.Show($"Erro ao migrar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }
}
