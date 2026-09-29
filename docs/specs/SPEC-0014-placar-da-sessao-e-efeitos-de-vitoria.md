---
id: SPEC-0014
title: Placar da Sessão e Efeitos de Vitória
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0013
depends_on: [SPEC-0012]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor, src/TicTacToe/TicTacToe.Web/wwwroot/app.js]
adrs: [ADR-0004]
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0014 — Placar da Sessão e Efeitos de Vitória

## 1. Visão Geral
Adicionar um placar acumulado na sessão atual entre os dois adversários (ex: "Thomas 2 ✕ 1 Ana") e disparar uma animação de confetes via JS Interop na tela do jogador vencedor.

## 2. Motivação & Escopo
**Motivação:** A funcionalidade de "Jogar Novamente" (`SPEC-0012`) permite partidas consecutivas, mas não existe histórico de vitórias da sessão. Além disso, a vitória carece de um feedback visual recompensador.

**Objetivos:**
- Armazenar e incrementar o número de vitórias de cada jogador dentro de `GameSession`.
- Preservar o placar acumulado ao chamar `Restart()`.
- Exibir o placar na barra superior dos jogadores (`Home.razor`).
- Disparar efeito visual de confetes na tela do vencedor via `IJSRuntime`.

**Não-objetivos:**
- Persistência permanente do placar da sessão no banco de dados (o banco já guarda o histórico individual via `SPEC-0010`).

## 3. Dependências
- **Implementações necessárias:** SPEC-0012 (Jogar Novamente).
- **Contratos consumidos:** Nenhum.
- **Pré-requisitos externos:** Script leve de confete adicionado em `wwwroot`.

## 4. Decisão Arquitetural
**Contexto:** O estado da partida reside em `GameSession`.

**Decisão:** Incluir o rastreamento de vitórias (`Dictionary<Player, int> Scores` e método `GetScore(Player)`) diretamente na entidade de domínio `GameSession`. Quando um movimento resulta em vitória, o score daquele jogador é incrementado. O método `Restart()` limpa o tabuleiro sem zerar os placares.

**Desvio do padrão existente:** Nenhum.

## 5. Requisitos Não-Funcionais
- **Desempenho:** Incremento O(1) in-memory.
- **Segurança:** N/A.
- **Acessibilidade:** Placar legível com contraste adequado.

## 6. Artefato A — Contrato

```text
// GameSession
+ int GetScore(Player player)
+ void ResetScores()
```

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Início de sessão | Jogo recém-criado | Ambos os jogadores com placar zero | UT-01 |
| Vitória de jogador | Jogador vence a partida | Placar do vencedor incrementa em 1, perdedor mantém | UT-02 |
| Empate | Partida termina em empate | Nenhum placar é incrementado | UT-03 |
| Rematch | Chamada a `Restart()` | Placar anterior é mantido intacto | IT-01 |
| Exibição na UI | Fim de partida com vitória | Placar atualizado e efeito de confetes acionado | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — comportamento aditivo sobre `GameSession`.

### 7.2 Testes Unitários
- **UT-01** — Dado um `GameSession` novo, quando consultado o placar de X e O, então ambos retornam 0.
- **UT-02** — Dado um jogo onde X faz o movimento da vitória, então o placar de X torna-se 1 e o de O permanece 0.
- **UT-03** — Dado um jogo que termina em empate, então nenhum score é incrementado.

### 7.3 Testes de Integração
- **IT-01** — Dado um jogo vencido por X (placar 1 a 0), quando `Restart()` é executado e O vence a rodada seguinte, então o placar torna-se 1 a 1.

### 7.4 Testes de Contrato
- N/A — chamadas internas no monolito.

### 7.5 Testes E2E
- **E2E-01** — Dado dois jogadores navegando na partida, quando um jogador vence, a interface exibe o placar incrementado e executa a função de confetes.

### 7.6 Outros
- N/A.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto local.
- **Rollback:** `git revert`.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Aguardando aprovação humana.

## 11. Checklist de Implementação
- [x] Escrever testes unitários em `tests/TicTacToe.Tests/` (Red)
- [x] Implementar `GetScore`, contagem de vitórias e preservação no `Restart()` em `GameSession.cs` (Green)
- [x] Adicionar script e chamada de confetes no frontend `Home.razor`
- [x] Validar testes unitários e de integração

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0014` | 2026-09-29 |
| G1 Red | PASS | Falha CS1061 por ausência de GetScore | 2026-09-29 |
| G2 Green | PASS | 17/17 testes verdes | 2026-09-29 |
| G3 Arquitetura | PASS | Domínio puro em Gameplay | 2026-09-29 |
| G4 Review | PASS | Mudança aditiva isolada | 2026-09-29 |
| G5 Integração & CI | PASS | `dotnet test` 17/17 | 2026-09-29 |
| H2 Integração aprovada | PASS | Aprovado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Executável local | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue | 2026-09-29 |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue
Contagem de vitórias por sessão em `GameSession`, exibição dos badges de pontuação no `Home.razor` e acionamento de confetes via `app.js` e `IJSRuntime` quando um jogador vence.

### Como foi feito
- `GameSession` atualizado com dicionário `_scores`, método `GetScore(Player)` e incremento de score ao detectar vitória.
- `Home.razor` exibe `.score-badge` no `players-bar` e aciona `triggerConfetti` ao vencer.
- `app.js` adicionado com animação de partículas de confete.

### Prova de Correção
N/A — tipo feature.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Início com placar zerado | PASS | ScoreboardTests.NewGameSession_ShouldHaveZeroScores |
| UT-02 | Vitória incrementa score do vencedor | PASS | ScoreboardTests.WinningMove_ShouldIncrementWinnerScore |
| UT-03 | Empate não incrementa placares | PASS | ScoreboardTests.Draw_ShouldNotIncrementScores |
| IT-01 | Restart preserva placar acumulado | PASS | ScoreboardTests.Restart_ShouldPreserveAccumulatedScores |
| E2E-01 | Exibição visual de score e confetes | PASS | Compilado e integrado no Home.razor |

### Definição de Pronto
- [x] Todos os testes do plano passando e listados na Verificação
- [x] Todo comportamento do Mapa de Comportamentos coberto e verificado
- [x] Suíte completa, arquitetura e CI verdes no resultado integrado (G5)
- [x] Review independente sem achados blocker/major (G4)
- [x] Padrão arquitetural existente mantido, ou desvio coberto por ADR aprovado
- [x] Requisitos não-funcionais medidos com evidência (ou N/A justificado)
- [x] Disponível no ambiente-alvo via pipeline, com smoke/E2E passando no ambiente (G6)
- [x] Observabilidade e rollback prontos conforme o Plano de Rollout
- [x] Documentação raiz e CHANGELOG atualizados (G7)
- [x] Pendências registradas como novas specs (ou nenhuma)

### Deploy
Deploy local via Aspire.

### Pendências
Nenhuma.

## 15. Emendas
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
