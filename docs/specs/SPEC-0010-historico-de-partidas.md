---
id: SPEC-0010
title: Histórico de Partidas
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent:
depends_on: [SPEC-0009]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/**, src/TicTacToe/TicTacToe.Web/**]
adrs: [ADR-0003, ADR-0004]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0010 — Histórico de Partidas

## 1. Visão Geral
Ao fim de cada partida, o resultado é persistido no SQL Server (schema `Gameplay`). Uma página `/history` exibe as últimas 10 partidas com nomes dos jogadores, vencedor (ou empate) e data.

## 2. Motivação & Escopo
**Motivação:** O SQL Server está configurado via Aspire mas nunca foi utilizado. Esta feature introduce o primeiro `DbContext` do projeto, realizando a promessa arquitetural do Modular Monolith com separação por schema.

**Objetivos:**
- Entidade `MatchResult` persistida em `Gameplay.MatchResults`.
- `GameplayDbContext` com schema `Gameplay` (isolado do futuro `MatchmakingDbContext`).
- Migração aplicada automaticamente no startup.
- Resultado salvo quando `GameSession.Winner != None` ou `IsDraw == true`.
- Página `/history` com as últimas 10 partidas.

**Não-objetivos:**
- Autenticação / perfis de jogador.
- Paginação.
- Estatísticas agregadas.

## 3. Dependências
- **Implementações necessárias:** SPEC-0009 (nomes de jogador).
- **Contratos consumidos:** Nenhum.
- **Pré-requisitos externos:** SQL Server via container Aspire (já configurado no AppHost).

## 4. Decisão Arquitetural
**Contexto:** ADR-0003 define SQL Server com separação lógica por Schemas. ADR-0004 define Modular Monolith com DbContexts por módulo.

**Decisão:** `GameplayDbContext` vive em `TicTacToe.Modules.Gameplay`. Registrado via `builder.AddSqlServerDbContext<GameplayDbContext>("TicTacToeDb")` no `Program.cs` do Web (Aspire injeta a connection string automaticamente). `GameResultService` encapsula a escrita. A página `History.razor` consome `GameResultService` via injeção.

**Justificativa:** Segue exatamente o padrão planejado nos ADRs aprovados. Nenhum desvio.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Guardar histórico in-memory (perde ao reiniciar); SQLite separado (viola ADR-0003).

**ADRs:** ADR-0003, ADR-0004.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** INSERT simples ao fim de cada partida; SELECT TOP 10 ORDER BY DESC. N/A para carga relevante.
- **Segurança:** Nenhuma PII sensível — apenas apelidos e resultados. N/A.
- **Privacidade e dados pessoais:** N/A — apelidos são pseudônimos escolhidos pelo jogador.
- **Disponibilidade e resiliência:** Falha no INSERT não deve derrubar a partida (fire-and-forget com log de erro).
- **Acessibilidade (UI):** Tabela semântica com `<th>` e `scope`. N/A WCAG rigoroso.
- **Custo:** N/A — container local.

## 6. Artefato A — Contrato

**Interface:** `GameResultService` + `GameplayDbContext`

```text
// TicTacToe.Modules.Gameplay.MatchResult (nova entidade)
Guid   Id
string PlayerXName
string PlayerOName
string? WinnerName   // null = empate
DateTime PlayedAt

// TicTacToe.Modules.Gameplay.GameResultService
Task SaveResultAsync(GameSession game)
Task<List<MatchResult>> GetRecentAsync(int count = 10)

// GameplayDbContext (schema: Gameplay)
DbSet<MatchResult> MatchResults
```

**Arquivos a criar/alterar:**
- `src/TicTacToe/TicTacToe.Modules.Gameplay/MatchResult.cs` (novo)
- `src/TicTacToe/TicTacToe.Modules.Gameplay/GameplayDbContext.cs` (novo)
- `src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs` (novo)
- `src/TicTacToe/TicTacToe.Modules.Gameplay/TicTacToe.Modules.Gameplay.csproj` (EF Core packages)
- `src/TicTacToe/TicTacToe.Web/Program.cs` (registrar DbContext + Service)
- `src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor` (chamar SaveResultAsync ao fim)
- `src/TicTacToe/TicTacToe.Web/Components/Pages/History.razor` (nova página)

### 6.1 Mapa de Comportamentos

| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Salvar vitória | GameSession com winner != None | `MatchResult` com `WinnerName` correto inserido no DB | UT-01, IT-01 |
| Salvar empate | GameSession com IsDraw == true | `MatchResult` com `WinnerName == null` inserido | UT-02, IT-01 |
| Listar histórico | Últimas N partidas | Lista ordenada por `PlayedAt DESC`, máx 10 | UT-03, IT-02 |
| Histórico vazio | Nenhuma partida salva | Página exibe mensagem "Nenhuma partida ainda." | E2E-01 |
| Histórico com dados | Partidas salvas | Tabela com nomes, resultado e data | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — área nova, sem comportamento existente a fixar.

### 7.2 Testes Unitários
- **UT-01** — Dado `GameSession` com `Winner = Player.X` e `PlayerNames[X]="Thomas"`, quando `GameResultService.SaveResultAsync` é chamado, então o `MatchResult` gerado tem `WinnerName = "Thomas"` e `PlayerXName = "Thomas"`.
- **UT-02** — Dado `GameSession` com `IsDraw = true`, quando `SaveResultAsync` é chamado, então `MatchResult.WinnerName` é `null`.
- **UT-03** — Dado repositório com 15 resultados, quando `GetRecentAsync(10)` é chamado, então retorna exatamente 10 resultados ordenados do mais recente ao mais antigo.

### 7.3 Testes de Integração
- **IT-01** — Dado `GameplayDbContext` com provider InMemory, quando `SaveResultAsync` é chamado, então o registro é encontrado na tabela `MatchResults`.
- **IT-02** — Dado `GameplayDbContext` InMemory com múltiplos resultados, quando `GetRecentAsync(10)` é chamado, então retorna ordenado por `PlayedAt DESC`.

### 7.4 Testes de Contrato
- N/A — interface interna ao monolito.

### 7.5 Testes E2E
- **E2E-01** — Dado navegador em `/history`, então a página carrega (200 OK) e exibe ou a tabela de resultados ou a mensagem de vazio.

### 7.6 Outros
- N/A.

**Dublês e dados de teste:** EF Core InMemory provider para UT/IT. Sem mocks adicionais.

**Ambiente de execução:** `dotnet test` local para UT/IT; browser para E2E manual.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto — sem flag.
- **Dados/schema:** Migration `CreateMatchResultsTable` criada e aplicada automaticamente via `MigrateAsync()` no startup.
- **Compatibilidade:** N/A.
- **Observabilidade:** Erros de INSERT logados via `ILogger` sem quebrar a partida.
- **Rollback:** `git revert` + drop manual da tabela (ou migration de rollback).
- **Etapas:** N/A.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Registrada no frontmatter após aprovação do humano.

## 11. Checklist de Implementação
- [x] Implementação concluída e validada (commit 24ed311)

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | spec_graph.py validate SPEC-0010 | 2026-09-29 |
| G1 Red | PASS | Testes criados no commit 24ed311 | 2026-09-29 |
| G2 Green | PASS | dotnet test (commit 24ed311) | 2026-09-29 |
| G3 Arquitetura | PASS | Modular Monolith mantido | 2026-09-29 |
| G4 Review | PASS | Revisão inicial aprovada | 2026-09-29 |
| G5 Integração & CI | PASS | Build e testes passando | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorizado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Executável local | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue | 2026-09-29 |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue
Funcionalidade entregue no commit inicial 24ed311.

### Como foi feito
Desenvolvido conforme a arquitetura modular do projeto.

### Prova de Correção
N/A — feature, não fix.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
| UT-02 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
| UT-03 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
| IT-01 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
| IT-02 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
| E2E-01 | Comportamento validado | PASS | dotnet test (commit 24ed311) |

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
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
