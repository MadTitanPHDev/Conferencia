using System.IO;
using System.Text.Json;

namespace ConferenciaNFs.Infrastructure;

public sealed class AppSettingsStore
{
    public static AppSettingsStore Instance { get; } = new();

    private const string NomeArquivo = "app-settings.json";
    private const string NomeExemplo = "app-settings.example.json";

    private static readonly JsonSerializerOptions OpcoesJson = new() { WriteIndented = true };

    private readonly string _settingsPath;

    private AppSettingsStore()
    {
        _settingsPath = GarantirArquivo();
        Data = Carregar();
    }

    public AppSettings Data { get; private set; }

    /// <summary>
    /// Arquivo em uso. Fica fora da pasta de instalacao porque o Velopack troca essa pasta
    /// inteira a cada atualizacao, o que apagava as configuracoes.
    /// </summary>
    public string CaminhoArquivo => _settingsPath;

    public static string PastaDados => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ConferenciaNFs");

    public void Salvar() => SalvarEm(_settingsPath, Data);

    public void Recarregar()
    {
        Data = Carregar();
    }

    private static string GarantirArquivo()
    {
        var destino = Path.Combine(PastaDados, NomeArquivo);

        try
        {
            if (File.Exists(destino))
                return destino;

            Directory.CreateDirectory(PastaDados);

            // Primeira execucao no local novo: herda o arquivo que estava na pasta do app e,
            // se nao houver nenhum, parte do exemplo distribuido junto com o pacote.
            string[] origens =
            [
                Path.Combine(AppContext.BaseDirectory, NomeArquivo),
                Path.Combine(AppContext.BaseDirectory, NomeExemplo)
            ];

            foreach (var origem in origens)
            {
                if (!File.Exists(origem))
                    continue;

                File.Copy(origem, destino);
                break;
            }
        }
        catch
        {
            // Sem acesso a %AppData%: o app ainda abre, mas com os padroes em memoria.
        }

        return destino;
    }

    private AppSettings Carregar()
    {
        var settings = Ler(_settingsPath) ?? new AppSettings();

        if (CompletarComExemplo(settings))
            SalvarEm(_settingsPath, settings);

        return settings;
    }

    /// <summary>
    /// Preenche connection string vazia com a do exemplo, para que um arquivo gravado antes
    /// dessas chaves existirem nao impeca a abertura do app.
    /// </summary>
    private static bool CompletarComExemplo(AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.ConnectionString)
            && !string.IsNullOrWhiteSpace(settings.MysqlConnectionString))
            return false;

        var exemplo = Ler(Path.Combine(AppContext.BaseDirectory, NomeExemplo));
        if (exemplo is null)
            return false;

        var alterou = false;

        if (string.IsNullOrWhiteSpace(settings.ConnectionString)
            && !string.IsNullOrWhiteSpace(exemplo.ConnectionString))
        {
            settings.ConnectionString = exemplo.ConnectionString;
            alterou = true;
        }

        if (string.IsNullOrWhiteSpace(settings.MysqlConnectionString)
            && !string.IsNullOrWhiteSpace(exemplo.MysqlConnectionString))
        {
            settings.MysqlConnectionString = exemplo.MysqlConnectionString;
            alterou = true;
        }

        return alterou;
    }

    private static AppSettings? Ler(string caminho)
    {
        try
        {
            return File.Exists(caminho)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(caminho))
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static void SalvarEm(string caminho, AppSettings settings)
    {
        try
        {
            var pasta = Path.GetDirectoryName(caminho);
            if (!string.IsNullOrEmpty(pasta))
                Directory.CreateDirectory(pasta);

            File.WriteAllText(caminho, JsonSerializer.Serialize(settings, OpcoesJson));
        }
        catch
        {
            // Preferencias nao criticas.
        }
    }
}

public sealed class AppSettings
{
    public bool JanelaFixadaNoTopo { get; set; }
    public bool TemaEscuro { get; set; }

    /// <summary>
    /// Connection string Npgsql. Ex.: Host=127.0.0.1;Port=5432;Database=conferencia_nfs_1;Username=conferencia;Password=...
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Connection string MySqlConnector (VSM, somente leitura).
    /// Ex.: Server=192.168.21.2;Port=33021;Database=myouro;User ID=compras;Password=...
    /// </summary>
    public string MysqlConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Duplo clique na conferencia: true copia o numero da NF; false abre os itens.
    /// </summary>
    public bool DuploCliqueCopiaNumero { get; set; }

    /// <summary>
    /// Ultimo intervalo De/Ate escolhido, em dd/MM/yyyy. Restaurado na abertura somente
    /// enquanto a data final for o dia de hoje; depois disso o app volta ao padrao (ontem).
    /// </summary>
    public string UltimoIntervaloInicio { get; set; } = string.Empty;

    public string UltimoIntervaloFim { get; set; } = string.Empty;
}
