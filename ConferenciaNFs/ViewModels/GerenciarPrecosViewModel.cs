using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Microsoft.Win32;

namespace ConferenciaNFs.ViewModels;

public sealed class GerenciarPrecosViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private PrecoTabelaImportacao? _selecionada;
    private string _novoNome = string.Empty;
    private string _mensagem = "Importe uma planilha (EAN, nome, preco, preco 2) e depois exclua a anterior se for substituir.";
    private bool _isCarregando;

    public GerenciarPrecosViewModel(NotaFiscalRepository repository)
    {
        _repository = repository;
        Importacoes = new ObservableCollection<PrecoTabelaImportacao>();
        ImportarCommand = new AsyncRelayCommand(_ => ImportarAsync(), _ => PodeImportar);
        ExcluirCommand = new AsyncRelayCommand(_ => ExcluirAsync(), _ => Selecionada is not null && !IsCarregando);
        AtualizarCommand = new AsyncRelayCommand(_ => CarregarAsync());
        _ = CarregarAsync();
    }

    public ObservableCollection<PrecoTabelaImportacao> Importacoes { get; }

    public PrecoTabelaImportacao? Selecionada
    {
        get => _selecionada;
        set
        {
            if (!SetProperty(ref _selecionada, value))
                return;
            ((AsyncRelayCommand)ExcluirCommand).RaiseCanExecuteChanged();
        }
    }

    public string NovoNome
    {
        get => _novoNome;
        set
        {
            if (!SetProperty(ref _novoNome, value))
                return;
            ((AsyncRelayCommand)ImportarCommand).RaiseCanExecuteChanged();
        }
    }

    public string Mensagem
    {
        get => _mensagem;
        private set => SetProperty(ref _mensagem, value);
    }

    public bool IsCarregando
    {
        get => _isCarregando;
        private set
        {
            if (!SetProperty(ref _isCarregando, value))
                return;
            ((AsyncRelayCommand)ImportarCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)ExcluirCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand ImportarCommand { get; }
    public ICommand ExcluirCommand { get; }
    public ICommand AtualizarCommand { get; }

    private bool PodeImportar =>
        !IsCarregando && !string.IsNullOrWhiteSpace(NovoNome);

    private async Task CarregarAsync()
    {
        try
        {
            IsCarregando = true;
            var lista = await _repository.ListarPrecosImportacoesAsync();
            Importacoes.Clear();
            foreach (var item in lista)
                Importacoes.Add(item);
            Selecionada = Importacoes.FirstOrDefault();
            Mensagem = Importacoes.Count == 0
                ? "Nenhuma tabela importada. Informe um nome e escolha o Excel."
                : $"{Importacoes.Count} tabela(s) compartilhada(s) no Postgres.";
        }
        catch (Exception ex)
        {
            Mensagem = "Nao foi possivel carregar as tabelas.";
            MessageBox.Show($"Erro ao carregar tabelas de preco:\n{ex.Message}", "Tabelas de preco",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private async Task ImportarAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar planilha de precos",
            Filter = "Excel (*.xlsx;*.xls)|*.xlsx;*.xls"
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsCarregando = true;
            Mensagem = "Lendo planilha...";
            var itens = await Task.Run(() => PrecoTabelaExcelImportador.Ler(dialog.FileName));
            var criada = await _repository.ImportarTabelaPrecosAsync(NovoNome, itens);
            NovoNome = string.Empty;
            await CarregarAsync();
            Mensagem = $"'{criada.Nome}' importada com {criada.QuantidadeItens} item(ns).";
        }
        catch (Exception ex)
        {
            Mensagem = "Falha na importacao.";
            MessageBox.Show(ex.Message, "Tabelas de preco", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private async Task ExcluirAsync()
    {
        if (Selecionada is null)
            return;

        var confirmar = MessageBox.Show(
            $"Excluir a tabela '{Selecionada.Nome}'? Todos os PCs deixam de ve-la.",
            "Tabelas de preco",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmar != MessageBoxResult.Yes)
            return;

        try
        {
            IsCarregando = true;
            await _repository.ExcluirPrecoImportacaoAsync(Selecionada.Id);
            await CarregarAsync();
            Mensagem = "Tabela excluida.";
        }
        catch (Exception ex)
        {
            Mensagem = "Nao foi possivel excluir.";
            MessageBox.Show(ex.Message, "Tabelas de preco", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }
}
