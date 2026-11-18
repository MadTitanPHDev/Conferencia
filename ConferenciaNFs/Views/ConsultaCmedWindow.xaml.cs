using System.ComponentModel;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class ConsultaCmedWindow : System.Windows.Window
{
    public ConsultaCmedWindow(NotaFiscalRepository repository)
    {
        InitializeComponent();
        var viewModel = new ConsultaCmedViewModel(repository);
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void CampoBusca_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not ConsultaCmedViewModel viewModel)
            return;

        if (viewModel.BuscarCommand.CanExecute(null))
            viewModel.BuscarCommand.Execute(null);
        e.Handled = true;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ConsultaCmedViewModel.AliquotaLinhaAtual))
            return;
        if (DataContext is ConsultaCmedViewModel viewModel && viewModel.AliquotaLinhaAtual is not null)
            TabelaPrecos.ScrollIntoView(viewModel.AliquotaLinhaAtual);
    }
}
