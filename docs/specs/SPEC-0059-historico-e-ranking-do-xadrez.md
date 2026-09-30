---
id: SPEC-0059
title: Histórico e ranking do xadrez
tier: full
type: feature
user_facing: true
status: in-progress
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
- [ ] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [ ] Escrever `SPEC-0059:CH-01`, `SPEC-0059:E2E-01`, `SPEC-0059:IT-01`, `SPEC-0059:IT-02`, `SPEC-0059:UT-01`, `SPEC-0059:UT-02`, `SPEC-0059:UT-03`, `SPEC-0059:UT-04` com a tag `SPEC-0059:<ID>` em commits `test(...)` com `Refs: SPEC-0059` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [ ] Refactor mantendo tudo verde
- [ ] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes`, `tools/tailwind/build.sh --check` e `verify SPEC-0059 --base origin/main`

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0059`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
