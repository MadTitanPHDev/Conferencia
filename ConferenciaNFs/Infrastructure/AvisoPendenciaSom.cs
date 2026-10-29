using System.IO;
using System.Media;
using System.Windows;

namespace ConferenciaNFs.Infrastructure;

public static class AvisoPendenciaSom
{
    private static readonly Uri Arquivo = new(
        "pack://application:,,,/Assets/mixkit-software-interface-back-2575.wav",
        UriKind.Absolute);

    private static SoundPlayer? _player;
    private static MemoryStream? _stream;

    public static void Tocar()
    {
        var recurso = Application.GetResourceStream(Arquivo);
        if (recurso is null)
            return;

        var memoria = new MemoryStream();
        recurso.Stream.CopyTo(memoria);
        memoria.Position = 0;

        var player = new SoundPlayer(memoria);
        player.Load();
        player.Play();

        _player = player;
        _stream = memoria;
    }
}
