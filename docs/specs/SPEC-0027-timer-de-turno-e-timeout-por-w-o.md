---
id: SPEC-0027
title: Timer de turno e timeout por W.O.
tier: full
type: feature
user_facing: true
status: in-progress
created: 2026-09-29
parent:
depends_on: []
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/**, src/TicTacToe/TicTacToe.Web/**, tests/TicTacToe.Tests/**]
adrs: [ADR-0004, ADR-0007]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0027 — Timer de turno e timeout por W.O.

## 1. Visão Geral
Adiciona controle de tempo limite por turno no Jogo da Velha (15 segundos por jogada) com contagem regressiva visual via MudBlazor e encerramento automático por W.O. (vitória do adversário) caso o jogador da vez não realize sua jogada antes do tempo zerar.

## 2. Motivação & Escopo
**Motivação:** Atualmente as partidas podem ficar travadas indefinidamente se um dos jogadores parar de responder ou abandonar o navegador sem realizar jogadas. Um timer de turno mantém a dinâmica competitiva, agiliza as partidas e resolve abandonos de forma justa.

**Objetivos (dentro do escopo):**
- Controle de tempo no domínio (`GameSession`): 15 segundos por jogada.
- Decremento periódico e evento de atualização em tempo real (`OnStateChanged` / `OnTimerTick`).
- Penalidade de Timeout: vitória automática por W.O. para o adversário e incremento no placar ao atingir 0 segundos.
- Reinício do timer a cada jogada válida e ao reiniciar a partida (`Restart()`).
- Interrupção do timer quando a partida é concluída (vitória normal, empate ou timeout) ou descartada (`Dispose`).
- Indicadores visuais na interface com MudBlazor (`MudChip` com ícone de relógio e `MudProgressLinear` colorido conforme o tempo restante: verde > 7s, amarelo 4–7s, vermelho ≤ 3s).
- Mensagem informativa de vitória por W.O. no término por tempo.
- Funcionamento em todos os modos: Multijogador Online (público e privado) e Solo vs Robô.

**Não-objetivos (fora do escopo):**
- Configuração de tempo customizado por usuário na interface (fixo em 15s nesta versão).
- Banco de tempo estilo xadrez (acréscimo por jogada / incremento Fischer).

## 3. Dependências
- **Implementações necessárias:** N/A (base estável na branch `main`).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** Pacote MudBlazor já configurado (ADR-0007).

## 4. Decisão Arquitetural
**Contexto:** O domínio do jogo reside em `TicTacToe.Modules.Gameplay` (`GameSession.cs`), seguindo a arquitetura Modular Monolith (ADR-0004). O front-end utiliza Blazor Server reativo com componentes MudBlazor (ADR-0007).

**Decisão:**
- O estado e o decremento do timer residem dentro da `GameSession`, gerenciados por um temporizador assíncrono interno (`PeriodicTimer` / `Timer`) que dispara eventos reativos sem bloquear threads.
- `GameSession` implementa `IDisposable` para garantir que o temporizador em segundo plano seja liberado quando a sessão for descartada.
- O componente `GameBoard.razor` consome `RemainingSeconds` e `IsTimedOut` da sessão ativa, exibindo o relógio e a barra de progresso sem lógica de tempo concorrente no cliente.

**Justificativa:** Centralizar o relógio no domínio da sessão assegura que ambos os jogadores conectados à mesma partida (e observadores) compartilhem exatamente a mesma contagem de tempo com integridade de regras.

**Desvio do padrão existente:** Nenhum. Mantém Modular Monolith e eventos reativos em memória estabelecidos na SPEC-0025.

**Alternativas descartadas:**
- Timer exclusivo no Javascript do frontend: descartado por falta de integridade (um jogador poderia pausar o JS ou dessincronizar do oponente).
- Passar a vez em vez de W.O.: descartado porque no jogo da velha dar duas jogadas consecutivas ao adversário desequilibra o tabuleiro de forma disfuncional.

**ADRs:** ADR-0004 (Modular Monolith) e ADR-0007 (MudBlazor).

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Decremento de timer com baixíssimo overhead de CPU usando eventos assíncronos leves em memória.
- **Segurança:** Validação autoritativa do tempo no servidor, impedindo jogadas após o tempo limite ser esgotado.
- **Privacidade e dados pessoais:** N/A — nenhum dado pessoal coletado.
- **Disponibilidade e resiliência:** Cancelamento seguro do temporizador no descarte da sessão (`Dispose`) para evitar vazamentos de memória.
- **Acessibilidade (UI):** Informação textual clara do tempo restante além da cor da barra de progresso (WCAG 2.2 AA).
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `GameSession` em `TicTacToe.Modules.Gameplay`:

```csharp
public class GameSession : IDisposable
{
    public const int DefaultTurnTimeSeconds = 15;
    public int RemainingSeconds { get; }
    public bool IsTimedOut { get; }

    public void StartTimer();
    public void StopTimer();
    // MakeMove reinicia RemainingSeconds = 15
    // Restart reinicia RemainingSeconds = 15
    // Dispose interrompe o timer
}
```

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Início de turno | Turno iniciado ou partida iniciada | `RemainingSeconds` inicia em 15 | UT-01 |
| Decremento do tempo | Tick a cada 1 segundo decorrido | `RemainingSeconds` reduz em 1 e emite `OnStateChanged` | UT-02 |
| Jogada válida a tempo | Jogador realiza `MakeMove` válido antes de zerar | `RemainingSeconds` reseta para 15 para o próximo jogador | UT-03 |
| Timeout / W.O. | `RemainingSeconds` atinge 0 | `Winner` é atribuído ao oponente, `IsTimedOut == true`, score incrementado e timer parado | UT-04 |
| Reinício da partida | Chamada a `Restart()` | `RemainingSeconds` reseta para 15, `IsTimedOut == false` e timer reinicia | UT-05 |
| Fim de jogo normal | Jogador vence com 3 em linha ou empate | Timer é pausado e não ocorrem mais timeouts | UT-06 |
| Renderização na UI | `GameBoard.razor` ativo | Exibe chip com segundos e barra de progresso com cor proporcional | IT-01 |
| Mensagem de W.O. na UI | Fim de jogo por timeout | `GameBoard.razor` exibe alerta de vitória por W.O. | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — funcionalidade nova aditiva ao `GameSession`.

### 7.2 Testes Unitários
- **UT-01** — Dado novo `GameSession`, quando o timer inicia, então `RemainingSeconds` é 15 e `IsTimedOut` é falso.
- **UT-02** — Dado `GameSession` ativo, quando ocorre tick de segundo, então `RemainingSeconds` diminui e `OnStateChanged` é invocado.
- **UT-03** — Dado jogador realizando `MakeMove` válido, quando a jogada é confirmada, então `RemainingSeconds` reseta para 15.
- **UT-04** — Dado jogador cujo tempo esgota (`RemainingSeconds <= 0`), então o oponente é declarado `Winner` por W.O., `IsTimedOut` é verdadeiro e o score do vencedor aumenta.
- **UT-05** — Dado jogo finalizado por timeout, quando `Restart()` é chamado, então o jogo reinicia com `RemainingSeconds = 15` e `IsTimedOut = false`.
- **UT-06** — Dado jogo com vitória normal ou empate, quando o tempo transcorre, o timer não altera o vencedor nem causa timeout adicional.

### 7.3 Testes de Integração
- **IT-01** — Dado componente `GameBoard.razor` com sessão em andamento, quando renderizado via bUnit, então exibe o componente de timer com os segundos restantes.

### 7.4 Testes de Contrato
- N/A — não há contratos entre microsserviços.

### 7.5 Testes E2E
- **E2E-01** — Dado componente `GameBoard.razor` com sessão finalizada por timeout, quando renderizado, então o alerta informa vitória por W.O. devido a tempo esgotado.

### 7.6 Outros
- N/A.

**Dublês e dados de teste:** EF Core InMemory provider e instâncias reais de `GameSession`.
**Ambiente de execução:** `dotnet test` e bUnit.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto no branch integrado via Pull Request.
- **Dados/schema:** N/A (não altera tabelas do banco).
- **Compatibilidade:** Totalmente compatível com `MatchResult` existente.
- **Observabilidade:** Logs informando encerramento de partidas por W.O.
- **Rollback:** Reversão do Pull Request via git revert.
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
- [x] Qual deve ser a duração padrão do tempo de cada jogada? — 15 segundos por turno (thomas, 2026-09-29)
- [x] O que deve acontecer quando o tempo do jogador da vez zerar (timeout)? — Vitória do oponente por W.O. (thomas, 2026-09-29)
- [x] Em quais modos de jogo o timer deve estar ativo? — Em todos os modos: Multijogador público, Salas privadas e Solo vs Robô (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente após o humano responder "Aprovado".

## 11. Checklist de Implementação

**Fase 1 — Red (Testes de Domínio e UI)**
- [ ] Escrever testes unitários `SPEC-0027:UT-01` a `UT-06` em `tests/TicTacToe.Tests/GameTimerTests.cs`
- [ ] Escrever teste de integração bUnit `SPEC-0027:IT-01` e `SPEC-0027:E2E-01` em `tests/TicTacToe.Tests/GameBoardTimerTests.cs`
- [ ] Confirmar que os testes falham pelo motivo esperado (CS1061 / campos inexistentes)

**Fase 2 — Green (Implementação de Domínio e UI)**
- [ ] Implementar propriedades `RemainingSeconds`, `IsTimedOut`, lógica de decremento, timeout por W.O. e `IDisposable` em `GameSession.cs`
- [ ] Atualizar `GameBoard.razor` adicionando `MudChip` com contagem regressiva e `MudProgressLinear` colorido
- [ ] Atualizar exibição de resultado em `GameBoard.razor` quando `IsTimedOut == true` (vitória por W.O.)
- [ ] Validar que todos os testes passam no `dotnet test`

**Fase 3 — Refactor & Qualidade**
- [ ] Executar `dotnet format --verify-no-changes`
- [ ] Confirmar zero warnings (`TreatWarningsAsErrors=true`)

**Fase 4 — Integração e PR**
- [ ] Validar G0 (`spec_graph.py validate SPEC-0027`) e G1/G4 (`spec_graph.py verify SPEC-0027`)
- [ ] Enviar branch `feat/SPEC-0027-tempo-por-turno` para o remoto
- [ ] Abrir Pull Request com corpo gerado por `spec_graph.py pr SPEC-0027`
- [ ] Confirmar CI verde no GitHub Actions (G5)

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | spec_graph.py validate SPEC-0027 | 2026-09-29 |
| G1 Red | PASS | Testes criados e falhando por CS1061/CS1674 | 2026-09-29 |
| G2 Green | PENDING | | |
| G3 Arquitetura | PENDING | | |
| G4 Review | PENDING | | |
| G5 Integração & CI | PENDING | | |
| H2 Integração aprovada | PENDING | | |
| G6 Deploy | PENDING | | |
| G7 Pronto & Docs | PENDING | | |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue

### Como foi feito

### Prova de Correção
N/A — feature, não fix.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|

### Definição de Pronto
- [ ] Todos os testes do plano passando e listados na Verificação
- [ ] Todo comportamento do Mapa de Comportamentos coberto e verificado
- [ ] Suíte completa, arquitetura e CI verdes no resultado integrado (G5)
- [ ] Review independente sem achados blocker/major (G4)
- [ ] Padrão arquitetural existente mantido, ou desvio coberto por ADR aprovado
- [ ] Requisitos não-funcionais medidos com evidência (ou N/A justificado)
- [ ] Disponível no ambiente-alvo via pipeline, com smoke/E2E passando no ambiente (G6)
- [ ] Observabilidade e rollback prontos conforme o Plano de Rollout
- [ ] Documentação raiz e CHANGELOG atualizados (G7)
- [ ] Pendências registradas como novas specs (ou nenhuma)

### Deploy

### Pendências

## 15. Emendas
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
