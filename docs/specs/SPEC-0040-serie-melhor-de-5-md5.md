---
id: SPEC-0040
title: Série melhor de 5 (MD5)
tier: full
type: feature
user_facing: false
status: approved
created: 2026-09-29
parent: SPEC-0035
depends_on: [SPEC-0031, SPEC-0036]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/BotTurnRunner.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/MatchResult.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameplayDbContext.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations/**, src/TicTacToe/TicTacToe.Modules.Matchmaking/MatchmakingService.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs, tests/TicTacToe.Tests/SeriesRulesTests.cs, tests/TicTacToe.Tests/SeriesMatchmakingTests.cs, tests/TicTacToe.Tests/SeriesPersistenceTests.cs]
adrs: []
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0040 — Série melhor de 5 (MD5)

## 1. Visão Geral
Introduz a **série melhor de 5** (vence quem chegar a 3 rodadas) como formato opcional de partida: regras no `GameSession`, **pareamento por formato** no matchmaking, **persistência por rodada** com identificação da série e o **robô abrindo rodadas** quando for a vez dele de começar. O formato padrão continua sendo a partida única, sem qualquer mudança de comportamento. A interface (lobby e arena) vem na SPEC-0044; até lá a série existe no domínio, mas não é alcançável pelo usuário.

## 2. Motivação & Escopo
**Motivação:** o Stitch trata a série MD5 como formato principal do modo competitivo: "Rodada N de 5", placar da série e "Match point". O app só tem partida única com placar acumulado da sessão.

**Objetivos (dentro do escopo):**
- `SeriesFormat` (`Single`, `BestOf5`) e estado de série em `GameSession`: id da série, número da rodada, vitórias por lado, alvo (3), série encerrada, vencedor da série, "match point".
- Regras da série: ponto por rodada vencida (inclusive por W.O.), **empate repete a rodada sem ponto**, **quem abre a rodada alterna** entre os jogadores, e a série termina ao chegar a 3 vitórias.
- `Restart()` na série: com a série em andamento e a rodada encerrada, inicia a próxima rodada; com a série encerrada, inicia **nova série** (zera o placar); durante uma rodada em andamento, não faz nada.
- Matchmaking por formato: fila separada para cada formato; sala privada herda o formato de quem cria.
- `BotTurnRunner`: executa a jogada do robô com atraso injetável, tanto depois da jogada humana quanto na **abertura de rodada** quando o robô começa.
- Persistência: cada rodada é gravada como `MatchResult` com `SeriesId`, `RoundNumber` e `BestOf` (colunas anuláveis; nulas na partida única). Migration **aditiva** (`AddSeriesInfo`).

**Não-objetivos (fora do escopo):**
- Qualquer interface (seletor de formato, cabeçalho da rodada, placar da série, rótulos): SPEC-0044.
- Persistir a série como uma linha própria ou contar séries no ranking (cada rodada é uma partida para histórico e ranking, ver Questões em Aberto).
- Abandonar a série, revanche com aceite e desconexão (SPEC-0041, SPEC-0042).
- Séries de outros tamanhos (melhor de 3, melhor de 7).

## 3. Dependências
- **Implementações necessárias:** SPEC-0031 (arena e `GameSession` estabilizados) e SPEC-0036 (colunas de `MatchResult`, `EndReason`, `Mode` e migrations em sequência).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** `GameSession` tem placar acumulado (`_scores`), `Restart()` e turno inicial sempre X; `MatchmakingService` mantém uma única fila e não referencia `Gameplay` (ADR-0004); a `Home` orquestra o robô com `Task.Delay(250)` embutido (`Home.razor.cs`).

**Decisão:** a série é um modo do próprio `GameSession` (o placar da série reutiliza `_scores`); o matchmaking recebe o formato como inteiro `bestOf` (1 ou 5) para não depender de tipos do `Gameplay`; o robô passa para um serviço testável com atraso injetável.

**Justificativa:** mantém o padrão de sessão única por partida e os módulos desacoplados; `BotTurnRunner` era necessário para testar o robô abrindo rodadas sem esperas reais.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** entidade `Series` separada (persistência e ciclo de vida a mais sem necessidade); compartilhar o enum entre módulos (violaria a regra de dependências); manter o atraso embutido na `Home` (não testável).

**ADRs:** N/A.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** estado de série é O(1) por sessão; duas filas em memória; sem novas consultas.
- **Segurança:** transições de rodada validadas no servidor (`Restart` só age com a rodada encerrada); o cliente não define placar nem rodada.
- **Privacidade e dados pessoais:** N/A — sem dado pessoal novo.
- **Disponibilidade e resiliência:** todas as mudanças de estado sob o `lock` existente; `Restart` repetido ou simultâneo dos dois jogadores é idempotente (não pula rodada); série encerrada não aceita jogadas até `Restart`.
- **Acessibilidade (UI):** N/A — sem UI.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `GameSession`, `BotTurnRunner`, `MatchmakingService` e `MatchResult`.

```text
enum SeriesFormat { Single = 0, BestOf5 = 1 }

GameSession
  GameSession(bool enableBackgroundTimer = true, TimeProvider? timeProvider = null, SeriesFormat format = SeriesFormat.Single)
  SeriesFormat Format { get; }
  Guid? SeriesId { get; }                       // nulo em Single; novo a cada série
  int RoundNumber { get; }                      // exibida: vitórias decididas + 1 (repetição por empate mantém o número); 1 em Single
  int SeriesTarget { get; }                     // 3 em BestOf5
  bool IsSeriesOver { get; }   Player SeriesWinner { get; }
  bool IsMatchPoint(Player player)              // vitórias == SeriesTarget − 1 e série em andamento
  Player RoundStarter { get; }                  // quem abre a rodada: alterna a cada rodada decidida; empate repete o mesmo
  int GetScore(Player)                          // Single: vitórias da sessão · BestOf5: vitórias da série

Regras BestOf5
  vitória (linha ou W.O.) → ponto ao vencedor; empate → sem ponto, mesma rodada repetida
  vitórias == 3 → IsSeriesOver = true, SeriesWinner definido; MakeMove passa a retornar false
  Restart(): série encerrada → nova série (placar zerado, novo SeriesId, X abre) · rodada encerrada → próxima rodada
             rodada em andamento → nenhuma ação
  Single: comportamento atual inalterado

BotTurnRunner(TimeProvider time)
  Task RunAsync(GameSession game, Player bot, AiDifficulty difficulty, TimeSpan delay, CancellationToken ct)
    // aguarda o atraso; se ainda for a vez do robô e a rodada estiver ativa, joga AiPlayer.GetBestMove

MatchmakingService (parâmetro opcional; 1 = partida única, 5 = melhor de 5)
  JoinQueue(..., int bestOf = 1)     CreatePrivateRoom(..., int bestOf = 1)     JoinPrivateRoom(...)  // herda da sala
  int GetMatchBestOf(Guid matchId)

MatchResult   Guid? SeriesId · int? RoundNumber · int? BestOf          // migration AddSeriesInfo (3 colunas anuláveis)
```

**Arquivos/módulos afetados:** ver `touches`. A `Home` passa a usar `BotTurnRunner` e a mapear `bestOf` ↔ `SeriesFormat`; o seletor de formato na UI é da SPEC-0044.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Partida única | Formato Single | Sem série; `Restart` e placar como hoje | UT-01 |
| Pontuação | Vitória por linha e por W.O. na série | Ponto ao vencedor; rodada seguinte com `RoundNumber` + 1 | UT-02 |
| Empate | Rodada empatada na série | Sem ponto; mesma rodada repetida; `RoundNumber` inalterado | UT-03 |
| Fim da série | 3 vitórias | `IsSeriesOver`, `SeriesWinner`; jogadas rejeitadas | UT-04 |
| Match point | 2 vitórias | `IsMatchPoint` verdadeiro; falso ao vencer a série | UT-05 |
| Quem abre | Rodadas decididas 0, 1, 2… e empate | Alterna X, O, X…; empate repete o mesmo | UT-06 |
| Reinício | Rodada em andamento / encerrada / série encerrada | Nada / próxima rodada / nova série com placar zerado | UT-07 |
| Restart concorrente | Dois `Restart` seguidos | Só um avanço de rodada | UT-07 |
| Robô abre rodada | Robô é o próximo a abrir | `BotTurnRunner` joga após o atraso; não joga se a vez mudou | UT-08 |
| Pareamento por formato | Duas pessoas em formatos diferentes e iguais | Só pares do mesmo formato; sala privada herda o formato | UT-09 |
| Gravação da rodada | Rodadas de uma série | `SeriesId` comum, `RoundNumber` e `BestOf` gravados; Single com nulos | IT-01 |
| Migration aditiva | Banco com partidas antigas | Só `AddColumn` anulável; leituras antigas inalteradas | IT-02 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado `GameSession` no formato padrão, então placar acumulado, `Restart()` (X abre) e regras de vitória, empate e W.O. permanecem como hoje (guarda: passa antes da mudança).

### 7.2 Testes Unitários
- **UT-01** — Dado `GameSession` sem série, então `Format` = Single, `SeriesId` nulo e o fluxo de rodadas segue o atual (cobre `CH-01`).
- **UT-02** — Dado `BestOf5`, quando X vence uma rodada por linha e O vence outra por W.O., então cada um soma 1 ponto e `RoundNumber` avança para 2 e depois 3.
- **UT-03** — Dado `BestOf5`, quando uma rodada empata, então ninguém pontua, o tabuleiro é liberado no próximo `Restart` e `RoundNumber` permanece o mesmo.
- **UT-04** — Dado X com 2 pontos que vence a terceira rodada, então `IsSeriesOver` é verdadeiro, `SeriesWinner` = X e `MakeMove` retorna falso; a série termina em no máximo 5 rodadas decididas.
- **UT-05** — Dado X com 2 vitórias na série em andamento, então `IsMatchPoint(X)` é verdadeiro e `IsMatchPoint(O)` é falso; após a vitória de X, ambos são falsos.
- **UT-06** — Dado uma série, então `RoundStarter` é X na rodada 1, O na 2, X na 3; após um empate, o mesmo jogador abre a repetição.
- **UT-07** — Dado `Restart()` com a rodada em andamento, encerrada e com a série encerrada, então respectivamente nada muda, a próxima rodada começa e uma nova série começa com placar zerado e novo `SeriesId`; dois `Restart()` seguidos avançam uma única vez.
- **UT-08** — Dado `BotTurnRunner` com `TimeProvider` de teste, quando o robô deve abrir a rodada, então joga após o atraso; se a vez mudar ou a rodada terminar antes, não joga; cancelamento é respeitado.
- **UT-09** — Dado `MatchmakingService`, quando dois jogadores entram na fila com `bestOf` 5 e outros dois com 1, e um deles entra com 5 e outro com 1, então só pares do mesmo formato se juntam; uma sala privada criada com 5 entrega `GetMatchBestOf` = 5 a quem entra.

### 7.3 Testes de Integração
- **IT-01** — Dado `GameplayDbContext` InMemory, quando três rodadas de uma série e uma partida única são gravadas, então as três compartilham `SeriesId`, têm `RoundNumber` 1–3 e `BestOf` 5, e a partida única tem os três campos nulos.
- **IT-02** — Dado a migration `AddSeriesInfo`, então contém somente `AddColumn` anulável (3 colunas) e linhas antigas continuam sendo lidas por `GetRecentAsync` e `GetLeaderboardAsync`.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (a SPEC-0044 consome a API pública de `GameSession`, coberta por UT-02 a UT-07).

### 7.5 Testes E2E
N/A — `user_facing: false`; a jornada do usuário é a `SPEC-0044:E2E-01`.

### 7.6 Outros
- SQL da migration conferido (`ADD` apenas).

**Dublês e dados de teste:** `TimeProvider` de teste, `GameSession(enableBackgroundTimer: false)`, EF InMemory.

**Ambiente de execução:** xUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; o formato padrão é a partida única, então nenhum usuário entra em série até a SPEC-0044.
- **Dados/schema:** expand only — três colunas anuláveis.
- **Compatibilidade:** `JoinQueue`, `CreatePrivateRoom` e `GameSession` com parâmetros opcionais; fluxo atual inalterado.
- **Observabilidade:** log de informação ao iniciar/encerrar série (id e vencedor).
- **Rollback:** `git revert`; colunas ficam sem uso.
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
- [x] **Empate numa rodada da série:** repetir a rodada sem ponto (recomendado: a série sempre termina em até 5 rodadas decididas e "Rodada N de 5" é sempre verdadeiro) ou contar a rodada e, se a série empatar após 5, jogar desempate? — Repetir a rodada sem ponto. (thomas, 2026-09-29)
- [x] **Quem abre cada rodada:** alternar entre os jogadores (recomendado: justo; exige o robô abrir rodadas no solo) ou X abre sempre (simples, mas favorece X)? — Alternar quem abre a rodada. (thomas, 2026-09-29)
- [x] **Histórico e ranking:** cada **rodada** conta como uma partida (recomendado: nenhuma mudança nas consultas) ou cada **série** conta como uma partida (exige persistir o resultado da série e mudar as SPEC-0038/0039)? — Cada rodada conta como uma partida. (thomas, 2026-09-29)
- [x] **Pareamento online:** filas **separadas por formato** (recomendado: ninguém cai numa série sem escolher) ou o formato só vale para sala privada e solo? — Filas separadas por formato. (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [ ] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [ ] Escrever os testes `SPEC-0040:CH-01`, `SPEC-0040:UT-01`, `SPEC-0040:UT-02`, `SPEC-0040:UT-03`, `SPEC-0040:UT-04`, `SPEC-0040:UT-05`, `SPEC-0040:UT-06`, `SPEC-0040:UT-07`, `SPEC-0040:UT-08`, `SPEC-0040:UT-09`, `SPEC-0040:IT-01`, `SPEC-0040:IT-02` com a tag `SPEC-0040:<ID>`, em commits `test(...)` com `Refs: SPEC-0040`, tocando só `test_paths`
- [ ] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [ ] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0040`)

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [ ] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [ ] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [ ] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [ ] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável
- [ ] PR com `spec_graph.py pr SPEC-0040`, CI verde (G5) e aprovação do merge (H2)
- [ ] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0040`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
