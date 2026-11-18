namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Tarja preta e tarja vermelha sob restricao exigem receita e saem como Controlado.
/// A tarja original continua visivel na consulta.
/// </summary>
public static class CmedTarja
{
    public const string Controlado = "Controlado";
    public const string Receita = "Receita";
    public const string VendaLivre = "Venda livre";
    public const string NaoInformada = "Tarja nao informada";

    public static bool EhControlado(string? tarja)
    {
        var texto = Normalizar(tarja);
        return texto == "TARJA PRETA"
            || texto.StartsWith("TARJA VERMELHA SOB RESTRICAO", StringComparison.Ordinal);
    }

    public static string Rotulo(string? tarja)
    {
        if (EhControlado(tarja))
            return Controlado;

        var texto = Normalizar(tarja);
        if (texto == "TARJA VERMELHA")
            return Receita;
        if (texto == "TARJA SEM TARJA")
            return VendaLivre;
        return NaoInformada;
    }

    private static string Normalizar(string? tarja)
    {
        var texto = CmedTexto.NormalizarCabecalho(tarja).Replace("*", string.Empty);
        return texto.Trim(' ', '-', '(', ')');
    }
}
