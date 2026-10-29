using System.Globalization;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Nome de pessoa do cadastro compartilhado. A chave ignora maiúsculas e espaços extras.
/// </summary>
public static class PessoaNome
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string NormalizarExibicao(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return string.Empty;

        var partes = nome.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', partes);
    }

    public static string Chave(string? nome)
        => NormalizarExibicao(nome).ToLower(PtBr);

    public static bool EhValido(string? nome)
        => NormalizarExibicao(nome).Length >= 2;
}
