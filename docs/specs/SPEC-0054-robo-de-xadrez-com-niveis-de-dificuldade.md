---
id: SPEC-0054
title: Robô de xadrez com níveis de dificuldade
tier: full
type: feature
user_facing: false
status: in-progress
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0050]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Chess/Bots/**, tests/TicTacToe.Tests/ChessBotTests.cs, tests/TicTacToe.Tests/ChessEvaluationTests.cs]
adrs: [ADR-0011]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0054 — Robô de xadrez com níveis de dificuldade

## 1. Visão Geral
Cria o **robô de xadrez**: abstração `IChessBot`, avaliação de posição (material e tabelas de casas) e um motor de busca próprio (negamax com poda alfa-beta) com dois níveis, **Fácil** e **Médio**, escolhidos por profundidade e ruído controlado.

## 2. Motivação & Escopo
**Motivação:** O usuário quer treinar contra um robô com dificuldade. O ADR-0011 decidiu um motor próprio atrás de `IChessBot`, sem processo externo nem licença GPL.

**Objetivos (dentro do escopo):**
- `IChessBot` (`Name`, `ChooseMoveAsync(position, ct)`), `ChessBotLevel { Easy, Medium }` e `ChessBots.Create(level, seed)`.
- `ChessEvaluation.Evaluate`: material, tabelas de casas por peça e pontuação de mate, do ponto de vista de quem joga.
- Nível **Fácil**: profundidade 1 com ruído (parte dos lances é escolhida ao acaso entre os legais, semente controlável).
- Nível **Médio**: negamax com poda alfa-beta, profundidade 3, ordenação de lances (capturas primeiro) e teto de nós configurável (`maxNodes`, padrão 200 mil); desempate por semente.
- Cancelamento respeitado; sempre devolve um lance legal quando existir algum.

**Não-objetivos (fora do escopo):**
- Integração na interface e atraso de jogada (SPEC-0058).
- Nível Difícil, Stockfish/UCI, livro de aberturas, tabelas de transposição e busca em segundo plano.
- Nomes de abertura ou análise de partida.

## 3. Dependências
- **Implementações necessárias:** SPEC-0050 (`ChessGame`/`Position` e lances legais; o robô só precisa da posição).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `AiPlayer` do jogo da velha (Minimax) e `BotTurnRunner` (SPEC-0040); módulo Chess independente (ADR-0010).

**Decisão:** Motor próprio em `Modules.Chess/Bots`, atrás de `IChessBot`, com semente e limite de nós para ser determinístico e barato (ADR-0011).

**Justificativa:** Suficiente para treino, sem dependências, testável; o Stockfish fica como implementação futura da mesma interface.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Stockfish via UCI e wrapper NuGet (ADR-0011).

**ADRs:** ADR-0011

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Médio: no máximo ~200 mil nós e menos de 2 s por lance em uma posição de meio de jogo no CI (medido no teste); Fácil: milissegundos.
- **Segurança:** N/A — sem entrada externa; o robô só devolve lances legais.
- **Privacidade e dados pessoais:** N/A — sem dados pessoais.
- **Disponibilidade e resiliência:** Cancelamento devolve o melhor lance encontrado até o momento (ou um legal); nunca trava a thread da interface (roda em `Task.Run`).
- **Acessibilidade (UI):** N/A — sem interface.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `TicTacToe.Modules.Chess: IChessBot, ChessBotLevel, ChessBots, ChessEvaluation`

```text
enum ChessBotLevel { Easy, Medium }

interface IChessBot
  string Name { get; }                                                      // "Robô Fácil 🤖", "Robô Médio 🤖"
  Task<Move?> ChooseMoveAsync(Position position, CancellationToken ct)      // nulo só se não houver lance legal

static class ChessBots
  static IChessBot Create(ChessBotLevel level, int? seed = null, int? maxNodes = null)   // mesma semente + mesma posição → mesmo lance; maxNodes só limita o nível Médio
  static string NameOf(ChessBotLevel level)

static class ChessEvaluation
  static int Evaluate(Position position)   // centipeões do ponto de vista de quem joga; mate = ±100000 (ajustado pela distância)
  Valores: peão 100, cavalo 320, bispo 330, torre 500, dama 900; tabelas de casas simples por peça.

Fácil:  20% dos lances aleatórios entre os legais; nos demais, melhor lance de profundidade 1
Médio:  negamax + alfa-beta, profundidade 3, capturas primeiro, limite de 200000 nós; ao cancelar devolve o melhor lance completo já avaliado
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Avaliação | Material igual, vantagem de peça, mate | Sinais e ordens de grandeza corretos; simétrica entre cores | UT-01 |
| Legalidade | Muitas posições de partidas jogadas pelo robô | Todo lance devolvido é legal; nulo só sem lances | UT-02 |
| Tática simples | Mate em 1, dama pendurada, peça pendurada própria | Médio dá o mate, captura a dama e não deixa a dama ser capturada | UT-03 |
| Determinismo | Mesma semente e posição | Mesmo lance; sementes diferentes podem variar no Fácil | UT-04 |
| Cancelamento e limites | Cancelar durante a busca; limite de nós | Devolve lance legal rápido; respeita o teto de nós | UT-05 |
| Força relativa | Partidas entre Médio, Fácil e aleatório com sementes fixas | Médio vence Fácil e Fácil vence o aleatório no agregado | IT-01 |
| Tempo por lance | Posição de meio de jogo no CI | Médio responde abaixo do limite | IT-02 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — código novo.

### 7.2 Testes Unitários
- **UT-01** — Dado posições com material igual, uma peça a mais e um mate dado, então `Evaluate` é ≈ 0, positivo para quem tem a peça (do ponto de vista de quem joga) e enorme para o mate; trocar as cores espelha o sinal.
- **UT-02** — Dado muitas posições geradas jogando robôs, então todo lance devolvido pertence a `LegalMoves`, e só há retorno nulo em posição sem lances.
- **UT-03** — Dado mate em 1, dama adversária desprotegida e uma dama própria ameaçada, então o robô Médio dá o mate, captura a dama e evita perder a dama.
- **UT-04** — Dado a mesma semente e a mesma posição, então o lance é o mesmo; o Fácil com sementes diferentes produz pelo menos dois lances distintos em uma posição de abertura.
- **UT-05** — Dado cancelamento no meio da busca do Médio e um `maxNodes` pequeno (por exemplo 500) em posição complexa, então devolve um lance legal em menos de 100 ms após o cancelamento e a busca nunca avalia mais nós que o teto (contador exposto para teste).

### 7.3 Testes de Integração
- **IT-01** — Dado 6 partidas Médio × Fácil (cores alternadas, sementes fixas) e 6 partidas Fácil × aleatório, cada uma limitada a 160 meios-lances com adjudicação por material no teto (quem tem mais material vence; igual empata), então o Médio vence o Fácil em pelo menos 5 e o Fácil vence o aleatório em pelo menos 4 (limiares calibrados pelo implementador; tempo total do teste registrado e abaixo de 60 s no CI).
- **IT-02** — Dado uma posição de meio de jogo, então o Médio responde em menos de 2 s no CI.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
N/A — `user_facing: false`.

### 7.6 Outros
- Os limiares de força (IT-01) são calibrados pelo implementador na primeira execução e registrados na spec por emenda se precisarem mudar; o critério é o Médio ser claramente mais forte que o Fácil.

**Dublês e dados de teste:** `Position`/`ChessGame` reais; semente fixa; sem mocks.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; sem consumidores até a SPEC-0058.
- **Dados/schema:** N/A
- **Compatibilidade:** N/A — código novo.
- **Observabilidade:** N/A — biblioteca pura.
- **Rollback:** Reverter o PR.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Quer jogar contra robô com dificuldades? — Sim; o Architect escolheu motor próprio com Fácil e Médio (ADR-0011) e deixou o Stockfish como evolução futura (thomas, 2026-09-29)
- - [x] Precisa de novo desenho no Stitch para o robô? — Não: o lobby reaproveita o cartão "Duelo contra IA" do jogo da velha; se quiser um visual dedicado, o prompt fica registrado no relatório do planejamento (Architect, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [ ] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [ ] Escrever `SPEC-0054:IT-01`, `SPEC-0054:IT-02`, `SPEC-0054:UT-01`, `SPEC-0054:UT-02`, `SPEC-0054:UT-03`, `SPEC-0054:UT-04`, `SPEC-0054:UT-05` com a tag `SPEC-0054:<ID>` em commits `test(...)` com `Refs: SPEC-0054` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [ ] Refactor mantendo tudo verde
- [ ] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes` e `verify SPEC-0054 --base origin/main`

**Fase final: Integração, entrega e documentação**
- [ ] Review independente (G4)
- [ ] Integração + CI verde (G5) e aprovação (H2)
- [ ] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
| G1 Red | PASS | verify SPEC-0054: Red antes do Green (ba08207), 7/7 testes do plano rastreados; 23 falharam por NotImplementedException | 2026-09-29 |
| G2 Green | PASS | dotnet test 610 verdes (1 pulado: perft pesado); format limpo; verify PASS; força calibrada: Médio venceu Fácil 38/40 e Fácil venceu aleatório 40/40 | 2026-09-29 |
| G3 Arquitetura | N/A | fronteira de módulos coberta por ChessModuleBoundaryTests (SPEC-0049) | 2026-09-29 |
| G4 Review | PASS | Review independente (subagente): PASS; achado maior (UT-05 vacuoso) corrigido com cancelamento por gancho de nós, mais mate em 2, maxNodes <= 0 e mate no relógio 100 (bug real corrigido). Dívidas: EasyChessBot/MediumChessBot públicos, LastNodeCount mutável por instância, sem detecção de repetição, Médio não converte finais simples (sem heurística de finais) | 2026-09-29 |
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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0054`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
