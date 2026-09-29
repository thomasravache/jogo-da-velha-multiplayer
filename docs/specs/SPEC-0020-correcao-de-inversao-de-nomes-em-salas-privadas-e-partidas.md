---
id: SPEC-0020
title: Correção de Inversão de Nomes em Salas Privadas e Partidas
tier: lite
type: fix
user_facing: true
status: in-progress
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
- [ ] Escrever testes de regressão em `tests/TicTacToe.Tests/DeterministicPlayerAssignmentTests.cs` com tags `SPEC-0020:UT-01`, `SPEC-0020:UT-02`, `SPEC-0020:IT-01` e `SPEC-0020:E2E-01`
- [ ] Confirmar que os testes falham antes da implementação (Red)

**Fase 2: Implementação (Green)**
- [ ] No `MatchmakingService`, armazenar par determinístico de conexões por partida (`_matchPlayers[matchId] = (playerX, playerO)`)
- [ ] Implementar métodos `GetMatchPlayers(Guid matchId)` e `GetMatchPlayerNames(Guid matchId)`
- [ ] Em `Home.razor`, atualizar `EnsureGameExists()` para definir nomes e sincronizar `MyPlayer` a partir do par determinístico
- [ ] Confirmar que todos os testes passam (Green)

**Fase 3: Refactor & Qualidade**
- [ ] Validar cobertura e suíte de testes (`dotnet test`)
- [ ] Registrar evidências dos gates G1–G4

**Fase final: Integração e Entrega**
- [ ] Preencher Relatório de Entrega
- [ ] Fechar spec (G7) e atualizar INDEX.md

## 8. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0020`: 0 erros | 2026-09-29 |
| G1 Red | PENDING | | |
| G2 Green | PENDING | | |
| G3 Arquitetura | PENDING | | |
| G4 Review | PENDING | | |
| G5 Integração & CI | PENDING | | |
| H2 Integração aprovada | PENDING | | |
| G6 Deploy | PENDING | | |
| G7 Pronto & Docs | PENDING | | |

## 9. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 10. Relatório de Entrega

### O que foi entregue

### Como foi feito

### Prova de Correção
<!-- type fix: teste de regressão falhou antes (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|

### Definição de Pronto
- [ ] Teste de regressão falhou antes e passa depois da correção
- [ ] Suíte completa, arquitetura e CI verdes (G2, G3, G5)
- [ ] Review independente sem achados blocker/major (G4)
- [ ] Padrão existente mantido
- [ ] Disponível no ambiente-alvo via pipeline (G6)
- [ ] Documentação/CHANGELOG atualizados quando aplicável (G7)
- [ ] Outras ocorrências registradas como novas specs (ou nenhuma)

### Deploy

### Pendências

## 11. Emendas
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
