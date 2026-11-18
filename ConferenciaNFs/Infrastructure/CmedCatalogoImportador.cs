using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

public sealed record CmedImportacaoResultado(
    IReadOnlyList<CmedProduto> Produtos,
    int TotalParesEan,
    IReadOnlyList<string> Aliquotas,
    string Publicacao,
    string FontePmc,
    string FontePmvg,
    IReadOnlyList<string> Avisos);

public static class CmedCatalogoImportador
{
    private static readonly Regex PublicacaoRegex = new(
        @"Publicada em\s+(\d{2}/\d{2}/\d{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DataNoNomeRegex = new(
        @"lista_(?:pmc|pmvg)_(\d{4})(\d{2})(\d{2})_",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, string> CamposFixos = new(StringComparer.Ordinal)
    {
        ["SUBSTANCIA"] = "Substancia",
        ["CNPJ"] = "Cnpj",
        ["LABORATORIO"] = "Laboratorio",
        ["CODIGO GGREM"] = "Ggrem",
        ["REGISTRO"] = "Registro",
        ["EAN 1"] = "Ean1",
        ["EAN 2"] = "Ean2",
        ["EAN 3"] = "Ean3",
        ["PRODUTO"] = "Produto",
        ["APRESENTACAO"] = "Apresentacao",
        ["CLASSE TERAPEUTICA"] = "Classe",
        ["TIPO DE PRODUTO (STATUS DO PRODUTO)"] = "Tipo",
        ["REGIME DE PRECO"] = "Regime",
        ["RESTRICAO HOSPITALAR"] = "Restricao",
        ["CAP"] = "Cap",
        ["CONFAZ 87"] = "Confaz87",
        ["TARJA"] = "Tarja"
    };

    private const string PrefixoDestinacao = "DESTINACAO COMERCIAL";

    public static CmedImportacaoResultado Ler(
        string caminhoPmc,
        string caminhoPmvg,
        IProgress<string>? progresso = null)
    {
        if (!File.Exists(caminhoPmc))
            throw new FileNotFoundException("Planilha PMC nao encontrada.", caminhoPmc);
        if (!File.Exists(caminhoPmvg))
            throw new FileNotFoundException("Planilha PMVG nao encontrada.", caminhoPmvg);

        progresso?.Report("Lendo planilha PMC...");
        var pmc = LerPlanilha(caminhoPmc, "PMC", progresso);

        progresso?.Report("Lendo planilha PMVG...");
        var pmvg = LerPlanilha(caminhoPmvg, "PMVG", progresso);

        var avisos = new List<string>();
        if (pmc.Produtos.Count == 0)
            throw new InvalidOperationException("Nenhuma linha de dados na planilha PMC.");
        if (pmvg.Produtos.Count == 0)
            throw new InvalidOperationException("Nenhuma linha de dados na planilha PMVG.");
        if (pmc.Aliquotas.Count == 0)
            throw new InvalidOperationException("Nenhuma coluna de preco PF reconhecida.");

        var aliquotas = new List<string>(pmc.Aliquotas);
        foreach (var aliquota in pmvg.Aliquotas)
        {
            if (!aliquotas.Contains(aliquota, StringComparer.Ordinal))
                aliquotas.Add(aliquota);
        }

        var unificados = new Dictionary<string, CmedProduto>(StringComparer.Ordinal);
        foreach (var produto in pmc.Produtos)
            unificados[Chave(produto)] = produto;

        foreach (var produto in pmvg.Produtos)
        {
            var chave = Chave(produto);
            if (!unificados.TryGetValue(chave, out var existente))
            {
                unificados[chave] = produto;
                continue;
            }

            var destino = CmedPrecos.Desserializar(existente.PrecosJson);
            var origem = CmedPrecos.Desserializar(produto.PrecosJson);
            foreach (var (aliquota, valor) in origem.Pf)
                destino.Pf[aliquota] = valor;
            foreach (var (aliquota, valor) in origem.Pmc)
                destino.Pmc[aliquota] = valor;
            foreach (var (aliquota, valor) in origem.Pmvg)
                destino.Pmvg[aliquota] = valor;

            existente.PrecosJson = destino.Serializar();
            existente.LinhaOrigem = $"{existente.LinhaOrigem}+{produto.LinhaOrigem}";
        }

        var produtos = unificados.Values.ToList();
        var pares = produtos.Sum(p => p.Eans().Distinct(StringComparer.Ordinal).Count());
        var publicacao = pmc.Publicacao ?? pmvg.Publicacao ?? DataDoNome(caminhoPmc) ?? "desconhecida";

        progresso?.Report($"Catalogo pronto: {produtos.Count} produtos, {pares} EANs.");

        return new CmedImportacaoResultado(
            produtos,
            pares,
            aliquotas,
            publicacao,
            Path.GetFileName(caminhoPmc),
            Path.GetFileName(caminhoPmvg),
            avisos);
    }

    private sealed record PlanilhaLida(
        List<CmedProduto> Produtos,
        List<string> Aliquotas,
        string? Publicacao);

    private static PlanilhaLida LerPlanilha(string caminho, string sigla, IProgress<string>? progresso)
    {
        using var livro = new XLWorkbook(caminho);
        var publicacao = LerPublicacao(livro);
        var aba = LocalizarAbaDados(livro);
        var ultimaColuna = aba.LastColumnUsed()?.ColumnNumber() ?? 0;
        var ultimaLinha = aba.LastRowUsed()?.RowNumber() ?? 0;
        if (ultimaColuna == 0 || ultimaLinha < 2)
            return new PlanilhaLida([], [], publicacao);

        var campos = new Dictionary<string, int>(StringComparer.Ordinal);
        var colunasPreco = new List<(int Coluna, string Tipo, string Rotulo)>();
        var aliquotas = new List<string>();

        for (var coluna = 1; coluna <= ultimaColuna; coluna++)
        {
            var titulo = CmedTexto.NormalizarCabecalho(aba.Cell(1, coluna).GetString());
            if (titulo.Length == 0)
                continue;

            if (CamposFixos.TryGetValue(titulo, out var campo))
                campos[campo] = coluna;

            if (titulo.StartsWith(PrefixoDestinacao, StringComparison.Ordinal))
            {
                campos["Destinacao"] = coluna;
                continue;
            }

            if (titulo.StartsWith("PF ", StringComparison.Ordinal))
            {
                var rotulo = CmedTexto.RotuloPreco(titulo, "PF ");
                colunasPreco.Add((coluna, CmedPrecos.TipoPf, rotulo));
                aliquotas.Add(rotulo);
            }
            else if (titulo.StartsWith("PMC ", StringComparison.Ordinal))
            {
                colunasPreco.Add((coluna, CmedPrecos.TipoPmc, CmedTexto.RotuloPreco(titulo, "PMC ")));
            }
            else if (titulo.StartsWith("PMVG ", StringComparison.Ordinal))
            {
                colunasPreco.Add((coluna, CmedPrecos.TipoPmvg, CmedTexto.RotuloPreco(titulo, "PMVG ")));
            }
        }

        var produtos = new List<CmedProduto>(Math.Max(0, ultimaLinha - 1));
        for (var linha = 2; linha <= ultimaLinha; linha++)
        {
            var produto = new CmedProduto
            {
                LinhaOrigem = $"{sigla}:{linha}",
                Registro = Texto(aba, linha, campos, "Registro") ?? string.Empty,
                Ggrem = Texto(aba, linha, campos, "Ggrem") ?? string.Empty,
                Ean1 = CmedTexto.NormalizarEan(Texto(aba, linha, campos, "Ean1")),
                Ean2 = CmedTexto.NormalizarEan(Texto(aba, linha, campos, "Ean2")),
                Ean3 = CmedTexto.NormalizarEan(Texto(aba, linha, campos, "Ean3")),
                Substancia = Texto(aba, linha, campos, "Substancia") ?? string.Empty,
                Laboratorio = Texto(aba, linha, campos, "Laboratorio") ?? string.Empty,
                Cnpj = Texto(aba, linha, campos, "Cnpj") ?? string.Empty,
                Produto = Texto(aba, linha, campos, "Produto") ?? string.Empty,
                Apresentacao = Texto(aba, linha, campos, "Apresentacao") ?? string.Empty,
                Classe = Texto(aba, linha, campos, "Classe") ?? string.Empty,
                Tipo = Texto(aba, linha, campos, "Tipo") ?? string.Empty,
                Regime = Texto(aba, linha, campos, "Regime") ?? string.Empty,
                Restricao = Texto(aba, linha, campos, "Restricao") ?? string.Empty,
                Tarja = Texto(aba, linha, campos, "Tarja") ?? string.Empty,
                Destinacao = Texto(aba, linha, campos, "Destinacao") ?? string.Empty,
                Cap = Texto(aba, linha, campos, "Cap") ?? string.Empty,
                Confaz87 = Texto(aba, linha, campos, "Confaz87") ?? string.Empty
            };

            if (produto.Produto.Length == 0
                && produto.Registro.Length == 0
                && produto.Ean1 is null
                && produto.Ean2 is null)
            {
                continue;
            }

            var precos = new CmedPrecos();
            foreach (var (coluna, tipo, rotulo) in colunasPreco)
            {
                var valor = Preco(aba.Cell(linha, coluna));
                if (!valor.HasValue || rotulo.Length == 0)
                    continue;

                switch (tipo)
                {
                    case CmedPrecos.TipoPf:
                        precos.Pf[rotulo] = valor.Value;
                        break;
                    case CmedPrecos.TipoPmc:
                        precos.Pmc[rotulo] = valor.Value;
                        break;
                    case CmedPrecos.TipoPmvg:
                        precos.Pmvg[rotulo] = valor.Value;
                        break;
                }
            }

            produto.PrecosJson = precos.Serializar();
            produto.Busca = CmedTexto.Busca(
                produto.Produto,
                produto.Substancia,
                produto.Laboratorio,
                produto.Apresentacao,
                produto.Registro,
                produto.Ean1,
                produto.Ean2,
                produto.Ean3);
            produtos.Add(produto);

            if (linha % 5000 == 0)
                progresso?.Report($"{sigla}: {linha} linhas lidas...");
        }

        return new PlanilhaLida(produtos, aliquotas, publicacao);
    }

    private static IXLWorksheet LocalizarAbaDados(XLWorkbook livro)
    {
        foreach (var aba in livro.Worksheets)
        {
            var primeira = aba.FirstRowUsed();
            if (primeira is null)
                continue;

            var titulos = new HashSet<string>(StringComparer.Ordinal);
            foreach (var celula in primeira.CellsUsed())
                titulos.Add(CmedTexto.NormalizarCabecalho(celula.GetString()));

            if (titulos.Contains("REGISTRO") && titulos.Contains("EAN 1") && titulos.Contains("PRODUTO"))
                return aba;
        }

        return livro.Worksheets.Last();
    }

    private static string? LerPublicacao(XLWorkbook livro)
    {
        var aba = livro.Worksheets.First();
        var ultimaLinha = Math.Min(aba.LastRowUsed()?.RowNumber() ?? 0, 40);
        var ultimaColuna = Math.Min(aba.LastColumnUsed()?.ColumnNumber() ?? 0, 20);

        for (var linha = 1; linha <= ultimaLinha; linha++)
        {
            for (var coluna = 1; coluna <= ultimaColuna; coluna++)
            {
                var texto = aba.Cell(linha, coluna).GetString();
                var achado = PublicacaoRegex.Match(texto);
                if (achado.Success)
                    return achado.Groups[1].Value;
            }
        }

        return null;
    }

    private static string? DataDoNome(string caminho)
    {
        var achado = DataNoNomeRegex.Match(Path.GetFileName(caminho));
        return achado.Success
            ? $"{achado.Groups[3].Value}/{achado.Groups[2].Value}/{achado.Groups[1].Value}"
            : null;
    }

    private static string? Texto(IXLWorksheet aba, int linha, IReadOnlyDictionary<string, int> campos, string campo)
    {
        if (!campos.TryGetValue(campo, out var coluna))
            return null;

        var celula = aba.Cell(linha, coluna);
        if (celula.IsEmpty())
            return null;

        var texto = celula.DataType == XLDataType.Number
            ? celula.GetDouble().ToString("0", CultureInfo.InvariantCulture)
            : celula.GetString();

        texto = texto.Replace('\u00A0', ' ').Trim();
        return texto.Length == 0 ? null : texto;
    }

    private static decimal? Preco(IXLCell celula)
    {
        if (celula.IsEmpty())
            return null;

        if (celula.DataType == XLDataType.Number)
            return CmedNumero.Converter(celula.GetDouble().ToString("0.########", CultureInfo.InvariantCulture));

        return CmedNumero.Converter(celula.GetString());
    }

    private static string Chave(CmedProduto produto) => string.Join(
        '\u001F',
        produto.Registro,
        produto.Ggrem,
        produto.Ean1 ?? string.Empty,
        produto.Produto.ToUpperInvariant(),
        produto.Apresentacao.ToUpperInvariant());
}
