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

    private void GrdResultados_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridRow>((DependencyObject)e.OriginalSource) is null)
            return;

        if (DataContext is PesquisarProdutoViewModel viewModel)
            viewModel.AbrirItensSelecionado();
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
