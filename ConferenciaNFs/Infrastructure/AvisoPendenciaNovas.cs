namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// A primeira leitura só memoriza o que já está aberto. O som vale para o que chegar depois.
/// </summary>
public static class AvisoPendenciaNovas
{
    public static IReadOnlyList<T> Separar<T>(
        IReadOnlyList<T> atuais,
        HashSet<long> conhecidas,
        Func<T, long> id,
        ref bool jaLeu)
    {
        if (!jaLeu)
        {
            conhecidas.Clear();
            foreach (var item in atuais)
                conhecidas.Add(id(item));
            jaLeu = true;
            return [];
        }

        var novas = new List<T>();
        var idsAtuais = new HashSet<long>();
        foreach (var item in atuais)
        {
            var itemId = id(item);
            idsAtuais.Add(itemId);
            if (conhecidas.Add(itemId))
                novas.Add(item);
        }

        conhecidas.RemoveWhere(existente => !idsAtuais.Contains(existente));
        return novas;
    }
}
