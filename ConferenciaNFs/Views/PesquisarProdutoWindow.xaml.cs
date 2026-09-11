using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class PesquisarProdutoWindow : Window
{
    public PesquisarProdutoWindow(NotaFiscalRepository repository, VsmComprasReader reader)
    {
        InitializeComponent();
        DataContext = new PesquisarProdutoViewModel(repository, reader);
        Loaded += (_, _) => WindowPinService.Instance.RegistrarJanela(this);
        PreviewKeyDown += PesquisarProdutoWindow_PreviewKeyDown;
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();

    private void PesquisarProdutoWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        Close();
        e.Handled = true;
    }

    private void GrdResultados_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            return;

        if (DataContext is PesquisarProdutoViewModel viewModel && viewModel.CopiarNumeroNotaSelecionada())
            e.Handled = true;
    }

    private void GrdResultados_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        var row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);
        if (row is null)
            return;

        row.IsSelected = true;
        grid.SelectedItem = row.Item;
    }

    private void GrdResultados_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridRow>((DependencyObject)e.OriginalSource) is null)
            return;

        if (DataContext is PesquisarProdutoViewModel viewModel)
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
