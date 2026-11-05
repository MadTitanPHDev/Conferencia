using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Na pesquisa de produto, a devolução é do item buscado, não da nota inteira.
/// </summary>
public static class DevolucaoItemPesquisa
{
    public const string Devolvido = "Devolvido";
    public const string NaFila = "Na fila";
    public const string Nao = "Não";

    public static string Rotulo(string? statusDevolucao)
    {
        var status = statusDevolucao?.Trim() ?? string.Empty;
        if (status.Length == 0)
            return Nao;

        if (string.Equals(status, StatusDevolucaoValues.Devolvida, StringComparison.OrdinalIgnoreCase))
            return Devolvido;

        if (string.Equals(status, StatusDevolucaoValues.Pendente, StringComparison.OrdinalIgnoreCase))
            return NaFila;

        if (string.Equals(status, StatusDevolucaoValues.Absorvido, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, StatusDevolucaoValues.PerdeuPrazo, StringComparison.OrdinalIgnoreCase))
            return StatusDevolucaoValues.ObterRotulo(status);

        return Nao;
    }

    public static string Resolver(
        int codProd,
        string? ean,
        IEnumerable<ItemDevolucaoConsulta> itens)
    {
        ItemDevolucaoConsulta? melhor = null;
        var pesoMelhor = -1;
        var eanBusca = SomenteDigitos(ean);

        foreach (var item in itens)
        {
            if (!Combina(codProd, eanBusca, item))
                continue;

            var peso = Peso(item.StatusDevolucao);
            if (peso <= pesoMelhor)
                continue;

            pesoMelhor = peso;
            melhor = item;
        }

        return melhor is null ? Nao : Rotulo(melhor.StatusDevolucao);
    }

    public static bool ConfereFiltro(string rotulo, string? filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro))
            return true;

        return string.Equals(rotulo.Trim(), filtro.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Combina(int codProd, string eanBusca, ItemDevolucaoConsulta item)
    {
        if (codProd > 0 && item.CodProd == codProd)
            return true;

        var eanItem = SomenteDigitos(item.BarrasEan);
        return eanBusca.Length >= 8
            && eanItem.Length >= 8
            && string.Equals(eanBusca, eanItem, StringComparison.Ordinal);
    }

    private static int Peso(string? status)
    {
        var texto = status?.Trim() ?? string.Empty;
        if (string.Equals(texto, StatusDevolucaoValues.Devolvida, StringComparison.OrdinalIgnoreCase))
            return 4;
        if (string.Equals(texto, StatusDevolucaoValues.Pendente, StringComparison.OrdinalIgnoreCase))
            return 3;
        if (string.Equals(texto, StatusDevolucaoValues.Absorvido, StringComparison.OrdinalIgnoreCase))
            return 2;
        if (string.Equals(texto, StatusDevolucaoValues.PerdeuPrazo, StringComparison.OrdinalIgnoreCase))
            return 1;
        return 0;
    }

    private static string SomenteDigitos(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        return new string(texto.Where(char.IsDigit).ToArray());
    }
}

public sealed class ItemDevolucaoConsulta
{
    public int CodCompra { get; set; }
    public int CodProd { get; set; }
    public string BarrasEan { get; set; } = string.Empty;
    public string StatusDevolucao { get; set; } = string.Empty;
}
