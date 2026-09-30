---
id: SPEC-0048
title: Seleção de jogos e navegação por jogo
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0047, SPEC-0058, SPEC-0060]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Pages/GameSelect.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor, src/TicTacToe/TicTacToe.Web/Components/Layout/MainLayout.razor, src/TicTacToe/TicTacToe.Web/wwwroot/css/cyber-arena.css, tests/TicTacToe.Tests/GameSelectTests.cs, tests/TicTacToe.Tests/ShellLayoutTests.cs]
adrs: [ADR-0008]
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0048 — Seleção de jogos e navegação por jogo

## 1. Visão Geral
Cria a tela de **seleção de jogos** (Jogo da Velha ou Xadrez) como entrada do app e move o lobby do jogo da velha para `/velha`. O menu principal passa a tratar `/`, `/velha` e `/xadrez` como "Jogar".

## 2. Motivação & Escopo
**Motivação:** Com dois jogos, o usuário precisa escolher qual jogar. O Stitch tem a tela "Seleção de Jogos" (`docs/design/stitch/chess/selecao-desktop.png`).

**Objetivos (dentro do escopo):**
- Página `/` (`GameSelect`) com duas cartas: **Jogo da Velha** (acento vermelho, link `/velha`) e **Xadrez** (acento ciano, link `/xadrez`), cada uma com título, descrição curta, chip "Online" e botão "Jogar".
- `Home.razor` passa de `/` para `/velha`, sem alterar componentes, estados ou testes do jogo da velha.
- `MainLayout`: o item "Jogar" fica ativo em `/`, `/velha` e `/xadrez` (correspondência exata de segmento, sem casar `/xadrezfoo`).

**Não-objetivos (fora do escopo):**
- Página do xadrez em si (SPEC-0056) e o seletor de jogo em histórico e ranking (SPEC-0059).
- Lembrar o último jogo escolhido, redirecionamentos automáticos e contagem de jogadores online (dado de protótipo).
- Qualquer mudança no jogo da velha além da rota.

## 3. Dependências
- **Implementações necessárias:** SPEC-0047 (jogo como conceito do modelo), SPEC-0058 e SPEC-0060 (o xadrez só é linkado quando a partida está completa: lobby, arena, robô, abandono, revanche e desconexão).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** Páginas Blazor Server interativas com `@page` (Home, History, Leaderboard) e primitivos Cyber Arena (`NeonCard`, `PillButton`, `StatusChip`, `PageHeader`) da SPEC-0043; layout da SPEC-0043. Referência: `Leaderboard.razor` para a composição de página.

**Decisão:** Nova página de apresentação `GameSelect` sem estado; `Home` só troca a rota; `MainLayout` estende a regra de item ativo.

**Justificativa:** Mantém componentes puros de apresentação (ADR-0006) e mexe o mínimo no jogo da velha.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Mostrar o seletor dentro do lobby (mistura responsabilidades); mudar a rota do jogo da velha só depois (deixaria `/` ambíguo).

**ADRs:** ADR-0008 (primitivos de UI)

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** N/A — página estática, sem consulta.
- **Segurança:** N/A — links internos fixos.
- **Privacidade e dados pessoais:** N/A — sem dados.
- **Disponibilidade e resiliência:** Marcadores antigos (`/`) passam a mostrar a seleção; quem entra direto em `/velha` cai no lobby de sempre.
- **Acessibilidade (UI):** Cartas como links com rótulo ("Jogar Jogo da Velha"), foco visível, ordem de tabulação lógica, contraste AA; Lighthouse ≥ 90 em `/`.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `rota / (GameSelect) · rota /velha (Home) · item "Jogar" do MainLayout`

```text
GameSelect (/)     PageTitle "Escolha seu jogo · XO Arena"
                   duas cartas: [Jogo da Velha → /velha] [Xadrez → /xadrez]; cada uma: título, descrição, chip "Online", link "Jogar"
Home               @page "/velha"   (nenhuma outra mudança)
MainLayout         item "Jogar" ativo (aria-current="page") quando o caminho é "/", "/velha" ou "/xadrez"; caminho parecido ("/xadrezfoo") não ativa
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Seleção | Abrir `/` | Duas cartas com título, descrição, chip e link de destino | UT-01, E2E-01 |
| Rota do jogo da velha | Abrir `/velha` | Lobby do jogo da velha como antes | UT-02, CH-01 |
| Item ativo do menu | Caminhos `/`, `/velha`, `/xadrez`, `/xadrezfoo`, `/history` | "Jogar" ativo nos três primeiros; nunca no parecido; "Histórico" só em `/history` | UT-03 |
| Acessibilidade | Cartas | Links nomeados, ordem lógica de foco | UT-04 |
| Jornada | Escolher jogo e chegar ao lobby | Navegação por link chega ao lobby do jogo da velha | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado o `Home` renderizado diretamente, então lobby, arena e reinício seguem iguais (guarda: os testes das SPEC-0030/0031/0037/0041/0044 continuam passando; só a rota muda).

### 7.2 Testes Unitários
- **UT-01** — Dado `GameSelect`, então há duas cartas com título, descrição, chip "Online" e links para `/velha` e `/xadrez`.
- **UT-02** — Dado a classe `Home`, então seu atributo de rota é `/velha` e `GameSelect` responde em `/`.
- **UT-03** — Dado `MainLayout` em `/`, `/velha`, `/xadrez`, `/xadrezfoo` e `/history`, então "Jogar" só recebe `aria-current` nos três primeiros e "Histórico" só no último.
- **UT-04** — Dado `GameSelect`, então as cartas são `<a>` com rótulo acessível e ordem de foco Velha → Xadrez; sem `<style>` inline.

### 7.3 Testes de Integração
- **IT-01** — Dado o layout com `GameSelect` como corpo e o `NavigationManager`, quando o link do jogo da velha é ativado, então a navegação vai para `/velha` e o item "Jogar" permanece ativo.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
- **E2E-01** — Jornada (bUnit): abrir `/`, ver as duas cartas, seguir "Jogar" do jogo da velha e encontrar o lobby ("Procurar oponente").

### 7.6 Outros
- Revisão visual (H2): 390px e 1280px contra `docs/design/stitch/chess/selecao-desktop.png`; lista de omitidos conferida (chip "Online" é rótulo fixo; contagem de jogadores online do mock não entra).
- `tools/tailwind/build.sh` executado e `--check` sem diferença (classes novas nas cartas).
- Lighthouse Acessibilidade ≥ 90 em `/`.

**Dublês e dados de teste:** `BunitContext`, `FakeNavigationManager` do bUnit.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; esta é a spec que "liga" o xadrez: as rotas já existem e só passam a ter link aqui, quando o xadrez está completo.
- **Dados/schema:** N/A
- **Compatibilidade:** `/` deixa de ser o lobby do jogo da velha; favoritos antigos passam a ver a seleção (um clique a mais).
- **Observabilidade:** N/A
- **Rollback:** Reverter o PR restaura `/` como lobby do jogo da velha.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] O lobby do jogo da velha vai para `/velha` e `/` vira a seleção? — Sim, como no Stitch (Architect, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [x] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [x] Escrever `SPEC-0048:CH-01`, `SPEC-0048:E2E-01`, `SPEC-0048:IT-01`, `SPEC-0048:UT-01`, `SPEC-0048:UT-02`, `SPEC-0048:UT-03`, `SPEC-0048:UT-04` com a tag `SPEC-0048:<ID>` em commits `test(...)` com `Refs: SPEC-0048` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [x] Refactor mantendo tudo verde
- [x] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes`, `tools/tailwind/build.sh --check` e `verify SPEC-0048 --base origin/main`

**Fase final: Integração, entrega e documentação**
- [x] Review independente (G4)
- [x] Integração + CI verde (G5) e aprovação (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
| G1 Red | PASS | Red confirmado: 7 testes falharam pelo motivo certo (b6dc270); CH-01 passou como caracterização; verify PASS | 2026-09-29 |
| G2 Green | PASS | dotnet test: 891 total, 890 passam, 1 pulado, 0 falhas; format e tailwind --check limpos; 4 execuções da suíte no review sem falha | 2026-09-29 |
| G3 Arquitetura | N/A | sem novas regras estruturais | 2026-09-29 |
| G4 Review | PASS | Review independente PASS (0 bloqueantes/maiores) | 2026-09-29 |
| G5 Integração & CI | PASS | PR #57: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
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

Tela de seleção de jogos em / (cartas Jogo da Velha e Xadrez, links acessíveis), lobby da velha movido para /velha e item "Jogar" do menu ativo em /, /velha e /xadrez (desktop e mobile).

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

GameSelect.razor (@page "/") com NeonCard e acentos X/O; Home.razor só troca a rota para /velha; MainLayout.NavItem com Paths e correspondência exata de segmento; CSS regenerado.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0048:CH-01 | Dado o `Home` renderizado diretamente, então lobby, arena e reinício seguem iguais (guarda: os testes das SPEC | PASS | `dotnet test` 890/890 no CI (dotnet-ci) do PR #57 |
| SPEC-0048:UT-01 | Dado `GameSelect`, então há duas cartas com título, descrição, chip "Online" e links para `/velha` e `/xadrez` | PASS | `dotnet test` 890/890 no CI (dotnet-ci) do PR #57 |
| SPEC-0048:UT-02 | Dado a classe `Home`, então seu atributo de rota é `/velha` e `GameSelect` responde em `/`. | PASS | `dotnet test` 890/890 no CI (dotnet-ci) do PR #57 |
| SPEC-0048:UT-03 | Dado `MainLayout` em `/`, `/velha`, `/xadrez`, `/xadrezfoo` e `/history`, então "Jogar" só recebe `aria-curren | PASS | `dotnet test` 890/890 no CI (dotnet-ci) do PR #57 |
| SPEC-0048:UT-04 | Dado `GameSelect`, então as cartas são `<a>` com rótulo acessível e ordem de foco Velha → Xadrez; sem `<style> | PASS | `dotnet test` 890/890 no CI (dotnet-ci) do PR #57 |
| SPEC-0048:IT-01 | Dado o layout com `GameSelect` como corpo e o `NavigationManager`, quando o link do jogo da velha é ativado, e | PASS | `dotnet test` 890/890 no CI (dotnet-ci) do PR #57 |
| SPEC-0048:E2E-01 | Jornada (bUnit): abrir `/`, ver as duas cartas, seguir "Jogar" do jogo da velha e encontrar o lobby ("Procurar | PASS | `dotnet test` 890/890 no CI (dotnet-ci) do PR #57 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #57 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Revisão visual a 390px/1280px contra o Stitch e Lighthouse não executados (app nunca aberto). Sem contagem online/Novo jogo/métricas do mock. Links de History/Leaderboard/logo para / caem na seleção (um clique a mais); ajustar na SPEC-0059 ou futura.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0048`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
