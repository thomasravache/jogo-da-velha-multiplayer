---
id: SPEC-0039
title: Ranking avançado
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0035
depends_on: [SPEC-0033, SPEC-0037, SPEC-0038]
consumes_contract: []
contract_version: 2
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/LeaderboardModels.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/LeaderboardAnalysis.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/AiPlayer.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Leaderboard.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Leaderboard.razor.css, tests/TicTacToe.Tests/LeaderboardAnalysisTests.cs, tests/TicTacToe.Tests/LeaderboardQueryTests.cs, tests/TicTacToe.Tests/LeaderboardAdvancedUiTests.cs, tests/TicTacToe.Tests/LeaderboardCyberArenaTests.cs, tests/TicTacToe.Tests/TailwindDesignSystemTests.cs]
adrs: [ADR-0009]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0039 — Ranking avançado

## 1. Visão Geral
Evolui o ranking de "top 10 por vitórias" para uma **classificação por jogador (identidade)** com **vitórias, derrotas, empates, aproveitamento e sequência de vitórias**, **paginação** e um cartão **"Sua posição"** com destaque "VOCÊ" na própria linha. Partidas contra o robô não contam no ranking global. Usa a identidade da SPEC-0037 e os componentes de paginação da SPEC-0038.

## 2. Motivação & Escopo
**Motivação:** hoje o ranking agrupa por apelido (homônimos se fundem, o robô entra como jogador) e mostra só vitórias e último triunfo. O Stitch traz a classificação completa e a posição do jogador.

**Objetivos (dentro do escopo):**
- Referências: `docs/design/stitch/ranking-desktop.html` e `ranking-mobile.html`.
- **Chave do jogador:** `PlayerId` quando existir; partidas antigas (sem `PlayerId`) agrupam por apelido e podem coexistir com a versão nova do mesmo jogador (limitação registrada).
- **Colunas:** posição, jogador, vitórias, derrotas, empates, aproveitamento, sequência atual de vitórias, último triunfo.
- **Ordem:** vitórias decrescentes, desempate por último triunfo mais recente (regra atual da SPEC-0017); só entra quem tem pelo menos 1 vitória.
- **Paginação** de 10 por página; a posição continua entre páginas (página 2 começa na 11ª).
- **Sua posição:** cartão com posição, vitórias/total, aproveitamento e sequência do jogador atual; a linha do jogador na tabela ganha "VOCÊ". Sem vitória ainda: "Vença uma partida para entrar no ranking".
- **Exclusões:** partidas em modo Solo (SPEC-0036) e partidas antigas em que algum lado é robô (nome no padrão de `AiPlayer.GetBotName`) não contam para ninguém.
- Pódio dos três primeiros mostra vitórias, aproveitamento e sequência.

**Não-objetivos (fora do escopo):**
- ELO, divisões, temporada e "Ciclo encerra em" (backlog E), status online/"há X" e contagem de duelistas ativos (backlog J), filtros Amigos/Minha divisão (backlog M/E).
- Mostrar jogadores com zero vitórias.
- Unificar automaticamente o histórico antigo (por apelido) com o novo (por `PlayerId`).
- Mudar a gravação de partidas.

## 3. Dependências
- **Implementações necessárias:** SPEC-0033 (página e visual), SPEC-0037 (`PlayerId` nas partidas) e SPEC-0038 (`Pager`, `StatTile` e o padrão de consulta paginada).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** `GameResultService.GetLeaderboardAsync(top)` agrupa em memória por `WinnerName` (SPEC-0017); consultas paginadas do histórico (SPEC-0038); `AiPlayer.GetBotName` (SPEC-0015).

**Decisão:** nova consulta `GetLeaderboardPageAsync` que projeta as colunas necessárias e delega o cálculo a funções puras (`LeaderboardAnalysis`); `GetLeaderboardAsync` permanece para compatibilidade dos testes existentes. Detecção de robô nos dados antigos por `AiPlayer.IsBotName`.

**Justificativa:** mantém regras fora da UI e testáveis; a agregação em memória é suficiente para a escala do projeto (medida em `IT-03`).

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** agregar por SQL com `GROUP BY` e janelas para sequência (complexidade sem necessidade agora); tabela materializada de estatísticas (precisaria de escrita adicional e migration).

**ADRs:** ADR-0009.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** agregação em memória sobre a projeção mínima; 10.000 partidas em menos de 500 ms no teste de integração (`IT-03`). Acima disso a decisão de materializar estatísticas vira nova spec.
- **Segurança:** consulta parametrizada; o `PlayerId` do jogador vem do serviço de identidade e não é exibido; nenhuma ação depende dele.
- **Privacidade e dados pessoais:** exibe apenas apelidos que já aparecem hoje; o nome exibido de um jogador com `PlayerId` é o apelido da partida mais recente.
- **Disponibilidade e resiliência:** dados nulos de partidas antigas tratados; sem partidas, estado vazio.
- **Acessibilidade (UI):** tabela semântica, paginação com `aria-label`, "VOCÊ" com texto (não só cor), pódio com "1º/2º/3º"; contraste AA; Lighthouse ≥ 90.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `GameResultService.GetLeaderboardPageAsync` e página `/leaderboard`.

```text
record LeaderboardQuery(Guid? MyPlayerId, int Page /*1-based*/, int PageSize = 10)

record LeaderboardEntry(int Position, string DisplayName, int Wins, int Losses, int Draws,
                        double WinRatePercent, int WinStreak, DateTime LastWinAtUtc, bool IsMe)

record LeaderboardPage(IReadOnlyList<LeaderboardEntry> Items, int TotalPlayers, int Page, int PageCount,
                       LeaderboardEntry? Me /*entrada do jogador atual, mesmo fora da página; nulo se não pontuou*/)

Regras (LeaderboardAnalysis, funções puras)
  chave           = PlayerId se houver; senão apelido (partida antiga)
  entra em partida se: Mode ≠ Solo E nenhum lado é robô (AiPlayer.IsBotName nos nomes de partidas sem Mode)
  vitória         = lado do jogador == WinnerSide (legado sem WinnerSide: WinnerName == apelido)
  aproveitamento  = vitórias / (vitórias + derrotas + empates) × 100, 1 casa decimal
  sequência       = vitórias consecutivas a partir da partida mais recente do jogador; empate e derrota interrompem
  ordem           = vitórias desc, depois LastWinAtUtc desc; só quem tem ≥ 1 vitória
  posição         = índice global + 1 (contínua entre páginas)
  nome exibido    = apelido da partida mais recente do jogador

record LeaderboardGame(string PlayerXName, string PlayerOName, Guid? PlayerXId, Guid? PlayerOId, string? WinnerName,
                       string? WinnerSide, GameMode? Mode, DateTime PlayedAtUtc)   // projeção mínima de MatchResult

LeaderboardAnalysis.Counts(LeaderboardGame) : bool                                  // regra de exclusão (solo/robô)
LeaderboardAnalysis.Rank(IEnumerable<LeaderboardGame>, Guid? myPlayerId) : IReadOnlyList<LeaderboardEntry>   // todos com ≥ 1 vitória, ordenados, com posição
LeaderboardAnalysis.Paginate(IReadOnlyList<LeaderboardEntry>, int page, int pageSize) : LeaderboardPage       // página limitada ao intervalo; Me = entrada com IsMe
  partida antiga homônima (mesmo apelido nos dois lados, sem WinnerSide) não atribui vitória a ninguém

AiPlayer.IsBotName(string) : bool   // reconhece os nomes de GetBotName (fácil, impossível, Minimax)
```

**Arquivos/módulos afetados:** ver `touches`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Agrupamento | Duas pessoas com o mesmo apelido e `PlayerId` diferentes | Duas entradas separadas | UT-01 |
| Partidas antigas | Sem `PlayerId` | Agrupadas por apelido | UT-01 |
| Robô | Solo e partidas antigas contra robô | Não contam para nenhum jogador | UT-01, IT-02 |
| Estatísticas | 5 V, 2 D, 1 E, sequência final | Aproveitamento 62,5% e sequência correta | UT-01 |
| Ordem e desempate | Mesma quantidade de vitórias | Último triunfo mais recente primeiro | UT-01 |
| Posição contínua | 25 jogadores, 10 por página | Página 2 começa em 11; 3 páginas | UT-02 |
| Interface | Pódio, tabela, "VOCÊ", "Sua posição", paginador | Colunas e textos conforme o contrato; três variações do cartão (ranqueado, sem vitória, sem identidade) | UT-03 |
| Estados | Carregando / vazio | Como na SPEC-0033 | UT-04 |
| Consulta | Página 1 e 2; jogador na página 2 | Itens, totais e `Me` corretos mesmo fora da página | IT-01 |
| Exclusões | Base com partidas solo e contra robô | Nenhuma entra na classificação | IT-02 |
| Escala | 10.000 partidas | Consulta em menos de 500 ms | IT-03 |
| Jornada | Ranking semeado com identidade | Pódio, tabela completa, cartão e página 2 | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado `GetLeaderboardAsync(10)` com dados legados, então a ordem por vitórias e o desempate por último triunfo permanecem (guarda; cobre `SPEC-0017`, que continua passando).

**Testes existentes afetados:** `LeaderboardCyberArenaTests` (SPEC-0033) passa a cobrir também as novas colunas; nenhuma asserção anterior é removida.

### 7.2 Testes Unitários
- **UT-01** — Dado um conjunto de partidas com `PlayerId` distintos e mesmo apelido, partidas antigas sem `PlayerId`, partidas solo e contra robô, vitórias/derrotas/empates e sequências variadas, então `LeaderboardAnalysis` devolve as entradas esperadas (contagens, aproveitamento com 1 casa, sequência, ordem, desempate, nome mais recente) e exclui solo/robô.
- **UT-02** — Dado 25 jogadores e `PageSize=10`, então as páginas têm 10, 10 e 5 itens, as posições são contínuas (11–20 na página 2) e páginas fora do intervalo são limitadas.
- **UT-03** — Dado a página com um serviço de teste, então a tabela exibe as oito colunas, a linha do jogador tem "VOCÊ", o pódio mostra vitórias/aproveitamento/sequência e o cartão "Sua posição" cobre os três casos: ranqueado (posição e números), sem vitória ("Vença uma partida para entrar no ranking") e sem identidade (cartão oculto).
- **UT-04** — Dado carregando e sem dados, então o indicador e o estado vazio da SPEC-0033 permanecem.

### 7.3 Testes de Integração
- **IT-01** — Dado `GameplayDbContext` InMemory com 25 jogadores, quando `GetLeaderboardPageAsync` roda para as páginas 1 e 2 com o jogador atual na página 2, então itens, totais, número de páginas e `Me` estão corretos.
- **IT-02** — Dado partidas em modo Solo, partidas antigas com nome de robô e partidas normais, então apenas as normais entram na classificação.
- **IT-03** — Dado 10.000 partidas sintéticas, então a consulta responde em menos de 500 ms.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
- **E2E-01** — Jornada do ranking (bUnit): com identidade e dados semeados, a página mostra o pódio, a tabela com derrotas, empates, aproveitamento e sequência, o cartão "Sua posição" e a segunda página.

### 7.6 Outros
- Revisão visual (H2): 390px e 1280px contra `ranking-mobile.png`/`ranking-desktop.png`; lista de omitidos conferida.
- Lighthouse Acessibilidade ≥ 90 em `/leaderboard`.

**Dublês e dados de teste:** EF InMemory com construtor de partidas, identidade em memória, serviço de ranking de teste para a UI.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; sem migration.
- **Dados/schema:** N/A.
- **Compatibilidade:** `GetLeaderboardAsync` permanece; partidas antigas continuam contando por apelido.
- **Observabilidade:** tempo da consulta em log de depuração; revisão visual no PR.
- **Rollback:** `git revert` do PR.
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
Nenhuma além das decisões transversais registradas no épico SPEC-0035 (partidas solo e partidas antigas): esta spec implementa a resposta a elas.

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [x] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [x] Escrever os testes `SPEC-0039:CH-01`, `SPEC-0039:UT-01`, `SPEC-0039:UT-02`, `SPEC-0039:UT-03`, `SPEC-0039:UT-04`, `SPEC-0039:IT-01`, `SPEC-0039:IT-02`, `SPEC-0039:IT-03`, `SPEC-0039:E2E-01` com a tag `SPEC-0039:<ID>`, em commits `test(...)` com `Refs: SPEC-0039`, tocando só `test_paths`
- [x] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [x] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0039`)

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [x] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [x] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [x] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [x] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável — **não executada** (app não aberto no navegador); aceita pela autorização de merge, ver Pendências
- [x] PR com `spec_graph.py pr SPEC-0039`, CI verde (G5) e aprovação do merge (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
| G1 Red | PASS | verify SPEC-0039: Red antes do Green (60878cb), 9/9 testes do plano rastreados | 2026-09-29 |
| G2 Green | PASS | dotnet test 260/260 local; format e tailwind --check limpos; verify PASS | 2026-09-29 |
| G3 Arquitetura | N/A | sem suíte Category=Architecture | 2026-09-29 |
| G4 Review | PASS | Review independente (subagente): PASS, 0 bloqueantes/maiores; menores tratados (mesma chave nos dois lados ignorada com teste, chip VOCÊ aria-hidden). Restam como follow-up: teste da sequência anti-resposta velha, fuso do servidor em FormatDate | 2026-09-29 |
| G5 Integração & CI | PASS | PR #22: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
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

Ranking por jogador (PlayerId; partidas antigas por apelido) com vitórias, derrotas, empates, aproveitamento, sequência e último triunfo, paginação de 10, pódio com aproveitamento e sequência, cartão 'Sua posição' (ranqueado, sem vitória, sem identidade) e 'VOCÊ' na linha. Partidas solo e antigas contra robô não contam.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

LeaderboardAnalysis (funções puras: Counts, Rank, Paginate), GameResultService.GetLeaderboardPageAsync (projeção mínima e agregação em memória), AiPlayer.IsBotName, Leaderboard.razor com identidade em OnAfterRenderAsync (falha oculta o cartão), sequência anti-resposta velha, erro com nova tentativa; reaproveita Pager.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0039:CH-01 | Dado `GetLeaderboardAsync(10)` com dados legados, então a ordem por vitórias e o desempate por último triunfo  | PASS | `dotnet test` 260/260 no CI (dotnet-ci) do PR #22 |
| SPEC-0039:UT-01 | Dado um conjunto de partidas com `PlayerId` distintos e mesmo apelido, partidas antigas sem `PlayerId`, partid | PASS | `dotnet test` 260/260 no CI (dotnet-ci) do PR #22 |
| SPEC-0039:UT-02 | Dado 25 jogadores e `PageSize=10`, então as páginas têm 10, 10 e 5 itens, as posições são contínuas (11–20 na  | PASS | `dotnet test` 260/260 no CI (dotnet-ci) do PR #22 |
| SPEC-0039:UT-03 | Dado a página com um serviço de teste, então a tabela exibe as oito colunas, a linha do jogador tem "VOCÊ", o  | PASS | `dotnet test` 260/260 no CI (dotnet-ci) do PR #22 |
| SPEC-0039:UT-04 | Dado carregando e sem dados, então o indicador e o estado vazio da SPEC-0033 permanecem. | PASS | `dotnet test` 260/260 no CI (dotnet-ci) do PR #22 |
| SPEC-0039:IT-01 | Dado `GameplayDbContext` InMemory com 25 jogadores, quando `GetLeaderboardPageAsync` roda para as páginas 1 e  | PASS | `dotnet test` 260/260 no CI (dotnet-ci) do PR #22 |
| SPEC-0039:IT-02 | Dado partidas em modo Solo, partidas antigas com nome de robô e partidas normais, então apenas as normais entr | PASS | `dotnet test` 260/260 no CI (dotnet-ci) do PR #22 |
| SPEC-0039:IT-03 | Dado 10.000 partidas sintéticas, então a consulta responde em menos de 500 ms. | PASS | `dotnet test` 260/260 no CI (dotnet-ci) do PR #22 |
| SPEC-0039:E2E-01 | Jornada do ranking (bUnit): com identidade e dados semeados, a página mostra o pódio, a tabela com derrotas, e | PASS | `dotnet test` 260/260 no CI (dotnet-ci) do PR #22 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #22 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Verificação visual e Lighthouse (app não aberto no navegador). IT-03 é smoke em InMemory. Falta teste da sequência anti-resposta velha e do descarte. FormatDate usa o fuso do servidor. Ordem do pódio no DOM é 2º, 1º, 3º para leitores de tela.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0039`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
| 2 | 2026-09-29 | Contrato ganha `LeaderboardGame`, `LeaderboardAnalysis.Counts/Rank/Paginate` (assinaturas das funções puras); `touches` inclui `LeaderboardCyberArenaTests.cs` e `TailwindDesignSystemTests.cs` | Explicitar a projeção e as funções puras testadas por UT-01/UT-02; a página passa a exigir o serviço de identidade, então os testes existentes que a renderizam precisam registrá-lo | SPEC-0033 e SPEC-0034 (testes); sem consumidores do contrato | thomas (autorização permanente, 2026-09-29) |
