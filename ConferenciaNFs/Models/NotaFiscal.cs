using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConferenciaNFs.Models;

public class NotaFiscal : INotifyPropertyChanged
{
    private string _statusConferencia = StatusConferenciaValues.Pendente;
    private string _observacao = string.Empty;

    public long Id { get; set; }
    public string ApelidoLoja { get; set; } = string.Empty;
    public string NumNota { get; set; } = string.Empty;
    public string NomeForn { get; set; } = string.Empty;
    public string CnpjForn { get; set; } = string.Empty;
    public decimal ValorNota { get; set; }
    public string DataCompra { get; set; } = string.Empty;
    public string DataEmissao { get; set; } = string.Empty;
    public string DiaConferencia { get; set; } = string.Empty;
    public string ChaveUnica { get; set; } = string.Empty;

    public string StatusConferencia
    {
        get => _statusConferencia;
        set
        {
            if (_statusConferencia == value)
                return;

            _statusConferencia = value;
            OnPropertyChanged();
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public static string GerarChaveUnica(
        string apelidoLoja,
        string numNota,
        string cnpjForn,
        string diaConferencia,
        string dataCompra)
        => $"{apelidoLoja.Trim()}_{numNota.Trim()}_{cnpjForn.Trim()}_{diaConferencia.Trim()}_{dataCompra.Trim()}";

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public static class StatusConferenciaValues
{
    public const string Pendente = "Pendente";
    public const string Verde = "Verde";
    public const string Amarelo = "Amarelo";
    public const string Vermelho = "Vermelho";
    public const string Laranja = "Laranja";
    public const string Azul = "Azul";

    public static readonly IReadOnlyList<string> Todos =
    [
        Pendente,
        Verde,
        Amarelo,
        Vermelho,
        Laranja,
        Azul
    ];

    public static string ObterDescricao(string status) => status switch
    {
        Pendente => "Nao conferido",
        Verde => "Nota correta",
        Amarelo => "Nota com advertencia",
        Vermelho => "Nota devolvida",
        Laranja => "Nota de outro dia",
        Azul => "Nota absorvida",
        _ => status
    };
}
