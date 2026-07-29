using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ConferenciaNFs.Infrastructure;

public sealed class ThemeService : INotifyPropertyChanged
{
    public static ThemeService Instance { get; } = new();

    private bool _isDarkTheme;

    private ThemeService()
    {
        _isDarkTheme = AppSettingsStore.Instance.Data.TemaEscuro;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value)
                return;

            _isDarkTheme = value;
            AplicarTema(value);
            AppSettingsStore.Instance.Data.TemaEscuro = value;
            AppSettingsStore.Instance.Salvar();
            OnPropertyChanged();
            OnPropertyChanged(nameof(TextoBotao));
            OnPropertyChanged(nameof(DicaBotao));
        }
    }

    public string TextoBotao => IsDarkTheme ? "Claro" : "Escuro";

    public string DicaBotao => IsDarkTheme
        ? "Alternar para tema claro"
        : "Alternar para tema escuro estilo Cursor";

    public void Inicializar() => AplicarTema(_isDarkTheme);

    private static void AplicarTema(bool isDark)
    {
        var app = Application.Current;
        var themeUri = new Uri(
            isDark ? "Themes/Colors.Dark.xaml" : "Themes/Colors.Light.xaml",
            UriKind.Relative);

        var novoTema = new ResourceDictionary { Source = themeUri };
        var merged = app.Resources.MergedDictionaries;

        for (var i = 0; i < merged.Count; i++)
        {
            var source = merged[i].Source?.OriginalString ?? string.Empty;
            if (!source.Contains("Colors.", StringComparison.OrdinalIgnoreCase))
                continue;

            merged[i] = novoTema;
            return;
        }

        merged.Insert(0, novoTema);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
