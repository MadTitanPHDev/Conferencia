using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Infrastructure;

public static class DivergenciaValidador
{
    public static string? Validar(IEnumerable<DivergenciaLinha> linhas, long pessoaId)
    {
        if (pessoaId <= 0)
            return "Escolha a pessoa que vai receber a divergencia.";

        var escolhidas = linhas.Where(l => l.Selecionado).ToList();
        if (escolhidas.Count == 0)
            return "Selecione ao menos um produto.";

        foreach (var linha in escolhidas)
        {
            if (linha.Quantidade <= 0)
                return $"Informe a quantidade de {linha.NomeProd}.";

            if (linha.Quantidade > linha.QuantidadeNota)
                return $"A quantidade de {linha.NomeProd} nao pode ser maior que a da NF ({linha.QuantidadeNota:N3}).";
        }

        return null;
    }
}
