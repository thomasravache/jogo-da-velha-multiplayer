---
id: SPEC-0031
title: Arena da partida Cyber Arena
tier: full
type: feature
user_facing: true
status: approved
created: 2026-09-29
parent: SPEC-0028
depends_on: [SPEC-0043]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.css, src/TicTacToe/TicTacToe.Web/Components/Game/GameBoard.razor, src/TicTacToe/TicTacToe.Web/Components/Game/Scoreboard.razor, src/TicTacToe/TicTacToe.Web/Components/Game/PlayerCard.razor, src/TicTacToe/TicTacToe.Web/Components/Game/RematchBar.razor, src/TicTacToe/TicTacToe.Web/Components/Ui/NeonProgress.razor, src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Web/wwwroot/app.js, tests/TicTacToe.Tests/ArenaCyberArenaTests.cs, tests/TicTacToe.Tests/GameSessionWinningLineTests.cs, tests/TicTacToe.Tests/DecomposedComponentsTests.cs, tests/TicTacToe.Tests/GameBoardTimerTests.cs, tests/TicTacToe.Tests/MudBlazorIntegrationTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0031 — Arena da partida Cyber Arena

## 1. Visão Geral
Redesenha a tela de partida (`/` com partida ativa) no visual Cyber Arena: dois **cards de jogador** (X coral, O ciano, com placar da sessão e destaque de quem joga), **tabuleiro** com brilho por jogador, marcas em SVG, **destaque da linha vencedora** em verde pulsante, **barra de tempo** com cores por faixa, banners de resultado e W.O., e o botão de jogar novamente. No mobile o shell entra em modo imersivo (sem barra inferior). Nenhuma regra de jogo muda; a única adição de domínio é a propriedade derivada `GameSession.WinningLine`.

## 2. Motivação & Escopo
**Motivação:** a arena é a tela mais usada e hoje mistura `div`s customizados, `MudChip`/`MudProgressLinear` e CSS em `Home.razor.css`. O `DESIGN.md` especifica tabuleiro, cards, timer e caminho vencedor com detalhes que o visual atual não tem.

**Objetivos (dentro do escopo):**
- Referências: `docs/design/stitch/arena-desktop.html` (cards nas laterais do tabuleiro, quadro de até 960px) e `arena-mobile.html` (cards no topo, tabuleiro até `min(90vw, 360px)`).
- `PlayerCard`: símbolo (✕ coral / ◯ ciano), nome, tag "Você" no jogador local, placar da sessão em Space Grotesk; jogador da vez com brilho da sua cor, o outro a 60% de opacidade.
- Faixa de status: "Sua vez, {nome}!", "Vez de {nome}…", "Você venceu, {nome}! 🎉", "{nome} venceu! 😅", "Deu velha! 🤝".
- Tabuleiro 3×3 quadrado: célula `surface-high` com borda interna, hover com prévia da marca do jogador da vez, marcas em SVG (X em cruz angular, O em anel), acessível (`role="button"`, `aria-label` "Casa 1, vazia" etc., foco por teclado apenas quando jogável).
- **Linha vencedora:** `GameSession.WinningLine` (3 índices) e realce verde com animação `win-pulse`.
- Timer (`NeonProgress` + "09s restantes"): verde > 50%, âmbar de 50% a 20%, vermelho com leve vibração < 20%; some quando a partida termina; texto fixo "Jogada fora do tempo = vitória do oponente por W.O.".
- Banner de W.O. quando `IsTimedOut` (regra da SPEC-0027).
- `RematchBar`: botão **Jogar novamente** (comportamento atual: reinicia a partida) quando há vencedor ou empate.
- `Home` ativa o modo imersivo (`ShellState`) durante a partida e o restaura ao sair.
- Confete (`app.js`) com a paleta dos tokens.
- `<PageTitle>` da página `/` com o nome "XO Arena".
- Remoção do CSS legado da arena em `Home.razor.css` e do título "Jogo da Velha ✕ ◯" da tela de partida.

**Não-objetivos (fora do escopo):**
- Elementos do Stitch sem backend, **omitidos**: série MD5/rodada, reações rápidas, log de lances, espectadores, ELO/nível dos jogadores, ping por jogador, "ID da arena", "SignalR Sync", atalhos NumPad/Auto-Lock, botão **Abandonar** e **Pedir revanche** com aceite. Destino: matriz de cobertura da SPEC-0035 (F, I e backlog).
- Botão "voltar" no header imersivo (vem com "abandonar").
- Qualquer mudança em `MakeMove`, timer, placar ou matchmaking, salvo expor `WinningLine`.
- Tela do lobby (SPEC-0030).

## 3. Dependências
- **Implementações necessárias:** SPEC-0043 — `ShellState`, `NeonCard`, `PillButton`, `StatusChip`, `Icon`.
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** componentes `GameBoard` (`Game`, `MyPlayer`, `OnCellClick`), `Scoreboard` (`Game`, `MyPlayer`) e `Home` (SPEC-0024); `GameSession` com `Board`, `Winner`, `CurrentTurn`, `RemainingSeconds`, `IsTimedOut` e o `CheckWin` privado (SPEC-0027); eventos reativos (SPEC-0018/0025).

**Decisão:** manter as APIs de `GameBoard` e `Scoreboard`; `Scoreboard` passa a renderizar os dois `PlayerCard` e a faixa de status com `display: contents`, para que a grade de `Home` posicione os cards ao lado do tabuleiro no desktop. `GameSession.WinningLine` é calculada a partir do mesmo conjunto de linhas usado por `CheckWin` (extraído para uma constante compartilhada), sem novo estado além de um campo definido quando alguém vence por jogada.

**Justificativa:** preserva os testes de comportamento existentes e mantém o domínio livre de UI; a linha vencedora é útil também à persistência (SPEC-0036).

**Desvio do padrão existente:** substitui `MudChip`/`MudProgressLinear`/`MudAlert` por primitivos Cyber Arena (ADR-0008).

**Alternativas descartadas:** calcular a linha vencedora na UI (duplica `CheckWin` e a SPEC-0036 precisaria dela no domínio); dividir `GameBoard` em vários componentes (escopo além do necessário).

**ADRs:** ADR-0008.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** animações só com CSS (`transform`/`opacity`) e respeitando `prefers-reduced-motion`; tick de 1 s do timer causa apenas re-render do componente de partida; medido no bUnit (uma renderização por evento).
- **Segurança:** N/A — sem entrada nova; a regra de turno continua validada no domínio (`MakeMove`).
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** `WinningLine` nula em empate, W.O. e partida em andamento; a UI não assume que existe.
- **Acessibilidade (UI):** células com nome acessível e estado; foco por teclado (Enter/Espaço) só em células jogáveis; status da partida em região `aria-live="polite"`; cor nunca é o único indicador (símbolo X/O + texto); `prefers-reduced-motion` desliga pulso e vibração, mas o realce verde da linha vencedora permanece (só cor, sem animação); contraste AA (SPEC-0043). Lighthouse ≥ 90 na partida.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** tela `/` com partida ativa; componentes `PlayerCard`, `GameBoard`, `Scoreboard`, `RematchBar`; propriedade `GameSession.WinningLine`.

```text
GameSession
  IReadOnlyList<int>? WinningLine { get; }
    // 3 índices (0–8) ordenados de forma crescente da linha que deu a vitória por jogada;
    // null enquanto a partida corre, em empate, em vitória por W.O. e após Restart()

PlayerCard   [Parameter] Player Player · string Name · int Score · bool IsMe · bool IsActive
GameBoard    [Parameter] GameSession Game · Player MyPlayer · EventCallback<int> OnCellClick   (inalterado)
Scoreboard   [Parameter] GameSession Game · Player MyPlayer                                      (inalterado)
RematchBar   [Parameter] bool Finished · EventCallback OnRematch
NeonProgress [Parameter] double Value(0–100) · Tone="Success|Warning|Danger" · string? Label

Faixas do timer (DefaultTurnTimeSeconds = 15): restante > 50% → Success · > 20% → Warning · senão Danger
Realce de célula: data-win="true" nas 3 células de WinningLine; animação "win-pulse" (1,5 s)
Layout: >= lg  [PlayerCard X] [tabuleiro + timer + status] [PlayerCard O]      < lg  [X][O] · status · tabuleiro · timer
```

**Arquivos/módulos afetados:** ver `touches`. `GameSession.cs` ganha `WinningLine` e reutiliza o conjunto de linhas de `CheckWin`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Cards dos jogadores | Partida com nomes e placar | Dois cards com símbolo, nome e placar; "Você" só no jogador local | UT-01 |
| Jogador da vez | `CurrentTurn` X ou O | Card ativo com brilho da cor; o outro a 60% de opacidade | UT-02 |
| Faixa de status | Vez local, vez do rival, vitória local, derrota, empate | Texto e `aria-live` conforme o caso | UT-03 |
| Jogada | Clique/Enter em célula vazia na vez do jogador local | `OnCellClick(index)` emitido; em célula ocupada, fora da vez ou com fim de jogo, nada acontece | UT-04 |
| Acessibilidade das células | Célula vazia jogável / ocupada | `aria-label` "Casa N, vazia" / "Casa N, X"; foco só em jogável | UT-04 |
| Linha vencedora (domínio) | Vitória por jogada em cada uma das 8 linhas | `WinningLine` = a linha; `null` em empate, W.O. e após reinício | UT-05 |
| Realce da linha | `WinningLine` presente | 3 células com `data-win="true"`; demais sem | UT-06 |
| Timer | `RemainingSeconds` 15, 7, 2 | Faixa Success, Warning, Danger; rótulo "NNs restantes"; some no fim da partida | UT-07 |
| Banner de W.O. | `IsTimedOut` | Aviso "Tempo esgotado! Vitória por W.O. para {nome}" | UT-08 |
| Jogar novamente | Partida terminada | `RematchBar` visível; clique dispara `OnRematch`; oculto durante a partida | UT-09 |
| Confete | `app.js` | Paleta igual aos tokens (primary, secondary, gold, success) | UT-10 |
| Layout responsivo | Marcação da arena | Classes que posicionam cards ao lado do tabuleiro em `lg` e no topo abaixo | UT-11 |
| Sem resquício do Mud | Arena em qualquer estado | Markup sem `mud-` | UT-12 |
| Modo imersivo | Partida iniciada e depois encerrada | `ShellState.Immersive` verdadeiro durante a partida; restaurado ao sair | IT-01 |
| Partida solo ponta a ponta | Apelido, solo, primeira jogada | Arena visível e ✕ na casa escolhida | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado `GameSession`, então `MakeMove` mantém as regras atuais (turno alternado, vitória por jogada, `Restart`) — fixa o comportamento antes de extrair as linhas de vitória.

**Testes existentes afetados:** `DecomposedComponentsTests` (afirma `players-bar`, `status`, `.cell.playable` e o conteúdo de `Home.razor.css` com `.game-container`/`.board`), `GameBoardTimerTests` (afirma `mud-progress-linear`) e `MudBlazorIntegrationTests` `SPEC-0026:IT-02` (GameBoard). São atualizados para os novos seletores/primitivos; mudanças justificadas pela troca de biblioteca e pela remoção do CSS legado (ADR-0008).

### 7.2 Testes Unitários
- **UT-01** — Dado `Scoreboard` com "Alice" (X, placar 2) e "Bob" (O, placar 1) e `MyPlayer=X`, então há dois `PlayerCard` com nome, símbolo e placar, e a tag "Você" só no card de Alice.
- **UT-02** — Dado `CurrentTurn=X` e depois `O`, então o card da vez tem a classe de brilho da sua cor e o outro tem a classe de opacidade reduzida.
- **UT-03** — Dado cada estado (vez local, vez do rival, vitória local, derrota, empate), então a faixa de status exibe o texto do contrato e está numa região `aria-live="polite"`.
- **UT-04** — Dado `GameBoard` na vez do jogador local, então células vazias têm `role="button"`, `tabindex="0"` e `aria-label` "Casa N, vazia"; clicar ou pressionar Enter/Espaço dispara `OnCellClick(index)`; célula ocupada, fora da vez ou com partida encerrada não dispara e não é focável.
- **UT-05** — Dado `GameSession`, para cada uma das 8 linhas vencedoras, então `WinningLine` é a linha correta em ordem crescente; nula em partida em andamento, empate, W.O. e após `Restart()`.
- **UT-06** — Dado `GameBoard` com `WinningLine = [0,4,8]`, então exatamente as células 0, 4 e 8 têm `data-win="true"`.
- **UT-07** — Dado `GameBoard` com `RemainingSeconds` 15, 7 e 2, então o `NeonProgress` está em Success, Warning e Danger, o rótulo mostra "15s", "07s", "02s restantes" e o bloco do timer não existe com vitória ou empate.
- **UT-08** — Dado `IsTimedOut` com vencedor, então o banner de W.O. cita o nome do vencedor.
- **UT-09** — Dado `RematchBar`, então com `Finished=false` não renderiza o botão; com `Finished=true` renderiza "Jogar novamente" e o clique dispara `OnRematch`.
- **UT-10** — Dado `wwwroot/app.js`, então a paleta do confete contém os hex de `primary`, `secondary`, `gold` e `success` dos tokens e não contém os hex antigos.
- **UT-11** — Dado o markup da arena, então há classes que dispõem cards e tabuleiro em três colunas a partir de `lg` e empilhados abaixo.
- **UT-12** — Dado `Scoreboard`, `PlayerCard`, `GameBoard` e `RematchBar` em qualquer estado, então o markup não contém `mud-`; e `Home.razor` declara `<PageTitle>` com "XO Arena".

### 7.3 Testes de Integração
- **IT-01** — Dado `Home` renderizado no bUnit com `MatchmakingService`, `ConcurrentDictionary<Guid, GameSession>`, `GameResultService` (EF InMemory) e `ShellState`, quando o jogador informa o apelido e inicia o solo, então `ShellState.Immersive` fica verdadeiro; ao descartar o componente, é restaurado.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (`WinningLine` é consumida pela SPEC-0036 depois desta).

### 7.5 Testes E2E
- **E2E-01** — Jornada da partida solo (bUnit): renderizar `Home`, informar o apelido, iniciar o solo (Fácil), então cards, tabuleiro, timer e status aparecem; clicar na casa central coloca ✕ nela.

### 7.6 Outros
- Revisão visual (H2): 390px e 1280px contra `arena-mobile.png`/`arena-desktop.png` em cinco estados (vez local, vez do rival, vitória com linha, empate, W.O.).
- Acessibilidade: Lighthouse ≥ 90; navegação completa da partida só com teclado.
- `prefers-reduced-motion`: conferido manualmente no navegador.

**Dublês e dados de teste:** `GameSession(enableBackgroundTimer: false)` com `Tick()` manual (padrão das SPEC-0027); EF InMemory para `GameResultService`; `BunitContext` com `JSInterop` em modo Loose.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; nenhuma regra de jogo muda.
- **Dados/schema:** N/A — `WinningLine` não é persistida aqui.
- **Compatibilidade:** APIs de `GameBoard`, `Scoreboard` e eventos de `GameSession` inalteradas.
- **Observabilidade:** revisão visual dos cinco estados; console sem erros.
- **Rollback:** `git revert` do PR.
- **Etapas de migração/coexistência:** etapa 3 de 4 (tela 2 de 4).

## 9. Questões em Aberto
- [x] As faixas de cor do timer passam a seguir o `DESIGN.md` (percentual: verde > 50%, âmbar 50%–20%, vermelho < 20%; recomendado) ou mantêm as atuais em segundos (verde > 7 s, âmbar > 3 s)? Com 15 s a diferença é de 1 s. — Faixas percentuais do `DESIGN.md`. (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0031`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
