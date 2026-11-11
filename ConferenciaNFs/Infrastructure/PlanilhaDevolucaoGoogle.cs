using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Lê a coluna A de cada aba da planilha de devoluções. A chave fica em %AppData%.
/// </summary>
public static class PlanilhaDevolucaoGoogle
{
    public const string PlanilhaId = "1ho0Fweiz9GW8yqrH3wyD2FVr46ExpvJcHBULNsjsNsU";

    private static readonly HttpClient Http = new();
    private static string? _token;
    private static DateTimeOffset _tokenExpira;

    public static string? EncontrarChave()
    {
        var pasta = AppSettingsStore.PastaDados;
        if (!Directory.Exists(pasta))
            return null;

        foreach (var arquivo in Directory.GetFiles(pasta, "*.json"))
        {
            var nome = Path.GetFileName(arquivo);
            if (nome.Equals("app-settings.json", StringComparison.OrdinalIgnoreCase)
                || nome.Contains("example", StringComparison.OrdinalIgnoreCase))
                continue;

            string texto;
            try
            {
                texto = File.ReadAllText(arquivo);
            }
            catch (IOException)
            {
                continue;
            }

            if (texto.Contains("\"private_key\"", StringComparison.Ordinal)
                && texto.Contains("\"client_email\"", StringComparison.Ordinal))
                return arquivo;
        }

        return null;
    }

    public static async Task<IReadOnlyDictionary<int, HashSet<string>>?> LerNumerosPorLojaAsync(
        CancellationToken cancellationToken = default)
    {
        var chave = EncontrarChave();
        if (chave is null)
            return null;

        var token = await ObterTokenAsync(chave, cancellationToken);
        if (token is null)
            return null;

        using var metaPedido = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{PlanilhaId}?fields=sheets.properties.title");
        metaPedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var metaResposta = await Http.SendAsync(metaPedido, cancellationToken);
        if (!metaResposta.IsSuccessStatusCode)
            return null;

        using var meta = JsonDocument.Parse(await metaResposta.Content.ReadAsStringAsync(cancellationToken));
        var abas = meta.RootElement.GetProperty("sheets").EnumerateArray()
            .Select(aba => aba.GetProperty("properties").GetProperty("title").GetString() ?? string.Empty)
            .Where(titulo => titulo.Length > 0)
            .ToList();
        if (abas.Count == 0)
            return null;

        var consulta = string.Join("&", abas.Select(aba =>
            "ranges=" + Uri.EscapeDataString($"'{aba.Replace("'", "''")}'!A:A")));
        using var valoresPedido = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://sheets.googleapis.com/v4/spreadsheets/{PlanilhaId}/values:batchGet?{consulta}");
        valoresPedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var valoresResposta = await Http.SendAsync(valoresPedido, cancellationToken);
        if (!valoresResposta.IsSuccessStatusCode)
            return null;

        using var valores = JsonDocument.Parse(await valoresResposta.Content.ReadAsStringAsync(cancellationToken));
        var lidas = new List<(string Aba, IEnumerable<string> ColunaA)>();
        var faixas = valores.RootElement.GetProperty("valueRanges").EnumerateArray().ToList();
        for (var i = 0; i < abas.Count && i < faixas.Count; i++)
        {
            var celulas = new List<string>();
            if (faixas[i].TryGetProperty("values", out var linhas))
            {
                foreach (var linha in linhas.EnumerateArray())
                {
                    if (linha.GetArrayLength() == 0)
                        continue;
                    celulas.Add(linha[0].ToString());
                }
            }

            lidas.Add((abas[i], celulas));
        }

        return CruzamentoPlanilhaDevolucao.IndexarAbas(lidas);
    }

    private static async Task<string?> ObterTokenAsync(string caminhoChave, CancellationToken cancellationToken)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _tokenExpira)
            return _token;

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(caminhoChave, cancellationToken));
        var email = doc.RootElement.GetProperty("client_email").GetString();
        var pem = doc.RootElement.GetProperty("private_key").GetString();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pem))
            return null;

        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var header = Base64Url(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"typ\":\"JWT\"}"));
        var claim = Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["iss"] = email,
            ["scope"] = "https://www.googleapis.com/auth/spreadsheets.readonly",
            ["aud"] = "https://oauth2.googleapis.com/token",
            ["iat"] = agora,
            ["exp"] = agora + 3600
        })));
        using var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        var assinatura = Base64Url(rsa.SignData(
            Encoding.UTF8.GetBytes(header + "." + claim),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1));

        using var resposta = await Http.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = header + "." + claim + "." + assinatura
            }),
            cancellationToken);
        if (!resposta.IsSuccessStatusCode)
            return null;

        using var token = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(cancellationToken));
        _token = token.RootElement.GetProperty("access_token").GetString();
        _tokenExpira = DateTimeOffset.UtcNow.AddMinutes(50);
        return _token;
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
