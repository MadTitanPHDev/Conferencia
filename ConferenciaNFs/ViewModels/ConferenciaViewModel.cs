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

    public ConferenciaViewModel(
        NotaFiscalRepository repository,
        string apelidoLoja,
        IReadOnlyList<string> diasConferencia,
        VsmComprasReader? vsmReader = null)
    {
        _repository = repository;
        _vsmReader = vsmReader;
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
        CopiarNumeroNotaCommand = new RelayCommand(_ => CopiarNumeroNotaSelecionada(), _ => NotaSelecionada is not null);
        AbrirItensCommand = new RelayCommand(_ => AbrirItensNotaSelecionada(), _ => NotaSelecionada is not null);
        AlternarDuploCliqueCommand = new RelayCommand(_ => DuploCliqueCopiaNumero = !DuploCliqueCopiaNumero);

        _duploCliqueCopiaNumero = AppSettingsStore.Instance.Data.DuploCliqueCopiaNumero;

        _ = CarregarNotasAsync();
    }

    public string ApelidoLoja { get; }
    public IReadOnlyList<string> DiasConferencia { get; }
    public string DataCompra { get; }

    public string TituloConferencia => $"Conferencia - {ApelidoLoja} - {DataCompra}";

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
            OnPropertyChanged(nameof(TextoAtalhos));
            MensagemStatus = value
                ? "Duplo clique copia o numero da nota. F12 alterna para itens."
                : "Duplo clique abre os itens da nota. F12 alterna para copiar.";
        }
    }

    public string TextoDuploClique => DuploCliqueCopiaNumero
        ? "Duplo clique: copiar"
        : "Duplo clique: itens";

    public string DicaDuploClique => DuploCliqueCopiaNumero
        ? "F12: duplo clique passa a abrir os itens"
        : "F12: duplo clique passa a copiar o numero";

    public string TextoAtalhos => DuploCliqueCopiaNumero
        ? "Esc: fechar · Duplo clique: copiar numero · F12: abre itens · Ctrl+C / botao direito: copiar · Scroll: troca nota · 0 Branco · 1/F1 Verde · 2/F2 Amarelo · 3/F3 Vermelho · 4/F4 Laranja · 5/F8 Absorver"
        : "Esc: fechar · Duplo clique: itens · F12: copia numero · Ctrl+C / botao direito: copiar · Scroll: troca nota · 0 Branco · 1/F1 Verde · 2/F2 Amarelo · 3/F3 Vermelho · 4/F4 Laranja · 5/F8 Absorver";

    public NotaFiscal? NotaSelecionada
    {
        get => _notaSelecionada;
        set
        {
            if (!SetProperty(ref _notaSelecionada, value))
                return;

            ObservacaoTexto = value?.Observacao ?? string.Empty;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string ObservacaoTexto
    {
        get => _observacaoTexto;
        set => SetProperty(ref _observacaoTexto, value);
    }

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

        if (_vsmReader is null)
        {
            MessageBox.Show(
                "A conexao com o VSM nao esta configurada.\nNao e possivel carregar os itens da nota.",
                "Itens da nota",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (NotaSelecionada.CodCompra is not int codCompra || codCompra <= 0)
        {
            MessageBox.Show(
                "Esta nota nao tem codigo do VSM.\nSincronize o dia para carregar os itens.",
                "Itens da nota",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var janela = new DetalheNotaWindow(_vsmReader, NotaSelecionada)
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

            Notas.Clear();
            foreach (var nota in notas)
                Notas.Add(nota);

            AplicarFiltroNotas();
            MensagemStatus = $"{Notas.Count} nota(s) carregada(s) em {DataCompra}.";
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

        var lista = query.ToList();
        NotasVisiveis.Clear();
        foreach (var nota in lista)
            NotasVisiveis.Add(nota);

        if (selecionadaAntes is not null && NotasVisiveis.Contains(selecionadaAntes))
            NotaSelecionada = selecionadaAntes;
        else
            NotaSelecionada = NotasVisiveis.FirstOrDefault();
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
        StatusConferenciaValues.Pendente => "Pendente",
        StatusConferenciaValues.Verde => "Correta",
        StatusConferenciaValues.Amarelo => "Advertencia",
        StatusConferenciaValues.Vermelho => "Devolvida",
        StatusConferenciaValues.Laranja => "Outro dia",
        StatusConferenciaValues.Azul => "Absorvida",
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

        try
        {
            await _repository.AtualizarStatusConferenciaAsync(NotaSelecionada.Id, novoStatus);
            NotaSelecionada.StatusConferencia = novoStatus;
            MensagemStatus = novoStatus == StatusConferenciaValues.Vermelho
                ? $"Nota {NotaSelecionada.NumNota}: {StatusConferenciaValues.ObterDescricao(novoStatus)} (entrou na fila de devolucoes)."
                : $"Nota {NotaSelecionada.NumNota}: {StatusConferenciaValues.ObterDescricao(novoStatus)}.";
            AplicarFiltroNotas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao atualizar status:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
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
            MensagemStatus = string.IsNullOrEmpty(texto)
                ? $"Observacao removida da nota {NotaSelecionada.NumNota}."
                : $"Nota {NotaSelecionada.NumNota}: observacao \"{texto}\" salva.";
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
        var jaEstaVerde = NotaSelecionada.StatusConferencia == StatusConferenciaValues.Verde;

        if (jaTemObservacao && jaEstaVerde)
            return;

        try
        {
            if (!jaTemObservacao)
            {
                await _repository.AtualizarObservacaoAsync(NotaSelecionada.Id, texto);
                NotaSelecionada.Observacao = texto;
                ObservacaoTexto = texto;
            }

            if (!jaEstaVerde)
            {
                await _repository.AtualizarStatusConferenciaAsync(
                    NotaSelecionada.Id,
                    StatusConferenciaValues.Verde);
                NotaSelecionada.StatusConferencia = StatusConferenciaValues.Verde;
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
