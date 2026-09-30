using System.Reflection;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Verifica atualizacoes no GitHub Releases (Velopack) sem bloquear o uso do app.
/// So instala em copias feitas pelo Setup/Portable do Velopack (nao no F5 do Visual Studio).
/// </summary>
public static class AppUpdateService
{
    private const string RepoUrl = "https://github.com/MadTitanPHDev/Conferencia";

    public static string ObterVersaoInstalada()
    {
        try
        {
            var mgr = CriarGerenciador();
            if (mgr.IsInstalled && mgr.CurrentVersion is not null)
                return mgr.CurrentVersion.ToString();
        }
        catch
        {
            // Copia de desenvolvimento ou Velopack indisponivel: usa a versao do assembly.
        }

        return typeof(AppUpdateService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?.Split('+')[0]
            ?? "desconhecida";
    }

    public static Task VerificarAsync(Window? owner = null)
        => VerificarAsync(owner, silencioso: true);

    public static async Task VerificarAsync(Window? owner, bool silencioso)
    {
        try
        {
            var mgr = CriarGerenciador();
            if (!mgr.IsInstalled)
            {
                if (!silencioso)
                {
                    MessageBox.Show(
                        owner,
                        $"Esta copia nao foi instalada pelo Setup.\nVersao do codigo: {ObterVersaoInstalada()}\n\n" +
                        "Use o instalador Velopack das lojas para receber atualizacoes pelo GitHub.",
                        "Atualizacao",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return;
            }

            var update = await mgr.CheckForUpdatesAsync().ConfigureAwait(true);
            var atual = mgr.CurrentVersion?.ToString() ?? ObterVersaoInstalada();
            if (update is null)
            {
                if (!silencioso)
                {
                    MessageBox.Show(
                        owner,
                        $"Voce ja esta na versao mais recente.\n\nVersao instalada: {atual}",
                        "Atualizacao",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return;
            }

            var nova = update.TargetFullRelease.Version.ToString();
            var resposta = MessageBox.Show(
                owner,
                $"Ha uma nova versao disponivel.\n\n" +
                $"Atual: {atual}\n" +
                $"Nova: {nova}\n\n" +
                "Deseja baixar e reiniciar agora?\n" +
                $"(Suas configuracoes ficam em {AppSettingsStore.PastaDados} e sao preservadas.)",
                "Atualizacao disponivel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (resposta != MessageBoxResult.Yes)
                return;

            await mgr.DownloadUpdatesAsync(update).ConfigureAwait(true);
            mgr.ApplyUpdatesAndRestart(update);
        }
        catch (Exception ex)
        {
            if (silencioso)
                return;

            MessageBox.Show(
                owner,
                $"Nao foi possivel verificar atualizacoes.\n\n{ex.Message}",
                "Atualizacao",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private static UpdateManager CriarGerenciador()
        => new(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
}
