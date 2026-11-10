using System.Globalization;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public readonly record struct CustoComparacaoPreco(decimal Valor, string Aviso);

public readonly record struct LinhaPrecoEscolhida(PrecoTabelaItem Item, bool PeloEanTributario);

public static class ComparacaoPrecoTabela
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

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

    public static bool NotaIncluiSt(int codLoja, string? apelidoLoja) =>
        CustoItemNota.UsaCustoComSt(codLoja)
        || LojaAliasNormalizer.Normalizar(apelidoLoja) == "NOVA ESPER";

    public static CustoComparacaoPreco ResolverCusto(
        decimal custoCalculado,
        decimal custoUnitVsm,
        decimal quantidade)
    {
        if (custoUnitVsm <= 0)
            return new CustoComparacaoPreco(custoCalculado, string.Empty);

        if (quantidade > 1
            && Math.Abs(custoCalculado - custoUnitVsm * quantidade)
               <= ComparacaoPrecoValores.ToleranciaReais * quantidade)
        {
            return new CustoComparacaoPreco(
                custoUnitVsm,
                $"unit. VSM R$ {custoUnitVsm.ToString("N2", PtBr)}");
        }

        if (Math.Abs(custoCalculado - custoUnitVsm) > ComparacaoPrecoValores.ToleranciaReais)
        {
            return new CustoComparacaoPreco(
                custoCalculado,
                $"VSM R$ {custoUnitVsm.ToString("N2", PtBr)}");
        }

        return new CustoComparacaoPreco(custoCalculado, string.Empty);
    }

    public static PrecoTabelaItem? Escolher(
        IReadOnlyList<PrecoTabelaItem> candidatos,
        string? apelidoLoja)
    {
        if (candidatos.Count == 0)
            return null;

        var lojaNota = LojaAliasNormalizer.Normalizar(apelidoLoja);
        foreach (var grupo in candidatos.GroupBy(c => c.ImportacaoId).OrderByDescending(g => g.Key))
        {
            var linhas = grupo.ToList();
            if (lojaNota.Length > 0)
            {
                var especifica = linhas.FirstOrDefault(l =>
                    LojaAliasNormalizer.Normalizar(l.Loja) == lojaNota);
                if (especifica is not null)
                    return especifica;
            }

            var geral = linhas.FirstOrDefault(l => string.IsNullOrWhiteSpace(l.Loja));
            if (geral is not null)
                return geral;
        }

        return null;
    }

    public static LinhaPrecoEscolhida? Localizar(
        string? ean,
        string? eanTributario,
        IReadOnlyDictionary<string, IReadOnlyList<PrecoTabelaItem>> mapa,
        string? apelidoLoja)
    {
        var primaria = ChaveValida(ean);
        if (primaria.Length > 0
            && mapa.TryGetValue(primaria, out var daPrimaria))
        {
            var escolhida = Escolher(daPrimaria, apelidoLoja);
            if (escolhida is not null)
                return new LinhaPrecoEscolhida(escolhida, false);
        }

        var tributaria = ChaveValida(eanTributario);
        if (tributaria.Length > 0
            && tributaria != primaria
            && mapa.TryGetValue(tributaria, out var daTributaria))
        {
            var escolhida = Escolher(daTributaria, apelidoLoja);
            if (escolhida is not null)
                return new LinhaPrecoEscolhida(escolhida, true);
        }

        return null;
    }

    public static (string Resultado, string Texto) Descrever(
        decimal custoNf,
        PrecoTabelaItem tabela,
        bool notaComSt,
        string avisoCusto,
        bool peloEanTributario)
    {
        var usouPrecoComSt = notaComSt && tabela.PrecoSecundario is > 0;
        var referencia = usouPrecoComSt ? tabela.PrecoSecundario!.Value : tabela.PrecoPrimario;
        var resultado = Classificar(custoNf, referencia);

        var partes = new List<string>
        {
            $"R$ {referencia.ToString("N2", PtBr)}",
            TextoDiferenca(custoNf, referencia),
            tabela.NomeImportacao
        };

        if (usouPrecoComSt)
            partes.Add("com ST");
        else if (tabela.PrecoSecundario is > 0)
            partes.Add($"2o R$ {tabela.PrecoSecundario.Value.ToString("N2", PtBr)}");

        if (!string.IsNullOrWhiteSpace(tabela.Loja))
            partes.Add(tabela.Loja);

        if (peloEanTributario)
            partes.Add("EAN trib");

        if (!string.IsNullOrWhiteSpace(avisoCusto))
            partes.Add(avisoCusto);

        return (resultado, string.Join(" · ", partes.Where(p => p.Length > 0)));
    }

    public static string TextoDiferenca(decimal custoNf, decimal precoTabela)
    {
        var diferenca = custoNf - precoTabela;
        if (Math.Abs(diferenca) <= ComparacaoPrecoValores.ToleranciaReais)
            return "igual";

        var percentual = precoTabela == 0 ? 0m : diferenca / precoTabela * 100m;
        var sinal = diferenca > 0 ? "+" : "-";
        return $"{sinal}R$ {Math.Abs(diferenca).ToString("N2", PtBr)} ({sinal}{Math.Abs(percentual).ToString("N1", PtBr)}%)";
    }

    private static string ChaveValida(string? ean) =>
        EanNormalizer.EhValido(ean) ? EanNormalizer.ChaveComparacao(ean) : string.Empty;
}
