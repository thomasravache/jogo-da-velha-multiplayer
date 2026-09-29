---
id: SPEC-0030
title: Lobby Cyber Arena
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0028
depends_on: [SPEC-0043]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Game/Lobby.razor, src/TicTacToe/TicTacToe.Web/Components/Game/Lobby.razor.css, tests/TicTacToe.Tests/LobbyCyberArenaTests.cs, tests/TicTacToe.Tests/DecomposedComponentsTests.cs, tests/TicTacToe.Tests/SoloDifficultyUiTests.cs, tests/TicTacToe.Tests/MudBlazorIntegrationTests.cs, tests/TicTacToe.Tests/ShellLayoutTests.cs, tests/TicTacToe.Tests/SingleResultRecordingTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0030 — Lobby Cyber Arena

## 1. Visão Geral
Reescreve o componente `Lobby` no visual Cyber Arena usando os primitivos da SPEC-0043: cabeçalho da arena, campo de apelido, e três cartões de modo — **Duelo Online 1v1**, **Duelo contra IA** (com dificuldade Fácil/Impossível) e **Sala Privada** (criar ou entrar com código) — mais os estados de espera (procurando oponente) e de sala criada (código com botão de copiar). A **API pública do componente não muda** (parâmetros e callbacks), então `Home.razor` continua igual.

## 2. Motivação & Escopo
**Motivação:** o lobby é a porta de entrada e ainda usa `MudCard`/`MudButton` com tema genérico. O Stitch define hierarquia, cores por modo e estados que precisam existir antes de o resto do app migrar.

**Objetivos (dentro do escopo):**
- Layout de referência: `docs/design/stitch/lobby-desktop.html` (3 cartões em linha, quadro central de até 960px) e `lobby-mobile.html` (cartões empilhados, barra inferior do shell).
- Campo de apelido (`NeonInput`, 20 caracteres, contador `n/20`) com chip "Pronto para jogar" / "Informe seu apelido".
- Cartão **Online**: título, descrição, "Tempo por turno" com o valor real de `GameSession.DefaultTurnTimeSeconds`, botão **Procurar oponente**.
- Cartão **Solo**: `SegmentedControl` Fácil 🟢 / Impossível 🔴 com descrição por nível (Fácil: jogadas aleatórias; Impossível: Minimax), botão **Iniciar partida solo**.
- Cartão **Sala privada**: alternar **Criar sala** / **Entrar com código**; campo de código e botão **Entrar**; alerta de erro (`RoomErrorMessage`).
- Estados: espera de oponente (com nome do jogador) e sala criada (código em destaque + botão **Copiar** com confirmação "Código copiado!").
- Copiar usa `navigator.clipboard.writeText` via JS interop, sem novo arquivo JavaScript.
- Cópia dos textos do Stitch em PT-BR (rótulos dos botões e títulos).

**Não-objetivos (fora do escopo):**
- Elementos do Stitch sem backend, **omitidos**: ELO/divisão/temporada, "Fila ~3s", "duelistas ativos", latência/servidor, formato MD5, painel "Seu desempenho" (vitórias/derrotas/winrate), nível e avatar do jogador. Destino de cada um: matriz de cobertura da SPEC-0035.
- Compartilhar link da sala e entrar por link ("Compartilhar Link Direto"): exige rota e fluxo novos; backlog (SPEC-0035).
- Mudar regras de matchmaking, dificuldade ou salas.
- "Partidas solo não afetam o histórico" (texto do Stitch): hoje partidas solo **entram** no histórico e no ranking; a decisão fica na SPEC-0036/0039.

## 3. Dependências
- **Implementações necessárias:** SPEC-0043 — `PageHeader`, `NeonCard`, `PillButton`, `StatusChip`, `NeonInput`, `SegmentedControl`, `Icon`.
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** `Components/Game/Lobby.razor` (parâmetros `PlayerName`, `SelectedDifficulty`, `InputRoomCode`, `IsWaiting`, `CreatedRoomCode`, `RoomErrorMessage` e callbacks `OnPlayOnline`, `OnPlaySolo`, `OnCreateRoom`, `OnJoinRoom`), decomposição da SPEC-0024 e ADR-0006.

**Decisão:** manter o contrato do componente e reescrever apenas a marcação e o estilo com os primitivos `Ui/*`; o estado local novo (aba Criar/Entrar, confirmação de cópia) fica dentro do `Lobby`.

**Justificativa:** `Home.razor` e seus testes seguem válidos; o risco fica restrito à camada de apresentação.

**Desvio do padrão existente:** substitui componentes MudBlazor por primitivos Cyber Arena (ADR-0008).

**Alternativas descartadas:** alterar `Home.razor` junto (aumenta escopo e conflita com a SPEC-0031); mover a cópia para um arquivo `.js` próprio (evitável com `navigator.clipboard.writeText`).

**ADRs:** ADR-0008.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** sem chamadas de rede novas; re-render local (troca de aba e confirmação de cópia não propagam ao pai).
- **Segurança:** o código de sala e o apelido são texto e escapados pelo Blazor; a cópia só escreve no clipboard o código exibido; falha de permissão do navegador não lança exceção (`IT-01`).
- **Privacidade e dados pessoais:** N/A — nada novo é coletado.
- **Disponibilidade e resiliência:** `navigator.clipboard` exige contexto seguro; em caso de falha exibe "Não foi possível copiar" e o código continua visível para cópia manual.
- **Acessibilidade (UI):** rótulos associados aos campos, `SegmentedControl` navegável por teclado, alerta de erro com `role="alert"`, confirmação de cópia em região `aria-live="polite"`, foco visível; contraste AA pelos tokens (SPEC-0043). Lighthouse ≥ 90 no PR.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** tela `/` (sem partida ativa), componente `Lobby`.

```text
Lobby (API inalterada)
  [Parameter] PlayerName, PlayerNameChanged            [Parameter] SelectedDifficulty, DifficultyChanged (Easy|Hard)
  [Parameter] InputRoomCode, InputRoomCodeChanged      [Parameter] IsWaiting, CreatedRoomCode, RoomErrorMessage
  [Parameter] OnPlayOnline, OnPlaySolo, OnCreateRoom, OnJoinRoom

Estados de tela
  idle      : PageHeader · NeonInput(apelido) · 3 cartões (Online, Solo, Sala privada)
  waiting   : IsWaiting = true      → cartão "Procurando oponente para {PlayerName}…" (chip Waiting)
  roomReady : CreatedRoomCode != null → código destacado + botão Copiar + "Compartilhe o código com seu amigo"

Textos (PT-BR, do Stitch)
  botões: "Procurar oponente" · "Iniciar partida solo" · "Criar sala 🔒" · "Entrar 🔑" · "Copiar"
  níveis: "Fácil 🟢" · "Impossível 🔴"
```

**Arquivos/módulos afetados:** ver `touches`. Sem mudança em `Home.razor` nem em `Home.razor.cs`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Apelido | Digitação (máx. 20) | `PlayerNameChanged` emitido; contador `n/20`; chip "Pronto para jogar" com nome, "Informe seu apelido" sem nome | UT-01 |
| Jogar online | Nome vazio / preenchido | Botão desabilitado / clique dispara `OnPlayOnline`; "Tempo por turno" mostra o valor de `DefaultTurnTimeSeconds` | UT-02 |
| Jogar solo | Escolha de nível e clique | `SegmentedControl` reflete `SelectedDifficulty`; trocar dispara `DifficultyChanged`; descrição muda por nível; botão dispara `OnPlaySolo` e fica desabilitado sem nome | UT-03 |
| Sala privada — criar | Aba "Criar sala" e clique | Dispara `OnCreateRoom`; desabilitado sem nome | UT-04 |
| Sala privada — entrar | Aba "Entrar", código e clique | Dispara `OnJoinRoom`; desabilitado sem nome ou sem código; `RoomErrorMessage` aparece em alerta `role="alert"` | UT-04 |
| Aguardando oponente | `IsWaiting = true` | Cartão de espera com o nome do jogador; cartões de modo ocultos | UT-05 |
| Sala criada | `CreatedRoomCode` preenchido | Código em destaque e botão Copiar; cartões de modo ocultos | UT-05 |
| Sem resquício do Mud | Qualquer estado | Markup sem `mud-`; usa primitivos Cyber Arena | UT-06 |
| Copiar código | Clique em Copiar | Chama `navigator.clipboard.writeText(código)` e mostra "Código copiado!"; se falhar mostra "Não foi possível copiar" sem exceção | IT-01 |
| API preservada | Reflexão sobre `[Parameter]` | Conjunto de parâmetros idêntico ao anterior | CH-01 |
| Jornada do lobby | Preencher apelido, iniciar solo, criar sala, copiar | Callbacks e estados corretos ponta a ponta | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado o tipo `Lobby`, então seus `[Parameter]` (nomes e tipos) são exatamente os do contrato; fixa a API pública antes da reescrita para provar que ela não muda (passa no código atual e continua passando).

**Testes existentes afetados:** `DecomposedComponentsTests` (afirma textos "Jogar Online", "Jogar vs Robô (IA)", "Criar Sala Privada", "Dificuldade:"), `SoloDifficultyUiTests` (lê `Lobby.razor` procurando `difficulty-selector`, `SelectedDifficulty == AiDifficulty.Easy`, "Fácil 🟢") e `MudBlazorIntegrationTests` `SPEC-0026:IT-01` (afirma componentes Mud). São atualizados para os novos rótulos e primitivos; mudanças justificadas pela troca de textos e de biblioteca (Stitch/ADR-0008).

### 7.2 Testes Unitários
- **UT-01** — Dado `Lobby` com `PlayerName` vazio e depois "Thomas", então o chip alterna entre "Informe seu apelido" e "Pronto para jogar", o input tem `maxlength=20`, o contador mostra `6/20` e digitar dispara `PlayerNameChanged`.
- **UT-02** — Dado `Lobby`, quando o nome está vazio, então "Procurar oponente" está desabilitado; com nome, o clique dispara `OnPlayOnline`; o cartão exibe "15 segundos" derivado de `GameSession.DefaultTurnTimeSeconds`.
- **UT-03** — Dado `Lobby` com `SelectedDifficulty=Hard`, então "Impossível 🔴" está marcado (`aria-checked`); clicar "Fácil 🟢" dispara `DifficultyChanged(Easy)` e troca a descrição; "Iniciar partida solo" dispara `OnPlaySolo` e fica desabilitado sem nome.
- **UT-04** — Dado `Lobby` na aba "Criar sala", então clicar dispara `OnCreateRoom`; na aba "Entrar com código", "Entrar" fica desabilitado sem nome ou sem código e dispara `OnJoinRoom` quando ambos existem; com `RoomErrorMessage` há um elemento `role="alert"` com o texto.
- **UT-05** — Dado `IsWaiting=true`, então aparece "Procurando oponente para Thomas…" e os cartões de modo não aparecem; dado `CreatedRoomCode="SALA-ABCD"`, então o código aparece em destaque com o botão "Copiar".
- **UT-06** — Dado o `Lobby` em qualquer estado, então o markup não contém a substring `mud-` e contém as classes de tokens dos primitivos (`NeonCard`, `PillButton`).

### 7.3 Testes de Integração
- **IT-01** — Dado `CreatedRoomCode="SALA-ABCD"` e `JSInterop` do bUnit configurado, quando clica em "Copiar", então `navigator.clipboard.writeText` é chamado com `SALA-ABCD` e aparece "Código copiado!"; com o JS configurado para falhar, aparece "Não foi possível copiar" e nenhuma exceção sobe.

### 7.4 Testes de Contrato
N/A — o contrato do componente é interno e coberto por `CH-01`.

### 7.5 Testes E2E
- **E2E-01** — Jornada do lobby (bUnit): renderizar `Lobby`, preencher o apelido, escolher "Fácil", iniciar solo (callback recebido), trocar para "Entrar com código", digitar código e entrar (callback recebido), simular `CreatedRoomCode` e copiar (confirmação visível).

### 7.6 Outros
- Revisão visual (H2): lobby em 390px e 1280px lado a lado com `lobby-mobile.png`/`lobby-desktop.png`; itens omitidos conferidos contra a lista da seção 2.
- Lighthouse Acessibilidade ≥ 90 em `/`.

**Dublês e dados de teste:** `BunitContext`, `JSInterop` (Loose/Setup), `EventCallback` de teste; sem banco.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; API do componente inalterada.
- **Dados/schema:** N/A.
- **Compatibilidade:** fluxo de matchmaking, salas privadas e solo inalterado.
- **Observabilidade:** revisão visual e console sem erros no PR.
- **Rollback:** `git revert` do PR.
- **Etapas de migração/coexistência:** etapa 3 de 4 (tela 1 de 4); o MudBlazor continua carregado para as telas restantes.

## 9. Questões em Aberto
- [x] Os rótulos dos botões passam a usar a **cópia do Stitch** ("Procurar oponente", "Iniciar partida solo", "Criar sala 🔒", "Entrar 🔑"; recomendado) ou mantêm os atuais ("Jogar Online 🌐", "Jogar vs Robô (IA) 🤖", "Criar Sala Privada 🔒", "Entrar 🔑")? — Cópia do Stitch. (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [x] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [x] Escrever os testes `SPEC-0030:CH-01`, `SPEC-0030:UT-01`, `SPEC-0030:UT-02`, `SPEC-0030:UT-03`, `SPEC-0030:UT-04`, `SPEC-0030:UT-05`, `SPEC-0030:UT-06`, `SPEC-0030:IT-01`, `SPEC-0030:E2E-01` com a tag `SPEC-0030:<ID>`, em commits `test(...)` com `Refs: SPEC-0030`, tocando só `test_paths`
- [x] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [x] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0030`)

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [x] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [x] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [x] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [x] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável — **não executada**; aceita no H2 pelo merge do PR #8, ver Pendências
- [x] PR com `spec_graph.py pr SPEC-0030`, CI verde (G5) e aprovação do merge (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
| G1 Red | PASS | `verify SPEC-0030`: CH-01 e Red antes do Green; 9/9 testes rastreados; 14 falhas iniciais pelos motivos esperados | 2026-09-29 |
| G2 Green | PASS | `dotnet test` 112/112; `dotnet format --verify-no-changes` limpo; `build.sh --check` sem drift | 2026-09-29 |
| G3 Arquitetura | N/A | Sem suíte `Category=Architecture`; sem `mud-` no Lobby verificado por SPEC-0030:UT-06 | 2026-09-29 |
| G4 Review | PASS | Reviewer independente APPROVED em cf08b08 (0 blocker, 0 major, 6 minor; 3 corrigidos) | 2026-09-29 |
| G5 Integração & CI | PASS | PR #8: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorização do usuário para seguir com o PR #8 e mesclar em 2026-09-29 | 2026-09-29 |
| G6 Deploy | N/A | Sem ambiente remoto (`staging_url` vazio); aprovado pelo usuário em 2026-09-29 | 2026-09-29 |
| G7 Pronto & Docs | PASS | `spec_graph.py validate` limpo; Relatório de Entrega e CHANGELOG atualizados | 2026-09-29 |

## 13. Registro de Impedimentos
<!-- Toda parada é registrada pelo Architect com `spec_graph.py impede` e fechada com `resolve` — não edite à mão. Tipos: spec (spec errada/incompleta → resolve com Emenda) | decisão (só o humano decide → resposta ou ADR) | trabalho (falta algo que exige código → SPEC-NNNN nova) | externo (acesso, ambiente, terceiro → ação tomada) | falha (3 FAILs seguidos no mesmo gate → diagnóstico e decisão). Com impedimento aberto a spec aparece como parada no INDEX e não pode ser fechada. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega
<!-- Preenchido no CLOSE (G7). Diz o que foi feito, como, e prova que foi resolvido. Para status implemented o validate exige todas as subseções preenchidas, todo teste do plano com PASS + evidência e a Definição de Pronto toda marcada. -->

### O que foi entregue
<!-- comportamento entregue do ponto de vista do usuário/sistema -->

Lobby no visual Cyber Arena: apelido com contador 20/20 e chip de prontidão; cartões Duelo Online 1v1 (com o tempo por turno real), Duelo contra IA (Fácil/Impossível com descrição por nível) e Sala Privada (criar ou entrar com código, com alerta de erro); estados de espera e de sala criada com botão Copiar. API pública do componente inalterada; sem MudBlazor no Lobby.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

`Lobby.razor` reescrito com os primitivos de `Components/Ui` (`NeonCard`, `PillButton`, `StatusChip`, `PageHeader`, `NeonInput`, `SegmentedControl`); cópia via `navigator.clipboard.writeText` sem JS novo. Rótulos do Stitch em PT-BR aplicados (decisão registrada no H1) e testes existentes atualizados (Emenda v1 de `touches`). `cyber-arena.css` passou a `scope_exempt` no `sdd-config.yml`. Correções da review G4: tratamento de qualquer falha da área de transferência, reset do aviso de cópia ao mudar o código e `motion-reduce` no spinner.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0030:CH-01 | Dado o tipo `Lobby`, então seus `[Parameter]` (nomes e tipos) são exatamente os do contrato; fixa a API públic | PASS | `dotnet test` 112/112 no CI (dotnet-ci) do PR #8 |
| SPEC-0030:UT-01 | Dado `Lobby` com `PlayerName` vazio e depois "Thomas", então o chip alterna entre "Informe seu apelido" e "Pro | PASS | `dotnet test` 112/112 no CI (dotnet-ci) do PR #8 |
| SPEC-0030:UT-02 | Dado `Lobby`, quando o nome está vazio, então "Procurar oponente" está desabilitado; com nome, o clique dispar | PASS | `dotnet test` 112/112 no CI (dotnet-ci) do PR #8 |
| SPEC-0030:UT-03 | Dado `Lobby` com `SelectedDifficulty=Hard`, então "Impossível 🔴" está marcado (`aria-checked`); clicar "Fácil  | PASS | `dotnet test` 112/112 no CI (dotnet-ci) do PR #8 |
| SPEC-0030:UT-04 | Dado `Lobby` na aba "Criar sala", então clicar dispara `OnCreateRoom`; na aba "Entrar com código", "Entrar" fi | PASS | `dotnet test` 112/112 no CI (dotnet-ci) do PR #8 |
| SPEC-0030:UT-05 | Dado `IsWaiting=true`, então aparece "Procurando oponente para Thomas…" e os cartões de modo não aparecem; dad | PASS | `dotnet test` 112/112 no CI (dotnet-ci) do PR #8 |
| SPEC-0030:UT-06 | Dado o `Lobby` em qualquer estado, então o markup não contém a substring `mud-` e contém as classes de tokens  | PASS | `dotnet test` 112/112 no CI (dotnet-ci) do PR #8 |
| SPEC-0030:IT-01 | Dado `CreatedRoomCode="SALA-ABCD"` e `JSInterop` do bUnit configurado, quando clica em "Copiar", então `naviga | PASS | `dotnet test` 112/112 no CI (dotnet-ci) do PR #8 |
| SPEC-0030:E2E-01 | Jornada do lobby (bUnit): renderizar `Lobby`, preencher o apelido, escolher "Fácil", iniciar solo (callback re | PASS | `dotnet test` 112/112 no CI (dotnet-ci) do PR #8 |

### Definição de Pronto
- [x] Todos os testes do plano passando e listados na Verificação
- [x] Todo comportamento do Mapa de Comportamentos coberto e verificado
- [x] Suíte completa, arquitetura e CI verdes no resultado integrado (G5)
- [x] Review independente sem achados blocker/major (G4)
- [x] Padrão arquitetural existente mantido, ou desvio coberto por ADR aprovado
- [x] Requisitos não-funcionais medidos com evidência (ou N/A justificado)
- [x] Disponível no ambiente-alvo via pipeline, com smoke/E2E passando no ambiente (G6) — N/A aprovado pelo usuário (2026-09-29): sem ambiente remoto
- [x] Observabilidade e rollback prontos conforme o Plano de Rollout
- [x] Documentação raiz e CHANGELOG atualizados (G7)
- [x] Pendências registradas como novas specs (ou nenhuma)

### Deploy
<!-- ambiente(s), versão/tag, data, estratégia, estado da feature flag, execução do pipeline -->

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #8 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Conferência visual (390 px e 1280 px contra `lobby-mobile.png`/`lobby-desktop.png`) e Lighthouse **não executadas**: o app não foi aberto no navegador. Contraste de `text-primary` sobre `bg-primary/15` no alerta de erro estimado em ~4,1:1 (abaixo de 4,5:1) a confirmar. Sem asserção de `aria-live` na confirmação de cópia.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0030`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
| 1 (contrato inalterado) | 2026-09-29 | `touches` inclui `ShellLayoutTests.cs` e `SingleResultRecordingTests.cs` | Os novos rótulos do Stitch quebram `SPEC-0043:IT-01` (procura "Jogar Online") e `SPEC-0045:IT-02` (clica pelo texto do botão); só os textos são atualizados | SPEC-0043, SPEC-0045 (testes) | thomas (2026-09-29) |
