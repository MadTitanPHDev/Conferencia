using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class ConsultaCmedViewModel : ViewModelBase
{
    public const string AliquotaPadrao = "18%";

    private readonly NotaFiscalRepository _repository;
    private readonly List<CmedProduto> _produtos = [];
    private string _termo = string.Empty;
    private string _mensagem = string.Empty;
    private string _aliquota = AliquotaPadrao;
    private bool _isCarregando;
    private CmedConsultaResultado? _selecionado;
    private CmedAliquotaPreco? _aliquotaLinhaAtual;
    private string _faixaProduto = string.Empty;
    private string _faixaPrecos = "Digite um código de barras e pressione Enter.";
    private string _faixaControle = string.Empty;

    public ConsultaCmedViewModel(NotaFiscalRepository repository)
    {
        _repository = repository;
        Resultados = new ObservableCollection<CmedConsultaResultado>();
        Aliquotas = new ObservableCollection<string> { AliquotaPadrao };
        Detalhe = new ObservableCollection<CmedCampo>();
        PrecosPorAliquota = new ObservableCollection<CmedAliquotaPreco>();
        BuscarCommand = new AsyncRelayCommand(_ => BuscarAsync(), _ => !IsCarregando && Termo.Trim().Length >= 2);
        AtualizarCommand = new AsyncRelayCommand(_ => GarantirCatalogoAsync(forcar: true), _ => !IsCarregando);
        CopiarEanCommand = new RelayCommand(_ => CopiarEan(), _ => !string.IsNullOrWhiteSpace(Selecionado?.Ean));
        _ = GarantirCatalogoAsync(forcar: false);
    }

    public ObservableCollection<CmedConsultaResultado> Resultados { get; }
    public ObservableCollection<string> Aliquotas { get; }
    public ObservableCollection<CmedCampo> Detalhe { get; }
    public ObservableCollection<CmedAliquotaPreco> PrecosPorAliquota { get; }
    public ICommand BuscarCommand { get; }
    public ICommand AtualizarCommand { get; }
    public ICommand CopiarEanCommand { get; }

    public string Termo
    {
        get => _termo;
        set
        {
            if (!SetProperty(ref _termo, value))
                return;
            ((AsyncRelayCommand)BuscarCommand).RaiseCanExecuteChanged();
        }
    }

    public string Aliquota
    {
        get => _aliquota;
        set
        {
            if (!SetProperty(ref _aliquota, string.IsNullOrWhiteSpace(value) ? AliquotaPadrao : value))
                return;
            if (_produtos.Count > 0 && !IsCarregando)
                Repintar();
        }
    }

    public string FaixaProduto
    {
        get => _faixaProduto;
        private set => SetProperty(ref _faixaProduto, value);
    }

    public string FaixaPrecos
    {
        get => _faixaPrecos;
        private set => SetProperty(ref _faixaPrecos, value);
    }

    public string FaixaControle
    {
        get => _faixaControle;
        private set => SetProperty(ref _faixaControle, value);
    }

    public CmedAliquotaPreco? AliquotaLinhaAtual
    {
        get => _aliquotaLinhaAtual;
        private set => SetProperty(ref _aliquotaLinhaAtual, value);
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
            ((AsyncRelayCommand)BuscarCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)AtualizarCommand).RaiseCanExecuteChanged();
        }
    }

    public CmedConsultaResultado? Selecionado
    {
        get => _selecionado;
        set
        {
            if (!SetProperty(ref _selecionado, value))
                return;
            ((RelayCommand)CopiarEanCommand).RaiseCanExecuteChanged();
            AtualizarFaixaEDetalhe();
        }
    }

    private async Task GarantirCatalogoAsync(bool forcar)
    {
        NotaFiscalRepository.CmedMetadados meta;
        try
        {
            meta = await _repository.ObterCmedMetadadosAsync();
            AplicarAliquotas(meta.Aliquotas);
            if (meta.Importado)
                Mensagem = TextoCatalogo(meta);
            else
                Mensagem = "Buscando a lista vigente na ANVISA...";
        }
        catch (Exception ex)
        {
            Mensagem = ex.Message;
            return;
        }

        CmedFontes fontes;
        try
        {
            fontes = await CmedDownload.DescobrirAsync();
        }
        catch (Exception ex)
        {
            if (!meta.Importado)
                Mensagem = "Nao foi possivel consultar a ANVISA. " + ex.Message;
            return;
        }

        var nome = CmedDownload.NomeArquivo(fontes.UrlPmc);
        if (meta.Importado && string.Equals(meta.FontePmc, nome, StringComparison.OrdinalIgnoreCase))
        {
            if (forcar)
                Mensagem = $"A lista ja esta na publicacao de {meta.Publicacao}.";
            return;
        }

        try
        {
            IsCarregando = true;
            var progresso = new Progress<string>(texto => Mensagem = texto);
            var pasta = Path.Combine(AppSettingsStore.PastaDados, "cmed");
            var pmc = await CmedDownload.BaixarAsync(
                fontes.UrlPmc,
                Path.Combine(pasta, nome),
                progresso);
            var nomePmvg = CmedDownload.NomeArquivo(fontes.UrlPmvg);
            var pmvg = await CmedDownload.BaixarAsync(
                fontes.UrlPmvg,
                Path.Combine(pasta, nomePmvg),
                progresso);

            Mensagem = "Lendo as planilhas. A primeira vez leva alguns minutos.";
            var resultado = await _repository.ImportarCatalogoCmedAsync(pmc, pmvg, progresso);
            AplicarAliquotas(resultado.Aliquotas);
            Mensagem = $"Lista da ANVISA gravada. Publicacao {resultado.Publicacao}. {resultado.Produtos.Count} apresentacoes.";
            if (Termo.Trim().Length >= 2)
                await BuscarAsync();
        }
        catch (Exception ex)
        {
            Mensagem = meta.Importado
                ? TextoCatalogo(meta) + " A atualizacao nao entrou: " + ex.Message
                : "Nao foi possivel baixar a lista da ANVISA. " + ex.Message;
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private static string TextoCatalogo(NotaFiscalRepository.CmedMetadados meta)
        => $"Publicacao CMED {meta.Publicacao}. {meta.TotalProdutos} apresentacoes, importadas em {meta.ImportadoEm}.";

    private async Task BuscarAsync()
    {
        var termo = Termo.Trim();
        if (termo.Length < 2)
            return;

        try
        {
            IsCarregando = true;
            var lista = await _repository.ConsultarCmedAsync(termo, Aliquota);
            _produtos.Clear();
            _produtos.AddRange(lista.Select(item => item.Produto));
            Repintar();
            Mensagem = lista.Count == 0
                ? $"Nenhum resultado para {termo}."
                : $"{lista.Count} resultado(s) para {termo}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Consulta CMED", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private void AplicarAliquotas(IReadOnlyList<string> aliquotas)
    {
        if (aliquotas.Count == 0)
            return;

        Aliquotas.Clear();
        foreach (var aliquota in aliquotas)
            Aliquotas.Add(aliquota);

        if (!Aliquotas.Contains(Aliquota))
            Aliquota = Aliquotas.Contains(AliquotaPadrao) ? AliquotaPadrao : Aliquotas[0];
    }

    private void Repintar()
    {
        var id = _selecionado?.Produto.Id;
        Resultados.Clear();
        foreach (var produto in _produtos)
            Resultados.Add(Montar(produto));
        Selecionado = Resultados.FirstOrDefault(item => item.Produto.Id == id) ?? Resultados.FirstOrDefault();
        if (Selecionado is null)
            AtualizarFaixaEDetalhe();
    }

    private CmedConsultaResultado Montar(CmedProduto produto)
    {
        var precos = CmedPrecos.Desserializar(produto.PrecosJson);
        return new CmedConsultaResultado
        {
            Produto = produto,
            Aliquota = Aliquota,
            Pf = precos.Obter(CmedPrecos.TipoPf, Aliquota),
            Pmc = precos.Obter(CmedPrecos.TipoPmc, Aliquota),
            Pmvg = precos.Obter(CmedPrecos.TipoPmvg, Aliquota),
            Controle = CmedTarja.Rotulo(produto.Tarja)
        };
    }

    private void AtualizarFaixaEDetalhe()
    {
        Detalhe.Clear();
        PrecosPorAliquota.Clear();
        var item = Selecionado;
        if (item is null)
        {
            FaixaProduto = string.Empty;
            FaixaPrecos = _produtos.Count == 0
                ? "Digite um código de barras e pressione Enter."
                : "Selecione um medicamento na lista.";
            FaixaControle = string.Empty;
            AliquotaLinhaAtual = null;
            return;
        }

        var apresentacao = item.Apresentacao.Trim();
        FaixaProduto = apresentacao.Length == 0 ? item.Nome : $"{item.Nome} — {apresentacao}";
        FaixaPrecos = $"PF {item.PfTexto}   ·   PMC {item.PmcTexto}   ·   PMVG {item.PmvgTexto}      (ICMS {Aliquota})";
        FaixaControle = item.EhControlado ? "Controlado" : string.Empty;

        var produto = item.Produto;
        AdicionarCampo("EAN", item.Ean);
        var outros = new[] { produto.Ean2, produto.Ean3 }
            .Where(ean => !string.IsNullOrWhiteSpace(ean))
            .ToArray();
        if (outros.Length > 0)
            AdicionarCampo("Outros EAN", string.Join(", ", outros));
        AdicionarCampo("Produto", produto.Produto);
        AdicionarCampo("Apresentação", produto.Apresentacao);
        AdicionarCampo("Laboratório", produto.Laboratorio);
        AdicionarCampo("CNPJ", produto.Cnpj);
        AdicionarCampo("Registro ANVISA", produto.Registro);
        AdicionarCampo("Código GGREM", produto.Ggrem);
        AdicionarCampo("Substância", produto.Substancia);
        AdicionarCampo("Classe", produto.Classe);
        AdicionarCampo("Tipo", produto.Tipo);
        AdicionarCampo("Regime de preço", produto.Regime);
        AdicionarCampo("Tarja", produto.Tarja);
        AdicionarCampo("Controle", item.Controle, item.EhControlado);
        AdicionarCampo("Restrição hospitalar", produto.Restricao);
        AdicionarCampo("CAP", produto.Cap);
        AdicionarCampo("CONFAZ 87", produto.Confaz87);
        AdicionarCampo("Destinação comercial", produto.Destinacao);

        var precos = CmedPrecos.Desserializar(produto.PrecosJson);
        var ordem = Aliquotas.Count > 0
            ? Aliquotas.ToList()
            : precos.Pf.Keys.Concat(precos.Pmc.Keys).Concat(precos.Pmvg.Keys).Distinct(StringComparer.Ordinal).ToList();
        foreach (var aliquota in ordem)
        {
            var linha = new CmedAliquotaPreco
            {
                Aliquota = aliquota,
                Pf = Moeda(precos.Obter(CmedPrecos.TipoPf, aliquota)),
                Pmc = Moeda(precos.Obter(CmedPrecos.TipoPmc, aliquota)),
                Pmvg = Moeda(precos.Obter(CmedPrecos.TipoPmvg, aliquota)),
                Selecionada = string.Equals(aliquota, Aliquota, StringComparison.Ordinal)
            };
            PrecosPorAliquota.Add(linha);
            if (linha.Selecionada)
                AliquotaLinhaAtual = linha;
        }
    }

    private void AdicionarCampo(string rotulo, string? valor, bool destaque = false)
    {
        Detalhe.Add(new CmedCampo
        {
            Rotulo = rotulo,
            Valor = string.IsNullOrWhiteSpace(valor) ? "—" : valor.Trim(),
            Destaque = destaque
        });
    }

    private void CopiarEan()
    {
        var ean = Selecionado?.Ean;
        if (string.IsNullOrWhiteSpace(ean))
        {
            Mensagem = "Este produto não possui EAN publicado.";
            return;
        }

        Clipboard.SetText(ean);
        Mensagem = $"EAN {ean} copiado.";
    }

    private static string Moeda(decimal? valor)
        => valor.HasValue
            ? valor.Value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))
            : "—";
}

public sealed class CmedCampo
{
    public string Rotulo { get; init; } = string.Empty;
    public string Valor { get; init; } = "—";
    public bool Destaque { get; init; }
}

public sealed class CmedAliquotaPreco
{
    public string Aliquota { get; init; } = string.Empty;
    public string Pf { get; init; } = "—";
    public string Pmc { get; init; } = "—";
    public string Pmvg { get; init; } = "—";
    public bool Selecionada { get; init; }
}
