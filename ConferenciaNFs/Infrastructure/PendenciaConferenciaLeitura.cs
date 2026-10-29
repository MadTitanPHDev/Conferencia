using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Leitura da divergência enviada. A lista só mostra pendência aberta.
/// O período da conferência não entra nessa decisão.
/// </summary>
public static class PendenciaConferenciaLeitura
{
    public static bool ApareceNaLista(string? statusPendencia) =>
        string.Equals(
            statusPendencia?.Trim(),
            StatusPendenciaCompraValues.Aberta,
            StringComparison.OrdinalIgnoreCase);

    public static bool ApareceComoConcluida(string? statusPendencia)
    {
        var status = statusPendencia?.Trim() ?? string.Empty;
        return string.Equals(status, StatusPendenciaCompraValues.Ficou, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, StatusPendenciaCompraValues.Devolvida, StringComparison.OrdinalIgnoreCase);
    }

    public static string JuntarObservacao(string? observacaoNota, string? observacaoPendencia)
    {
        var nota = (observacaoNota ?? string.Empty).Trim();
        var pendencia = (observacaoPendencia ?? string.Empty).Trim();
        if (pendencia.Length == 0)
            return nota;
        if (nota.Length == 0)
            return pendencia;
        if (nota.Contains(pendencia, StringComparison.OrdinalIgnoreCase))
            return nota;
        return $"{nota} | {pendencia}";
    }

    public static string TextoItens(IEnumerable<PendenciaCompraItem> itens)
    {
        var nomes = itens
            .OrderBy(i => i.Sequencia)
            .Select(i => string.IsNullOrWhiteSpace(i.NomeProd)
                ? $"Produto {i.CodProd}"
                : i.NomeProd.Trim())
            .Where(nome => nome.Length > 0);
        return string.Join(" · ", nomes);
    }

    public static void CopiarDestinoParaVisivel(NotaFiscal visivel, IEnumerable<NotaFiscal> grupo)
    {
        if (!string.IsNullOrWhiteSpace(visivel.DestinoPendencia))
            return;

        var fonte = grupo.FirstOrDefault(n =>
            n.Id != visivel.Id
            && !string.IsNullOrWhiteSpace(n.DestinoPendencia)
            && UnificacaoNotasConferencia.MesmaIdentidade(visivel, n)
            && string.Equals(
                n.StatusConferencia,
                visivel.StatusConferencia,
                StringComparison.OrdinalIgnoreCase));

        if (fonte is null)
            return;

        visivel.DestinoPendencia = fonte.DestinoPendencia;
        visivel.EnviadoPorPendencia = fonte.EnviadoPorPendencia;
        visivel.ItensPendencia = fonte.ItensPendencia;
    }
}
