using System.Windows;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class GerenciarPrecosWindow : Window
{
    public GerenciarPrecosWindow(NotaFiscalRepository repository)
    {
        InitializeComponent();
        DataContext = new GerenciarPrecosViewModel(repository);
        Loaded += (_, _) => WindowPinService.Instance.RegistrarJanela(this);
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();
}
