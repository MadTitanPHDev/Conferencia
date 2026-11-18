namespace ConferenciaNFs.Models;

public sealed class CmedConsultaResultado
{
    public CmedProduto Produto { get; init; } = new();
    public string Aliquota { get; init; } = string.Empty;
    public decimal? Pf { get; init; }
    public decimal? Pmc { get; init; }
    public decimal? Pmvg { get; init; }
    public string Controle { get; init; } = string.Empty;

    public string Ean => Produto.EanPrincipal;
    public string Nome => Produto.Produto;
    public string Apresentacao => Produto.Apresentacao;
    public string Laboratorio => Produto.Laboratorio;
    public string Substancia => Produto.Substancia;
    public string Tarja => Produto.Tarja;
    public string Classe => Produto.Classe;
    public bool EhControlado => Controle == "Controlado";

    public string PfTexto => Formatar(Pf);
    public string PmcTexto => Formatar(Pmc);
    public string PmvgTexto => Formatar(Pmvg);

    private static string Formatar(decimal? valor)
        => valor.HasValue
            ? valor.Value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))
            : "—";
}
