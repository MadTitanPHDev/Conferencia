# ConferenciaNFs

App desktop (WPF, .NET 8) para a conferência diária de notas fiscais de entrada das lojas.

As notas vêm do **VSM (ERP)** em MySQL, somente leitura. Tudo o que a equipe decide — status, observação, fila de devolução — fica em um **PostgreSQL** compartilhado, acessado por várias máquinas ao mesmo tempo.

Versão publicada: **1.0.35** ([release no GitHub](https://github.com/MadTitanPHDev/Conferencia/releases/tag/v1.0.35))

**Onde estamos:** a conferência do dia a dia está em produção, com sync VSM por computador, status, envio de divergência ao comprador, fila de devoluções e comparação de preço. O núcleo continua o PostgreSQL, sem gravar no VSM. O app separado de compradores segue em espera: o comprador usa as telas de Divergências deste mesmo programa. **Adiado de propósito:** retry nas gravações quando o Postgres corta a conexão (`Exception while reading from stream`), faixas de quantidade da tabela Teuto e o relatório em lote de diferença de preços.

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
   │ itens_compra     │               │ Devolucoes / Pessoas     │
   │ pedido           │               │ PendenciasCompra         │
   └──────────────────┘               │ PrecosImportacoes        │
            ▲                         │ LojaOrdem / LojaVsmMap   │
            │ consulta                │ Distribuidoras           │
            │ (itens, EAN, pedido)    │ AppConfig                │
            └──────────── ConferenciaNFs (WPF) ───────────────┘
```

O app **nunca grava no VSM**. O sync em massa só roda no computador em que o menu ☰ está com **Puxar notas neste PC** ligado. O padrão é desligado, e essa escolha fica só no `app-settings.json` da máquina.

### O dia da conferência

Cada nota pertence ao dia em que **entrou no VSM** (`DATACOMPRA`), não à data de emissão da NF. Esse é o campo `DiaConferencia`.

Se a mesma compra aparecer novamente em outro dia, a linha anterior **permanece no dia em que foi conferida**. O dia novo recebe uma linha própria, com o status herdado conforme as regras abaixo. Uma correção de loja ou chave só pode realocar a nota dentro do mesmo dia.

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
  "SincronizarVsm": false,
  "PessoaIdNestePc": 0,
  "DuploCliqueCopiaNumero": false,
  "ConnectionString": "Host=127.0.0.1;Port=5432;Database=conferencia_nfs_1;Username=conferencia;Password=1234",
  "MysqlConnectionString": "Server=192.168.21.2;Port=33021;Database=myouro;User ID=compras;Password="
}
```

| Chave | Para que serve |
|---|---|
| `ConnectionString` | PostgreSQL da conferência. **Obrigatória** — sem ela o app não abre. |
| `MysqlConnectionString` | VSM, somente leitura. Se vazia, o sync e a pesquisa por EAN ficam indisponíveis; o CSV continua como alternativa. |
| `SincronizarVsm` | Este PC puxa notas do VSM ao abrir, no timer e no botão. Padrão `false`. |
| `PessoaIdNestePc` | Pessoa do cadastro que usa este computador. Sobrevive ao atualizar. Não filtra a lista da conferência. |
| `TemaEscuro` | Tema claro/escuro. |
| `JanelaFixadaNoTopo` | Mantém as janelas acima das outras. |
| `DuploCliqueCopiaNumero` | Na conferência: `true` = duplo clique copia o número; `false` = abre os itens. Alterna com **F12**. |
| `UltimoIntervaloInicio` / `UltimoIntervaloFim` | Último De/Até usado, em `dd/MM/yyyy`. Gravado pelo app; restaurado na abertura só enquanto a data final for hoje. |

O `.gitignore` ignora `app-settings.json` — **nunca** comite senhas. O `app-settings.example.json` é versionado com a senha do MySQL em branco, de propósito.

As tabelas do PostgreSQL são criadas automaticamente na primeira conexão. Não há script de migração para rodar à mão.

---

## Uso no dia a dia

1. Abra o app. Ele entra no dia anterior. O VSM só é puxado se este PC estiver autorizado no menu.
2. Ajuste **De / Até** se precisar cobrir fim de semana ou feriado.
3. Clique no card da loja para abrir a conferência.
4. Marque cada nota com o status (teclado é mais rápido que o mouse).
5. **Amarelo** pergunta se a nota vai ao comprador ou fica só na conferência. **Vermelho** abre a pré-nota e entra na fila de **Devoluções**.

No PC autorizado, o sync roda ao abrir o intervalo, no botão **Sincronizar VSM** e automaticamente **a cada 30 minutos**. Ele nunca apaga notas de um dia anterior — só insere e atualiza o dia sincronizado.

O sync automático revê apenas **hoje e ontem**: como `DATACOMPRA` é a data em que a nota entrou no VSM, dia passado não recebe nota nova. Trocar o intervalo ou clicar em **Sincronizar VSM** cobre o período inteiro. Enquanto sincroniza, a barra de progresso na tela inicial mostra o andamento.

Na loja, conferência e devoluções abrem **na mesma janela**. O menu lateral some: **☰** abre o painel flutuante e a seta volta às lojas. Em janela pequena o layout fica compacto.

**Importar CSV** é o plano B para quando o MySQL estiver fora. Todas as linhas do arquivo entram na **data final** do intervalo.

---

## Status de conferência

| Status | Cor | Significado |
|---|---|---|
| Pendente | Branco | Ainda não conferida |
| Verde | Verde | Nota correta |
| Amarelo | Amarelo | Falar com compras. Pode ir ao comprador ou ficar só na conferência |
| Vermelho | Vermelho | Devolver — abre a pré-nota e entra na fila de devoluções |
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
| Vermelho | Vermelho (ou Laranja, se a devolução já estiver registrada) |
| Pendente | Pendente |

Nota nova, sem essa identidade em outro dia, **permanece em branco**. Laranja não é mais aplicado só porque a emissão é antiga.

A identidade da nota é buscada por `CodCompra`, chave da NFe, ou pela combinação loja + número + CNPJ. O recurso é ligado no menu ☰ (**herdar status**), e há um **recalcular** para reaplicar as regras no intervalo aberto. O recálculo não transforma amarelo em laranja.

Cada status tem origem: **Manual** (operador), **Herdado** (sincronização) ou **Sistema**. A sincronização e o recálculo **não desfazem** uma escolha manual. A linha herdada guarda o `Id` da nota anterior, e toda mudança de status/observação entra em `NotaStatusHistorico`.

Quando o operador marca Verde, Azul, Vermelho ou Laranja, o amarelo automático dos dias seguintes da mesma compra vira Laranja. Amarelo pintado na mão permanece. Uma passagem única, na primeira abertura de uma versão com essa regra, corrigiu o desenho que já existia. A repintura não muda o dia da nota nem apaga status.

Mudar o status para qualquer cor que não seja Amarelo cancela a pendência **aberta** daquela compra. O histórico Ficou ou Devolvida permanece. Amarelo enviado ao comprador cria ou mantém a pendência.

---

## Atalhos de teclado

### Conferência da loja

| Tecla | Ação |
|---|---|
| `0` | Pendente (Branco) |
| `1` ou `F1` | Verde |
| `2` ou `F2` | Amarelo (enviar ao comprador ou só deixar amarelo) |
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
| `Esc` | Voltar às lojas (na janela principal) |

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
| **Dashboard** | Intervalo De/Até, cards das lojas com pendências, sincronizar (com progresso, só no PC autorizado), importar CSV, exportar tudo. Tema claro/escuro e pin no topo. Quem usa este PC fica no menu. |
| **Conferência da loja** | Abre embutida na janela principal. Grade colorida por status, observações, filtros, exportar Excel da loja. Coluna Distribuidora. Interruptor Copiar / Abrir nota (F12). Notas iguais no mesmo status aparecem unificadas na tela, sem mover o dia. |
| **Itens da nota** | Itens do VSM (e snapshot no Postgres quando já gravados): produto, EAN, quantidade, custo, lote, validade. Mostra a situação do VSM (Em aberto, Finalizada, Reaberta, Devolvido). **Buscar preço** compara com as tabelas importadas. |
| **Pré-nota** | Ao marcar Vermelho: itens, motivo e classe, gravados no nosso banco. |
| **Divergências** | Notas amarelas enviadas a uma pessoa. O amarelo só da conferência não entra. O comprador responde por item. A equipe da conferência recebe aviso sonoro quando a pendência é resolvida. Há a lista **Devolvidas pelo comprador**. |
| **Pré-nota da divergência** | Itens enviados, com comprador e motivo. O item selecionado compara quantidade, preço e desconto da nota com os pedidos da mesma loja no dia da emissão ou da entrada. |
| **Devoluções** | Fila das notas Vermelho, na mesma janela. Andamento: Enviar XML, Anexar nota, Aguardando dist. Prazo = emissão + dias da distribuidora. Coluna VSM. |
| **Pesquisar nota** | Busca por número e mostra o status da primeira conferência daquela NF. |
| **Pesquisar produto** | Busca por EAN no VSM, com período, loja, distribuidora, status, custo mínimo e se o item foi devolvido. A coluna Devolvido é do produto buscado. O limite do VSM é 400 linhas, antes dos filtros locais. |
| **Gerenciar lojas** | Ordem e nome de exibição dos cards. |
| **Distribuidoras** | Prazo de devolução por CNPJ e se o prazo é rastreado. O nome exibido prefere o cadastro. |
| **Importar distribuidoras** | Planilha com CNPJ e Fornecedor. Preenche nome vazio ou que seja só o CNPJ. Não cria CNPJ novo, não muda prazo e não troca um nome já preenchido. O CNPJ 16.619.378/0001-08 permanece Predileta. |
| **Pessoas** | Cadastro de quem recebe divergência. A equipe da conferência (Paulo, Guilherme, Matheus, João Gabriel) é quem ouve o aviso de resolução. |
| **Motivos** | Cadastro de motivos de devolução. |
| **Tabelas de preço** | Planilhas compartilhadas. Sem cabeçalho: EAN, nome, preço, preço com ST. Com cabeçalho, a coluna Loja também vale. |

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
├── Views/                  Telas (várias como UserControl na MainWindow)
├── Controls/               ThemeToggle, PinWindowToggle
├── Themes/                 Cores claro/escuro e estilos
└── Assets/                 Ícones e logos
```

Pontos que concentram as regras:

- `Data/NotaFiscalRepository.cs` — schema, sync, herança, consultas, devoluções, preços e nomes de distribuidora.
- `Data/VsmSyncService.cs` — sincroniza um dia ou um intervalo.
- `Infrastructure/DataCompraParser.cs` — datas em `dd/MM/yyyy` e enumeração do intervalo.
- `Infrastructure/StatusHerancaImportacao.cs` — regras de herança e da repintura do amarelo automático.
- `Infrastructure/PoliticaSyncVsm.cs` — quais PCs puxam o VSM.
- `Infrastructure/ComparacaoPrecoTabela.cs` — cor, diferença e escolha da tabela de preço.
- `Infrastructure/ComparacaoPedidoNota.cs` — nota contra pedido na divergência.

### Datas

`DiaConferencia`, `DataCompra` e `DataEmissao` são **texto** no formato `dd/MM/yyyy`. Por isso o filtro de intervalo monta a lista de dias e usa `DiaConferencia = ANY(@Dias)` — não `BETWEEN`.

---

## Banco de dados

PostgreSQL, criado automaticamente pelo app.

| Tabela | Conteúdo |
|---|---|
| `NotasFiscais` | A nota e o que a equipe marcou. `ChaveUnica` = `loja_num_cnpj_diaConferencia_dataCompra`. Também guarda `StatusVsm` e `ReabertaVsm`, lidos do VSM. |
| `NotaStatusHistorico` | Mudanças de status, observação e origem. |
| `Devolucoes` | Fila das notas Vermelho (1 por nota). |
| `PendenciasCompra` / `PendenciaCompraItens` | Divergência enviada a uma pessoa, com status Aberta, Cancelada, Ficou ou Devolvida. |
| `Pessoas` | Quem pode receber uma divergência. |
| `LojaOrdem` | Ordem e nome de exibição dos cards. |
| `LojaVsmMap` | `CODLOJA` do VSM → apelido da loja. |
| `Distribuidoras` | CNPJ, nome, prazo de devolução, rastrear prazo. |
| `PrecosImportacoes` / `PrecosItens` | Tabelas de preço. O item pode ter loja e um segundo preço (com ST). |
| `AppConfig` | Chave/valor: herança ligada, data mínima de devoluções, correções aplicadas. |
| `ItensNota` | Snapshot dos itens da NF no nosso banco (pré-nota e detalhe offline). |
| `DevolucaoItens` | Itens, quantidades, motivo e classe da pré-nota. |
| `DevolucaoObservacoes` | Histórico de observações da fila. |
| `MotivosDevolucao` | Cadastro de motivos. |

O VSM é lido em `compras` (cabeçalho, `STATUS` e `REABERTA`), `itens_compra` (itens e busca por EAN, incluindo `BARRAS_EANTRIB`) e `pedido` / `pedidoitens` (comparação da divergência). O usuário MySQL não lê a tabela `fornecedor`; o nome da distribuidora vem do nosso cadastro, com o da nota como reserva.

### Comparação de preço

O botão **Buscar preço**, no detalhe da nota, cruza o EAN com as tabelas importadas.

- Vale a tabela **mais recente** que cubra o item. O menor preço de uma tabela antiga não ganha.
- Se a planilha tiver loja, a linha daquela loja prevalece. Uma tabela nova só de outra loja não apaga o preço geral da tabela anterior.
- O custo da nota é valor do item menos desconto. Na loja 29 (Nova Esperança) soma ST e FCP. Se a planilha tiver preço com ST, a Nova Esperança usa esse preço.
- A coluna mostra a diferença em reais e em percentual. Até R$ 0,02 conta como igual. Até 10% acima, mais essa folga, fica azul-claro. Acima disso, vermelho.
- Se o EAN comercial não estiver na tabela e o tributário estiver, usa o tributário.
- Se o valor gravado for o total da linha, a cor usa o preço unitário do VSM. Uma divergência que não seja isso continua no custo calculado e mostra o valor do VSM ao lado.

---

## Publicando uma versão

1. Atualize `<Version>`, `<AssemblyVersion>`, `<FileVersion>` e `<InformationalVersion>` em `ConferenciaNFs/ConferenciaNFs.csproj`.
2. Commit e push na `main`.
3. Gere os pacotes:

```powershell
.\scripts\release.ps1 -Version 1.0.35
```

4. O próprio script publica a release quando chamado com `-CreateGitHubRelease`. As notas saem de um arquivo temporário, para o PowerShell não quebrar o texto. Também dá para publicar à mão com os arquivos de `artifacts\Releases\`.

```powershell
.\scripts\release.ps1 -Version 1.0.35 -CreateGitHubRelease
```

O script publica self-contained win-x64, empacota com Velopack e leva no pacote **apenas** o `app-settings.example.json`. Nem as suas configurações de desenvolvimento nem um `app-settings.json` padrão vão junto — é justamente isso que fazia a configuração do usuário ser zerada a cada atualização.

---

## Problemas conhecidos

- **Ao subir de 1.0.9 para 1.0.10, a configuração precisa ser refeita uma última vez.** A atualização em si apaga o `app-settings.json` da pasta de instalação antes de o código novo rodar, então não há o que migrar. A partir da 1.0.10 o arquivo mora em `%AppData%\ConferenciaNFs\` e sobrevive às próximas atualizações.
- **Em desenvolvimento, o `app-settings.json` do projeto só vale na primeira execução.** Depois disso o app lê o de `%AppData%`; edite lá, ou apague o de `%AppData%` para migrar de novo.
- **Binários não assinados.** O SmartScreen alerta na instalação.
- **Release manual.** Não há CI; os pacotes saem da máquina de quem publica.
- **`ImportacaoCsvTests` depende de arquivos locais** em `c:\Users\User\Desktop\...`. Dois testes estão com `Skip`; o de detecção de delimitador falha em outras máquinas.
- **Código morto no repositório**: `ResolverChaveDataCompraAsync`, `LimparNotasDoDiaAtualAsync` e as sobrecargas de dia único de `ObterResumoDiaAsync`, `ObterNotasPorLojaAsync` e `ObterTodasNotasPorDataAsync` ficaram sem chamador depois do intervalo De/Até.
- **`Exception while reading from stream` ao atualizar status.** O Npgsql reaproveita um soquete morto do pool (rede, idle do Postgres). Há retry só na abertura da conexão, não na gravação. Fica para um ajuste posterior (retry na operação + keepalive). Um PC com o Postgres local em `127.0.0.1` reduz a frequência; não elimina sozinho.
- **Linhas movidas pela 1.0.25 sem backup anterior.** A recuperação automática só restaurou as cinco notas da loja 36 confirmadas pela equipe. As demais possíveis realocações ficam no relatório de dry-run; sem evidência do dia original, não são recriadas.

---

## Documentação complementar

- `README-DETALHADO.md` — histórico técnico, incidentes, tentativas de correção, regressões, pendências e objetivo final do ConferenciaNFs.
- `ConferenciaNFs-Documentacao.html` — documentação interna da equipe: o que o app faz, o que já foi entregue, objetivo e o que está em espera.
- `ConferenciaNFs-Apresentacao.html` — visão técnica (arquitetura, telas, pacotes).
- `ConferenciaNFs-Planejamento-Ecossistema.html` — rascunho antigo do ecossistema; o estado atual está na documentação acima.
