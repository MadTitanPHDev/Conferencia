using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public static class ComparacaoPrecoTabela
{
    public static string Classificar(decimal? custoNf, decimal? precoTabela)
    {
        if (!precoTabela.HasValue || precoTabela.Value <= 0)
            return ComparacaoPrecoValores.Cinza;

        if (!custoNf.HasValue)
            return ComparacaoPrecoValores.Cinza;

        var nf = custoNf.Value;
        var tabela = precoTabela.Value;
        var tolerancia = ComparacaoPrecoValores.ToleranciaReais;

        if (nf <= tabela + tolerancia)
            return ComparacaoPrecoValores.Verde;

        if (nf <= tabela * ComparacaoPrecoValores.FaixaAzulFator + tolerancia)
            return ComparacaoPrecoValores.AzulClaro;

        return ComparacaoPrecoValores.Vermelho;
    }
}
