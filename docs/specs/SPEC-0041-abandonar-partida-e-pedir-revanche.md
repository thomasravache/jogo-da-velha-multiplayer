---
id: SPEC-0041
title: Abandonar partida e pedir revanche
tier: full
type: feature
user_facing: true
status: approved
created: 2026-09-29
parent: SPEC-0035
depends_on: [SPEC-0044, SPEC-0045]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs, src/TicTacToe/TicTacToe.Web/Components/Game/RematchBar.razor, src/TicTacToe/TicTacToe.Web/Components/Game/ArenaActions.razor, src/TicTacToe/TicTacToe.Web/Components/Game/Scoreboard.razor, tests/TicTacToe.Tests/LeaveAndRematchTests.cs, tests/TicTacToe.Tests/LeaveAndRematchUiTests.cs, tests/TicTacToe.Tests/ArenaCyberArenaTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0041 — Abandonar partida e pedir revanche

## 1. Visão Geral
Acrescenta dois fluxos que o Stitch mostra na arena: **Abandonar** (com confirmação) e **Pedir revanche** com aceite do oponente. Hoje qualquer jogador pode reiniciar a partida sozinho e não existe forma de sair dela. Passam a valer: abandono = vitória do oponente por W.O. (motivo "Abandono"); revanche entre humanos só começa se o outro aceitar; e há um **Voltar ao lobby** ao fim da partida.

## 2. Motivação & Escopo
**Motivação:** sair de uma partida em andamento só é possível recarregando a página, o que deixa o oponente esperando o tempo do turno; e o "Jogar novamente" reinicia a partida do oponente sem o seu consentimento.

**Objetivos (dentro do escopo):**
- Referências: `docs/design/stitch/arena-desktop.html` e `arena-mobile.html` (botões **Abandonar** e **Pedir revanche**).
- **Abandonar** em partida online, privada ou solo, com confirmação em duas etapas ("Abandonar" → "Confirmar abandono?" / "Cancelar").
  - Online e privada: o oponente vence a rodada por `EndReason.Abandon`; em **série**, o abandono encerra a série com vitória do oponente.
  - Solo: sai para o lobby sem gravar resultado.
- **Voltar ao lobby** ao fim da partida (e para o oponente que ficou depois de um abandono).
- **Revanche com aceite** entre humanos: **Pedir revanche** → o oponente vê "{nome} pediu revanche" com **Aceitar** e **Recusar**; aceitar inicia nova partida (ou nova série); recusar avisa quem pediu; pedidos simultâneos são aceitos automaticamente; o pedido expira em 30 s; se o oponente saiu, a revanche fica indisponível ("Oponente saiu da partida").
- Solo: **Jogar novamente** continua imediato.
- Dentro de uma série, **Próxima rodada** continua imediata (a série já foi combinada).

**Não-objetivos (fora do escopo):**
- Detectar oponente desconectado (SPEC-0042).
- Chat, reações e taunts (backlog G).
- Penalidade por abandono (ELO, banimento, contagem).
- Reconectar-se a uma partida abandonada.

## 3. Dependências
- **Implementações necessárias:** SPEC-0044 (rótulos e estado da série na arena e na `RematchBar`) e SPEC-0045 (gravação única do resultado, para o abandono ser registrado uma vez).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** `GameSession` compartilhada pelos dois circuitos (`ConcurrentDictionary<Guid, GameSession>`), eventos `OnStateChanged` (SPEC-0018), `Restart()` público e sem consentimento (SPEC-0012), `EndReason.Abandon` reservado na SPEC-0036.

**Decisão:** o protocolo de saída e de revanche vive em `GameSession` (estado sob o `lock` existente) e é sinalizado pelo mesmo evento; a UI só apresenta e dispara comandos (`Leave`, `RequestRematch`, `AcceptRematch`, `DeclineRematch`). `Restart()` continua existindo para o fluxo interno e para o solo.

**Justificativa:** o estado é compartilhado pelos dois jogadores e precisa ser atômico; o padrão de eventos já sincroniza as duas telas.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** canal de mensagens separado para a revanche (infraestrutura nova sem necessidade); manter `Restart` livre com confirmação só na UI (inseguro: outro cliente ainda poderia reiniciar).

**ADRs:** ADR-0008 (primitivos de UI).

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** sem custo relevante; um evento de estado por comando.
- **Segurança:** os comandos validam no servidor quem está agindo (`Player`) e o estado da partida: só o dono da vez de responder aceita/recusa, e só quem ainda está na partida abandona ou pede; comandos inválidos retornam falso e não alteram o estado.
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** comandos concorrentes (dois aceites, abandono simultâneo, abandono durante pedido de revanche) resultam em um único efeito; o pedido expira por tempo para não travar a tela.
- **Acessibilidade (UI):** botões com rótulo; confirmação de abandono anunciada (`aria-live`) e operável por teclado; avisos do oponente em região `aria-live="polite"`; foco vai para o botão de confirmação; contraste AA; Lighthouse ≥ 90.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** comandos de `GameSession`, `RematchBar`, `ArenaActions` e `Home`.

```text
enum RematchState { None, Requested, Declined, Expired }

GameSession
  LeaveResult Leave(Player player)
    // partida em andamento (não solo)  → vitória do oponente, EndReason = Abandon; em série, IsSeriesOver = true e SeriesWinner = oponente
    // partida em andamento (solo)       → LeaveResult.Discarded (nenhum resultado)
    // partida encerrada                 → marca o jogador como ausente
  bool HasLeft(Player player)

  bool RequestRematch(Player player)     // encerrada, oponente presente; solo reinicia na hora; requisição do outro lado = aceite automático
  bool AcceptRematch(Player player)      // só quem recebeu o pedido; reinicia (nova partida ou nova série)
  bool DeclineRematch(Player player)     // só quem recebeu; RematchState = Declined
  RematchState RematchState { get; }  Player? RematchRequestedBy { get; }
  // o pedido expira em 30 s (RematchState = Expired) via o Tick existente
  enum LeaveResult { Forfeited, Discarded, Left, Rejected }

RematchBar   [Parameter] RematchKind Kind (Rematch|NextRound|NewSeries) · RematchState State · string? RequesterName · bool OpponentLeft
             · EventCallback OnRequest/OnAccept/OnDecline/OnNextRound/OnBackToLobby
ArenaActions [Parameter] bool InProgress · EventCallback OnLeave       // Abandonar com confirmação em duas etapas

Textos
  "Abandonar" → "Confirmar abandono?" [Confirmar] [Cancelar]
  "Pedir revanche" · "Aguardando resposta…" · "{nome} pediu revanche" [Aceitar] [Recusar] · "Oponente recusou a revanche"
  "Oponente abandonou. Vitória por W.O." · "Oponente saiu da partida" · "Voltar ao lobby"
```

**Arquivos/módulos afetados:** ver `touches`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Abandono online | Partida em andamento | Oponente vence; `EndReason=Abandon`; resultado gravado uma vez | UT-01, IT-01 |
| Abandono em série | Série em andamento | Série encerrada com vitória do oponente | UT-01 |
| Abandono solo | Partida solo | Volta ao lobby, nenhum resultado gravado | UT-02 |
| Sair após o fim | Partida encerrada | Jogador marcado como ausente; oponente vê "Oponente saiu da partida" | UT-03 |
| Pedido de revanche | Partida encerrada, oponente presente | Estado `Requested`; oponente vê o pedido | UT-04 |
| Aceite | Oponente aceita | Nova partida/série iniciada uma única vez | UT-04, UT-05 |
| Recusa e expiração | Recusa / 30 s sem resposta | `Declined` / `Expired`; quem pediu é avisado | UT-04 |
| Pedidos simultâneos | Os dois pedem | Aceite automático | UT-04 |
| Comandos inválidos | Fora do estado/da vez | Retornam falso; nada muda | UT-05 |
| Solo e série | Solo / rodada encerrada em série | Reinício imediato / próxima rodada imediata | UT-06 |
| Barra de ações | Todos os estados | Rótulos e botões conforme o contrato | UT-07 |
| Confirmação de abandono | Clique em Abandonar | Duas etapas; cancelar não abandona; teclado | UT-08 |
| Avisos ao oponente | Abandono / saída / recusa | Textos com `aria-live` | UT-09 |
| Voltar ao lobby | Partida encerrada | Retorna ao lobby e restaura o shell | UT-10, IT-01 |
| Duas telas | Dois `Home` na mesma partida | Fluxos de abandono e de revanche sincronizados | IT-01, IT-02 |
| Jornada | Terminar, pedir/aceitar revanche, abandonar | Fluxo completo | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado `GameSession`, então `Restart()` continua reiniciando a partida e zerando tabuleiro, vencedor e timer como hoje (guarda: passa antes da mudança; `ModuleTests SPEC-0012` já cobre e permanece verde).

**Testes existentes afetados:** `ArenaCyberArenaTests` (o botão de revanche deixa de reiniciar direto em partidas online/privadas). Ajuste justificado pela mudança de regra (aceite do oponente, Stitch).

### 7.2 Testes Unitários
- **UT-01** — Dado uma partida online em andamento e depois uma série em andamento, quando um jogador chama `Leave`, então o oponente vence com `EndReason=Abandon` (e `IsSeriesOver` com `SeriesWinner`=oponente na série); o resultado retornado é `Forfeited`.
- **UT-02** — Dado uma partida solo em andamento, quando o jogador chama `Leave`, então retorna `Discarded` e a sessão não produz resultado gravável.
- **UT-03** — Dado uma partida encerrada, quando um jogador chama `Leave`, então `HasLeft` é verdadeiro para ele e o outro passa a ver a revanche indisponível.
- **UT-04** — Dado uma partida encerrada, então: `RequestRematch(X)` deixa `Requested` por X; `AcceptRematch(O)` reinicia; `DeclineRematch(O)` deixa `Declined`; `RequestRematch` dos dois lados aceita automaticamente; após 30 s de `Tick` sem resposta o estado é `Expired`.
- **UT-05** — Dado dois `AcceptRematch` simultâneos e comandos fora de estado (partida em andamento, jogador errado, oponente ausente), então reinicia uma única vez e os inválidos retornam falso sem mudar o estado.
- **UT-06** — Dado uma partida solo encerrada e uma rodada de série encerrada, então `RequestRematch` (solo) e `Restart` (próxima rodada) agem imediatamente, sem consentimento.
- **UT-07** — Dado `RematchBar` em cada combinação de `Kind`, `State` e `OpponentLeft`, então os rótulos e botões são os do contrato e cada clique dispara o callback certo.
- **UT-08** — Dado `ArenaActions` em andamento, quando clica em "Abandonar", então aparece "Confirmar abandono?" com "Confirmar" e "Cancelar"; cancelar restaura; confirmar dispara `OnLeave`; operável por Enter/Espaço/Esc.
- **UT-09** — Dado abandono do oponente, saída do oponente e revanche recusada, então os avisos aparecem em região `aria-live="polite"` com o texto do contrato.
- **UT-10** — Dado uma partida encerrada, então "Voltar ao lobby" dispara `OnBackToLobby`.

### 7.3 Testes de Integração
- **IT-01** — Dado dois `Home` no bUnit ligados à mesma partida online, quando um abandona, então ele volta ao lobby com o shell restaurado, o outro vê "Oponente abandonou. Vitória por W.O.", e **uma única** linha é gravada com `EndReason=Abandon` e o vencedor certo.
- **IT-02** — Dado dois `Home` na mesma partida encerrada, quando um pede revanche, então o outro vê o pedido; ao aceitar, ambos veem a partida nova; ao recusar, quem pediu vê "Oponente recusou a revanche".

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
- **E2E-01** — Jornada (bUnit, dois jogadores): terminar uma partida, pedir e aceitar revanche, jogar até o meio e abandonar; o abandono aparece como vitória por W.O. para o outro.

### 7.6 Outros
- Revisão visual (H2): estados da barra de ações e avisos em 390px e 1280px.
- Concorrência: teste de corrida com duas tarefas chamando `AcceptRematch`/`Leave` juntas (parte do `UT-05`).

**Dublês e dados de teste:** `TimeProvider` de teste, `GameSession(enableBackgroundTimer: false)`, dois `Home` no mesmo `ConcurrentDictionary`, EF InMemory.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto.
- **Dados/schema:** N/A — `EndReason.Abandon` já existe (SPEC-0036).
- **Compatibilidade:** `Restart()` permanece; partidas solo e próximas rodadas de série não mudam de comportamento.
- **Observabilidade:** log de informação de abandono e de revanche (id da partida, lado).
- **Rollback:** `git revert` do PR.
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
- [x] **Abandonar em partida solo:** sair sem registrar resultado (recomendado: não há adversário humano a beneficiar) ou registrar como derrota? — Sair sem registrar resultado. (thomas, 2026-09-29)
- [x] **Abandonar dentro de uma série:** encerra a série inteira com vitória do oponente (recomendado) ou só a rodada atual? — Encerra a série inteira com vitória do oponente. (thomas, 2026-09-29)
- [x] **Validade do pedido de revanche:** expira em **30 s** (recomendado) ou fica pendente até alguém sair? — Expira em 30 s. (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [ ] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [ ] Escrever os testes `SPEC-0041:CH-01`, `SPEC-0041:UT-01`, `SPEC-0041:UT-02`, `SPEC-0041:UT-03`, `SPEC-0041:UT-04`, `SPEC-0041:UT-05`, `SPEC-0041:UT-06`, `SPEC-0041:UT-07`, `SPEC-0041:UT-08`, `SPEC-0041:UT-09`, `SPEC-0041:UT-10`, `SPEC-0041:IT-01`, `SPEC-0041:IT-02`, `SPEC-0041:E2E-01` com a tag `SPEC-0041:<ID>`, em commits `test(...)` com `Refs: SPEC-0041`, tocando só `test_paths`
- [ ] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [ ] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0041`)

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [ ] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [ ] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [ ] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [ ] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável
- [ ] PR com `spec_graph.py pr SPEC-0041`, CI verde (G5) e aprovação do merge (H2)
- [ ] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
| G1 Red | PENDING | | |
| G2 Green | PENDING | | |
| G3 Arquitetura | PENDING | | |
| G4 Review | PENDING | | |
| G5 Integração & CI | PENDING | | |
| H2 Integração aprovada | PENDING | | |
| G6 Deploy | PENDING | | |
| G7 Pronto & Docs | PENDING | | |

## 13. Registro de Impedimentos
<!-- Toda parada é registrada pelo Architect com `spec_graph.py impede` e fechada com `resolve` — não edite à mão. Tipos: spec (spec errada/incompleta → resolve com Emenda) | decisão (só o humano decide → resposta ou ADR) | trabalho (falta algo que exige código → SPEC-NNNN nova) | externo (acesso, ambiente, terceiro → ação tomada) | falha (3 FAILs seguidos no mesmo gate → diagnóstico e decisão). Com impedimento aberto a spec aparece como parada no INDEX e não pode ser fechada. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega
<!-- Preenchido no CLOSE (G7). Diz o que foi feito, como, e prova que foi resolvido. Para status implemented o validate exige todas as subseções preenchidas, todo teste do plano com PASS + evidência e a Definição de Pronto toda marcada. -->

### O que foi entregue
<!-- comportamento entregue do ponto de vista do usuário/sistema -->

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->
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
<!-- ambiente(s), versão/tag, data, estratégia, estado da feature flag, execução do pipeline -->

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0041`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
