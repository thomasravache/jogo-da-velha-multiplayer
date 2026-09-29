---
id: SPEC-0055
title: Peças SVG e tabuleiro interativo
tier: full
type: feature
user_facing: true
status: proposed
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0050]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Chess/ChessPiece.razor, src/TicTacToe/TicTacToe.Web/Components/Chess/ChessBoard.razor, src/TicTacToe/TicTacToe.Web/Components/Chess/PromotionPicker.razor, src/TicTacToe/TicTacToe.Web/Styles/cyber-arena.input.css, src/TicTacToe/TicTacToe.Web/wwwroot/css/cyber-arena.css, tests/TicTacToe.Tests/ChessPieceTests.cs, tests/TicTacToe.Tests/ChessBoardTests.cs, tests/TicTacToe.Tests/ChessDesignTokensTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by:
approved_at:
---

# SPEC-0055 — Peças SVG e tabuleiro interativo

## 1. Visão Geral
Entrega os componentes visuais do xadrez: as **12 peças em SVG** (conjunto do Stitch), o **tabuleiro interativo** (orientação, seleção, casas de lances legais, última jogada, rei em xeque, mover por clique, teclado e arrastar) e o **seletor de promoção**, com contraste verificado por teste.

## 2. Motivação & Escopo
**Motivação:** O Stitch desenha o tabuleiro e as peças, mas o mock tem peças pretas quase invisíveis e o tabuleiro mobile incompleto. Esta spec implementa os componentes corrigindo esses defeitos.

**Objetivos (dentro do escopo):**
- `ChessPiece` (cor, tipo, tamanho): SVG inline do conjunto em `docs/design/stitch/chess/pecas.md`, com `role="img"` e título em português.
- `ChessBoard` (apresentação): recebe `Position`, orientação, casa selecionada, destinos legais, último lance, casa do rei em xeque e callbacks; coordenadas a–h e 1–8; rótulo de cada casa ("e2, peão branco", "e4, vazia, destino possível").
- Interação: clicar peça e depois destino; teclado (setas movem o foco, Enter/Espaço selecionam, Esc cancela); arrastar e soltar; jogada de peça de promoção aciona `PromotionPicker`.
- `PromotionPicker` (Dama, Torre, Bispo, Cavalo; atalhos Q R B N; Esc cancela) em diálogo acessível.
- Tokens de cor do tabuleiro e das peças em `cyber-arena.input.css` com contraste ≥ 3:1 (contorno da peça × casa clara e casa escura), verificados por teste.
- Responsivo: tabuleiro quadrado que cabe em 390px de largura sem rolagem horizontal.

**Não-objetivos (fora do escopo):**
- Relógios, cartões de jogador, lista de lances e fim de partida (SPEC-0057).
- Regras: o tabuleiro não valida lances, só apresenta os destinos que recebe.
- Animação de deslocamento, sons e temas de peças alternativos.

## 3. Dependências
- **Implementações necessárias:** SPEC-0050 (`Position`, `Square`, `PieceType`, `Move` para o modelo de apresentação).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** Nenhum pacote; SVGs vêm de `docs/design/stitch/chess/pecas.md`.

## 4. Decisão Arquitetural
**Contexto:** Componentes Blazor de apresentação com primitivos Cyber Arena (SPEC-0043) e CSS por Tailwind standalone (ADR-0008); `GameBoard` do jogo da velha como referência de interação por teclado (SPEC-0031); teste de contraste de tokens (`ShellLayoutTests.TokenPairs_ShouldMeetWcagAa`).

**Decisão:** Componentes puros em `Components/Chess`; peças como SVG inline; tokens do tabuleiro em `@theme`; contraste garantido por teste sobre os tokens.

**Justificativa:** Segue ADR-0006/0008; SVG inline escala sem requisição extra e aceita tokens de cor.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Peças como `<img>` (sem tokens, mais requisições); canvas (perde acessibilidade); fonte de peças Unicode (varia por sistema).

**ADRs:** ADR-0008

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** 64 casas e até 32 peças renderizadas sem custo perceptível; sem JS próprio para o fluxo de clique (arrastar usa eventos Blazor).
- **Segurança:** N/A — apresentação; a validação do lance é do servidor.
- **Privacidade e dados pessoais:** N/A — sem dados pessoais.
- **Disponibilidade e resiliência:** Sem estado de domínio; recebe tudo por parâmetros.
- **Acessibilidade (UI):** Cada casa é um botão com rótulo textual; foco visível; todo lance feito por teclado; peça e destino não dependem só de cor; contraste ≥ 3:1 (componentes gráficos) e AA no texto; `prefers-reduced-motion` respeitado; Lighthouse ≥ 90.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `Componentes ChessPiece, ChessBoard e PromotionPicker; tokens --color-board-light/dark e derivados`

```text
ChessPiece      [Parameter] PieceColor Color · PieceType Type · int Size = 40   → <svg role="img"><title>Rei branco</title>…</svg> (12 variantes do pecas.md)

ChessBoard
  [Parameter] Position Position                      // peças a desenhar
  [Parameter] PieceColor Orientation = White         // quem fica embaixo; coordenadas acompanham
  [Parameter] Square? Selected                       // casa selecionada (destaque)
  [Parameter] IReadOnlyList<Square> Destinations     // destinos legais da peça selecionada (ponto se vazia, anel se captura)
  [Parameter] Move? LastMove                         // origem e destino destacados
  [Parameter] Square? CheckSquare                    // casa do rei em xeque
  [Parameter] bool Interactive = true                // false: só leitura (fim de partida, vez do outro)
  [Parameter] EventCallback<Square> OnSquareActivated   // clique, Enter/Espaço
  [Parameter] EventCallback<(Square From, Square To)> OnDragMove   // arrastar e soltar
  Cada casa: <button data-square="e4" aria-label="e4, peão branco" aria-pressed=…>; grade role="grid"; setas movem o foco entre casas.

PromotionPicker
  [Parameter] PieceColor Color · EventCallback<PieceType> OnChosen · EventCallback OnCancel
  quatro botões (Dama, Torre, Bispo, Cavalo), Q/R/B/N escolhem, Esc cancela; role="dialog" aria-label="Promoção de peão"; foco inicial na dama.

Tokens (cyber-arena.input.css, @theme): --color-board-light, --color-board-dark, --color-piece-white-outline (#00D2D3), --color-piece-black-outline (#FF4757),
  --color-square-selected, --color-square-target, --color-square-last, --color-square-check.
  Regra testada: razão de contraste ≥ 3.0 entre cada contorno de peça e cada cor de casa; ≥ 3.0 entre destaque e casa.
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Peças | 12 combinações de cor e tipo | SVG com título em português, `role=img` e cores dos tokens | UT-01 |
| Tabuleiro | Posição inicial, orientação branca e preta | 64 casas, coordenadas certas, peças nas casas corretas | UT-02 |
| Destaques | Seleção, destinos, último lance, xeque | Marcações distintas e não só por cor (texto/ícone/atributo) | UT-03 |
| Clique | Peça própria, destino, outra peça, mesma casa | Callbacks na ordem; tabuleiro não interativo ignora | UT-04 |
| Teclado | Setas, Enter, Espaço, Esc | Foco percorre casas e ativa como o clique | UT-05 |
| Arrastar | Soltar em destino e fora do tabuleiro | `OnDragMove` só em destino; soltar fora é ignorado | UT-06 |
| Promoção | Quatro escolhas, atalhos, Esc | Callback da peça escolhida; cancelar não escolhe; foco inicial na dama | UT-07 |
| Contraste | Tokens de casas, contornos e destaques | Razões ≥ 3:1 nos pares definidos | UT-08 |
| Layout responsivo | Largura de 390px | Sem rolagem horizontal; tabuleiro quadrado (classes utilitárias esperadas) | UT-09 |
| Sem legado | Marcação dos componentes | Sem `<style>` inline, sem MudBlazor | UT-09 |
| Jornada | Partida curta jogada por cliques no tabuleiro | Lances legais aplicados, destaque de último lance, promoção escolhida | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — componentes novos.

### 7.2 Testes Unitários
- **UT-01** — Dado as 12 combinações de cor e tipo, então cada `ChessPiece` renderiza um `<svg role="img">` com o título em português ("Rei branco", "Peão preto", …), 64×64 de `viewBox` e as cores do conjunto.
- **UT-02** — Dado `Position.Start` com orientação branca e preta, então há 64 casas `data-square`, 32 peças, coordenadas a–h/1–8 na ordem certa e a fileira 1 fica embaixo para as brancas e em cima para as pretas.
- **UT-03** — Dado casa selecionada, destinos (vazios e com captura), último lance e rei em xeque, então cada estado tem marcação própria (atributo e texto no rótulo, além da cor).
- **UT-04** — Dado clique em uma peça e depois em um destino, então `OnSquareActivated` é chamado na ordem; com `Interactive=false` nenhum callback é chamado.
- **UT-05** — Dado setas, Enter, Espaço e Esc em uma casa focada, então o foco se move pela grade (e não sai dela), Enter/Espaço ativam a casa e Esc dispara o cancelamento da seleção.
- **UT-06** — Dado arrastar uma peça e soltá-la em um destino ou fora do tabuleiro, então `OnDragMove` só é chamado com origem e destino válidos.
- **UT-07** — Dado `PromotionPicker`, então há quatro botões (Dama, Torre, Bispo, Cavalo), as teclas Q/R/B/N escolhem a peça, Esc chama `OnCancel`, o foco inicial fica na dama e há `role="dialog"` com rótulo.
- **UT-08** — Dado o CSS de entrada, então os tokens de contorno de peça e de casa atendem razão ≥ 3,0 em todos os pares definidos (branca e preta × casa clara e escura; destaques × casas).
- **UT-09** — Dado a marcação, então não há `<style>` inline nem `mud-`, o contêiner do tabuleiro usa largura fluida com `aspect-square` e nenhuma classe de largura fixa maior que 390px.

### 7.3 Testes de Integração
- **IT-01** — Dado o `ChessBoard` ligado a um `ChessGame` de teste que responde aos callbacks (seleciona, destina, promove), quando se jogam `e2→e4`, `e7→e5` e uma promoção pelo tabuleiro, então a posição exibida acompanha o jogo e o último lance é destacado.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
- **E2E-01** — Jornada (bUnit): tabuleiro na posição inicial, jogar `e2→e4` por clique e depois `e7→e5` para as pretas com orientação invertida, chegar a uma promoção `e8` e escolher dama; o tabuleiro mostra as peças, o destaque do último lance e a escolha.

### 7.6 Outros
- Revisão visual (H2): 390px e 1280px contra `docs/design/stitch/chess/arena-desktop.png` (corrigindo o contraste das pretas) e `arena-mobile.png` (tabuleiro completo).
- Lighthouse Acessibilidade ≥ 90 na arena (com a SPEC-0057).
- `tools/tailwind/build.sh --check` sem diferença no CSS gerado.

**Dublês e dados de teste:** `BunitContext` com `JSInterop` frouxo para foco; posições em FEN; `ChessGame` real do módulo Chess.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; os componentes só aparecem com a arena (SPEC-0057).
- **Dados/schema:** N/A
- **Compatibilidade:** Novos tokens não alteram os existentes.
- **Observabilidade:** N/A
- **Rollback:** Reverter o PR; nada depende dos componentes até a SPEC-0057.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] As peças vêm do Stitch ou de conjunto livre? — Do Stitch (folha SVG gerada e transcrita em `docs/design/stitch/chess/pecas.md`); se o contraste real não passar no teste de tokens, ajustam-se as cores por token (Architect, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0055`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
