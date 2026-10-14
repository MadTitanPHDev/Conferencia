namespace ConferenciaNFs.Models;

public sealed class LojaAmareloDistribuidora
{
    public string ApelidoLoja { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public sealed class LojaPendenciaNota
{
    public string ApelidoLoja { get; set; } = string.Empty;
    public string NumNota { get; set; } = string.Empty;
    public string NomeForn { get; set; } = string.Empty;
    public string StatusConferencia { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;

    public bool TemObservacao => !string.IsNullOrWhiteSpace(Observacao);

    public string TextoTitulo
    {
        get
        {
            var status = StatusConferencia == StatusConferenciaValues.Amarelo ? "Amarelo" : "Branco";
            var fornecedor = string.IsNullOrWhiteSpace(NomeForn) ? status : NomeForn.Trim();
            if (fornecedor.Length > 18)
                fornecedor = fornecedor[..16] + "…";
            return $"{NumNota} · {fornecedor}";
        }
    }

    public string TextoObservacao
    {
        get
        {
            if (!TemObservacao)
                return "Sem observação";
            var obs = Observacao.Trim().ReplaceLineEndings(" ");
            return obs.Length <= 72 ? obs : obs[..70] + "…";
        }
    }
}

public sealed class LojaPendencia
{
    private const int LimitePreviaAmarelo = 3;
    public const int LimiteNotasHover = 4;

    public string ApelidoLoja { get; set; } = string.Empty;
    public int Pendencias { get; set; }
    public int QtdAmarelo { get; set; }
    public int NumeroOrdem { get; set; } = 9999;
    public string NomeExibicao { get; set; } = string.Empty;
    public IReadOnlyList<LojaAmareloDistribuidora> AmarelosPorDistribuidora { get; set; } = [];
    public IReadOnlyList<LojaPendenciaNota> NotasPendentes { get; set; } = [];
    public int NotasPendentesOcultas { get; set; }

    public bool PossuiOrdemCadastrada => NumeroOrdem < 9999;

    public bool TemPendencias => Pendencias > 0;

    public int QtdBranco => Math.Max(0, Pendencias - QtdAmarelo);

    public string TituloCard => PossuiOrdemCadastrada
        ? $"{NumeroOrdem} · {NomeExibicao}"
        : ApelidoLoja;

    public string TextoPendentes => Pendencias switch
    {
        0 => "Nenhuma pendente",
        1 => "1 pendente",
        _ => $"{Pendencias} pendentes"
    };

    public string TextoHoverResumo
    {
        get
        {
            if (!TemPendencias)
                return "Loja conferida. Clique para reabrir.";

            var partes = new List<string>();
            if (QtdBranco > 0)
                partes.Add(QtdBranco == 1 ? "1 em branco" : $"{QtdBranco} em branco");
            if (QtdAmarelo > 0)
                partes.Add(QtdAmarelo == 1 ? "1 amarela" : $"{QtdAmarelo} amarelas");
            return string.Join(" · ", partes);
        }
    }

    public bool TemPreviaAmarelo => QtdAmarelo > 0 && AmarelosPorDistribuidora.Count > 0;

    public bool TemNotasPendentes => NotasPendentes.Count > 0;

    public bool TemMaisNotasPendentes => NotasPendentesOcultas > 0;

    public string TextoMaisNotasPendentes =>
        NotasPendentesOcultas == 1 ? "+1 nota" : $"+{NotasPendentesOcultas} notas";

    public string TextoAmareloDistribuidoras
    {
        get
        {
            if (AmarelosPorDistribuidora.Count == 0)
                return string.Empty;

            var top = AmarelosPorDistribuidora.Take(LimitePreviaAmarelo).ToList();
            var partes = top.Select(x => $"{EncurtarNome(x.Nome)} {x.Quantidade}");
            var texto = string.Join(" · ", partes);
            var resto = AmarelosPorDistribuidora.Count - top.Count;
            return resto > 0 ? $"{texto} · +{resto}" : texto;
        }
    }

    public string TextoToolTipCard
    {
        get
        {
            if (!TemPendencias)
                return $"{TituloCard}\n{TextoHoverResumo}";

            var texto = $"{TituloCard}\n{TextoPendentes}\n{TextoHoverResumo}";
            if (TemPreviaAmarelo)
                texto += $"\nAmarelo: {TextoAmareloDistribuidoras}";
            foreach (var nota in NotasPendentes.Take(LimiteNotasHover))
            {
                texto += $"\n{nota.TextoTitulo}";
                if (nota.TemObservacao)
                    texto += $" — {nota.TextoObservacao}";
            }
            if (TemMaisNotasPendentes)
                texto += $"\n{TextoMaisNotasPendentes}";
            return texto;
        }
    }

    private static string EncurtarNome(string nome)
    {
        var limpo = (nome ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(limpo))
            return "Sem nome";
        return limpo.Length <= 22 ? limpo : limpo[..20] + "…";
    }
}
