namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Letras de compras.STATUS no VSM. A está em aberto. F está finalizada.
/// D é devolvido. F com REABERTA = S foi finalizada e depois reaberta.
/// As demais letras aparecem como estão.
/// </summary>
public static class StatusCompraVsm
{
    public const string Aberta = "A";
    public const string Finalizada = "F";
    public const string Devolvido = "D";

    public static string Rotulo(string? status, string? reaberta)
    {
        var letra = (status ?? string.Empty).Trim().ToUpperInvariant();
        var reabertaSim = string.Equals(
            (reaberta ?? string.Empty).Trim(),
            "S",
            StringComparison.OrdinalIgnoreCase);

        if (letra == Aberta)
            return "Em aberto";

        if (letra == Finalizada && reabertaSim)
            return "Reaberta";

        if (letra == Finalizada)
            return "Finalizada";

        if (letra == Devolvido)
            return "Devolvido";

        return letra.Length == 0 ? "—" : letra;
    }
}
