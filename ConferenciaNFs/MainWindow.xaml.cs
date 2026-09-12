using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        try
        {
            var connectionString = AppSettingsStore.Instance.Data.ConnectionString?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Configure a ConnectionString do PostgreSQL em:\n" +
                    AppSettingsStore.Instance.CaminhoArquivo);
            }

            var repository = new NotaFiscalRepository(connectionString);
            repository.TestarConexao();

            VsmSyncService? syncVsm = null;
            var mysql = AppSettingsStore.Instance.Data.MysqlConnectionString?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(mysql))
                syncVsm = new VsmSyncService(repository, new VsmComprasReader(mysql));

            DataContext = new DashboardViewModel(repository, syncVsm);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Nao foi possivel conectar ao PostgreSQL.\n\n" +
                $"{ex.Message}\n\n" +
                "Verifique:\n" +
                $"• ConnectionString em {AppSettingsStore.Instance.CaminhoArquivo}\n" +
                "• Servico PostgreSQL em execucao neste PC\n" +
                "• Firewall liberando a porta 5432 (para colegas na rede)\n" +
                "• Host, usuario e senha corretos",
                "Erro de conexao",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Loaded += (_, _) => Close();
            return;
        }

        Loaded += (_, _) => WindowPinService.Instance.RegistrarJanela(this);
        Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void ToggleMenuAvancado_Click(object sender, RoutedEventArgs e)
    {
        var abrir = MenuAvancadoDrawer.Visibility != Visibility.Visible;
        MenuAvancadoDrawer.Visibility = abrir ? Visibility.Visible : Visibility.Collapsed;
        MenuAvancadoOverlay.Visibility = abrir ? Visibility.Visible : Visibility.Collapsed;
    }

    private void FecharMenuAvancado_Click(object sender, MouseButtonEventArgs e)
    {
        FecharMenuAvancado();
        e.Handled = true;
    }

    private void FecharMenuAvancado_Click(object sender, RoutedEventArgs e)
    {
        FecharMenuAvancado();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || MenuAvancadoDrawer.Visibility != Visibility.Visible)
            return;

        FecharMenuAvancado();
        e.Handled = true;
    }

    private void FecharMenuAvancado()
    {
        MenuAvancadoDrawer.Visibility = Visibility.Collapsed;
        MenuAvancadoOverlay.Visibility = Visibility.Collapsed;
    }

}
