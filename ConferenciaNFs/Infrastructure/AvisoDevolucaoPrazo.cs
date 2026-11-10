using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public readonly record struct DevolucaoParaAviso(
    long Id,
    string NumNota,
    string ApelidoLoja,
    string Status,
    bool RastrearPrazo,
    int? PrazoDias,
    DateTime? Emissao,
    string? UltimoAviso);

public readonly record struct AvisoDevolucaoPronto(
    long Id,
    string NumNota,
    string ApelidoLoja,
    string Texto,
    bool PrazoAcabando);

/// <summary>
/// Lais recebe o lembrete hoje. Quando o cadastro tiver o cargo Devolução,
/// a mesma regra passa a incluir quem estiver nesse cargo.
/// </summary>
public static class AvisoDevolucaoPrazo
{
    public const int DiasParaAcabar = 7;
    public const int IntervaloDias = 2;

    public static bool DeveReceber(string? nome)
    {
        var chave = PessoaNome.Chave(nome);
        return chave is "lais" or "laís";
    }

    public static IReadOnlyList<AvisoDevolucaoPronto> Selecionar(
        IEnumerable<DevolucaoParaAviso> devolucoes,
        DateTime hoje)
    {
        var dia = hoje.Date;
        var prontos = new List<AvisoDevolucaoPronto>();
        foreach (var item in devolucoes)
        {
            if (!string.Equals(item.Status, StatusDevolucaoValues.Pendente, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IntervaloCumprido(item.UltimoAviso, dia))
                continue;

            var texto = Texto(item, dia);
            if (texto is null)
                continue;

            prontos.Add(new AvisoDevolucaoPronto(
                item.Id,
                item.NumNota,
                item.ApelidoLoja,
                texto,
                PrazoAcabando(item, dia)));
        }

        return prontos;
    }

    public static bool IntervaloCumprido(string? ultimoAviso, DateTime hoje)
    {
        var ultimo = DataCompraParser.TentarConverter(ultimoAviso);
        if (ultimo is null)
            return true;

        return (hoje.Date - ultimo.Value.Date).Days >= IntervaloDias;
    }

    private static bool PrazoAcabando(DevolucaoParaAviso item, DateTime hoje)
    {
        if (!item.RastrearPrazo || item.PrazoDias is null || item.Emissao is null)
            return false;

        var restantes = (item.Emissao.Value.Date.AddDays(item.PrazoDias.Value) - hoje.Date).Days;
        return restantes <= DiasParaAcabar;
    }

    private static string? Texto(DevolucaoParaAviso item, DateTime hoje)
    {
        var nota = $"Nota {item.NumNota} · {item.ApelidoLoja}";
        if (!item.RastrearPrazo)
            return $"{nota} · sem prazo rastreado";

        if (item.PrazoDias is null || item.Emissao is null)
            return $"{nota} · prazo nao configurado";

        var restantes = (item.Emissao.Value.Date.AddDays(item.PrazoDias.Value) - hoje.Date).Days;
        if (restantes > DiasParaAcabar)
            return null;

        if (restantes < 0)
            return $"{nota} · vencida ha {Math.Abs(restantes)} dia(s)";

        if (restantes == 0)
            return $"{nota} · vence hoje";

        return $"{nota} · vence em {restantes} dia(s)";
    }
}
