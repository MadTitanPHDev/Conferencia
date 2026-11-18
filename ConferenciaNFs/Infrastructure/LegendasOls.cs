using System.Globalization;

namespace ConferenciaNFs.Infrastructure;

public sealed class LegendaOlsColuna
{
    public required string Pessoa { get; init; }
    public required IReadOnlyList<string> Itens { get; init; }
}

public sealed class LegendaOlsBloco
{
    public required string Titulo { get; init; }
    public required IReadOnlyList<LegendaOlsColuna> Colunas { get; init; }
}

/// <summary>
/// Listas da primeira aba de Planilha Perfumarias. As outras abas repetem
/// Generico e Referencia com nomes diferentes e ficam de fora.
/// </summary>
public static class LegendasOls
{
    private static readonly StringComparer Ordem =
        StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);

    public static LegendaOlsBloco OlsPerfumaria { get; } = Bloco(
        "Ols Perfumaria",
        Coluna("Maria",
            "Perf geral + Falteiro", "Abbott", "Apis Flora", "Babysec", "Cor&Ton",
            "Danone (Leites)", "Eurofral", "Falamed/Abs (Mulher Ativa)",
            "Fralda popular InovaCare/Vitaplus", "GPZ", "Maf (Biosintetica)",
            "Mantecorp", "Mili", "Nestle", "OL P&G", "Omron", "Promoções Perfumaria",
            "Ricosol", "Tena", "Turma M/Pom Pom/Bigfral"),
        Coluna("Luara",
            "Avent COTE", "Baruel", "Closeup/Oral B", "Coty", "Dermo",
            "Des Dove/Rex (150) 250ml/Clinical", "Duracell", "Eudora", "Fixodent",
            "Granado", "Havaianas", "Huggies", "Kuka", "Mam", "Neopan",
            "Nutrasenior", "OAZ", "Plenitud", "Principia"),
        Coluna("Maju",
            "Above 150ml", "ALTF+F6", "Alva", "Anjo", "Belessa/Hana Penina",
            "Bellaphytus", "Confort Master", "DentalClean", "Giovanna Baby",
            "Hipopo", "Labotrat", "Maquiagem", "Nivea", "Nutrisse", "Ox",
            "Renova", "Sustagen", "Tint Casting/Imedia", "Tint Kol/Soft/Bigen"));

    public static LegendaOlsBloco LojasPerfumaria { get; } = Bloco(
        "Lojas Perfumaria",
        ColunaLojas("Luara", 1, 13),
        ColunaLojas("Maju", 14, 26),
        ColunaLojas("Maria", 27, 38));

    public static LegendaOlsBloco OlsGenerico { get; } = Bloco(
        "Ols Generico",
        Coluna("Hillary",
            "União Quimica", "Natulab", "Althaia", "Herbamed", "Biosintetica",
            "Hertz", "E.M.S Similar", "Vitafor", "Pharlab", "NutraVitton",
            "Merck Genérico", "Biolab", "(Se voltar) Supley"),
        Coluna("Vinícius",
            "Cimed", "Teuto", "Prati", "Vitamedic", "Catarinense", "Neo Quimica",
            "Legrand Sim/Lib", "Sandoz", "Airela", "Geolab", "Nova Quimica",
            "Globo", "QualyNutri"),
        Coluna("Ernani",
            "Integral Medica (Guilherme)", "Germed", "E.M.S Genérico", "Uevo"));

    public static LegendaOlsBloco OlsReferencia { get; } = Bloco(
        "Ols Referencia",
        Coluna("Juliana",
            "Atlética", "Desc FP", "Eurofarma Genérico", "Fracionados Hypera",
            "Genom", "Haleon", "Hypera", "Medley Genérico", "Rambaxy Genérico",
            "Sacola", "Sanofi"),
        Coluna("Mayara",
            "Apsen", "Bayer", "Biolab", "Libbs", "Merck", "Sigma"),
        Coluna("Rafael",
            "Ache", "Astra", "Gross", "Marjan", "Myralis", "PBM", "Procter",
            "Puran/Zinpass", "Servier", "Torrent"));

    private static LegendaOlsBloco Bloco(string titulo, params LegendaOlsColuna[] colunas) =>
        new()
        {
            Titulo = titulo,
            Colunas = colunas.OrderBy(c => c.Pessoa, Ordem).ToArray()
        };

    private static LegendaOlsColuna Coluna(string pessoa, params string[] itens) =>
        new() { Pessoa = pessoa, Itens = itens.OrderBy(i => i, Ordem).ToArray() };

    private static LegendaOlsColuna ColunaLojas(string pessoa, int inicio, int fim)
    {
        var linhas = new List<string>();
        for (var cod = inicio; cod <= fim; cod++)
        {
            var loja = LojaVsmMapSeed.Lojas.FirstOrDefault(l => l.CodLoja == cod);
            linhas.Add(loja is null ? cod.ToString() : $"{cod} {loja.NomeExibicao}");
        }

        return new LegendaOlsColuna
        {
            Pessoa = pessoa,
            Itens = linhas.OrderBy(NomeDaLoja, Ordem).ToArray()
        };
    }

    private static string NomeDaLoja(string linha)
    {
        var espaco = linha.IndexOf(' ');
        return espaco < 0 ? linha : linha[(espaco + 1)..];
    }
}
