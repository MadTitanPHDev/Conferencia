using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Unifica na tela copias da mesma NF no intervalo aberto quando o status e o mesmo.
/// Nao apaga nem muda <c>DiaConferencia</c> — so escolhe qual linha mostrar.
/// </summary>
public static class UnificacaoNotasConferencia
{
    public static IReadOnlyList<NotaFiscal> UnificarMesmoStatus(IReadOnlyList<NotaFiscal> notas)
    {
        if (notas.Count <= 1)
            return notas;

        var escolhidas = new HashSet<long>();
        foreach (var grupo in AgruparPorIdentidade(notas))
        {
            foreach (var subgrupo in grupo.GroupBy(StatusNormalizado, StringComparer.OrdinalIgnoreCase))
            {
                var visivel = subgrupo
                    .OrderByDescending(n => n.DiaConferencia, Comparer<string>.Create(DataCompraParser.CompararAscendente))
                    .ThenByDescending(n => n.Id)
                    .First();
                escolhidas.Add(visivel.Id);
            }
        }

        return notas.Where(n => escolhidas.Contains(n.Id)).ToList();
    }

    public static IReadOnlyList<NotaFiscal> CopiasComMesmoStatus(
        NotaFiscal alvo,
        IReadOnlyList<NotaFiscal> notas)
    {
        ArgumentNullException.ThrowIfNull(alvo);
        var statusAlvo = StatusNormalizado(alvo);
        return notas
            .Where(n => n.Id != alvo.Id
                        && MesmaIdentidade(alvo, n)
                        && string.Equals(StatusNormalizado(n), statusAlvo, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static bool MesmaIdentidade(NotaFiscal a, NotaFiscal b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Id != 0 && a.Id == b.Id)
            return true;

        var codA = a.CodCompra ?? 0;
        var codB = b.CodCompra ?? 0;
        if (codA > 0 && codA == codB)
            return true;

        var nfeA = (a.NfeChaveAcesso ?? string.Empty).Trim();
        var nfeB = (b.NfeChaveAcesso ?? string.Empty).Trim();
        if (nfeA.Length > 0 && string.Equals(nfeA, nfeB, StringComparison.OrdinalIgnoreCase))
            return true;

        var numA = NumNotaNormalizer.Normalizar(a.NumNota);
        var numB = NumNotaNormalizer.Normalizar(b.NumNota);
        if (numA.Length == 0 || numB.Length == 0)
            return false;

        return string.Equals(a.ApelidoLoja?.Trim(), b.ApelidoLoja?.Trim(), StringComparison.OrdinalIgnoreCase)
               && string.Equals(numA, numB, StringComparison.OrdinalIgnoreCase)
               && string.Equals(CnpjDigitos(a.CnpjForn), CnpjDigitos(b.CnpjForn), StringComparison.Ordinal);
    }

    private static IReadOnlyList<IReadOnlyList<NotaFiscal>> AgruparPorIdentidade(IReadOnlyList<NotaFiscal> notas)
    {
        var n = notas.Count;
        var parent = Enumerable.Range(0, n).ToArray();

        int Find(int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }

            return i;
        }

        void Union(int x, int y)
        {
            x = Find(x);
            y = Find(y);
            if (x != y)
                parent[y] = x;
        }

        for (var i = 0; i < n; i++)
        {
            for (var j = i + 1; j < n; j++)
            {
                if (MesmaIdentidade(notas[i], notas[j]))
                    Union(i, j);
            }
        }

        return notas
            .Select((nota, i) => (nota, grupo: Find(i)))
            .GroupBy(x => x.grupo)
            .Select(g => (IReadOnlyList<NotaFiscal>)g.Select(x => x.nota).ToList())
            .ToList();
    }

    private static string StatusNormalizado(NotaFiscal nota)
        => string.IsNullOrWhiteSpace(nota.StatusConferencia)
            ? StatusConferenciaValues.Pendente
            : nota.StatusConferencia.Trim();

    private static string CnpjDigitos(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return string.Empty;

        return string.Concat(cnpj.Where(char.IsDigit));
    }
}
