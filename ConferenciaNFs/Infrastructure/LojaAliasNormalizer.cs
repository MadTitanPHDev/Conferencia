using System.Globalization;
using System.Text;

namespace ConferenciaNFs.Infrastructure;

public static class LojaAliasNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> AliasCanonicos =
        new Dictionary<string, string>
        {
            ["BARAO"] = "BARAO",
            ["VELASQUES"] = "VELASQUES",
            ["COHAB"] = "COHAB",
            ["REGENTE"] = "REGENTE",
            ["RANCHARIA"] = "RANCHARIA",
            ["MARTINOPOL"] = "MARTINOPOL",
            ["MARTINOPOLIS"] = "MARTINOPOL",
            ["PIRAPOZINH"] = "PIRAPOZINH",
            ["PIRAPOZINHO"] = "PIRAPOZINH",
            ["MACHADO"] = "MACHADO",
            ["BERNARDES"] = "BERNARDES",
            ["QUATA"] = "QUATA",
            ["MIRANTE"] = "MIRANTE",
            ["RANCHA CEN"] = "RANCHA CEN",
            ["RANCHARIA CENTRO"] = "RANCHA CEN",
            ["DAMHA"] = "DAMHA",
            ["DAHMA"] = "DAMHA",
            ["TEODORO"] = "TEODORO",
            ["OSVALDO CR"] = "OSVALDO CR",
            ["OSVALDO CRUZ"] = "OSVALDO CR",
            ["QUATA FILI"] = "QUATA FILI",
            ["QUATA FIL"] = "QUATA FILI",
            ["QUATA FILIAL"] = "QUATA FILI",
            ["MARACAI"] = "MARACAI",
            ["BASTOS"] = "BASTOS",
            ["TARUMA"] = "TARUMA",
            ["LUCELIA"] = "LUCELIA",
            ["LUCELIA CE"] = "LUCELIA CE",
            ["LUCELIA CENTRO"] = "LUCELIA CE",
            ["MANOEL GOU"] = "MANOEL GOU",
            ["MANOEL GO"] = "MANOEL GOU",
            ["MANOEL GOULART"] = "MANOEL GOU",
            ["RINOPOLIS"] = "RINOPOLIS",
            ["CORONEL MA"] = "CORONEL MA",
            ["CORONEL MARCONDES"] = "CORONEL MA",
            ["ADAMANTINA"] = "ADAMANTINA",
            ["MIRANTE CE"] = "MIRANTE CE",
            ["MIRANTE CENTRO"] = "MIRANTE CE",
            ["PARAPUA"] = "PARAPUÃ",
            ["FLORIDA PA"] = "FLORIDA PA",
            ["FLORIDA PAULISTA"] = "FLORIDA PA",
            ["NOVA ESPER"] = "NOVA ESPER",
            ["NOVA ESPERANCA"] = "NOVA ESPER",
            ["TUPA"] = "TUPA",
            ["ANDRADINA"] = "ANDRADINA",
            ["AVENIDA BR"] = "AVENIDA BR",
            ["AVENIDA BRASIL"] = "AVENIDA BR",
            ["PARAGUACU"] = "PARAGUACU",
            ["PARAGUASSU"] = "PARAGUACU",
            ["TARABAI"] = "TARABAI",
            ["CASTILHO"] = "CASTILHO",
            ["PEREIRA BA"] = "PEREIRA BA",
            ["PEREIRA BARRETO"] = "PEREIRA BA",
            ["PRIMAVERA"] = "PRIMAVERA",
            ["NARANDIBA"] = "NARANDIBA"
        };

    public static string Normalizar(string? apelidoLoja)
    {
        if (string.IsNullOrWhiteSpace(apelidoLoja))
            return string.Empty;

        var chave = Simplificar(apelidoLoja);
        return AliasCanonicos.TryGetValue(chave, out var aliasCanonico)
            ? aliasCanonico
            : apelidoLoja.Trim();
    }

    private static string Simplificar(string valor)
    {
        var texto = valor.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(texto.Length);
        var ultimoFoiEspaco = false;

        foreach (var caractere in texto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caractere);
            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(caractere))
            {
                if (!ultimoFoiEspaco)
                {
                    builder.Append(' ');
                    ultimoFoiEspaco = true;
                }

                continue;
            }

            builder.Append(caractere);
            ultimoFoiEspaco = false;
        }

        return builder.ToString().Trim();
    }
}
