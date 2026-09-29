---
id: SPEC-0060
title: Abandono, revanche e desconexão no xadrez
tier: full
type: feature
user_facing: true
status: approved
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0056, SPEC-0057, SPEC-0058, SPEC-0061]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Chess/ChessArena.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/ChessHome.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/ChessHome.razor.cs, src/TicTacToe/TicTacToe.Web/Services/Presence/**, src/TicTacToe/TicTacToe.Web/wwwroot/css/cyber-arena.css, tests/TicTacToe.Tests/ChessLeaveAndRematchTests.cs, tests/TicTacToe.Tests/ChessDisconnectTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0060 — Abandono, revanche e desconexão no xadrez

## 1. Visão Geral
Liga à arena de xadrez os fluxos de **abandonar** (com confirmação), **voltar ao lobby**, **revanche com aceite** (cores trocadas) e **W.O. por desconexão** com aviso e contagem, reaproveitando os componentes do jogo da velha (`ArenaActions`, `RematchBar`) e a presença por circuito.

## 2. Motivação & Escopo
**Motivação:** A sessão de xadrez já implementa esses comandos (SPEC-0052); falta apresentá-los, como no jogo da velha (SPEC-0041 e 0042).

**Objetivos (dentro do escopo):**
- `ChessArena` mostra `ArenaActions` (Abandonar com confirmação em duas etapas) durante a partida e o `RematchBar` com consentimento no fim ("Pedir revanche", "Aceitar", "Recusar", estados de recusa, expiração e oponente ausente, "Voltar ao lobby").
- `ChessHome` trata `Leave`, revanche e volta ao lobby (remove a sessão quando ninguém mais a usa), grava o resultado do abandono uma vez.
- Avisos: "Oponente abandonou. Vitória por W.O.", "Oponente desconectado. Aguardando reconexão…" com contagem à parte e "Oponente desconectou. Vitória por W.O.".
- Presença: `MatchPresenceContext` aceita também sessões de xadrez (um relator de conexão) e a `ChessHome` associa e desassocia o jogador; sair da tela conta como queda.

**Não-objetivos (fora do escopo):**
- Proposta de empate; revanche em partida solo (imediata, SPEC-0058).
- Mudança no comportamento do jogo da velha (só a generalização do contexto de presença, mantendo a API atual).

## 3. Dependências
- **Implementações necessárias:** SPEC-0056 (`ChessHome`), SPEC-0057 (`ChessArena`), SPEC-0058 (mesmo arquivo `ChessHome`; o robô muda de lado na revanche) e SPEC-0061 (comandos de abandono, revanche e presença no domínio).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `ArenaActions`, `RematchBar` (com `Consent`) e `MatchPresenceContext`/`MatchPresenceCircuitHandler` (SPEC-0041/0042) e o fluxo na `Home` (`LeaveGame`, `BackToLobby`, `ReturnToLobby`).

**Decisão:** Reutilizar `ArenaActions` e `RematchBar` (mapeando `ChessRematchState` para o estado do componente) e estender o contexto de presença com um relator genérico (`Attach(Action<bool>)`), mantendo `Attach(GameSession, Player)`.

**Justificativa:** Evita duplicar componentes e a lógica de presença; a extensão é compatível com o que existe.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Duplicar os componentes para xadrez; um segundo circuito handler só para xadrez (dois pontos de verdade de presença).

**ADRs:** ADR-0008

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Um evento de estado por comando; contagem de desconexão atualizada pelo `Tick` de 1 s.
- **Segurança:** Comandos validados no servidor pela sessão (quem age, estado); o jogador só age pelo seu lado.
- **Privacidade e dados pessoais:** N/A — sem dado novo.
- **Disponibilidade e resiliência:** Falha ao gravar o abandono não prende o jogador na arena (volta ao lobby de qualquer jeito); presença idempotente entre `Dispose` e fechamento do circuito.
- **Acessibilidade (UI):** Confirmação de abandono anunciada e operável por teclado (Esc cancela, foco volta ao botão); avisos em regiões `aria-live` estáticas (contagem fora da região viva); contraste AA.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `ChessArena (ações e revanche) · ChessHome (Leave, revanche, retorno) · MatchPresenceContext.Attach(Action<bool>)`

```text
ChessArena (acréscimos)   <ArenaActions InProgress OnLeave/> enquanto a partida corre; <RematchBar Finished Kind="Rematch" Consent="true" State RequestedByMe RequesterName OpponentLeft
                            OnRequest OnAccept OnDecline OnBackToLobby/> ao fim (em solo Consent=false e revanche imediata)
  mapeamento ChessRematchState → RematchState do componente: None, Requested, Declined, Expired
  depois de uma revanche as cores trocam: MyColor = Session.ColorOf(MySeat) é relida a cada estado, o tabuleiro vira, e Leave/SetConnection/Request/Accept usam SEMPRE a cor corrente do assento
  avisos (região aria-live): "Oponente abandonou. Vitória por W.O." · "Você abandonou a partida" · "Oponente desconectou. Vitória por W.O." · "Você foi desconectado. Derrota por W.O."
  região do aviso de queda: "Oponente desconectado. Aguardando reconexão…" (estática); contagem "{n}s" em elemento à parte com aria-hidden

ChessHome   LeaveGame(): Leave(MyColor); Forfeited → recorder.SaveOnceAsync; sempre ReturnToLobby()
            RequestRematch / AcceptRematch / DeclineRematch → comandos da sessão; BackToLobby(): Leave + ReturnToLobby()
            ReturnToLobby(): solta o evento, Detach da presença, remove e descarta a sessão quando os dois saíram (ou solo)
            Dispose: SetConnection(MyColor, false) em partida humana em andamento; Detach

MatchPresenceContext   void Attach(Action<bool> reportConnection)   // além de Attach(GameSession, Player); Detach limpa os dois; o handler chama o relator
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Abandonar | Clique, confirmar, cancelar, Esc | Duas etapas; cancelar devolve o foco ao botão; confirmar chama `Leave` | UT-01 |
| Revanche na barra | Estados none, pedido, recebido, recusada, expirada, oponente saiu | Rótulos e botões do contrato; solo mantém o botão imediato | UT-02 |
| Avisos | Abandono, saída, desconexão e W.O. | Textos nas regiões vivas; contagem fora da região | UT-03 |
| Presença genérica | Relator anexado, conexão cai e volta, circuito fecha | Chama o relator; sem partida não faz nada; API antiga inalterada | UT-04 |
| Voltar ao lobby | Fim de partida e sair | Retorna ao lobby; sessão removida quando ninguém mais a usa | UT-05 |
| Abandono em duas telas | Um abandona | Ele volta ao lobby; o outro vê W.O.; uma linha `Abandon` gravada | IT-01 |
| Revanche em duas telas | Pedir, recusar, pedir de novo, aceitar | Estados sincronizados; cores trocadas na nova partida | IT-02 |
| Desconexão em duas telas | Um circuito cai; tempo passa; ou volta em 5 s | Aviso, depois W.O. `Disconnect` gravado uma vez; retorno cancela | IT-03 |
| Jornada | Terminar, revanche, jogar, abandonar; queda e W.O. | Fluxos completos | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — funcionalidade nova no xadrez; jogo da velha coberto pelas suas specs.

### 7.2 Testes Unitários
- **UT-01** — Dado `ChessArena` com a partida em andamento, então "Abandonar" abre a confirmação, Cancelar/Esc fecham e devolvem o foco, e Confirmar chama `Leave` do jogador.
- **UT-02** — Dado uma partida encerrada humana, então a barra mostra "Pedir revanche", "Aguardando resposta…", "{nome} pediu revanche" com Aceitar/Recusar, "Oponente recusou a revanche", "O pedido de revanche expirou" e "Oponente saiu da partida" conforme o estado; em solo o botão reinicia na hora.
- **UT-03** — Dado abandono do oponente, desconexão em contagem e W.O. por desconexão, então os textos do contrato aparecem nas regiões `aria-live` e a contagem fica em elemento `aria-hidden` fora da região.
- **UT-04** — Dado `MatchPresenceContext.Attach(Action<bool>)` e o handler, então queda e retorno chamam o relator com `false`/`true`, o fechamento do circuito chama `false` e solta o contexto, e sem relator nada acontece; a API `Attach(GameSession, Player)` continua igual.
- **UT-05** — Dado "Voltar ao lobby" e "Abandonar", então a `ChessHome` volta ao lobby, restaura o shell, e a sessão só é removida quando os dois saíram (ou é solo).

### 7.3 Testes de Integração
- **IT-01** — Dado dois `ChessHome` na mesma partida, quando um abandona, então volta ao lobby, o outro vê "Oponente abandonou. Vitória por W.O." e uma única linha `Abandon` é gravada com o vencedor certo.
- **IT-02** — Dado dois `ChessHome` na mesma partida encerrada, quando um pede revanche, o outro recusa e depois pede, e o primeiro aceita, então os estados aparecem nas duas telas e a nova partida começa com as cores trocadas e **o tabuleiro de cada tela vira** para a nova cor; um abandono logo depois é atribuído ao jogador certo.
- **IT-03** — Dado dois `ChessHome` na mesma partida, quando um circuito "cai" e o relógio avança 15 s, então o outro vê o aviso e depois a vitória por W.O. com uma única linha `Disconnect`; se volta em 5 s, o aviso some e a partida continua.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
- **E2E-01** — Jornada (bUnit, dois jogadores): terminar uma partida, pedir e aceitar a revanche, jogar um lance e abandonar (o outro ganha por W.O.); em outra partida, um circuito cai e o outro ganha por W.O. de desconexão, e o histórico exibe "Desconexão do oponente".

### 7.6 Outros
- Verificação manual em navegador real (fechar a aba e recarregar durante a partida; W.O. em ~15 s), registrada no PR quando houver ambiente.
- Revisão visual (H2): estados da barra e avisos em 390px e 1280px.
- `tools/tailwind/build.sh` executado e `--check` sem diferença.

**Dublês e dados de teste:** `ManualTime`, dois `ChessHome` no mesmo contêiner, EF InMemory, `TimeProvider` registrado no DI dos testes.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto.
- **Dados/schema:** N/A
- **Compatibilidade:** `MatchPresenceContext` mantém `Attach(GameSession, Player)`; o jogo da velha não muda.
- **Observabilidade:** Logs de presença já existentes cobrem a queda; W.O. de desconexão do xadrez é registrado no mesmo padrão.
- **Rollback:** Reverter o PR; a arena volta a não ter abandono e revanche.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Empate por acordo? — Fora do escopo (thomas, 2026-09-29); por isso não há botão "Propor empate"
- - [x] Sair da tela conta como queda no xadrez também? — Sim, mesma regra do jogo da velha (Architect, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [ ] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [ ] Escrever `SPEC-0060:E2E-01`, `SPEC-0060:IT-01`, `SPEC-0060:IT-02`, `SPEC-0060:IT-03`, `SPEC-0060:UT-01`, `SPEC-0060:UT-02`, `SPEC-0060:UT-03`, `SPEC-0060:UT-04`, `SPEC-0060:UT-05` com a tag `SPEC-0060:<ID>` em commits `test(...)` com `Refs: SPEC-0060` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [ ] Refactor mantendo tudo verde
- [ ] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes`, `tools/tailwind/build.sh --check` e `verify SPEC-0060 --base origin/main`

**Fase final: Integração, entrega e documentação**
- [ ] Review independente (G4)
- [ ] Integração + CI verde (G5) e aprovação (H2)
- [ ] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0060`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
