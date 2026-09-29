---
id: SPEC-0056
title: Lobby de xadrez
tier: full
type: feature
user_facing: true
status: in-progress
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0053, SPEC-0057]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Pages/ChessHome.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/ChessHome.razor.cs, src/TicTacToe/TicTacToe.Web/Components/Chess/ChessLobby.razor, src/TicTacToe/TicTacToe.Web/wwwroot/css/cyber-arena.css, tests/TicTacToe.Tests/ChessLobbyTests.cs, tests/TicTacToe.Tests/ChessHomeTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0056 — Lobby de xadrez

## 1. Visão Geral
Entrega a página `/xadrez`: o **lobby de xadrez** (apelido, controle de tempo, cor, fila, sala privada) e a orquestração entre lobby, pareamento e arena — cria a `ChessSession` ao parear, associa os jogadores às cores, grava o resultado e volta ao lobby.

## 2. Motivação & Escopo
**Motivação:** O lobby é a porta de entrada do xadrez (`docs/design/stitch/chess/lobby-desktop.png`); sem ele a arena não é alcançável.

**Objetivos (dentro do escopo):**
- Página `/xadrez` (`ChessHome`, `@rendermode InteractiveServer`) e componente `ChessLobby` reaproveitando os primitivos do lobby do jogo da velha (`NeonInput`, `SegmentedControl`, `PillButton`, `StatusChip`).
- Apelido lembrado pela identidade anônima (SPEC-0037), controle de tempo (Bullet 1+0, Blitz 5+0, Rápida 10+5) com descrição do escolhido e cor (Brancas, Pretas, Aleatória) com descrição.
- "Procurar oponente": fila por `xadrez:{controle}` (preferência de cor registrada **antes** de entrar), estado "Na fila" com cancelar (`LeaveQueue`); "Sala privada": criar (código com copiar) e entrar com código, erro "Sala inválida ou já iniciada!".
- Ao parear: a sessão é criada **uma única vez** por `ChessMatchRegistry.GetOrCreate(matchId, …)` (sorteio de cores dentro do factory; nomes e `PlayerId` por assento); cada `ChessHome` descobre o próprio assento por `SeatOf(matchId, connectionId)` e renderiza `ChessArena MySeat`; modo imersivo do shell.
- Gravação do resultado por `ChessResultRecorder` ao fim da partida (uma vez), e volta ao lobby.
- Sair da página (descarte) cancela a busca ou a sala (`LeaveQueue`, `CancelPrivateRoom`) e solta as assinaturas.

**Não-objetivos (fora do escopo):**
- Modo solo contra o robô (SPEC-0058) e fluxos de abandono, revanche e desconexão (SPEC-0060).
- Estimativa de espera, contagem de jogadores online, rating e "Modo de combate" do mock.
- Alterar o lobby do jogo da velha.

## 3. Dependências
- **Implementações necessárias:** SPEC-0053 (fila por controle, cores, sessões e gravação) e SPEC-0057 (`ChessArena`).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `Home.razor(.cs)` + `Lobby.razor` (SPEC-0030/0037/0041): página que orquestra pareamento e partida, componente de lobby de apresentação, identidade via `PlayerIdentityService`, imersão via `ShellState`.

**Decisão:** Mesmo desenho: `ChessHome` orquestra e `ChessLobby` apresenta, com parâmetros e callbacks no estilo do `Lobby`.

**Justificativa:** Padrão já validado e revisado; a diferença é o controle de tempo e cor no lugar de dificuldade e formato.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Um único componente para os dois jogos (acoplaria regras diferentes); reaproveitar `Home` com ramos (aumentaria a complexidade do jogo da velha).

**ADRs:** ADR-0008

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Pareamento imediato em memória; a página não faz consulta ao banco para abrir.
- **Segurança:** Apelido limitado a 20 caracteres e escapado pelo Blazor; código da sala normalizado; o servidor decide as cores e o `PlayerId` vem da identidade, nunca do cliente.
- **Privacidade e dados pessoais:** Mesma nota de privacidade do lobby do jogo da velha; nenhum dado novo.
- **Disponibilidade e resiliência:** Falha do armazenamento do navegador não impede jogar (identidade só da sessão); saída da página solta as assinaturas de evento.
- **Acessibilidade (UI):** Controles como radiogrupos operáveis por teclado, campos rotulados, estados de fila e erro em regiões `aria-live`, contraste AA; Lighthouse ≥ 90 em `/xadrez`.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `rota /xadrez (ChessHome) e componente ChessLobby`

```text
ChessLobby (apresentação)
  [Parameter] string PlayerName · EventCallback<string> PlayerNameChanged · string? ReturningPlayerName
  [Parameter] TimeControl SelectedControl · EventCallback<TimeControl> ControlChanged          // SegmentedControl "Controle de tempo" (3)
  [Parameter] ColorPreference SelectedColor · EventCallback<ColorPreference> ColorChanged      // SegmentedControl "Sua cor" (Brancas, Pretas, Aleatória)
  [Parameter] bool IsWaiting · string? CreatedRoomCode · string InputRoomCode · EventCallback<string> InputRoomCodeChanged · string? RoomErrorMessage
  [Parameter] EventCallback OnPlayOnline · OnCancelSearch · OnCreateRoom · OnJoinRoom
  descrições: Bullet "1 minuto para cada jogador, sem acréscimo" · Blitz "5 minutos para cada jogador, sem acréscimo de tempo por lance"
              Rápida "10 minutos para cada jogador, com 5 segundos por lance"
              cor: "Sua preferência é atendida quando possível; se os dois pedirem a mesma cor, as cores são sorteadas" · Aleatória "O sorteio de cores ocorrerá automaticamente no início da partida"

ChessHome (/xadrez)  PageTitle "Xadrez · XO Arena"
  ordem: SetMatchPreference(conexão, preferência) ANTES de JoinQueue/CreatePrivateRoom/JoinPrivateRoom (o pareamento dispara dentro dessas chamadas)
  fila: matchmaking.JoinQueue(conexão, apelido, playerId, queueKey: "xadrez:{controle.Id}"); cancelar: LeaveQueue(conexão)
  sala privada: CreatePrivateRoom(..., queueKey) / JoinPrivateRoom(código, ..., game: "xadrez"); cancelar: CancelPrivateRoom
  ao parear: ChessMatchRegistry.GetOrCreate(matchId, factory) cria ChessSession(controle) uma vez, com SetSeat 0 e 1 (cores por ColorAssignment) e Mode Online|Private
  cada circuito: seat = registry.SeatOf(matchId, conexão); em partida: <ChessArena Session MySeat/>, ShellState.Set(true, "Partida de xadrez"); ao fim: ChessResultRecorder.SaveOnceAsync
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Apelido | Identidade com apelido salvo e sem | Campo preenchido com o apelido; chip de prontidão | UT-01 |
| Controle de tempo | Escolher Bullet, Blitz, Rápida | Radiogrupo marcado e descrição do escolhido | UT-02 |
| Cor | Escolher Brancas, Pretas, Aleatória | Radiogrupo marcado e descrição | UT-03 |
| Fila | Procurar sem nome; com nome; cancelar | Desabilitado sem nome; estado "Na fila"; cancelar volta ao lobby e tira a conexão da fila | UT-04 |
| Sala privada | Criar, copiar, entrar com código válido e inválido | Código exibido e copiado; erro "Sala inválida ou já iniciada!" | UT-05 |
| Pareamento de dois | Dois `ChessHome` com mesmo controle e preferências | Cada um vê a arena com a cor certa; sessão única; cores opostas mesmo com sorteio instável | IT-01 |
| Sem fantasma | Um jogador cancela ou sai da página e outro procura | O outro não pareia com quem saiu | IT-03 |
| Cores por preferência | Brancas × aleatória e pretas × pretas | Regra do contrato de cores; sorteio só no empate | IT-01 |
| Gravação | Partida encerrada nos dois circuitos | Uma linha gravada com controle e lances | IT-02 |
| Imersão e saída | Entrar e sair da partida | Shell imersivo ligado e restaurado | UT-06 |
| Acessibilidade | Controles e regiões | Rótulos, radiogrupos e regiões vivas presentes | UT-07 |
| Jornada | Dois jogadores do lobby ao mate | Fluxo completo até o resultado gravado | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — página e componente novos; o `Home` do jogo da velha não muda.

### 7.2 Testes Unitários
- **UT-01** — Dado `ChessLobby` com e sem `ReturningPlayerName`, então o campo mostra o apelido, o contador de 20 caracteres e o chip "Pronto para jogar" ou "Informe seu apelido".
- **UT-02** — Dado os três controles, então o radiogrupo "Controle de tempo" marca o atual, dispara `ControlChanged` ao escolher e mostra a descrição correspondente.
- **UT-03** — Dado as três cores, então o radiogrupo "Sua cor" marca a atual, dispara `ColorChanged` e mostra a descrição de "Aleatória" quando escolhida.
- **UT-04** — Dado "Procurar oponente", então fica desabilitado sem apelido, dispara `OnPlayOnline` com apelido, e no estado de espera mostra "Na fila" e o botão de cancelar que volta ao lobby.
- **UT-05** — Dado sala criada, sala com código copiado e entrada com código inválido, então o código aparece com "Copiar" e "Código copiado!", e o erro "Sala inválida ou já iniciada!" é anunciado em `role="alert"`.
- **UT-06** — Dado `ChessHome` que entra e sai da partida, então o shell fica imersivo com título "Partida de xadrez" e é restaurado ao voltar ao lobby e ao descartar a página.
- **UT-07** — Dado o lobby, então cada controle tem rótulo, os grupos são `role="radiogroup"`, não há `<style>` inline nem `mud-` e o título da aba é "Xadrez · XO Arena".

### 7.3 Testes de Integração
- **IT-01** — Dado dois `ChessHome` no bUnit com identidades diferentes, controle Blitz e preferências (Brancas × Aleatória; depois Pretas × Pretas), quando ambos procuram oponente (com um `coinFlip` de teste que devolve valores diferentes a cada chamada), então se pareiam, a sessão é a mesma e criada uma vez, e as duas arenas mostram cores opostas (no caso conflitante, sorteadas uma única vez).
- **IT-02** — Dado os dois `ChessHome` pareados, quando a partida termina, então uma única linha é gravada (`GameType=Chess`, controle, lances) e ambos podem voltar ao lobby.
- **IT-03** — Dado um `ChessHome` que cancela a busca (e outro que é descartado na fila), quando um terceiro procura oponente com o mesmo controle, então não pareia com nenhum dos dois; e a sala privada cancelada deixa de aceitar entrada.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
- **E2E-01** — Jornada (bUnit, dois jogadores): abrir `/xadrez`, escolher Blitz e Aleatória, procurar oponente, jogar o mate do pastor até o cartão de fim de partida e conferir, por `GameResultService.GetHistoryAsync(Game=Chess)`, a partida gravada (a tela de histórico do xadrez chega na SPEC-0059).

### 7.6 Outros
- Revisão visual (H2): 390px e 1280px contra `docs/design/stitch/chess/lobby-desktop.png`; lista de omitidos (modo de combate, barra de tempo de espera) conferida.
- `tools/tailwind/build.sh` executado e `--check` sem diferença.
- Lighthouse Acessibilidade ≥ 90 em `/xadrez`.

**Dublês e dados de teste:** `MatchmakingService` real, identidade em memória (`InMemoryPlayerStorage`), EF InMemory, `ManualTime`, `coinFlip` injetável no registro.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; a rota `/xadrez` fica pronta antes da seleção de jogos (SPEC-0048) apontar para ela.
- **Dados/schema:** N/A
- **Compatibilidade:** N/A — página nova.
- **Observabilidade:** Log de informação ao parear (id da sessão, controle) e ao gravar; erros como no jogo da velha.
- **Rollback:** Reverter o PR; a rota some.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Como escolher a cor na fila? — Preferência por jogador (Brancas, Pretas, Aleatória) com sorteio quando conflitam (Architect, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [ ] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [ ] Escrever `SPEC-0056:E2E-01`, `SPEC-0056:IT-01`, `SPEC-0056:IT-02`, `SPEC-0056:IT-03`, `SPEC-0056:UT-01`, `SPEC-0056:UT-02`, `SPEC-0056:UT-03`, `SPEC-0056:UT-04`, `SPEC-0056:UT-05`, `SPEC-0056:UT-06`, `SPEC-0056:UT-07` com a tag `SPEC-0056:<ID>` em commits `test(...)` com `Refs: SPEC-0056` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [ ] Refactor mantendo tudo verde
- [ ] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes`, `tools/tailwind/build.sh --check` e `verify SPEC-0056 --base origin/main`

**Fase final: Integração, entrega e documentação**
- [ ] Review independente (G4)
- [ ] Integração + CI verde (G5) e aprovação (H2)
- [ ] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
| G1 Red | PASS | verify SPEC-0056: Red antes do Green (0ceb6fc), 11/11 testes do plano rastreados; 18 falharam pelo motivo certo | 2026-09-29 |
| G2 Green | PASS | dotnet test 841 verdes (1 pulado: perft pesado); 25 testes do lobby/página em 5 execuções; format e tailwind --check limpos; verify PASS | 2026-09-29 |
| G3 Arquitetura | N/A | fronteira de módulos coberta por ChessModuleBoundaryTests (SPEC-0049) | 2026-09-29 |
| G4 Review | PASS | Review independente (subagente): PASS com 2 maiores (fila e sala não exclusivas; corrida entre pareamento e descarte) corrigidos com testes; menores tratados (captura de exceção no handler com log, estado do circuito por InvokeAsync, polling em vez de Task.Delay, dica de controle da sala). Dívidas: entrar na própria sala pareia consigo mesmo (igual ao Home), regiões vivas montadas condicionalmente, cor do anfitrião congelada ao criar a sala, revisão visual e Lighthouse no H2 | 2026-09-29 |
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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0056`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
