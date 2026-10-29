using System.Windows;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class GerenciarPessoasWindow : Window
{
    public GerenciarPessoasWindow(NotaFiscalRepository repository)
    {
        InitializeComponent();
        DataContext = new GerenciarPessoasViewModel(repository);
        WindowPinService.Instance.RegistrarJanela(this);
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();
}
