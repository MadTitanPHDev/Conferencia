using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ConferenciaNFs.Data;
using ConferenciaNFs.Infrastructure;
using ConferenciaNFs.Models;
using Microsoft.Win32;

namespace ConferenciaNFs.ViewModels;

public sealed class ConferenciaViewModel : ViewModelBase
{
    private readonly NotaFiscalRepository _repository;
    private NotaFiscal? _notaSelecionada;
    private bool _isCarregando;
    private string _mensagemStatus = string.Empty;
    private string _observacaoTexto = string.Empty;

    public ConferenciaViewModel(NotaFiscalRepository repository, string apelidoLoja, string dataCompra)
    {
        _repository = repository;
        ApelidoLoja = apelidoLoja;
        DataCompra = dataCompra;
        Notas = new ObservableCollection<NotaFiscal>();

        DefinirPendenteCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Pendente));
        DefinirVerdeCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Verde));
        DefinirAmareloCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Amarelo));
        DefinirVermelhoCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Vermelho));
        DefinirLaranjaCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Laranja));
        DefinirAzulCommand = new AsyncRelayCommand(_ => AlterarStatusAsync(StatusConferenciaValues.Azul));

        DefinirObservacaoPbmCommand = new AsyncRelayCommand(_ => SalvarObservacaoAsync(ObservacaoValues.Pbm));
        DefinirObservacaoUsoConsumoCommand = new AsyncRelayCommand(_ => SalvarObservacaoAsync(ObservacaoValues.UsoEConsumo));
        DefinirObservacaoConvenienciaCommand = new AsyncRelayCommand(_ => SalvarObservacaoAsync(ObservacaoValues.Conveniencia));
        SalvarObservacaoCommand = new AsyncRelayCommand(_ => SalvarObservacaoAsync(ObservacaoTexto));
        ExportarConferenciaCommand = new AsyncRelayCommand(_ => ExportarConferenciaAsync());

        _ = CarregarNotasAsync();
    }

    public string ApelidoLoja { get; }
    public string DataCompra { get; }

    public string TituloConferencia => $"Conferencia - {ApelidoLoja} - {DataCompra}";

    public ObservableCollection<NotaFiscal> Notas { get; }

    public NotaFiscal? NotaSelecionada
    {
        get => _notaSelecionada;
        set
        {
            if (!SetProperty(ref _notaSelecionada, value))
                return;

            ObservacaoTexto = value?.Observacao ?? string.Empty;
        }
    }

    public string ObservacaoTexto
    {
        get => _observacaoTexto;
        set => SetProperty(ref _observacaoTexto, value);
    }

    public bool IsCarregando
    {
        get => _isCarregando;
        private set => SetProperty(ref _isCarregando, value);
    }

    public string MensagemStatus
    {
        get => _mensagemStatus;
        private set => SetProperty(ref _mensagemStatus, value);
    }

    public ICommand DefinirPendenteCommand { get; }
    public ICommand DefinirVerdeCommand { get; }
    public ICommand DefinirAmareloCommand { get; }
    public ICommand DefinirVermelhoCommand { get; }
    public ICommand DefinirLaranjaCommand { get; }
    public ICommand DefinirAzulCommand { get; }

    public ICommand DefinirObservacaoPbmCommand { get; }
    public ICommand DefinirObservacaoUsoConsumoCommand { get; }
    public ICommand DefinirObservacaoConvenienciaCommand { get; }
    public ICommand SalvarObservacaoCommand { get; }
    public ICommand ExportarConferenciaCommand { get; }

    public bool CopiarNumeroNotaSelecionada()
    {
        if (NotaSelecionada is null || string.IsNullOrWhiteSpace(NotaSelecionada.NumNota))
            return false;

        try
        {
            var numero = NotaSelecionada.NumNota.Trim();
            Clipboard.SetText(numero);
            MensagemStatus = $"Numero da nota {numero} copiado.";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task CarregarNotasAsync()
    {
        try
        {
            IsCarregando = true;
            MensagemStatus = "Carregando notas...";

            var notas = await _repository.ObterNotasPorLojaAsync(ApelidoLoja, DataCompra);

            Notas.Clear();
            foreach (var nota in notas)
                Notas.Add(nota);

            NotaSelecionada = Notas.FirstOrDefault();
            MensagemStatus = $"{Notas.Count} nota(s) carregada(s) em {DataCompra}.";
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro ao carregar notas.";
            MessageBox.Show($"Erro ao carregar notas:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private async Task AlterarStatusAsync(string novoStatus)
    {
        if (NotaSelecionada is null)
        {
            MessageBox.Show("Selecione uma nota para alterar o status.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (NotaSelecionada.StatusConferencia == novoStatus)
            return;

        try
        {
            await _repository.AtualizarStatusConferenciaAsync(NotaSelecionada.Id, novoStatus);
            NotaSelecionada.StatusConferencia = novoStatus;
            MensagemStatus = $"Nota {NotaSelecionada.NumNota}: {StatusConferenciaValues.ObterDescricao(novoStatus)}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao atualizar status:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SalvarObservacaoAsync(string? observacao)
    {
        if (NotaSelecionada is null)
        {
            MessageBox.Show("Selecione uma nota para adicionar observacao.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var texto = (observacao ?? string.Empty).Trim();

        if (NotaSelecionada.Observacao == texto)
            return;

        try
        {
            await _repository.AtualizarObservacaoAsync(NotaSelecionada.Id, texto);
            NotaSelecionada.Observacao = texto;
            ObservacaoTexto = texto;
            MensagemStatus = string.IsNullOrEmpty(texto)
                ? $"Observacao removida da nota {NotaSelecionada.NumNota}."
                : $"Nota {NotaSelecionada.NumNota}: observacao \"{texto}\" salva.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar observacao:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportarConferenciaAsync()
    {
        if (Notas.Count == 0)
        {
            MessageBox.Show("Nao ha notas para exportar.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var nomeArquivo = $"{SanitizarNomeArquivo(ApelidoLoja)}_{SanitizarNomeArquivo(DataCompra)}_conferencia.xlsx";
        var dialog = new SaveFileDialog
        {
            Title = "Exportar conferencia",
            Filter = "Planilha Excel (*.xlsx)|*.xlsx",
            FileName = nomeArquivo,
            DefaultExt = ".xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsCarregando = true;
            MensagemStatus = "Exportando conferencia...";

            var caminho = dialog.FileName;
            var notas = Notas.ToList();
            var loja = ApelidoLoja;

            await Task.Run(() => ConferenciaExportador.Exportar(caminho, loja, notas));

            MensagemStatus = $"Conferencia exportada: {Path.GetFileName(caminho)}";
            MessageBox.Show($"Arquivo exportado com sucesso:\n{caminho}", "Exportacao concluida",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MensagemStatus = "Erro na exportacao.";
            MessageBox.Show($"Erro ao exportar conferencia:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCarregando = false;
        }
    }

    private static string SanitizarNomeArquivo(string nome)
    {
        var sanitizado = Regex.Replace(nome.Trim(), @"[<>:""/\\|?*]", "_");
        return string.IsNullOrWhiteSpace(sanitizado) ? "loja" : sanitizado;
    }
}
