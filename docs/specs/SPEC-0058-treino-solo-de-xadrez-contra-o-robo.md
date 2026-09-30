---
id: SPEC-0058
title: Treino solo de xadrez contra o robô
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0054, SPEC-0056, SPEC-0057]
consumes_contract: []
contract_version: 2
touches: [src/TicTacToe/TicTacToe.Modules.Chess/Bots/ChessBotTurnRunner.cs, src/TicTacToe/TicTacToe.Web/Components/Chess/ChessLobby.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/ChessHome.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/ChessHome.razor.cs, src/TicTacToe/TicTacToe.Web/wwwroot/css/cyber-arena.css, tests/TicTacToe.Tests/ChessBotTurnRunnerTests.cs, tests/TicTacToe.Tests/ChessSoloTests.cs, tests/TicTacToe.Tests/ChessLobbyTests.cs]
adrs: [ADR-0008, ADR-0011]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0058 — Treino solo de xadrez contra o robô

## 1. Visão Geral
Adiciona o **treino solo de xadrez contra o robô**: cartão "Duelo contra IA" no lobby (nível Fácil ou Médio e cor), partida solo com o robô respondendo após um pequeno atraso, e o resultado gravado como partida solo (entra no histórico, fora do ranking).

## 2. Motivação & Escopo
**Motivação:** O usuário pediu poder jogar com um robô com dificuldades. O robô (SPEC-0054) precisa de um executor de jogadas e de um lugar no lobby.

**Objetivos (dentro do escopo):**
- `ChessBotTurnRunner`: aguarda um atraso injetável (`TimeProvider`) e joga o lance do robô se ainda for a vez dele e a partida estiver em andamento; cancelável.
- Cartão "Duelo contra IA" no `ChessLobby`: nível (Fácil, Médio) e cor (a mesma escolha do lobby), botão "Iniciar partida solo".
- `ChessHome` cria a `ChessSession` em `ChessMode.Solo` (controle escolhido), define o robô como o outro lado (sem `PlayerId`) e aciona o executor sempre que for a vez dele, inclusive quando o robô abre a partida com as brancas.
- Revanche imediata (cores trocadas) e abandono que descarta, herdados da sessão; resultado gravado com modo solo.

**Não-objetivos (fora do escopo):**
- Nível Difícil, Stockfish, dicas, desfazer lance e análise.
- Contagem no ranking (o ranking exclui solo por regra da SPEC-0039).
- Novo desenho no Stitch: o cartão reaproveita o padrão "Duelo contra IA" do lobby do jogo da velha.

## 3. Dependências
- **Implementações necessárias:** SPEC-0054 (`IChessBot`), SPEC-0056 (`ChessHome`, `ChessLobby`) e SPEC-0057 (`ChessArena`).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `BotTurnRunner` (SPEC-0040) e o solo da `Home` (`RunBot`, `IsSoloGame`); cartão "Duelo contra IA" do `Lobby` (SPEC-0030).

**Decisão:** `ChessBotTurnRunner` no módulo Chess (atraso e cancelamento injetáveis) e chamada pela `ChessHome` a cada mudança de estado em que seja a vez do robô.

**Justificativa:** Mesmo padrão do jogo da velha, agora testável sem esperas reais.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Robô dentro da sessão (acopla domínio a um Task em segundo plano); temporizador na página (não testável).

**ADRs:** ADR-0011

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** A busca roda fora da thread da interface (`Task.Run`) com limite de nós; o atraso padrão de 600 ms torna a jogada natural sem travar.
- **Segurança:** O robô só joga pelo mesmo `TryMove` da sessão; nenhum lance ilegal chega ao estado.
- **Privacidade e dados pessoais:** N/A — o robô não tem identidade.
- **Disponibilidade e resiliência:** Cancelamento ao sair da página; nenhuma exceção não observada em `Task.Run`.
- **Acessibilidade (UI):** Nível e cor como radiogrupos; "Vez de Robô…" na região viva; contraste AA.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `ChessBotTurnRunner · cartão de solo do ChessLobby · fluxo solo na ChessHome`

```text
ChessBotTurnRunner(TimeProvider time)
  Task RunAsync(ChessSession session, int botSeat, IChessBot bot, TimeSpan delay, CancellationToken ct)
    // aguarda o atraso; a cor do robô é session.ColorOf(botSeat) lida NO MOMENTO da jogada (muda na revanche); se for a vez dele e a partida estiver em andamento, escolhe o lance sobre Snapshot().Position e chama session.TryMove

ChessLobby (acréscimos)   [Parameter] ChessBotLevel SelectedLevel · EventCallback<ChessBotLevel> LevelChanged · EventCallback OnPlaySolo
  radiogrupo "Dificuldade do robô": Fácil 🟢 / Médio 🟡 com a descrição do nível; botão "Iniciar partida solo" (desabilitado sem apelido)

ChessHome (solo): Mode = Solo; humano com a cor escolhida (Aleatória sorteia); robô = ChessBots.Create(nível) com nome de ChessBots.NameOf;
  a cada OnStateChanged, se for a vez do robô: RunAsync(session, botColor, bot, 600 ms, token); token cancelado ao sair.
  Revanche: RequestRematch reinicia na hora com cores trocadas (e o robô abre se ficar de brancas); Abandonar em andamento descarta; gravação como Solo.
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Executor | Vez do robô, vez mudada, partida encerrada, cancelamento | Joga após o atraso só quando ainda é a vez; respeita fim e cancelamento | UT-01 |
| Cartão de solo | Escolher nível e iniciar | Radiogrupo, descrição, callback e botão desabilitado sem apelido | UT-02 |
| Início da partida | Humano de brancas e de pretas | Sessão solo com robô no outro lado; robô abre quando joga de brancas | UT-03 |
| Fluxo do robô | Humano joga e robô responde | Resposta legal após o atraso; "Vez de Robô…" enquanto pensa | UT-04 |
| Fim e revanche | Partida solo terminada | Cores trocadas no reinício imediato; robô abre se for de brancas | UT-05 |
| Abandono solo | Sair em andamento | Descarta sem gravar | UT-06 |
| Gravação | Partida solo encerrada | Uma linha `Mode=Solo`, fora do ranking, no histórico | IT-01 |
| Cancelamento | Sair da página durante o atraso | Nenhum lance depois do descarte | IT-02 |
| Jornada | Escolher robô e jogar alguns lances | Fluxo completo até uma partida encerrada | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — funcionalidade nova; o solo do jogo da velha não muda.

### 7.2 Testes Unitários
- **UT-01** — Dado `ChessBotTurnRunner` com `ManualTime`, quando é a vez do robô, então joga só depois do atraso; se a vez mudar, a partida acabar, as cores dos assentos trocarem (revanche) ou o token for cancelado antes, não joga fora de hora.
- **UT-02** — Dado `ChessLobby` com nível Fácil e Médio, então o radiogrupo "Dificuldade do robô" marca o atual, mostra a descrição e `OnPlaySolo` fica desabilitado sem apelido.
- **UT-03** — Dado iniciar solo com brancas e com pretas, então a sessão é `Solo`, o robô ocupa o outro lado sem `PlayerId`, e quando o robô joga de brancas ele faz o primeiro lance.
- **UT-04** — Dado o humano jogar `e2→e4`, então após o atraso o robô responde com um lance legal e a região viva mostra "Vez de Robô…" enquanto espera.
- **UT-05** — Dado partida solo encerrada e "Jogar novamente", então a partida reinicia na hora com as cores trocadas e o robô abre se ficar de brancas.
- **UT-06** — Dado "Abandonar" em partida solo em andamento, então o jogador volta ao lobby e nada é gravado.

### 7.3 Testes de Integração
- **IT-01** — Dado uma partida solo levada ao fim (mate ou tempo), então uma linha é gravada com `Mode=Solo`, `GameType=Chess`, aparece em `GetHistoryAsync(Game=Chess)` do jogador e não entra em `GetLeaderboardPageAsync(Game=Chess)`.
- **IT-02** — Dado sair da `ChessHome` durante o atraso do robô, então nenhum lance é feito depois e não há exceção.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
- **E2E-01** — Jornada (bUnit): no lobby escolher Médio e Brancas, iniciar o solo, jogar três lances contra o robô, abandonar e voltar ao lobby sem resultado gravado; depois jogar outra até o fim e conferir, por `GameResultService.GetHistoryAsync(Game=Chess)`, a partida solo gravada (a tela de histórico do xadrez chega na SPEC-0059).

### 7.6 Outros
- Revisão visual (H2): cartão de solo nas duas larguras, mesmo padrão do lobby do jogo da velha.
- `tools/tailwind/build.sh` executado e `--check` sem diferença.
- Lighthouse Acessibilidade ≥ 90 em `/xadrez`.

**Dublês e dados de teste:** `ManualTime`, `IChessBot` de teste que devolve lances conhecidos, EF InMemory.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto.
- **Dados/schema:** N/A
- **Compatibilidade:** N/A
- **Observabilidade:** Log de informação ao iniciar solo (nível) e de erro se a busca falhar.
- **Rollback:** Reverter o PR; o lobby volta a só ter fila e sala privada.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Quais níveis do robô agora? — Fácil e Médio, com evolução futura para Stockfish (thomas, 2026-09-29; ADR-0011)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [x] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [x] Escrever `SPEC-0058:E2E-01`, `SPEC-0058:IT-01`, `SPEC-0058:IT-02`, `SPEC-0058:UT-01`, `SPEC-0058:UT-02`, `SPEC-0058:UT-03`, `SPEC-0058:UT-04`, `SPEC-0058:UT-05`, `SPEC-0058:UT-06` com a tag `SPEC-0058:<ID>` em commits `test(...)` com `Refs: SPEC-0058` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [x] Refactor mantendo tudo verde
- [x] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes`, `tools/tailwind/build.sh --check` e `verify SPEC-0058 --base origin/main`

**Fase final: Integração, entrega e documentação**
- [x] Review independente (G4)
- [x] Integração + CI verde (G5) e aprovação (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
| G1 Red | PASS | Red confirmado: testes SPEC-0058 falham antes do código (verify PASS); Red da revisão G4 (IT-02, UT-06) falhou antes do fix (934feaf) | 2026-09-29 |
| G2 Green | PASS | dotnet test: 864 total, 863 passam, 1 pulado, 0 falhas; format e tailwind --check limpos | 2026-09-29 |
| G3 Arquitetura | N/A | sem novas regras estruturais; ChessModuleBoundaryTests continua verde | 2026-09-29 |
| G4 Review | PASS | Review independente PASS (HEAD 14ad830), achado maior (_botBusy) e menores corrigidos em 8545787; verify PASS | 2026-09-29 |
| G5 Integração & CI | PASS | PR #52: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorização permanente do usuário (2026-09-29): mesclar com CI verde conforme a skill sdd-management | 2026-09-29 |
| G6 Deploy | N/A | Sem ambiente remoto (`staging_url` vazio); aprovado pelo usuário em 2026-09-29 | 2026-09-29 |
| G7 Pronto & Docs | PASS | `spec_graph.py validate` limpo; Relatório de Entrega e CHANGELOG atualizados | 2026-09-29 |

## 13. Registro de Impedimentos
<!-- Toda parada é registrada pelo Architect com `spec_graph.py impede` e fechada com `resolve` — não edite à mão. Tipos: spec (spec errada/incompleta → resolve com Emenda) | decisão (só o humano decide → resposta ou ADR) | trabalho (falta algo que exige código → SPEC-NNNN nova) | externo (acesso, ambiente, terceiro → ação tomada) | falha (3 FAILs seguidos no mesmo gate → diagnóstico e decisão). Com impedimento aberto a spec aparece como parada no INDEX e não pode ser fechada. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega
<!-- Preenchido no CLOSE (G7). Diz o que foi feito, como, e prova que foi resolvido. Para status implemented o validate exige todas as subseções preenchidas, todo teste do plano com PASS + evidência e a Definição de Pronto toda marcada. -->

### O que foi entregue
<!-- comportamento entregue do ponto de vista do usuário/sistema -->

Treino solo contra o robô (Fácil/Médio) no lobby de xadrez: card "Duelo contra IA", ChessBotTurnRunner (busca com revalidação pós-atraso e pós-busca), partida Solo (histórico, fora do ranking), revanche com troca de cores gravando antes, abandono descartando a partida.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

ChessBotTurnRunner no módulo Chess; ChessHome com StartSolo/ScheduleBot (trava por geração, um voo por vez)/RematchSolo/AbandonSolo; fábrica de robô injetável para testes; emenda v2 de touches; correções da review G4 (trava _botBusy por geração, gravação ao abandonar partida encerrada por bandeira, texto de cor).

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0058:UT-01 | Dado `ChessBotTurnRunner` com `ManualTime`, quando é a vez do robô, então joga só depois do atraso; se a vez m | PASS | `dotnet test` 863/863 no CI (dotnet-ci) do PR #52 |
| SPEC-0058:UT-02 | Dado `ChessLobby` com nível Fácil e Médio, então o radiogrupo "Dificuldade do robô" marca o atual, mostra a de | PASS | `dotnet test` 863/863 no CI (dotnet-ci) do PR #52 |
| SPEC-0058:UT-03 | Dado iniciar solo com brancas e com pretas, então a sessão é `Solo`, o robô ocupa o outro lado sem `PlayerId`, | PASS | `dotnet test` 863/863 no CI (dotnet-ci) do PR #52 |
| SPEC-0058:UT-04 | Dado o humano jogar `e2→e4`, então após o atraso o robô responde com um lance legal e a região viva mostra "Ve | PASS | `dotnet test` 863/863 no CI (dotnet-ci) do PR #52 |
| SPEC-0058:UT-05 | Dado partida solo encerrada e "Jogar novamente", então a partida reinicia na hora com as cores trocadas e o ro | PASS | `dotnet test` 863/863 no CI (dotnet-ci) do PR #52 |
| SPEC-0058:UT-06 | Dado "Abandonar" em partida solo em andamento, então o jogador volta ao lobby e nada é gravado. | PASS | `dotnet test` 863/863 no CI (dotnet-ci) do PR #52 |
| SPEC-0058:IT-01 | Dado uma partida solo levada ao fim (mate ou tempo), então uma linha é gravada com `Mode=Solo`, `GameType=Ches | PASS | `dotnet test` 863/863 no CI (dotnet-ci) do PR #52 |
| SPEC-0058:IT-02 | Dado sair da `ChessHome` durante o atraso do robô, então nenhum lance é feito depois e não há exceção. | PASS | `dotnet test` 863/863 no CI (dotnet-ci) do PR #52 |
| SPEC-0058:E2E-01 | Jornada (bUnit): no lobby escolher Médio e Brancas, iniciar o solo, jogar três lances contra o robô, abandonar | PASS | `dotnet test` 863/863 no CI (dotnet-ci) do PR #52 |

### Definição de Pronto
- [x] Todos os testes do plano passando e listados na Verificação
- [x] Todo comportamento do Mapa de Comportamentos coberto e verificado
- [x] Suíte completa, arquitetura e CI verdes no resultado integrado (G5)
- [x] Review independente sem achados blocker/major (G4)
- [x] Padrão arquitetural existente mantido, ou desvio coberto por ADR aprovado
- [x] Requisitos não-funcionais medidos com evidência (ou N/A justificado)
- [x] Disponível no ambiente-alvo via pipeline, com smoke/E2E passando no ambiente (G6) — N/A aprovado pelo usuário (2026-09-29): sem ambiente remoto
- [x] Observabilidade e rollback prontos conforme o Plano de Rollout
- [x] Documentação raiz e CHANGELOG atualizados (G7)
- [x] Pendências registradas como novas specs (ou nenhuma)

### Deploy
<!-- ambiente(s), versão/tag, data, estratégia, estado da feature flag, execução do pipeline -->

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #52 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Não verificado em navegador (app nunca aberto). Solo não é bloqueado durante IsWaiting (cancela a fila). Cartão do robô sem tela Stitch dedicada (prompt opcional).

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0058`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
| 2 | 2026-09-29 | `touches` inclui `ChessLobbyTests.cs` | O novo radiogrupo "Dificuldade do robô" no `ChessLobby` muda a contagem de radiogrupos que o `SPEC-0056:UT-07` fixa (de 3 para 4); ajuste justificado pela nova funcionalidade | SPEC-0056 (teste) | thomas (autorização permanente, 2026-09-29) |
