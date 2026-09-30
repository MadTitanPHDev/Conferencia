using System.Windows;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class GerenciarMotivosWindow : Window
{
    public GerenciarMotivosWindow(NotaFiscalRepository repository)
    {
        InitializeComponent();
        DataContext = new GerenciarMotivosViewModel(repository);
        Loaded += (_, _) => WindowPinService.Instance.RegistrarJanela(this);
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();
}
