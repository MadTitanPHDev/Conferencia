namespace ConferenciaNFs.ViewModels;

public sealed class StatusFiltroOpcao : ViewModelBase
{
    private bool _estaAtivo = true;
    private readonly Action _aoAlterar;

    public StatusFiltroOpcao(string status, string rotulo, Action aoAlterar)
    {
        Status = status;
        Rotulo = rotulo;
        _aoAlterar = aoAlterar;
    }

    public string Status { get; }
    public string Rotulo { get; }

    public bool EstaAtivo
    {
        get => _estaAtivo;
        set
        {
            if (!SetProperty(ref _estaAtivo, value))
                return;

            _aoAlterar();
        }
    }
}
