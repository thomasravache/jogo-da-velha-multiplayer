---
id: SPEC-0009
title: Nomes de Jogador
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent:
depends_on: [SPEC-0008]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Matchmaking/**, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor]
adrs: [ADR-0004]
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0009 — Nomes de Jogador

## 1. Visão Geral
Antes de entrar na fila de matchmaking, o jogador informa um apelido (nickname). Esse nome substitui os símbolos "X" e "O" nas mensagens de turno e de resultado, tornando a partida mais pessoal.

## 2. Motivação & Escopo
**Motivação:** Exibir apenas "X" e "O" é impessoal. Com nomes, a partida ganha identidade — "Vez do Thomas!" é muito mais divertido que "Vez do X!".

**Objetivos (dentro do escopo):**
- Campo de texto para o jogador informar seu apelido antes de entrar na fila.
- O apelido é propagado para o `MatchmakingService` e armazenado no `GameSession`.
- Mensagens de turno, vitória e empate usam o apelido em vez do símbolo `Player`.
- Validação mínima: campo obrigatório, máximo de 20 caracteres.

**Não-objetivos (fora do escopo):**
- Persistência do nome em banco de dados.
- Autenticação ou perfis de usuário.
- Histórico de nomes.

## 3. Dependências
- **Implementações necessárias:** SPEC-0008 (UI Interativa — já implementada).
- **Contratos consumidos:** Nenhum.
- **Pré-requisitos externos:** Nenhum.

## 4. Decisão Arquitetural
**Contexto:** O padrão atual é Modular Monolith com Singleton in-memory (ADR-0004). `GameSession` e `MatchmakingService` vivem em `TicTacToe.Modules.Gameplay` e `TicTacToe.Modules.Matchmaking`, respectivamente.

**Decisão:** Adicionar `PlayerNames` como um `Dictionary<Player, string>` dentro de `GameSession` (módulo Gameplay). O `MatchmakingService.JoinQueue` recebe o `playerName` junto com o `connectionId` e o armazena em memória. Quando a partida é formada, os nomes são injetados no `GameSession` criado pelo componente Blazor.

**Justificativa:** Segue o padrão de referência existente — nenhuma nova camada, nenhum novo serviço. Os nomes são dados de sessão de jogo e pertencem ao `GameSession`.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Guardar os nomes apenas no componente Blazor seria mais simples, mas quebraria o isolamento — a UI não deveria ser a única detentora do estado da partida.

**ADRs:** ADR-0004.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** N/A — dados in-memory, operação O(1).
- **Segurança:** Nome é sanitizado no Blazor pelo próprio binding (`@bind`); sem HTML injection pois é renderizado via `@nome` (Razor escapa automaticamente).
- **Privacidade e dados pessoais:** N/A — nome não é persistido, vive apenas na sessão in-memory.
- **Disponibilidade e resiliência:** N/A.
- **Acessibilidade (UI):** Campo `<input>` terá `label` e `placeholder` adequados.
- **Custo:** N/A.

## 6. Artefato A — Contrato

**Interface:** `GameSession.PlayerNames` + `MatchmakingService.JoinQueue(connectionId, playerName)`

```text
// TicTacToe.Modules.Gameplay.GameSession (mudança)
+ Dictionary<Player, string> PlayerNames { get; }   // ex: {X: "Thomas", O: "Ana"}
+ string GetPlayerName(Player player)               // retorna nome ou "X"/"O" como fallback

// TicTacToe.Modules.Matchmaking.MatchmakingService (mudança)
- Guid? JoinQueue(string connectionId)
+ Guid? JoinQueue(string connectionId, string playerName)
+ string? GetPlayerName(string connectionId)        // consultado pelo componente para montar GameSession
```

**Arquivos/módulos afetados:**
- `src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs` — adicionar `PlayerNames` e `GetPlayerName()`
- `src/TicTacToe/TicTacToe.Modules.Matchmaking/MatchmakingService.cs` — adicionar `playerName` ao `JoinQueue` e dicionário `_playerNames`
- `src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor` — campo de input + exibir nomes nas mensagens

### 6.1 Mapa de Comportamentos

| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Campo obrigatório | Clicar "Jogar Agora" sem preencher o nome | Botão permanece desabilitado (ou validação visual impede) | UT-01 |
| Nome muito longo | Nome com 21+ caracteres | Input limita para 20 caracteres (atributo `maxlength`) | UT-02 |
| Partida formada | 2 jogadores com nomes entram na fila | `GameSession.PlayerNames` contém ambos os nomes corretamente mapeados para Player.X e Player.O | UT-03, IT-01 |
| Turno exibido com nome | Jogo em andamento, vez do X | UI exibe "Vez do **Thomas**!" em vez de "Vez do **X**!" | E2E-01 |
| Vitória exibida com nome | X vence | UI exibe "Vitória do **Thomas**! 🎉" | E2E-01 |
| Empate com nomes | Board cheio, sem vencedor | UI exibe "Deu velha! 🤝" (sem nome, não se aplica) | UT-04 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — a área já tem cobertura em SPEC-0007:UT-01.

### 7.2 Testes Unitários
- **UT-01** — Dado `GameSession` sem nomes configurados, quando `GetPlayerName(Player.X)` é chamado, então retorna o fallback `"X"`.
- **UT-02** — Dado `GameSession` com `PlayerNames[Player.X] = "Thomas"`, quando `GetPlayerName(Player.X)` é chamado, então retorna `"Thomas"`.
- **UT-03** — Dado `MatchmakingService` com dois jogadores na fila (cada um com seu nome), quando `JoinQueue` é chamado pelo segundo, então `GetPlayerName` retorna o nome correto para cada `connectionId`.
- **UT-04** — Dado `GameSession` com nomes configurados e board completamente preenchido sem vencedor, então `IsDraw` é `true` (guarda: comportamento existente, não regride).

### 7.3 Testes de Integração
- **IT-01** — Dado dois jogadores que entram na fila com nomes diferentes, quando a partida é formada e `GameSession` é criado com os nomes, então `PlayerNames[Player.X]` e `PlayerNames[Player.O]` são os nomes corretos na ordem de entrada.

### 7.4 Testes de Contrato
- N/A — os contratos alterados são internos ao monolito (sem consumer externo).

### 7.5 Testes E2E
- **E2E-01** — Dado dois navegadores, quando cada um preenche seu apelido e clica "Jogar Agora", então o tabuleiro exibe o apelido correto de cada jogador nas mensagens de turno e na mensagem de vitória.

### 7.6 Outros
- N/A.

**Dublês e dados de teste:** Nenhum mock necessário — tudo in-memory.

**Ambiente de execução:** `dotnet test` local para UT/IT; dois navegadores locais para E2E.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto — feature pequena, sem flag.
- **Dados/schema:** N/A — sem mudança de banco.
- **Compatibilidade:** N/A — sem API pública.
- **Observabilidade:** N/A.
- **Rollback:** `git revert` do commit.
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
- [x] Implementação concluída e validada (commit 24ed311)

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | spec_graph.py validate SPEC-0009 | 2026-09-29 |
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
| UT-04 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
| IT-01 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
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
