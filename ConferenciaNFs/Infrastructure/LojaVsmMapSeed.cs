using ConferenciaNFs.Models;

namespace ConferenciaNFs.Infrastructure;

/// <summary>
/// Seed da planilha CODLOJA / APELIDOLOJA / LOJA / CNPJ (Pasta1.xlsx).
/// </summary>
public static class LojaVsmMapSeed
{
    public static readonly IReadOnlyList<LojaVsmMap> Lojas =
    [
        Item(1, "BARAO", "Barao", "04.834.843/0001-44"),
        // O VSM rotula CODLOJA 2-5 com o nome da loja vizinha.
        // O codigo aponta para o CNPJ real, nao para o apelido exibido no VSM.
        Item(2, "RANCHARIA", "Rancharia", "18.465.867/0001-88"),
        Item(3, "VELASQUES", "Velasques", "02.025.871/0001-95"),
        Item(4, "COHAB", "Cohab", "12.040.504/0001-14"),
        Item(5, "REGENTE", "Regente", "15.303.440/0001-95"),
        Item(6, "MARTINOPOL", "Martinopolis", "19.671.223/0001-09"),
        Item(7, "PIRAPOZINH", "Pirapozinho", "04.834.843/0002-25"),
        Item(8, "MACHADO", "Machado", "04.834.843/0003-06"),
        Item(9, "BERNARDES", "Bernardes", "04.834.843/0004-97"),
        Item(10, "QUATA", "Quata", "04.834.843/0005-78"),
        Item(11, "MIRANTE", "Mirante", "04.834.843/0007-30"),
        Item(12, "RANCHA CEN", "Rancharia Centro", "04.834.843/0008-10"),
        Item(13, "DAMHA", "Damha", "04.834.843/0009-00"),
        Item(14, "TEODORO", "Teodoro", "04.834.843/0016-20"),
        Item(15, "OSVALDO CR", "Osvaldo Cruz", "04.834.843/0010-35"),
        Item(16, "QUATA FILI", "Quata Filial", "04.834.843/0006-59"),
        Item(17, "MARACAI", "Maracai", "04.834.843/0017-01"),
        Item(18, "BASTOS", "Bastos", "04.834.843/0019-73"),
        Item(19, "TARUMA", "Taruma", "04.834.843/0018-92"),
        Item(20, "LUCELIA", "Lucelia", "04.834.843/0020-07"),
        Item(21, "MANOEL GOU", "Manoel Goulart", "04.834.843/0022-79"),
        Item(22, "RINOPOLIS", "Rinopolis", "04.834.843/0021-98"),
        Item(23, "CORONEL MA", "Coronel Marcondes", "04.834.843/0023-50"),
        Item(24, "ADAMANTINA", "Adamantina", "04.834.843/0025-11"),
        Item(25, "MIRANTE CE", "Mirante Centro", "04.834.843/0024-30"),
        Item(26, "PARAPUÃ", "Parapua", "04.834.843/0011-16"),
        Item(27, "FLORIDA PA", "Florida Paulista", "04.834.843/0012-05"),
        Item(28, "LUCELIA CE", "Lucelia Centro", "04.834.843/0013-88"),
        Item(29, "NOVA ESPER", "Nova Esperanca", "04.834.843/0014-69"),
        Item(30, "TUPA", "Tupa", "04.834.843/0015-40"),
        Item(31, "AVENIDA BR", "Avenida Brasil", "04.834.843/0027-83"),
        Item(32, "ANDRADINA", "Andradina", "04.834.843/0026-00"),
        Item(33, "PARAGUACU", "Paraguacu Paulista", "04.834.843/0028-64"),
        Item(34, "TARABAI", "Tarabai", "04.834.843/0029-45"),
        Item(35, "CASTILHO", "Castilho", "04.834.843/0030-89"),
        Item(36, "PEREIRA BA", "Pereira Barreto", "04.834.843/0031-60"),
        Item(37, "PRIMAVERA", "Rosana (Primavera)", "04.834.843/0032-40"),
        Item(38, "NARANDIBA", "Narandiba", "04.834.843/0033-21"),
    ];

    private static LojaVsmMap Item(int codLoja, string apelido, string nome, string cnpj) => new()
    {
        CodLoja = codLoja,
        ApelidoLoja = apelido,
        NomeExibicao = nome,
        CnpjLoja = CnpjNormalizer.Normalizar(cnpj)
    };
}
