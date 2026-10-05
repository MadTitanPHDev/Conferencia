using System.ComponentModel;
using System.Runtime.CompilerServices;
using ConferenciaNFs.Infrastructure;

namespace ConferenciaNFs.Models;

public static class StatusDevolucaoValues
{
    public const string Pendente = "Pendente";
    public const string Devolvida = "Devolvida";
    public const string Absorvido = "Absorvido";
    public const string PerdeuPrazo = "PerdeuPrazo";

    public static readonly IReadOnlyList<string> Todos =
    [
        Pendente,
        Devolvida,
        Absorvido,
        PerdeuPrazo
    ];

    public static string ObterRotulo(string status) => status switch
    {
        Pendente => "Pendente",
        Devolvida => "Devolvida ao dist.",
        Absorvido => "Absorvido",
        PerdeuPrazo => "Perdeu o prazo",
        _ => status
    };

    public static bool EstaConcluido(string status) =>
        status is Devolvida or Absorvido or PerdeuPrazo;
}

public sealed class DevolucaoItem : INotifyPropertyChanged
{
    private string _statusDevolucao = StatusDevolucaoValues.Pendente;
    private string _andamento = AndamentoDevolucaoValues.AguardandoLoja;
    private string _observacao = string.Empty;
    private string? _dataConclusao;

    public long Id { get; set; }
    public long NotaFiscalId { get; set; }
    public string ApelidoLoja { get; set; } = string.Empty;
    public string NumNota { get; set; } = string.Empty;
    public string NomeForn { get; set; } = string.Empty;
    public string CnpjForn { get; set; } = string.Empty;
    public decimal ValorNota { get; set; }
    public string DataEmissao { get; set; } = string.Empty;
    public string DiaConferencia { get; set; } = string.Empty;
    public string DataMarcada { get; set; } = string.Empty;
    public int? PrazoDevolucaoDias { get; set; }
    public bool RastrearPrazo { get; set; }

    public string StatusDevolucao
    {
        get => _statusDevolucao;
        set
        {
            if (_statusDevolucao == value)
                return;

            _statusDevolucao = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusRotulo));
            OnPropertyChanged(nameof(PodeConcluir));
        }
    }

    public string Andamento
    {
        get => _andamento;
        set
        {
            if (_andamento == value)
                return;

            _andamento = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AndamentoRotulo));
        }
    }

    public string Observacao
    {
        get => _observacao;
        set
        {
            if (_observacao == value)
                return;

            _observacao = value;
            OnPropertyChanged();
        }
    }

    public string? DataConclusao
    {
        get => _dataConclusao;
        set
        {
            if (_dataConclusao == value)
                return;

            _dataConclusao = value;
            OnPropertyChanged();
        }
    }

    public string StatusRotulo => StatusDevolucaoValues.ObterRotulo(StatusDevolucao);

    public string AndamentoRotulo => AndamentoDevolucaoValues.ObterRotulo(Andamento);

    public bool PodeConcluir => StatusDevolucao == StatusDevolucaoValues.Pendente;

    public DateTime? DataEmissaoParseada => DataCompraParser.TentarConverter(DataEmissao);

    /// <summary>
    /// Corte da fila: data em que a devolucao foi marcada (nao a emissao, nem o historico da conferencia).
    /// </summary>
    public DateTime? DataReferenciaRastreio =>
        DataCompraParser.TentarConverter(DataMarcada)
        ?? DataCompraParser.TentarConverter(DiaConferencia);

    public DateTime? DataLimite
    {
        get
        {
            if (!RastrearPrazo || !PrazoDevolucaoDias.HasValue || DataEmissaoParseada is null)
                return null;

            return DataEmissaoParseada.Value.Date.AddDays(PrazoDevolucaoDias.Value);
        }
    }

    public int? DiasRestantes
    {
        get
        {
            if (DataLimite is null)
                return null;

            return (DataLimite.Value.Date - DateTime.Today).Days;
        }
    }

    public string SituacaoPrazo
    {
        get
        {
            if (!RastrearPrazo)
                return "Nao rastreada";

            if (!PrazoDevolucaoDias.HasValue)
                return "Prazo nao configurado";

            if (DataEmissaoParseada is null)
                return "Emissao invalida";

            var dias = DiasRestantes!.Value;
            if (dias < 0)
                return $"Vencida ha {Math.Abs(dias)} dia(s)";

            if (dias == 0)
                return "Vence hoje";

            return $"{dias} dia(s) restantes";
        }
    }

    public string DataLimiteTexto =>
        DataLimite.HasValue ? DataCompraParser.Formatar(DataLimite.Value) : "—";

    public string Urgencia
    {
        get
        {
            if (StatusDevolucao != StatusDevolucaoValues.Pendente)
                return "Concluida";

            if (!RastrearPrazo)
                return "SemRastreio";

            if (!PrazoDevolucaoDias.HasValue || DataEmissaoParseada is null)
                return "SemPrazo";

            var dias = DiasRestantes!.Value;
            if (dias < 0)
                return "Vencida";
            if (dias <= 3)
                return "Critica";
            if (dias <= 7)
                return "Atencao";

            return "Ok";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
