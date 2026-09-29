---
id: SPEC-0033
title: Ranking Cyber Arena
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0028
depends_on: [SPEC-0043]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Pages/Leaderboard.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Leaderboard.razor.css, tests/TicTacToe.Tests/LeaderboardCyberArenaTests.cs]
adrs: [ADR-0008]
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0033 — Ranking Cyber Arena

## 1. Visão Geral
Redesenha a página `/leaderboard` no visual Cyber Arena com o dado que existe hoje (`PlayerRank`: nome, vitórias e último triunfo, top 10): **pódio** com os três primeiros e **tabela de classificação** completa, com estados de carregamento e vazio e um botão **Jogar agora**. O bloco `<style>` inline da página sai.

## 2. Motivação & Escopo
**Motivação:** a página usa `<style>` inline com a paleta antiga e uma tabela crua; o Stitch traz pódio e tabela com hierarquia e destaque de ouro.

**Objetivos (dentro do escopo):**
- Referências: `docs/design/stitch/ranking-desktop.html` (pódio 2º–1º–3º e tabela) e `ranking-mobile.html`.
- Cabeçalho (`PageHeader`): eyebrow "Classificação", título "Classificação Global", subtítulo "Os melhores por vitórias".
- **Pódio** com os três primeiros: 1º ao centro e mais alto (ouro), 2º à esquerda (prata), 3º à direita (bronze); posição, nome e vitórias. Com menos de três jogadores, o pódio mostra só os existentes.
- **Tabela**: Posição, Jogador, Vitórias e Último triunfo (`dd/MM HH:mm` local); linhas 1–3 com destaque de ouro.
- Estados: carregando e vazio ("Nenhum jogador pontuou ainda" + botão **Jogar agora**).
- `<PageTitle>` "Ranking · XO Arena".
- Remoção do `<style>` inline.

**Não-objetivos (fora do escopo):**
- Elementos do Stitch sem backend, **omitidos**: ELO, divisões e temporada, "Sua posição" (VOCÊ), derrotas, empates, aproveitamento, sequência, status online/"há X", contagem de "duelistas ativos", região, filtros (Top 100/Amigos/Minha Divisão), paginação, "Ciclo encerra em", bônus de ELO. Destino: SPEC-0037 (identidade), SPEC-0039 (colunas e paginação) e matriz da SPEC-0035; ELO e temporada ficam no backlog.
- Mudar como o leaderboard é calculado (partidas solo e nomes de robôs continuam contando; decisão na SPEC-0036/0039).
- Mudanças no `GameResultService` ou no banco.

## 3. Dependências
- **Implementações necessárias:** SPEC-0043 — `PageHeader`, `NeonCard`, `PillButton`, `Icon`.
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** `Components/Pages/Leaderboard.razor` (`@rendermode InteractiveServer`, `GameResultService.GetLeaderboardAsync(10)` retorna `PlayerRank(PlayerName, Wins, LastWinAt)`).

**Decisão:** mesma página, consulta e ordenação (vitórias decrescentes, depois último triunfo); apenas a apresentação muda.

**Justificativa:** menor risco; o dado avançado entra na SPEC-0039.

**Desvio do padrão existente:** substitui `<style>` inline e tabela crua por primitivos Cyber Arena (ADR-0008).

**Alternativas descartadas:** calcular colunas novas (derrotas, aproveitamento) já aqui — exigiria mudar a consulta e o contrato (SPEC-0039).

**ADRs:** ADR-0008.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** mesma consulta de 10 linhas.
- **Segurança:** nomes escapados pelo Blazor.
- **Privacidade e dados pessoais:** N/A — mesmos dados já exibidos.
- **Disponibilidade e resiliência:** N/A — comportamento de erro da consulta inalterado.
- **Acessibilidade (UI):** tabela semântica (`th scope`); pódio com `aria-label` e texto "1º/2º/3º" (medalha não é o único indicador); contraste AA; Lighthouse ≥ 90.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** rota `/leaderboard`.

```text
Dados (inalterados): PlayerRank { PlayerName, Wins, LastWinAt(UTC) }
Consulta (inalterada): GameResultService.GetLeaderboardAsync(10)

Pódio     : ranks[0] centro (ouro, maior) · ranks[1] esquerda (prata) · ranks[2] direita (bronze)
Tabela    : posição (índice + 1) · jogador · vitórias · último triunfo "dd/MM HH:mm" local; linhas 1–3 em ouro
Estados   : carregando · vazio ("Nenhum jogador pontuou ainda." + "Jogar agora")
```

**Arquivos/módulos afetados:** ver `touches`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Pódio completo | 3 ou mais jogadores | 1º ao centro e maior, 2º à esquerda, 3º à direita, com nome e vitórias | UT-01 |
| Pódio parcial | 1 ou 2 jogadores | Só os existentes, sem cartões vazios | UT-02 |
| Tabela | N jogadores | Uma linha por jogador com posição, nome, vitórias e data; 1–3 em ouro | UT-03 |
| Estados | Carregando / sem dados | Indicador acessível / mensagem e botão "Jogar agora" para `/` | UT-04 |
| Sem legado | Página em qualquer estado | Sem `<style>` inline e sem `mud-` | UT-05 |
| Dados reais | Partidas no banco | Ordem por vitórias e desempate por último triunfo, limite 10 | IT-01 |
| Jornada | Abrir `/leaderboard` com dados | Pódio, tabela e ação "Jogar agora" | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado `GetLeaderboardAsync(10)` com vitórias empatadas em contagem, então desempata pelo último triunfo mais recente e ignora empates (já coberto por `SPEC-0017`; fixa o contrato consumido e passa antes da mudança).

**Testes existentes afetados:** nenhum (a página não tinha testes de UI; `LeaderboardTests` cobre só o serviço).

### 7.2 Testes Unitários
- **UT-01** — Dado 4 jogadores, então o pódio tem 3 cartões na ordem visual 2º, 1º, 3º, o 1º tem a classe de maior altura e ouro, e cada cartão mostra "1º/2º/3º", nome e vitórias.
- **UT-02** — Dado 1 e depois 2 jogadores, então o pódio tem 1 e 2 cartões, sem cartões vazios.
- **UT-03** — Dado 5 jogadores, então a tabela tem 5 linhas com posição 1–5, nome, vitórias e `dd/MM HH:mm`, e as linhas 1–3 têm a classe de destaque de ouro.
- **UT-04** — Dado carregando e depois sem dados, então há indicador acessível e, no vazio, mensagem e link "Jogar agora" para `/`.
- **UT-05** — Dado `Leaderboard.razor` em qualquer estado, então o markup não contém `mud-`, o arquivo não contém a tag `<style>` e declara `<PageTitle>` com "Ranking · XO Arena".

### 7.3 Testes de Integração
- **IT-01** — Dado um `GameplayDbContext` InMemory com vitórias de vários jogadores (inclusive empate de contagem), quando a página é renderizada, então a ordem e o limite de 10 respeitam a consulta.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
- **E2E-01** — Jornada do ranking (bUnit): página com dados semeados exibe pódio, tabela e a ação "Jogar agora".

### 7.6 Outros
- Revisão visual (H2): 390px e 1280px contra `ranking-mobile.png`/`ranking-desktop.png`; lista de omitidos conferida.
- Lighthouse Acessibilidade ≥ 90 em `/leaderboard`.

**Dublês e dados de teste:** `GameplayDbContext` EF InMemory (padrão de `LeaderboardTests`).

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto.
- **Dados/schema:** N/A.
- **Compatibilidade:** rota e consulta inalteradas.
- **Observabilidade:** revisão visual e console sem erros.
- **Rollback:** `git revert` do PR.
- **Etapas de migração/coexistência:** etapa 3 de 4 (tela 4 de 4).

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [x] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [x] Escrever os testes `SPEC-0033:CH-01`, `SPEC-0033:UT-01`, `SPEC-0033:UT-02`, `SPEC-0033:UT-03`, `SPEC-0033:UT-04`, `SPEC-0033:UT-05`, `SPEC-0033:IT-01`, `SPEC-0033:E2E-01` com a tag `SPEC-0033:<ID>`, em commits `test(...)` com `Refs: SPEC-0033`, tocando só `test_paths`
- [x] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [x] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0033`)

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [x] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [x] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [x] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [x] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável — **não executada**; aceita no H2 pelo merge do PR #11, ver Pendências
- [x] PR com `spec_graph.py pr SPEC-0033`, CI verde (G5) e aprovação do merge (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
| G1 Red | PASS | `verify SPEC-0033`: Red antes do Green; 8/8 testes rastreados; 6 falhas iniciais pelos motivos esperados (CH-01 e IT-01 guardas) | 2026-09-29 |
| G2 Green | PASS | `dotnet test` 131/131; `dotnet format --verify-no-changes` limpo; `build.sh --check` sem drift | 2026-09-29 |
| G3 Arquitetura | N/A | Sem suíte `Category=Architecture`; ausência de `mud-` e de `<style>` verificada por SPEC-0033:UT-05 | 2026-09-29 |
| G4 Review | PASS | Reviewer independente APPROVED em fb6f68a (0 blocker, 0 major; minors de nomenclatura e transbordo corrigidos) | 2026-09-29 |
| G5 Integração & CI | PASS | PR #11: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorização do usuário para mesclar o PR #11 em 2026-09-29 | 2026-09-29 |
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

Página `/leaderboard` no visual Cyber Arena: pódio 2º–1º–3º (1º ao centro, maior e em ouro; com 1 ou 2 jogadores só os existentes), tabela semântica com posição, jogador, vitórias e último triunfo (top 3 em ouro), estados de carregamento e vazio e a ação Jogar agora. Sem `<style>` inline nem MudBlazor; título da aba com XO Arena.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

`Leaderboard.razor` reescrito com os primitivos `Ui`; consulta e ordenação inalteradas (`GetLeaderboardAsync(10)`). Correções da review G4: propriedade `PodiumOrder` e `min-w-0` para nomes longos.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0033:CH-01 | Dado `GetLeaderboardAsync(10)` com vitórias empatadas em contagem, então desempata pelo último triunfo mais re | PASS | `dotnet test` 131/131 no CI (dotnet-ci) do PR #11 |
| SPEC-0033:UT-01 | Dado 4 jogadores, então o pódio tem 3 cartões na ordem visual 2º, 1º, 3º, o 1º tem a classe de maior altura e  | PASS | `dotnet test` 131/131 no CI (dotnet-ci) do PR #11 |
| SPEC-0033:UT-02 | Dado 1 e depois 2 jogadores, então o pódio tem 1 e 2 cartões, sem cartões vazios. | PASS | `dotnet test` 131/131 no CI (dotnet-ci) do PR #11 |
| SPEC-0033:UT-03 | Dado 5 jogadores, então a tabela tem 5 linhas com posição 1–5, nome, vitórias e `dd/MM HH:mm`, e as linhas 1–3 | PASS | `dotnet test` 131/131 no CI (dotnet-ci) do PR #11 |
| SPEC-0033:UT-04 | Dado carregando e depois sem dados, então há indicador acessível e, no vazio, mensagem e link "Jogar agora" pa | PASS | `dotnet test` 131/131 no CI (dotnet-ci) do PR #11 |
| SPEC-0033:UT-05 | Dado `Leaderboard.razor` em qualquer estado, então o markup não contém `mud-`, o arquivo não contém a tag `<st | PASS | `dotnet test` 131/131 no CI (dotnet-ci) do PR #11 |
| SPEC-0033:IT-01 | Dado um `GameplayDbContext` InMemory com vitórias de vários jogadores (inclusive empate de contagem), quando a | PASS | `dotnet test` 131/131 no CI (dotnet-ci) do PR #11 |
| SPEC-0033:E2E-01 | Jornada do ranking (bUnit): página com dados semeados exibe pódio, tabela e a ação "Jogar agora". | PASS | `dotnet test` 131/131 no CI (dotnet-ci) do PR #11 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #11 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Coluna "Último triunfo" oculta abaixo de `md` (decisão de implementação a validar no H2); estado "carregando" verificado só pelo código-fonte; ordem do pódio no DOM é a visual (2º, 1º, 3º); conferência visual e Lighthouse não executadas.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0033`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
