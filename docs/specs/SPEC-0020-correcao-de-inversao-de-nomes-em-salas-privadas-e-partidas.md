---
id: SPEC-0020
title: Correção de Inversão de Nomes em Salas Privadas e Partidas
tier: lite
type: fix
user_facing: true
status: implemented
created: 2026-09-29
parent:
depends_on: []
consumes_contract: []
touches: [src/TicTacToe/TicTacToe.Modules.Matchmaking/**, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor, tests/TicTacToe.Tests/**]
adrs: []
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0020 — Correção de Inversão de Nomes em Salas Privadas e Partidas

## 1. Problema
Em partidas iniciadas via sala privada (e eventualmente no matchmaking público), os nomes dos jogadores aparecem invertidos: o criador da sala (Host, ex: "maneiro") joga como `Player.X`, mas a interface exibe "Sua vez, legal" (nome do convidado), e a barra de jogadores exibe os símbolos trocados com os nomes.

## 2. Causa Raiz
No componente `Home.razor`, o método `EnsureGameExists()` recupera as conexões dos jogadores através de:
`Matchmaking.ActiveMatches.Where(kvp => kvp.Value == MatchId.Value).Select(kvp => kvp.Key).ToList()`
Como `ActiveMatches` é um `ConcurrentDictionary<string, Guid>`, a iteração das chaves não possui ordem determinística (depende do algoritmo de hashing das chaves/GUIDs das conexões). O primeiro elemento que a iteração retorna é atribuído a `Player.X` e o segundo a `Player.O`.
Quando a conexão do convidado cai em um bucket anterior à do host, o convidado é registrado no `GameSession` como `Player.X` e o host como `Player.O`, gerando incompatibilidade com `MyPlayer` e invertendo os nomes.

## 3. Mudança Proposta
- No `MatchmakingService`, armazenar explicitamente os papéis dos jogadores por partida: `_matchPlayers[matchId] = (PlayerX: hostConnectionId, PlayerO: guestConnectionId)` (tanto em `JoinPrivateRoom` quanto em `JoinQueue`).
- Adicionar método `GetMatchPlayers(Guid matchId)` e `GetMatchPlayerNames(Guid matchId)` retornando a ordem determinística de `Player.X` e `Player.O`.
- Em `Home.razor`, substituir a busca não-determinística em `ActiveMatches` pela consulta direta a `GetMatchPlayers(matchId)` / `GetMatchPlayerNames(matchId)`.
- Sincronizar `MyPlayer` com base nessa mesma definição determinística (`MyPlayer = match.PlayerX == ConnectionId ? Player.X : Player.O`).

**Padrão seguido:** `src/TicTacToe/TicTacToe.Modules.Matchmaking/MatchmakingService.cs`

**Rollback:** `git revert`

**Outras ocorrências:** Nenhuma.

## 4. Plano de Testes (TDD)
- Caracterização: N/A — área já coberta.
- **UT-01** — Dado host criando sala privada e convidado entrando, `GetMatchPlayers` retorna deterministamente o host como `PlayerX` e o convidado como `PlayerO`, e `GetMatchPlayerNames` retorna seus nomes na mesma ordem.
- **UT-02** — Dado matchmaking público, o primeiro jogador que entrou na fila é deterministamente `PlayerX` e o segundo é `PlayerO`.
- **IT-01** — Em múltiplas salas criadas com chaves aleatórias, os nomes atribuídos a `Player.X` e `Player.O` no `GameSession` nunca invertem.
- **E2E-01** — No componente `Home.razor`, os jogadores da partida são associados diretamente a partir da ordem determinística do `MatchmakingService`.

## 5. Questões em Aberto
Nenhuma.

## 6. Aprovação (H1)
Aguardando aprovação humana.

## 7. Checklist de Implementação

**Fase 1: Testes de Regressão (Red)**
- [x] Escrever testes de regressão em `tests/TicTacToe.Tests/DeterministicPlayerAssignmentTests.cs` com tags `SPEC-0020:UT-01`, `SPEC-0020:UT-02`, `SPEC-0020:IT-01` e `SPEC-0020:E2E-01`
- [x] Confirmar que os testes falham antes da implementação (Red)

**Fase 2: Implementação (Green)**
- [x] No `MatchmakingService`, armazenar par determinístico de conexões por partida (`_matchPlayers[matchId] = (playerX, playerO)`)
- [x] Implementar métodos `GetMatchPlayers(Guid matchId)` e `GetMatchPlayerNames(Guid matchId)`
- [x] Em `Home.razor`, atualizar `EnsureGameExists()` para definir nomes e sincronizar `MyPlayer` a partir do par determinístico
- [x] Confirmar que todos os testes passam (Green)

**Fase 3: Refactor & Qualidade**
- [x] Validar cobertura e suíte de testes (`dotnet test`)
- [x] Registrar evidências dos gates G1–G4

**Fase final: Integração e Entrega**
- [x] Preencher Relatório de Entrega
- [x] Fechar spec (G7) e atualizar INDEX.md

## 8. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0020`: 0 erros | 2026-09-29 |
| G1 Red | PASS | `spec_graph.py verify SPEC-0020`: commit c30326d com falha CS1061 e rastreabilidade 4/4 | 2026-09-29 |
| G2 Green | PASS | `dotnet test`: 41/41 testes verdes | 2026-09-29 |
| G3 Arquitetura | PASS | Arquitetura mantida (módulo Matchmaking isolado, UI consome contratos) | 2026-09-29 |
| G4 Review | PASS | Revisão independente: escopo estrito em touches, Red antes do Green | 2026-09-29 |
| G5 Integração & CI | PASS | `dotnet test`: 41/41 verdes, `verify` 0 falhas | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorizado pelo usuário ('Pode implementar') | 2026-09-29 |
| G6 Deploy | PASS | Executável local Aspire / Web atualizado | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue, 41/41 testes passando | 2026-09-29 |

## 9. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 10. Relatório de Entrega

### O que foi entregue
Correção definitiva da inversão de nomes de jogadores em salas privadas e partidas públicas através de rastreamento determinístico e explícito dos papéis de `Player.X` e `Player.O` no `MatchmakingService` e sincronização no Blazor.

### Como foi feito
- Adicionado dicionário `_matchPlayers` no `MatchmakingService` indexado por `matchId` contendo a tupla `(PlayerX, PlayerO)`.
- Criados métodos de consulta `GetMatchPlayers(matchId)` e `GetMatchPlayerNames(matchId)`.
- Atualizado `Home.razor` em `EnsureGameExists()` para recuperar deterministicamente os nomes e sincronizar `MyPlayer`.

### Prova de Correção
No commit `c30326d` (Red), os testes de regressão falharam com CS1061 pela ausência de `GetMatchPlayers` e `GetMatchPlayerNames`. No commit `9959017` (Green), a implementação foi adicionada e os 41 testes da suíte passaram com sucesso.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Sala privada define Host como PlayerX e Convidado como PlayerO | PASS | DeterministicPlayerAssignmentTests.PrivateRoom_ShouldAssignHostAsPlayerX_AndGuestAsPlayerO |
| UT-02 | Matchmaking público define primeiro jogador como PlayerX e segundo como PlayerO | PASS | DeterministicPlayerAssignmentTests.QueueMatchmaking_ShouldAssignFirstPlayerAsPlayerX_AndSecondAsPlayerO |
| IT-01 | Múltiplas salas privadas nunca invertem a ordem dos nomes | PASS | DeterministicPlayerAssignmentTests.MultiplePrivateRooms_ShouldNeverInvertPlayerOrder |
| E2E-01 | Home.razor utiliza papéis determinísticos do MatchmakingService | PASS | DeterministicPlayerAssignmentTests.HomeRazor_ShouldConsumeDeterministicMatchPlayers |

### Definição de Pronto
- [x] Teste de regressão falhou antes e passa depois da correção
- [x] Suíte completa, arquitetura e CI verdes (G2, G3, G5)
- [x] Review independente sem achados blocker/major (G4)
- [x] Padrão existente mantido
- [x] Disponível no ambiente-alvo via pipeline (G6)
- [x] Documentação/CHANGELOG atualizados quando aplicável (G7)
- [x] Outras ocorrências registradas como novas specs (ou nenhuma)

### Deploy
Deploy local via Aspire e Web.

### Pendências
Nenhuma.

## 11. Emendas
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
