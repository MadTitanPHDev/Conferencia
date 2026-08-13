using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class DevolucoesWindow : Window
{
    public DevolucoesWindow(NotaFiscalRepository repository)
    {
        InitializeComponent();
        DataContext = new DevolucoesViewModel(repository);
        Loaded += (_, _) => WindowPinService.Instance.RegistrarJanela(this);
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();

    private void GrdDevolucoes_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            return;

        if (DataContext is DevolucoesViewModel viewModel && viewModel.CopiarNumeroNotaSelecionada())
            e.Handled = true;
    }

    private void GrdDevolucoes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridRow>((DependencyObject)e.OriginalSource) is null)
            return;

        if (DataContext is DevolucoesViewModel viewModel)
            viewModel.CopiarNumeroNotaSelecionada();
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
