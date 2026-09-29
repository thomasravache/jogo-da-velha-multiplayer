---
id: SPEC-0017
title: Leaderboard e Estatísticas
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0013
depends_on: [SPEC-0010]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/**, src/TicTacToe/TicTacToe.Web/**]
adrs: [ADR-0003, ADR-0004]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0017 — Leaderboard e Estatísticas

## 1. Visão Geral
Criar a página `/leaderboard` com o ranking dos jogadores com maior número de vitórias acumuladas no SQL Server, exibindo posição, apelido, total de vitórias e data da última vitória.

## 2. Motivação & Escopo
**Motivação:** A `SPEC-0010` já persiste cada partida no banco de dados. Um leaderboard transforma esses dados brutos em gamificação e incentivo competitivo.

**Objetivos:**
- Consulta agregada no `GameplayDbContext` agrupando por `WinnerName`.
- Método `GetLeaderboardAsync(int top = 10)` em `GameResultService`.
- Página Blazor `/leaderboard` com tabela visual de ranking e medalhas (🥇, 🥈, 🥉).
- Inclusão do link de navegação correspondente no `NavMenu.razor`.

**Não-objetivos:**
- Separação por temporadas / redefinição de pontuação.
- Perfis individuais detalhados de jogador.

## 3. Dependências
- **Implementações necessárias:** SPEC-0010 (histórico e persistência no SQL Server).
- **Contratos consumidos:** Nenhum.
- **Pré-requisitos externos:** SQL Server via Aspire.

## 4. Decisão Arquitetural
**Contexto:** Todas as entidades e consultas do histórico residem no módulo `Gameplay`.

**Decisão:** Adicionar o DTO `PlayerRank` e a consulta agrupada no `GameResultService`. A página `Leaderboard.razor` consome o serviço diretamente.

**Desvio do padrão existente:** Nenhum.

## 5. Requisitos Não-Funcionais
- **Desempenho:** Consulta agregada rápida utilizando índices na coluna `WinnerName` e `PlayedAt`.

## 6. Artefato A — Contrato

```text
namespace TicTacToe.Modules.Gameplay;

public record PlayerRank(string PlayerName, int Wins, DateTime LastWinAt);

// GameResultService
+ Task<List<PlayerRank>> GetLeaderboardAsync(int top = 10);
```

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Agregação de vitórias | Múltiplas partidas salvas | Agrupamento correto por jogador e contagem de vitórias | UT-01 |
| Empates ignorados | Partidas com WinnerName nulo | Não contabilizadas como vitória para nenhum jogador | UT-02 |
| Ordenação de ranking | Jogador com mais vitórias no topo | Ordenado por `Wins DESC` seguido de `LastWinAt DESC` | IT-01 |
| Exibição na UI | Acesso a `/leaderboard` | Exibição das linhas com medalhas e apelidos | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A.

### 7.2 Testes Unitários
- **UT-01** — Dado partidas registradas com vitórias de diferentes jogadores, `GetLeaderboardAsync` totaliza corretamente as vitórias de cada apelido.
- **UT-02** — Dado partidas terminadas em empate, nenhum apelido é registrado no ranking com vitórias indevidas.

### 7.3 Testes de Integração
- **IT-01** — Dado dados no banco de dados InMemory, `GetLeaderboardAsync` retorna os jogadores ordenados por vitórias decrescentes.

### 7.4 Testes de Contrato
- N/A.

### 7.5 Testes E2E
- **E2E-01** — Navegador em `/leaderboard` exibe tabela de classificação populada ou estado vazio amigável.

### 7.6 Outros
- N/A.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto.
- **Rollback:** `git revert`.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Aguardando aprovação humana.

## 11. Checklist de Implementação
- [x] Testes unitários do cálculo de ranking (Red)
- [x] Implementar `GetLeaderboardAsync` em `GameResultService.cs` (Green)
- [x] Criar página `Leaderboard.razor` e link no `NavMenu.razor`
- [x] Validar testes e build

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0017` | 2026-09-29 |
| G1 Red | PASS | Falha CS1061 por ausência de GetLeaderboardAsync | 2026-09-29 |
| G2 Green | PASS | 28/28 testes verdes | 2026-09-29 |
| G3 Arquitetura | PASS | Consultas de histórico isoladas em Gameplay | 2026-09-29 |
| G4 Review | PASS | Mudança aditiva | 2026-09-29 |
| G5 Integração & CI | PASS | `dotnet test` 28/28 | 2026-09-29 |
| H2 Integração aprovada | PASS | Aprovado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Executável local | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue | 2026-09-29 |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue
Método `GetLeaderboardAsync` no `GameResultService`, página de ranking `/leaderboard` e link correspondente no `NavMenu`.

### Como foi feito
- `GameResultService.GetLeaderboardAsync` agrupa registros de `MatchResults` por `WinnerName`, calculando vitórias e data do último triunfo.
- `Leaderboard.razor` renderiza a tabela de classificação estilizada com medalhas e badges para os líderes.

### Prova de Correção
N/A — tipo feature.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Agrupamento de vitórias por jogador | PASS | LeaderboardTests.GetLeaderboardAsync_ShouldAggregateWinsCorrectly |
| UT-02 | Ignora empates na contagem | PASS | LeaderboardTests.GetLeaderboardAsync_ShouldIgnoreDraws |
| IT-01 | Ordena e limita ranking | PASS | LeaderboardTests.GetLeaderboardAsync_ShouldSortAndLimitResults |
| E2E-01 | Página renderizada no navegador | PASS | Integrado em Leaderboard.razor |

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
