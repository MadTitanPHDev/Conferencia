using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Views;

public static class AvisoPendenciaJanela
{
    public static void MostrarResolucoes(IReadOnlyList<PendenciaAviso> resolvidas)
    {
        if (resolvidas.Count == 0)
            return;

        var linhas = resolvidas.Take(4).Select(TextoResolucao).Where(t => t.Length > 0).ToList();
        if (linhas.Count == 0)
            return;

        if (resolvidas.Count > 4)
            linhas.Add($"e mais {resolvidas.Count - 4}");

        MostrarTexto("Pendencia resolvida", "O comprador respondeu\n\n" + string.Join("\n", linhas));
    }

    public static void Mostrar(IReadOnlyList<PendenciaAviso> novas)
    {
        if (novas.Count == 0)
            return;

        var linhas = novas.Take(4).Select(Texto).ToList();
        if (novas.Count > linhas.Count)
            linhas.Add($"e mais {novas.Count - linhas.Count}");

        MostrarTexto("Nova pendencia", "Nova pendencia para voce\n\n" + string.Join("\n", linhas));
    }

    private static void MostrarTexto(string titulo, string texto)
    {
        var janela = new Window
        {
            Title = titulo,
            Width = 380,
            SizeToContent = SizeToContent.Height,
            WindowStyle = WindowStyle.ToolWindow,
            ResizeMode = ResizeMode.NoResize,
            Topmost = true,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Background = Brushes.White,
            Content = new TextBlock
            {
                Text = texto,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(16),
                FontSize = 14
            }
        };

        var area = SystemParameters.WorkArea;
        janela.Left = area.Right - janela.Width - 16;
        janela.Loaded += (_, _) => janela.Top = area.Bottom - janela.ActualHeight - 16;
        janela.Show();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            janela.Close();
        };
        timer.Start();
    }

    private static string Texto(PendenciaAviso aviso)
    {
        var por = string.IsNullOrWhiteSpace(aviso.EnviadoPorNome)
            ? string.Empty
            : $" · {aviso.EnviadoPorNome}";
        return $"Nota {aviso.NumNota} · {aviso.ApelidoLoja}{por}";
    }

    private static string TextoResolucao(PendenciaAviso aviso)
    {
        var rotulo = EquipeConferencia.RotuloResolucao(aviso.StatusPendencia);
        if (rotulo.Length == 0)
            return string.Empty;

        return $"Nota {aviso.NumNota} · {aviso.ApelidoLoja} · {rotulo}";
    }
}
