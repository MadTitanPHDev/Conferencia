using ConferenciaNFs.Models;

namespace ConferenciaNFs.ViewModels;

public sealed class ImportacaoPrecoOpcao : ViewModelBase
{
    private bool _estaSelecionada;

    public ImportacaoPrecoOpcao(PrecoTabelaImportacao importacao)
    {
        Id = importacao.Id;
        Nome = importacao.Nome;
        QuantidadeItens = importacao.QuantidadeItens;
    }

    public long Id { get; }
    public string Nome { get; }
    public int QuantidadeItens { get; }
    public string Rotulo => $"{Nome} ({QuantidadeItens})";

    public bool EstaSelecionada
    {
        get => _estaSelecionada;
        set => SetProperty(ref _estaSelecionada, value);
    }
}
