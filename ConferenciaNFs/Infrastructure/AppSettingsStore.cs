using System.IO;
using System.Text.Json;

namespace ConferenciaNFs.Infrastructure;

public sealed class AppSettingsStore
{
    public static AppSettingsStore Instance { get; } = new();

    private readonly string _settingsPath;

    private AppSettingsStore()
    {
        _settingsPath = Path.Combine(AppContext.BaseDirectory, "app-settings.json");
        Data = Carregar();
    }

    public AppSettings Data { get; private set; }

    public void Salvar()
    {
        try
        {
            var json = JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Preferencias nao criticas.
        }
    }

    public void Recarregar()
    {
        Data = Carregar();
    }

    private AppSettings Carregar()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new AppSettings();

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
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
}
