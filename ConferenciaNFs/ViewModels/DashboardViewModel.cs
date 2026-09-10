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
    private DateTime? _dataSelecionada = DataCompraParser.DiaPadraoAbertura();
    private string? _dataCompraAtiva;
    private bool _herancaStatusImportacao;
    private int _totalPendencias;
    private int _totalNotasNovas;
    private int _totalNotas;

    public DashboardViewModel(NotaFiscalRepository repository, VsmSyncService? syncVsm = null)
    {
        _repository = repository;
        _syncVsm = syncVsm;
        Lojas = new ObservableCollection<LojaPendencia>();

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

        _ = InicializarAsync();
    }

    private bool PodeExportarTodas =>
        DataSelecionada.HasValue && !EstaVazio && !IsCarregando;

    private bool PodeRecalcularHerancaDia =>
        HerancaStatusImportacao && DataSelecionada.HasValue && !EstaVazio && !IsCarregando;

    public ObservableCollection<LojaPendencia> Lojas { get; }

    public DateTime? DataSelecionada
    {
        get => _dataSelecionada;
        set
        {
            if (!SetProperty(ref _dataSelecionada, value))
                return;

            OnPropertyChanged(nameof(DataSelecionadaTexto));
            OnPropertyChanged(nameof(TituloPagina));
            AtualizarComandosDoDia();
            _ = AbrirDiaAsync();
        }
    }

    public string DataSelecionadaTexto =>
        _dataSelecionada.HasValue
            ? DataCompraParser.Formatar(_dataSelecionada.Value)
            : "Nenhum dia selecionado";

    public string TituloPagina =>
        _dataSelecionada.HasValue
            ? $"Conferencia {DataSelecionadaTexto} - {_totalPendencias} com pendencias. Total de notas do dia {_totalNotasNovas} - Total de notas {_totalNotas}"
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

    private async Task InicializarAsync()
    {
        if (!DataSelecionada.HasValue)
        {
            _dataSelecionada = DataCompraParser.DiaPadraoAbertura();
            OnPropertyChanged(nameof(DataSelecionada));
            OnPropertyChanged(nameof(DataSelecionadaTexto));
            OnPropertyChanged(nameof(TituloPagina));
        }

        await CarregarConfiguracoesAsync();
        await AbrirDiaAsync();
        IniciarTimerSyncVsm();
    }

    private bool PodeSincronizarVsm =>
        SyncVsmDisponivel && DataSelecionada.HasValue && !IsCarregando && !_estaSincronizando;

    private void IniciarTimerSyncVsm()
    {
        if (_syncVsm is null || _timerSyncVsm is not null)
            return;

        _timerSyncVsm = new DispatcherTimer { Interval = IntervaloSyncVsm };
        _timerSyncVsm.Tick += OnTimerSyncVsm;
        _timerSyncVsm.Start();
    }

    private void OnTimerSyncVsm(object? sender, EventArgs e)
        => _ = AbrirDiaAsync();

    private async Task AbrirDiaAsync()
    {
        await SincronizarVsmCoreAsync(exibirErro: false);
        await CarregarLojasAsync();
    }

    private async Task SincronizarVsmManualAsync()
    {
        await SincronizarVsmCoreAsync(exibirErro: true);
        await CarregarLojasAsync();
    }

    private async Task SincronizarVsmCoreAsync(bool exibirErro)
    {
        if (_syncVsm is null || !DataSelecionada.HasValue || _estaSincronizando)
            return;

        try
        {
            _estaSincronizando = true;
            AtualizarComandosDoDia();
            MensagemStatus = $"Sincronizando notas do VSM em {DataSelecionadaTexto}...";

            var resultado = await _syncVsm.SincronizarDiaAsync(DataSelecionada.Value);
            var detalheIgnoradas = resultado.Ignoradas > 0
                ? $" · {resultado.Ignoradas} ignorada(s)"
                : string.Empty;
            var detalheRelocadas = resultado.Relocadas > 0
                ? $" · {resultado.Relocadas} realocada(s)"
                : string.Empty;
            MensagemStatus =
                $"VSM {DataSelecionadaTexto}: {resultado.Inseridas} nova(s), " +
                $"{resultado.Atualizadas} atualizada(s), {resultado.Lidas} lida(s)" +
                $"{detalheIgnoradas}{detalheRelocadas}.";
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

    private async Task CarregarLojasAsync()
    {
        try
        {
            IsCarregando = true;

            if (!DataSelecionada.HasValue)
            {
                Lojas.Clear();
                EstaVazio = true;
                _dataCompraAtiva = null;
                AplicarResumoDia(new ResumoDiaConferencia());
                MensagemStatus = "Selecione um dia no calendario para conferir.";
                return;
            }

            _dataCompraAtiva = await _repository.ResolverChaveDataCompraAsync(DataSelecionada.Value);
            MensagemStatus = $"Carregando lojas de {DataSelecionadaTexto}...";

            var lojas = await _repository.ObterPendenciasPorLojaAsync(_dataCompraAtiva);
            var resumo = await _repository.ObterResumoDiaAsync(_dataCompraAtiva);

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
        catch (Exception ex)
        {
            MensagemStatus = "Erro ao carregar dados.";
            MessageBox.Show($"Erro ao carregar pendencias:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
            AtualizarComandosDoDia();
        }
    }

    private async Task ImportarCsvAsync()
    {
        if (!DataSelecionada.HasValue)
        {
            MessageBox.Show(
                "Selecione o dia da conferencia no calendario antes de importar o CSV.",
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

            var diaConferencia = DataCompraParser.Formatar(DataSelecionada.Value);
            var inseridos = await _repository.ImportarCsvAsync(dialog.FileName, diaConferencia);
            MensagemStatus = $"{inseridos} nota(s) nova(s) importada(s) para {diaConferencia}.";

            MessageBox.Show(
                $"{inseridos} nota(s) nova(s) importada(s) para o dia {diaConferencia}.\nTodas as linhas do CSV ficam neste dia, independente da data de compra de cada nota.",
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

        if (string.IsNullOrWhiteSpace(_dataCompraAtiva))
        {
            MessageBox.Show("Selecione um dia antes de abrir a conferencia.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var janela = new ConferenciaWindow(_repository, loja.ApelidoLoja, _dataCompraAtiva, _syncVsm?.Reader)
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

        if (string.IsNullOrWhiteSpace(_dataCompraAtiva))
        {
            MessageBox.Show("Selecione um dia antes de recalcular.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirmar = MessageBox.Show(
            $"Recalcular status e observacao herdados das notas de {DataSelecionadaTexto}?\n\n" +
            "Verde/Azul no historico (loja + nota + CNPJ, ou chave NFe) viram Laranja.\n" +
            "Notas em branco com emissao anterior ao ultimo dia ja conferido tambem viram Laranja.\n" +
            "Notas conferidas hoje pela primeira vez permanecem como estao.\n" +
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

            var atualizadas = await _repository.RecalcularHerancaImportacaoDiaAsync(_dataCompraAtiva);

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
        if (string.IsNullOrWhiteSpace(_dataCompraAtiva))
        {
            MessageBox.Show("Selecione um dia antes de exportar.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsCarregando = true;
            MensagemStatus = "Preparando exportacao total...";
            ((AsyncRelayCommand)ExportarTodasCommand).RaiseCanExecuteChanged();

            var notas = await _repository.ObterTodasNotasPorDataAsync(_dataCompraAtiva);
            if (notas.Count == 0)
            {
                MessageBox.Show("Nao ha notas para exportar neste dia.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var nomeArquivo = $"{SanitizarNomeArquivo(_dataCompraAtiva)}_conferencia_todas_lojas.xlsx";
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

            await Task.Run(() => ConferenciaExportador.ExportarTodas(caminho, _dataCompraAtiva, notas));

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
