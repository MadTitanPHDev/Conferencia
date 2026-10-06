using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ConferenciaNFs.Controls;

public partial class LojaPatternBackground : UserControl
{
    private static readonly Brush Fundo = CriarPincel(Color.FromRgb(0x0B, 0x1F, 0x3F));
    private static readonly Pen Grade = CriarCaneta(Color.FromArgb(0x08, 255, 255, 255), 1);
    private static readonly Brush Ponto40 = CriarPincel(Color.FromArgb(0x0D, 255, 255, 255));
    private static readonly Brush Ponto60 = CriarPincel(Color.FromArgb(0x0B, 255, 255, 255));
    private static readonly Pen Listra = CriarCaneta(Color.FromArgb(0x06, 255, 255, 255), 10);

    public LojaPatternBackground()
    {
        InitializeComponent();
        SizeChanged += (_, _) => InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var largura = ActualWidth;
        var altura = ActualHeight;
        if (largura <= 0 || altura <= 0)
            return;

        dc.PushClip(new RectangleGeometry(new Rect(0, 0, largura, altura)));
        dc.DrawRectangle(Fundo, null, new Rect(0, 0, largura, altura));

        for (double x = 0; x <= largura; x += 20)
            dc.DrawLine(Grade, new Point(x, 0), new Point(x, altura));
        for (double y = 0; y <= altura; y += 20)
            dc.DrawLine(Grade, new Point(0, y), new Point(largura, y));

        for (double x = 20; x <= largura; x += 40)
        {
            for (double y = 20; y <= altura; y += 40)
                dc.DrawEllipse(Ponto40, null, new Point(x, y), 1, 1);
        }

        for (double x = 30; x <= largura; x += 60)
        {
            for (double y = 30; y <= altura; y += 60)
                dc.DrawEllipse(Ponto60, null, new Point(x, y), 1, 1);
        }

        var alcance = largura + altura;
        const double eixo = 0.70710678118;
        for (double t = -alcance; t <= alcance; t += 20)
        {
            var cx = t * eixo;
            var cy = -t * eixo;
            var s = alcance * 2;
            dc.DrawLine(
                Listra,
                new Point(cx - eixo * s, cy - eixo * s),
                new Point(cx + eixo * s, cy + eixo * s));
        }

        dc.Pop();
    }

    private static SolidColorBrush CriarPincel(Color cor)
    {
        var pincel = new SolidColorBrush(cor);
        pincel.Freeze();
        return pincel;
    }

    private static Pen CriarCaneta(Color cor, double espessura)
    {
        var caneta = new Pen(CriarPincel(cor), espessura)
        {
            StartLineCap = PenLineCap.Flat,
            EndLineCap = PenLineCap.Flat
        };
        caneta.Freeze();
        return caneta;
    }
}
