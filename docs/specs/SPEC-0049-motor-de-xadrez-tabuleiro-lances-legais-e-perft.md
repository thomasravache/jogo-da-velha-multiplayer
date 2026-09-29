---
id: SPEC-0049
title: "Motor de xadrez: tabuleiro, lances legais e perft"
tier: full
type: foundation
user_facing: false
status: proposed
created: 2026-09-29
parent: SPEC-0046
depends_on: []
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Chess/TicTacToe.Modules.Chess.csproj, src/TicTacToe/TicTacToe.Modules.Chess/Rules/**, TicTacToe.sln, tests/TicTacToe.Tests/TicTacToe.Tests.csproj, tests/TicTacToe.Tests/ChessSquareTests.cs, tests/TicTacToe.Tests/ChessPositionTests.cs, tests/TicTacToe.Tests/ChessMoveGenerationTests.cs, tests/TicTacToe.Tests/ChessPerftTests.cs, tests/TicTacToe.Tests/ChessModuleBoundaryTests.cs]
adrs: [ADR-0010]
external: []
size: M
approved_by:
approved_at:
---

# SPEC-0049 — Motor de xadrez: tabuleiro, lances legais e perft

## 1. Visão Geral
Cria o módulo `TicTacToe.Modules.Chess` com o **motor de regras**: tabuleiro, casas, peças, posição a partir de FEN, geração de **lances legais** (xeque, roque, en passant, promoção) e `Perft` para provar a correção. É a base pura (sem I/O, sem Blazor) que as demais specs de xadrez usam.

## 2. Motivação & Escopo
**Motivação:** Xadrez exige regras precisas e verificáveis. O ADR-0010 decidiu escrever o motor no projeto; esta spec o entrega, com o módulo isolado (ADR-0004) e validado por contagens de perft.

**Objetivos (dentro do escopo):**
- Projeto `TicTacToe.Modules.Chess` (net10.0, mesmas propriedades de análise dos demais) adicionado à solução e referenciado pelo projeto de testes.
- Tipos: `PieceColor`, `PieceType`, `Piece`, `Square` (a1 = 0 … h8 = 63, `Parse`/`ToString`), `Move` (origem, destino, promoção opcional).
- `Position`: imutável, cria-se com `Position.Start` ou `Position.FromFen`; expõe peça por casa, lado a jogar, direitos de roque, alvo de en passant, relógios de meio-lance e de lance, `ToFen()`.
- `Position.LegalMoves()` completa: movimento de todas as peças, capturas, xeque e cravadas, roque (direitos, casas livres, sem passar por casa atacada nem sair de xeque), en passant (inclusive xeque descoberto), promoção nas quatro peças.
- `Position.Apply(Move)` devolve a nova posição atualizando lado, roque, en passant e relógios; lance ilegal lança `ArgumentException`.
- `Position.IsInCheck(PieceColor)` e `Perft.Count(Position, depth)`.
- Teste de fronteira: o módulo não referencia Gameplay, Matchmaking nem Web.

**Não-objetivos (fora do escopo):**
- SAN, histórico, fim de jogo, repetição e regra dos 50 lances (SPEC-0050).
- Relógio, sessão, robô e qualquer interface.
- Chess960 (roque com torres em colunas variáveis) — fora do escopo; o motor não o suporta.
- Desempenho de bitboards: a estrutura pode ser simples.

## 3. Dependências
- **Implementações necessárias:** N/A — módulo novo, sem dependências de outras specs.
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** Nenhum pacote novo; o projeto usa só a BCL.

## 4. Decisão Arquitetural
**Contexto:** Modular monolith (ADR-0004): módulos independentes que só se combinam na Web; propriedades de compilação estrita e analisadores (ADR-0005; `Directory.Build.props`). Referência: `TicTacToe.Modules.Gameplay` para estrutura de projeto e `GameSession`/`AiPlayer` para o estilo de domínio.

**Decisão:** Motor próprio no novo módulo, com `Position` imutável e funções puras; validado por perft (ADR-0010).

**Justificativa:** Imutabilidade torna o motor simples de testar, de usar na busca do robô e seguro entre circuitos; o perft prova a geração de lances.

**Desvio do padrão existente:** Nenhum (módulo novo dentro do padrão modular; o ADR-0010 registra a escolha de não usar biblioteca).

**Alternativas descartadas:** ChessRealms.Engine e Rudzoft.ChessLib (ADR-0010).

**ADRs:** ADR-0010

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** O perft de profundidade padrão do CI (~350 mil nós no total) roda em poucos segundos; o perft pesado é `Category=Slow`; `LegalMoves` de uma posição típica em microssegundos a poucos milissegundos.
- **Segurança:** FEN é entrada não confiável (virá de testes, nunca do usuário nesta spec): `FromFen` valida campos e devolve erro sem exceção inesperada.
- **Privacidade e dados pessoais:** N/A — sem dados pessoais.
- **Disponibilidade e resiliência:** Funções puras e imutáveis, sem estado global; seguras para uso concorrente.
- **Acessibilidade (UI):** N/A — sem interface.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `TicTacToe.Modules.Chess: Square, Piece, Move, Position, Perft`

```text
namespace TicTacToe.Modules.Chess

enum PieceColor { White, Black }
enum PieceType  { Pawn, Knight, Bishop, Rook, Queen, King }
readonly record struct Piece(PieceColor Color, PieceType Type)

readonly record struct Square(int Index)                 // 0..63, a1 = 0, b1 = 1, ..., h8 = 63
  int File => Index % 8;   int Rank => Index / 8;
  static Square Parse(string algebraic)                  // "e4"; inválido → FormatException
  static bool TryParse(string text, out Square square)
  override string ToString()                             // "e4"

readonly record struct Move(Square From, Square To, PieceType? Promotion = null)
  // roque = o REI anda duas casas (e1→g1, e1→c1, e8→g8, e8→c8); a torre é movida por Apply. En passant = peão para a casa de passagem.

sealed class Position
  static Position Start { get; }                          // posição inicial padrão
  static Position FromFen(string fen)                     // FEN com 6 campos; inválido → FormatException
  static bool TryFromFen(string fen, out Position? position)
  string ToFen()
  PieceColor SideToMove { get; }
  Piece? PieceAt(Square square)
  bool CanCastle(PieceColor color, bool kingSide)
  Square? EnPassantTarget { get; }                        // casa por onde o peão passou ao avançar duas casas (como no campo 4 do FEN)
  int HalfmoveClock { get; }   int FullmoveNumber { get; }
  IReadOnlyList<Move> LegalMoves()                        // lances legais do lado a jogar
  bool IsInCheck(PieceColor color)
  Position Apply(Move move)                               // ArgumentException se ilegal
  bool IsLegal(Move move)

static class Perft
  static long Count(Position position, int depth)         // depth >= 0; 0 = 1 nó
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Casas | Texto "e4", "a1", "h8", inválidos | Índices corretos e ida e volta; inválido não é aceito | UT-01 |
| FEN | FEN válido, com e sem direitos, com en passant, e inválidos | Ida e volta idêntica; inválido → erro tratado | UT-02 |
| Movimento das peças | Cavalo, bispo, torre, dama, rei, peão em posições isoladas e bloqueadas | Conjunto exato de lances | UT-03 |
| Roque | Direitos, caminho livre, casas atacadas, rei em xeque, torre movida | Roque só quando permitido | UT-04 |
| En passant | Captura possível e captura que deixaria o rei em xeque | Lance gerado só quando legal | UT-05 |
| Promoção | Peão na sétima fileira, com e sem captura | Quatro lances de promoção por destino | UT-06 |
| Xeque e cravadas | Rei em xeque simples e duplo; peça cravada | Só lances que resolvem o xeque; cravada só anda na linha | UT-07 |
| Aplicar lance | Lances comuns, roque, en passant, promoção, ilegal | Estado atualizado (lado, roque, en passant, relógios); ilegal lança erro | UT-08 |
| Xeque | Posições com e sem xeque | IsInCheck correto para cada cor | UT-09 |
| Correção global | Perft das posições de referência | Contagens iguais às publicadas (CI: profundidade padrão; local: `IT-01b`) | IT-01, IT-01b |
| Fronteira do módulo | Referências dos projetos Chess, Gameplay e Matchmaking | Chess sem Gameplay, Matchmaking nem Web; os outros sem Chess | IT-02 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — módulo novo, sem comportamento existente a preservar.

### 7.2 Testes Unitários
- **UT-01** — Dado textos "a1", "e4", "h8" e inválidos ("i9", "", "e", "e44"), então `Parse`/`ToString` são inversos e os inválidos falham em `TryParse`.
- **UT-02** — Dado a posição inicial, uma posição com en passant, uma sem direitos de roque e FENs inválidos (campos faltando, rei ausente ou duplicado, peão na 1ª ou 8ª fileira, fileira com soma ≠ 8, rei do lado que não joga em xeque), então `FromFen`/`ToFen` fazem a ida e volta e os inválidos são rejeitados sem exceção inesperada.
- **UT-03** — Dado posições mínimas de cada peça (livre, bloqueada por aliada, com capturas), então `LegalMoves` devolve exatamente os lances esperados.
- **UT-04** — Dado posições de roque (livre, caminho ocupado, casa de passagem atacada, rei em xeque, direito perdido, torre capturada), então o roque só é gerado quando permitido, nos dois lados e nas duas cores, com o lance codificado como o rei andando duas casas (`LegalMoves` contém `e1→g1`) e `Apply` movendo a torre.
- **UT-05** — Dado en passant disponível e o caso em que a captura exporia o rei na mesma fileira, então o lance é gerado apenas no primeiro caso.
- **UT-06** — Dado um peão na sétima fileira (sem e com captura), então há um lance por peça de promoção (dama, torre, bispo, cavalo) para cada destino.
- **UT-07** — Dado rei em xeque simples, xeque duplo e uma peça cravada, então só saem lances que resolvem o xeque (no duplo, só o rei anda) e a peça cravada só se move ao longo da linha da cravada.
- **UT-08** — Dado `Apply` para um lance comum, um avanço duplo, um roque, um en passant e uma promoção, então lado, direitos de roque, alvo de en passant, relógio de meio-lance (zera em captura e peão) e número do lance são atualizados; lance ilegal lança `ArgumentException`.
- **UT-09** — Dado posições com e sem xeque por cada tipo de peça, então `IsInCheck` responde certo para brancas e pretas.

### 7.3 Testes de Integração
- **IT-01** — Dado as posições de referência da página de resultados de perft da chessprogramming wiki, quando `Perft.Count` roda na profundidade padrão de CI (inicial até 4; Kiwipete e posições 3 a 6 até 3), então as contagens são as publicadas: inicial `rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1` = 20, 400, 8.902, 197.281; Kiwipete `r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1` = 48, 2.039, 97.862; posição 3 `8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1` = 14, 191, 2.812; posição 4 `r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1` = 6, 264, 9.467; posição 5 `rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8` = 44, 1.486, 62.379; posição 6 `r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10` = 46, 2.079, 89.890.
- **IT-01b** — Dado as mesmas posições (`Category=Slow`, fora do CI padrão; executado localmente pelo implementador, com evidência no PR), quando `Perft.Count` roda nas profundidades maiores, então as contagens publicadas se confirmam: inicial na 5 = 4.865.609; Kiwipete na 4 = 4.085.603; posição 3 na 4 = 43.238 e na 5 = 674.624; posição 4 na 4 = 422.333; posição 5 na 4 = 2.103.487; posição 6 na 4 = 3.894.594.
- **IT-02** — Dado os `.csproj` de Chess, Gameplay e Matchmaking, então Chess não referencia Gameplay, Matchmaking nem Web, e Gameplay e Matchmaking não referenciam Chess (`Category=Architecture`).

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
N/A — `user_facing: false`.

### 7.6 Outros
- O tempo total dos testes novos desta spec no CI (IT-01 de profundidade padrão) fica abaixo de 20 s; o perft pesado é `IT-01b` (`Category=Slow`) e roda localmente.
- Confirmar as contagens contra a fonte pública (https://www.chessprogramming.org/Perft_Results) ao escrever os testes.

**Dublês e dados de teste:** Posições em FEN escritas nos testes (sem mocks).

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; módulo sem consumidores até as specs seguintes.
- **Dados/schema:** N/A
- **Compatibilidade:** N/A — código novo.
- **Observabilidade:** N/A — biblioteca pura.
- **Rollback:** Reverter o PR; nada depende do módulo ainda.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Motor próprio ou biblioteca? — Motor próprio, decisão delegada ao Architect e registrada no ADR-0010 (thomas, 2026-09-29)
- - [x] Chess960 entra? — Não, só xadrez tradicional por enquanto (thomas, 2026-09-29)

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0049`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
