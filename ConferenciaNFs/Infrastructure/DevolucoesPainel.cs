using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public sealed class DevolucoesPainel
{
    public int Pendentes { get; init; }
    public decimal ValorPendentes { get; init; }
    public int Vencidas { get; init; }
    public int AVencer { get; init; }
    public int Devolvidas { get; init; }
    public decimal ValorDevolvido { get; init; }
    public int Absorvidas { get; init; }
    public decimal ValorAbsorvido { get; init; }
    public int PerdeuPrazo { get; init; }
    public IReadOnlyList<string> TopLojas { get; init; } = [];
    public IReadOnlyList<string> PorAndamento { get; init; } = [];
    public IReadOnlyList<string> PorClasse { get; init; } = [];
    public IReadOnlyList<string> PorMotivo { get; init; } = [];

    public static DevolucoesPainel Calcular(
        IReadOnlyList<DevolucaoItem> notas,
        IReadOnlyDictionary<long, IReadOnlyList<DevolucaoItemPreNota>> itensPorDevolucao)
    {
        var pendentes = notas.Where(d => d.StatusDevolucao == StatusDevolucaoValues.Pendente).ToList();
        var devolvidas = notas.Where(d => d.StatusDevolucao == StatusDevolucaoValues.Devolvida).ToList();
        var absorvidas = notas.Where(d => d.StatusDevolucao == StatusDevolucaoValues.Absorvido).ToList();

        var itens = notas
            .SelectMany(n => itensPorDevolucao.TryGetValue(n.Id, out var linhas) ? linhas : [])
            .ToList();

        return new DevolucoesPainel
        {
            Pendentes = pendentes.Count,
            ValorPendentes = pendentes.Sum(d => d.ValorNota),
            Vencidas = pendentes.Count(d => d.Urgencia == "Vencida"),
            AVencer = pendentes.Count(d => d.DiasRestantes is >= 0 and <= 7),
            Devolvidas = devolvidas.Count,
            ValorDevolvido = devolvidas.Sum(d => d.ValorNota),
            Absorvidas = absorvidas.Count,
            ValorAbsorvido = absorvidas.Sum(d => d.ValorNota),
            PerdeuPrazo = notas.Count(d => d.StatusDevolucao == StatusDevolucaoValues.PerdeuPrazo),
            TopLojas = pendentes
                .GroupBy(d => d.ApelidoLoja)
                .Select(g => (Loja: g.Key, Valor: g.Sum(x => x.ValorNota)))
                .OrderByDescending(x => x.Valor)
                .Take(5)
                .Select(x => $"{x.Loja} · R$ {x.Valor:N2}")
                .ToList(),
            PorAndamento = pendentes
                .GroupBy(d => AndamentoDevolucaoValues.ObterRotulo(d.Andamento))
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList(),
            PorClasse = itens
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Classe) ? "Sem classe" : i.Classe)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList(),
            PorMotivo = itens
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Motivo) ? "Sem motivo" : i.Motivo)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList()
        };
    }
}
