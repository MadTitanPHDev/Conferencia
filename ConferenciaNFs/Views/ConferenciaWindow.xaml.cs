using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ConferenciaNFs.Models;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class ConferenciaWindow : UserControl
{
    public ConferenciaWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => Focus();
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
                Focus();
        };
        PreviewKeyDown += ConferenciaWindow_PreviewKeyDown;
    }

    private void Fechar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConferenciaViewModel viewModel)
            viewModel.AoFechar?.Invoke();
    }

    private void ConferenciaWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            TxtBuscaNota.Focus();
            TxtBuscaNota.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Escape)
            return;

        if (DataContext is ConferenciaViewModel viewModel)
            viewModel.AoFechar?.Invoke();
        e.Handled = true;
    }

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
            viewModel.ExecutarAcaoDuploClique();
    }

    private void GrdNotas_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not ConferenciaViewModel viewModel)
            return;

        var itensVisiveis = GrdNotas.Items;
        if (itensVisiveis.Count == 0)
            return;

        var direcao = e.Delta < 0 ? 1 : -1;
        var atual = viewModel.NotaSelecionada ?? GrdNotas.SelectedItem;
        var indiceAtual = atual is null ? -1 : itensVisiveis.IndexOf(atual);

        var novoIndice = indiceAtual < 0
            ? (direcao > 0 ? 0 : itensVisiveis.Count - 1)
            : Math.Clamp(indiceAtual + direcao, 0, itensVisiveis.Count - 1);

        if (novoIndice == indiceAtual)
        {
            e.Handled = true;
            return;
        }

        if (itensVisiveis[novoIndice] is not NotaFiscal nota)
        {
            e.Handled = true;
            return;
        }

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
