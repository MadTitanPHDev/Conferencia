using System.Windows;
using System.Windows.Controls;
using ConferenciaNFs.Infrastructure;

namespace ConferenciaNFs.Controls;

public partial class PinWindowToggle : UserControl
{
    public PinWindowToggle()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (Window.GetWindow(this) is { } window)
                WindowPinService.Instance.RegistrarJanela(window);
        };
    }
}
