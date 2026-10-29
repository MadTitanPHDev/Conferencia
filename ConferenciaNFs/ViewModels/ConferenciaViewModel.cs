using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.Views;
using Microsoft.Win32;

namespace ConferenciaNFs.ViewModels;

public sealed class ConferenciaViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private readonly VsmComprasReader? _vsmReader;
    private NotaFiscal? _notaSelecionada;
    private bool _isCarregando;
    private string _mensagemStatus = string.Empty;
    private string _observacaoTexto = string.Empty;
    private bool _modoSomenteSelecionadas;
    private bool _atualizandoFiltro;
    private bool _duploCliqueCopiaNumero;
    private string _textoBuscaNota = string.Empty;
    private long? _notaIdParaSelecionar;

    public ConferenciaViewModel(
        NotaFiscalRepository repository,
        string apelidoLoja,
        IReadOnlyList<string> diasConferencia,
        VsmComprasReader? vsmReader = null,
        long? notaIdParaSelecionar = null)
    {
        _repository = repository;
        _vsmReader = vsmReader;
        _notaIdParaSelecionar = notaIdParaSelecionar;
        ApelidoLoja = apelidoLoja;
        DiasConferencia = NormalizarDias(diasConferencia);
        DataCompra = FormatarPeriodo(DiasConferencia);
        Notas = new ObservableCollection<NotaFiscal>();
        NotasVisiveis = new ObservableCollection<NotaFiscal>();
        FiltrosStatus = new ObservableCollection<StatusFiltroOpcao>(
            StatusConferenciaValues.Todos.Select(status =>
                new StatusFiltroOpcao(
                    status,
                    ObterRotuloFiltro(status),
                    AplicarFiltroNotas)));

        DefinirPendenteCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Pendente));
        DefinirVerdeCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Verde));
        DefinirAmareloCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Amarelo));
        DefinirVermelhoCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Vermelho));
        DefinirLaranjaCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Laranja));
        DefinirAzulCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Azul));

        DefinirObservacaoPbmCommand = new AsyncRelayCommand(_ => AplicarObservacaoRapidaAsync(ObservacaoValues.Pbm));
        DefinirObservacaoUsoConsumoCommand = new AsyncRelayCommand(_ => AplicarObservacaoRapidaAsync(ObservacaoValues.UsoEConsumo));
        DefinirObservacaoConvenienciaCommand = new AsyncRelayCommand(_ => AplicarObservacaoRapidaAsync(ObservacaoValues.Conveniencia));
        DefinirObservacaoBonificacaoCommand = new AsyncRelayCommand(_ => AplicarObservacaoRapidaAsync(ObservacaoValues.Bonificacao));
        DefinirObservacaoEncomendaCommand = new AsyncRelayCommand(_ => AplicarObservacaoRapidaAsync(ObservacaoValues.Encomenda));
        SalvarObservacaoCommand = new AsyncRelayCommand(_ => SalvarObservacaoAsync(ObservacaoTexto));
        ExportarConferenciaCommand = new AsyncRelayCommand(_ => ExportarConferenciaAsync());
        SelecionarTodosFiltrosCommand = new RelayCommand(_ => DefinirTodosFiltros(true));
        LimparFiltrosCommand = new RelayCommand(_ => DefinirTodosFiltros(false));
        LimparBuscaNotaCommand = new RelayCommand(_ => TextoBuscaNota = string.Empty);
        CopiarNumeroNotaCommand = new RelayCommand(_ => CopiarNumeroNotaSelecionada(), _ => NotaSelecionada is not null);
        AbrirItensCommand = new RelayCommand(_ => AbrirItensNotaSelecionada(), _ => NotaSelecionada is not null);
        AlternarDuploCliqueCommand = new RelayCommand(_ => DuploCliqueCopiaNumero = !DuploCliqueCopiaNumero);

        _duploCliqueCopiaNumero = AppSettingsStore.Instance.Data.DuploCliqueCopiaNumero;

        _ = CarregarNotasAsync();
    }

    public Action? AoFechar { get; set; }

    public string ApelidoLoja { get; }
    public IReadOnlyList<string> DiasConferencia { get; }
    public string DataCompra { get; }

    public string TituloConferencia => $"Conferência - {ApelidoLoja} - {DataCompra}";

    public ObservableCollection<NotaFiscal> Notas { get; }
    public ObservableCollection<NotaFiscal> NotasVisiveis { get; }
    public ObservableCollection<StatusFiltroOpcao> FiltrosStatus { get; }

    public bool ModoSomenteSelecionadas
    {
        get => _modoSomenteSelecionadas;
        set
        {
            if (!SetProperty(ref _modoSomenteSelecionadas, value))
                return;

            AplicarFiltroNotas();
            OnPropertyChanged(nameof(TextoModoFiltro));
        }
    }

    public string TextoModoFiltro => ModoSomenteSelecionadas
        ? "Modo: somente status marcados"
        : "Modo: priorizar status marcados no topo";

    public string TextoBuscaNota
    {
        get => _textoBuscaNota;
        set
        {
            if (!SetProperty(ref _textoBuscaNota, value))
                return;

            AplicarFiltroNotas();
        }
    }

    public ICommand LimparBuscaNotaCommand { get; }

    public bool DuploCliqueCopiaNumero
    {
        get => _duploCliqueCopiaNumero;
        set
        {
            if (!SetProperty(ref _duploCliqueCopiaNumero, value))
                return;

            AppSettingsStore.Instance.Data.DuploCliqueCopiaNumero = value;
            AppSettingsStore.Instance.Salvar();
            OnPropertyChanged(nameof(TextoDuploClique));
            OnPropertyChanged(nameof(DicaDuploClique));
            MensagemStatus = value
                ? "Duplo clique copia o número. F12 alterna para abrir itens."
                : "Duplo clique abre os itens. F12 alterna para copiar.";
        }
    }

    public string TextoDuploClique => DuploCliqueCopiaNumero
        ? "Duplo clique: copiar"
        : "Duplo clique: itens";

    public string DicaDuploClique => DuploCliqueCopiaNumero
        ? "F12: duplo clique passa a abrir os itens"
        : "F12: duplo clique passa a copiar o número";

    public NotaFiscal? NotaSelecionada
    {
        get => _notaSelecionada;
        set
        {
            if (!SetProperty(ref _notaSelecionada, value))
                return;

            ObservacaoTexto = value?.Observacao ?? string.Empty;
            OnPropertyChanged(nameof(TextoPendenciaSelecionada));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string ObservacaoTexto
    {
        get => _observacaoTexto;
        set => SetProperty(ref _observacaoTexto, value);
    }

    public string TextoPendenciaSelecionada =>
        NotaSelecionada is null || string.IsNullOrWhiteSpace(NotaSelecionada.DestinoPendencia)
            ? string.Empty
            : string.IsNullOrWhiteSpace(NotaSelecionada.EnviadoPorPendencia)
                ? $"Enviada para {NotaSelecionada.DestinoPendencia}. {NotaSelecionada.ItensPendencia}"
                : $"Enviada por {NotaSelecionada.EnviadoPorPendencia} para {NotaSelecionada.DestinoPendencia}. {NotaSelecionada.ItensPendencia}";

    public bool IsCarregando
    {
        get => _isCarregando;
        private set => SetProperty(ref _isCarregando, value);
    }

    public string MensagemStatus
    {
        get => _mensagemStatus;
        private set => SetProperty(ref _mensagemStatus, value);
    }

    public ICommand DefinirPendenteCommand { get; }
    public ICommand DefinirVerdeCommand { get; }
    public ICommand DefinirAmareloCommand { get; }
    public ICommand DefinirVermelhoCommand { get; }
    public ICommand DefinirLaranjaCommand { get; }
    public ICommand DefinirAzulCommand { get; }

    public ICommand DefinirObservacaoPbmCommand { get; }
    public ICommand DefinirObservacaoUsoConsumoCommand { get; }
    public ICommand DefinirObservacaoConvenienciaCommand { get; }
    public ICommand DefinirObservacaoBonificacaoCommand { get; }
    public ICommand DefinirObservacaoEncomendaCommand { get; }
    public ICommand SalvarObservacaoCommand { get; }
    public ICommand ExportarConferenciaCommand { get; }
    public ICommand SelecionarTodosFiltrosCommand { get; }
    public ICommand LimparFiltrosCommand { get; }
    public ICommand CopiarNumeroNotaCommand { get; }
    public ICommand AbrirItensCommand { get; }
    public ICommand AlternarDuploCliqueCommand { get; }

    public void ExecutarAcaoDuploClique()
    {
        if (DuploCliqueCopiaNumero)
            CopiarNumeroNotaSelecionada();
        else
            AbrirItensNotaSelecionada();
    }

    public bool CopiarNumeroNotaSelecionada()
    {
        if (NotaSelecionada is null || string.IsNullOrWhiteSpace(NotaSelecionada.NumNota))
            return false;

        try
        {
            var numero = NotaSelecionada.NumNota.Trim();
            Clipboard.SetText(numero);
            MensagemStatus = $"Numero da nota {numero} copiado.";
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void AbrirItensNotaSelecionada()
    {
        if (NotaSelecionada is null)
        {
            MessageBox.Show("Selecione uma nota para ver os itens.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_vsmReader is null && NotaSelecionada.CodCompra is not int)
        {
            MessageBox.Show(
                "A conexao com o VSM nao esta configurada e esta nota nao tem foto de itens.",
                "Itens da nota",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (_vsmReader is not null && (NotaSelecionada.CodCompra is not int codCompra || codCompra <= 0))
        {
            MessageBox.Show(
                "Esta nota nao tem codigo do VSM.\nSincronize o dia para carregar os itens.",
                "Itens da nota",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var janela = new DetalheNotaWindow(_vsmReader, NotaSelecionada, _repository)
        {
            Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                ?? Application.Current.MainWindow
        };

        janela.ShowDialog();
    }

    private async Task CarregarNotasAsync()
    {
        try
        {
            IsCarregando = true;
            MensagemStatus = "Carregando notas...";

            var notas = await _repository.ObterNotasPorLojaAsync(ApelidoLoja, DiasConferencia);
            var pendencias = await _repository.ObterPendenciasAbertasPorNotasAsync(
                notas.Select(n => n.Id).ToList());
            foreach (var nota in notas)
            {
                if (!pendencias.TryGetValue(nota.Id, out var pendencia))
                    continue;
                nota.DestinoPendencia = pendencia.PessoaNome;
                nota.EnviadoPorPendencia = pendencia.EnviadoPorNome;
                nota.ItensPendencia = pendencia.ItensTexto;
            }

            Notas.Clear();
            foreach (var nota in notas)
                Notas.Add(nota);

            AplicarFiltroNotas();
            SelecionarNotaDaPesquisa();
            MensagemStatus = $"{NotasVisiveis.Count} nota(s) em {DataCompra}.";
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro ao carregar notas.";
            MessageBox.Show($"Erro ao carregar notas:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private void AplicarFiltroNotas()
    {
        if (_atualizandoFiltro)
            return;

        var selecionadaAntes = NotaSelecionada;
        var statusAtivos = FiltrosStatus
            .Where(f => f.EstaAtivo)
            .Select(f => f.Status)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        IEnumerable<NotaFiscal> query = Notas;

        if (!string.IsNullOrWhiteSpace(TextoBuscaNota))
            query = query.Where(n => NotaCorrespondeBusca(n, TextoBuscaNota));

        if (ModoSomenteSelecionadas)
        {
            query = query.Where(n => statusAtivos.Contains(n.StatusConferencia));
        }
        else if (statusAtivos.Count > 0 && statusAtivos.Count < FiltrosStatus.Count)
        {
            query = query
                .OrderBy(n => statusAtivos.Contains(n.StatusConferencia) ? 0 : 1)
                .ThenBy(n => Notas.IndexOf(n));
        }

        var lista = UnificacaoNotasConferencia.UnificarMesmoStatus(query.ToList()).ToList();
        foreach (var visivel in lista)
            PendenciaConferenciaLeitura.CopiarDestinoParaVisivel(visivel, Notas);

        NotasVisiveis.Clear();
        foreach (var nota in lista)
            NotasVisiveis.Add(nota);

        if (selecionadaAntes is not null && NotasVisiveis.Contains(selecionadaAntes))
            NotaSelecionada = selecionadaAntes;
        else if (selecionadaAntes is not null)
        {
            NotaSelecionada = NotasVisiveis.FirstOrDefault(n =>
                                 UnificacaoNotasConferencia.MesmaIdentidade(selecionadaAntes, n)
                                 && string.Equals(
                                     n.StatusConferencia,
                                     selecionadaAntes.StatusConferencia,
                                     StringComparison.OrdinalIgnoreCase))
                             ?? NotasVisiveis.FirstOrDefault();
        }
        else
            NotaSelecionada = NotasVisiveis.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(TextoBuscaNota))
        {
            MensagemStatus = NotasVisiveis.Count == 0
                ? $"Nenhuma nota com '{TextoBuscaNota.Trim()}' nesta loja."
                : $"{NotasVisiveis.Count} nota(s) com '{TextoBuscaNota.Trim()}' nesta loja.";
        }
    }

    private void SelecionarNotaDaPesquisa()
    {
        if (_notaIdParaSelecionar is not long idAlvo)
            return;

        var alvo = Notas.FirstOrDefault(n => n.Id == idAlvo);
        _notaIdParaSelecionar = null;
        if (alvo is null)
            return;

        if (!NotasVisiveis.Contains(alvo))
        {
            var visivelDoGrupo = NotasVisiveis.FirstOrDefault(n =>
                UnificacaoNotasConferencia.MesmaIdentidade(alvo, n)
                && string.Equals(
                    n.StatusConferencia,
                    alvo.StatusConferencia,
                    StringComparison.OrdinalIgnoreCase));
            if (visivelDoGrupo is not null)
                alvo = visivelDoGrupo;
            else
                NotasVisiveis.Insert(0, alvo);
        }

        NotaSelecionada = alvo;
        MensagemStatus = $"Nota {alvo.NumNota} aberta a partir da pesquisa.";
    }

    private static bool NotaCorrespondeBusca(NotaFiscal nota, string busca)
    {
        var termo = busca.Trim();
        if (termo.Length == 0)
            return true;

        if (nota.NumNota.Contains(termo, StringComparison.OrdinalIgnoreCase))
            return true;

        var notaNorm = NumNotaNormalizer.Normalizar(nota.NumNota);
        var termoNorm = NumNotaNormalizer.Normalizar(termo);
        return termoNorm.Length > 0
               && (notaNorm.Contains(termoNorm, StringComparison.OrdinalIgnoreCase)
                   || termoNorm.Contains(notaNorm, StringComparison.OrdinalIgnoreCase));
    }

    private void DefinirTodosFiltros(bool ativo)
    {
        _atualizandoFiltro = true;
        try
        {
            foreach (var filtro in FiltrosStatus)
                filtro.EstaAtivo = ativo;
        }
        finally
        {
            _atualizandoFiltro = false;
        }

        AplicarFiltroNotas();
    }

    private static string ObterRotuloFiltro(string status) => status switch
    {
        StatusConferenciaValues.Pendente => "Não conferido",
        StatusConferenciaValues.Verde => "Correta",
        StatusConferenciaValues.Amarelo => "Falar com compras",
        StatusConferenciaValues.Vermelho => "Devolver",
        StatusConferenciaValues.Laranja => "Já vista",
        StatusConferenciaValues.Azul => "Absorver na loja",
        _ => status
    };

    private async Task AlterarStatusAsync(string novoStatus)
    {
        if (NotaSelecionada is null)
        {
            MessageBox.Show("Selecione uma nota para alterar o status.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (NotaSelecionada.StatusConferencia == novoStatus)
            return;

        var statusAnterior = NotaSelecionada.StatusConferencia;
        try
        {
            if (novoStatus == StatusConferenciaValues.Vermelho)
            {
                if (!await ConfirmarPreNotaAsync())
                    return;
                NotaSelecionada.StatusConferencia = novoStatus;
                NotaSelecionada.OrigemStatus = OrigemStatusValues.Manual;
                var copiasVermelho = await ReplicarStatusNasCopiasAsync(NotaSelecionada, statusAnterior, novoStatus);
                MensagemStatus = MensagemStatusComCopias(
                    $"Nota {NotaSelecionada.NumNota}: {StatusConferenciaValues.ObterDescricao(novoStatus)} (pre-nota gravada).",
                    copiasVermelho);
                AplicarFiltroNotas();
                return;
            }

            if (novoStatus == StatusConferenciaValues.Amarelo)
            {
                if (!await AplicarAmareloAsync(statusAnterior))
                    return;
                return;
            }

            await _repository.AtualizarStatusConferenciaAsync(NotaSelecionada.Id, novoStatus);
            NotaSelecionada.StatusConferencia = novoStatus;
            NotaSelecionada.OrigemStatus = OrigemStatusValues.Manual;
            var copias = await ReplicarStatusNasCopiasAsync(NotaSelecionada, statusAnterior, novoStatus);
            MensagemStatus = MensagemStatusComCopias(
                $"Nota {NotaSelecionada.NumNota}: {StatusConferenciaValues.ObterDescricao(novoStatus)}.",
                copias);
            AplicarFiltroNotas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao atualizar status:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<bool> AplicarAmareloAsync(string statusAnterior)
    {
        if (NotaSelecionada is null)
            return false;

        var escolhaJanela = new EscolhaAmareloWindow
        {
            Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                ?? Application.Current.MainWindow
        };
        escolhaJanela.ShowDialog();
        var escolha = escolhaJanela.Escolha;

        if (!EscolhaAmareloEfeito.DevePintar(escolha))
            return false;

        if (EscolhaAmareloEfeito.DeveCriarPendencia(escolha))
        {
            if (AppSettingsStore.Instance.Data.PessoaIdNestePc <= 0)
            {
                MessageBox.Show(
                    "Escolha quem usa este PC no menu (☰) antes de enviar a divergencia.",
                    "Divergencia",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return false;
            }

            var janela = new DivergenciaPreNotaWindow(_repository, _vsmReader, NotaSelecionada)
            {
                Owner = escolhaJanela.Owner
            };
            if (janela.ShowDialog() != true || !janela.Confirmou)
                return false;

            NotaSelecionada.StatusConferencia = StatusConferenciaValues.Amarelo;
            NotaSelecionada.OrigemStatus = OrigemStatusValues.Manual;
            NotaSelecionada.DestinoPendencia = janela.PessoaDestino;
            NotaSelecionada.EnviadoPorPendencia = janela.EnviadoPorNome;
            NotaSelecionada.ItensPendencia = janela.ItensEnviados;
            OnPropertyChanged(nameof(TextoPendenciaSelecionada));
            var copiasEnviadas = await ReplicarStatusNasCopiasAsync(
                NotaSelecionada,
                statusAnterior,
                StatusConferenciaValues.Amarelo);
            var destino = string.IsNullOrWhiteSpace(janela.PessoaDestino)
                ? "o comprador"
                : janela.PessoaDestino;
            MensagemStatus = MensagemStatusComCopias(
                $"Nota {NotaSelecionada.NumNota}: amarelo, enviada para {destino}. Continua nesta lista.",
                copiasEnviadas);
            AplicarFiltroNotas();
            return true;
        }

        await _repository.DeixarAmareloSomenteConferenciaAsync(NotaSelecionada.Id);
        await TentarFotografarItensAsync();
        NotaSelecionada.StatusConferencia = StatusConferenciaValues.Amarelo;
        NotaSelecionada.OrigemStatus = OrigemStatusValues.Manual;
        NotaSelecionada.DestinoPendencia = string.Empty;
        NotaSelecionada.EnviadoPorPendencia = string.Empty;
        NotaSelecionada.ItensPendencia = string.Empty;
        OnPropertyChanged(nameof(TextoPendenciaSelecionada));
        var copias = await ReplicarStatusNasCopiasAsync(
            NotaSelecionada,
            statusAnterior,
            StatusConferenciaValues.Amarelo);
        MensagemStatus = MensagemStatusComCopias(
            $"Nota {NotaSelecionada.NumNota}: {StatusConferenciaValues.ObterDescricao(StatusConferenciaValues.Amarelo)}.",
            copias);
        AplicarFiltroNotas();
        return true;
    }

    private async Task<bool> ConfirmarPreNotaAsync()
    {
        if (NotaSelecionada is null)
            return false;

        var janela = new PreNotaWindow(_repository, _vsmReader, NotaSelecionada)
        {
            Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                ?? Application.Current.MainWindow
        };

        return janela.ShowDialog() == true && janela.Confirmou;
    }

    private async Task TentarFotografarItensAsync()
    {
        if (NotaSelecionada is null)
            return;

        try
        {
            await ItemNotaSnapshotServico.GarantirAsync(_repository, _vsmReader, NotaSelecionada);
        }
        catch
        {
            // Foto e melhor esforco: o Amarelo ja foi gravado.
        }
    }

    private async Task SalvarObservacaoAsync(string? observacao)
    {
        if (NotaSelecionada is null)
        {
            MessageBox.Show("Selecione uma nota para adicionar observacao.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var texto = (observacao ?? string.Empty).Trim();

        if (NotaSelecionada.Observacao == texto)
            return;

        try
        {
            await _repository.AtualizarObservacaoAsync(NotaSelecionada.Id, texto);
            NotaSelecionada.Observacao = texto;
            ObservacaoTexto = texto;
            var copias = await ReplicarObservacaoNasCopiasAsync(NotaSelecionada, texto);
            var baseMsg = string.IsNullOrEmpty(texto)
                ? $"Observacao removida da nota {NotaSelecionada.NumNota}."
                : $"Nota {NotaSelecionada.NumNota}: observacao \"{texto}\" salva.";
            MensagemStatus = MensagemStatusComCopias(baseMsg, copias);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar observacao:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AplicarObservacaoRapidaAsync(string observacao)
    {
        if (NotaSelecionada is null)
        {
            MessageBox.Show("Selecione uma nota para adicionar observacao.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var texto = observacao.Trim();
        var jaTemObservacao = NotaSelecionada.Observacao == texto;
        var statusAnterior = NotaSelecionada.StatusConferencia;
        var jaEstaVerde = statusAnterior == StatusConferenciaValues.Verde;

        if (jaTemObservacao && jaEstaVerde)
            return;

        try
        {
            if (!jaTemObservacao)
            {
                await _repository.AtualizarObservacaoAsync(NotaSelecionada.Id, texto);
                NotaSelecionada.Observacao = texto;
                ObservacaoTexto = texto;
                await ReplicarObservacaoNasCopiasAsync(NotaSelecionada, texto);
            }

            if (!jaEstaVerde)
            {
                await _repository.AtualizarStatusConferenciaAsync(
                    NotaSelecionada.Id,
                    StatusConferenciaValues.Verde);
                NotaSelecionada.StatusConferencia = StatusConferenciaValues.Verde;
                NotaSelecionada.OrigemStatus = OrigemStatusValues.Manual;
                await ReplicarStatusNasCopiasAsync(
                    NotaSelecionada,
                    statusAnterior,
                    StatusConferenciaValues.Verde);
            }

            MensagemStatus =
                $"Nota {NotaSelecionada.NumNota}: observacao \"{texto}\" e status Verde.";
            AplicarFiltroNotas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao aplicar observacao:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportarConferenciaAsync()
    {
        if (Notas.Count == 0)
        {
            MessageBox.Show("Nao ha notas para exportar.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var nomeArquivo = $"{SanitizarNomeArquivo(ApelidoLoja)}_{SanitizarNomeArquivo(DataCompra)}_conferencia.xlsx";
        var dialog = new SaveFileDialog
        {
            Title = "Exportar conferencia",
            Filter = "Planilha Excel (*.xlsx)|*.xlsx",
            FileName = nomeArquivo,
            DefaultExt = ".xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsCarregando = true;
            MensagemStatus = "Exportando conferencia...";

            var caminho = dialog.FileName;
            var notas = Notas.ToList();
            var loja = ApelidoLoja;

            await Task.Run(() => ConferenciaExportador.Exportar(caminho, loja, notas));

            MensagemStatus = $"Conferencia exportada: {Path.GetFileName(caminho)}";
            MessageBox.Show($"Arquivo exportado com sucesso:\n{caminho}", "Exportacao concluida",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro na exportacao.";
            MessageBox.Show($"Erro ao exportar conferencia:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private async Task<int> ReplicarStatusNasCopiasAsync(
        NotaFiscal origem,
        string statusAnterior,
        string novoStatus)
    {
        var copias = Notas
            .Where(n => n.Id != origem.Id
                        && UnificacaoNotasConferencia.MesmaIdentidade(origem, n)
                        && string.Equals(
                            n.StatusConferencia,
                            statusAnterior,
                            StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var copia in copias)
        {
            await _repository.AtualizarStatusConferenciaAsync(copia.Id, novoStatus);
            copia.StatusConferencia = novoStatus;
            copia.OrigemStatus = OrigemStatusValues.Manual;
        }

        return copias.Count;
    }

    private async Task<int> ReplicarObservacaoNasCopiasAsync(NotaFiscal origem, string texto)
    {
        var copias = UnificacaoNotasConferencia.CopiasComMesmoStatus(origem, Notas);
        foreach (var copia in copias)
        {
            await _repository.AtualizarObservacaoAsync(copia.Id, texto);
            copia.Observacao = texto;
            copia.OrigemStatus = OrigemStatusValues.Manual;
        }

        return copias.Count;
    }

    private static string MensagemStatusComCopias(string mensagem, int copias)
        => copias <= 0
            ? mensagem
            : $"{mensagem} {copias} copia(s) do intervalo atualizada(s).";

    private static IReadOnlyList<string> NormalizarDias(IReadOnlyList<string> dias)
    {
        var limpos = (dias ?? [])
            .Select(d => d?.Trim() ?? string.Empty)
            .Where(d => d.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (limpos.Count == 0)
            throw new ArgumentException("Informe ao menos um dia de conferencia.", nameof(dias));

        return limpos;
    }

    private static string FormatarPeriodo(IReadOnlyList<string> dias)
    {
        if (dias.Count == 1)
            return dias[0];

        var ordenados = dias
            .OrderBy(d => d, Comparer<string>.Create(DataCompraParser.CompararAscendente))
            .ToList();
        var inicio = DataCompraParser.TentarConverter(ordenados[0]);
        var fim = DataCompraParser.TentarConverter(ordenados[^1]);
        if (inicio.HasValue && fim.HasValue)
            return DataCompraParser.FormatarIntervalo(inicio.Value, fim.Value);

        return $"{ordenados[0]} a {ordenados[^1]}";
    }

    private static string SanitizarNomeArquivo(string nome)
    {
        var sanitizado = Regex.Replace(nome.Trim(), @"[<>:""/\\|?*]", "_");
        return string.IsNullOrWhiteSpace(sanitizado) ? "loja" : sanitizado;
    }
}
