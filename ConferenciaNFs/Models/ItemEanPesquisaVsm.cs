namespace ConferenciaNFs.Models;

/// <summary>
/// Item encontrado no VSM pela busca de EAN. Somente leitura.
/// </summary>
public sealed class ItemEanPesquisaVsm
{
    public int CodCompra { get; set; }
    public int CodLoja { get; set; }
    public string? NumNota { get; set; }
    public DateTime? DataCompra { get; set; }
    public string? CnpjForn { get; set; }
    public decimal ValorNota { get; set; }
    public int CodProd { get; set; }
    public string? NomeProd { get; set; }
    public decimal QuantItemCompra { get; set; }
    public decimal CustoUnit { get; set; }
    public string? BarrasEan { get; set; }
}

public sealed class ItemEanPesquisaResultado
{
    public int CodCompra { get; set; }
    public string Loja { get; set; } = string.Empty;
    public string DataCompra { get; set; } = string.Empty;
    public string NumNota { get; set; } = string.Empty;
    public int CodProd { get; set; }
    public string NomeProd { get; set; } = string.Empty;
    public string BarrasEan { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal Custo { get; set; }
    public decimal ValorNota { get; set; }
    public string NomeForn { get; set; } = string.Empty;
    public string StatusConferencia { get; set; } = string.Empty;

    public string StatusRotulo => string.IsNullOrWhiteSpace(StatusConferencia)
        ? "—"
        : StatusConferenciaValues.ObterDescricao(StatusConferencia);
}
