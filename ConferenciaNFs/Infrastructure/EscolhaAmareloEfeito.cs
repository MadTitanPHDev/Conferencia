using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// O que a escolha do Amarelo faz com a nota. Cancelar não pinta.
/// Só para a conferência pinta sem pendência. Enviar cria a pendência e pinta.
/// </summary>
public static class EscolhaAmareloEfeito
{
    public static bool DevePintar(EscolhaAmarelo escolha)
        => escolha != EscolhaAmarelo.Cancelar;

    public static bool DeveCriarPendencia(EscolhaAmarelo escolha)
        => escolha == EscolhaAmarelo.EnviarComprador;
}
