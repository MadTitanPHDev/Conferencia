using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class PendenciasCompraWindow : UserControl
{
    public PendenciasCompraWindow()
    {
        InitializeComponent();
    }

    private void GrdPendencias_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || Keyboard.Modifiers != ModifierKeys.Control)
            return;

        if (DataContext is PendenciasCompraViewModel viewModel && viewModel.CopiarNumeroNotaSelecionada())
            e.Handled = true;
    }

    private async void GrdPendencias_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridRow>((System.Windows.DependencyObject)e.OriginalSource) is null)
            return;

        if (DataContext is PendenciasCompraViewModel viewModel)
            await viewModel.AbrirItensDaSelecionadaAsync();
    }

    private static T? FindParent<T>(System.Windows.DependencyObject? child) where T : System.Windows.DependencyObject
    {
        while (child is not null)
        {
            if (child is T parent)
                return parent;

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
