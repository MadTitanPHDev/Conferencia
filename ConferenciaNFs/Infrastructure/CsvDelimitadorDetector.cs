using System.IO;

namespace ConferenciaNFs.Infrastructure;

public static class CsvDelimitadorDetector
{
    private const int LinhaCabecalho = 8;

    public static char Detectar(string caminhoArquivo)
    {
        using var reader = new StreamReader(caminhoArquivo);

        for (var linha = 1; linha < LinhaCabecalho; linha++)
        {
            if (reader.ReadLine() is null)
                return ';';
        }

        var cabecalho = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(cabecalho))
            return ';';

        var pontoVirgula = cabecalho.Count(c => c == ';');
        var virgula = cabecalho.Count(c => c == ',');

        return pontoVirgula >= virgula ? ';' : ',';
    }
}
