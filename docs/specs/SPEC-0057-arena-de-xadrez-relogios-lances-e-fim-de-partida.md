---
id: SPEC-0057
title: "Arena de xadrez: relógios, lances e fim de partida"
tier: full
type: feature
user_facing: true
status: approved
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0052, SPEC-0055]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Chess/ChessArena.razor, src/TicTacToe/TicTacToe.Web/Components/Chess/ChessPlayerCard.razor, src/TicTacToe/TicTacToe.Web/Components/Chess/ChessMoveList.razor, src/TicTacToe/TicTacToe.Web/Components/Chess/CapturedPieces.razor, src/TicTacToe/TicTacToe.Web/Components/Chess/ChessEndCard.razor, src/TicTacToe/TicTacToe.Web/wwwroot/css/cyber-arena.css, tests/TicTacToe.Tests/ChessArenaTests.cs, tests/TicTacToe.Tests/ChessMoveListTests.cs, tests/TicTacToe.Tests/ChessEndCardTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0057 — Arena de xadrez: relógios, lances e fim de partida

## 1. Visão Geral
Entrega a **arena de xadrez**: o componente `ChessArena` que apresenta uma `ChessSession` — tabuleiro interativo, cartões dos jogadores com **relógio**, peças capturadas, **lista de lances** em SAN, avisos de vez e xeque, seletor de promoção e o cartão de **fim de partida** — e traduz cliques em lances validados pela sessão.

## 2. Motivação & Escopo
**Motivação:** A arena é a tela principal do Stitch de xadrez (`docs/design/stitch/chess/arena-desktop.png`). Com sessão (SPEC-0052) e tabuleiro (SPEC-0055) prontos, falta a composição que os torna jogáveis.

**Objetivos (dentro do escopo):**
- `ChessArena` (parâmetros: `ChessSession Session`, `int MySeat`) que **relê a cor corrente do assento a cada estado** (`Session.ColorOf(MySeat)`), com tabuleiro orientado para quem joga, seleção e destinos legais calculados a partir do `Snapshot().Position`, lances por clique, teclado e arrastar, promoção com `PromotionPicker`.
- `ChessPlayerCard`: nome, chip "Você", cor, **relógio** `mm:ss` (vermelho e pulsando abaixo de 10 s, destacado em quem joga) e peças capturadas (`CapturedPieces`).
- `ChessMoveList`: lances em duas colunas numeradas, último destacado, rolagem; faixa horizontal no mobile.
- Avisos em região `aria-live`: "Sua vez", "Vez de {nome}…", "Xeque!".
- `ChessEndCard`: resultado e motivo (xeque-mate, afogamento, material insuficiente, 50 lances, repetição, tempo, abandono, desconexão), lances e duração.
- Relógio na tela atualizado a cada segundo pelo evento da sessão; a arena só lê o `Snapshot` imutável (nunca o `ChessGame` mutável).

**Não-objetivos (fora do escopo):**
- Botões e fluxos de abandono, revanche, presença e W.O. por desconexão (SPEC-0060).
- Lobby e ciclo de vida da sessão na página (SPEC-0056), robô (SPEC-0058).
- Proposta de empate, rating, nome de abertura e "vantagem" numérica de material (dado de protótipo).

## 3. Dependências
- **Implementações necessárias:** SPEC-0052 (`ChessSession`) e SPEC-0055 (`ChessBoard`, `PromotionPicker`, tokens).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `Home` + `Scoreboard` + `GameBoard` no jogo da velha (SPEC-0031): componentes de apresentação que leem o estado da sessão e re-renderizam pelo evento `OnStateChanged`; modo imersivo do shell (SPEC-0043).

**Decisão:** `ChessArena` lê a sessão diretamente (como `Scoreboard` lê `GameSession`) e ela mesma assina `OnStateChanged` (descartando a assinatura ao sair); subcomponentes puros recebem valores.

**Justificativa:** Mantém o padrão de componentes finos da SPEC-0031 e concentra a assinatura de eventos em um ponto só.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Ligar a assinatura na página (`ChessHome`), como a `Home`: acopla a arena à página e dificulta testá-la sozinha.

**ADRs:** ADR-0008

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Re-renderização por segundo apenas nos relógios e no estado; lista de lances até centenas de itens sem custo perceptível.
- **Segurança:** Todo lance é revalidado pela sessão no servidor; o cliente só envia origem, destino e peça de promoção; jogador fora da vez não altera nada.
- **Privacidade e dados pessoais:** Só nomes já visíveis; `PlayerId` nunca é exibido.
- **Disponibilidade e resiliência:** Erro ao jogar (lance recusado) não quebra o circuito: a seleção é limpa e uma mensagem curta aparece; a assinatura de evento é solta no descarte.
- **Acessibilidade (UI):** Região viva única para avisos; relógios com `role="timer"` e rótulo, sem anunciar a cada segundo (só abaixo de 10 s uma vez); lista de lances como lista ordenada; foco e teclado do tabuleiro (SPEC-0055); contraste AA; Lighthouse ≥ 90.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `ChessArena, ChessPlayerCard, CapturedPieces, ChessMoveList, ChessEndCard`

```text
ChessArena     [Parameter] ChessSession Session · int MySeat · EventCallback OnBackToLobby (usado pela SPEC-0060)
  a cor do jogador é sempre Session.ColorOf(MySeat), relida a cada estado (muda na revanche): a orientação do tabuleiro, "Sua vez" e o cartão "Você" acompanham
  layout: cartão do oponente, tabuleiro, cartão do jogador, lista de lances, avisos; desktop em três colunas (cartões laterais, tabuleiro ao centro), mobile empilhado
  fluxo do jogador: clicar peça própria → destinos legais (ChessGame.DestinationsFrom) → clicar destino → session.TryMove; promoção abre PromotionPicker
  só interativo quando é a vez da cor corrente do assento e a partida está em andamento; a orientação é a cor corrente do assento
  avisos (região aria-live="polite"): "Sua vez, {nome}!" · "Vez de {nome}…" · "Xeque!" (quando o lado a jogar está em xeque)

ChessPlayerCard   [Parameter] string Name · PieceColor Color · bool IsMe · bool IsActive · TimeSpan Remaining · IReadOnlyList<PieceType> Captured
   relógio "mm:ss" (abaixo de 10 s: classe de alerta e data-low="true"); peças capturadas em miniatura (CapturedPieces)

ChessMoveList     [Parameter] IReadOnlyList<ChessMove> Moves
   <ol> numerada por lance completo: "1. e4 e5"; último lance com aria-current; rolagem para o fim

ChessEndCard      [Parameter] ChessResult Result · string? WinnerName · int MoveCount /*meios-lances*/ · TimeSpan Duration   // mostra lances completos (⌈meios/2⌉)
   título por resultado ("Xeque-mate — {nome} venceu", "Empate por afogamento", "Vitória por tempo", …) e resumo (lances, duração)
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Composição | Sessão nova em pé de igualdade | Tabuleiro, dois cartões, relógios, lista de lances vazia e aviso da vez | UT-01 |
| Jogar por clique | Peça própria → destino legal; destino ilegal; peça do oponente; fora da vez | Só o legal chega à sessão; seleção limpa nos demais | UT-02 |
| Promoção | Peão à última fileira | Seletor aparece; escolher aplica o lance; cancelar mantém a jogada pendente sem aplicar | UT-03 |
| Relógios | Tempo restante e abaixo de 10 s; vez de cada lado | Formato mm:ss, alerta de tempo baixo e destaque de quem joga | UT-04 |
| Lista de lances | Lances de várias jogadas | Duas colunas numeradas, último destacado | UT-05 |
| Capturadas | Capturas dos dois lados | Miniaturas por cor de quem capturou | UT-06 |
| Avisos | Vez própria, vez do outro, xeque | Textos do contrato na região viva | UT-07 |
| Fim de partida | Mate, afogamento, tempo e demais motivos | Cartão com título e resumo corretos; tabuleiro somente leitura | UT-08 |
| Assinatura de evento | Descartar a arena e mudar a sessão | Sem re-render nem exceção após o descarte | UT-09 |
| Troca de cores | Cores dos assentos trocadas | Orientação e textos seguem a cor corrente do assento | UT-10 |
| Duas telas | Dois `ChessArena` na mesma sessão | Um lance feito por um aparece no outro | IT-01 |
| Jornada | Partida curta até o mate pela interface | Lances, relógio, lista e cartão final | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — componentes novos; o jogo da velha não muda.

### 7.2 Testes Unitários
- **UT-01** — Dado uma `ChessSession` nova e `MySeat` com as brancas, então a arena mostra o tabuleiro orientado para as brancas, os dois cartões com nomes e relógios iguais, a lista de lances vazia e "Sua vez".
- **UT-02** — Dado clique em peça própria, em destino legal, em destino ilegal, em peça do oponente e fora da vez, então só o lance legal chega à sessão e nos demais casos a seleção é limpa sem lance.
- **UT-03** — Dado peão na sétima fileira, quando o destino é a oitava, então o seletor de promoção abre; escolher a dama aplica `e7→e8=Q`; cancelar não aplica nenhum lance.
- **UT-04** — Dado tempos de 04:12 e 00:09, então os cartões mostram `mm:ss`, o de 9 s recebe o estado de alerta, e o cartão de quem joga fica destacado.
- **UT-05** — Dado 5 lances jogados, então a lista mostra "1. e4 e5 2. …" em ordem, com o último lance marcado como atual.
- **UT-06** — Dado capturas de ambos os lados, então cada cartão lista as peças que aquele jogador capturou.
- **UT-07** — Dado a vez própria, a vez do oponente e um xeque, então a região `aria-live` traz "Sua vez, {nome}!", "Vez de {nome}…" e "Xeque!".
- **UT-08** — Dado o fim por mate, afogamento, tempo, abandono, desconexão, material insuficiente, 50 lances e repetição, então `ChessEndCard` mostra o título e o resumo de cada um e o tabuleiro fica sem interação.
- **UT-09** — Dado o descarte da arena, quando a sessão muda depois, então nenhuma re-renderização nem exceção ocorre (`RenderCount` do bUnit inalterado).
- **UT-10** — Dado uma sessão cujas cores dos assentos são trocadas (como na revanche), então a mesma arena passa a orientar o tabuleiro para a nova cor do assento e a mostrar "Sua vez" conforme a nova cor.

### 7.3 Testes de Integração
- **IT-01** — Dado dois `ChessArena` (assentos das brancas e das pretas) sobre a mesma `ChessSession`, quando as brancas jogam `e2→e4`, então a arena das pretas mostra o lance, o destaque e passa a aceitar o lance de resposta.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
- **E2E-01** — Jornada (bUnit): duas arenas (assentos 0 e 1) na mesma sessão, jogar o mate do pastor por cliques (e4, e5, Bc4, Nc6, Qh5, Nf6, Qxf7#); a lista mostra os lances em SAN, o relógio da vez alterna, e o cartão final anuncia o xeque-mate para as brancas.

### 7.6 Outros
- Revisão visual (H2): 390px e 1280px contra `docs/design/stitch/chess/arena-desktop.png` e `arena-mobile.png` (o mobile do mock é incompleto; vale a definição desta spec); lista de omitidos conferida (rating, empate, abertura, vantagem).
- `tools/tailwind/build.sh` executado e `--check` sem diferença.
- Lighthouse Acessibilidade ≥ 90 na arena.

**Dublês e dados de teste:** `ChessSession` real com `ManualTime` e `enableBackgroundTimer: false`; `BunitContext`; sessões montadas via `SetSeat`.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; a arena só aparece com o lobby (SPEC-0056).
- **Dados/schema:** N/A
- **Compatibilidade:** N/A — componentes novos.
- **Observabilidade:** N/A
- **Rollback:** Reverter o PR.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Mostrar "vantagem" de material (+3)? — Não como número autoritativo do Stitch; as peças capturadas bastam nesta spec (Architect, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [ ] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [ ] Escrever `SPEC-0057:E2E-01`, `SPEC-0057:IT-01`, `SPEC-0057:UT-01`, `SPEC-0057:UT-02`, `SPEC-0057:UT-03`, `SPEC-0057:UT-04`, `SPEC-0057:UT-05`, `SPEC-0057:UT-06`, `SPEC-0057:UT-07`, `SPEC-0057:UT-08`, `SPEC-0057:UT-09`, `SPEC-0057:UT-10` com a tag `SPEC-0057:<ID>` em commits `test(...)` com `Refs: SPEC-0057` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [ ] Refactor mantendo tudo verde
- [ ] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes`, `tools/tailwind/build.sh --check` e `verify SPEC-0057 --base origin/main`

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0057`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
