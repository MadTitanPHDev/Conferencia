using System.IO;
using System.Windows;
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
                GarantirArquivoExemploSettings();
                throw new InvalidOperationException(
                    "Configure a ConnectionString do PostgreSQL em app-settings.json " +
                    "(copie de app-settings.example.json se ainda nao existir).");
            }

            var repository = new NotaFiscalRepository(connectionString);
            repository.TestarConexao();
            DataContext = new DashboardViewModel(repository);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Nao foi possivel conectar ao PostgreSQL.\n\n" +
                $"{ex.Message}\n\n" +
                "Verifique:\n" +
                "• app-settings.json na pasta do aplicativo (ConnectionString)\n" +
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
    }

    private static void GarantirArquivoExemploSettings()
    {
        try
        {
            var pasta = AppContext.BaseDirectory;
            var destino = Path.Combine(pasta, "app-settings.json");
            var exemplo = Path.Combine(pasta, "app-settings.example.json");

            if (!File.Exists(destino) && File.Exists(exemplo))
                File.Copy(exemplo, destino);
        }
        catch
        {
            // Melhor esforço.
        }
    }
}
