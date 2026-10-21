# ConferenciaNFs — histórico técnico, incidentes e visão de evolução

Este documento complementa o `README.md`. O README principal explica como instalar,
configurar e usar o aplicativo. Este segundo README registra as decisões técnicas,
os erros encontrados, as tentativas de correção, as regressões, as limitações atuais
e o objetivo de longo prazo do ConferenciaNFs.

Estado de referência deste documento:

- Data: 21/10/2026.
- Versão publicada: 1.0.27.
- Aplicativo: WPF em .NET 8 para Windows.
- Banco operacional: PostgreSQL compartilhado.
- ERP VSM: MySQL em modo somente leitura.
- Distribuição: Velopack por GitHub Releases.

> Este arquivo não contém senhas, credenciais ou endereços internos completos.
> Configurações reais devem permanecer somente em
> `%AppData%\ConferenciaNFs\app-settings.json`.

---

## 1. Resumo executivo

O ConferenciaNFs começou como uma ferramenta para substituir a conferência baseada
em CSV por um fluxo compartilhado, rastreável e mais próximo do VSM. Atualmente ele:

- lê notas e itens do VSM sem gravar no ERP;
- guarda no PostgreSQL as decisões da equipe;
- permite classificar notas por cor/status;
- cria pré-notas de devolução por produto;
- mantém uma fila de devoluções com andamento;
- consulta produtos por EAN;
- compara custos com tabelas de preço;
- exporta conferências e devoluções;
- atualiza as máquinas por Velopack.

O maior risco encontrado até agora não foi a leitura do VSM, mas a definição da
identidade histórica de uma nota. Uma mesma compra pode reaparecer em outro
`DATACOMPRA`. Houve versões que moveram a linha antiga para o dia novo e versões
que mantiveram uma linha por dia. A regra atual, consolidada na 1.0.26, é:

1. O que foi conferido em um dia anterior deve permanecer naquele dia.
2. Se a nota aparecer novamente hoje, deve existir uma linha própria para hoje.
3. A linha de hoje recebe status por herança.
4. A emissão antiga, sozinha, não transforma uma nota em laranja.
5. A identidade é procurada por `CodCompra`, chave da NF-e ou
   loja + número da nota + CNPJ.

---

## 2. Princípios que não devem ser quebrados

### 2.1 VSM somente leitura

O aplicativo pode consultar `compras`, `itens_compra` e outras tabelas liberadas,
mas não deve gravar no banco do VSM. Status, observações, decisões, pré-notas,
andamentos e histórico pertencem ao PostgreSQL do ConferenciaNFs.

### 2.2 O PostgreSQL é o registro da operação

O banco compartilhado deve representar o que a equipe realmente fez:

- em qual dia a nota foi conferida;
- qual status foi escolhido;
- qual observação foi registrada;
- quais produtos foram separados para devolução;
- qual andamento foi dado;
- quando e por quem cada ação aconteceu, quando a auditoria for implantada.

### 2.3 Dias anteriores são históricos

Uma reentrada no VSM não pode transformar silenciosamente o passado. A sincronização
de terça-feira não deve retirar uma linha de sábado ou segunda-feira. A linha nova
deve ser criada para terça e herdar a situação anterior.

### 2.4 Alterações destrutivas precisam ser explícitas

Uma rotina normal de sincronização não deve apagar histórico. Exclusões administrativas
da fila ou limpezas manuais precisam continuar separadas, confirmadas pelo usuário e
documentadas.

### 2.5 Rollbacks devem ser direcionados

Voltar todo o código para uma versão anterior pode reintroduzir bugs já corrigidos.
O incidente de 20/10/2026 demonstrou que um rollback total restaura tanto o
comportamento desejado quanto os defeitos daquela versão. A partir de agora, a
preferência deve ser por reversões pequenas, por funcionalidade, com testes de
regressão.

---

## 3. Arquitetura atual

### 3.1 Componentes

- `ConferenciaNFs`: aplicativo WPF usado pela equipe.
- MySQL VSM: fonte de notas, itens, custos e EANs; somente leitura.
- PostgreSQL: notas sincronizadas e todas as decisões internas.
- GitHub Releases: origem das atualizações.
- Velopack: empacotamento, instalação e atualização automática.

### 3.2 Fluxo principal

```text
MySQL VSM
  compras / itens_compra
          |
          | leitura
          v
ConferenciaNFs
          |
          | status, observações, itens e andamento
          v
PostgreSQL compartilhado
```

### 3.3 Datas

Há três conceitos diferentes:

- `DataEmissao`: data em que a NF foi emitida.
- `DataCompra`: `DATACOMPRA` informado pelo VSM.
- `DiaConferencia`: dia operacional ao qual a linha pertence no ConferenciaNFs.

Esses campos são armazenados como texto em `dd/MM/yyyy`. Por isso, consultas de
intervalo enumeram os dias e usam igualdade, em vez de depender de `BETWEEN`.

### 3.4 Identidade

`ChaveUnica` inclui loja, número, CNPJ, dia da conferência e data da compra. Isso
permite que a mesma nota tenha uma linha em dias diferentes.

Para procurar histórico, o sistema tenta, nesta ordem lógica:

- `CodCompra`;
- chave de acesso da NF-e;
- loja + número da nota + CNPJ.

Nenhum campo isolado é suficiente:

- `CodCompra` não é uma chave global única no banco do VSM;
- número da nota pode se repetir;
- CNPJ sozinho agrupa muitas notas do mesmo fornecedor;
- fornecedor + valor pode coincidir em compras diferentes.

---

## 4. Regras atuais de status

Status disponíveis:

- Pendente: branco, ainda não conferido.
- Verde: nota correta.
- Amarelo: requer atenção ou decisão.
- Vermelho: devolução.
- Laranja: nota já conhecida de outro dia.
- Azul: absorvido.

Quando uma nota reaparece em um dia novo:

- Verde vira Laranja.
- Azul vira Laranja.
- Laranja continua Laranja.
- Amarelo continua Amarelo.
- Vermelho continua Vermelho.
- Vermelho vira Laranja quando já existe devolução registrada para a identidade.
- Pendente continua Pendente.

A herança deve olhar somente dias anteriores. Uma sincronização de sábado dentro
de um intervalo sábado–terça não pode usar a terça-feira como “histórico”.

Uma nota com emissão antiga, mas sem conferência anterior reconhecida, permanece
branca. Data de emissão não equivale a histórico de conferência.

---

## 5. Registro de incidentes e correções

### 5.1 Configuração apagada após atualização Velopack

**Sintoma**

Após atualizar o aplicativo, algumas máquinas perdiam as strings de conexão e outras
preferências.

**Causa**

O `app-settings.json` ficava dentro da pasta substituída pelo Velopack.

**Tentativas e limitações**

Tentar migrar o arquivo somente depois de iniciar a versão nova não recuperava um
arquivo que a atualização anterior já havia removido.

**Correção**

Desde a 1.0.10, a configuração real fica em `%AppData%\ConferenciaNFs`. O pacote
leva apenas `app-settings.example.json`.

**Efeito residual**

A passagem da 1.0.9 para a 1.0.10 ainda exigiu configurar uma última vez nas
máquinas em que o arquivo anterior já havia sido apagado.

---

### 5.2 Pacote apontando para banco incorreto

**Sintoma**

Uma versão empacotada podia iniciar apontando para uma base diferente da base
operacional esperada.

**Correção**

A 1.0.5 consolidou o banco correto no processo de implantação. Depois disso, a
configuração foi retirada do pacote e movida para `%AppData%`.

**Lição**

Conexões de produção não devem ser embutidas no binário nem sobrescritas por release.

---

### 5.3 CSV como fonte principal e limitações do fallback

**Problema original**

O CSV exigia exportação manual e podia atrasar a visão das notas.

**Melhoria**

A partir da 1.0.6, o aplicativo passou a sincronizar diretamente o VSM.

**Limitações que permanecem**

- O CSV continua sendo plano B.
- Todas as linhas do arquivo são atribuídas à data final do intervalo.
- O CSV não fornece `CodCompra` nem, no formato atual, toda a identidade usada pelo VSM.
- Uma importação pode criar uma linha paralela quando não encontra a mesma
  `ChaveUnica`.
- Reimportar a mesma chave atualiza dados básicos, mas não deve sobrescrever uma
  conferência já realizada.

---

### 5.4 Notas de outro dia e cor laranja

**Sintoma**

Notas com emissão anterior apareciam brancas, ou notas já conferidas reapareciam
com cor inesperada.

**Causas encontradas**

- A data de emissão foi confundida com histórico de conferência.
- Algumas notas não possuíam linha anterior no PostgreSQL.
- Linhas anteriores haviam sido movidas ou apagadas por versões que realocavam
  `CodCompra`.
- A identidade entre dias podia não coincidir.
- Um histórico mais recente ainda Pendente impedia usar um histórico colorido mais
  antigo.

**Tratamentos realizados ao longo das versões**

- 1.0.7: herança de status na sincronização VSM.
- 1.0.8: preservação do dia original e criação de Laranja no novo dia.
- 1.0.11: Pendente deixou de contar como conferência válida para gerar Laranja.
- 1.0.21: tentativa de eliminar duplicidade movendo a mesma `CodCompra`.
- 1.0.23: retorno à preservação dos dias e ajuste das regras.
- 1.0.25: rollback total reintroduziu a realocação.
- 1.0.26: correção direcionada para preservar o dia anterior e herdar no novo dia.

**Regra consolidada**

Emissão antiga não basta. É necessário encontrar uma conferência anterior pela
identidade da nota.

---

### 5.5 Incidente de 20/10/2026: poucas notas no dia atual

**Sintoma**

O VSM possuía aproximadamente 1.687 notas para 20/10, mas o dashboard exibia
aproximadamente 527.

**O que foi verificado**

- O filtro da tela lia `DiaConferencia`, não o VSM em tempo real.
- Apenas 527 linhas haviam sido efetivamente gravadas para o dia.
- Havia notas de hoje ainda localizadas em dias anteriores.
- Mais de mil notas ainda não haviam sido inseridas no PostgreSQL.
- A abertura/troca de intervalo executava sincronização com erro visual reduzido;
  certas falhas podiam resultar apenas na mensagem genérica.
- O processo local já havia terminado sem completar toda a carga.

**Tratamento usado no diagnóstico**

Foi executada a mesma sincronização do serviço, com saída detalhada. Ela levou cerca
de dois minutos e meio e informou:

- 1.689 notas lidas naquele momento;
- 1.069 inseridas;
- 620 atualizadas;
- 93 realocadas.

Depois disso, o total do dia passou a refletir as notas disponíveis naquele instante.

**Falha do tratamento**

A sincronização resolveu a quantidade, mas estava usando o comportamento da 1.0.21:
as 93 realocações podiam retirar linhas de dias anteriores. Portanto, corrigir o
total do dia expôs novamente o problema de perda do histórico diário.

**Correção estrutural**

A 1.0.26 impede realocação entre dias. Uma reentrada passa a criar/manter uma linha
no dia novo e consultar o histórico anterior.

---

### 5.6 Incidente de 20/10/2026: rollback para funcionalidades antigas

Este foi o incidente mais importante do ciclo recente.

#### Situação antes do rollback

- 1.0.22 permitia trabalhar com grupos diferentes de motivo/classe na pré-nota e
  preservava linhas de dias anteriores.
- 1.0.23 corrigia a herança para não apagar o passado e não sobrescrever status já
  conferidos.
- 1.0.24 desativava a virtualização problemática da grade da pré-nota para impedir
  que o scroll marcasse itens errados.

#### Motivo do rollback

Foi solicitado que todas as funções voltassem exatamente ao comportamento da
1.0.21. O código foi restaurado integralmente e depois publicado como 1.0.25.

#### O que o rollback resolveu

- Restaurou o comportamento conhecido da 1.0.21.
- Permitiu que a mesma `CodCompra` fosse levada ao dia novo.
- Facilitou visualizar no dia atual compras que ainda estavam associadas ao dia
  anterior.

#### O que o rollback reintroduziu

- Linhas antigas podiam ser movidas para o dia novo.
- Quando já havia destino, a origem podia ser mesclada e removida.
- Verde e Vermelho podiam permanecer com a mesma cor no dia novo em vez de passar
  pelas regras de herança.
- Correções da pré-nota feitas nas versões 1.0.22 e 1.0.24 foram perdidas.
- A documentação e comentários que diziam “o sync não apaga” ficaram incompatíveis
  com o comportamento real.

#### Evidência no banco

Durante a investigação:

- existiam notas Vermelhas no dia atual ligadas a devoluções marcadas em dias bem
  anteriores;
- essas devoluções haviam acompanhado a linha realocada;
- não havia, naquele retrato do banco, uma correspondência exata entre as notas
  Vermelhas ainda existentes em 17/10 ou 19/10 e as notas brancas de 20/10;
- alguns casos visualmente semelhantes tinham fornecedor e valor iguais, mas
  número de NF e `CodCompra` diferentes, portanto eram compras distintas.

Isso mostrou dois problemas diferentes:

1. realocação indevida de histórico verdadeiro;
2. percepção de repetição em notas que eram tecnicamente diferentes.

#### Correção final desse ponto

A 1.0.26 foi feita de forma direcionada:

- consulta de realocação limitada ao mesmo `DiaConferencia`;
- registros de outros dias permanecem intactos;
- reentrada em novo dia segue a herança;
- herança considera somente dias estritamente anteriores;
- testes cobrem a proibição de realocação entre 19/10 e 20/10.

#### Limitação

A 1.0.26 impede novas perdas, mas não consegue reconstruir automaticamente tudo o
que já foi removido. Não existe tabela de auditoria completa com estado anterior,
dia original, usuário e antes/depois. Recuperar histórico antigo exige backup ou
outra fonte confiável.

---

### 5.7 Pré-nota com vários motivos

**Sintoma original**

Ao marcar um grupo, aplicar motivo/classe, marcar outro grupo e aplicar outro motivo,
o último clique podia sobrescrever os itens que continuavam selecionados.

**Tentativa na 1.0.22**

- Aplicar motivo/classe desmarcava o grupo processado.
- A confirmação considerava linhas preenchidas, mesmo que já estivessem desmarcadas.
- Motivo e classe podiam ser ajustados por linha.

**Falha posterior**

O rollback total para a 1.0.21 removeu essa implementação.

**Estado na 1.0.26**

O comportamento atual voltou a aplicar o último motivo/classe em todas as linhas que
ainda estiverem marcadas. A confirmação considera apenas as linhas selecionadas.

**Situação**

Regressão conhecida. Deve ser reaplicada de forma isolada, sem alterar a regra de
histórico entre dias.

---

### 5.8 Scroll da pré-nota marcando outros itens

**Sintoma**

Ao rolar repetidamente a grade da pré-nota, itens adicionais podiam aparecer
marcados.

**Diagnóstico**

Não era apenas defeito visual. A virtualização e reciclagem de linhas do `DataGrid`,
com bindings de atualização imediata, podiam escrever o estado do controle reciclado
em outro objeto da lista.

**Correção na 1.0.24**

A virtualização de linhas e colunas foi desativada na grade.

**Falha posterior**

O rollback para a 1.0.21 removeu a correção.

**Estado na 1.0.26**

Regressão conhecida e prioritária. A correção da 1.0.24 precisa ser reaplicada
isoladamente e validada junto com o fluxo de vários motivos.

---

### 5.9 Motivo e classe desaparecendo ao reabrir pré-nota

**Sintoma**

Uma pré-nota salva podia ser reaberta sem mostrar corretamente motivo e classe.

**Correção**

A 1.0.20 restaurou o carregamento dos valores gravados.

**Risco atual**

Mudanças posteriores no modelo de seleção e confirmação precisam preservar essa
leitura. Reaplicar a solução de vários motivos sem teste pode reintroduzir o problema.

---

### 5.10 Custos de itens

**Problemas encontrados**

- O custo exibido precisava considerar o valor de compra menos desconto.
- Na loja 29, o cálculo também precisava incluir ICMS-ST e FCP-ST.
- EANs com zeros à esquerda não encontravam corretamente a tabela de preço.

**Correções**

- 1.0.13: custo líquido após desconto.
- 1.0.19: comparação de EAN mais tolerante.
- 1.0.21: ST e FCP-ST para a loja 29.

**Cuidados**

Regras fiscais por loja não devem ser generalizadas para todas as unidades sem
validação.

---

### 5.11 Fila de devoluções

**Problemas tratados**

- necessidade de prazo por distribuidora;
- andamento da devolução;
- pré-nota por item;
- quantidade, motivo e classe;
- status Absorvido;
- histórico de observações;
- exportação;
- corte de data da fila;
- reabertura em modo somente leitura após conclusão.

**Riscos**

- A identidade da devolução acompanha a identidade da nota.
- Uma mescla errada de notas pode transferir a devolução para outra linha.
- Remoções administrativas anteriores à data mínima são destrutivas e devem
  continuar exigindo confirmação.
- Vermelho com devolução registrada vira Laranja em uma nova ocorrência; hoje a
  regra considera qualquer devolução registrada, não apenas concluída.

---

### 5.12 Falhas de conexão PostgreSQL

**Sintoma**

`Exception while reading from stream` ao atualizar status ou gravar dados.

**Diagnóstico**

Uma conexão ociosa do pool pode estar inválida depois de interrupções de rede ou
encerramento pelo servidor.

**Tratamento atual**

Há proteção na abertura da conexão, mas não retry completo na operação de gravação.

**Por que não foi concluído**

O problema foi adiado para priorizar o fluxo de negócio. Um retry mal implementado
pode repetir operações não idempotentes.

**Tratamento futuro recomendado**

- retry somente para falhas transitórias conhecidas;
- reabrir a conexão e repetir a transação inteira;
- revisar idempotência de cada comando;
- configurar keepalive;
- registrar telemetria da falha sem expor credenciais.

---

### 5.13 Permissões limitadas no MySQL

O usuário de leitura do VSM não possui acesso estável a todas as tabelas. A leitura
do nome do fornecedor pode falhar; nesse caso o aplicativo continua com CNPJ e
outros dados disponíveis.

Isso não deve interromper a sincronização principal. Funcionalidades futuras de
fabricante, estoque e roteamento dependem de permissões adicionais.

---

### 5.14 Sincronização silenciosa e diagnóstico difícil

Ao abrir ou trocar intervalo, algumas exceções de sincronização são apresentadas
apenas como mensagem genérica. O botão manual mostra mais detalhes.

Isso contribuiu para o caso em que a tela mostrava 527 notas enquanto o VSM já tinha
mais de 1.600.

Melhoria pendente:

- registrar início, fim, duração, lidas, inseridas, atualizadas, ignoradas e erro;
- mostrar último sync concluído;
- diferenciar “carregando”, “sincronização falhou” e “dados atualizados”;
- permitir exportar um diagnóstico sem incluir strings de conexão.

---

### 5.15 Segunda-feira e “Desde sábado”

O preset seleciona do sábado mais recente até hoje. O sync manual processa cada dia
do intervalo. O timer automático, porém, revê apenas ontem e hoje.

Isso é intencional para reduzir custo, mas exige atenção:

- abrir o intervalo desde sábado ou clicar em sincronizar cobre todo o período;
- manter apenas hoje aberto não relê sábado;
- a herança pode consultar o histórico já gravado, sem reler o VSM daquele sábado.

---

### 5.16 Build e publicação

Problemas operacionais já observados:

- build falhando por DLL bloqueada pelo aplicativo ou por outro processo .NET;
- build e testes em paralelo disputando o mesmo arquivo temporário;
- binários sem assinatura, gerando SmartScreen;
- ferramenta `vpk` local desatualizada em relação à versão mais recente;
- publicação manual, sem pipeline de CI;
- comandos de PowerShell diferem de Bash (`&&` e heredoc não são portáveis para
  todas as versões usadas);
- push direto para `main` exige atenção e confirmação.

Tratamento atual:

- repetir build isoladamente quando houver lock;
- rodar testes antes da release;
- usar o script `scripts/release.ps1`;
- manter `app-settings.json` fora do pacote;
- não atualizar ferramentas de empacotamento no meio de uma correção sem necessidade.

---

### 5.17 Testes

Pontos positivos:

- regras de herança possuem testes;
- regras de realocação e comparação de dia possuem testes;
- parsers, custos, EAN e partes da devolução possuem cobertura.

Limitações:

- dois testes de importação CSV estão ignorados porque dependem de arquivos locais;
- o projeto de testes não está incluído na solução principal;
- não há teste de integração automatizado contra um banco PostgreSQL descartável;
- não há teste ponta a ponta do update Velopack;
- não há CI executando build e testes em cada push;
- boa parte das regressões recentes ocorreu na interação entre banco, sync e UI,
  além do alcance dos testes unitários atuais.

---

## 6. Linha do tempo resumida

### Fundação e distribuição

- 1.0.0: empacotamento Velopack e melhorias iniciais de conferência.
- 1.0.3: fluxo de devoluções e distribuidoras.
- 1.0.4: contraste do Amarelo no tema escuro.
- 1.0.5: banco operacional corrigido.
- 1.0.6: sincronização VSM, itens e pesquisa por EAN.

### Histórico e intervalo

- 1.0.7: herança de status na leitura do VSM.
- 1.0.8: manter nota conferida no dia original.
- 1.0.9: intervalo manual de `DATACOMPRA`.
- 1.0.10: configurações em `%AppData%`.
- 1.0.11: somente status realmente conferido gera herança.

### Preço, devolução e experiência

- 1.0.12: comparação com tabelas compartilhadas e filtro por loja.
- 1.0.13: custo após desconto.
- 1.0.14: pré-nota, andamento e sincronização mais rápida.
- 1.0.15–1.0.19: janela integrada, progresso, pesquisa, cards, filtros e EAN.
- 1.0.20: restauração de motivo e classe ao reabrir pré-nota.

### Ciclo crítico 1.0.21–1.0.26

- 1.0.21: `CodCompra` passa a ser realocada quando muda `DATACOMPRA`; inclui ST da
  loja 29.
- 1.0.22: múltiplos motivos na devolução e preservação de cada dia.
- 1.0.23: regras de herança corrigidas sem apagar o dia anterior.
- 1.0.24: correção do scroll que marcava itens.
- 1.0.25: rollback total para o comportamento da 1.0.21; reintroduziu realocação
  entre dias e removeu correções recentes da pré-nota.
- 1.0.26: correção direcionada da preservação histórica e da herança somente de
  dias anteriores.
- Código posterior à 1.0.26: origem do status (`Manual`/`Herdado`/`Sistema`),
  vínculo da herança, trigger `NotaStatusHistorico` e proteção contra sync
  sobrescrever escolha do operador. Recuperação conservadora das cinco notas
  confirmadas da loja 36 (19/10 Verde, 20/10 Laranja herdado).

---

## 7. Problemas conhecidos após a correção de origem do status

Prioridade alta:

1. Reaplicar a correção de virtualização da pré-nota, evitando marcação por scroll.
2. Reaplicar o fluxo seguro de vários motivos/classes sem sobrescrever grupos.
3. Melhorar visibilidade de falhas de sincronização.

Prioridade técnica:

1. Retry transacional para falhas transitórias do PostgreSQL.
2. CI com build e testes.
3. Teste de integração com PostgreSQL descartável.
4. Assinatura digital dos binários.
5. Atualização planejada do Velopack/vpk.
6. Remoção ou reativação consciente de código sem chamadores.
7. Tirar os testes CSV da dependência de arquivos do Desktop.

Limitações de dados:

1. A auditoria começa a partir da migração de origem do status. Alterações
   anteriores à 1.0.25/1.0.26 não têm antes/depois.
2. Sem backup pré-incidente, só as cinco notas confirmadas da loja 36 foram
   restauradas automaticamente. Outras possíveis realocações ficam no relatório.
3. Dados do VSM podem mudar de `DATACOMPRA`.
4. Identidades incompletas ou alteradas entre dias impedem herança.
5. CSV não possui a mesma riqueza de identidade da sincronização VSM.

---

## 8. Melhorias em espera

### 8.1 App dos compradores

É o próximo objetivo de negócio.

O Amarelo deixará de ser apenas uma cor. A conferência deverá:

- selecionar os produtos com divergência;
- indicar a categoria ou fabricante;
- encaminhar a pendência ao comprador responsável;
- registrar prazo, resposta e histórico.

O comprador poderá:

- aceitar a divergência e devolver a nota à conferência como Verde, com observação;
- decidir pela devolução e enviar a nota para a fila já existente;
- registrar comentário e justificativa.

### 8.2 Usuários e auditoria

Antes de distribuir o fluxo a vários compradores, o sistema precisa de identidade
individual:

- usuário;
- aplicação de origem;
- ação;
- valor anterior;
- valor novo;
- data e hora;
- nota e item afetados.

Essa camada teria evitado a dificuldade de reconstruir o histórico removido durante
as realocações.

### 8.3 Roteamento entre equipes

Inicialmente pode ser manual. Depois:

- fabricante/categoria para comprador;
- lista de pendências por responsável;
- histórico visível para conferência, comprador e devolução;
- notificações internas.

### 8.4 Pedido salvo versus NF

Está tecnicamente viável, mas continua em espera.

O cruzamento não pode usar apenas CNPJ. A combinação útil inclui:

- loja;
- CNPJ;
- itens;
- quantidades;
- janela de data;
- situação do pedido.

O resultado esperado seria “bate”, “bate parcialmente” ou “não encontrado”.

### 8.5 Integração SEFAZ

Permanece em espera. Exige certificado digital e operação fiscal segura. Com os
itens já disponíveis no VSM, só se justifica para obter XML completo ou cobrir notas
ausentes no ERP.

### 8.6 Fabricante, estoque e automação

Dependem de tabelas e permissões adicionais no VSM. Incluem:

- cadastro confiável de fabricante;
- estoque oficial;
- roteamento automático;
- regras de priorização;
- alertas.

### 8.7 Resiliência e observabilidade

- retry transacional;
- keepalive;
- log estruturado;
- tela de saúde da sincronização;
- duração e contadores por dia;
- identificação de notas ignoradas;
- alerta quando VSM e PostgreSQL divergem significativamente.

---

## 9. Objetivo final do ConferenciaNFs

O objetivo não é apenas uma tela de conferência. É formar um núcleo operacional
próprio, preservando o VSM como fonte somente leitura.

```text
                         MySQL VSM
                       somente leitura
                              |
                              v
                    PostgreSQL compartilhado
             notas, itens, usuários, auditoria,
          pendências, pré-notas, devoluções e histórico
                  /             |              \
                 v              v               v
          ConferenciaNFs    Compradores      Devolução
             WPF atual       app novo       módulo/app
```

Resultado esperado:

- Conferência identifica e classifica.
- Compradores decidem divergências por produto.
- Devolução executa e acompanha o retorno.
- Todos usam o mesmo histórico.
- Nenhuma decisão depende de alterar o VSM.
- Toda ação relevante é auditável.
- Atualizações não perdem configuração nem dados.
- Uma nota pode reaparecer sem apagar o que foi feito antes.

O aplicativo atual não deve ser descartado. Ele é a primeira frente desse
ecossistema. O caminho planejado é evoluir o PostgreSQL compartilhado, manter o
ConferenciaNFs como aplicação da conferência e criar o app dos compradores sobre o
mesmo núcleo.

---

## 10. Regras para próximas alterações

Antes de alterar sincronização, identidade ou status:

1. Registrar claramente a regra de negócio.
2. Criar teste que reproduza o defeito.
3. Testar sábado → segunda → terça.
4. Testar nota Pendente, Verde, Azul, Amarela, Vermelha e Laranja.
5. Testar com e sem devolução.
6. Verificar que o dia anterior continua no banco.
7. Comparar quantidade e valor entre VSM e PostgreSQL.
8. Evitar rollback total quando o problema é localizado.
9. Não publicar sem build e testes.
10. Não executar recuperação destrutiva sem backup.

Antes de alterar a pré-nota:

1. Testar múltiplos grupos de motivo/classe.
2. Rolar a grade repetidamente.
3. Confirmar quais itens efetivamente serão gravados.
4. Reabrir a pré-nota.
5. Validar motivo, classe, quantidade e seleção.
6. Testar com virtualização ligada e desligada.

Antes de publicar:

1. Atualizar versões do projeto e documentação.
2. Rodar `dotnet test`.
3. Rodar build Release isoladamente.
4. Conferir que `app-settings.json` não está no pacote.
5. Conferir notas da release.
6. Publicar commit e tag correspondentes.
7. Validar a atualização em uma máquina antes da implantação ampla.

---

## 11. Escopo da garantia “nada deve ser apagado”

Na 1.0.26, essa garantia significa que uma sincronização de um novo dia não pode
apagar ou mover uma linha de um dia anterior.

Ainda existem operações diferentes que podem remover ou consolidar dados:

- correção de duplicidade técnica dentro do mesmo dia;
- limpeza administrativa explicitamente acionada;
- remoção confirmada na fila de devoluções;
- políticas de corte de dados da fila.

Para uma garantia absoluta de retenção, a evolução correta é:

- substituir exclusão física por arquivamento ou soft delete;
- criar auditoria antes/depois;
- restringir exclusões a perfis autorizados;
- exigir confirmação e motivo;
- manter backup e política de restauração.

Essa evolução ainda não foi implementada.

---

## 12. Fontes de verdade

- `README.md`: instalação, uso e arquitetura resumida.
- `README-DETALHADO.md`: histórico de incidentes, limitações e visão futura.
- `ConferenciaNFs-Documentacao.html`: documentação visual interna, atualmente
  defasada em relação à versão 1.0.26 em alguns trechos.
- Histórico Git: mudanças publicadas por versão.
- Código e testes da versão atual: comportamento efetivo.

Quando documentação e código divergirem, a divergência deve ser registrada e
corrigida; não se deve assumir que comentários antigos representam o comportamento
em produção.
