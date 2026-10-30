using System.IO;
using Microsoft.Win32;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Abre o aplicativo quando o usuário entra no Windows.
/// Usa o executável estável do Velopack, não a pasta que muda a cada atualização.
/// </summary>
public static class InicioComWindows
{
    private const string NomeRegistro = "ConferenciaNFs";
    private const string ChaveRun = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static string CaminhoParaIniciar(string processPath)
    {
        var arquivo = Path.GetFileName(processPath);
        var pasta = Path.GetDirectoryName(processPath);
        if (string.IsNullOrWhiteSpace(arquivo) || string.IsNullOrWhiteSpace(pasta))
            return processPath;

        if (!string.Equals(Path.GetFileName(pasta), "current", StringComparison.OrdinalIgnoreCase))
            return processPath;

        var raiz = Directory.GetParent(pasta)?.FullName;
        return string.IsNullOrWhiteSpace(raiz)
            ? processPath
            : Path.Combine(raiz, arquivo);
    }

    public static void Garantir()
    {
        try
        {
            if (!AppUpdateService.EstaInstalado())
                return;

            var caminho = CaminhoParaIniciar(Environment.ProcessPath ?? string.Empty);
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
                return;

            using var chave = Registry.CurrentUser.OpenSubKey(ChaveRun, writable: true);
            chave?.SetValue(NomeRegistro, $"\"{caminho}\"");
        }
        catch
        {
            // Sem permissão no registro: o app segue abrindo normalmente.
        }
    }
}
