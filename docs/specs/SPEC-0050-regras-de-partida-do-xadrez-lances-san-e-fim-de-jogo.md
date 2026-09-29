---
id: SPEC-0050
title: "Regras de partida do xadrez: lances, SAN e fim de jogo"
tier: full
type: feature
user_facing: false
status: proposed
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0049]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Chess/Game/**, tests/TicTacToe.Tests/ChessGameTests.cs, tests/TicTacToe.Tests/ChessSanTests.cs, tests/TicTacToe.Tests/ChessGameEndTests.cs]
adrs: [ADR-0010]
external: []
size: M
approved_by:
approved_at:
---

# SPEC-0050 — Regras de partida do xadrez: lances, SAN e fim de jogo

## 1. Visão Geral
Modela a **partida de xadrez** sobre o motor: aplicar lances jogados por casas, registrar o histórico com notação algébrica (SAN), capturas por cor e detectar o fim do jogo por regras (xeque-mate, afogamento, material insuficiente, regra dos 50 lances e tripla repetição).

## 2. Motivação & Escopo
**Motivação:** A interface e a sessão precisam de um objeto que entenda a partida (lance jogado, notação, capturas, resultado). O fim por regras é lógica pura, testável sem relógio nem rede.

**Objetivos (dentro do escopo):**
- `ChessGame` (histórico de `ChessMove`, `Position` atual, resultado) com `TryPlay(from, to, promotion)`, `LegalMoves`, `DestinationsFrom`, `NeedsPromotion`.
- SAN completo: peça, captura (`x`), desambiguação, promoção (`=Q`), roque (`O-O`, `O-O-O`), xeque (`+`) e mate (`#`).
- Capturas por cor (`CapturedBy`), útil para as telas.
- Fim por regras: xeque-mate, afogamento, material insuficiente, 50 lances (100 meios-lances) e tripla repetição (automáticos).
- `ChessResult` e `ChessEndReason` (inclui motivos que a sessão aplicará: tempo, abandono, desconexão).
- `MovesSan` (lista de SAN e texto separado por espaço) para persistência.

**Não-objetivos (fora do escopo):**
- Relógio, sessão, comandos de jogador e presença (SPEC-0051, SPEC-0052).
- Proposta e aceite de empate (decisão do produto: fora do escopo).
- Leitura de SAN/PGN como recurso do produto (os testes usam um auxiliar próprio); nome de abertura; avaliação de material como "vantagem".

## 3. Dependências
- **Implementações necessárias:** SPEC-0049 — motor com `Position`, lances legais e `Apply`.
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** Segue o estilo de `GameSession` (estado de partida, resultado, contagem de lances) e `HistoryAnalysis` (funções puras) do jogo da velha; motor da SPEC-0049.

**Decisão:** `ChessGame` mutável e não concorrente (o lock fica na sessão, SPEC-0052) sobre `Position` imutável; SAN e fim de jogo como funções puras em classes estáticas testáveis.

**Justificativa:** Mantém a lógica de partida separada da de sessão, permitindo testar cada uma isoladamente.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Guardar SAN só na interface (não serviria à persistência e ao robô); tratar fim de jogo na sessão (mistura regras com tempo e conexão).

**ADRs:** ADR-0010

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** `TryPlay` e detecção de fim em milissegundos; repetição por comparação de chaves de posição, sem varrer texto.
- **Segurança:** Entrada só via casas e peça de promoção; lance ilegal é recusado sem alterar o estado.
- **Privacidade e dados pessoais:** N/A — sem dados pessoais.
- **Disponibilidade e resiliência:** Estado só muda em lance válido; após o fim, `TryPlay` recusa.
- **Acessibilidade (UI):** N/A — sem interface (o SAN alimenta a lista de lances acessível da SPEC-0057).
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `TicTacToe.Modules.Chess: ChessGame, ChessMove, ChessResult, ChessOutcome, ChessEndReason`

```text
enum ChessOutcome   { WhiteWins, BlackWins, Draw }
enum ChessEndReason { Checkmate, Stalemate, InsufficientMaterial, FiftyMoveRule, ThreefoldRepetition,
                      Timeout, Resignation, Abandon, Disconnect }       // os quatro últimos são aplicados pela sessão
record ChessResult(ChessOutcome Outcome, ChessEndReason Reason)
record ChessMove(Move Move, string San, PieceType Piece, PieceType? Captured, bool IsCheck, bool IsCheckmate)

sealed class ChessGame
  ChessGame(Position? start = null)
  Position Position { get; }                                  // posição atual
  IReadOnlyList<ChessMove> Moves { get; }                     // histórico
  ChessResult? Result { get; }                                // nulo em andamento
  bool IsOver { get; }
  IReadOnlyList<Move> LegalMoves()
  IReadOnlyList<Square> DestinationsFrom(Square from)
  bool NeedsPromotion(Square from, Square to)
  bool TryPlay(Square from, Square to, PieceType? promotion, out ChessMove? played)   // falso: ilegal, fim, promoção ausente/ inválida
  IReadOnlyList<PieceType> CapturedBy(PieceColor color)
  bool HasMatingMaterial(PieceColor color)                    // falso para K, K+B, K+N (usado pela sessão na vitória por tempo)
  string MovesSan { get; }                                    // "e4 e5 Nf3 ..." (sem números)
  void End(ChessResult result)                                // fim externo (tempo, abandono, desconexão): só se em andamento

Regras de fim (automáticas, após cada lance)
  xeque-mate → vitória de quem jogou; afogamento → empate;
  material insuficiente: K vs K, K+B vs K, K+N vs K, B vs B da mesma cor de casa (e múltiplos bispos da mesma cor de casa) → empate
  HalfmoveClock >= 100 → empate por 50 lances; posição repetida 3 vezes (peças, lado a jogar, direitos de roque, en passant possível) → empate
SAN: peça (K Q R B N; peão sem letra), desambiguação por coluna, depois fileira, depois casa, "x" em captura (peão inclui a coluna de origem),
     promoção "=Q", roque "O-O"/"O-O-O", sufixo "+" (xeque) ou "#" (mate)
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Jogar lance | Lance legal e ilegal; promoção obrigatória | Estado avança só no legal; promoção ausente é recusada | UT-01 |
| SAN | Capturas, roques, promoções, xeque, mate | Texto conforme o contrato | UT-02 |
| Desambiguação | Dois cavalos/torres/damas ao mesmo destino | Coluna, depois fileira, depois casa | UT-03 |
| Xeque-mate e afogamento | Mate do pastor, mate do louco, posição de afogamento | Resultado e motivo corretos | UT-04 |
| Material insuficiente | K×K, K+B×K, K+N×K, bispos da mesma cor | Empate automático; K+N×K+N e K+B×K+B de cores diferentes não | UT-05 |
| 50 lances | Relógio de meio-lance em 99 e lance neutro | Empate ao chegar a 100; captura ou peão zera | UT-06 |
| Repetição | Sequência de cavalos repetida três vezes | Empate ao terceiro aparecimento da posição | UT-07 |
| Capturas | Sequência com capturas e en passant | Peças capturadas por cor corretas | UT-08 |
| Fim externo | `End` durante e depois do jogo | Só encerra se em andamento; jogadas depois são recusadas | UT-09 |
| Partidas completas | Jogos conhecidos em SAN | Repetidos até o resultado esperado; `MovesSan` reproduz o texto | IT-01 |
| Persistência do texto | `MovesSan` de um jogo e sua reexecução | Reconstrói a mesma posição final (FEN) | IT-02 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — código novo sobre o motor da SPEC-0049.

### 7.2 Testes Unitários
- **UT-01** — Dado um `ChessGame` novo, quando joga `e2→e4`, então o histórico tem um lance, o lado a jogar muda e `TryPlay` de um lance ilegal, de casa vazia ou fora da vez retorna falso sem alterar o estado; promoção sem peça informada (ou com rei/peão) é recusada.
- **UT-02** — Dado lances de peça, captura de peão (`exd5`), roque curto e longo, promoção com captura (`exf8=Q+`), xeque e mate, então o SAN gerado segue o contrato.
- **UT-03** — Dado dois cavalos, duas torres e duas damas que alcançam o mesmo destino, então o SAN desambigua por coluna, depois por fileira, depois pela casa completa.
- **UT-04** — Dado o mate do pastor, o mate do louco e uma posição de afogamento, então o resultado é vitória de quem deu o mate ou empate, com `Checkmate`/`Stalemate`.
- **UT-05** — Dado K×K, K+B×K, K+N×K e bispos de cor de casa igual, então o empate por `InsufficientMaterial` é automático; K+N×K+N, K+N+N×K, K+P×K e bispos de cores diferentes seguem em jogo; `HasMatingMaterial(color)` é falso só para K, K+B e K+N.
- **UT-06** — Dado uma posição com relógio de meio-lance 99, quando sai um lance neutro, então há empate por `FiftyMoveRule`; captura ou lance de peão zera o relógio; e se o lance que chega a 100 for xeque-mate, o mate prevalece sobre o empate.
- **UT-07** — Dado a sequência Cf3 Cf6 Cg1 Cg8 repetida, então a terceira ocorrência da posição inicial encerra em `ThreefoldRepetition`; a mesma disposição com direitos de roque diferentes não conta como repetição; e o alvo de en passant só entra na chave da posição quando existe captura en passant legal (um avanço duplo sem peão adversário ao lado não quebra a repetição).
- **UT-08** — Dado uma sequência com capturas comuns e en passant, então `CapturedBy(White)` e `CapturedBy(Black)` listam as peças certas.
- **UT-09** — Dado `End` com resultado durante o jogo e depois do fim, então só o primeiro encerra; `TryPlay` depois do fim retorna falso.

### 7.3 Testes de Integração
- **IT-01** — Dado partidas conhecidas em SAN (a "Ópera" de Morphy — desambiguação `Nbd7`, roque longo e mate `Rd8#`; uma partida com roque, en passant e promoção; uma empatada por afogamento), quando reexecutadas com um auxiliar **de teste** que acha o lance legal cujo SAN gerado é igual ao texto, então o resultado e o `MovesSan` são os esperados (a desambiguação só considera lances legais: peça cravada não conta).
- **IT-02** — Dado o `MovesSan` de uma partida, quando reexecutado pelo mesmo auxiliar de teste a partir da posição inicial, então a posição final (FEN) coincide com a original (prova só a ida e volta; a correção do SAN é da IT-01; base para a persistência da SPEC-0053).

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
N/A — `user_facing: false`.

### 7.6 Outros
- N/A

**Dublês e dados de teste:** Sequências de SAN e FENs escritas nos testes; nenhum mock.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; sem consumidores até as specs seguintes.
- **Dados/schema:** N/A
- **Compatibilidade:** N/A — código novo.
- **Observabilidade:** N/A — biblioteca pura.
- **Rollback:** Reverter o PR.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] "Propor empate" entra? — Não (thomas, 2026-09-29); por isso não há acordo de empate no modelo
- - [x] Repetição e 50 lances são automáticos? — Sim, automáticos (empate imediato), simplificação registrada pelo Architect (2026-09-29)

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0050`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
