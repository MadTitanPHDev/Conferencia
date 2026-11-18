using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConferenciaNFs.Models;

/// <summary>
/// Precos de uma apresentacao CMED, por tipo e por aliquota de ICMS.
/// </summary>
public sealed class CmedPrecos
{
    public const string TipoPf = "PF";
    public const string TipoPmc = "PMC";
    public const string TipoPmvg = "PMVG";

    private static readonly JsonSerializerOptions JsonOpcoes = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Dictionary<string, decimal> Pf { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, decimal> Pmc { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, decimal> Pmvg { get; init; } = new(StringComparer.Ordinal);

    public decimal? Obter(string tipo, string aliquota) => tipo switch
    {
        TipoPf => Pf.TryGetValue(aliquota, out var pf) ? pf : null,
        TipoPmc => Pmc.TryGetValue(aliquota, out var pmc) ? pmc : null,
        TipoPmvg => Pmvg.TryGetValue(aliquota, out var pmvg) ? pmvg : null,
        _ => null
    };

    public string Serializar()
    {
        var mapa = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.Ordinal);
        if (Pf.Count > 0) mapa[TipoPf] = Pf;
        if (Pmc.Count > 0) mapa[TipoPmc] = Pmc;
        if (Pmvg.Count > 0) mapa[TipoPmvg] = Pmvg;
        return JsonSerializer.Serialize(mapa, JsonOpcoes);
    }

    public static CmedPrecos Desserializar(string? json)
    {
        var precos = new CmedPrecos();
        if (string.IsNullOrWhiteSpace(json))
            return precos;

        try
        {
            var mapa = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(json);
            if (mapa is null)
                return precos;

            foreach (var (tipo, valores) in mapa)
            {
                var destino = tipo switch
                {
                    TipoPf => precos.Pf,
                    TipoPmc => precos.Pmc,
                    TipoPmvg => precos.Pmvg,
                    _ => null
                };

                if (destino is null || valores is null)
                    continue;

                foreach (var (aliquota, valor) in valores)
                    destino[aliquota] = valor;
            }
        }
        catch (JsonException)
        {
        }

        return precos;
    }
}
