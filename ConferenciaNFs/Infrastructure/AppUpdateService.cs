using System.Windows;
using Velopack;
using Velopack.Sources;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Verifica atualizacoes no GitHub Releases (Velopack) sem bloquear o uso do app.
/// So funciona em instalacoes feitas pelo Setup/Portable do Velopack (nao no F5 do Visual Studio).
/// </summary>
public static class AppUpdateService
{
    private const string RepoUrl = "https://github.com/MadTitanPHDev/Conferencia";

    public static async Task VerificarAsync(Window? owner = null)
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
            if (!mgr.IsInstalled)
                return;

            var update = await mgr.CheckForUpdatesAsync().ConfigureAwait(true);
            if (update is null)
                return;

            var atual = mgr.CurrentVersion?.ToString() ?? "?";
            var nova = update.TargetFullRelease.Version.ToString();

            var resposta = MessageBox.Show(
                owner,
                $"Ha uma nova versao disponivel.\n\n" +
                $"Atual: {atual}\n" +
                $"Nova: {nova}\n\n" +
                "Deseja baixar e reiniciar agora?\n" +
                "(O arquivo app-settings.json local sera preservado.)",
                "Atualizacao disponivel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (resposta != MessageBoxResult.Yes)
                return;

            await mgr.DownloadUpdatesAsync(update).ConfigureAwait(true);
            mgr.ApplyUpdatesAndRestart(update);
        }
        catch
        {
            // Rede/GitHub indisponivel: nao atrapalhar o uso diario.
        }
    }
}
