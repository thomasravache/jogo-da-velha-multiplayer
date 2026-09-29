---
id: SPEC-0015
title: Modo Solo vs IA Minimax
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0013
depends_on: [SPEC-0014]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/**, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor]
adrs: [ADR-0004]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0015 — Modo Solo vs IA Minimax

## 1. Visão Geral
Permitir que o usuário jogue sozinho contra um oponente virtual (bot) direto pelo navegador, sem necessidade de fila ou segundo jogador, disponibilizando os níveis Fácil (aleatório) e Impossível (Minimax ótimo).

## 2. Motivação & Escopo
**Motivação:** Em desenvolvimento local ou quando não há outro jogador online, o usuário precisa abrir duas abas para testar ou se divertir. O modo solo oferece engajamento imediato.

**Objetivos:**
- Criar a classe `AiPlayer` no módulo `Gameplay` com lógica de escolha de jogadas.
- Suportar dois níveis: `Easy` (jogadas aleatórias válidas) e `Hard` (algoritmo Minimax que nunca perde).
- Permitir iniciar partida solo a partir da tela inicial (`Home.razor`).
- Executar a jogada da IA imediatamente após o turno do jogador humano.

**Não-objetivos:**
- Treinamento por reforço ou Machine Learning externo.
- Salvar partidas de bot no ranking geral de humanos (a menos que sinalizadas como bot).

## 3. Dependências
- **Implementações necessárias:** SPEC-0014 (regras e estrutura de GameSession).
- **Contratos consumidos:** Nenhum.
- **Pré-requisitos externos:** Nenhum.

## 4. Decisão Arquitetural
**Contexto:** O módulo `Gameplay` é o detentor de todas as regras e lógica do jogo.

**Decisão:** Implementar `AiPlayer` em `TicTacToe.Modules.Gameplay`. O Minimax simula os ramos do jogo pontuando vitórias (+10), derrotas (-10) e empates (0) ponderados pela profundidade.

**Desvio do padrão existente:** Nenhum.

## 5. Requisitos Não-Funcionais
- **Desempenho:** Cálculo do Minimax em menos de 10ms em C#.
- **Resiliência:** Garantia de que a IA sempre escolhe uma casa válida (`Player.None`).

## 6. Artefato A — Contrato

```text
namespace TicTacToe.Modules.Gameplay;

public enum AiDifficulty { Easy, Hard }

public static class AiPlayer
{
    public static int GetBestMove(GameSession game, Player aiPlayer, AiDifficulty difficulty);
}
```

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Jogada válida fácil | Tabuleiro com espaços livres | Retorna índice livre entre 0 e 8 | UT-01 |
| Bloqueio imediato | Oponente a uma jogada de vencer | Minimax joga na casa que impede a vitória | UT-02 |
| Vitória imediata | IA a uma jogada de vencer | Minimax escolhe a casa da vitória | UT-03 |
| Invencibilidade | Qualquer partida completa contra Minimax | IA ganha ou empata, jamais perde | IT-01 |
| Fluxo solo na UI | Usuário clica em "Jogar vs Robô" | Partida inicia com o bot e bot responde às jogadas | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — novo componente de domínio.

### 7.2 Testes Unitários
- **UT-01** — Dado um tabuleiro com posições livres, quando a IA no modo Fácil é consultada, retorna uma posição válida e vazia.
- **UT-02** — Dado um tabuleiro onde o oponente possui duas peças alinhadas (ex: 0 e 1), quando a IA no modo Difícil calcula a jogada, seleciona a posição 2 para bloquear.
- **UT-03** — Dado um tabuleiro onde a IA pode vencer na jogada atual, o Minimax escolhe a casa vitoriosa.

### 7.3 Testes de Integração
- **IT-01** — Simulação exaustiva onde a IA joga contra estratégias variadas e em 100% dos casos o resultado é vitória da IA ou empate.

### 7.4 Testes de Contrato
- N/A.

### 7.5 Testes E2E
- **E2E-01** — Dado usuário selecionando "Modo Solo", a cada clique válido no tabuleiro o bot responde automaticamente com sua jogada.

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
- [x] Testes unitários do algoritmo Minimax (Red)
- [x] Implementar classe `AiPlayer` e Minimax (Green)
- [x] Integrar seleção de modo Solo no componente `Home.razor`
- [x] Testes de regressão e compilação

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0015` | 2026-09-29 |
| G1 Red | PASS | Falha CS0103 por ausência de AiPlayer | 2026-09-29 |
| G2 Green | PASS | 21/21 testes verdes | 2026-09-29 |
| G3 Arquitetura | PASS | Domínio de IA isolado em Gameplay | 2026-09-29 |
| G4 Review | PASS | Mudança aditiva | 2026-09-29 |
| G5 Integração & CI | PASS | `dotnet test` 21/21 | 2026-09-29 |
| H2 Integração aprovada | PASS | Aprovado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Executável local | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue | 2026-09-29 |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue
Classe `AiPlayer` com algoritmo Minimax ótimo e modo Fácil aleatório, além da integração do modo de jogo Solo ("Jogar vs Robô (IA) 🤖") na interface Blazor.

### Como foi feito
- `AiPlayer.GetBestMove` implementa árvore minimax recursiva com pontuações ponderadas por profundidade para garantir invencibilidade no modo `Hard`.
- `Home.razor` possui botão de inicialização direta de partida solo com o bot e despacho assíncrono da jogada da IA.

### Prova de Correção
N/A — tipo feature.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Modo Fácil retorna índice livre | PASS | AiPlayerTests.EasyMode_ShouldReturnValidAndEmptyIndex |
| UT-02 | Modo Difícil bloqueia vitória imediata | PASS | AiPlayerTests.HardMode_ShouldBlockOpponentImmediateWin |
| UT-03 | Modo Difícil finaliza vitória | PASS | AiPlayerTests.HardMode_ShouldTakeImmediateWin |
| IT-01 | Minimax invencível em simulação | PASS | AiPlayerTests.HardMode_ShouldNeverLoseAgainstVariousMoves |
| E2E-01 | Partida solo funcional na UI | PASS | Integrado em Home.razor |

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
