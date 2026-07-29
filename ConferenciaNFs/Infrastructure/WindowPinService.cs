using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ConferenciaNFs.Infrastructure;

namespace ConferenciaNFs.Infrastructure;

public sealed class WindowPinService : INotifyPropertyChanged
{
    public static WindowPinService Instance { get; } = new();

    private readonly List<Window> _windows = [];
    private bool _isPinned;

    private WindowPinService()
    {
        _isPinned = AppSettingsStore.Instance.Data.JanelaFixadaNoTopo;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned == value)
                return;

            _isPinned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TextoBotao));
            OnPropertyChanged(nameof(DicaBotao));
            AplicarEmTodasJanelas();
            AppSettingsStore.Instance.Data.JanelaFixadaNoTopo = value;
            AppSettingsStore.Instance.Salvar();
        }
    }

    public string TextoBotao => IsPinned ? "Fixado" : "Fixar";

    public string DicaBotao => IsPinned
        ? "Janela fixada acima dos outros programas. Clique para desfixar."
        : "Fixar janela acima dos outros programas.";

    public void Alternar() => IsPinned = !IsPinned;

    public void RegistrarJanela(Window window)
    {
        if (_windows.Contains(window))
            return;

        _windows.Add(window);
        window.Topmost = IsPinned;
        window.Closed += RemoverJanela;
    }

    private void RemoverJanela(object? sender, EventArgs e)
    {
        if (sender is not Window window)
            return;

        window.Closed -= RemoverJanela;
        _windows.Remove(window);
    }

    private void AplicarEmTodasJanelas()
    {
        foreach (var window in _windows.ToArray())
        {
            if (window.IsLoaded)
                window.Topmost = IsPinned;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
