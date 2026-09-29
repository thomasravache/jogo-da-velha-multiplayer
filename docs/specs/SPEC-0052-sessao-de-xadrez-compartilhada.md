---
id: SPEC-0052
title: Sessão de xadrez compartilhada
tier: full
type: feature
user_facing: false
status: proposed
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0050, SPEC-0051]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Chess/Session/**, tests/TicTacToe.Tests/ChessSessionTests.cs, tests/TicTacToe.Tests/ChessSessionLeaveTests.cs, tests/TicTacToe.Tests/ChessSessionRematchTests.cs, tests/TicTacToe.Tests/ChessSessionPresenceTests.cs]
adrs: []
external: []
size: M
approved_by:
approved_at:
---

# SPEC-0052 — Sessão de xadrez compartilhada

## 1. Visão Geral
Cria a **sessão de xadrez** compartilhada pelos dois circuitos: jogadores e cores, lances validados por vez, relógio, resultado, desistência (abandono), revanche com aceite (trocando de cor), presença com W.O. por desconexão e evento de mudança de estado. É o equivalente do `GameSession` do jogo da velha para o xadrez.

## 2. Motivação & Escopo
**Motivação:** A partida em rede precisa de um objeto único, atômico e observável por dois circuitos. No jogo da velha isso foi construído em várias specs (0027, 0041, 0042); aqui o domínio nasce completo e a interface só o apresenta.

**Objetivos (dentro do escopo):**
- `ChessSession` com `TryMove` (valida quem joga, vez, fim e promoção) integrando `ChessGame` e `ChessClock`.
- Resultado por regras (via `ChessGame`), por tempo (bandeira), por abandono (`Leave`) e por desconexão (`SetConnection` + `Tick`).
- Revanche com aceite: `RequestRematch`/`AcceptRematch`/`DeclineRematch`, expiração em 30 s, **troca de cores** ao reiniciar; partida solo reinicia na hora.
- `Forfeit` como único caminho de desistência; `Leave` descarta em solo; `HasLeft`.
- Identidade (`SetPlayer` com nome e `PlayerId`), modo (`Online`, `Private`, `Solo`), `TryMarkResultRecorded`, instantes e duração.
- Evento `OnStateChanged` sempre disparado fora do lock; `Tick()` dirige bandeira, desconexão e expiração da revanche.

**Não-objetivos (fora do escopo):**
- Pareamento, cores por preferência e persistência (SPEC-0053).
- Robô (SPEC-0054/0058) e qualquer interface (SPEC-0055 a 0060).
- Proposta de empate e série de partidas.

## 3. Dependências
- **Implementações necessárias:** SPEC-0050 (`ChessGame`, resultado, SAN) e SPEC-0051 (`ChessClock`, `TimeControl`).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `GameSession` (SPEC-0027, 0040, 0041, 0042): estado compartilhado sob `lock`, `OnStateChanged` fora do lock, `TimeProvider` injetável, `Tick` por timer de 1 s, `Leave`/rematch/presença. Referência: `GameSession.cs`, `LeaveAndRematchTests`, `DisconnectForfeitTests`.

**Decisão:** `ChessSession` no módulo Chess repetindo o padrão de `GameSession`, com tipos próprios (`ChessLeaveResult`, `ChessRematchState`, `ChessMode`) para não colidir com os de Gameplay nas páginas que importam os dois módulos.

**Justificativa:** Mantém os módulos independentes e reaproveita o desenho já validado (atomicidade, eventos fora do lock, expiração no Tick).

**Desvio do padrão existente:** Nenhum (padrão existente aplicado a outro jogo).

**Alternativas descartadas:** Generalizar `GameSession` para os dois jogos (refactor grande e arriscado no jogo da velha já entregue); compartilhar enums entre módulos (violaria a fronteira).

**ADRs:** N/A

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Comandos em milissegundos; um evento de estado por comando; `Tick` O(1).
- **Segurança:** Todo comando valida no servidor quem age e o estado (turno, fim, presença); comandos inválidos retornam falso e não alteram nada; o `PlayerId` não é exposto.
- **Privacidade e dados pessoais:** N/A — nomes já visíveis; `PlayerId` só para gravação.
- **Disponibilidade e resiliência:** Comandos concorrentes (dois aceites, abandono e lance simultâneos) produzem um único efeito; o pedido de revanche expira; eventos fora do lock evitam deadlock.
- **Acessibilidade (UI):** N/A — sem interface.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `TicTacToe.Modules.Chess: ChessSession, ChessMode, ChessLeaveResult, ChessRematchState`

```text
enum ChessMode { Online, Private, Solo }
enum ChessLeaveResult { Forfeited, Discarded, Left, Rejected }
enum ChessRematchState { None, Requested, Declined, Expired }

sealed class ChessSession : IDisposable
  const int DisconnectGraceSeconds = 15;   // como no jogo da velha
  ChessSession(TimeControl control, TimeProvider? timeProvider = null, bool enableBackgroundTimer = true, Position? start = null)
  Guid Id;  ChessGame Game;  ChessClock Clock;  TimeControl Control;  ChessMode Mode { get; set; }
  void SetPlayer(PieceColor color, string name, Guid? playerId);  string GetPlayerName(PieceColor);  Guid? GetPlayerId(PieceColor)
  bool TryMove(PieceColor player, Square from, Square to, PieceType? promotion, out ChessMove? played)
       // falso: fim, fora da vez, ilegal, promoção ausente; ao jogar: Clock.Press; fim por regras encerra e para o relógio
  ChessResult? Result { get; }   bool IsOver { get; }
  DateTimeOffset StartedAtUtc;  DateTimeOffset? EndedAtUtc;  TimeSpan? Duration
  bool Forfeit(PieceColor loser, ChessEndReason reason)          // Resignation|Abandon|Disconnect|Timeout; falso se encerrada
  ChessLeaveResult Leave(PieceColor player)                      // em andamento: Forfeited (Abandon) ou Discarded (solo); encerrada: Left; repetida: Rejected
  bool HasLeft(PieceColor color)
  bool RequestRematch(PieceColor player);  bool AcceptRematch(PieceColor player);  bool DeclineRematch(PieceColor player)
       // solo: reinicia na hora; humanos: pedido/aceite/recusa, pedidos simultâneos = aceite automático, expira em 30 s;
       // ao reiniciar: nova partida, cores TROCADAS, relógio novo, presença e pedido limpos
  ChessRematchState RematchState { get; };  PieceColor? RematchRequestedBy { get; }
  void SetConnection(PieceColor player, bool connected);  int? DisconnectSecondsLeft(PieceColor player)
  void Tick()                                                    // bandeira (Timeout), desconexão (Disconnect) e expiração de revanche
  event Action? OnStateChanged                                   // nunca dentro do lock
  bool TryMarkResultRecorded()                                   // verdadeiro só no primeiro chamador por partida
Regras: derrota por tempo = vitória do outro por Timeout (sem verificar material do vencedor);
        abandono e desconexão só valem em partida humana; presença é ignorada em solo e com a partida encerrada.
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Jogar lance | Lance legal na vez; fora da vez; ilegal; após o fim; promoção | Aceito só o legal na vez; relógio passa ao outro; demais retornam falso | UT-01 |
| Fim por regras | Mate, afogamento, repetição na sessão | Resultado registrado, relógio parado, `EndedAtUtc` definido | UT-02 |
| Fim por tempo | Bandeira cai com o relógio simulado | Vitória do outro por `Timeout` | UT-03 |
| Desistência | `Forfeit`/`Leave` em andamento, solo, encerrada, repetida | Abandon, Discarded, Left, Rejected; idempotente | UT-04 |
| Presença | Queda e retorno; 15 s; dois caem; solo e encerrada | Disconnect só após a tolerância; regras de exceção | UT-05 |
| Revanche | Pedido, aceite, recusa, simultâneo, expiração, oponente ausente | Estados e efeitos do contrato; cores trocadas ao reiniciar | UT-06 |
| Concorrência | Dois aceites e lance/abandono simultâneos | Um único efeito | UT-07 |
| Solo | Revanche e abandono em solo | Reinício imediato; abandono descarta sem resultado gravável | UT-08 |
| Gravação única | `TryMarkResultRecorded` repetido | Verdadeiro uma só vez por partida; volta a valer após revanche | UT-09 |
| Eventos | Handler que reentra na sessão | Evento disparado fora do lock | UT-10 |
| Partida completa | Mate do pastor com relógio; W.O. por tempo; revanche | Fluxos ponta a ponta no domínio | IT-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — código novo.

### 7.2 Testes Unitários
- **UT-01** — Dado uma sessão nova, quando brancas jogam `e2→e4`, então o lance entra no histórico e o relógio passa às pretas; jogar fora da vez, um lance ilegal, depois do fim ou promoção sem peça retorna falso sem mudar o estado.
- **UT-02** — Dado o mate do pastor e uma posição de afogamento, então `Result` traz o resultado e o motivo por regras, `IsOver` é verdadeiro, o relógio para e `EndedAtUtc`/`Duration` ficam definidos.
- **UT-03** — Dado o relógio simulado avançando além do tempo de quem joga e `Tick`, então o outro vence por `Timeout` e novos lances são recusados.
- **UT-04** — Dado `Forfeit(loser, reason)` e `Leave` em partida em andamento, solo em andamento, encerrada e repetida, então: o outro vence com o motivo dado (Abandon para `Leave`), o solo devolve `Discarded` sem resultado gravável, a encerrada devolve `Left` e a repetida `Rejected`; `Forfeit` em partida encerrada retorna falso.
- **UT-05** — Dado `SetConnection(false)` por 5 s e retorno, então a partida segue; por 15 s de `Tick`, o outro vence por `Disconnect`; com dois desconectados vale quem estoura primeiro; em solo e com a partida encerrada o evento é ignorado; `DisconnectSecondsLeft` conta de 15 a 0 e é nulo com a partida encerrada.
- **UT-06** — Dado uma partida encerrada, então: pedido → `Requested`; aceite pelo outro reinicia com **as cores trocadas** e relógio novo; recusa → `Declined`; pedidos dos dois lados aceitam automaticamente; 30 s sem resposta → `Expired` (novo pedido permitido); pedido com o oponente ausente é recusado.
- **UT-07** — Dado dois `AcceptRematch` em paralelo e um lance concorrente com `Leave`, então reinicia uma única vez e o abandono e o lance não produzem dois resultados.
- **UT-08** — Dado uma sessão solo, então `RequestRematch` reinicia na hora (sem aceite, cores trocadas) e `Leave` em andamento descarta a partida.
- **UT-09** — Dado `TryMarkResultRecorded` chamado duas vezes na mesma partida encerrada, então retorna verdadeiro e depois falso; após revanche volta a valer para a nova partida.
- **UT-10** — Dado um handler de `OnStateChanged` que consulta a sessão a partir de outra thread, então a consulta não fica bloqueada (evento fora do lock) em lance, `Leave`, `Tick` que expira revanche e `SetConnection`.

### 7.3 Testes de Integração
- **IT-01** — Dado uma sessão real com `ManualTime`, quando se joga o mate do pastor com tempo correndo, depois outra partida termina por W.O. de tempo e uma revanche é aceita, então resultados, motivos, cores trocadas e relógios são os esperados de ponta a ponta.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
N/A — `user_facing: false`.

### 7.6 Outros
- Corrida entre `AcceptRematch`, `Leave` e `TryMove` repetida 200 vezes sem estado inconsistente (parte do UT-07).

**Dublês e dados de teste:** `ManualTime` (existente), `ChessGame` e `ChessClock` reais; sem mocks.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; sem consumidores até as specs de pareamento e interface.
- **Dados/schema:** N/A
- **Compatibilidade:** N/A — código novo.
- **Observabilidade:** N/A — biblioteca; logs na camada Web (specs seguintes).
- **Rollback:** Reverter o PR.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Revanche troca as cores? — Sim, como no xadrez de torneio (Architect, 2026-09-29)
- - [x] Vitória por tempo com material insuficiente do adversário? — Vitória simples, sem verificar material (Architect, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
| G1 Red | PENDING | | |
| G2 Green | PENDING | | |
| G3 Arquitetura | PENDING | | |
| G4 Review | PENDING | | |
| G5 Integração & CI | PENDING | | |
| H2 Integração aprovada | PENDING | | |
| G6 Deploy | PENDING | | |
| G7 Pronto & Docs | PENDING | | |

## 13. Registro de Impedimentos
<!-- Toda parada é registrada pelo Architect com `spec_graph.py impede` e fechada com `resolve` — não edite à mão. Tipos: spec (spec errada/incompleta → resolve com Emenda) | decisão (só o humano decide → resposta ou ADR) | trabalho (falta algo que exige código → SPEC-NNNN nova) | externo (acesso, ambiente, terceiro → ação tomada) | falha (3 FAILs seguidos no mesmo gate → diagnóstico e decisão). Com impedimento aberto a spec aparece como parada no INDEX e não pode ser fechada. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega
<!-- Preenchido no CLOSE (G7). Diz o que foi feito, como, e prova que foi resolvido. Para status implemented o validate exige todas as subseções preenchidas, todo teste do plano com PASS + evidência e a Definição de Pronto toda marcada. -->

### O que foi entregue
<!-- comportamento entregue do ponto de vista do usuário/sistema -->

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|

### Definição de Pronto
- [ ] Todos os testes do plano passando e listados na Verificação
- [ ] Todo comportamento do Mapa de Comportamentos coberto e verificado
- [ ] Suíte completa, arquitetura e CI verdes no resultado integrado (G5)
- [ ] Review independente sem achados blocker/major (G4)
- [ ] Padrão arquitetural existente mantido, ou desvio coberto por ADR aprovado
- [ ] Requisitos não-funcionais medidos com evidência (ou N/A justificado)
- [ ] Disponível no ambiente-alvo via pipeline, com smoke/E2E passando no ambiente (G6)
- [ ] Observabilidade e rollback prontos conforme o Plano de Rollout
- [ ] Documentação raiz e CHANGELOG atualizados (G7)
- [ ] Pendências registradas como novas specs (ou nenhuma)

### Deploy
<!-- ambiente(s), versão/tag, data, estratégia, estado da feature flag, execução do pipeline -->

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0052`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
