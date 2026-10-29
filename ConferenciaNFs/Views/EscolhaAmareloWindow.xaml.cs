using System.Windows;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Views;

public partial class EscolhaAmareloWindow : Window
{
    public EscolhaAmareloWindow()
    {
        InitializeComponent();
        WindowPinService.Instance.RegistrarJanela(this);
    }

    public EscolhaAmarelo Escolha { get; private set; } = EscolhaAmarelo.Cancelar;

    private void Enviar_Click(object sender, RoutedEventArgs e)
    {
        Escolha = EscolhaAmarelo.EnviarComprador;
        DialogResult = true;
    }

    private void Somente_Click(object sender, RoutedEventArgs e)
    {
        Escolha = EscolhaAmarelo.SomenteConferencia;
        DialogResult = true;
    }
}
