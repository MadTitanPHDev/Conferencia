using System.Windows;
using ConferenciaNFs.Infrastructure;

namespace ConferenciaNFs;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // O store cria/migra o arquivo em %AppData% antes de qualquer leitura.
        AppSettingsStore.Instance.Recarregar();
        ThemeService.Instance.Inicializar();

        var mainWindow = new MainWindow();
        mainWindow.Show();

        // Nao bloqueia a abertura; so pergunta se houver release mais nova no GitHub.
        _ = AppUpdateService.VerificarAsync(mainWindow);
    }
}
