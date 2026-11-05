using System.Globalization;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Compara o item da NF com os pedidos da mesma loja na data de emissão ou de entrada.
/// </summary>
public static class ComparacaoPedidoNota
{
    private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");

    public static decimal PercentualDesconto(decimal valorItem, decimal desconto)
    {
        if (valorItem <= 0)
            return 0;

        return Math.Round(desconto / valorItem * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static ComparacaoPedidoExibicao Exibir(ComparacaoProdutoPedido item)
    {
        var descNota = PercentualDesconto(item.ValorItem, item.DescontoNota);
        var precoTotalNota = item.PrecoUnitarioNota * item.QtdNota;
        var qtdPedido = item.TemPedido ? item.QtdAtendida : 0m;
        var precoUnitPedido = item.TemPedido ? item.PrecoUnitarioPedido : 0m;
        var descPedido = item.TemPedido ? item.DescontoPedido : 0m;
        var precoTotalPedido = precoUnitPedido * (qtdPedido > 0 ? qtdPedido : item.QtdPedida);

        return new ComparacaoPedidoExibicao
        {
            Situacao = item.TemPedido
                ? Situacao(item.QtdNota, item.QtdAtendida, item.QtdPedida)
                : "Não solicitado",
            QtdNota = Numero(item.QtdNota),
            QtdPedido = item.TemPedido ? Numero(qtdPedido) : "—",
            DifQtd = item.TemPedido ? Numero(item.QtdNota - qtdPedido) : "—",
            DifQtdAlerta = item.TemPedido && item.QtdNota != qtdPedido,
            PrecoTotalNota = Dinheiro(precoTotalNota),
            PrecoTotalPedido = item.TemPedido ? Dinheiro(precoTotalPedido) : "—",
            DifPrecoTotal = item.TemPedido ? Dinheiro(precoTotalNota - precoTotalPedido) : "—",
            DifPrecoTotalAlerta = item.TemPedido && Diferente(precoTotalNota, precoTotalPedido),
            PrecoUnitNota = Dinheiro(item.PrecoUnitarioNota),
            PrecoUnitPedido = item.TemPedido ? Dinheiro(precoUnitPedido) : "—",
            DifPrecoUnit = item.TemPedido ? Dinheiro(item.PrecoUnitarioNota - precoUnitPedido) : "—",
            DifPrecoUnitAlerta = item.TemPedido && Diferente(item.PrecoUnitarioNota, precoUnitPedido),
            DescontoNota = Percentual(descNota),
            DescontoPedido = item.TemPedido ? Percentual(descPedido) : "—",
            DifDesconto = item.TemPedido ? Percentual(descNota - descPedido) : "—",
            DifDescontoAlerta = item.TemPedido && Diferente(descNota, descPedido)
        };
    }

    public static string Texto(ComparacaoProdutoPedido item)
    {
        var tela = Exibir(item);
        return $"{tela.Situacao} · qtd {tela.QtdNota} / {tela.QtdPedido} · desconto na nota {tela.DescontoNota}";
    }

    public static string Situacao(decimal qtdNota, decimal qtdAtendida, decimal qtdPedida)
    {
        if (qtdAtendida <= 0 && qtdPedida <= 0)
            return "Nao solicitado";

        if (qtdNota > 0 && qtdAtendida >= qtdNota)
            return "Atendido";

        if (qtdAtendida > 0)
            return "Atendido parcialmente";

        return "Pedido sem atendimento";
    }

    private static bool Diferente(decimal a, decimal b) => Math.Abs(a - b) >= 0.01m;

    private static string Percentual(decimal valor) => valor.ToString("0.##", Pt);

    private static string Dinheiro(decimal valor) => valor.ToString("N2", Pt);

    private static string Numero(decimal valor) =>
        valor == decimal.Truncate(valor)
            ? valor.ToString("0", Pt)
            : valor.ToString("0.###", Pt);
}

public sealed class ComparacaoPedidoExibicao
{
    public string Situacao { get; init; } = "—";
    public string QtdNota { get; init; } = "—";
    public string QtdPedido { get; init; } = "—";
    public string DifQtd { get; init; } = "—";
    public bool DifQtdAlerta { get; init; }
    public string PrecoTotalNota { get; init; } = "—";
    public string PrecoTotalPedido { get; init; } = "—";
    public string DifPrecoTotal { get; init; } = "—";
    public bool DifPrecoTotalAlerta { get; init; }
    public string PrecoUnitNota { get; init; } = "—";
    public string PrecoUnitPedido { get; init; } = "—";
    public string DifPrecoUnit { get; init; } = "—";
    public bool DifPrecoUnitAlerta { get; init; }
    public string DescontoNota { get; init; } = "—";
    public string DescontoPedido { get; init; } = "—";
    public string DifDesconto { get; init; } = "—";
    public bool DifDescontoAlerta { get; init; }
}
