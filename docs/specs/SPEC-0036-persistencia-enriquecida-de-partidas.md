---
id: SPEC-0036
title: Persistência enriquecida de partidas
tier: full
type: feature
user_facing: false
status: implemented
created: 2026-09-29
parent: SPEC-0035
depends_on: [SPEC-0031, SPEC-0045]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/MatchResult.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameplayDbContext.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations/**, src/TicTacToe/TicTacToe.Modules.Matchmaking/MatchmakingService.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs, tests/TicTacToe.Tests/GameSessionMatchDetailsTests.cs, tests/TicTacToe.Tests/MatchDetailsPersistenceTests.cs]
adrs: []
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0036 — Persistência enriquecida de partidas

## 1. Visão Geral
Passa a gravar, para cada partida terminada, além dos nomes e do vencedor: **duração**, **número de lances**, **motivo do fim** (linha, empate, W.O. por tempo; abandono e desconexão já previstos), **lado vencedor** (X ou O), **linha vencedora**, **tabuleiro final** e **modo** (Online, Privada, Solo). São os dados que o Histórico e o Ranking avançados (SPEC-0038 e SPEC-0039) e as séries (SPEC-0040) precisam. Nenhuma tela muda nesta spec.

## 2. Motivação & Escopo
**Motivação:** `MatchResult` guarda só `PlayerXName`, `PlayerOName`, `WinnerName` e `PlayedAt`. O Stitch mostra duração, motivo do fim, lance decisivo e tempo médio por lance, e distinguir partidas solo de online; nada disso existe no banco.

**Objetivos (dentro do escopo):**
- `GameSession`: `StartedAtUtc`, `EndedAtUtc`, `Duration`, `MoveCount`, `EndReason`, `FinalBoard`, `Mode` (com relógio injetável para teste).
- `MatchResult`: colunas novas, **todas anuláveis** (partidas antigas ficam sem os dados): `DurationSeconds`, `MoveCount`, `EndReason`, `WinnerSide`, `WinningLine`, `FinalBoard`, `Mode`. `WinnerSide` resolve a ambiguidade de `WinnerName` quando os dois jogadores têm o mesmo apelido.
- `GameResultService.SaveResultAsync` grava os novos campos.
- Migration EF **apenas aditiva** (`AddMatchDetails`).
- `MatchmakingService.IsPrivateMatch(matchId)` e `Home` atribuindo `GameSession.Mode` (Online, Privada, Solo).

**Não-objetivos (fora do escopo):**
- Mostrar qualquer dado novo na UI (SPEC-0038, SPEC-0039).
- Identidade dos jogadores (SPEC-0037) e séries (SPEC-0040).
- Gravar a lista de lances com tempos (log de lances e replay: backlog H da SPEC-0035); `MoveCount` e `FinalBoard` bastam para as telas planejadas.
- Preencher retroativamente partidas antigas.

## 3. Dependências
- **Implementações necessárias:** SPEC-0031 — `GameSession.WinningLine` já existe e é persistida aqui; SPEC-0045 — cada rodada é gravada uma única vez, então as colunas novas não nascem duplicadas (e os mesmos arquivos `GameSession`, `GameResultService` e `Home.razor.cs` evoluem em sequência).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** migrations EF aplicadas no startup (`MigrateAsync`, já existente).

## 4. Decisão Arquitetural
**Contexto:** módulo `Gameplay` com `GameSession` (estado em memória com `lock`), `GameResultService` (grava `MatchResult` ao fim da partida, SPEC-0010), `GameplayDbContext` e migrations (SPEC-0025); módulo `Matchmaking` (ADR-0004, sem referência direta a `Gameplay`).

**Decisão:** o estado de fim de partida é calculado dentro de `GameSession` no mesmo `lock` das jogadas, e `GameResultService` apenas o lê. O modo entra em `GameSession.Mode`, definido pela `Home` (que conhece o fluxo); a distinção privada/pública vem de `MatchmakingService.IsPrivateMatch`, mantendo os módulos desacoplados.

**Justificativa:** segue o fluxo existente (grava-se ao terminar) e evita consultar `Matchmaking` a partir de `Gameplay`.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** gravar lista de lances com tempos (dado além do necessário); calcular a duração no serviço a partir de `PlayedAt` (não representa o início real); passar o modo como parâmetro do `SaveResultAsync` (aumenta os pontos de chamada na `Home`).

**ADRs:** N/A.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** uma gravação por partida; colunas pequenas (inteiros e strings curtas); sem novo índice nesta spec.
- **Segurança:** valores derivados do estado do servidor, nunca de entrada do cliente.
- **Privacidade e dados pessoais:** nenhum dado pessoal novo (o modo, a duração e o tabuleiro não identificam o jogador); identidade fica na SPEC-0037.
- **Disponibilidade e resiliência:** falha ao gravar continua sendo registrada em log sem interromper a partida (comportamento atual de `SaveResultAsync`); migration aditiva e reversível por `git revert` (colunas ficam sem uso).
- **Acessibilidade (UI):** N/A — sem UI.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `GameSession`, `MatchResult`, `GameResultService.SaveResultAsync(GameSession)`, `MatchmakingService.IsPrivateMatch(Guid)`.

```text
enum GameMode   { Online = 0, Private = 1, Solo = 2 }
enum EndReason  { Line = 0, Draw = 1, Timeout = 2, Abandon = 3, Disconnect = 4 }   // 3 e 4 usados pelas SPEC-0041/0042

GameSession
  GameSession(bool enableBackgroundTimer = true, TimeProvider? timeProvider = null)
  GameMode Mode { get; set; }                 // padrão Online
  DateTimeOffset StartedAtUtc { get; }        // na criação e a cada Restart()
  DateTimeOffset? EndedAtUtc { get; }         // ao vencer, empatar ou estourar o tempo
  TimeSpan? Duration { get; }                 // EndedAtUtc − StartedAtUtc; null em andamento
  int MoveCount { get; }                      // jogadas válidas; zerado no Restart()
  EndReason? EndReason { get; }               // null em andamento
  string FinalBoard { get; }                  // 9 chars: 'X','O','-' na ordem 0..8

MatchResult (colunas novas, todas anuláveis)
  int? DurationSeconds · int? MoveCount · EndReason? EndReason (string, ≤16) ·
  string? WinnerSide ("X"|"O"; nulo em empate) · string? WinningLine ("0,4,8") · string? FinalBoard (char 9) · GameMode? Mode (string, ≤8)

MatchmakingService
  bool IsPrivateMatch(Guid matchId)           // true se a partida veio de sala privada

Migration: AddMatchDetails — ADD COLUMN nullable ×7, sem DROP/ALTER de colunas existentes.
```

**Arquivos/módulos afetados:** ver `touches`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Duração | Partida de 30 s até a vitória | `Duration` = 30 s; `EndedAtUtc` definido; `null` em andamento | UT-01 |
| Lances | 5 jogadas válidas e uma inválida | `MoveCount` = 5; zerado no `Restart()` | UT-02 |
| Motivo do fim | Vitória por linha / empate / estouro do tempo | `EndReason` = Line / Draw / Timeout; `null` em andamento | UT-03 |
| Tabuleiro final | Partida com 4 jogadas | `FinalBoard` com 9 caracteres `X`/`O`/`-` | UT-04 |
| Modo | Solo, privada, fila | `Mode` Solo / Private / Online; padrão Online | UT-05 |
| Sala privada | Partida criada por código | `IsPrivateMatch` verdadeiro; falso para fila | UT-06 |
| Gravação | Partida terminada por linha, empate e W.O. | `MatchResult` com todos os campos coerentes, inclusive `WinnerSide` (X ou O; nulo no empate) | IT-01 |
| Migration aditiva | Banco com partidas antigas | Migration `AddMatchDetails` só adiciona colunas anuláveis; partidas antigas carregam com valores nulos | IT-02 |
| Consultas atuais | Histórico e leaderboard com linhas antigas e novas | Mesmo resultado que antes | IT-02 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — a gravação atual (vencedor, empate, `PlayedAt`) já é coberta por `SPEC-0010:UT-01/UT-02/IT-01` e a leitura por `SPEC-0017`; permanecem como guarda.

### 7.2 Testes Unitários
- **UT-01** — Dado `GameSession` com `TimeProvider` de teste, quando 30 s se passam e alguém vence, então `Duration` = 30 s e `EndedAtUtc` está definido; em andamento ambos são nulos.
- **UT-02** — Dado 5 jogadas válidas e 1 inválida (casa ocupada), então `MoveCount` = 5; após `Restart()`, 0.
- **UT-03** — Dado uma vitória por linha, um empate e um estouro de tempo (`Tick()`), então `EndReason` = Line, Draw e Timeout, respectivamente, e nulo em andamento.
- **UT-04** — Dado uma partida com X na casa 4 e O na casa 0, então `FinalBoard` = `"O---X----"`.
- **UT-05** — Dado `GameSession` novo, então `Mode` = Online; ao atribuir Solo ou Private, é refletido.
- **UT-06** — Dado `MatchmakingService`, quando duas pessoas se pareiam por sala privada e outras duas pela fila, então `IsPrivateMatch` é verdadeiro para a primeira e falso para a segunda.

### 7.3 Testes de Integração
- **IT-01** — Dado `GameplayDbContext` InMemory, quando `SaveResultAsync` grava uma vitória por linha, um empate e uma vitória por W.O., então cada `MatchResult` tem `DurationSeconds`, `MoveCount`, `EndReason`, `WinnerSide` (nulo no empate), `WinningLine` (nula em empate e W.O.), `FinalBoard` e `Mode` coerentes com a sessão; com dois jogadores de mesmo apelido, `WinnerSide` ainda identifica o lado.
- **IT-02** — Dado o modelo do contexto e a migration `AddMatchDetails`, então a migration só contém `AddColumn` anulável para as sete colunas (sem `DropColumn`, `AlterColumn` nem `DropTable`), e uma linha antiga (campos nulos) é lida por `GetRecentAsync` e `GetLeaderboardAsync` com o mesmo resultado que antes.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs nesta fase (as specs seguintes consomem o modelo persistido, coberto por IT-01 e IT-02).

### 7.5 Testes E2E
N/A — `user_facing: false`.

### 7.6 Outros
- Verificação manual no PR: o SQL gerado pela migration (`dotnet ef migrations script`) contém somente `ALTER TABLE ... ADD`.

**Dublês e dados de teste:** `FakeTimeProvider`/`TimeProvider` de teste, `GameSession(enableBackgroundTimer: false)`, EF InMemory (padrão de `MatchHistoryTests`).

**Ambiente de execução:** xUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; a migration roda no startup existente.
- **Dados/schema:** expand only — sete colunas anuláveis; nenhuma coluna existente muda; partidas antigas seguem com nulos.
- **Compatibilidade:** consultas e telas atuais inalteradas.
- **Observabilidade:** log de erro existente em `SaveResultAsync`; conferir no banco que as novas colunas são preenchidas após uma partida real.
- **Rollback:** `git revert` do PR; as colunas ficam sem uso (sem perda de dados).
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [x] N/A — sem testes de caracterização neste plano (área já coberta ou nova)

**Fase 1: Testes (Red)**
- [x] Escrever os testes `SPEC-0036:UT-01`, `SPEC-0036:UT-02`, `SPEC-0036:UT-03`, `SPEC-0036:UT-04`, `SPEC-0036:UT-05`, `SPEC-0036:UT-06`, `SPEC-0036:IT-01`, `SPEC-0036:IT-02` com a tag `SPEC-0036:<ID>`, em commits `test(...)` com `Refs: SPEC-0036`, tocando só `test_paths`
- [x] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [x] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0036`)

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [x] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [x] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [x] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [x] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável — N/A (sem UI)
- [x] PR com `spec_graph.py pr SPEC-0036`, CI verde (G5) e aprovação do merge (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
| G1 Red | PASS | `verify SPEC-0036`: Red antes do Green; 8/8 testes rastreados; 8 falhas iniciais pelos motivos esperados | 2026-09-29 |
| G2 Green | PASS | `dotnet test` 156/156; `dotnet format --verify-no-changes` limpo; SQL da migration com 7 ALTER TABLE ADD anuláveis | 2026-09-29 |
| G3 Arquitetura | N/A | sem suíte `Category=Architecture`; migration aditiva verificada por SPEC-0036:IT-02 | 2026-09-29 |
| G4 Review | PASS | Reviewer independente APPROVED em a1050e3 (0 blocker, 0 major) | 2026-09-29 |
| G5 Integração & CI | PASS | PR #16: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
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

Cada partida terminada passa a ser gravada com duração, número de lances, motivo do fim (linha, empate ou W.O. por tempo), lado vencedor (X ou O, resolvendo homônimos), linha vencedora, tabuleiro final e modo (online, sala privada ou solo). Partidas antigas seguem com esses campos nulos e as consultas de histórico e ranking não mudaram.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

`GameSession` ganhou início, fim, duração, lances, motivo, tabuleiro final e modo, com relógio injetável (`TimeProvider`) e tudo sob o `lock`; `MatchResult` ganhou 7 colunas anuláveis (migration aditiva `AddMatchDetails`, conferida no SQL gerado); `MatchmakingService.IsPrivateMatch` informa a origem da partida e a `Home` define o modo da sessão. `Gameplay.EndReason` desambigua tipo e propriedade. O CI acusou CA1875 (Regex.Count), que só o SDK mais novo do CI enxerga; corrigido no teste.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0036:UT-01 | Dado `GameSession` com `TimeProvider` de teste, quando 30 s se passam e alguém vence, então `Duration` = 30 s  | PASS | `dotnet test` 161/161 no CI (dotnet-ci) do PR #16 |
| SPEC-0036:UT-02 | Dado 5 jogadas válidas e 1 inválida (casa ocupada), então `MoveCount` = 5; após `Restart()`, 0. | PASS | `dotnet test` 161/161 no CI (dotnet-ci) do PR #16 |
| SPEC-0036:UT-03 | Dado uma vitória por linha, um empate e um estouro de tempo (`Tick()`), então `EndReason` = Line, Draw e Timeo | PASS | `dotnet test` 161/161 no CI (dotnet-ci) do PR #16 |
| SPEC-0036:UT-04 | Dado uma partida com X na casa 4 e O na casa 0, então `FinalBoard` = `"O---X----"`. | PASS | `dotnet test` 161/161 no CI (dotnet-ci) do PR #16 |
| SPEC-0036:UT-05 | Dado `GameSession` novo, então `Mode` = Online; ao atribuir Solo ou Private, é refletido. | PASS | `dotnet test` 161/161 no CI (dotnet-ci) do PR #16 |
| SPEC-0036:UT-06 | Dado `MatchmakingService`, quando duas pessoas se pareiam por sala privada e outras duas pela fila, então `IsP | PASS | `dotnet test` 161/161 no CI (dotnet-ci) do PR #16 |
| SPEC-0036:IT-01 | Dado `GameplayDbContext` InMemory, quando `SaveResultAsync` grava uma vitória por linha, um empate e uma vitór | PASS | `dotnet test` 161/161 no CI (dotnet-ci) do PR #16 |
| SPEC-0036:IT-02 | Dado o modelo do contexto e a migration `AddMatchDetails`, então a migration só contém `AddColumn` anulável pa | PASS | `dotnet test` 161/161 no CI (dotnet-ci) do PR #16 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #16 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Nenhuma funcional. Dívidas registradas pela review: o dicionário de partidas privadas nunca é limpo (mesmo padrão dos outros); arredondamento de duração não testado; `Duration`/`EndedAtUtc` só testados em vitória por linha; o Down da migration não é verificado por teste.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0036`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
