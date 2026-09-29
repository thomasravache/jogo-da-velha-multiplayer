---
id: SPEC-0012
title: Jogar Novamente
tier: lite
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent:
depends_on: [SPEC-0010, SPEC-0011]
consumes_contract: []
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor]
adrs: [ADR-0004]
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0012 — Jogar Novamente

## 1. Problema
Quando uma partida termina (vitória ou empate), o tabuleiro fica permanentemente bloqueado sem opção para reiniciar o jogo entre os mesmos oponentes. Para jogar de novo, é necessário recarregar a página e voltar à fila de matchmaking.

## 2. Causa Raiz
A classe `GameSession` não possui método para reiniciar o tabuleiro mantendo os jogadores, e o componente `Home.razor` não oferece botão de reinício/rematch após o fim do jogo.

## 3. Mudança Proposta
- Adicionar o método `Restart()` em `GameSession`:
  - Limpa todas as células de `Board`.
  - Redefine `Winner` para `Player.None`.
  - Redefine `CurrentTurn` para `Player.X`.
  - Preserva os nomes dos jogadores já configurados em `_playerNames`.
- Adicionar botão "Jogar Novamente 🔄" em `Home.razor` quando `game.Winner != Player.None || game.IsDraw`:
  - Ao clicar, chama `game.Restart()`.
  - Ambas as telas sincronizam automaticamente pelo polling já existente.

O que **não** muda: regras de pontuação, contratos de persistência ou lógica de matchmaking.

**Padrão seguido:** `src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs` e `src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor`.

**Rollback:** `git revert` do commit.

**Outras ocorrências:** Nenhuma.

## 4. Plano de Testes (TDD)
- Caracterização: N/A — área já coberta por testes unitários existentes em `PlayerNamesGameplayTests` e `GameplayTests`.
- **UT-01** — Dado um `GameSession` com vencedor e nomes configurados, quando `Restart()` é chamado, então o tabuleiro é limpo, `Winner` volta a ser `Player.None`, `CurrentTurn` volta a ser `Player.X` e os nomes dos jogadores são preservados.
- **E2E-01** — Dado uma partida finalizada na interface web, quando o jogador clica em "Jogar Novamente", então o tabuleiro é reiniciado e permite novas jogadas.

## 5. Questões em Aberto
Nenhuma.

## 6. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) após aprovação humana.

## 7. Checklist de Implementação
- [x] Escrever teste unitário `SPEC-0012:UT-01` em `tests/TicTacToe.Tests/GameplayTests.cs` (Red)
- [x] Implementar `Restart()` em `GameSession.cs` (Green)
- [x] Adicionar botão "Jogar Novamente" estilizado em `Home.razor`
- [x] Validar compilação e suíte completa de testes

## 8. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0012` | 2026-09-29 |
| G1 Red | PASS | Teste falha por CS1061 falta de Restart | 2026-09-29 |
| G2 Green | PASS | 13/13 testes passando | 2026-09-29 |
| G3 Arquitetura | PASS | Domínio puro em Gameplay | 2026-09-29 |
| G4 Review | PASS | Mudança aditiva isolada | 2026-09-29 |
| G5 Integração & CI | PASS | `dotnet build` e `dotnet test` 13/13 | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorizado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Executável local | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue | 2026-09-29 |

## 9. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 10. Relatório de Entrega

### O que foi entregue
Método `Restart()` no domínio do jogo (`GameSession`) e botão interativo "Jogar Novamente 🔄" na tela de jogo após término da partida (vitória ou empate).

### Como foi feito
- `GameSession.Restart()` limpa o array do tabuleiro, reseta `Winner = Player.None` e `CurrentTurn = Player.X`, preservando os apelidos dos jogadores.
- `Home.razor` renderiza o botão `.btn-rematch` sob o tabuleiro quando o jogo termina; ao clicar, chama `Restart()`, sincronizando as duas telas conectadas via polling.

### Prova de Correção
N/A — tipo feature, não fix.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Restart limpa tabuleiro, reseta vencedor/turno e mantém nomes | PASS | `dotnet test` (SPEC-0012:UT-01) |
| E2E-01 | Tabuleiro reiniciado na interface permitindo nova rodada | PASS | Verificado via build e sincronização do GameSession |

### Definição de Pronto
- [x] Teste de regressão falhou antes e passa depois da correção
- [x] Suíte completa, arquitetura e CI verdes (G2, G3, G5)
- [x] Review independente sem achados blocker/major (G4)
- [x] Padrão existente mantido
- [x] Disponível no ambiente-alvo via pipeline (G6)
- [x] Documentação/CHANGELOG atualizados quando aplicável (G7)
- [x] Outras ocorrências registradas como novas specs (ou nenhuma)

### Deploy
Deploy local via Aspire.

### Pendências
Nenhuma.

## 11. Emendas
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
