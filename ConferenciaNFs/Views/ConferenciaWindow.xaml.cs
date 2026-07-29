using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class ConferenciaWindow : Window
{
    public ConferenciaWindow(NotaFiscalRepository repository, string apelidoLoja, string dataCompra)
    {
        InitializeComponent();
        DataContext = new ConferenciaViewModel(repository, apelidoLoja, dataCompra);
        Title = ((ConferenciaViewModel)DataContext).TituloConferencia;
        Loaded += (_, _) => WindowPinService.Instance.RegistrarJanela(this);
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();

    private void GrdNotas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        var row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);
        if (row is null)
            return;

        row.IsSelected = true;
        grid.SelectedItem = row.Item;
    }

    private void GrdNotas_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            return;

        if (DataContext is ConferenciaViewModel viewModel && viewModel.CopiarNumeroNotaSelecionada())
            e.Handled = true;
    }

    private void GrdNotas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridRow>((DependencyObject)e.OriginalSource) is null)
            return;

        if (DataContext is ConferenciaViewModel viewModel)
            viewModel.CopiarNumeroNotaSelecionada();
    }

    private void GrdNotas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not ConferenciaViewModel viewModel || viewModel.Notas.Count == 0)
            return;

        // Scroll para baixo = próxima nota; para cima = anterior.
        var direcao = e.Delta < 0 ? 1 : -1;
        var indiceAtual = viewModel.NotaSelecionada is null
            ? -1
            : viewModel.Notas.IndexOf(viewModel.NotaSelecionada);

        var novoIndice = indiceAtual < 0
            ? (direcao > 0 ? 0 : viewModel.Notas.Count - 1)
            : Math.Clamp(indiceAtual + direcao, 0, viewModel.Notas.Count - 1);

        if (novoIndice == indiceAtual)
        {
            e.Handled = true;
            return;
        }

        var nota = viewModel.Notas[novoIndice];
        viewModel.NotaSelecionada = nota;
        GrdNotas.SelectedItem = nota;
        GrdNotas.ScrollIntoView(nota);
        GrdNotas.Focus();
        e.Handled = true;
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
