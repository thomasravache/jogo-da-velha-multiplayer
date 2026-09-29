---
id: SPEC-0061
title: "Ciclo de vida da sessão de xadrez: abandono, revanche e presença"
tier: full
type: feature
user_facing: false
status: implemented
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0052]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Chess/Session/ChessSession.Lifecycle.cs, src/TicTacToe/TicTacToe.Modules.Chess/Session/ChessLifecycleTypes.cs, tests/TicTacToe.Tests/ChessSessionLeaveTests.cs, tests/TicTacToe.Tests/ChessSessionRematchTests.cs, tests/TicTacToe.Tests/ChessSessionPresenceTests.cs]
adrs: []
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0061 — Ciclo de vida da sessão de xadrez: abandono, revanche e presença

## 1. Visão Geral
Completa a `ChessSession` com o **ciclo de vida da partida**: desistência única (`Forfeit`), abandono (`Leave`), revanche com aceite que **troca as cores dos assentos**, presença com W.O. por desconexão e a expiração dos pedidos no `Tick`. É o equivalente, no xadrez, do que o jogo da velha ganhou nas SPEC-0041 e SPEC-0042.

## 2. Motivação & Escopo
**Motivação:** A interface de abandono, revanche e desconexão (SPEC-0060) precisa de comandos atômicos e testados no domínio, e o resultado gravado (SPEC-0053) precisa dos motivos Abandon e Disconnect.

**Objetivos (dentro do escopo):**
- `Forfeit(loser, reason)` como único caminho de desistência (Resignation, Abandon, Disconnect); `Leave(player)` devolve `Forfeited`, `Discarded` (solo), `Left` ou `Rejected`; `HasLeft`.
- Revanche: `RequestRematch`/`AcceptRematch`/`DeclineRematch`, pedidos simultâneos aceitam automaticamente, expira em 30 s (`Expired`); ao reiniciar **as cores dos assentos trocam** e o relógio é novo; solo reinicia na hora.
- Presença: `SetConnection` e `DisconnectSecondsLeft`; passados 15 s de desconexão o `Tick` encerra por `Disconnect`; ignorada em solo e com a partida encerrada.
- Tipos `ChessLeaveResult` e `ChessRematchState`; `Tick` chama a expiração de pedido e a de desconexão (`TickLifecycle`).

**Não-objetivos (fora do escopo):**
- Interface, presença por circuito (SPEC-0060) e pareamento/persistência (SPEC-0053).
- Proposta de empate.
- Abortar partida parada antes do primeiro lance.

## 3. Dependências
- **Implementações necessárias:** SPEC-0052 (núcleo da sessão, assentos, snapshot e o gancho `TickLifecycle`).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `GameSession.Leave/RequestRematch/SetConnection` (SPEC-0041/0042): atomicidade sob lock, eventos fora do lock, expiração no `Tick`, `Forfeit` único. Referência: `GameSession.cs`, `LeaveAndRematchTests`, `DisconnectForfeitTests`.

**Decisão:** Arquivo parcial `ChessSession.Lifecycle.cs` com a mesma máquina de estados do jogo da velha, sobre assentos (a presença e a saída são por assento, para sobreviver à troca de cores).

**Justificativa:** Reaproveita o desenho já validado e mantém o núcleo pequeno; guardar presença por assento evita erro quando as cores trocam na revanche.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Tudo em um arquivo (spec L); presença por cor (quebra na troca de cores).

**ADRs:** N/A

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Comandos em milissegundos; `Tick` O(1).
- **Segurança:** Todo comando valida quem age e o estado; inválidos retornam falso sem alterar nada.
- **Privacidade e dados pessoais:** N/A — sem dados novos.
- **Disponibilidade e resiliência:** Comandos concorrentes (dois aceites, abandono e lance simultâneos) produzem um único efeito; pedido expira; eventos fora do lock.
- **Acessibilidade (UI):** N/A — sem interface.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `TicTacToe.Modules.Chess: ChessSession (Forfeit, Leave, revanche, presença), ChessLeaveResult, ChessRematchState`

```text
enum ChessLeaveResult { Forfeited, Discarded, Left, Rejected }
enum ChessRematchState { None, Requested, Declined, Expired }

partial class ChessSession
  const int DisconnectGraceSeconds = 15
  bool Forfeit(PieceColor loser, ChessEndReason reason)          // Resignation|Abandon|Disconnect|Timeout; outro lado vence; falso se encerrada; marca o perdedor como ausente
  ChessLeaveResult Leave(PieceColor player)                      // em andamento: Forfeited (Abandon) ou Discarded (solo: sem resultado gravável); encerrada: Left; repetida: Rejected
  bool HasLeft(PieceColor color)                                 // por assento (sobrevive à troca de cores)
  bool RequestRematch(PieceColor player);  bool AcceptRematch(PieceColor player);  bool DeclineRematch(PieceColor player)
       // solo: reinicia na hora; humanos: pedido/aceite/recusa; pedidos simultâneos = aceite automático; expira em 30 s;
       // ao reiniciar: RestartCore(swapColors: true) — nova partida de start ?? Position.Start, cores dos assentos TROCADAS, relógio novo, presença e pedido limpos
       // pedido com oponente ausente é recusado (falso)
  ChessRematchState RematchState { get; }  PieceColor? RematchRequestedBy { get; }   // a cor de quem pediu no momento do pedido
  void SetConnection(PieceColor player, bool connected)          // ignorado em solo, partida encerrada e ausente; contagem por assento
  int? DisconnectSecondsLeft(PieceColor player)                  // 15 → 0; nulo se conectado ou partida encerrada
  partial void TickLifecycle()                                   // expira pedido (30 s) e derruba por Disconnect (15 s); o primeiro a estourar perde
Regras: abandono e desconexão só valem em partida humana; após a revanche `TryMarkResultRecorded` volta a valer para a nova partida.
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Desistência | `Forfeit`/`Leave` em andamento, solo, encerrada, repetida | Abandon, Discarded, Left, Rejected; idempotente; perdedor marcado ausente | UT-01 |
| Presença | Queda e retorno; 15 s; dois caem; solo e encerrada | Disconnect só após a tolerância; regras de exceção; contagem regressiva | UT-02 |
| Revanche | Pedido, aceite, recusa, simultâneo, expiração, oponente ausente | Estados e efeitos do contrato; cores dos assentos trocadas ao reiniciar | UT-03 |
| Troca de cores e presença | Revanche seguida de abandono ou queda do assento que agora joga de outra cor | Presença e saída seguem o assento, não a cor antiga | UT-04 |
| Concorrência | Dois aceites e lance/abandono simultâneos | Um único efeito | UT-05 |
| Solo | Revanche e abandono em solo | Reinício imediato (cores trocadas); abandono descarta sem resultado gravável | UT-06 |
| Gravação única | Resultado gravado e nova partida após revanche | `TryMarkResultRecorded` volta a valer | UT-07 |
| Eventos | Handler que reentra na sessão | Evento fora do lock em `Leave`, revanche e expiração | UT-08 |
| Fluxo de partida | Partida termina por abandono; outra por queda; revanche aceita | Resultados, motivos e cores trocadas de ponta a ponta | IT-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — código novo.

### 7.2 Testes Unitários
- **UT-01** — Dado `Forfeit(loser, reason)` e `Leave` em partida em andamento, solo em andamento, encerrada e repetida, então: o outro vence com o motivo dado (Abandon para `Leave`), o solo devolve `Discarded` sem resultado gravável, a encerrada devolve `Left` e a repetida `Rejected`; `Forfeit` em partida encerrada retorna falso; quem desiste fica `HasLeft`.
- **UT-02** — Dado `SetConnection(false)` por 5 s e retorno, então a partida segue; por 15 s de `Tick`, o outro vence por `Disconnect`; com dois desconectados vale quem estoura primeiro; em solo, com a partida encerrada ou com o jogador ausente o evento é ignorado; `DisconnectSecondsLeft` conta de 15 a 0 e é nulo com a partida encerrada.
- **UT-03** — Dado uma partida encerrada, então: pedido → `Requested`; aceite pelo outro reinicia com **as cores trocadas** e relógio novo; recusa → `Declined`; pedidos dos dois lados aceitam automaticamente; 30 s sem resposta → `Expired` (novo pedido permitido); pedido com o oponente ausente é recusado.
- **UT-04** — Dado uma revanche aceita (cores trocadas), então `SetConnection`, `Leave` e `HasLeft` chamados pela cor **nova** agem sobre o assento correto, e o assento que jogava de brancas agora joga de pretas em `ColorOf`.
- **UT-05** — Dado dois `AcceptRematch` em paralelo e um lance concorrente com `Leave`, então reinicia uma única vez e o abandono e o lance não produzem dois resultados (corrida repetida 50 vezes).
- **UT-06** — Dado uma sessão solo, então `RequestRematch` reinicia na hora (sem aceite, cores trocadas) e `Leave` em andamento descarta a partida.
- **UT-07** — Dado `TryMarkResultRecorded` chamado após a partida e de novo depois de uma revanche, então é verdadeiro em cada partida nova e falso na repetição.
- **UT-08** — Dado um handler de `OnStateChanged` que consulta a sessão de outra thread, então a consulta não fica bloqueada em `Leave`, `AcceptRematch` e `Tick` que expira um pedido.

### 7.3 Testes de Integração
- **IT-01** — Dado uma sessão real com `ManualTime`, quando uma partida termina por abandono, outra por desconexão e uma revanche é aceita, então os resultados, os motivos (`Abandon`, `Disconnect`), as cores trocadas e os relógios novos são os esperados.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
N/A — `user_facing: false`.

### 7.6 Outros
- N/A

**Dublês e dados de teste:** `ManualTime` (existente); sessões reais; sem mocks.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; sem consumidores até as specs de interface.
- **Dados/schema:** N/A
- **Compatibilidade:** N/A — código novo.
- **Observabilidade:** N/A — biblioteca; logs na camada Web.
- **Rollback:** Reverter o PR.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Revanche troca as cores? — Sim, como no xadrez de torneio; os assentos guardam a cor corrente (Architect, 2026-09-29)
- - [x] Empate por acordo? — Fora do escopo (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [x] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [x] Escrever `SPEC-0061:IT-01`, `SPEC-0061:UT-01`, `SPEC-0061:UT-02`, `SPEC-0061:UT-03`, `SPEC-0061:UT-04`, `SPEC-0061:UT-05`, `SPEC-0061:UT-06`, `SPEC-0061:UT-07`, `SPEC-0061:UT-08` com a tag `SPEC-0061:<ID>` em commits `test(...)` com `Refs: SPEC-0061` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [x] Refactor mantendo tudo verde
- [x] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes` e `verify SPEC-0061 --base origin/main`

**Fase final: Integração, entrega e documentação**
- [x] Review independente (G4)
- [x] Integração + CI verde (G5) e aprovação (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
| G1 Red | PASS | verify SPEC-0061: Red antes do Green (3a93641), 9/9 testes do plano rastreados; 44 falharam por NotImplementedException | 2026-09-29 |
| G2 Green | PASS | dotnet test 720 verdes (1 pulado: perft pesado); 47 testes da spec em 5 execuções sem falha; format limpo; verify PASS | 2026-09-29 |
| G3 Arquitetura | N/A | fronteira de módulos coberta por ChessModuleBoundaryTests (SPEC-0049) | 2026-09-29 |
| G4 Review | PASS | Review independente (subagente): PASS com ressalvas; maior (_restarting liberado tarde: abandono e queda perdidos no handler do restart) corrigido com 3 testes; revanche solo passou a exigir partida encerrada. Dívidas: Forfeit(Resignation) marca o perdedor ausente (a UI usa Leave/Abandon), Forfeit(Timeout) público ignora material de mate, RestartCore zera a marca de gravação (o gravador precisa registrar antes da revanche), Snapshot não expõe presença/revanche | 2026-09-29 |
| G5 Integração & CI | PASS | PR #43: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
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

Ciclo de vida da sessão de xadrez: desistência única (Forfeit), abandono (Leave: Forfeited, Discarded em solo, Left, Rejected), revanche com aceite que troca as cores dos assentos, pedidos simultâneos com aceite automático e expiração em 30 s, presença com W.O. por desconexão após 15 s (primeiro a estourar perde) e solo com reinício imediato.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

Arquivo parcial ChessSession.Lifecycle.cs com estado por assento (saída e queda sobrevivem à troca de cores), eventos sempre fora do lock, flag de reinício liberado antes do evento do restart (bug achado na review e corrigido com teste), expiração e desconexão no TickLifecycle.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0061:UT-01 | Dado `Forfeit(loser, reason)` e `Leave` em partida em andamento, solo em andamento, encerrada e repetida, entã | PASS | `dotnet test` 720/720 no CI (dotnet-ci) do PR #43 |
| SPEC-0061:UT-02 | Dado `SetConnection(false)` por 5 s e retorno, então a partida segue; por 15 s de `Tick`, o outro vence por `D | PASS | `dotnet test` 720/720 no CI (dotnet-ci) do PR #43 |
| SPEC-0061:UT-03 | Dado uma partida encerrada, então: pedido → `Requested`; aceite pelo outro reinicia com **as cores trocadas**  | PASS | `dotnet test` 720/720 no CI (dotnet-ci) do PR #43 |
| SPEC-0061:UT-04 | Dado uma revanche aceita (cores trocadas), então `SetConnection`, `Leave` e `HasLeft` chamados pela cor **nova | PASS | `dotnet test` 720/720 no CI (dotnet-ci) do PR #43 |
| SPEC-0061:UT-05 | Dado dois `AcceptRematch` em paralelo e um lance concorrente com `Leave`, então reinicia uma única vez e o aba | PASS | `dotnet test` 720/720 no CI (dotnet-ci) do PR #43 |
| SPEC-0061:UT-06 | Dado uma sessão solo, então `RequestRematch` reinicia na hora (sem aceite, cores trocadas) e `Leave` em andame | PASS | `dotnet test` 720/720 no CI (dotnet-ci) do PR #43 |
| SPEC-0061:UT-07 | Dado `TryMarkResultRecorded` chamado após a partida e de novo depois de uma revanche, então é verdadeiro em ca | PASS | `dotnet test` 720/720 no CI (dotnet-ci) do PR #43 |
| SPEC-0061:UT-08 | Dado um handler de `OnStateChanged` que consulta a sessão de outra thread, então a consulta não fica bloqueada | PASS | `dotnet test` 720/720 no CI (dotnet-ci) do PR #43 |
| SPEC-0061:IT-01 | Dado uma sessão real com `ManualTime`, quando uma partida termina por abandono, outra por desconexão e uma rev | PASS | `dotnet test` 720/720 no CI (dotnet-ci) do PR #43 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #43 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Forfeit(Resignation) marca o perdedor como ausente (a interface usa Leave/Abandon); Forfeit(Timeout) público ignora material de mate; RestartCore zera a marca de gravação, então o gravador precisa registrar a partida antes da revanche (SPEC-0053/0056); Snapshot não expõe presença nem revanche (a interface lê HasLeft, RematchState e DisconnectSecondsLeft).

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0061`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
