using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class DetalheNotaWindow : Window
{
    public DetalheNotaWindow(
        VsmComprasReader reader,
        NotaFiscal nota,
        NotaFiscalRepository? repository = null)
    {
        InitializeComponent();
        DataContext = new DetalheNotaViewModel(reader, nota, repository);
        Title = ((DetalheNotaViewModel)DataContext).TituloNota;
        Loaded += (_, _) => WindowPinService.Instance.RegistrarJanela(this);
        PreviewKeyDown += DetalheNotaWindow_PreviewKeyDown;
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();

    private void DetalheNotaWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        Close();
        e.Handled = true;
    }

    private void GrdItens_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridRow>((DependencyObject)e.OriginalSource) is null)
            return;

        if (DataContext is DetalheNotaViewModel viewModel)
            viewModel.CopiarEanItemSelecionado();
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
