using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Quem recebe o aviso quando o comprador resolve a pendência.
/// </summary>
public static class EquipeConferencia
{
    private static readonly HashSet<string> Nomes =
    [
        PessoaNome.Chave("Paulo"),
        PessoaNome.Chave("Guilherme"),
        PessoaNome.Chave("Matheus"),
        PessoaNome.Chave("João Gabriel"),
        PessoaNome.Chave("Joao Gabriel")
    ];

    public static bool EhMembro(string? nome) => Nomes.Contains(PessoaNome.Chave(nome));

    public static string RotuloResolucao(string? statusPendencia)
    {
        if (string.Equals(statusPendencia?.Trim(), StatusPendenciaCompraValues.Ficou, StringComparison.OrdinalIgnoreCase))
            return "absorvida";

        if (string.Equals(statusPendencia?.Trim(), StatusPendenciaCompraValues.Devolvida, StringComparison.OrdinalIgnoreCase))
            return "devolvida";

        return string.Empty;
    }
}
