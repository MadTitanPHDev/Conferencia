namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Sync em massa do VSM e local deste PC. Nao usa o Postgres: cada maquina decide sozinha.
/// </summary>
public static class PoliticaSyncVsm
{
    public static bool DeveSincronizar(bool mysqlConfigurado, bool nesteComputador)
        => mysqlConfigurado && nesteComputador;
}
