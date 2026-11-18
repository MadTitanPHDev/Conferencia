using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ConferenciaNFs.Infrastructure;

public sealed record CmedFontes(string UrlPmc, string UrlPmvg, string? Publicacao);

/// <summary>
/// Acha e baixa as duas planilhas publicas da CMED. A pagina da ANVISA aponta
/// a lista vigente; os enderecos de 09/11/2026 cobrem a pagina fora do ar.
/// </summary>
public static partial class CmedDownload
{
    public const string PaginaPrecos =
        "https://www.gov.br/anvisa/pt-br/assuntos/medicamentos/cmed/precos/precos-de-medicamentos";

    public const string UrlPmcFallback =
        "https://www.gov.br/anvisa/pt-br/assuntos/medicamentos/cmed/precos/arquivos/lista_pmc_20261109_111901662.xlsx/@@download/file";

    public const string UrlPmvgFallback =
        "https://www.gov.br/anvisa/pt-br/assuntos/medicamentos/cmed/precos/arquivos/lista_pmvg_20261109_111901662.xlsx/@@download/file";

    private static readonly HttpClient Http = CriarCliente();

    [GeneratedRegex(@"href\s*=\s*[""']([^""']*lista_pmc[^""']*\.xlsx[^""']*)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex HrefPmcRegex();

    [GeneratedRegex(@"href\s*=\s*[""']([^""']*lista_pmvg[^""']*\.xlsx[^""']*)[""']", RegexOptions.IgnoreCase)]
    private static partial Regex HrefPmvgRegex();

    [GeneratedRegex(@"lista_(?:pmc|pmvg)_[^/?]+\.xlsx", RegexOptions.IgnoreCase)]
    private static partial Regex NomePlanilhaRegex();

    public static CmedFontes InterpretarPagina(string? html)
    {
        var texto = WebUtility.HtmlDecode(html ?? string.Empty);
        var pmc = Resolver(Primeiro(HrefPmcRegex(), texto)) ?? UrlPmcFallback;
        var pmvg = Resolver(Primeiro(HrefPmvgRegex(), texto)) ?? UrlPmvgFallback;
        return new CmedFontes(pmc, pmvg, PublicacaoDoNome(pmc));
    }

    public static string NomeArquivo(string url)
    {
        var achado = NomePlanilhaRegex().Match(url);
        return achado.Success ? achado.Value : "lista.xlsx";
    }

    public static async Task<CmedFontes> DescobrirAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var resposta = await Http.GetAsync(PaginaPrecos, cancellationToken);
            resposta.EnsureSuccessStatusCode();
            var html = await resposta.Content.ReadAsStringAsync(cancellationToken);
            return InterpretarPagina(html);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return InterpretarPagina(null);
        }
    }

    public static async Task<string> BaixarAsync(
        string url,
        string destino,
        IProgress<string>? progresso = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        var temporario = destino + ".part";
        var nome = Path.GetFileName(destino);
        progresso?.Report($"Baixando {nome}...");

        using var resposta = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        resposta.EnsureSuccessStatusCode();
        var total = resposta.Content.Headers.ContentLength ?? 0;

        await using (var entrada = await resposta.Content.ReadAsStreamAsync(cancellationToken))
        await using (var saida = new FileStream(temporario, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var buffer = new byte[256 * 1024];
            long lido = 0;
            int bloco;
            while ((bloco = await entrada.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await saida.WriteAsync(buffer.AsMemory(0, bloco), cancellationToken);
                lido += bloco;
                if (total > 0)
                    progresso?.Report($"Baixando {nome}: {lido * 100 / total}%");
            }
        }

        var tamanho = new FileInfo(temporario).Length;
        if (tamanho < 100_000 || !ParecePlanilha(temporario))
        {
            File.Delete(temporario);
            throw new InvalidOperationException(
                "A ANVISA nao devolveu a planilha. A pagina pode ter mudado ou o download foi bloqueado.");
        }

        File.Move(temporario, destino, overwrite: true);
        progresso?.Report($"{nome} baixado ({tamanho / 1048576.0:0.0} MB)");
        return destino;
    }

    private static bool ParecePlanilha(string caminho)
    {
        Span<byte> inicio = stackalloc byte[2];
        using var arquivo = File.OpenRead(caminho);
        return arquivo.Read(inicio) == 2 && inicio[0] == (byte)'P' && inicio[1] == (byte)'K';
    }

    private static string? Primeiro(Regex regex, string texto)
    {
        var achado = regex.Match(texto);
        return achado.Success ? achado.Groups[1].Value : null;
    }

    private static string? Resolver(string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
            return null;

        if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return href;

        if (href.StartsWith('/'))
            return "https://www.gov.br" + href;

        return new Uri(new Uri(PaginaPrecos), href).ToString();
    }

    private static string? PublicacaoDoNome(string url)
    {
        var achado = NomePlanilhaRegex().Match(url);
        if (!achado.Success || achado.Value.Length < 18)
            return null;

        var digitos = achado.Value.Substring(10, 8);
        if (digitos.Any(c => !char.IsDigit(c)))
            return null;

        return $"{digitos[6..8]}/{digitos[4..6]}/{digitos[0..4]}";
    }

    private static HttpClient CriarCliente()
    {
        var cliente = new HttpClient { Timeout = TimeSpan.FromMinutes(4) };
        cliente.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36");
        cliente.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9");
        return cliente;
    }
}
