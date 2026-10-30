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

    public static string? ValidarAtribuidos(IEnumerable<DivergenciaLinha> linhas)
    {
        var atribuidos = linhas.Where(l => l.CompradorId > 0).ToList();
        if (atribuidos.Count == 0)
            return "Aplique um comprador aos itens marcados.";

        foreach (var linha in atribuidos)
        {
            if (linha.Quantidade <= 0)
                return $"Informe a quantidade de {linha.NomeProd}.";

            if (linha.Quantidade > linha.QuantidadeNota)
                return $"A quantidade de {linha.NomeProd} nao pode ser maior que a da NF ({linha.QuantidadeNota:N3}).";

            if (string.IsNullOrWhiteSpace(linha.Motivo))
                return $"Informe o motivo de {linha.NomeProd}.";
        }

        return null;
    }
}
