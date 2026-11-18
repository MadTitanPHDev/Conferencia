namespace ConferenciaNFs.Models;

public sealed class CmedProduto
{
    public long Id { get; set; }
    public string Registro { get; set; } = string.Empty;
    public string Ggrem { get; set; } = string.Empty;
    public string? Ean1 { get; set; }
    public string? Ean2 { get; set; }
    public string? Ean3 { get; set; }
    public string Substancia { get; set; } = string.Empty;
    public string Laboratorio { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Produto { get; set; } = string.Empty;
    public string Apresentacao { get; set; } = string.Empty;
    public string Classe { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Regime { get; set; } = string.Empty;
    public string Restricao { get; set; } = string.Empty;
    public string Tarja { get; set; } = string.Empty;
    public string Destinacao { get; set; } = string.Empty;
    public string Cap { get; set; } = string.Empty;
    public string Confaz87 { get; set; } = string.Empty;
    public string PrecosJson { get; set; } = "{}";
    public string LinhaOrigem { get; set; } = string.Empty;
    public string Busca { get; set; } = string.Empty;

    public string EanPrincipal
        => !string.IsNullOrWhiteSpace(Ean1) ? Ean1!
         : !string.IsNullOrWhiteSpace(Ean2) ? Ean2!
         : !string.IsNullOrWhiteSpace(Ean3) ? Ean3!
         : string.Empty;

    public IEnumerable<string> Eans()
    {
        if (!string.IsNullOrWhiteSpace(Ean1)) yield return Ean1!;
        if (!string.IsNullOrWhiteSpace(Ean2)) yield return Ean2!;
        if (!string.IsNullOrWhiteSpace(Ean3)) yield return Ean3!;
    }
}
