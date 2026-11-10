using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ConferenciaNFs.Infrastructure;

namespace ConferenciaNFs.Controls;

public partial class LegendaOlsChip : UserControl
{
    private readonly DispatcherTimer _fechar;

    public static readonly DependencyProperty BlocoProperty = DependencyProperty.Register(
        nameof(Bloco),
        typeof(LegendaOlsBloco),
        typeof(LegendaOlsChip),
        new PropertyMetadata(null));

    public LegendaOlsChip()
    {
        InitializeComponent();
        _fechar = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _fechar.Tick += (_, _) =>
        {
            _fechar.Stop();
            if (!IsMouseOver && !Quadro.IsMouseOver)
                Quadro.IsOpen = false;
        };
    }

    public LegendaOlsBloco? Bloco
    {
        get => (LegendaOlsBloco?)GetValue(BlocoProperty);
        set => SetValue(BlocoProperty, value);
    }

    private void AoEntrar(object sender, MouseEventArgs e)
    {
        _fechar.Stop();
        Quadro.IsOpen = true;
    }

    private void AoSair(object sender, MouseEventArgs e) => _fechar.Start();
}
