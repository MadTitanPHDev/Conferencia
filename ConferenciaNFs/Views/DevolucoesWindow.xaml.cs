using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class DevolucoesWindow : UserControl
{
    public DevolucoesWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Focus();
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
                Focus();
        };
    }

    private void Fechar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is DevolucoesViewModel viewModel)
            viewModel.AoFechar?.Invoke();
    }

    private void GrdDevolucoes_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            return;

        if (DataContext is DevolucoesViewModel viewModel && viewModel.CopiarNumeroNotaSelecionada())
            e.Handled = true;
    }

    private async void GrdDevolucoes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridRow>((DependencyObject)e.OriginalSource) is null)
            return;

        if (DataContext is DevolucoesViewModel viewModel)
            await viewModel.AbrirItensDaSelecionadaAsync();
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
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
