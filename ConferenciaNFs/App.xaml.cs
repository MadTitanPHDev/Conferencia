using System.IO;
using System.Windows;
using ConferenciaNFs.Infrastructure;

namespace ConferenciaNFs;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        GarantirAppSettingsNaPasta();
        AppSettingsStore.Instance.Recarregar();
        ThemeService.Instance.Inicializar();

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private static void GarantirAppSettingsNaPasta()
    {
        try
        {
            var pasta = AppContext.BaseDirectory;
            var settingsPath = Path.Combine(pasta, "app-settings.json");
            var exemploPath = Path.Combine(pasta, "app-settings.example.json");

            if (!File.Exists(settingsPath) && File.Exists(exemploPath))
            {
                File.Copy(exemploPath, settingsPath);
                return;
            }

            // Arquivo antigo sem ConnectionString: completa a partir do exemplo.
            if (!File.Exists(settingsPath) || !File.Exists(exemploPath))
                return;

            AppSettings? atuais = null;
            try
            {
                atuais = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath));
            }
            catch
            {
                // ignora
            }

            if (!string.IsNullOrWhiteSpace(atuais?.ConnectionString))
                return;

            var exemploSettings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(exemploPath));
            if (string.IsNullOrWhiteSpace(exemploSettings?.ConnectionString))
                return;

            atuais ??= new AppSettings();
            atuais.ConnectionString = exemploSettings.ConnectionString;
            File.WriteAllText(
                settingsPath,
                System.Text.Json.JsonSerializer.Serialize(atuais, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Melhor esforço.
        }
    }
}
