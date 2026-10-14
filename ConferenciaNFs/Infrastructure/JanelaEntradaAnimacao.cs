using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ConferenciaNFs.Infrastructure;

public static class JanelaEntradaAnimacao
{
    private static readonly HashSet<Window> Preparadas = [];

    public static void AplicarJanela(Window janela, FrameworkElement? conteudo = null)
    {
        if (!Preparadas.Add(janela))
            return;

        janela.Closed += (_, _) => Preparadas.Remove(janela);

        if (janela.IsLoaded)
            return;

        var alvo = conteudo ?? janela.Content as FrameworkElement;
        GarantirTranslate(alvo);
        janela.Opacity = 0;
        janela.Loaded += (_, _) => Animar(janela, alvo, eixoY: true);
    }

    public static void AplicarPainel(FrameworkElement elemento)
    {
        elemento.BeginAnimation(UIElement.OpacityProperty, null);
        elemento.Opacity = 0;
        var translate = GarantirTranslate(elemento);
        translate.BeginAnimation(TranslateTransform.XProperty, null);
        translate.BeginAnimation(TranslateTransform.YProperty, null);
        translate.X = 24;
        translate.Y = 0;
        Animar(elemento, elemento, eixoY: false);
    }

    private static TranslateTransform GarantirTranslate(FrameworkElement? elemento)
    {
        if (elemento is null)
            return new TranslateTransform();

        if (elemento.RenderTransform is TranslateTransform existente)
            return existente;

        var novo = new TranslateTransform();
        elemento.RenderTransform = novo;
        return novo;
    }

    private static void Animar(UIElement alvoOpacidade, FrameworkElement? conteudo, bool eixoY)
    {
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        alvoOpacidade.BeginAnimation(UIElement.OpacityProperty, fade);

        if (conteudo?.RenderTransform is not TranslateTransform translate)
            return;

        var de = eixoY ? 16d : 24d;
        var propriedade = eixoY ? TranslateTransform.YProperty : TranslateTransform.XProperty;
        translate.BeginAnimation(propriedade, new DoubleAnimation(de, 0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }
}
