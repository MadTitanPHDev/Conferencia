using ConferenciaNFs.Models;
using ConferenciaNFs.ViewModels;

namespace ConferenciaNFs.Infrastructure;

public static class PreNotaValidador
{
    public static string? Validar(IEnumerable<PreNotaLinha> linhas)
    {
        var escolhidas = linhas.Where(l => l.Selecionado).ToList();
        if (escolhidas.Count == 0)
            return "Selecione ao menos um produto, com quantidade, motivo e classe.";

        foreach (var linha in escolhidas)
        {
            if (linha.QuantidadeDevolver <= 0)
                return $"Informe a quantidade a devolver de {linha.NomeProd}.";

            if (linha.QuantidadeDevolver > linha.QuantidadeNota)
                return $"A quantidade de {linha.NomeProd} nao pode ser maior que a da NF ({linha.QuantidadeNota:N3}).";

            if (string.IsNullOrWhiteSpace(linha.Motivo))
                return $"Selecione o motivo de {linha.NomeProd}.";

            if (string.IsNullOrWhiteSpace(linha.Classe)
                || !ClasseDevolucaoValues.Todas.Contains(linha.Classe))
                return $"Selecione a classe de {linha.NomeProd}.";
        }

        return null;
    }
}
