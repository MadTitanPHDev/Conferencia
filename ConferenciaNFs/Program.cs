using System;
using Velopack;

namespace ConferenciaNFs;

/// <summary>
/// Entry point required by Velopack (hooks de instalacao/update).
/// A verificacao automatica de novas versoes sera adicionada depois.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
