using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class PreNotaViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private readonly VsmComprasReader? _reader;
    private readonly NotaFiscal _nota;
    private readonly PreNotaModo _modo;
    private readonly DevolucaoItem? _devolucao;
    private bool _isCarregando;
    private string _mensagem = "Carregando itens...";
    private bool _confirmou;
    private string _motivoLote = string.Empty;
    private string _classeLote = string.Empty;

    public PreNotaViewModel(
        NotaFiscalRepository repository,
        VsmComprasReader? reader,
        NotaFiscal nota,
        PreNotaModo modo = PreNotaModo.Criacao,
        DevolucaoItem? devolucao = null)
    {
        _repository = repository;
        _reader = reader;
        _nota = nota;
        _modo = modo;
        _devolucao = devolucao;
        Linhas = new ObservableCollection<PreNotaLinha>();
        Motivos = new ObservableCollection<string>();
        Classes = new ObservableCollection<string>(ClasseDevolucaoValues.Todas);
        ConfirmarCommand = new AsyncRelayCommand(
            _ => ConfirmarAsync(),
            _ => PodeEditar && !IsCarregando && Linhas.Count > 0);
        AplicarMotivoClasseMarcadosCommand = new RelayCommand(
            _ => AplicarMotivoClasseMarcados(),
            _ => PodeEditar && !IsCarregando);
        _ = CarregarAsync();
    }

    public ObservableCollection<PreNotaLinha> Linhas { get; }
    public ObservableCollection<string> Motivos { get; }
    public ObservableCollection<string> Classes { get; }
    public ICommand ConfirmarCommand { get; }
    public ICommand AplicarMotivoClasseMarcadosCommand { get; }

    public bool Confirmou => _confirmou;

    public bool PodeEditar =>
        _modo == PreNotaModo.Criacao
        || (_modo == PreNotaModo.ConfirmacaoFila && _devolucao?.PodeConcluir == true);

    public string Titulo => _modo == PreNotaModo.ConfirmacaoFila
        ? $"Confirmar itens · {_nota.NumNota} · {_nota.ApelidoLoja}"
        : $"Pré-nota · {_nota.NumNota} · {_nota.ApelidoLoja}";

    public string TextoBotaoConfirmar => _modo == PreNotaModo.ConfirmacaoFila
        ? "Confirmar itens e marcar devolvida ao dist."
        : "Confirmar e devolver";

    public string TextoCancelar => PodeEditar ? "Cancelar" : "Fechar";

    public string MotivoLote
    {
        get => _motivoLote;
        set => SetProperty(ref _motivoLote, value ?? string.Empty);
    }

    public string ClasseLote
    {
        get => _classeLote;
        set => SetProperty(ref _classeLote, value ?? string.Empty);
    }

    public string TextoResumo
    {
        get
        {
            var marcados = Linhas.Where(l => l.Selecionado).ToList();
            var quantidade = marcados.Sum(l => l.QuantidadeDevolver);
            var valor = marcados.Sum(l => l.QuantidadeDevolver * l.CustoNota);
            return $"{marcados.Count} marcado(s) · {quantidade:N3} un. · R$ {valor:N2}";
        }
    }

    public bool IsCarregando
    {
        get => _isCarregando;
        private set
        {
            if (!SetProperty(ref _isCarregando, value))
                return;
            ((AsyncRelayCommand)ConfirmarCommand).RaiseCanExecuteChanged();
        }
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    public Action? Fechar { get; set; }

    private async Task CarregarAsync()
    {
        try
        {
            IsCarregando = true;
            var motivos = await _repository.ListarMotivosDevolucaoAsync(somenteAtivos: true);
            Motivos.Clear();
            foreach (var motivo in motivos)
                Motivos.Add(motivo.Descricao);

            if (Motivos.Count > 0 && string.IsNullOrWhiteSpace(MotivoLote))
                MotivoLote = Motivos[0];
            if (Classes.Count > 0 && string.IsNullOrWhiteSpace(ClasseLote))
                ClasseLote = Classes[0];

            IReadOnlyList<ItemNotaSnapshot> itens =
                await ItemNotaSnapshotServico.GarantirAsync(_repository, _reader, _nota);
            var preNota = _devolucao is null
                ? []
                : await _repository.ObterItensPreNotaAsync(_devolucao.Id);
            var porSequencia = preNota.ToDictionary(i => i.Sequencia);

            if (itens.Count == 0 && preNota.Count > 0)
            {
                itens = preNota.Select(i => new ItemNotaSnapshot
                {
                    NotaFiscalId = _nota.Id,
                    Sequencia = i.Sequencia,
                    CodProd = i.CodProd,
                    NomeProd = i.NomeProd,
                    BarrasEan = i.BarrasEan,
                    QuantItemCompra = i.Quantidade,
                    CustoNota = i.CustoNota
                }).ToList();
            }

            LimparLinhas();
            foreach (var item in itens)
            {
                var linha = new PreNotaLinha(item);
                if (porSequencia.TryGetValue(item.Sequencia, out var marcado))
                {
                    linha.Selecionado = true;
                    linha.QuantidadeDevolver = marcado.Quantidade;
                    linha.Motivo = marcado.Motivo;
                    linha.Classe = marcado.Classe;
                }

                linha.PropertyChanged += OnLinhaPropertyChanged;
                Linhas.Add(linha);
            }

            ((AsyncRelayCommand)ConfirmarCommand).RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(TextoResumo));

            if (Linhas.Count == 0)
            {
                Mensagem = "Nao ha itens desta nota. Abra os itens com o VSM no ar ou sincronize o dia.";
                return;
            }

            if (!PodeEditar)
            {
                Mensagem = "Itens ja confirmados nesta devolucao (somente leitura).";
                return;
            }

            Mensagem = _modo == PreNotaModo.ConfirmacaoFila
                ? "Confira de novo os produtos, quantidades, motivo e classe. Confirmar marca a nota como devolvida."
                : Motivos.Count == 0
                    ? "Cadastre um motivo em Motivos de devolucao antes de confirmar."
                    : "Marque os produtos e a quantidade. Depois aplique motivo e classe aos marcados.";
        }
        catch (Exception ex)
        {
            Mensagem = "Nao foi possivel carregar os itens.";
            MessageBox.Show($"Erro ao preparar a pre-nota:\n{ex.Message}", "Devolucao",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private void LimparLinhas()
    {
        foreach (var linha in Linhas)
            linha.PropertyChanged -= OnLinhaPropertyChanged;
        Linhas.Clear();
    }

    private void OnLinhaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PreNotaLinha.Selecionado)
            or nameof(PreNotaLinha.QuantidadeDevolver))
            OnPropertyChanged(nameof(TextoResumo));
    }

    private void AplicarMotivoClasseMarcados()
    {
        foreach (var linha in Linhas.Where(l => l.Selecionado))
        {
            linha.Motivo = MotivoLote;
            linha.Classe = ClasseLote;
        }
    }

    private async Task ConfirmarAsync()
    {
        var erro = PreNotaValidador.Validar(Linhas);
        if (erro is not null)
        {
            MessageBox.Show(erro, "Pre-nota", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsCarregando = true;
            var escolhidos = Linhas.Where(l => l.Selecionado).Select(l => l.ParaGravacao()).ToList();
            if (_modo == PreNotaModo.ConfirmacaoFila)
            {
                if (_devolucao is null)
                    throw new InvalidOperationException("Devolucao nao informada.");

                await _repository.ConfirmarDevolucaoComItensAsync(
                    _devolucao.Id,
                    _nota.Id,
                    escolhidos);
            }
            else
            {
                await _repository.ConfirmarPreNotaAsync(_nota.Id, escolhidos);
            }

            _confirmou = true;
            Fechar?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gravar a pre-nota:\n{ex.Message}", "Devolucao",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }
}
