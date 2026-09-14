# ConferenciaNFs

App desktop (WPF, .NET 8) para a conferência diária de notas fiscais de entrada das lojas.

As notas vêm do **VSM (ERP)** em MySQL, somente leitura. Tudo o que a equipe decide — status, observação, fila de devolução — fica em um **PostgreSQL** compartilhado, acessado por várias máquinas ao mesmo tempo.

Versão atual: **1.0.11**

---

## Índice

- [Como funciona](#como-funciona)
- [Instalação](#instalação)
- [Configuração](#configuração)
- [Uso no dia a dia](#uso-no-dia-a-dia)
- [Status de conferência](#status-de-conferência)
- [Atalhos de teclado](#atalhos-de-teclado)
- [Telas](#telas)
- [Desenvolvimento](#desenvolvimento)
- [Arquitetura](#arquitetura)
- [Banco de dados](#banco-de-dados)
- [Publicando uma versão](#publicando-uma-versão)
- [Problemas conhecidos](#problemas-conhecidos)

---

## Como funciona

```
   MySQL VSM (myouro)                 PostgreSQL (conferencia_nfs_1)
   somente leitura                    tudo que a equipe decide
   ┌──────────────────┐               ┌──────────────────────────┐
   │ compras          │──── sync ────▶│ NotasFiscais             │
   │ itens_compra     │               │ Devolucoes               │
   └──────────────────┘               │ LojaOrdem / LojaVsmMap   │
            ▲                         │ Distribuidoras           │
            │ consulta                │ AppConfig                │
            │ (itens, EAN)            └──────────────────────────┘
            └──────────── ConferenciaNFs (WPF) ───────────────┘
```

O app **nunca grava no VSM**.

### O dia da conferência

Cada nota pertence ao dia em que **entrou no VSM** (`DATACOMPRA`), não à data de emissão da NF. Esse é o campo `DiaConferencia`.

Uma nota conferida no sábado **permanece no sábado**. Se o VSM a trouxer de novo na segunda (código de compra novo), ela vira uma **linha nova** no dia da segunda, com o status herdado — ela não "viaja" de dia nem esvazia o dia anterior.

### Intervalo de datas

O dashboard abre em **ontem → ontem** (um dia só, igual ao comportamento clássico) — ou retoma o intervalo da sessão anterior, enquanto a data final dele ainda for hoje.

Na segunda-feira, ou depois de um feriado, basta alargar o intervalo — por exemplo **sábado até segunda**. O app sincroniza cada `DATACOMPRA` do período e mostra tudo em uma lista só, com uma coluna **Dia** indicando a origem de cada nota. Limite: **7 dias**.

Três atalhos abaixo dos campos preenchem as duas pontas de uma vez:

| Atalho | Preenche |
|---|---|
| **Ontem** | De e Até no dia anterior. |
| **Desde sabado** | Do último sábado até hoje — cobre a segunda-feira e a volta de feriado. |
| **Em aberto** | Começa no dia mais antigo (dentro de 7 dias) que ainda tem nota pendente. |

---

## Instalação

Baixe o instalador mais recente em [Releases](https://github.com/MadTitanPHDev/Conferencia/releases) — `ConferenciaNFs-win-Setup.exe` — e execute.

Instalações feitas pelo Setup (Velopack) verificam novas versões no GitHub **ao abrir o app** e oferecem a atualização.

> O Windows pode exibir um aviso do SmartScreen: os binários ainda não são assinados digitalmente.

---

## Configuração

O arquivo `app-settings.json` fica em **`%AppData%\ConferenciaNFs\`**, fora da pasta de instalação — que o Velopack substitui inteira a cada atualização. Na primeira execução o app migra para lá o arquivo que estiver na pasta do executável; se não houver nenhum, parte do `app-settings.example.json` distribuído no pacote.

```json
{
  "JanelaFixadaNoTopo": false,
  "TemaEscuro": false,
  "DuploCliqueCopiaNumero": false,
  "ConnectionString": "Host=127.0.0.1;Port=5432;Database=conferencia_nfs_1;Username=conferencia;Password=1234",
  "MysqlConnectionString": "Server=192.168.21.2;Port=33021;Database=myouro;User ID=compras;Password="
}
```

| Chave | Para que serve |
|---|---|
| `ConnectionString` | PostgreSQL da conferência. **Obrigatória** — sem ela o app não abre. |
| `MysqlConnectionString` | VSM, somente leitura. Se vazia, o sync e a pesquisa por EAN ficam indisponíveis; o CSV continua como alternativa. |
| `TemaEscuro` | Tema claro/escuro. |
| `JanelaFixadaNoTopo` | Mantém as janelas acima das outras. |
| `DuploCliqueCopiaNumero` | Na conferência: `true` = duplo clique copia o número; `false` = abre os itens. Alterna com **F12**. |
| `UltimoIntervaloInicio` / `UltimoIntervaloFim` | Último De/Até usado, em `dd/MM/yyyy`. Gravado pelo app; restaurado na abertura só enquanto a data final for hoje. |

O `.gitignore` ignora `app-settings.json` — **nunca** comite senhas. O `app-settings.example.json` é versionado com a senha do MySQL em branco, de propósito.

As tabelas do PostgreSQL são criadas automaticamente na primeira conexão. Não há script de migração para rodar à mão.

---

## Uso no dia a dia

1. Abra o app. Ele já entra no dia anterior e sincroniza o VSM.
2. Ajuste **De / Até** se precisar cobrir fim de semana ou feriado.
3. Clique no card da loja para abrir a conferência.
4. Marque cada nota com o status (teclado é mais rápido que o mouse).
5. Notas **Vermelho** entram sozinhas na fila de **Devoluções**.

O sync roda ao abrir o intervalo, no botão **Sincronizar VSM** e automaticamente **a cada 30 minutos** enquanto o app estiver aberto. Ele nunca apaga notas — só insere e atualiza.

O sync automático revê apenas **hoje e ontem**: como `DATACOMPRA` é a data em que a nota entrou no VSM, dia passado não recebe nota nova. Trocar o intervalo ou clicar em **Sincronizar VSM** cobre o período inteiro.

**Importar CSV** é o plano B para quando o MySQL estiver fora. Todas as linhas do arquivo entram na **data final** do intervalo.

---

## Status de conferência

| Status | Cor | Significado |
|---|---|---|
| Pendente | Branco | Ainda não conferida |
| Verde | Verde | Nota correta |
| Amarelo | Amarelo | Nota com advertência |
| Vermelho | Vermelho | Nota devolvida — entra na fila de devoluções |
| Laranja | Laranja | Nota de outro dia |
| Azul | Azul | Nota absorvida |

Pendência, para o contador do dashboard, é **Pendente + Amarelo**.

### Herança entre dias

Quando o VSM traz de novo uma nota já conferida em um dia anterior, a linha do novo dia nasce com status herdado:

| Estava como | Vira |
|---|---|
| Verde ou Azul | Laranja |
| Laranja | Laranja |
| Amarelo | Amarelo |
| Vermelho | Vermelho (ou Laranja, se a devolução já foi concluída) |
| Pendente | Pendente |

Nota nova, sem essa identidade em outro dia, **permanece em branco**. Laranja não é mais aplicado só porque a emissão é antiga.

A identidade da nota é buscada por `CodCompra`, chave da NFe, ou pela combinação loja + número + CNPJ. O recurso é ligado no menu ☰ (**herdar status**), e há um **recalcular** para reaplicar as regras no intervalo aberto.

---

## Atalhos de teclado

### Conferência da loja

| Tecla | Ação |
|---|---|
| `0` | Pendente (Branco) |
| `1` ou `F1` | Verde |
| `2` ou `F2` | Amarelo |
| `3` ou `F3` | Vermelho |
| `4` ou `F4` | Laranja |
| `5` ou `F8` | Azul |
| `F5` | Observação PBM |
| `F6` | Observação USO E CONSUMO |
| `F7` | Observação CONVENIENCIA |
| `F9` | Observação BONIFICACAO |
| `F11` | Observação ENCOMENDA |
| `Ctrl+S` | Salvar a observação digitada |
| `Enter` | Salvar a observação (dentro do campo de texto) |
| `Ctrl+C` | Copiar o número da nota |
| `F12` | Alternar o que o duplo clique faz |
| `Esc` | Fechar a janela |

Mouse: **duplo clique** copia o número ou abre os itens (conforme F12), **scroll** troca a nota selecionada, **botão direito** abre o menu com todas as ações.

### Outras janelas

| Janela | Atalhos |
|---|---|
| Devoluções | `Ctrl+C` e duplo clique copiam o número da nota |
| Pesquisar produto | `Enter` pesquisa · `Ctrl+C` e duplo clique copiam o número · botão direito abre os itens · `Esc` fecha |
| Pesquisar nota | `Enter` pesquisa |
| Itens da nota | duplo clique copia o EAN · `Esc` fecha |
| Dashboard | `Esc` fecha o menu ☰ |

---

## Telas

| Tela | O que faz |
|---|---|
| **Dashboard** | Intervalo De/Até, cards das lojas com pendências, sincronizar, importar CSV, exportar tudo. |
| **Conferência da loja** | Grade das notas do intervalo, colorida por status. Marcar status, observações, filtros, exportar Excel da loja. |
| **Itens da nota** | Itens vindos do VSM: produto, EAN, quantidade, custo, lote, validade. |
| **Devoluções** | Fila das notas Vermelho: pendentes, vencidas, a vencer, concluídas. Prazo = emissão + dias da distribuidora. |
| **Pesquisar nota** | Busca por número e mostra o status da primeira conferência daquela NF. |
| **Pesquisar produto** | Busca por EAN no VSM a partir de uma data, com custo mínimo opcional. |
| **Gerenciar lojas** | Ordem e nome de exibição dos cards. |
| **Distribuidoras** | Prazo de devolução por CNPJ e se o prazo é rastreado. |

---

## Desenvolvimento

Requisitos: **.NET 8 SDK**, Windows, PostgreSQL acessível. MySQL do VSM é opcional para rodar.

```powershell
git clone https://github.com/MadTitanPHDev/Conferencia.git
cd Conferencia

# configure a conexão antes de rodar
copy ConferenciaNFs\app-settings.example.json ConferenciaNFs\app-settings.json

dotnet build ConferenciaNFs\ConferenciaNFs.csproj
dotnet run  --project ConferenciaNFs\ConferenciaNFs.csproj
dotnet test ConferenciaNFs.Tests\ConferenciaNFs.Tests.csproj
```

> O projeto de testes **não** faz parte de `ConferenciaNFs.sln`; aponte o `.csproj` direto, como acima.

Se o build falhar com "arquivo bloqueado por outro processo", feche o app antes de compilar.

### Dependências

| Pacote | Versão | Uso |
|---|---|---|
| Npgsql | 8.0.6 | PostgreSQL |
| MySqlConnector | 2.3.7 | VSM (leitura) |
| Dapper | 2.1.35 | Consultas |
| ClosedXML | 0.104.2 | Exportação Excel |
| CsvHelper | 33.0.1 | Importação CSV |
| Velopack | 1.2.0 | Instalador e atualização |
| Microsoft.Data.Sqlite | 8.0.11 | Só a migração do banco antigo |

---

## Arquitetura

MVVM, sem framework de injeção de dependência. A janela cria o ViewModel, que recebe o repositório.

```
ConferenciaNFs/
├── Program.cs              Entry point (hooks do Velopack)
├── App.xaml.cs             Startup: settings, tema, checagem de update
├── MainWindow.xaml(.cs)    Dashboard
├── Data/                   PostgreSQL, VSM e sync
├── Infrastructure/         Serviços, parsers, regras, converters
├── Models/                 Entidades e constantes de domínio
├── ViewModels/             Estado e comandos das telas
├── Views/                  Janelas secundárias
├── Controls/               ThemeToggle, PinWindowToggle
├── Themes/                 Cores claro/escuro e estilos
└── Assets/                 Ícones e logos
```

Pontos que concentram as regras:

- `Data/NotaFiscalRepository.cs` — schema, sync, herança, consultas, devoluções.
- `Data/VsmSyncService.cs` — sincroniza um dia ou um intervalo.
- `Infrastructure/DataCompraParser.cs` — datas em `dd/MM/yyyy` e enumeração do intervalo.
- `Infrastructure/StatusHerancaImportacao.cs` — regras de herança de status.

### Datas

`DiaConferencia`, `DataCompra` e `DataEmissao` são **texto** no formato `dd/MM/yyyy`. Por isso o filtro de intervalo monta a lista de dias e usa `DiaConferencia = ANY(@Dias)` — não `BETWEEN`.

---

## Banco de dados

PostgreSQL, criado automaticamente pelo app.

| Tabela | Conteúdo |
|---|---|
| `NotasFiscais` | A nota e o que a equipe marcou. `ChaveUnica` = `loja_num_cnpj_diaConferencia_dataCompra`. |
| `Devolucoes` | Fila das notas Vermelho (1 por nota). |
| `LojaOrdem` | Ordem e nome de exibição dos cards. |
| `LojaVsmMap` | `CODLOJA` do VSM → apelido da loja. |
| `Distribuidoras` | CNPJ, prazo de devolução, rastrear prazo. |
| `AppConfig` | Chave/valor: herança ligada, data mínima de devoluções, correções aplicadas. |

O VSM é lido em `compras` (cabeçalho da NF) e `itens_compra` (itens e busca por EAN, incluindo `BARRAS_EANTRIB`).

---

## Publicando uma versão

1. Atualize `<Version>`, `<AssemblyVersion>`, `<FileVersion>` e `<InformationalVersion>` em `ConferenciaNFs/ConferenciaNFs.csproj`.
2. Commit e push na `main`.
3. Gere os pacotes:

```powershell
.\scripts\release.ps1 -Version 1.0.10
```

4. Publique a release com os arquivos de `artifacts\Releases\`:

```powershell
gh release create v1.0.10 (Get-ChildItem artifacts\Releases -File).FullName `
  --title "ConferenciaNFs 1.0.10" --notes "..."
```

Ou faça os dois passos de uma vez com `.\scripts\release.ps1 -Version 1.0.10 -CreateGitHubRelease`.

O script publica self-contained win-x64, empacota com Velopack e leva no pacote **apenas** o `app-settings.example.json`. Nem as suas configurações de desenvolvimento nem um `app-settings.json` padrão vão junto — é justamente isso que fazia a configuração do usuário ser zerada a cada atualização.

---

## Problemas conhecidos

- **Ao subir de 1.0.9 para 1.0.10, a configuração precisa ser refeita uma última vez.** A atualização em si apaga o `app-settings.json` da pasta de instalação antes de o código novo rodar, então não há o que migrar. A partir da 1.0.10 o arquivo mora em `%AppData%\ConferenciaNFs\` e sobrevive às próximas atualizações.
- **Em desenvolvimento, o `app-settings.json` do projeto só vale na primeira execução.** Depois disso o app lê o de `%AppData%`; edite lá, ou apague o de `%AppData%` para migrar de novo.
- **Binários não assinados.** O SmartScreen alerta na instalação.
- **Release manual.** Não há CI; os pacotes saem da máquina de quem publica.
- **`ImportacaoCsvTests` depende de arquivos locais** em `c:\Users\User\Desktop\...`. Dois testes estão com `Skip`; o de detecção de delimitador falha em outras máquinas.
- **Código morto no repositório**: `ResolverChaveDataCompraAsync`, `LimparNotasDoDiaAtualAsync` e as sobrecargas de dia único de `ObterResumoDiaAsync`, `ObterNotasPorLojaAsync` e `ObterTodasNotasPorDataAsync` ficaram sem chamador depois do intervalo De/Até.

---

## Documentação complementar

- `ConferenciaNFs-Documentacao.html` — documentação funcional detalhada para a equipe.
- `ConferenciaNFs-Apresentacao.html` — visão técnica.
- `ConferenciaNFs-Planejamento-Ecossistema.html` — planejamento antigo, desatualizado.
