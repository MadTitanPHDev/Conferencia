namespace ConferenciaNFs.Models;

/// <summary>
/// Custo de entrada da NF. Na loja 29 (Nova Esperança/PR) soma ICMS-ST e FCP-ST.
/// </summary>
public static class CustoItemNota
{
    public const int CodLojaParana = 29;

    public static bool UsaCustoComSt(int codLoja) => codLoja == CodLojaParana;

    public static decimal SemImposto(decimal valorItemCompra, decimal desconto)
        => Math.Max(0m, valorItemCompra - desconto);

    public static decimal Calcular(
        int codLoja,
        decimal valorItemCompra,
        decimal desconto,
        decimal custoValorSt = 0m,
        decimal custoValorFcpSt = 0m)
    {
        var baseCusto = SemImposto(valorItemCompra, desconto);
        if (!UsaCustoComSt(codLoja))
            return baseCusto;

        return Math.Max(0m, baseCusto + Math.Max(0m, custoValorSt) + Math.Max(0m, custoValorFcpSt));
    }
}
