using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class GerenciarDistribuidorasViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private readonly List<Distribuidora> _todas = [];
    private Distribuidora? _selecionada;
    private string _filtroTexto = string.Empty;
    private string _filtroModo = "Todas";
    private string _editarPrazoTexto = string.Empty;
    private bool _editarRastrearPrazo;
    private string _mensagem = string.Empty;

    public GerenciarDistribuidorasViewModel(NotaFiscalRepository repository)
    {
        _repository = repository;
        Distribuidoras = new ObservableCollection<Distribuidora>();
        FiltrosModo = new ObservableCollection<string>(["Todas", "Rastreando", "Sem prazo", "Nao rastreando"]);

        SalvarEdicaoCommand = new AsyncRelayCommand(_ => SalvarEdicaoAsync(), _ => Selecionada is not null);
        AlternarRastreioCommand = new AsyncRelayCommand(
            param => AlternarRastreioAsync(param as Distribuidora),
            param => param is Distribuidora);
        AtualizarCommand = new AsyncRelayCommand(_ => CarregarAsync());

        _ = CarregarAsync();
    }

    public ObservableCollection<Distribuidora> Distribuidoras { get; }
    public ObservableCollection<string> FiltrosModo { get; }

    public Distribuidora? Selecionada
    {
        get => _selecionada;
        set
        {
            if (!SetProperty(ref _selecionada, value))
                return;

            if (value is null)
            {
                EditarPrazoTexto = string.Empty;
                EditarRastrearPrazo = false;
            }
            else
            {
                EditarPrazoTexto = value.PrazoDevolucaoDias?.ToString() ?? string.Empty;
                EditarRastrearPrazo = value.RastrearPrazo;
            }

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

    public string EditarPrazoTexto
    {
        get => _editarPrazoTexto;
        set => SetProperty(ref _editarPrazoTexto, value);
    }

    public bool EditarRastrearPrazo
    {
        get => _editarRastrearPrazo;
        set => SetProperty(ref _editarRastrearPrazo, value);
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    public ICommand SalvarEdicaoCommand { get; }
    public ICommand AlternarRastreioCommand { get; }
    public ICommand AtualizarCommand { get; }

    private async Task CarregarAsync()
    {
        try
        {
            var lista = await _repository.ObterDistribuidorasAsync();
            _todas.Clear();
            _todas.AddRange(lista);

            AplicarFiltro();

            var rastreando = _todas.Count(d => d.RastrearPrazo);
            var semPrazo = _todas.Count(d => d.RastrearPrazo && !d.PrazoDevolucaoDias.HasValue);
            Mensagem = $"{_todas.Count} distribuidora(s) · {rastreando} com rastreio" +
                       (semPrazo > 0 ? $" · {semPrazo} com prazo nao configurado" : string.Empty);
        }
        catch (Exception ex)
        {
            Mensagem = "Erro ao carregar distribuidoras.";
            MessageBox.Show($"Erro ao carregar distribuidoras:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AplicarFiltro()
    {
        IEnumerable<Distribuidora> query = _todas;

        query = FiltroModo switch
        {
            "Rastreando" => query.Where(d => d.RastrearPrazo),
            "Sem prazo" => query.Where(d => d.RastrearPrazo && !d.PrazoDevolucaoDias.HasValue),
            "Nao rastreando" => query.Where(d => !d.RastrearPrazo),
            _ => query
        };

        var termo = FiltroTexto.Trim();
        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(d =>
                d.NomeForn.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || d.CnpjForn.Contains(termo, StringComparison.OrdinalIgnoreCase));
        }

        var cnpjSelecionado = Selecionada?.CnpjForn;
        Distribuidoras.Clear();
        foreach (var item in query)
            Distribuidoras.Add(item);

        Selecionada = cnpjSelecionado is null
            ? Distribuidoras.FirstOrDefault()
            : Distribuidoras.FirstOrDefault(d =>
                string.Equals(d.CnpjForn, cnpjSelecionado, StringComparison.OrdinalIgnoreCase))
              ?? Distribuidoras.FirstOrDefault();
    }

    private async Task AlternarRastreioAsync(Distribuidora? distribuidora)
    {
        if (distribuidora is null)
            return;

        var novoRastreio = !distribuidora.RastrearPrazo;

        try
        {
            await _repository.AtualizarDistribuidoraAsync(
                distribuidora.CnpjForn,
                distribuidora.PrazoDevolucaoDias,
                novoRastreio);

            distribuidora.RastrearPrazo = novoRastreio;
            if (ReferenceEquals(Selecionada, distribuidora)
                || string.Equals(Selecionada?.CnpjForn, distribuidora.CnpjForn, StringComparison.OrdinalIgnoreCase))
            {
                EditarRastrearPrazo = novoRastreio;
            }

            var rastreando = _todas.Count(d => d.RastrearPrazo);
            var semPrazo = _todas.Count(d => d.RastrearPrazo && !d.PrazoDevolucaoDias.HasValue);
            Mensagem = $"{distribuidora.NomeForn}: rastreio {(novoRastreio ? "ativado" : "desativado")} · {_todas.Count} cadastro(s) · {rastreando} com rastreio"
                       + (semPrazo > 0 ? $" · {semPrazo} com prazo nao configurado" : string.Empty);

            // Se o filtro atual esconder a linha, reaplica a lista.
            if (FiltroModo is "Rastreando" or "Nao rastreando" or "Sem prazo")
                AplicarFiltro();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao atualizar rastreio:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
            await CarregarAsync();
        }
    }

    private async Task SalvarEdicaoAsync()
    {
        if (Selecionada is null)
            return;

        int? prazo = null;
        var textoPrazo = EditarPrazoTexto.Trim();
        if (!string.IsNullOrWhiteSpace(textoPrazo))
        {
            if (!int.TryParse(textoPrazo, out var dias) || dias < 0)
            {
                MessageBox.Show(
                    "Informe um prazo em dias valido (numero inteiro >= 0) ou deixe em branco.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            prazo = dias;
        }

        try
        {
            await _repository.AtualizarDistribuidoraAsync(
                Selecionada.CnpjForn,
                prazo,
                EditarRastrearPrazo);

            Mensagem = $"Distribuidora {Selecionada.NomeForn} atualizada.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar distribuidora:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
