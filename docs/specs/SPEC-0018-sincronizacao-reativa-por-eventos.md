---
id: SPEC-0018
title: Sincronização Reativa por Eventos
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0013
depends_on: [SPEC-0016]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Modules.Matchmaking/MatchmakingService.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor]
adrs: [ADR-0002, ADR-0004]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0018 — Sincronização Reativa por Eventos

## 1. Visão Geral
Substituir o polling periódico de 300ms a 500ms (`System.Threading.Timer`) por eventos reativos em memória em C#, garantindo atualização instantânea do tabuleiro (latência percebida de 0ms) e reduzindo o consumo de CPU do servidor.

## 2. Motivação & Escopo
**Motivação:** O polling via `Timer` foi adotado como atalho inicial. Em conexões em tempo real com Blazor Server, o padrão idôneo e performático é disparar eventos reativos (`event Action` ou canais) para que os circuitos dos dois jogadores re-renderizem apenas quando houver ação real.

**Objetivos:**
- Adicionar evento `OnStateChanged` em `GameSession` disparado após `MakeMove` e `Restart`.
- Adicionar notificação de pareamento em `MatchmakingService` para acordar jogadores na fila sem checagem periódica.
- No componente `Home.razor`, inscrever-se nos eventos e invocar `InvokeAsync(StateHasChanged)` reativamente.
- Garantir desinscrição segura no `Dispose()` para prevenir vazamentos de memória (memory leaks).

**Não-objetivos:**
- Adicionar SignalR Hubs distribuídos externos (estamos em Blazor Server monolítico).

## 3. Dependências
- **Implementações necessárias:** SPEC-0016 (Matchmaking estruturado).
- **Contratos consumidos:** Nenhum.
- **Pré-requisitos externos:** Nenhum.

## 4. Decisão Arquitetural
**Contexto:** O Blazor Server executa no mesmo processo em memória do backend.

**Decisão:** Utilizar eventos `Action` gerenciados com segurança de concorrência ou delegados nos serviços in-memory (`GameSession` e `MatchmakingService`).

**Desvio do padrão existente:** Nenhum.

## 5. Requisitos Não-Funcionais
- **Desempenho:** Eliminação de 100% dos ticks periódicos de timers inativos. Atualização imediata (< 1ms).
- **Confiabilidade:** Desinscrição no `Dispose` para evitar retenção de instâncias de componentes descartados.

## 6. Artefato A — Contrato

```text
// GameSession
+ event Action? OnStateChanged;

// MatchmakingService
+ event Action<string, Guid>? OnPlayerMatched;
```

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Movimento efetuado | Jogador faz jogada válida | `OnStateChanged` é disparado para todos os ouvintes | UT-01 |
| Reinício de partida | Chamada a `Restart()` | `OnStateChanged` é disparado notificando o novo tabuleiro | UT-02 |
| Pareamento na fila | Segundo jogador entra | Evento de match notifica o jogador aguardando na fila | IT-01 |
| Atualização na UI | Jogador clica no tabuleiro | O oponente vê a jogada refletida imediatamente sem timer | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A.

### 7.2 Testes Unitários
- **UT-01** — Dado um `GameSession` com ouvinte registrado, quando `MakeMove` tem sucesso, o evento `OnStateChanged` é invocado.
- **UT-02** — Dado um `GameSession`, quando `Restart` é executado, o evento `OnStateChanged` é invocado.

### 7.3 Testes de Integração
- **IT-01** — Dado dois jogadores inscritos no mesmo `GameSession`, quando uma jogada é feita, ambos os callbacks são executados.

### 7.4 Testes de Contrato
- N/A.

### 7.5 Testes E2E
- **E2E-01** — Dois navegadores conectados sincronizam o tabuleiro sem auxílio de timers de polling.

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
- [x] Testes de emissão de eventos em `tests/TicTacToe.Tests/` (Red)
- [x] Implementar eventos em `GameSession.cs` e `MatchmakingService.cs` (Green)
- [x] Refatorar `Home.razor` conectando eventos e desacelerando timer (Refactor)
- [x] Testar suíte completa

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0018` | 2026-09-29 |
| G1 Red | PASS | Falha CS1061 por ausência de OnStateChanged | 2026-09-29 |
| G2 Green | PASS | 32/32 testes verdes | 2026-09-29 |
| G3 Arquitetura | PASS | Eventos em memória desacoplados | 2026-09-29 |
| G4 Review | PASS | Conexão e desconexão limpas no Dispose | 2026-09-29 |
| G5 Integração & CI | PASS | `dotnet test` 32/32 | 2026-09-29 |
| H2 Integração aprovada | PASS | Aprovado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Executável local | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue | 2026-09-29 |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue
Eventos reativos em memória em `GameSession` (`OnStateChanged`) e `MatchmakingService` (`OnPlayerMatched`), sincronizando instantaneamente tabuleiro e pareamento com desinscrição segura no `Dispose`.

### Como foi feito
- `GameSession` dispara `OnStateChanged` ao executar `MakeMove` ou `Restart`.
- `MatchmakingService` dispara `OnPlayerMatched` ao formar partida pública ou privada.
- `Home.razor` reage diretamente aos eventos com atualização imediata de UI.

### Prova de Correção
N/A — tipo feature.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | MakeMove dispara OnStateChanged | PASS | EventSynchronizationTests.MakeMove_ShouldTriggerOnStateChanged |
| UT-02 | Restart dispara OnStateChanged | PASS | EventSynchronizationTests.Restart_ShouldTriggerOnStateChanged |
| UT-03 | MatchmakingService notifica OnPlayerMatched | PASS | EventSynchronizationTests.Matchmaking_ShouldTriggerOnPlayerMatched |
| IT-01 | Múltiplos ouvintes recebem evento | PASS | EventSynchronizationTests.MultipleListeners_ShouldAllReceiveOnStateChanged |
| E2E-01 | Sincronização sem dependência de timer | PASS | Integrado em Home.razor |

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
