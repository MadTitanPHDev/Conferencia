using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConferenciaNFs.Models;

public sealed class Distribuidora : INotifyPropertyChanged
{
    private string _nomeForn = string.Empty;
    private int? _prazoDevolucaoDias;
    private bool _rastrearPrazo;

    public string CnpjForn { get; set; } = string.Empty;

    public string NomeForn
    {
        get => _nomeForn;
        set
        {
            if (_nomeForn == value)
                return;

            _nomeForn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SituacaoPrazo));
        }
    }

    public int? PrazoDevolucaoDias
    {
        get => _prazoDevolucaoDias;
        set
        {
            if (_prazoDevolucaoDias == value)
                return;

            _prazoDevolucaoDias = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SituacaoPrazo));
            OnPropertyChanged(nameof(PrazoTexto));
        }
    }

    public bool RastrearPrazo
    {
        get => _rastrearPrazo;
        set
        {
            if (_rastrearPrazo == value)
                return;

            _rastrearPrazo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SituacaoPrazo));
        }
    }

    public string PrazoTexto =>
        PrazoDevolucaoDias.HasValue ? PrazoDevolucaoDias.Value.ToString() : string.Empty;

    /// <summary>
    /// Resumo para a grade: nao rastreada, prazo nao configurado, ou N dias.
    /// </summary>
    public string SituacaoPrazo
    {
        get
        {
            if (!RastrearPrazo)
                return "Nao rastreada";

            if (!PrazoDevolucaoDias.HasValue)
                return "Prazo nao configurado";

            return $"{PrazoDevolucaoDias.Value} dias";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
