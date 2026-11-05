using ConferenciaNFs.Infrastructure;

namespace ConferenciaNFs.Models;

public sealed class PendenciaCompraResumo
{
    public long Id { get; set; }
    public long NotaFiscalId { get; set; }
    public long PessoaId { get; set; }
    public string PessoaNome { get; set; } = string.Empty;
    public string EnviadoPorNome { get; set; } = string.Empty;
    public string ApelidoLoja { get; set; } = string.Empty;
    public string NumNota { get; set; } = string.Empty;
    public string DiaConferencia { get; set; } = string.Empty;
    public int CodCompra { get; set; }
    public string StatusVsm { get; set; } = string.Empty;
    public string ReabertaVsm { get; set; } = string.Empty;
    public string SituacaoVsmRotulo => StatusCompraVsm.Rotulo(StatusVsm, ReabertaVsm);
    public string NomeForn { get; set; } = string.Empty;
    public string NomeDistribuidora { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;
    public string StatusPendencia { get; set; } = string.Empty;
    public string CriadaEm { get; set; } = string.Empty;
    public string RespondidaEm { get; set; } = string.Empty;
    public string ItensTexto { get; set; } = string.Empty;
    public List<PendenciaCompraItem> Itens { get; set; } = [];
}
