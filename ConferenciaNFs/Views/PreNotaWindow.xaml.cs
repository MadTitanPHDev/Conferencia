using System.Windows;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Views;

public partial class PreNotaWindow : Window
{
    public PreNotaWindow(
        NotaFiscalRepository repository,
        VsmComprasReader? reader,
        NotaFiscal nota,
        PreNotaModo modo = PreNotaModo.Criacao,
        DevolucaoItem? devolucao = null)
    {
        InitializeComponent();
        var vm = new PreNotaViewModel(repository, reader, nota, modo, devolucao);
        vm.Fechar = () =>
        {
            DialogResult = true;
            Close();
        };
        DataContext = vm;
        Loaded += (_, _) => WindowPinService.Instance.RegistrarJanela(this);
    }

    public bool Confirmou => DataContext is PreNotaViewModel vm && vm.Confirmou;

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
