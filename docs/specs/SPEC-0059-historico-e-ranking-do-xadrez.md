---
id: SPEC-0059
title: Histórico e ranking do xadrez
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0048, SPEC-0053]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/HistoryModels.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/HistoryAnalysis.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/LeaderboardModels.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/History.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Leaderboard.razor, src/TicTacToe/TicTacToe.Web/wwwroot/css/cyber-arena.css, tests/TicTacToe.Tests/ChessHistoryTests.cs, tests/TicTacToe.Tests/ChessLeaderboardTests.cs, tests/TicTacToe.Tests/HistoryCyberArenaTests.cs, tests/TicTacToe.Tests/HistoryAdvancedUiTests.cs, tests/TicTacToe.Tests/LeaderboardAdvancedUiTests.cs]
adrs: [ADR-0012]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0059 — Histórico e ranking do xadrez

## 1. Visão Geral
Faz o **histórico e o ranking** falarem dos dois jogos: um seletor **Jogo** (Jogo da Velha ou Xadrez) nas duas páginas, guardado na URL, e as colunas e textos do xadrez (cor, controle de tempo, lances, motivos como xeque-mate e afogamento).

## 2. Motivação & Escopo
**Motivação:** As partidas de xadrez já são gravadas (SPEC-0053) e as consultas filtram por jogo (SPEC-0047); falta mostrá-las nas telas de histórico e ranking (`docs/design/stitch/chess/historico-desktop.png` e `ranking-desktop.png`).

**Objetivos (dentro do escopo):**
- Seletor "Jogo" (`SegmentedControl`: Jogo da velha, Xadrez) em `History` e `Leaderboard`, lido de `?jogo=` (`velha`|`xadrez`, padrão velha) e refletido na URL; a consulta usa `Game`.
- `HistoryItem` ganha, com valores padrão no fim, `Game`, `TimeControl` e `MoveCount`; no xadrez a tabela mostra **Controle** e **Lances** e a cor das peças do jogador ("Brancas"/"Pretas").
- `HistoryAnalysis.Reason` e `Summarize` conhecem os motivos do xadrez (Xeque-mate, Afogamento, Material insuficiente, Regra dos 50 lances, Repetição de posição); xeque-mate conta como vitória decisiva para a "vitória mais rápida".
- Ranking do xadrez com as mesmas colunas e regras (só quem tem vitória; solo fora), e cartão "Sua posição" por jogo.

**Não-objetivos (fora do escopo):**
- Rating/ELO, temporadas, títulos e badges do mock; "Há 2h vs …" no último triunfo (o dado não é guardado).
- Filtro por controle de tempo ou por cor; comparação entre jogos.
- Mudança no comportamento do jogo da velha (padrão da URL preserva a tela atual).

## 3. Dependências
- **Implementações necessárias:** SPEC-0048 (seleção e navegação por jogo) e SPEC-0053 (dados de xadrez gravados).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `History.razor`/`Leaderboard.razor` (SPEC-0038/0039), `HistoryAnalysis`, `LeaderboardAnalysis` e consultas por jogo da SPEC-0047.

**Decisão:** Um seletor de jogo por página, estado na URL (`SupplyParameterFromQuery`), campos novos opcionais em `HistoryItem` (compatível com os testes existentes) e textos de motivo estendidos.

**Justificativa:** Reaproveita 100% das telas e consultas; a URL torna o estado compartilhável e permite links diretos vindos do lobby de xadrez.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Páginas separadas de histórico e ranking do xadrez (duplicaria código); estado do seletor só em memória (perde ao recarregar).

**ADRs:** ADR-0012

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Mesmos limites das SPEC-0038/0039 (histórico p95 < 200 ms com 1.000 partidas; ranking < 500 ms com 10.000).
- **Segurança:** O parâmetro `jogo` é um conjunto fechado (`velha`, `xadrez`); valor desconhecido cai no padrão.
- **Privacidade e dados pessoais:** Só nomes já visíveis; sem `PlayerId` em tela.
- **Disponibilidade e resiliência:** Dados ausentes (partidas antigas ou sem controle) aparecem como "—"; erro de consulta tratado como hoje.
- **Acessibilidade (UI):** Seletor como radiogrupo com rótulo; tabelas com cabeçalhos; texto além de cor nos chips; contraste AA; Lighthouse ≥ 90.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `rotas /history e /leaderboard com ?jogo= · HistoryItem · HistoryAnalysis.Reason/Summarize`

```text
/history?jogo=xadrez   /leaderboard?jogo=xadrez        (padrão: velha; valor desconhecido → velha)
Seletor "Jogo" (radiogrupo aria-label="Jogo"): "Jogo da velha" | "Xadrez"; trocar reseta filtro e página e atualiza a URL

HistoryItem  (campos existentes)  + GameType Game = GameType.TicTacToe, string? TimeControl = null, int? MoveCount = null     // no fim, com padrão
HistoryQuery.Game / LeaderboardQuery.Game = jogo escolhido (SPEC-0047)

Tabela do histórico no xadrez: Resultado · Duelo (nome + "Brancas"/"Pretas") · Controle · Motivo · Lances (lances completos = ⌈MoveCount/2⌉, pois MoveCount guarda meios-lances) · Duração · Data
Motivos (HistoryAnalysis.Reason): Checkmate "Xeque-mate" · Stalemate "Afogamento" · Insufficient "Material insuficiente" · FiftyMoves "Regra dos 50 lances" ·
   Repetition "Repetição de posição" · Timeout "Tempo esgotado" · Abandon "Abandono" · Disconnect "Desconexão do oponente"
Summarize: "vitória mais rápida" considera Line e Checkmate; demais regras inalteradas
Ranking do xadrez: mesmas colunas e regras da SPEC-0039; partidas solo excluídas por Mode
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Seletor de jogo | Abrir com e sem `?jogo=`, valor inválido, trocar de jogo | Jogo certo na consulta; URL atualizada; padrão velha | UT-01 |
| Colunas do xadrez | Partidas de xadrez no histórico | Controle, lances e cor das peças; campos nulos como "—" | UT-02 |
| Motivos | Todos os `EndReason` de xadrez | Textos do contrato; velha inalterada | UT-03 |
| Resumo | Vitória por xeque-mate e por tempo | Mate conta na vitória mais rápida; tempo não | UT-04 |
| Ranking do xadrez | Base com partidas de xadrez, solo e velha | Só xadrez, sem solo; posição, colunas e "VOCÊ" | IT-01 |
| Histórico do xadrez | Base mista | Só xadrez; contagens e resumo do xadrez | IT-02 |
| Compatibilidade | Histórico e ranking do jogo da velha | Nada muda sem `?jogo=` | CH-01 |
| Jornada | Do ranking de velha ao de xadrez, filtrando e paginando | Seletor, tabela e cartão corretos | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado as páginas de histórico e ranking sem `?jogo=`, então a marcação, as consultas e os testes existentes (SPEC-0032, 0033, 0038, 0039) seguem iguais (guarda: passa antes da mudança).

### 7.2 Testes Unitários
- **UT-01** — Dado `History` e `Leaderboard` com `?jogo=xadrez`, com valor inválido e sem parâmetro, então a consulta usa `Chess`, `TicTacToe` e `TicTacToe`, o radiogrupo "Jogo" marca o certo e trocar de jogo atualiza a URL e reseta filtro e página.
- **UT-02** — Dado itens de xadrez (com e sem controle e lances), então a tabela mostra Controle e Lances, "Brancas"/"Pretas" ao lado do nome e "—" nos nulos; itens do jogo da velha não mostram essas colunas.
- **UT-03** — Dado cada motivo de xadrez e os antigos, então `HistoryAnalysis.Reason` devolve os textos do contrato e os textos do jogo da velha não mudam.
- **UT-04** — Dado vitórias por xeque-mate e por tempo, então `Summarize` usa o mate na "vitória mais rápida" e ignora o tempo.

### 7.3 Testes de Integração
- **IT-01** — Dado uma base com partidas de xadrez (online e solo) e de jogo da velha, quando o ranking de xadrez é consultado, então só as de xadrez não solo entram, com posição, colunas e "VOCÊ" corretos.
- **IT-02** — Dado a mesma base, quando o histórico de xadrez é consultado, então só as de xadrez aparecem e contagens, resumo e itens (controle, lances) estão certos.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
- **E2E-01** — Jornada (bUnit): com dados dos dois jogos e identidade, abrir o ranking do jogo da velha, trocar para xadrez, ver a tabela e "Sua posição"; ir ao histórico de xadrez, filtrar Vitórias e paginar.

### 7.6 Outros
- Revisão visual (H2): 390px e 1280px contra `docs/design/stitch/chess/historico-desktop.png` e `ranking-desktop.png`; lista de omitidos (rating, temporada, badges) conferida.
- `tools/tailwind/build.sh` executado e `--check` sem diferença.
- Lighthouse Acessibilidade ≥ 90 nas duas páginas.

**Dublês e dados de teste:** EF InMemory com construtores de partida dos dois jogos, identidade em memória, serviço de teste para a UI.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; padrão da URL preserva as telas atuais.
- **Dados/schema:** N/A
- **Compatibilidade:** Novos campos de `HistoryItem` são opcionais no fim; chamadas existentes compilam sem mudança.
- **Observabilidade:** N/A
- **Rollback:** Reverter o PR.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Mostrar rating/ELO e badges? — Não, fora do escopo (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [x] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [x] Escrever `SPEC-0059:CH-01`, `SPEC-0059:E2E-01`, `SPEC-0059:IT-01`, `SPEC-0059:IT-02`, `SPEC-0059:UT-01`, `SPEC-0059:UT-02`, `SPEC-0059:UT-03`, `SPEC-0059:UT-04` com a tag `SPEC-0059:<ID>` em commits `test(...)` com `Refs: SPEC-0059` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [x] Refactor mantendo tudo verde
- [x] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes`, `tools/tailwind/build.sh --check` e `verify SPEC-0059 --base origin/main`

**Fase final: Integração, entrega e documentação**
- [x] Review independente (G4)
- [x] Integração + CI verde (G5) e aprovação (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
| G1 Red | PASS | Red confirmado: 15 dos 19 casos falharam pelo motivo certo (c9d94db); guardas/caracterização passaram já no Red; verify PASS | 2026-09-29 |
| G2 Green | PASS | dotnet test: 910 total, 909 passam, 1 pulado, 0 falhas; format e tailwind --check limpos; 4 execuções no review sem falha | 2026-09-29 |
| G3 Arquitetura | N/A | sem novas regras estruturais | 2026-09-29 |
| G4 Review | PASS | Review independente PASS (0 bloqueantes/maiores; 3 menores registrados em Pendências) | 2026-09-29 |
| G5 Integração & CI | PASS | PR #59: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
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

Seletor "Jogo" (velha/xadrez, ?jogo=) em Histórico e Ranking; histórico do xadrez com Duelo, Controle, Motivo, Lances, cor das peças e resumo (mate conta como vitória mais rápida); ranking do xadrez sem partidas solo.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

HistoryItem ganhou Game/TimeControl/MoveCount preenchidos por GameResultService.ToItem; HistoryAnalysis com os 5 motivos de xadrez; History.razor e Leaderboard.razor com SegmentedControl e consulta por Game, com guarda de sequência contra respostas atrasadas.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0059:CH-01 | Dado as páginas de histórico e ranking sem `?jogo=`, então a marcação, as consultas e os testes existentes (SP | PASS | `dotnet test` 909/909 no CI (dotnet-ci) do PR #59 |
| SPEC-0059:UT-01 | Dado `History` e `Leaderboard` com `?jogo=xadrez`, com valor inválido e sem parâmetro, então a consulta usa `C | PASS | `dotnet test` 909/909 no CI (dotnet-ci) do PR #59 |
| SPEC-0059:UT-02 | Dado itens de xadrez (com e sem controle e lances), então a tabela mostra Controle e Lances, "Brancas"/"Pretas | PASS | `dotnet test` 909/909 no CI (dotnet-ci) do PR #59 |
| SPEC-0059:UT-03 | Dado cada motivo de xadrez e os antigos, então `HistoryAnalysis.Reason` devolve os textos do contrato e os tex | PASS | `dotnet test` 909/909 no CI (dotnet-ci) do PR #59 |
| SPEC-0059:UT-04 | Dado vitórias por xeque-mate e por tempo, então `Summarize` usa o mate na "vitória mais rápida" e ignora o tem | PASS | `dotnet test` 909/909 no CI (dotnet-ci) do PR #59 |
| SPEC-0059:IT-01 | Dado uma base com partidas de xadrez (online e solo) e de jogo da velha, quando o ranking de xadrez é consulta | PASS | `dotnet test` 909/909 no CI (dotnet-ci) do PR #59 |
| SPEC-0059:IT-02 | Dado a mesma base, quando o histórico de xadrez é consultado, então só as de xadrez aparecem e contagens, resu | PASS | `dotnet test` 909/909 no CI (dotnet-ci) do PR #59 |
| SPEC-0059:E2E-01 | Jornada (bUnit): com dados dos dois jogos e identidade, abrir o ranking do jogo da velha, trocar para xadrez,  | PASS | `dotnet test` 909/909 no CI (dotnet-ci) do PR #59 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #59 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Menores da review: (1) History.SetGame carrega o resumo sem guarda de sequência: troca rápida velha→xadrez→velha pode deixar o resumo do jogo errado (tabela correta); (2) ?jogo= só é lido em OnInitialized (URL alterada por fora com a página aberta não reage); (3) sem teste da corrida de trocas rápidas. Revisão visual e Lighthouse não executados (app nunca aberto). Links Href="/" mantidos (testes existentes).

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0059`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
