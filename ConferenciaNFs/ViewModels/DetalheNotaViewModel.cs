using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class DetalheNotaViewModel : ViewModelBase
{
    private readonly VsmComprasReader? _reader;
    private readonly NotaFiscalRepository? _repository;
    private readonly NotaFiscal _nota;
    private bool _isCarregando;
    private bool _estaVazio = true;
    private string _mensagemStatus = "Carregando itens...";
    private string _situacaoVsm = "VSM: —";
    private ItemNotaPrecoLinha? _itemSelecionado;
    private Pessoa? _compradorLote;
    private string _motivoLote = string.Empty;

    public DetalheNotaViewModel(
        VsmComprasReader? reader,
        NotaFiscal nota,
        NotaFiscalRepository? repository = null)
    {
        _reader = reader;
        _repository = repository;
        _nota = nota;
        Itens = new ObservableCollection<ItemNotaPrecoLinha>();
        Pessoas = new ObservableCollection<Pessoa>();
        CompararPrecosCommand = new AsyncRelayCommand(_ => CompararPrecosAsync(), _ => PodeCompararPrecos);
        AplicarCompradorCommand = new RelayCommand(_ => AplicarComprador());
        EnviarPendenciaCommand = new AsyncRelayCommand(_ => EnviarPendenciaAsync(), _ => PodeEnviarPendencia);
        _ = CarregarItensAsync();
    }

    public ObservableCollection<ItemNotaPrecoLinha> Itens { get; }
    public ObservableCollection<Pessoa> Pessoas { get; }

    public bool PendenciaEnviada { get; private set; }
    public string DestinoEnviado { get; private set; } = string.Empty;
    public string ItensEnviados { get; private set; } = string.Empty;

    public bool PodeCompararPrecos => _repository is not null && !IsCarregando;
    public bool PodeEnviarPendencia => _repository is not null && !IsCarregando && Itens.Count > 0;

    public Pessoa? CompradorLote
    {
        get => _compradorLote;
        set => SetProperty(ref _compradorLote, value);
    }

    public string MotivoLote
    {
        get => _motivoLote;
        set => SetProperty(ref _motivoLote, value ?? string.Empty);
    }

    public ItemNotaPrecoLinha? ItemSelecionado
    {
        get => _itemSelecionado;
        set => SetProperty(ref _itemSelecionado, value);
    }

    public string TituloNota =>
        $"Nota {_nota.NumNota} · {_nota.ApelidoLoja}";

    public string SubtituloNota
    {
        get
        {
            var fornecedor = string.IsNullOrWhiteSpace(_nota.NomeForn) ? _nota.CnpjForn : _nota.NomeForn;
            return $"{fornecedor} · R$ {_nota.ValorNota:N2}";
        }
    }

    public string SituacaoVsm
    {
        get => _situacaoVsm;
        private set => SetProperty(ref _situacaoVsm, value);
    }

    public bool IsCarregando
    {
        get => _isCarregando;
        private set
        {
            if (!SetProperty(ref _isCarregando, value))
                return;
            OnPropertyChanged(nameof(PodeCompararPrecos));
            OnPropertyChanged(nameof(PodeEnviarPendencia));
            ((AsyncRelayCommand)CompararPrecosCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)EnviarPendenciaCommand).RaiseCanExecuteChanged();
        }
    }

    public bool EstaVazio
    {
        get => _estaVazio;
        private set => SetProperty(ref _estaVazio, value);
    }

    public string MensagemStatus
    {
        get => _mensagemStatus;
        private set => SetProperty(ref _mensagemStatus, value);
    }

    public string TextoResumo
    {
        get
        {
            if (Itens.Count == 0)
                return "Nenhum item carregado. Duplo clique copia o EAN.";

            return $"{Itens.Count} item(ns) · nota R$ {_nota.ValorNota:N2} · Duplo clique copia o EAN.";
        }
    }

    public ICommand CompararPrecosCommand { get; }
    public ICommand AplicarCompradorCommand { get; }
    public ICommand EnviarPendenciaCommand { get; }

    public bool CopiarEanItemSelecionado()
    {
        if (ItemSelecionado is null)
            return false;

        var ean = ItemSelecionado.EanParaCruzar;
        if (!EanNormalizer.EhValido(ean))
        {
            MensagemStatus = "Este item nao tem EAN para copiar.";
            return false;
        }

        try
        {
            Clipboard.SetText(ean);
            MensagemStatus = $"EAN {ean} copiado.";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task CompararPrecosAsync()
    {
        if (_repository is null)
            return;

        try
        {
            IsCarregando = true;
            var tabelas = await _repository.ListarPrecosImportacoesAsync();
            if (tabelas.Count == 0)
            {
                MessageBox.Show(
                    "Nenhuma tabela importada. Use Tabelas de preco no menu.",
                    "Buscar preco",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var ids = tabelas.Select(t => t.Id).ToList();
            var eans = Itens
                .SelectMany(i => new[] { i.BarrasEan, i.BarrasEanTrib })
                .OfType<string>()
                .Where(EanNormalizer.EhValido)
                .ToList();
            var mapa = await _repository.ObterPrecosPorEansAsync(ids, eans);

            var verde = 0;
            var azul = 0;
            var vermelho = 0;
            var cinza = 0;
            var ambar = 0;

            foreach (var linha in Itens)
            {
                var temEan = EanNormalizer.EhValido(linha.BarrasEan)
                    || EanNormalizer.EhValido(linha.BarrasEanTrib);
                if (!temEan)
                {
                    linha.AplicarSemEan();
                    ambar++;
                    continue;
                }

                var escolha = ComparacaoPrecoTabela.Localizar(
                    linha.BarrasEan,
                    linha.BarrasEanTrib,
                    mapa,
                    _nota.ApelidoLoja);
                if (escolha is null)
                {
                    linha.AplicarForaDaPlanilha();
                    cinza++;
                    continue;
                }

                var custo = ComparacaoPrecoTabela.ResolverCusto(
                    linha.CustoUnit,
                    linha.Item.CustoUnit,
                    linha.QuantItemCompra);
                linha.AplicarTabela(
                    escolha.Value.Item,
                    custo.Valor,
                    ComparacaoPrecoTabela.NotaIncluiSt(linha.Item.CodLoja, _nota.ApelidoLoja),
                    custo.Aviso,
                    escolha.Value.PeloEanTributario);
                switch (linha.ComparacaoResultado)
                {
                    case ComparacaoPrecoValores.Verde:
                        verde++;
                        break;
                    case ComparacaoPrecoValores.AzulClaro:
                        azul++;
                        break;
                    case ComparacaoPrecoValores.Vermelho:
                        vermelho++;
                        break;
                }
            }

            var rotuloTabelas = tabelas.Count == 1
                ? "1 tabela"
                : $"{tabelas.Count} tabelas";
            MensagemStatus =
                $"Preco ({rotuloTabelas}): {verde} verde · {azul} azul · {vermelho} vermelho · {cinza} fora · {ambar} sem EAN.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao comparar precos:\n{ex.Message}", "Buscar preco",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private async Task CarregarItensAsync()
    {
        try
        {
            IsCarregando = true;
            MensagemStatus = "Buscando itens...";
            await CarregarSituacaoVsmAsync();

            IReadOnlyList<ItemCompraVsm> itens;
            var veioDoSnapshot = false;

            if (_repository is not null)
            {
                var foto = await ItemNotaSnapshotServico.GarantirAsync(_repository, _reader, _nota);
                itens = foto.Select(ItemNotaSnapshotMap.ParaVsm).ToList();
                veioDoSnapshot = _reader is null || foto.Count > 0;
            }
            else
            {
                var codCompra = _nota.CodCompra ?? 0;
                itens = _reader is null
                    ? []
                    : await _reader.ListarItensPorCodCompraAsync(codCompra);
            }

            Itens.Clear();
            foreach (var item in itens)
                Itens.Add(new ItemNotaPrecoLinha(item));

            EstaVazio = Itens.Count == 0;
            ItemSelecionado = Itens.FirstOrDefault();
            await CarregarPessoasAsync();
            MensagemStatus = EstaVazio
                ? "Nenhum item encontrado para esta nota."
                : veioDoSnapshot && _reader is null
                    ? $"{Itens.Count} item(ns) da foto (VSM indisponivel)."
                    : $"{Itens.Count} item(ns) carregado(s).";
            OnPropertyChanged(nameof(TextoResumo));
        }
        catch (Exception ex)
        {
            EstaVazio = true;
            MensagemStatus = "Nao foi possivel carregar os itens.";
            MessageBox.Show(
                $"Erro ao buscar itens:\n{ex.Message}",
                "Itens da nota",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private async Task CarregarSituacaoVsmAsync()
    {
        var status = _nota.StatusVsm;
        var reaberta = _nota.ReabertaVsm;
        var codCompra = _nota.CodCompra ?? 0;

        if (_reader is not null && codCompra > 0)
        {
            try
            {
                var mapa = await _reader.ListarSituacaoPorCodCompraAsync([codCompra]);
                if (mapa.TryGetValue(codCompra, out var atual))
                {
                    status = atual.Status;
                    reaberta = atual.Reaberta;
                    _nota.StatusVsm = status;
                    _nota.ReabertaVsm = reaberta;
                    if (_repository is not null)
                        await _repository.GravarSituacaoVsmAsync(mapa);
                }
            }
            catch
            {
                // Mantem o valor gravado na ultima sincronizacao.
            }
        }

        SituacaoVsm = codCompra <= 0 && string.IsNullOrWhiteSpace(status)
            ? "VSM: sem codigo da compra"
            : $"VSM: {StatusCompraVsm.Rotulo(status, reaberta)}";
    }

    private async Task CarregarPessoasAsync()
    {
        if (_repository is null)
            return;

        var pessoas = await _repository.ListarPessoasAsync(somenteAtivos: true);
        Pessoas.Clear();
        foreach (var pessoa in pessoas)
            Pessoas.Add(pessoa);
    }

    private void AplicarComprador()
    {
        if (CompradorLote is null)
        {
            MessageBox.Show("Escolha o comprador.", "Enviar pendencia",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(MotivoLote))
        {
            MessageBox.Show("Escreva o motivo.", "Enviar pendencia",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var marcados = Itens.Where(i => i.Selecionado).ToList();
        if (marcados.Count == 0)
        {
            MessageBox.Show("Marque ao menos um item.", "Enviar pendencia",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var linha in marcados)
            linha.DefinirComprador(CompradorLote.Id, CompradorLote.Nome, MotivoLote.Trim());

        MensagemStatus = $"{marcados.Count} item(ns) para {CompradorLote.Nome}.";
    }

    private async Task EnviarPendenciaAsync()
    {
        if (_repository is null)
            return;

        if (AppSettingsStore.Instance.Data.PessoaIdNestePc <= 0)
        {
            MessageBox.Show(
                "Escolha quem usa este PC no menu (☰) antes de enviar a divergencia.",
                "Enviar pendencia",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var grupos = Itens.Where(i => i.CompradorId > 0).GroupBy(i => i.CompradorId).ToList();
        if (grupos.Count == 0)
        {
            MessageBox.Show("Aplique um comprador aos itens marcados.", "Enviar pendencia",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (grupos.SelectMany(g => g).Any(i => string.IsNullOrWhiteSpace(i.Motivo)))
        {
            MessageBox.Show("Informe o motivo dos itens marcados.", "Enviar pendencia",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            IsCarregando = true;
            foreach (var grupo in grupos)
            {
                var escolhidos = grupo.Select(i => i.ParaPendencia()).ToList();
                var motivos = string.Join(" · ", grupo.Select(i => i.Motivo.Trim()).Where(m => m.Length > 0).Distinct());
                await _repository.RegistrarPendenciaDivergenciaAsync(
                    _nota.Id,
                    grupo.Key,
                    motivos,
                    escolhidos,
                    AppSettingsStore.Instance.Data.PessoaIdNestePc);
            }

            var enviados = Itens.Where(i => i.CompradorId > 0).Select(i => i.ParaPendencia()).ToList();
            DestinoEnviado = string.Join(", ", grupos.Select(g => g.First().CompradorNome).Distinct());
            ItensEnviados = PendenciaConferenciaLeitura.TextoItens(enviados);
            _nota.StatusConferencia = StatusConferenciaValues.Amarelo;
            _nota.OrigemStatus = OrigemStatusValues.Manual;
            _nota.DestinoPendencia = DestinoEnviado;
            _nota.ItensPendencia = ItensEnviados;
            PendenciaEnviada = true;
            MensagemStatus = $"Nota {_nota.NumNota}: amarelo, enviada para {DestinoEnviado}. Continua na conferencia.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao enviar a pendencia:\n{ex.Message}", "Enviar pendencia",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }
}
