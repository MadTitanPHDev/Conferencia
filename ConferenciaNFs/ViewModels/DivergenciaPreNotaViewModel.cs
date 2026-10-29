using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class DivergenciaPreNotaViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private readonly VsmComprasReader? _reader;
    private readonly NotaFiscal _nota;
    private readonly long? _pendenciaId;
    private bool _podeEditar = true;
    private Pessoa? _pessoaSelecionada;
    private string _observacao = string.Empty;
    private string _filtroTexto = string.Empty;
    private string _mensagem = "Carregando itens...";
    private bool _isCarregando;
    private bool _confirmou;

    public DivergenciaPreNotaViewModel(
        NotaFiscalRepository repository,
        VsmComprasReader? reader,
        NotaFiscal nota,
        long? pendenciaId = null)
    {
        _repository = repository;
        _reader = reader;
        _nota = nota;
        _pendenciaId = pendenciaId;
        Linhas = new ObservableCollection<DivergenciaLinha>();
        LinhasView = CollectionViewSource.GetDefaultView(Linhas);
        LinhasView.Filter = PassaFiltro;
        Pessoas = new ObservableCollection<Pessoa>();
        ConfirmarCommand = new AsyncRelayCommand(_ => ConfirmarAsync(), _ => PodeConfirmar);
        FicarCommand = new AsyncRelayCommand(_ => ResponderAsync(ficar: true), _ => PodeResponder);
        DevolverCommand = new AsyncRelayCommand(_ => ResponderAsync(ficar: false), _ => PodeResponder);
        AlternarSelecaoTodosCommand = new RelayCommand(_ => AlternarSelecaoTodos(), _ => LinhasVisiveis.Any());
        LimparFiltroCommand = new RelayCommand(_ => FiltroTexto = string.Empty, _ => TemFiltro);
        CopiarEanCommand = new RelayCommand(p => CopiarEan(p as DivergenciaLinha), p => p is DivergenciaLinha);
        _ = CarregarAsync();
    }

    public ObservableCollection<DivergenciaLinha> Linhas { get; }
    public ICollectionView LinhasView { get; }
    public ObservableCollection<Pessoa> Pessoas { get; }
    public ICommand ConfirmarCommand { get; }
    public ICommand FicarCommand { get; }
    public ICommand DevolverCommand { get; }
    public ICommand AlternarSelecaoTodosCommand { get; }
    public ICommand LimparFiltroCommand { get; }
    public ICommand CopiarEanCommand { get; }

    public DivergenciaLinha? LinhaSelecionada { get; set; }

    public bool Confirmou => _confirmou;
    public string PessoaDestino { get; private set; } = string.Empty;
    public string ItensEnviados { get; private set; } = string.Empty;
    public Action? Fechar { get; set; }

    public string Titulo => $"Divergência · {_nota.NumNota} · {_nota.ApelidoLoja}";

    public bool PodeEditar
    {
        get => _podeEditar;
        private set
        {
            if (!SetProperty(ref _podeEditar, value))
                return;
            OnPropertyChanged(nameof(TextoBotaoConfirmar));
            OnPropertyChanged(nameof(TextoCancelar));
            OnPropertyChanged(nameof(MostrarResposta));
            ((AsyncRelayCommand)ConfirmarCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)FicarCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)DevolverCommand).RaiseCanExecuteChanged();
        }
    }

    public string TextoBotaoConfirmar => _pendenciaId.HasValue
        ? "Salvar"
        : "Enviar e deixar amarelo";

    public bool MostrarResposta => _pendenciaId.HasValue && PodeEditar;

    public string MensagemResultado { get; private set; } = string.Empty;

    public string TextoRemetente { get; private set; } = string.Empty;

    public string EnviadoPorNome { get; private set; } = string.Empty;

    public string TextoCancelar => PodeEditar ? "Cancelar" : "Fechar";

    private bool PodeConfirmar => PodeEditar && !IsCarregando && Linhas.Count > 0 && PessoaSelecionada is not null;

    private bool PodeResponder =>
        PodeEditar
        && _pendenciaId.HasValue
        && !IsCarregando
        && Linhas.Count > 0
        && PessoaSelecionada is not null;

    public Pessoa? PessoaSelecionada
    {
        get => _pessoaSelecionada;
        set
        {
            if (!SetProperty(ref _pessoaSelecionada, value))
                return;
            ((AsyncRelayCommand)ConfirmarCommand).RaiseCanExecuteChanged();
        }
    }

    public string Observacao
    {
        get => _observacao;
        set => SetProperty(ref _observacao, value ?? string.Empty);
    }

    public string FiltroTexto
    {
        get => _filtroTexto;
        set
        {
            if (!SetProperty(ref _filtroTexto, value ?? string.Empty))
                return;
            LinhasView.Refresh();
            NotificarFiltro();
        }
    }

    public bool TemFiltro => !string.IsNullOrWhiteSpace(FiltroTexto);

    public string TextoFiltroItens
    {
        get
        {
            var visiveis = LinhasVisiveis.Count();
            if (!TemFiltro)
                return visiveis == 1 ? "1 item" : $"{visiveis} itens";
            return visiveis == 0
                ? $"Nenhum item com \"{FiltroTexto.Trim()}\""
                : $"{visiveis} de {Linhas.Count}";
        }
    }

    public string TextoResumo
    {
        get
        {
            var marcados = Linhas.Where(l => l.Selecionado).ToList();
            var quantidade = marcados.Sum(l => l.Quantidade);
            var valor = marcados.Sum(l => l.TotalItem);
            return $"{marcados.Count} marcado(s) · {quantidade:N3} un. · R$ {valor:N2}";
        }
    }

    public string TextoSelecionarTodos =>
        LinhasVisiveis.Any() && LinhasVisiveis.All(l => l.Selecionado)
            ? "Desmarcar todos"
            : "Selecionar todos";

    public bool IsCarregando
    {
        get => _isCarregando;
        private set
        {
            if (!SetProperty(ref _isCarregando, value))
                return;
            ((AsyncRelayCommand)ConfirmarCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)FicarCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)DevolverCommand).RaiseCanExecuteChanged();
        }
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    private IEnumerable<DivergenciaLinha> LinhasVisiveis => Linhas.Where(LinhaPassaFiltro);

    private async Task CarregarAsync()
    {
        try
        {
            IsCarregando = true;
            PendenciaCompraResumo? gravada = _pendenciaId is long id
                ? await _repository.ObterPendenciaAsync(id)
                : null;
            PodeEditar = gravada is null || PendenciaConferenciaLeitura.ApareceNaLista(gravada.StatusPendencia);

            var pessoas = await _repository.ListarPessoasAsync(somenteAtivos: true);
            Pessoas.Clear();
            foreach (var pessoa in pessoas)
                Pessoas.Add(pessoa);
            if (gravada is { PessoaId: > 0 }
                && Pessoas.All(p => p.Id != gravada.PessoaId))
            {
                Pessoas.Insert(0, new Pessoa
                {
                    Id = gravada.PessoaId,
                    Nome = gravada.PessoaNome,
                    Ativo = false
                });
            }

            var itens = await ItemNotaSnapshotServico.GarantirAsync(_repository, _reader, _nota);
            if (itens.Count == 0 && gravada is { Itens.Count: > 0 })
            {
                itens = gravada.Itens.Select(i => new ItemNotaSnapshot
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

            var porSequencia = gravada?.Itens.ToDictionary(i => i.Sequencia)
                ?? new Dictionary<int, PendenciaCompraItem>();

            foreach (var linha in Linhas)
                linha.PropertyChanged -= OnLinhaPropertyChanged;
            Linhas.Clear();
            foreach (var item in itens)
            {
                var linha = new DivergenciaLinha(item);
                if (porSequencia.TryGetValue(item.Sequencia, out var marcado))
                {
                    linha.Selecionado = true;
                    linha.Quantidade = marcado.Quantidade;
                }

                linha.PropertyChanged += OnLinhaPropertyChanged;
                Linhas.Add(linha);
            }

            if (gravada is not null)
            {
                Observacao = gravada.Observacao;
                PessoaSelecionada = Pessoas.FirstOrDefault(p => p.Id == gravada.PessoaId);
                EnviadoPorNome = gravada.EnviadoPorNome;
                TextoRemetente = string.IsNullOrWhiteSpace(gravada.EnviadoPorNome)
                    ? string.Empty
                    : $"Enviado por {gravada.EnviadoPorNome}";
            }
            else
            {
                var remetente = Pessoas.FirstOrDefault(p => p.Id == AppSettingsStore.Instance.Data.PessoaIdNestePc);
                EnviadoPorNome = remetente?.Nome ?? string.Empty;
                TextoRemetente = remetente is null
                    ? "Escolha quem usa este PC no menu antes de enviar."
                    : $"Voce esta enviando como {remetente.Nome}";
            }

            OnPropertyChanged(nameof(TextoRemetente));

            LinhasView.Refresh();
            OnPropertyChanged(nameof(TextoResumo));
            NotificarFiltro();

            if (!PodeEditar)
            {
                Mensagem = "Divergencia concluida. Itens em somente leitura.";
                return;
            }

            if (Pessoas.Count == 0)
            {
                Mensagem = "Cadastre uma pessoa em Pessoas antes de enviar.";
                return;
            }

            if (Linhas.Count == 0)
            {
                Mensagem = "Nao ha itens desta nota. Abra os itens com o VSM no ar ou sincronize o dia.";
                return;
            }

            Mensagem = _pendenciaId.HasValue
                ? "Itens ja enviados. Ajuste a pessoa, a quantidade ou a observacao e salve."
                : "Marque os produtos, a quantidade e a pessoa. A nota continua na conferencia, em amarelo.";
        }
        catch (Exception ex)
        {
            Mensagem = "Nao foi possivel carregar a divergencia.";
            MessageBox.Show($"Erro ao preparar a divergencia:\n{ex.Message}", "Divergencia",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private void OnLinhaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DivergenciaLinha.Selecionado) or nameof(DivergenciaLinha.Quantidade))
        {
            OnPropertyChanged(nameof(TextoResumo));
            NotificarFiltro();
        }
    }

    public bool CopiarEanDaSelecionada() => CopiarEan(LinhaSelecionada);

    private bool CopiarEan(DivergenciaLinha? linha)
    {
        if (linha is null)
            return false;

        var ean = linha.EanParaCopiar;
        if (!EanNormalizer.EhValido(ean))
        {
            Mensagem = $"{linha.NomeProd} nao tem codigo de barras para copiar.";
            return false;
        }

        try
        {
            Clipboard.SetText(ean);
            Mensagem = $"Codigo {ean} copiado.";
            return true;
        }
        catch
        {
            Mensagem = "Nao foi possivel copiar o codigo de barras.";
            return false;
        }
    }

    private void AlternarSelecaoTodos()
    {
        var marcar = !LinhasVisiveis.All(l => l.Selecionado);
        foreach (var linha in LinhasVisiveis)
            linha.Selecionado = marcar;
        OnPropertyChanged(nameof(TextoResumo));
        NotificarFiltro();
    }

    private void NotificarFiltro()
    {
        OnPropertyChanged(nameof(TemFiltro));
        OnPropertyChanged(nameof(TextoFiltroItens));
        OnPropertyChanged(nameof(TextoSelecionarTodos));
        CommandManager.InvalidateRequerySuggested();
    }

    private bool PassaFiltro(object obj) => obj is DivergenciaLinha linha && LinhaPassaFiltro(linha);

    private bool LinhaPassaFiltro(DivergenciaLinha linha)
    {
        var termo = FiltroTexto.Trim();
        if (termo.Length == 0)
            return true;

        if (linha.NomeProd.Contains(termo, StringComparison.OrdinalIgnoreCase))
            return true;
        if (linha.CodProd.ToString().Contains(termo, StringComparison.OrdinalIgnoreCase))
            return true;

        var ean = EanNormalizer.Normalizar(linha.BarrasEan);
        var termoEan = EanNormalizer.Normalizar(termo);
        if (termoEan.Length > 0 && ean.Contains(termoEan, StringComparison.Ordinal))
            return true;

        return (linha.BarrasEan ?? string.Empty).Contains(termo, StringComparison.OrdinalIgnoreCase);
    }

    private async Task ResponderAsync(bool ficar)
    {
        if (_pendenciaId is not long pendenciaId)
            return;

        var confirmar = MessageBox.Show(
            ficar
                ? $"A nota {_nota.NumNota} volta para Verde, com a observacao desta divergencia.\n\nA pendencia fica concluida e nao e apagada."
                : $"A nota {_nota.NumNota} entra na fila de devolucoes com os itens marcados.\n\nA pendencia fica concluida como devolvida e nao e apagada.",
            ficar ? "Ficar com a divergencia" : "Devolver",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmar != MessageBoxResult.Yes)
            return;

        var pessoaId = PessoaSelecionada?.Id ?? 0;
        var erro = DivergenciaValidador.Validar(Linhas, pessoaId);
        if (erro is not null)
        {
            MessageBox.Show(erro, "Divergencia", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsCarregando = true;
            var escolhidos = Linhas.Where(l => l.Selecionado).Select(l => l.ParaGravacao()).ToList();
            await _repository.RegistrarPendenciaDivergenciaAsync(
                _nota.Id,
                pessoaId,
                Observacao,
                escolhidos);
            var status = await _repository.ResponderPendenciaAsync(pendenciaId, ficar);
            MensagemResultado = ficar
                ? $"Nota {_nota.NumNota} voltou para Verde."
                : status == StatusConferenciaValues.Laranja
                    ? $"A mesma compra ja estava na fila. A nota {_nota.NumNota} ficou como ja vista."
                    : $"Nota {_nota.NumNota} entrou na fila de devolucoes.";
            _confirmou = true;
            Fechar?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao responder a divergencia:\n{ex.Message}", "Divergencia",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private async Task ConfirmarAsync()
    {
        if (!PodeEditar)
            return;

        var pessoaId = PessoaSelecionada?.Id ?? 0;
        var erro = DivergenciaValidador.Validar(Linhas, pessoaId);
        if (erro is not null)
        {
            MessageBox.Show(erro, "Divergencia", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsCarregando = true;
            var escolhidos = Linhas.Where(l => l.Selecionado).Select(l => l.ParaGravacao()).ToList();
            await _repository.RegistrarPendenciaDivergenciaAsync(
                _nota.Id,
                pessoaId,
                Observacao,
                escolhidos,
                _pendenciaId.HasValue ? 0 : AppSettingsStore.Instance.Data.PessoaIdNestePc);
            PessoaDestino = PessoaSelecionada?.Nome ?? string.Empty;
            ItensEnviados = PendenciaConferenciaLeitura.TextoItens(escolhidos);
            _confirmou = true;
            Fechar?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gravar a divergencia:\n{ex.Message}", "Divergencia",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }
}
