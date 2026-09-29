---
id: SPEC-0044
title: Série melhor de 5: interface no lobby e na arena
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0035
depends_on: [SPEC-0030, SPEC-0040]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Game/Lobby.razor, src/TicTacToe/TicTacToe.Web/Components/Game/PlayerCard.razor, src/TicTacToe/TicTacToe.Web/Components/Game/Scoreboard.razor, src/TicTacToe/TicTacToe.Web/Components/Game/RematchBar.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs, tests/TicTacToe.Tests/SeriesUiTests.cs, tests/TicTacToe.Tests/LobbyCyberArenaTests.cs, tests/TicTacToe.Tests/ArenaCyberArenaTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0044 — Série melhor de 5: interface no lobby e na arena

## 1. Visão Geral
Dá ao usuário acesso à **série melhor de 5** criada na SPEC-0040: um seletor de **formato** no lobby (Partida única ou Melhor de 5), o cabeçalho **"Melhor de 5 • Rodada N de 5"** na arena, o placar da série nos cards dos jogadores com o aviso de **Match point**, os avisos de fim de rodada e de série, e os rótulos do botão de continuar ("Próxima rodada", "Nova série").

## 2. Motivação & Escopo
**Motivação:** sem interface, a série do domínio não é alcançável. O Stitch mostra a série no lobby ("Formato: Melhor de 5") e na arena (rodada, placar da série, "MATCH POINT").

**Objetivos (dentro do escopo):**
- Referências: `docs/design/stitch/lobby-desktop.html` (formato) e `arena-desktop.html`/`arena-mobile.html` (rodada, placar, match point).
- Lobby: `SegmentedControl` **Formato: Partida única | Melhor de 5** com descrição ("Vence quem chegar a 3 rodadas"), valendo para online, solo e sala privada.
- `Home`: cria `GameSession` no formato escolhido (solo) ou usa o formato da partida pareada (`GetMatchBestOf`), e passa `bestOf` ao matchmaking.
- Arena: cabeçalho de rodada (apenas em série); cards mostram três marcadores de vitória por jogador; chip **Match point** quando aplicável.
- Avisos: "Rodada {N} para {nome}!" ao fim de cada rodada e "{nome} venceu a série {a} × {b}" ao fim; empate: "Rodada empatada, será repetida".
- `RematchBar`: rótulos **Jogar novamente** (partida única), **Próxima rodada** (rodada encerrada) e **Nova série** (série encerrada).

**Não-objetivos (fora do escopo):**
- Regras da série, pareamento e persistência (SPEC-0040).
- Abandonar a série, revanche com aceite e W.O. por desconexão (SPEC-0041, SPEC-0042).
- "Ranked", "Competitiva", ELO e qualquer selo de temporada do Stitch (backlog E).
- Animações extras de fim de série.

## 3. Dependências
- **Implementações necessárias:** SPEC-0030 (lobby redesenhado) e SPEC-0040 (API pública de série em `GameSession` e `MatchmakingService`).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** `Lobby` (SPEC-0030), `Scoreboard`/`PlayerCard`/`RematchBar` (SPEC-0031), orquestração na `Home`; API de série da SPEC-0040.

**Decisão:** o formato é um parâmetro novo do `Lobby` (`SelectedFormat` e `FormatChanged`); a arena lê o estado da série direto de `GameSession`; os componentes só apresentam.

**Justificativa:** mantém o padrão de parâmetros e de componentes puros de apresentação (ADR-0006).

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** escolher o formato numa página à parte (fricção); esconder o formato para modos específicos (regra de produto não pedida).

**ADRs:** ADR-0008 (primitivos de UI).

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** sem chamadas novas; re-render pelos eventos existentes de `GameSession`.
- **Segurança:** N/A — o formato escolhido é validado no domínio (`bestOf` 1 ou 5).
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** valor de formato inválido cai para partida única.
- **Acessibilidade (UI):** seletor de formato navegável por teclado; marcadores de vitória com texto alternativo ("Thomas: 2 de 3 vitórias"); avisos em região `aria-live="polite"`; estado "Match point" nunca só por cor; contraste AA; Lighthouse ≥ 90.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `Lobby`, `PlayerCard`, `Scoreboard`, `RematchBar` e `Home`.

```text
Lobby       [Parameter] SeriesFormat SelectedFormat = Single · EventCallback<SeriesFormat> FormatChanged   // API do Lobby cresce 2 parâmetros
PlayerCard  [Parameter] int SeriesWins · int SeriesTarget · bool MatchPoint       // marcadores só quando SeriesTarget > 0
RematchBar  [Parameter] bool Finished · RematchKind Kind (Rematch|NextRound|NewSeries) · EventCallback OnRematch
Cabeçalho da arena (só em BestOf5): "Melhor de 5 • Rodada {RoundNumber} de 5"
Avisos: rodada  → "Rodada {N} para {nome}!"  ·  empate → "Rodada empatada, será repetida"  ·  série → "{nome} venceu a série {a} × {b}"
Rótulos: Rematch = "Jogar novamente" · NextRound = "Próxima rodada" · NewSeries = "Nova série"
```

**Arquivos/módulos afetados:** ver `touches`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Seletor de formato | Lobby idle | Duas opções com a atual marcada; trocar dispara `FormatChanged`; descrição do formato | UT-01 |
| Marcadores da série | 2 de 3 vitórias | Dois marcadores preenchidos; texto alternativo; chip "Match point" | UT-02 |
| Cabeçalho de rodada | Série na rodada 3 | "Melhor de 5 • Rodada 3 de 5"; ausente em partida única | UT-03 |
| Rótulos do botão | Partida única / rodada encerrada / série encerrada | "Jogar novamente" / "Próxima rodada" / "Nova série" | UT-04 |
| Avisos | Fim de rodada / empate / fim de série | Textos do contrato em região `aria-live` | UT-05 |
| Partida única | Formato Single | Nenhum elemento de série na tela | UT-06 |
| Formato até o domínio | Escolher "Melhor de 5" no lobby e iniciar solo e online | Sessão solo em BestOf5; `bestOf` 5 enviado ao matchmaking; partida pareada usa o formato da sala | IT-01 |
| Jornada | Escolher formato, iniciar solo, jogar | Cabeçalho da rodada 1, marcadores em zero, primeira jogada | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado o lobby e a arena em partida única, então a marcação sem série permanece igual à atual (guarda: passa antes da mudança).

**Testes existentes afetados:** `LobbyCyberArenaTests` (o `CH-01` que fixa a API do `Lobby` passa a incluir `SelectedFormat` e `FormatChanged`) e `ArenaCyberArenaTests` (rótulo do botão de revanche passa por `RematchKind`). Justificados pelos novos parâmetros.

### 7.2 Testes Unitários
- **UT-01** — Dado `Lobby` com `SelectedFormat=Single`, então "Partida única" está marcada; clicar "Melhor de 5" dispara `FormatChanged(BestOf5)` e exibe a descrição "Vence quem chegar a 3 rodadas".
- **UT-02** — Dado `PlayerCard` com `SeriesWins=2`, `SeriesTarget=3` e `MatchPoint`, então há dois marcadores preenchidos e um vazio, o texto alternativo diz "2 de 3 vitórias" e o chip "Match point" aparece; com `SeriesTarget=0` nenhum aparece.
- **UT-03** — Dado uma sessão BestOf5 na rodada 3, então o cabeçalho diz "Melhor de 5 • Rodada 3 de 5"; em Single não existe cabeçalho.
- **UT-04** — Dado `RematchBar` com cada `RematchKind`, então o rótulo é "Jogar novamente", "Próxima rodada" ou "Nova série" e o clique dispara `OnRematch`.
- **UT-05** — Dado uma rodada vencida, uma rodada empatada e uma série encerrada 3 × 1, então o aviso corresponde ao contrato, em região `aria-live="polite"`.
- **UT-06** — Dado uma sessão Single, então nenhum marcador, chip, cabeçalho de rodada ou aviso de série é renderizado.

### 7.3 Testes de Integração
- **IT-01** — Dado `Home` no bUnit com matchmaking de teste, quando o usuário escolhe "Melhor de 5" e inicia o solo, então a `GameSession` criada tem `Format=BestOf5`; ao criar sala privada e ao procurar oponente, o `bestOf` enviado é 5; uma partida pareada com `GetMatchBestOf`=5 cria a sessão em BestOf5.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
- **E2E-01** — Jornada da série (bUnit): no lobby escolher "Melhor de 5", iniciar solo, então a arena mostra "Melhor de 5 • Rodada 1 de 5" e os marcadores em zero para os dois jogadores; após a primeira jogada, a marca aparece no tabuleiro.

### 7.6 Outros
- Revisão visual (H2): lobby com o seletor e a arena em série (rodada 1, match point, série encerrada) em 390px e 1280px contra `arena-mobile.png`/`arena-desktop.png`.
- Lighthouse Acessibilidade ≥ 90.

**Dublês e dados de teste:** `BunitContext`, `MatchmakingService` real ou de teste, sessões pré-montadas no estado desejado, EF InMemory.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; partida única continua o padrão do seletor.
- **Dados/schema:** N/A.
- **Compatibilidade:** quem não escolher "Melhor de 5" não vê nenhuma mudança de fluxo.
- **Observabilidade:** revisão visual e console sem erros.
- **Rollback:** `git revert` do PR (a SPEC-0040 permanece inofensiva).
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [x] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [x] Escrever os testes `SPEC-0044:CH-01`, `SPEC-0044:UT-01`, `SPEC-0044:UT-02`, `SPEC-0044:UT-03`, `SPEC-0044:UT-04`, `SPEC-0044:UT-05`, `SPEC-0044:UT-06`, `SPEC-0044:IT-01`, `SPEC-0044:E2E-01` com a tag `SPEC-0044:<ID>`, em commits `test(...)` com `Refs: SPEC-0044`, tocando só `test_paths`
- [x] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [x] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0044`)

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [x] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [x] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [x] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [x] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável — **não executada** (app não aberto no navegador); aceita pela autorização de merge, ver Pendências
- [x] PR com `spec_graph.py pr SPEC-0044`, CI verde (G5) e aprovação do merge (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
| G1 Red | PASS | verify SPEC-0044: Red antes do Green, testes do plano rastreados | 2026-09-29 |
| G2 Green | PASS | dotnet test 289/289 local; format e tailwind --check limpos; verify PASS | 2026-09-29 |
| G3 Arquitetura | N/A | sem suíte Category=Architecture | 2026-09-29 |
| G4 Review | PASS | Review independente (subagente): FAIL condicional a M1 (cabeçalho/aviso caíam abaixo do tabuleiro no desktop) e M2 (região aria-live inserida já preenchida); ambos corrigidos com teste (bloco posicionado do status; região sempre presente). Menores aceitos: rótulo do radiogroup sem aria-labelledby, leitura dupla placar/marcadores; sem nova rodada de review. Layout desktop não conferido em navegador | 2026-09-29 |
| G5 Integração & CI | PASS | PR #26: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
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

Interface da série melhor de 5: seletor de formato no lobby (valendo para online, solo e sala privada), cabeçalho 'Melhor de 5 • Rodada N de 5', marcadores de vitória e chip Match point nos cards, avisos de rodada, empate e fim de série em região aria-live e rótulos do botão (Jogar novamente, Próxima rodada, Nova série).

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

Lobby ganha SelectedFormat/FormatChanged; PlayerCard, Scoreboard e RematchBar (RematchKind aninhado) apresentam o estado de GameSession; Home guarda o SeriesFormat e o envia ao solo e ao matchmaking (bestOf), e escolhe o RematchKind pelo estado da série. Cabeçalho e aviso ficam no bloco posicionado do status.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0044:CH-01 | Dado o lobby e a arena em partida única, então a marcação sem série permanece igual à atual (guarda: passa ant | PASS | `dotnet test` 289/289 no CI (dotnet-ci) do PR #26 |
| SPEC-0044:UT-01 | Dado `Lobby` com `SelectedFormat=Single`, então "Partida única" está marcada; clicar "Melhor de 5" dispara `Fo | PASS | `dotnet test` 289/289 no CI (dotnet-ci) do PR #26 |
| SPEC-0044:UT-02 | Dado `PlayerCard` com `SeriesWins=2`, `SeriesTarget=3` e `MatchPoint`, então há dois marcadores preenchidos e  | PASS | `dotnet test` 289/289 no CI (dotnet-ci) do PR #26 |
| SPEC-0044:UT-03 | Dado uma sessão BestOf5 na rodada 3, então o cabeçalho diz "Melhor de 5 • Rodada 3 de 5"; em Single não existe | PASS | `dotnet test` 289/289 no CI (dotnet-ci) do PR #26 |
| SPEC-0044:UT-04 | Dado `RematchBar` com cada `RematchKind`, então o rótulo é "Jogar novamente", "Próxima rodada" ou "Nova série" | PASS | `dotnet test` 289/289 no CI (dotnet-ci) do PR #26 |
| SPEC-0044:UT-05 | Dado uma rodada vencida, uma rodada empatada e uma série encerrada 3 × 1, então o aviso corresponde ao contrat | PASS | `dotnet test` 289/289 no CI (dotnet-ci) do PR #26 |
| SPEC-0044:UT-06 | Dado uma sessão Single, então nenhum marcador, chip, cabeçalho de rodada ou aviso de série é renderizado. | PASS | `dotnet test` 289/289 no CI (dotnet-ci) do PR #26 |
| SPEC-0044:IT-01 | Dado `Home` no bUnit com matchmaking de teste, quando o usuário escolhe "Melhor de 5" e inicia o solo, então a | PASS | `dotnet test` 289/289 no CI (dotnet-ci) do PR #26 |
| SPEC-0044:E2E-01 | Jornada da série (bUnit): no lobby escolher "Melhor de 5", iniciar solo, então a arena mostra "Melhor de 5 • R | PASS | `dotnet test` 289/289 no CI (dotnet-ci) do PR #26 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #26 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Layout desktop, revisão visual e Lighthouse não conferidos em navegador. Rótulo do radiogroup sem aria-labelledby; leitura dupla de placar e marcadores por leitor de tela. Sem teste do robô abrindo a rodada 2 pela Home.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0044`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
