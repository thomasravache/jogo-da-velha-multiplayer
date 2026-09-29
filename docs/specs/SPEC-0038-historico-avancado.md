---
id: SPEC-0038
title: Histórico avançado
tier: full
type: feature
user_facing: true
status: in-progress
created: 2026-09-29
parent: SPEC-0035
depends_on: [SPEC-0032, SPEC-0036, SPEC-0037]
consumes_contract: []
contract_version: 4
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/HistoryModels.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/HistoryAnalysis.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/History.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/History.razor.css, src/TicTacToe/TicTacToe.Web/Components/Ui/StatTile.razor, src/TicTacToe/TicTacToe.Web/Components/Ui/Pager.razor, tests/TicTacToe.Tests/HistoryAnalysisTests.cs, tests/TicTacToe.Tests/HistoryQueryTests.cs, tests/TicTacToe.Tests/HistoryAdvancedUiTests.cs, tests/TicTacToe.Tests/HistoryCyberArenaTests.cs, tests/TicTacToe.Tests/TailwindDesignSystemTests.cs]
adrs: [ADR-0009]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0038 — Histórico avançado

## 1. Visão Geral
Transforma o histórico em uma visão **pessoal e explorável**: resultado do ponto de vista do jogador (Vitória, Derrota, Deu velha, por W.O.), motivo do fim, duração, filtros com contagem, busca por adversário, ordenação, paginação e **resumo de desempenho** (total, taxa de vitória, sequência, recorde de vitória mais rápida e tempo médio por lance). Usa os dados da SPEC-0036 e a identidade da SPEC-0037.

## 2. Motivação & Escopo
**Motivação:** a página mostra as 10 últimas partidas globais, sem saber quais são do jogador. O Stitch traz o histórico como ferramenta de acompanhamento do próprio desempenho.

**Objetivos (dentro do escopo):**
- Referências: `docs/design/stitch/historico-desktop.html` e `historico-mobile.html`.
- **Escopo pessoal**: partidas em que o `PlayerId` do jogador é X ou O (inclui modo Solo); alternância para **Todos os jogadores** (histórico global, incluindo partidas antigas sem `PlayerId`).
- Resultado pela ótica do jogador: **Vitória**, **Derrota**, **Deu velha**, com a marca **W.O.** quando o fim foi por tempo, abandono ou desconexão (`Timeout`, `Abandon`, `Disconnect`).
- Colunas: resultado, duelo (o jogador local marcado "Você"), **motivo do fim** ("3 em linha horizontal/vertical/diagonal principal/secundária", "Tempo esgotado", "Abandono", "Desconexão do oponente", "Grid completo sem vencedor"), duração, data e hora.
- **Filtros** com contagem: Todas, Vitórias, Derrotas, Empates, Por W.O. (as contagens cobrem todo o histórico do escopo, não só a página).
- **Busca** por nome do adversário (contém, sem diferenciar maiúsculas), **ordenação** (mais recentes, menor duração, por resultado) e **paginação** (10 por página).
- **Resumo**: total de duelos, taxa de vitória com "NV • ND • NE", sequência atual de vitórias, vitória mais rápida por linha, tempo médio por lance.
- Partidas antigas com campos nulos exibem "—".

**Não-objetivos (fora do escopo):**
- ELO por jogador, "Top X% global", "Sincronizado há", "Logs EF", "Fila ranqueada", lance destaque com mini tabuleiro (backlog H/E da SPEC-0035).
- Painel de desempenho no lobby (backlog N).
- Exportar dados.
- Mudar a gravação de partidas (SPEC-0036/0037) ou o ranking (SPEC-0039).

## 3. Dependências
- **Implementações necessárias:** SPEC-0032 (página e visual), SPEC-0036 (duração, motivo, linha vencedora, lado vencedor, modo) e SPEC-0037 (`PlayerId` nas partidas e no lobby).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** `GameResultService.GetRecentAsync` (SPEC-0010) e a página `History` (SPEC-0032); consultas EF no módulo `Gameplay`; índices em `PlayerXId`/`PlayerOId` (SPEC-0037).

**Decisão:** consulta paginada no módulo `Gameplay` (`GetHistoryAsync`) que filtra, busca, ordena e pagina no banco; cálculos do resumo e classificações em funções puras (`HistoryAnalysis`) testáveis sem banco; a página só apresenta.

**Justificativa:** mantém regras fora da UI (ADR-0006) e escala melhor que carregar tudo em memória.

**Desvio do padrão existente:** Nenhum (mesmo módulo e estilo de serviço).

**Alternativas descartadas:** filtrar em memória no componente (não escala, mistura regra e UI); `MudDataGrid`/biblioteca de tabelas (removida, ADR-0008).

**ADRs:** ADR-0009 (identidade usada como chave de leitura).

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** consulta paginada com filtro por índice de `PlayerXId`/`PlayerOId`; p95 < 200 ms para um jogador com 1.000 partidas, medido em teste de integração com dados sintéticos.
- **Segurança:** a busca é parametrizada (EF), sem SQL montado por texto; o `PlayerId` vem do serviço de identidade e não é exibido; o escopo pessoal não expõe partidas de outros jogadores.
- **Privacidade e dados pessoais:** exibe apenas nomes já visíveis hoje; o escopo global continua igual ao comportamento atual.
- **Disponibilidade e resiliência:** dados nulos de partidas antigas tratados; página vazia tem estado próprio; erro de consulta não deve derrubar o circuito (tratado como no restante do app).
- **Acessibilidade (UI):** filtros como grupo de opções navegável por teclado, campo de busca rotulado, tabela/lista semântica, paginação com `aria-label`, resultado nunca só por cor; contraste AA; Lighthouse ≥ 90.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `GameResultService.GetHistoryAsync`, `GameResultService.GetPlayerSummaryAsync` e página `/history`.

```text
enum HistoryScope   { Mine, All }
enum HistoryFilter  { All, Wins, Losses, Draws, WalkOvers }      // WalkOvers = EndReason ∈ {Timeout, Abandon, Disconnect}
enum HistorySort    { Recent, ShortestDuration, Result }         // Result: Vitória, Deu velha, Derrota; depois mais recentes

record HistoryQuery(Guid? PlayerId, HistoryScope Scope, HistoryFilter Filter, string? Opponent,
                    HistorySort Sort, int Page /*1-based*/, int PageSize = 10)

record HistoryItem(Guid Id, string PlayerXName, string PlayerOName, HistoryOutcome? Outcome /*Win|Loss|Draw; nulo no escopo global*/,
                   bool WalkOver, string Reason, int? DurationSeconds, GameMode? Mode, DateTime PlayedAtUtc,
                   bool? IAmX /*nulo no escopo global*/, string? WinnerSide /*"X"|"O"|nulo; Emenda v2*/,
                   string? WinnerName /*nome do vencedor ou nulo; Emenda v3*/)

record HistoryPage(IReadOnlyList<HistoryItem> Items, int TotalItems, int Page, int PageCount, HistoryCounts Counts)
record HistoryCounts(int All, int Wins, int Losses, int Draws, int WalkOvers)

record PlayerSummary(int Total, int Wins, int Losses, int Draws, double WinRatePercent,
                     int CurrentWinStreak, int? FastestWinSeconds, double? AverageSecondsPerMove)

Regras (HistoryAnalysis, funções puras)
  resultado do jogador = comparação entre o lado do jogador (X ou O, por PlayerId) e MatchResult.WinnerSide
  taxa de vitória     = vitórias / total (empates e derrotas contam como não vitória), 1 casa decimal
  sequência atual     = vitórias consecutivas a partir da partida mais recente; empate e derrota interrompem
  vitória mais rápida = menor duração entre vitórias com EndReason = Line (exclui W.O.)
  tempo por lance     = média de DurationSeconds/MoveCount entre partidas com ambos os campos
  motivo              = Line → "3 em linha horizontal|vertical|diagonal principal|diagonal secundária"
                        Draw → "Grid completo sem vencedor" · Timeout → "Tempo esgotado" ·
                        Abandon → "Abandono" · Disconnect → "Desconexão do oponente" · nulo → "—"
```

**Arquivos/módulos afetados:** ver `touches`. `Pager` e `StatTile` são primitivos novos em `Components/Ui`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Resultado do jogador | Jogador X vence / O vence / empate; homônimos | Vitória, Derrota ou Deu velha pelo lado, não pelo nome | UT-01 |
| Motivo do fim | Linha (linhas, colunas, diagonais), empate, W.O., abandono, desconexão, nulo | Texto conforme o contrato | UT-02 |
| Duração | 22 s, 72 s, nulo | "22s", "1m 12s", "—" | UT-03 |
| Resumo | Base com 14 V, 3 D, 1 E | Total 18, 77,8%, sequência, recorde e tempo por lance corretos | UT-04 |
| Filtros e contagens | Cada filtro | Lista restrita; contagens sobre o escopo e a busca por adversário, nunca sobre o filtro nem a página. No escopo Todos só valem Todas, Empates e W.O. (Vitórias/Derrotas tratados como Todas e contagem 0) | IT-01 |
| Busca | Parte do nome do adversário | Só partidas contra nomes que contêm o texto | IT-01 |
| Ordenação | Recentes, menor duração, por resultado | Ordem conforme o contrato | IT-01 |
| Paginação | 25 partidas, 10 por página | 3 páginas; navegação anterior/próxima; limites | IT-01 |
| Escopo | Pessoal × todos; partidas antigas | Pessoal só com `PlayerId`; antigas só em "Todos" | IT-02 |
| Resumo persistido | `GetPlayerSummaryAsync` | Mesmos números do cálculo puro sobre o banco | IT-03 |
| Interface | Filtros, busca, ordenação, paginação, resumo | Controles refletem e alteram a consulta; dados nulos como "—" | UT-05, UT-06 |
| Jornada | Histórico com 12 partidas | Resumo, filtro Vitórias, busca, ordenação e página 2 | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado `GetRecentAsync(10)`, então continua devolvendo as 10 partidas mais recentes em ordem decrescente (guarda; o escopo "Todos" reaproveita essa ordenação).

**Testes existentes afetados:** nenhum além dos da SPEC-0032 (`HistoryCyberArenaTests`), que passam a cobrir o cabeçalho e o estado vazio da nova página; ajustes justificados pela troca de escopo padrão (pessoal).

### 7.2 Testes Unitários
- **UT-01** — Dado partidas com vencedor X, vencedor O, empate e nomes iguais nos dois lados, então `HistoryAnalysis.Classify` devolve Vitória, Derrota ou Deu velha pelo lado do jogador (`WinnerSide`), independente do nome.
- **UT-02** — Dado cada `EndReason` e linhas vencedoras (0-1-2, 3-4-5, 6-7-8, 0-3-6, 1-4-7, 2-5-8, 0-4-8, 2-4-6) e o valor nulo, então o texto do motivo segue o contrato.
- **UT-03** — Dado 22, 72 e nulo, então o formatador retorna "22s", "1m 12s" e "—".
- **UT-04** — Dado a base de exemplo (14 V, 3 D, 1 E, durações e lances conhecidos), então total, taxa (77,8%), sequência atual, vitória mais rápida e tempo médio por lance são os esperados; base vazia devolve zeros e nulos sem exceção.
- **UT-05** — Dado a página com um serviço de teste, então cada filtro, a busca (com debounce), a ordenação e a paginação chamam a consulta com os parâmetros certos e refletem o retorno; o filtro ativo é exposto por `aria-checked`/`aria-pressed`.
- **UT-06** — Dado partidas antigas com duração e motivo nulos, então a linha exibe "—" nesses campos e nenhum erro.

### 7.3 Testes de Integração
- **IT-01** — Dado `GameplayDbContext` InMemory com 25 partidas variadas (vitórias, derrotas, empates, W.O. por tempo/abandono/desconexão), quando `GetHistoryAsync` roda com cada filtro, busca, ordenação e página, então lista, ordem, total, número de páginas e contagens são os esperados, e as contagens independem da página.
- **IT-02** — Dado partidas com e sem `PlayerId` e de outros jogadores, então `Scope=Mine` devolve só as do jogador e `Scope=All` devolve todas (inclusive as antigas).
- **IT-03** — Dado a base de exemplo, então `GetPlayerSummaryAsync` devolve os mesmos números que `HistoryAnalysis` calculado à mão.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
- **E2E-01** — Jornada do histórico (bUnit): com identidade e 12 partidas semeadas, a página mostra o resumo; ao escolher "Vitórias" a lista reduz e a contagem confere; ao buscar um adversário a lista filtra; ao ordenar por menor duração a ordem muda; a página 2 traz os itens restantes.

### 7.6 Outros
- Desempenho: consulta paginada com 1.000 partidas sintéticas < 200 ms (teste de integração com temporizador generoso, ambiente de CI).
- Revisão visual (H2): 390px e 1280px contra `historico-mobile.png`/`historico-desktop.png`; lista de omitidos conferida.
- Lighthouse Acessibilidade ≥ 90 em `/history`.

**Dublês e dados de teste:** EF InMemory com construtor de dados de partida (`MatchResultBuilder`), relógio e identidade injetados, serviço de histórico de teste para a UI.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; sem migration nesta spec (índices já criados na SPEC-0037).
- **Dados/schema:** N/A.
- **Compatibilidade:** `GetRecentAsync` permanece; o escopo "Todos os jogadores" reproduz o comportamento anterior.
- **Observabilidade:** tempo da consulta em log de depuração; revisão visual no PR.
- **Rollback:** `git revert` do PR.
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
- [x] O escopo padrão do histórico deve ser **pessoal com alternância para "Todos os jogadores"** (recomendado: é o que o Stitch mostra e mantém a visão global disponível), **somente pessoal**, ou **global como hoje**? — Pessoal por padrão, com alternância para "Todos os jogadores". (thomas, 2026-09-29)
- [x] "Por W.O." deve incluir **abandono** além de tempo esgotado e desconexão (recomendado: sim, todos são vitória por desistência do oponente), ou só tempo esgotado e desconexão? — Sim, "Por W.O." inclui abandono. (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [ ] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [ ] Escrever os testes `SPEC-0038:CH-01`, `SPEC-0038:UT-01`, `SPEC-0038:UT-02`, `SPEC-0038:UT-03`, `SPEC-0038:UT-04`, `SPEC-0038:UT-05`, `SPEC-0038:UT-06`, `SPEC-0038:IT-01`, `SPEC-0038:IT-02`, `SPEC-0038:IT-03`, `SPEC-0038:E2E-01` com a tag `SPEC-0038:<ID>`, em commits `test(...)` com `Refs: SPEC-0038`, tocando só `test_paths`
- [ ] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [ ] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0038`)

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [ ] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [ ] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [ ] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [ ] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável
- [ ] PR com `spec_graph.py pr SPEC-0038`, CI verde (G5) e aprovação do merge (H2)
- [ ] Relatório de Entrega, docs raiz e CHANGELOG (G7)

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0038`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
| 2 | 2026-09-29 | `HistoryItem` ganha `WinnerSide` (string?), `Outcome` e `IAmX` passam a anuláveis; `touches` inclui `HistoryCyberArenaTests.cs` | O escopo global precisa exibir o vencedor (como na SPEC-0032) sem depender do nome; os testes da SPEC-0032 precisam registrar a identidade e ajustar o escopo padrão | SPEC-0032 (testes); sem consumidores do contrato | thomas (autorização permanente, 2026-09-29) |
| 3 | 2026-09-29 | `HistoryItem` ganha `WinnerName`; contagens passam a respeitar a busca (não o filtro); filtros de resultado pessoal não se aplicam ao escopo Todos | Partidas antigas com homônimos precisam do nome do vencedor para o chip; abas com contagem incoerente com a lista confundem | sem consumidores do contrato | thomas (autorização permanente, 2026-09-29) |
| 4 | 2026-09-29 | `touches` inclui `TailwindDesignSystemTests.cs` (testes da SPEC-0034 que renderizam a página) | A página passa a depender de `PlayerIdentityService` e abre no escopo pessoal; os testes de renderização da SPEC-0034 precisam registrar a identidade e alternar para "Todos" | SPEC-0034 (testes); sem consumidores do contrato | thomas (autorização permanente, 2026-09-29) |
