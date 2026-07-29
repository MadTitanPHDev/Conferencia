using CsvHelper.Configuration.Attributes;

namespace ConferenciaNFs.Models;

/// <summary>
/// Mapeamento das colunas do CSV (linha 8 = cabecalho).
/// </summary>
public sealed class NotaFiscalCsvRecord
{
    [Name("APELIDOLOJA")]
    public string ApelidoLoja { get; set; } = string.Empty;

    [Name("NUMNOTA")]
    public string NumNota { get; set; } = string.Empty;

    [Name("NOMEFORN")]
    public string NomeForn { get; set; } = string.Empty;

    [Name("DATACOMPRA")]
    public string DataCompra { get; set; } = string.Empty;

    [Name("DATAEMISSAO")]
    public string DataEmissao { get; set; } = string.Empty;

    [Name("VALORNOTA")]
    public string ValorNota { get; set; } = string.Empty;

    [Name("CNPJFORN")]
    public string CnpjForn { get; set; } = string.Empty;
}
