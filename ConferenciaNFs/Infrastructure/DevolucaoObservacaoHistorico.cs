using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class DevolucaoObservacaoHistorico
{
    public static string FormatarData(DateTime momento) =>
        momento.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    public static string Juntar(IReadOnlyList<DevolucaoObservacao> linhas)
    {
        if (linhas.Count == 0)
            return string.Empty;

        return string.Join(
            Environment.NewLine,
            linhas.OrderByDescending(l => l.Id).Select(l => l.Rotulo));
    }
}
