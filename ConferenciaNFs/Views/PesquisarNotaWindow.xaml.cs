using System.Windows;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class PesquisarNotaWindow : Window
{
    public PesquisarNotaWindow(
        NotaFiscalRepository repository,
        Action<NotaPesquisaResultado>? aoAbrirConferencia = null,
        Action<NotaPesquisaResultado>? aoAbrirFila = null)
    {
        InitializeComponent();
        DataContext = new PesquisarNotaViewModel(repository, aoAbrirConferencia, aoAbrirFila);
        WindowPinService.Instance.RegistrarJanela(this);
    }

    private void Fechar_Click(object sender, RoutedEventArgs e) => Close();
}
