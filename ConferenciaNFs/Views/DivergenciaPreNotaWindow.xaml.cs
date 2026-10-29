using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class DivergenciaPreNotaWindow : Window
{
    public DivergenciaPreNotaWindow(
        NotaFiscalRepository repository,
        VsmComprasReader? reader,
        NotaFiscal nota,
        long? pendenciaId = null)
    {
        InitializeComponent();
        var vm = new DivergenciaPreNotaViewModel(repository, reader, nota, pendenciaId);
        vm.Fechar = () =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = vm;
        WindowPinService.Instance.RegistrarJanela(this);
    }

    public bool Confirmou => DataContext is DivergenciaPreNotaViewModel vm && vm.Confirmou;

    public string PessoaDestino =>
        DataContext is DivergenciaPreNotaViewModel vm ? vm.PessoaDestino : string.Empty;

    public string ItensEnviados =>
        DataContext is DivergenciaPreNotaViewModel vm ? vm.ItensEnviados : string.Empty;

    public string MensagemResultado =>
        DataContext is DivergenciaPreNotaViewModel vm ? vm.MensagemResultado : string.Empty;

    public string EnviadoPorNome =>
        DataContext is DivergenciaPreNotaViewModel vm ? vm.EnviadoPorNome : string.Empty;

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void GrdItens_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<DataGridRow>((DependencyObject)e.OriginalSource) is null)
            return;

        if (DataContext is DivergenciaPreNotaViewModel viewModel)
            viewModel.CopiarEanDaSelecionada();
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
