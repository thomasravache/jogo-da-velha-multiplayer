---
id: SPEC-0046
title: Xadrez multiplayer
tier: epic
type: feature
status: implemented
created: 2026-09-29
depends_on: []
adrs: [ADR-0010, ADR-0011, ADR-0012]
external: []
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0046 — Xadrez multiplayer (Épico)
## 1. Visão
Adicionar o **xadrez** ao app como segundo jogo, no mesmo visual Cyber Arena e com a mesma identidade anônima: escolha de jogo, lobby com controle de tempo e cor, fila e sala privada, arena com relógio, lances em notação algébrica, promoção, fim de partida por todas as regras, abandono, revanche com aceite, W.O. por desconexão, treino solo contra um **robô com níveis de dificuldade**, e histórico e ranking por jogo. O jogo da velha continua funcionando como está.

## 2. Escopo
**Objetivos (dentro do escopo):**
- Generalizar o modelo de partidas e o matchmaking para mais de um jogo, sem quebrar o que existe (SPEC-0047, ADR-0012).
- Tela de seleção de jogos e navegação por jogo (SPEC-0048).
- Motor de regras próprio, validado por perft (SPEC-0049, SPEC-0050, ADR-0010), relógio (SPEC-0051), núcleo da sessão compartilhada (SPEC-0052) e seu ciclo de vida — abandono, revanche e presença (SPEC-0061).
- Pareamento por controle de tempo com preferência de cor e persistência das partidas de xadrez (SPEC-0053).
- Robô com níveis Fácil e Médio, atrás de `IChessBot` (SPEC-0054, ADR-0011) e treino solo (SPEC-0058).
- Peças SVG e tabuleiro interativo acessível (SPEC-0055), arena (SPEC-0057), lobby (SPEC-0056) e a interface de abandono, revanche e desconexão (SPEC-0060).
- Histórico e ranking por jogo (SPEC-0059).

**Não-objetivos (fora do escopo):**
- Chess960 e outras variantes; proposta de empate por acordo; modo espectador; chat.
- ELO, divisões, temporadas, títulos e selos do mock; nome de abertura; análise de partida; "vantagem" numérica de material.
- Robô de força alta (Stockfish/UCI): evolução futura atrás de `IChessBot`.
- Mudanças de comportamento no jogo da velha além da rota (`/` vira seleção e o lobby vai para `/velha`).

## 3. Arquitetura Alvo
**Contexto:** módulos `Gameplay` (jogo da velha, `MatchResult`, `GameResultService`) e `Matchmaking` (filas, salas, identidades por conexão) em Blazor Server (ADR-0004); UI Cyber Arena em Tailwind standalone (ADR-0008); identidade anônima (ADR-0009). Não há hoje um segundo jogo.

```text
Web (única camada que conhece os módulos)
  GameSelect (/) ── /velha (Home) ── /xadrez (ChessHome: ChessLobby + ChessArena)
  ChessMatchRegistry (sessão única por partida, assentos por conexão) · ChessResultRecorder ──▶ GameResultService.SaveChessAsync(ChessMatchRecord) ──▶ MatchResult(GameType=Chess, TimeControl, MovesSan, FinalFen)
Modules.Chess (novo, independente)                Modules.Gameplay              Modules.Matchmaking
  Rules: Position · Move · Perft (SPEC-0049)        MatchResult.GameType (0047)   fila por chave "xadrez:{controle}" (0047/0053)
  Game: ChessGame · SAN · fim de jogo (0050)         consultas por jogo (0047)     preferência de cor opaca (0053)
  Clock: TimeControl · ChessClock (0051)
  Session: ChessSession núcleo (0052) + ciclo de vida (0061) + ColorAssignment (0053)
  Bots: IChessBot · Fácil/Médio (0054) · ChessBotTurnRunner (0058)
```

**Decisões (ADRs):** ADR-0010 — motor de regras próprio validado por perft; ADR-0011 — robô próprio atrás de `IChessBot`; ADR-0012 — generalização multi-jogo do modelo de partidas.

**Regras de arquitetura a garantir (G3):**
- `TicTacToe.Modules.Chess` não referencia Gameplay, Matchmaking nem Web, e Gameplay e Matchmaking não referenciam Chess — `ChessModuleBoundaryTests` (SPEC-0049), que lê os `.csproj` nos dois sentidos.
- Migrations somente aditivas (colunas anuláveis ou com valor padrão) — testes de migration das SPEC-0047 e SPEC-0053.
- O motor gera exatamente os lances legais — `ChessPerftTests` (SPEC-0049).

## 4. Decomposição
| Spec | Título | Tier | Tipo | Tamanho | Depende de | Consome contrato de |
|---|---|---|---|---|---|---|
| SPEC-0047 | Generalização multi-jogo (GameType, filtros e fila por chave) | full | migration | M | — | — |
| SPEC-0049 | Motor de xadrez: tabuleiro, lances legais e perft | full | foundation | M | — | — |
| SPEC-0050 | Regras de partida do xadrez: lances, SAN e fim de jogo | full | feature | M | SPEC-0049 | — |
| SPEC-0051 | Relógio de xadrez com incremento | full | feature | S | SPEC-0049 | — |
| SPEC-0052 | Sessão de xadrez compartilhada | full | feature | M | SPEC-0050, SPEC-0051 | — |
| SPEC-0061 | Ciclo de vida da sessão de xadrez: abandono, revanche e presença | full | feature | M | SPEC-0052 | — |
| SPEC-0053 | Pareamento e persistência do xadrez | full | feature | M | SPEC-0047, SPEC-0052, SPEC-0055, SPEC-0061 | — |
| SPEC-0054 | Robô de xadrez com níveis de dificuldade | full | feature | M | SPEC-0050 | — |
| SPEC-0055 | Peças SVG e tabuleiro interativo | full | feature | M | SPEC-0050 | — |
| SPEC-0057 | Arena de xadrez: relógios, lances e fim de partida | full | feature | M | SPEC-0052, SPEC-0055 | — |
| SPEC-0056 | Lobby de xadrez | full | feature | M | SPEC-0053, SPEC-0057 | — |
| SPEC-0058 | Treino solo de xadrez contra o robô | full | feature | M | SPEC-0054, SPEC-0056, SPEC-0057 | — |
| SPEC-0060 | Abandono, revanche e desconexão no xadrez | full | feature | M | SPEC-0056, SPEC-0057, SPEC-0058, SPEC-0061 | — |
| SPEC-0048 | Seleção de jogos e navegação por jogo | full | feature | S | SPEC-0047, SPEC-0058, SPEC-0060 | — |
| SPEC-0059 | Histórico e ranking do xadrez | full | feature | M | SPEC-0048, SPEC-0053 | — |

As dependências refletem arquivos e migrações compartilhados (`MatchResult`, `GameResultService`, `ChessHome`, `ChessArena`); a fonte da verdade é o frontmatter de cada spec. A seleção de jogos (SPEC-0048) só é feita depois que o xadrez está completo (lobby, arena, robô, abandono, revanche e desconexão): até lá as rotas existem sem link e o xadrez fica "escuro"; o histórico e o ranking por jogo (SPEC-0059) vêm por último.

### Matriz de cobertura do Stitch (xadrez)
Legenda: **SPEC-NNNN** = coberto · **Descartado** = dado de protótipo ou fora de escopo por decisão.

| Tela | Elemento do Stitch | Destino |
|---|---|---|
| Seleção de jogos | Duas cartas de jogo | SPEC-0048 |
| Seleção de jogos | Chip "Online" (rótulo fixo); contagem de jogadores online | SPEC-0048 (rótulo); contagem: Descartado |
| Lobby | Apelido, controle de tempo, cor, fila, sala privada, estados de fila e erro | SPEC-0056 |
| Lobby | Cartão de solo contra robô | SPEC-0058 (reusa o padrão do lobby do jogo da velha; não desenhado no Stitch) |
| Lobby | "Modo de combate: PVP", barra de progresso de espera | Descartado |
| Arena | Tabuleiro, peças, destaques, coordenadas, promoção | SPEC-0055 |
| Arena | Cartões, relógios, capturadas, lista de lances, avisos, fim de partida | SPEC-0057 |
| Arena | Abandonar, revanche, desconexão | SPEC-0060 |
| Arena | Propor empate / acordo mútuo | Descartado (decisão do produto) |
| Arena | Rating, "Ranked", anti-cheat, latência, LVL, abertura, "vantagem" | Descartado |
| Histórico | Filtros, busca, ordenação, resumo, colunas de xadrez | SPEC-0059 |
| Ranking | Pódio, tabela, sua posição, VOCÊ | SPEC-0059 |
| Ranking | Títulos, troféus, temporada, "há 2h vs …" | Descartado |

## 5. Estratégia de Entrega
- **Ambientes:** como nas fases anteriores (pipeline do repositório; sem ambiente remoto em `sdd-config.yml`, G6 N/A).
- **Entrega por onda:** cada spec vai para a `main` por PR. O xadrez só fica visível ao usuário quando a SPEC-0048 liga a seleção de jogos; até lá as rotas existem sem link.
- **Feature flags:** nenhuma; a "flag" natural é o link da seleção de jogos (SPEC-0048), que só entra depois de o xadrez estar completo.
- **Rollback:** `git revert` por PR; migrations aditivas não exigem reversão de dados.
- **Métricas de sucesso pós-release:** suíte verde; jogo da velha inalterado (histórico e ranking iguais para partidas antigas); perft igual às contagens publicadas; sem exceções não tratadas no log.

## 6. Riscos & Mitigações
- **Bugs sutis nas regras** (en passant com xeque descoberto, roque por casa atacada) — perft com posições de referência, testes de cenários conhecidos e partidas completas em SAN (ADR-0010).
- **Robô fraco ou lento** — níveis calibrados por teste de força relativa e limite de nós e de tempo; evolução para Stockfish atrás de `IChessBot` (ADR-0011).
- **Generalização quebrar o jogo da velha** — migration aditiva com valor padrão, testes de caracterização das consultas legadas e parâmetro de jogo opcional (ADR-0012).
- **Estado compartilhado entre circuitos** — sessão com lock, eventos fora do lock, testes de corrida (padrão da SPEC-0041/0042).
- **Conflito de arquivos entre specs de interface** (`ChessArena`, `ChessHome`, `ChessLobby`) — dependências explícitas as executam em sequência (0056 → 0058 → 0060).
- **Tempo de CI** (perft, partidas de robô, corridas) — perft pesado é `Category=Slow` fora do CI padrão, torneio do robô reduzido e limite de 60 s por suíte nova.
- **Cores diferentes entre circuitos e fantasmas na fila** — sessão criada uma vez por partida com sorteio único e assentos por conexão (SPEC-0053); `LeaveQueue` e `CancelPrivateRoom` (SPEC-0047).
- **Relógio parado antes do primeiro lance** — partida parada não é abortada (fica para spec futura).
- **Mock do Stitch com defeitos** (peças pretas ilegíveis, mobile incompleto, extras fora de escopo) — matriz de omitidos, teste de contraste por tokens e revisão visual no H2.
- **Crescimento de escopo** (variantes, empate, ELO, espectador) — fora do escopo até nova spec.

## 7. Critérios de Aceite do Épico
- [x] O motor gera os lances legais corretos nas posições de referência — SPEC-0049:IT-01
- [x] Uma partida jogada em SAN até o resultado dá o resultado e o texto esperados — SPEC-0050:IT-01
- [x] O relógio desconta o tempo, soma o incremento e derruba a bandeira — SPEC-0051:IT-01
- [x] Uma partida completa com relógio e W.O. por tempo funciona no domínio — SPEC-0052:IT-01
- [x] Abandono, desconexão e revanche com troca de cores funcionam no domínio — SPEC-0061:IT-01
- [x] A partida de xadrez é gravada uma única vez com jogo, cores, controle, lances e motivo — SPEC-0053:IT-01
- [x] O robô Médio vence o Fácil e sempre joga lances legais — SPEC-0054:IT-01
- [x] O tabuleiro é jogável por clique, teclado e arrastar, com promoção e contraste verificado — SPEC-0055:E2E-01
- [x] Dois jogadores jogam uma partida até o mate pela arena — SPEC-0057:E2E-01
- [x] Dois jogadores se pareiam pelo lobby de xadrez e jogam até o resultado gravado — SPEC-0056:E2E-01
- [x] Abandonar, revanche com aceite e W.O. por desconexão funcionam na arena — SPEC-0060:E2E-01
- [x] O jogador treina contra o robô e a partida solo é gravada e fica fora do ranking — SPEC-0058:E2E-01
- [x] Partidas antigas continuam sendo jogo da velha e o filtro por jogo separa os resultados — SPEC-0047:IT-02
- [x] O usuário escolhe o jogo na seleção e chega ao lobby certo — SPEC-0048:E2E-01
- [x] Histórico e ranking mostram cada jogo separadamente, com as colunas do xadrez — SPEC-0059:E2E-01

## 8. Questões em Aberto
- [x] Quer jogar contra um robô com dificuldades? — Sim, se possível; se precisar de tela nova o Architect fornece o prompt do Stitch (thomas, 2026-09-29). O Architect avaliou opções (ADR-0011): motor próprio com Fácil e Médio agora, Stockfish depois; o cartão de solo reaproveita o padrão do lobby do jogo da velha.
- [x] "Propor empate" entra? — Não (thomas, 2026-09-29).
- [x] Motor de regras: biblioteca ou próprio? — Delegado ao Architect com ADR (thomas, 2026-09-29): motor próprio validado por perft (ADR-0010), porque as bibliotecas .NET verificadas são novas demais ou abandonadas.
- [x] Chess960 entra? — Não, xadrez tradicional por enquanto (thomas, 2026-09-29).
- [x] Robô e o ranking? — Solo fica no histórico e fora do ranking, como no jogo da velha (regra já aprovada na SPEC-0035; Architect, 2026-09-29).

## 9. Aprovação (H1)
Uma aprovação humana cobre o épico e as specs filhas apresentadas junto com ele. Registrada no frontmatter (`approved_by`, `approved_at`) do épico e de cada filha.

## 10. Registro de Impedimentos
<!-- Problemas que envolvem várias filhas (conflito de integração da onda, CI quebrado, decisão transversal). Impedimento que trava uma filha específica vai na própria filha. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 11. Relatório de Entrega
### O que foi entregue
Xadrez multiplayer completo ao lado do jogo da velha: módulo `TicTacToe.Modules.Chess` (regras validadas por perft, SAN, relógio Blitz/Rápida, sessão com assentos, revanche e desconexão, robô Fácil/Médio); persistência enriquecida com `GameType` e colunas de xadrez; lobby de xadrez com pareamento e sala privada; arena jogável por clique, teclado e arrastar com promoção; treino solo contra o robô (fora do ranking); abandono, revanche com aceite e W.O. por desconexão; tela de seleção de jogos em `/` (lobby da velha em `/velha`); histórico e ranking por jogo com as colunas do xadrez. 15 specs filhas (0047–0061) e os ADR-0010 (motor próprio), ADR-0011 (robô próprio) e ADR-0012 (`GameType` e chaves de fila).

### Como foi feito
Ondas 1–10 pelo grafo de dependências, cada spec com Red, Green, review independente (G4) e PR com CI verde (Build, Format & Test e sdd) antes do merge; fechamento documental em PR próprio. Emendas de `touches`/contrato: 0058 (v2) e 0060 (v2). A suíte foi endurecida contra testes sensíveis a tempo (PR de estabilização do debounce e do E2E do treino solo).

### Critérios de aceite
Todos marcados em §7 e provados pelos testes das filhas (SPEC-0049:IT-01, 0050:IT-01, 0051:IT-01, 0052:IT-01, 0061:IT-01, 0053:IT-01, 0054:IT-01, 0055:E2E-01, 0057:E2E-01, 0056:E2E-01, 0060:E2E-01, 0058:E2E-01, 0047:IT-02, 0048:E2E-01, 0059:E2E-01), todos verdes no CI da `main`.

### Métricas pós-release
Sem ambiente remoto (`staging_url` vazio; G6 N/A aprovado pelo usuário): suíte com 910 testes (909 passam, 1 pulado: perft pesado atrás de `CHESS_SLOW`) verdes no CI.

### Pendências
- **Nunca executado no navegador:** o app não foi aberto (Aspire exige contêiner de SQL Server); revisão visual a 390/1280 px contra o Stitch e Lighthouse ≥ 90 não foram feitas em nenhuma tela do xadrez.
- **Migrações EF escritas à mão** (`AddGameType`, `AddChessInfo`) e nunca aplicadas a um SQL Server real.
- Setas/Espaço rolam a página na arena (herdado); arrastar dentro de `button` pode falhar no Firefox.
- Desistir (Resignation) marca o perdedor como ausente, então não há revanche depois dela; queda de circuito durante a espera de revanche em partida encerrada é ignorada (30 s de expiração).
- 0059: resumo do histórico sem guarda de sequência em trocas rápidas de jogo; `?jogo=` lido só na inicialização; links `Href="/"` de Histórico/Ranking caem na seleção de jogos.
- Cartão do robô no lobby sem tela Stitch dedicada; rating/temporada/badges do mock não implementados; Chess960, empate por acordo e níveis além de Fácil/Médio ficaram fora do escopo.
- Teste sensível a tempo remanescente: `SPEC-0057:IT-01` apareceu em 1 de 8 execuções sob carga (não reproduzido nas execuções de review).

## 12. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda, aprovada pelo humano. -->
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
