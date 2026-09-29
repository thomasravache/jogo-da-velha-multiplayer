---
id: SPEC-0052
title: Sessão de xadrez compartilhada
tier: full
type: feature
user_facing: false
status: implemented
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0050, SPEC-0051]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Chess/Session/ChessSession.cs, src/TicTacToe/TicTacToe.Modules.Chess/Session/ChessSnapshot.cs, src/TicTacToe/TicTacToe.Modules.Chess/Session/ChessMode.cs, tests/TicTacToe.Tests/ChessSessionTests.cs, tests/TicTacToe.Tests/ChessSessionSnapshotTests.cs]
adrs: []
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0052 — Sessão de xadrez compartilhada

## 1. Visão Geral
Cria o **núcleo da sessão de xadrez** compartilhada pelos dois circuitos: dois assentos (cada um com nome, identidade e cor corrente), lances validados por vez, relógio, resultado por regras e por tempo, `Snapshot` imutável para a interface e evento de mudança de estado. Abandono, revanche e presença ficam na SPEC-0061 (mesma classe, arquivo parcial).

## 2. Motivação & Escopo
**Motivação:** A partida em rede precisa de um objeto único, atômico e observável por dois circuitos. O `GameSession` do jogo da velha cresceu em várias specs; aqui o núcleo nasce separado do ciclo de vida para caber em uma spec.

**Objetivos (dentro do escopo):**
- `ChessSession` (`partial`) com dois **assentos** (0 e 1): `SetSeat`, `ColorOf(seat)`, `SeatOf(color)`; a cor de cada assento pode trocar (revanche, SPEC-0061), então a interface sempre pergunta a cor corrente pelo assento.
- `TryMove` que primeiro reavalia o relógio (bandeira caída = derrota por tempo antes de aceitar o lance) e depois valida vez, fim, legalidade e promoção, jogando no `ChessGame` e pressionando o relógio.
- Resultado por regras (`ChessGame`) e por tempo; **vitória por tempo vira empate quando quem venceria não tem material de mate** (K, K+B, K+N), como na FIDE.
- `Snapshot()` imutável tirado sob lock (posição, lances copiados, capturadas, tempos, resultado, nomes, cores, modo): é o que a interface lê, nunca o `ChessGame` mutável.
- Evento `OnStateChanged` sempre fora do lock; `Tick()` (bandeira e gancho `TickLifecycle`); `TryMarkResultRecorded`; instantes e duração; primeira posição = `start ?? Position.Start`.

**Não-objetivos (fora do escopo):**
- Abandono, desistência, revanche e presença (SPEC-0061).
- Pareamento, cores por preferência e persistência (SPEC-0053); robô (SPEC-0054/0058); interface (SPEC-0055 a 0060).
- Proposta de empate e série.

## 3. Dependências
- **Implementações necessárias:** SPEC-0050 (`ChessGame`, resultado, SAN, `HasMatingMaterial`) e SPEC-0051 (`ChessClock`, `TimeControl`).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `GameSession` (SPEC-0027, 0040): estado compartilhado sob `lock`, `OnStateChanged` fora do lock, `TimeProvider` injetável, `Tick` por timer de 1 s. Referência: `GameSession.cs` e `GameSessionMatchDetailsTests`.

**Decisão:** `ChessSession` no módulo Chess, classe parcial (núcleo aqui, ciclo de vida na SPEC-0061), com assentos que guardam a cor corrente e `Snapshot` imutável para leitura pela interface; tipos próprios para não colidir com os de Gameplay nas páginas que importam os dois módulos.

**Justificativa:** Assentos resolvem a troca de cores na revanche sem que a interface guarde cor fixa; o snapshot evita enumerar a lista de lances enquanto o outro circuito joga.

**Desvio do padrão existente:** Nenhum estrutural; a leitura por snapshot é mais estrita que a do jogo da velha (que expõe o estado direto), por causa da lista de lances.

**Alternativas descartadas:** Expor `ChessGame` e `ChessClock` diretamente (risco de `InvalidOperationException` ao enumerar `Moves` durante um lance); generalizar `GameSession` (refactor arriscado no jogo da velha entregue).

**ADRs:** N/A

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Comandos em milissegundos; um evento por comando; `Snapshot` O(n) na quantidade de lances, sem custo perceptível.
- **Segurança:** Todo comando valida no servidor quem age e o estado (turno, fim); comandos inválidos retornam falso sem alterar nada; o `PlayerId` não é exposto no snapshot.
- **Privacidade e dados pessoais:** N/A — só nomes já visíveis.
- **Disponibilidade e resiliência:** Comandos concorrentes produzem um único efeito; leituras por snapshot nunca observam estado parcial; eventos fora do lock evitam deadlock.
- **Acessibilidade (UI):** N/A — sem interface.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `TicTacToe.Modules.Chess: ChessSession (núcleo), ChessSnapshot, ChessMode`

```text
enum ChessMode { Online, Private, Solo }

sealed partial class ChessSession : IDisposable
  ChessSession(TimeControl control, TimeProvider? timeProvider = null, bool enableBackgroundTimer = true, Position? start = null)
  Guid Id;  TimeControl Control;  ChessMode Mode { get; set; }
  // assentos: 0 e 1; cores opostas
  void SetSeat(int seat, string name, Guid? playerId, PieceColor color)
  PieceColor ColorOf(int seat);  int SeatOf(PieceColor color);  string GetSeatName(int seat);  Guid? GetSeatPlayerId(int seat)
  string GetPlayerName(PieceColor color);  Guid? GetPlayerId(PieceColor color)            // pela cor corrente
  bool TryMove(PieceColor player, Square from, Square to, PieceType? promotion, out ChessMove? played)
      // 1) Clock.Tick(): bandeira caída → encerra por Timeout e devolve falso; 2) recusa fim, vez errada, lance ilegal, promoção ausente;
      // 3) Game.TryPlay + Clock.Press; 4) fim por regras encerra e para o relógio; evento fora do lock
  ChessSnapshot Snapshot()
  ChessResult? Result { get; }  bool IsOver { get; }
  DateTimeOffset StartedAtUtc { get; }  DateTimeOffset? EndedAtUtc { get; }  TimeSpan? Duration { get; }
  void Tick()                                      // reavalia bandeira; chama TickLifecycle() (partial void, implementado na SPEC-0061)
  event Action? OnStateChanged                     // nunca dentro do lock
  bool TryMarkResultRecorded()                     // verdadeiro só no primeiro chamador por partida
  internal bool RestartCore(bool swapColors)       // nova partida de `start ?? Position.Start`, relógio novo, cores trocadas se pedido (usado pela SPEC-0061)
  partial void TickLifecycle()

record ChessSnapshot(Guid SessionId, ChessMode Mode, TimeControl Control, Position Position, IReadOnlyList<ChessMove> Moves,
                     IReadOnlyList<PieceType> CapturedByWhite, IReadOnlyList<PieceType> CapturedByBlack,
                     TimeSpan WhiteRemaining, TimeSpan BlackRemaining, PieceColor? ClockRunning, PieceColor SideToMove,
                     ChessResult? Result, string WhiteName, string BlackName, DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc)

Fim por tempo: vence quem não caiu (Outcome do outro lado, Reason = Timeout); se esse vencedor não tem material de mate → Outcome = Draw, Reason = Timeout.
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Assentos e cores | Definir dois assentos; consultar por assento e por cor | Cores opostas; nomes e identidades pelo assento; consulta por cor corrente | UT-01 |
| Jogar lance | Lance legal na vez; fora da vez; ilegal; após o fim; promoção | Aceito só o legal na vez; relógio passa ao outro; demais retornam falso | UT-02 |
| Fim por regras | Mate, afogamento e demais regras na sessão | Resultado registrado, relógio parado, `EndedAtUtc` definido | UT-03 |
| Fim por tempo | Bandeira cai; lance tentado depois da bandeira; vencedor sem material de mate | Vitória do outro por `Timeout`; lance recusado; empate se não há material de mate | UT-04 |
| Snapshot | Leitura durante e depois de lances | Instantâneo consistente e imutável; sem exceção sob leitura × lance concorrentes | UT-05 |
| Eventos | Handler que reentra na sessão | Evento disparado fora do lock | UT-06 |
| Gravação única | `TryMarkResultRecorded` repetido | Verdadeiro uma só vez por partida | UT-07 |
| Partida completa | Mate do pastor com relógio; partida por tempo | Fluxos ponta a ponta no domínio | IT-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — código novo.

### 7.2 Testes Unitários
- **UT-01** — Dado `SetSeat(0, …, White)` e `SetSeat(1, …, Black)`, então `ColorOf`, `SeatOf`, `GetPlayerName(color)` e `GetPlayerId(color)` são coerentes, e cores iguais nos dois assentos são recusadas.
- **UT-02** — Dado uma sessão nova, quando brancas jogam `e2→e4`, então o lance entra no histórico e o relógio das pretas passa a correr (o das brancas não descontou nada); jogar fora da vez, um lance ilegal, depois do fim ou promoção sem peça retorna falso sem mudar o estado.
- **UT-03** — Dado o mate do pastor e uma posição de afogamento, então `Result` traz o resultado e o motivo por regras, `IsOver` é verdadeiro, o relógio para e `EndedAtUtc`/`Duration` ficam definidos.
- **UT-04** — Dado o relógio simulado avançando além do tempo de quem joga, então `Tick` encerra com vitória do outro por `Timeout`; se ninguém rodou `Tick` e alguém tenta jogar depois da bandeira, o lance é recusado e a partida termina por `Timeout`; se o vencedor teria só rei (ou rei e uma peça menor), o resultado é `Draw` com motivo `Timeout`.
- **UT-05** — Dado `Snapshot()` chamado antes e depois de lances, então cada snapshot é imutável (a lista de lances antiga não muda); dado uma thread lendo snapshots em laço enquanto outra joga 40 lances, então nenhuma exceção ocorre e cada snapshot é consistente (posição com o número de lances igual ao do histórico).
- **UT-06** — Dado um handler de `OnStateChanged` que consulta a sessão a partir de outra thread, então a consulta não fica bloqueada (evento fora do lock) em lance e em `Tick` que derruba a bandeira.
- **UT-07** — Dado `TryMarkResultRecorded` chamado duas vezes na mesma partida encerrada, então retorna verdadeiro e depois falso.

### 7.3 Testes de Integração
- **IT-01** — Dado uma sessão real com `ManualTime`, quando se joga o mate do pastor com tempo correndo e depois outra partida termina por tempo, então resultados, motivos, relógios e snapshots são os esperados de ponta a ponta.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
N/A — `user_facing: false`.

### 7.6 Outros
- A corrida leitura × lance do UT-05 repete 50 vezes no CI sem estado inconsistente.

**Dublês e dados de teste:** `ManualTime` (existente), `ChessGame` e `ChessClock` reais; sem mocks.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; sem consumidores até as specs seguintes.
- **Dados/schema:** N/A
- **Compatibilidade:** N/A — código novo.
- **Observabilidade:** N/A — biblioteca; logs na camada Web (specs seguintes).
- **Rollback:** Reverter o PR.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Vitória por tempo com o vencedor sem material de mate? — Empate, como na FIDE (Architect, 2026-09-29; revisão do plano)
- - [x] O relógio corre desde a criação da sessão? — Não: só começa depois do primeiro lance das brancas (SPEC-0051); abortar partida parada fica para spec futura (Architect, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [x] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [x] Escrever `SPEC-0052:IT-01`, `SPEC-0052:UT-01`, `SPEC-0052:UT-02`, `SPEC-0052:UT-03`, `SPEC-0052:UT-04`, `SPEC-0052:UT-05`, `SPEC-0052:UT-06`, `SPEC-0052:UT-07` com a tag `SPEC-0052:<ID>` em commits `test(...)` com `Refs: SPEC-0052` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [x] Refactor mantendo tudo verde
- [x] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes` e `verify SPEC-0052 --base origin/main`

**Fase final: Integração, entrega e documentação**
- [x] Review independente (G4)
- [x] Integração + CI verde (G5) e aprovação (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
| G1 Red | PASS | verify SPEC-0052: Red antes do Green (ae795cc), 8/8 testes do plano rastreados; 19 falharam por NotImplementedException | 2026-09-29 |
| G2 Green | PASS | dotnet test 601 verdes (1 pulado: perft pesado); format limpo; verify PASS | 2026-09-29 |
| G3 Arquitetura | N/A | fronteira de módulos coberta por ChessModuleBoundaryTests (SPEC-0049) | 2026-09-29 |
| G4 Review | PASS | Review independente (subagente): PASS, 0 bloqueantes/maiores. Menores aceitos: SetSeat lança exceção (contrato diz void), leituras do relógio em instantes diferentes com TimeProvider.System (janela de microssegundos), handler que lança em OnStateChanged propaga (padrão do GameSession), InternalsVisibleTo dentro de ChessSession.cs, UT-05 pode dar falso verde mas nunca flaky, RestartCore sem validação (a SPEC-0061 valida) | 2026-09-29 |
| G5 Integração & CI | PASS | PR #39: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
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

Núcleo da sessão de xadrez compartilhada: dois assentos com cor corrente, lances validados por vez com o relógio, resultado por regras e por tempo (vitória vira empate se o vencedor não tem material de mate), Snapshot imutável para a interface, evento de mudança fora do lock, Tick e gravação única. Abandono, revanche e presença ficam na SPEC-0061.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

ChessSession (classe parcial) com lock único e ordem sessão→relógio, TryMove que reavalia a bandeira primeiro, timer de 1 s, snapshot com listas copiadas, RestartCore interno e gancho parcial TickLifecycle para a SPEC-0061; InternalsVisibleTo para os testes.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0052:UT-01 | Dado `SetSeat(0, …, White)` e `SetSeat(1, …, Black)`, então `ColorOf`, `SeatOf`, `GetPlayerName(color)` e `Get | PASS | `dotnet test` 602/602 no CI (dotnet-ci) do PR #39 |
| SPEC-0052:UT-02 | Dado uma sessão nova, quando brancas jogam `e2→e4`, então o lance entra no histórico e o relógio das pretas pa | PASS | `dotnet test` 602/602 no CI (dotnet-ci) do PR #39 |
| SPEC-0052:UT-03 | Dado o mate do pastor e uma posição de afogamento, então `Result` traz o resultado e o motivo por regras, `IsO | PASS | `dotnet test` 602/602 no CI (dotnet-ci) do PR #39 |
| SPEC-0052:UT-04 | Dado o relógio simulado avançando além do tempo de quem joga, então `Tick` encerra com vitória do outro por `T | PASS | `dotnet test` 602/602 no CI (dotnet-ci) do PR #39 |
| SPEC-0052:UT-05 | Dado `Snapshot()` chamado antes e depois de lances, então cada snapshot é imutável (a lista de lances antiga n | PASS | `dotnet test` 602/602 no CI (dotnet-ci) do PR #39 |
| SPEC-0052:UT-06 | Dado um handler de `OnStateChanged` que consulta a sessão a partir de outra thread, então a consulta não fica  | PASS | `dotnet test` 602/602 no CI (dotnet-ci) do PR #39 |
| SPEC-0052:UT-07 | Dado `TryMarkResultRecorded` chamado duas vezes na mesma partida encerrada, então retorna verdadeiro e depois  | PASS | `dotnet test` 602/602 no CI (dotnet-ci) do PR #39 |
| SPEC-0052:IT-01 | Dado uma sessão real com `ManualTime`, quando se joga o mate do pastor com tempo correndo e depois outra parti | PASS | `dotnet test` 602/602 no CI (dotnet-ci) do PR #39 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #39 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

SetSeat lança exceção (contrato diz void); janela de microssegundos entre leituras do relógio com TimeProvider.System; handler que lança em OnStateChanged propaga (padrão do GameSession); InternalsVisibleTo dentro de ChessSession.cs; UT-05 pode dar falso verde mas nunca instável.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0052`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
