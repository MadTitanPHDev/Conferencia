using System;
using Velopack;

namespace ConferenciaNFs;

/// <summary>
/// Entry point required by Velopack (hooks de instalacao/update).
/// Checagem de novas versoes: <see cref="Infrastructure.AppUpdateService"/>.
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
