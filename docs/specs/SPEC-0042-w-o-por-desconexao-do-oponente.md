---
id: SPEC-0042
title: W.O. por desconexão do oponente
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0035
depends_on: [SPEC-0041]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Web/Services/Presence/**, src/TicTacToe/TicTacToe.Web/Program.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs, src/TicTacToe/TicTacToe.Web/Components/Game/Scoreboard.razor, tests/TicTacToe.Tests/DisconnectForfeitTests.cs, tests/TicTacToe.Tests/PresenceCircuitHandlerTests.cs, tests/TicTacToe.Tests/DisconnectUiTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0042 — W.O. por desconexão do oponente

## 1. Visão Geral
Quando o oponente perde a conexão no meio de uma partida entre humanos, o jogador que ficou passa a ver um aviso com contagem regressiva e, se o oponente não voltar dentro da **tolerância** (15 s), vence por **W.O.** com motivo "Desconexão do oponente". Reconectando a tempo, a partida continua. O histórico já tem o texto desse motivo (SPEC-0038).

## 2. Motivação & Escopo
**Motivação:** hoje, se o oponente fecha a aba ou perde a rede, o jogador fica esperando: só o timer do turno (15 s) resolve, e apenas se for a vez do desconectado. O Stitch mostra "Vitória W.O. — Desconexão do oponente".

**Objetivos (dentro do escopo):**
- Referência: `docs/design/stitch/historico-desktop.html` (linha "Desconexão do oponente") e `arena-desktop.html`.
- `GameSession` registra a conexão de cada jogador humano (`SetConnection`) e, no `Tick` existente, encerra a partida com vitória do outro lado por `EndReason.Disconnect` quando a tolerância se esgota; reconexão cancela a contagem.
- `Forfeit(Player loser, EndReason reason)` público e único caminho de desistência (`Leave`, da SPEC-0041, passa a usá-lo com `Abandon`); em série, a desconexão encerra a série (mesma regra do abandono).
- `MatchPresenceCircuitHandler` (`CircuitHandler` do Blazor Server) informa queda, retorno e fechamento do circuito do jogador que está numa partida humana.
- `Home` associa e desassocia o jogador à partida (contexto de presença por circuito).
- Arena: aviso "Oponente desconectado. Aguardando reconexão… {n}s" e, ao esgotar, "Oponente desconectou. Vitória por W.O.".

**Não-objetivos (fora do escopo):**
- Partidas solo (não há oponente humano).
- Presença geral no lobby, "online agora", ping e latência (backlog J).
- Retomar a partida depois de recarregar a página (o novo circuito não recupera a partida; o F5 conta como desconexão).
- Persistir partidas quando **nenhum** circuito permanece vivo (aceito; registrado como limitação).

## 3. Dependências
- **Implementações necessárias:** SPEC-0041 — `Leave`/estado de saída, `EndReason.Abandon`, mudanças em `Home`, `Scoreboard` e `GameSession` em sequência (evita conflito).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** Blazor Server (`CircuitHandler`, `Microsoft.AspNetCore.Components.Server.Circuits`).

## 4. Decisão Arquitetural
**Contexto:** `GameSession` com `Tick()` a cada segundo (SPEC-0027), timeout do turno, `EndReason.Disconnect` reservado (SPEC-0036); um circuito por navegador; `Home` como orquestrador (SPEC-0024).

**Decisão:** a tolerância e o encerramento ficam no domínio (`GameSession`), reaproveitando o `Tick` e o relógio injetável; a infraestrutura de circuito (adaptador fino) só traduz eventos do Blazor em `SetConnection`.

**Justificativa:** mantém a regra testável sem circuitos reais e evita timers novos.

**Desvio do padrão existente:** introduz a pasta `Services/Presence` no projeto Web e um `CircuitHandler` registrado no DI (novo mecanismo de infraestrutura do Blazor Server, sem biblioteca nova).

**Alternativas descartadas:** timer por circuito no handler (duplica relógio e dificulta testes); depender apenas de `Dispose` do componente (o Blazor retém o circuito desconectado por minutos antes de descartá-lo); heartbeat próprio por SignalR (infraestrutura extra).

**ADRs:** ADR-0008 (primitivos de UI).

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** custo O(1) por sessão no `Tick`; nenhum timer adicional.
- **Segurança:** só o servidor decide o encerramento; o cliente não envia "estou desconectado"; o handler afeta apenas a partida do próprio circuito.
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** tolerância de **15 s** para quedas breves (rede móvel, aba em segundo plano); reconexão dentro do prazo retoma sem perda; `SetConnection` e `Forfeit` idempotentes e sob o `lock`; partida já encerrada ignora eventos de presença.
- **Acessibilidade (UI):** aviso de desconexão em região `aria-live="polite"` com a contagem regressiva sem ruído excessivo (anuncia ao iniciar e ao terminar, não a cada segundo); contraste AA; Lighthouse ≥ 90.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `GameSession`, `MatchPresenceCircuitHandler`, contexto de presença e aviso na arena.

```text
GameSession
  const int DisconnectGraceSeconds = 15
  void SetConnection(Player player, bool connected)      // false inicia a contagem; true a cancela; ignorado em solo e em partida encerrada
  int? DisconnectSecondsLeft(Player player)               // nulo se conectado
  bool Forfeit(Player loser, EndReason reason)            // vitória do outro lado; em série, encerra a série; falso se já encerrada
  // Tick(): se um jogador ficou desconectado por ≥ DisconnectGraceSeconds → Forfeit(jogador, Disconnect)
  // Leave(...) (SPEC-0041) chama Forfeit(jogador, Abandon)

MatchPresenceContext (Scoped)    Attach(GameSession, Player)  Detach()  GameSession? Session  Player Player
MatchPresenceCircuitHandler : CircuitHandler
  OnConnectionDownAsync  → Session?.SetConnection(Player, false)
  OnConnectionUpAsync    → Session?.SetConnection(Player, true)
  OnCircuitClosedAsync   → Session?.SetConnection(Player, false); Detach()

Home     ao entrar em partida humana: contexto.Attach(game, MyPlayer) · ao sair/descartar: contexto.Detach()
Arena    "Oponente desconectado. Aguardando reconexão… {n}s"  ·  ao esgotar: "Oponente desconectou. Vitória por W.O."
```

**Arquivos/módulos afetados:** ver `touches`. Registro em `Program.cs`: `AddScoped<MatchPresenceContext>()` e `AddScoped<CircuitHandler, MatchPresenceCircuitHandler>()`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Queda e retorno | Desconecta e volta em 5 s | Contagem cancelada; partida continua | UT-01 |
| Tolerância esgotada | Desconectado por 15 s | Outro lado vence; `EndReason=Disconnect`; em série, encerra a série | UT-01 |
| Casos ignorados | Solo, partida encerrada | Eventos de presença sem efeito | UT-02 |
| Dois desconectados | Ambos caem | Vale quem estourar primeiro; sem segundo encerramento | UT-02 |
| Desistência única | `Forfeit` e `Leave` | Mesmo caminho; motivos Abandon/Disconnect; idempotente | UT-03 |
| Adaptador de circuito | Queda, retorno, fechamento | Chamadas corretas a `SetConnection`; sem partida = sem efeito | UT-04 |
| Aviso na arena | Oponente desconectado / esgotado | Textos do contrato com contagem regressiva | UT-05 |
| Registro | DI da aplicação | Handler registrado como `CircuitHandler` com escopo | IT-02 |
| Duas telas | Um circuito cai, o outro observa | Aviso, depois vitória por W.O., resultado gravado uma vez com `Disconnect` | IT-01 |
| Reconexão | Volta antes de 15 s | Aviso some; partida segue | IT-01 |
| Jornada | Queda e W.O. | Fluxo visível ao jogador que ficou | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado `Tick()` sem eventos de presença, então o timer de turno e o W.O. por tempo (SPEC-0027) seguem idênticos (guarda: passa antes da mudança).

### 7.2 Testes Unitários
- **UT-01** — Dado uma partida online em andamento, quando o jogador O desconecta e reconecta em 5 s, então a partida segue; quando desconecta e passam 15 s de `Tick`, então X vence com `EndReason=Disconnect` (e, numa série, `IsSeriesOver` com X vencedor).
- **UT-02** — Dado uma partida solo e uma partida encerrada, então `SetConnection(false)` não tem efeito; dado dois jogadores desconectados em momentos diferentes, então o primeiro a estourar perde e o segundo não gera novo encerramento.
- **UT-03** — Dado `Forfeit(Player, EndReason)`, então concede a vitória ao outro lado com o motivo dado, é idempotente e retorna falso em partida encerrada; `Leave` produz o mesmo resultado com `Abandon`.
- **UT-04** — Dado `MatchPresenceCircuitHandler` com um `MatchPresenceContext` anexado e depois desanexado, então `OnConnectionDown/Up/CircuitClosed` chamam `SetConnection` corretamente e não fazem nada sem partida anexada.
- **UT-05** — Dado uma partida com o oponente desconectado e depois encerrada por desconexão, então o aviso mostra "Oponente desconectado. Aguardando reconexão… {n}s" e, no fim, "Oponente desconectou. Vitória por W.O.", em região `aria-live="polite"`.

### 7.3 Testes de Integração
- **IT-01** — Dado dois `Home` na mesma partida online, quando o circuito de um "cai" e o tempo avança 15 s, então o outro vê o aviso e depois a vitória por W.O., e **uma única** linha é gravada com `EndReason=Disconnect`; se o circuito volta em 5 s, o aviso some e a partida continua.
- **IT-02** — Dado o contêiner de DI da aplicação, então `MatchPresenceContext` é `Scoped` e existe um `CircuitHandler` do tipo `MatchPresenceCircuitHandler`; `Home.Dispose` desanexa o contexto.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
- **E2E-01** — Jornada (bUnit, dois jogadores): partida em andamento, um jogador é desconectado, o outro vê a contagem e ganha por W.O.; o histórico exibe "Desconexão do oponente".

### 7.6 Outros
- Verificação manual em navegador real (staging/local): fechar a aba do oponente e recarregar a página (F5) durante a partida; confirmar W.O. em ~15 s. Registrar no PR.
- Revisão visual (H2): aviso em 390px e 1280px.

**Dublês e dados de teste:** `TimeProvider` de teste, `GameSession(enableBackgroundTimer: false)` com `Tick()` manual, dois `Home` no mesmo dicionário, EF InMemory; handler chamado diretamente (sem circuito real).

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto.
- **Dados/schema:** N/A — `EndReason.Disconnect` já existe.
- **Compatibilidade:** partidas solo e partidas sem quedas não mudam.
- **Observabilidade:** log de informação em queda, retorno e W.O. por desconexão (id da partida, lado, segundos).
- **Rollback:** `git revert`; sem dados a desfazer.
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
- [x] **Tolerância de reconexão:** 15 s (recomendado: cobre quedas breves de rede móvel sem prender o jogador que ficou; igual ao timer de turno) ou outro valor? — 15 s. (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [x] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [x] Escrever os testes `SPEC-0042:CH-01`, `SPEC-0042:UT-01`, `SPEC-0042:UT-02`, `SPEC-0042:UT-03`, `SPEC-0042:UT-04`, `SPEC-0042:UT-05`, `SPEC-0042:IT-01`, `SPEC-0042:IT-02`, `SPEC-0042:E2E-01` com a tag `SPEC-0042:<ID>`, em commits `test(...)` com `Refs: SPEC-0042`, tocando só `test_paths`
- [x] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [x] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0042`)

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [x] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [x] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [x] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [x] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável — **não executada** (app não aberto no navegador); aceita pela autorização de merge, ver Pendências
- [x] PR com `spec_graph.py pr SPEC-0042`, CI verde (G5) e aprovação do merge (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
| G1 Red | PASS | verify SPEC-0042: Red antes do Green, testes do plano rastreados | 2026-09-29 |
| G2 Green | PASS | dotnet test 322/322 local; format e tailwind --check limpos; verify PASS | 2026-09-29 |
| G3 Arquitetura | N/A | sem suíte Category=Architecture | 2026-09-29 |
| G4 Review | PASS | Review independente (subagente): FAIL condicional a (1) região aria-live anunciando a cada segundo, (2) contagem obsoleta com rodada encerrada, (3) falta de logs de presença; todos corrigidos com testes; menor 4 (leitura atômica) corrigido. Restam como dívida: service locator opcional em Home (IServiceProvider), sessão vaza quando os dois circuitos somem, IT-02 valida o registro por leitura do Program.cs, timers reais nos testes de Home, queda durante a tela de resultado numa série não é rastreada, navegar para outra página conta como queda (decisão de produto a confirmar) | 2026-09-29 |
| G5 Integração & CI | PASS | PR #30: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorização permanente do usuário (2026-09-29): mesclar com CI verde conforme a skill sdd-management | 2026-09-29 |
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

W.O. por desconexão: a sessão acompanha a conexão de cada jogador humano (tolerância de 15 s, cancelada na reconexão), encerra a rodada com vitória do outro lado por Disconnect (em série, encerra a série) e mostra 'Oponente desconectado. Aguardando reconexão…' com contagem e 'Oponente desconectou. Vitória por W.O.'. Forfeit é o caminho único de desistência (Leave usa Abandon).

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

GameSession (SetConnection, DisconnectSecondsLeft, Forfeit/ForfeitCore, expiração no Tick com TimeProvider), MatchPresenceContext e MatchPresenceCircuitHandler registrados em Program.cs, Home associa/desassocia a partida e trata sair da tela como queda, Scoreboard com região aria-live estática e contagem à parte, logs de presença.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0042:CH-01 | Dado `Tick()` sem eventos de presença, então o timer de turno e o W.O. por tempo (SPEC-0027) seguem idênticos  | PASS | `dotnet test` 322/322 no CI (dotnet-ci) do PR #30 |
| SPEC-0042:UT-01 | Dado uma partida online em andamento, quando o jogador O desconecta e reconecta em 5 s, então a partida segue; | PASS | `dotnet test` 322/322 no CI (dotnet-ci) do PR #30 |
| SPEC-0042:UT-02 | Dado uma partida solo e uma partida encerrada, então `SetConnection(false)` não tem efeito; dado dois jogadore | PASS | `dotnet test` 322/322 no CI (dotnet-ci) do PR #30 |
| SPEC-0042:UT-03 | Dado `Forfeit(Player, EndReason)`, então concede a vitória ao outro lado com o motivo dado, é idempotente e re | PASS | `dotnet test` 322/322 no CI (dotnet-ci) do PR #30 |
| SPEC-0042:UT-04 | Dado `MatchPresenceCircuitHandler` com um `MatchPresenceContext` anexado e depois desanexado, então `OnConnect | PASS | `dotnet test` 322/322 no CI (dotnet-ci) do PR #30 |
| SPEC-0042:UT-05 | Dado uma partida com o oponente desconectado e depois encerrada por desconexão, então o aviso mostra "Oponente | PASS | `dotnet test` 322/322 no CI (dotnet-ci) do PR #30 |
| SPEC-0042:IT-01 | Dado dois `Home` na mesma partida online, quando o circuito de um "cai" e o tempo avança 15 s, então o outro v | PASS | `dotnet test` 322/322 no CI (dotnet-ci) do PR #30 |
| SPEC-0042:IT-02 | Dado o contêiner de DI da aplicação, então `MatchPresenceContext` é `Scoped` e existe um `CircuitHandler` do t | PASS | `dotnet test` 322/322 no CI (dotnet-ci) do PR #30 |
| SPEC-0042:E2E-01 | Jornada (bUnit, dois jogadores): partida em andamento, um jogador é desconectado, o outro vê a contagem e ganh | PASS | `dotnet test` 322/322 no CI (dotnet-ci) do PR #30 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #30 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Verificação manual em navegador (fechar aba e F5) não feita; registrar quando houver ambiente. Home usa IServiceProvider opcional para presença e relógio; sessão vaza em Games se os dois circuitos somem; IT-02 valida o registro lendo Program.cs; queda durante a tela de resultado numa série não é rastreada; navegar para outra página conta como queda (confirmar com o produto).

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0042`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
