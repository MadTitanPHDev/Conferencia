namespace ConferenciaNFs.Models;

/// <summary>
/// Item da nota cruzado com os pedidos da loja na emissão ou na entrada.
/// </summary>
public sealed class ComparacaoProdutoPedido
{
    public int CodProd { get; set; }
    public decimal QtdNota { get; set; }
    public decimal ValorItem { get; set; }
    public decimal DescontoNota { get; set; }
    public decimal PrecoUnitarioNota { get; set; }
    public decimal QtdAtendida { get; set; }
    public decimal QtdPedida { get; set; }
    public decimal PrecoUnitarioPedido { get; set; }
    public decimal DescontoPedido { get; set; }
    public int TemPedidoNumero { get; set; }
    public bool TemPedido => TemPedidoNumero != 0;
}
