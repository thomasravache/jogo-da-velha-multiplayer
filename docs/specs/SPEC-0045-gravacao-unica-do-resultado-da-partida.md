---
id: SPEC-0045
title: Gravação única do resultado da partida
tier: full
type: fix
user_facing: false
status: implemented
created: 2026-09-29
parent: SPEC-0035
depends_on: []
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs, tests/TicTacToe.Tests/SingleResultRecordingTests.cs]
adrs: []
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0045 — Gravação única do resultado da partida

## 1. Visão Geral
Corrige a gravação do resultado para que **cada partida terminada gere exatamente uma linha** em `MatchResult`, independente de quantos jogadores (circuitos) a observam. Hoje, numa partida online, cada jogador tem a sua instância de `Home` assinada no mesmo evento do jogo e cada uma grava o resultado, o que **duplicaria** partidas no histórico e as vitórias no ranking. É pré-requisito das specs que dependem de contagens corretas (SPEC-0036, SPEC-0038, SPEC-0039, SPEC-0041 e SPEC-0042).

## 2. Motivação & Escopo
**Motivação:** histórico e ranking (SPEC-0010, SPEC-0017) só são confiáveis se cada partida contar uma vez. A Fase 2 constrói filtros, aproveitamento e sequência em cima desses números.

**Problema (achado na leitura do código, ainda não reproduzido em execução — o teste de regressão confirma no Red):** em `Home.razor.cs`, a flag `_resultSaved` é um **campo da instância** de `Home`, isto é, do circuito. `OnGameStateChanged` é assinado por cada `Home` que entra na mesma partida (`EnsureGameExists` adiciona o handler), e o evento `GameSession.OnStateChanged` dispara para todos. Ao fim da partida, cada instância vê `_resultSaved == false` e chama `GameResultService.SaveResultAsync`. Em solo, a jogada do robô roda em `Task.Run` e concorre com `OnGameStateChanged` sobre a mesma flag (checar e depois marcar, sem atomicidade), o que também pode gravar duas vezes.

**Causa raiz:** a decisão "este resultado já foi gravado" está no lugar errado (por circuito, sem atomicidade) em vez de na partida, que é o objeto compartilhado.

**Objetivos (dentro do escopo):**
- `GameSession.TryMarkResultRecorded()`: marca atomicamente (sob o `lock` existente) que o resultado da rodada atual foi gravado; devolve verdadeiro só para o primeiro chamador; volta a zero em `Restart()`.
- `GameResultService.SaveOnceAsync(GameSession)`: grava somente se `TryMarkResultRecorded()` devolver verdadeiro.
- `Home.razor.cs`: substituir os três pontos que gravam (`OnGameStateChanged`, jogada humana, jogada do robô) por `SaveOnceAsync` e remover `_resultSaved`.

**Não-objetivos (fora do escopo):**
- Novos campos do resultado (SPEC-0036) e identidade (SPEC-0037).
- Remover duplicatas **já gravadas** (ver Questões em Aberto).
- Gravar resultado quando nenhum circuito permanece vivo (limitação aceita).

## 3. Dependências
- **Implementações necessárias:** N/A. Por ser correção de dados, vai primeiro; a SPEC-0031 (arena) também altera `Home.razor.cs` e `GameSession`, então as duas se ordenam por arquivos em comum e a arena parte do código já corrigido.
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** `GameSession` é compartilhada entre circuitos (`ConcurrentDictionary<Guid, GameSession>`), com `lock` para mudanças de estado; `GameResultService.SaveResultAsync` grava e registra falhas em log (SPEC-0010).

**Decisão:** a garantia "uma vez por rodada" fica na `GameSession` (fonte única de verdade) e o serviço a respeita; `SaveResultAsync` permanece, e passa a ser chamado apenas por `SaveOnceAsync`.

**Justificativa:** o objeto compartilhado é o único ponto onde a atomicidade é possível sem coordenação entre circuitos.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** deduplicar no banco por chave única (não há identificador natural da rodada; adiciona migration); só o "jogador X" grava (falha se o X sair antes do fim); serviço singleton assinando todas as sessões (mudança arquitetural maior que o defeito).

**ADRs:** N/A.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** um teste-e-marca sob `lock` já existente; custo desprezível.
- **Segurança:** N/A — sem entrada de usuário.
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** se a gravação falhar (exceção registrada em log), a rodada permanece marcada como gravada e **não** é regravada (evita duplicar em falhas parciais); a falha continua sem interromper a partida, como hoje.
- **Acessibilidade (UI):** N/A — sem UI.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `GameSession.TryMarkResultRecorded()` e `GameResultService.SaveOnceAsync(GameSession)`.

```text
GameSession
  bool TryMarkResultRecorded()      // true no primeiro chamador da rodada; false depois; zerado em Restart()

GameResultService
  Task<bool> SaveOnceAsync(GameSession game)   // true se gravou; false se já gravado ou partida não terminada
    // condição de gravação: Winner != None || IsDraw (inclui W.O.)
```

**Arquivos/módulos afetados:** ver `touches`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Primeira chamada | Partida terminada | `TryMarkResultRecorded()` verdadeiro | UT-01 |
| Chamadas seguintes | Mesma rodada | Falso | UT-01 |
| Concorrência | 50 chamadas simultâneas | Exatamente uma verdadeira | UT-02 |
| Nova rodada | Após `Restart()` | Volta a permitir uma gravação | UT-03 |
| Partida não terminada | Em andamento | `SaveOnceAsync` não grava | UT-04 |
| Gravação única no serviço | Duas chamadas seguidas e concorrentes | Uma linha no banco | IT-01 |
| Duas telas na partida online | Dois `Home` observando a mesma partida | Uma linha por partida (falha antes da correção) | IT-02 |
| Solo com robô | Jogada humana e jogada do robô encerram a partida | Uma linha | IT-02 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado `SaveResultAsync` com uma vitória e um empate, então grava `WinnerName` e `null` como hoje (já coberto por `SPEC-0010:UT-01/UT-02`; permanece como guarda).

### 7.2 Testes Unitários
- **UT-01** — Dado uma partida terminada, então a primeira chamada a `TryMarkResultRecorded()` devolve verdadeiro e as seguintes falso.
- **UT-02** — Dado 50 tarefas chamando `TryMarkResultRecorded()` ao mesmo tempo, então exatamente uma recebe verdadeiro.
- **UT-03** — Dado uma partida já marcada, quando `Restart()` acontece e a nova rodada termina, então `TryMarkResultRecorded()` volta a devolver verdadeiro uma vez.
- **UT-04** — Dado uma partida em andamento, então `SaveOnceAsync` devolve falso e nenhuma linha é gravada; dada uma partida encerrada por W.O., grava.

### 7.3 Testes de Integração
- **IT-01** — Dado `GameplayDbContext` InMemory, quando `SaveOnceAsync` é chamado duas vezes seguidas e depois 20 vezes em paralelo para a mesma partida terminada, então existe exatamente uma linha.
- **IT-02** — **Regressão (falha antes da correção):** dado dois `Home` no bUnit assinados na mesma partida online, quando a partida termina, então há uma única linha em `MatchResult`; e numa partida solo em que a jogada do robô encerra a partida, também uma única linha.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
N/A — `user_facing: false`; o efeito aparece no histórico e no ranking, cobertos pelas SPEC-0038 e SPEC-0039.

### 7.6 Outros
- Verificação manual antes do Red: contar linhas repetidas no banco local após uma partida online entre duas abas (confirma o defeito descrito).

**Dublês e dados de teste:** EF InMemory, `GameSession(enableBackgroundTimer: false)`, dois `Home` no mesmo dicionário de partidas.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto.
- **Dados/schema:** N/A — sem migration; as duplicatas já gravadas não são alteradas por esta spec.
- **Compatibilidade:** `SaveResultAsync` permanece; nenhuma tela muda.
- **Observabilidade:** log de depuração quando `SaveOnceAsync` ignora uma segunda gravação.
- **Rollback:** `git revert` do PR.
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
- [x] O que fazer com as **duplicatas já gravadas** (partidas online antigas, se o defeito se confirmar)? Recomendado: **aceitar o legado** e registrar a limitação; a alternativa é uma spec à parte com um script de limpeza **revisável e sem exclusão automática** (a heurística seria: mesmos jogadores, vencedor e horário em poucos segundos). — Aceitar o legado e registrar a limitação. (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [x] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [x] Escrever os testes `SPEC-0045:CH-01`, `SPEC-0045:UT-01`, `SPEC-0045:UT-02`, `SPEC-0045:UT-03`, `SPEC-0045:UT-04`, `SPEC-0045:IT-01`, `SPEC-0045:IT-02` com a tag `SPEC-0045:<ID>`, em commits `test(...)` com `Refs: SPEC-0045`, tocando só `test_paths`
- [x] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [x] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0045`)

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [x] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [x] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [x] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [x] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável — N/A (sem UI)
- [x] PR com `spec_graph.py pr SPEC-0045`, CI verde (G5) e aprovação do merge (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
| G1 Red | PASS | `verify SPEC-0045`: primeiro red 61b7e55; IT-02 falhou com 2 linhas (esperado 1); 7/7 testes rastreados | 2026-09-29 |
| G2 Green | PASS | `dotnet test` 76/76; `dotnet format --verify-no-changes` limpo; fix d06c33c | 2026-09-29 |
| G3 Arquitetura | N/A | Sem suíte `Category=Architecture` no projeto; nenhuma dependência entre módulos alterada | 2026-09-29 |
| G4 Review | PASS | Reviewer independente APPROVED em 80e2b69 (0 blocker, 0 major, 5 minor) | 2026-09-29 |
| G5 Integração & CI | PASS | PR #4: Build, Format & Test e sdd verdes; mesclado em 228e738 | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorização do merge do PR #4 pelo usuário em 2026-09-29 | 2026-09-29 |
| G6 Deploy | N/A | Sem ambiente remoto (`staging_url` vazio); aprovado pelo usuário em 2026-09-29 | 2026-09-29 |
| G7 Pronto & Docs | PASS | `spec_graph.py validate` limpo; Relatório de Entrega, README/CHANGELOG atualizados | 2026-09-29 |

## 13. Registro de Impedimentos
<!-- Toda parada é registrada pelo Architect com `spec_graph.py impede` e fechada com `resolve` — não edite à mão. Tipos: spec (spec errada/incompleta → resolve com Emenda) | decisão (só o humano decide → resposta ou ADR) | trabalho (falta algo que exige código → SPEC-NNNN nova) | externo (acesso, ambiente, terceiro → ação tomada) | falha (3 FAILs seguidos no mesmo gate → diagnóstico e decisão). Com impedimento aberto a spec aparece como parada no INDEX e não pode ser fechada. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega
<!-- Preenchido no CLOSE (G7). Diz o que foi feito, como, e prova que foi resolvido. Para status implemented o validate exige todas as subseções preenchidas, todo teste do plano com PASS + evidência e a Definição de Pronto toda marcada. -->

### O que foi entregue
<!-- comportamento entregue do ponto de vista do usuário/sistema -->

Cada partida terminada passa a gerar exatamente uma linha em `MatchResult`, mesmo com dois jogadores (circuitos) observando a mesma partida.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

`GameSession.TryMarkResultRecorded()` atômico sob o `lock` (zerado no `Restart`) e `GameResultService.SaveOnceAsync`; `Home.razor.cs` passou a usar `SaveOnceAsync` nos três pontos de gravação e perdeu o `_resultSaved` por circuito. Correção adicional do CI: `LogDebug` protegido por `IsEnabled` (CA1873). Duplicatas já gravadas antes da correção foram mantidas (decisão registrada no H1).

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

Regressão `SPEC-0045:IT-02`: falhou no commit 4f34318 (2 linhas em vez de 1) e passou no commit 42c0370.

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0045:CH-01 | Dado `SaveResultAsync` com uma vitória e um empate, então grava `WinnerName` e `null` como hoje (já coberto po | PASS | `dotnet test` 76/76 no CI (dotnet-ci) do PR #4 |
| SPEC-0045:UT-01 | Dado uma partida terminada, então a primeira chamada a `TryMarkResultRecorded()` devolve verdadeiro e as segui | PASS | `dotnet test` 76/76 no CI (dotnet-ci) do PR #4 |
| SPEC-0045:UT-02 | Dado 50 tarefas chamando `TryMarkResultRecorded()` ao mesmo tempo, então exatamente uma recebe verdadeiro. | PASS | `dotnet test` 76/76 no CI (dotnet-ci) do PR #4 |
| SPEC-0045:UT-03 | Dado uma partida já marcada, quando `Restart()` acontece e a nova rodada termina, então `TryMarkResultRecorded | PASS | `dotnet test` 76/76 no CI (dotnet-ci) do PR #4 |
| SPEC-0045:UT-04 | Dado uma partida em andamento, então `SaveOnceAsync` devolve falso e nenhuma linha é gravada; dada uma partida | PASS | `dotnet test` 76/76 no CI (dotnet-ci) do PR #4 |
| SPEC-0045:IT-01 | Dado `GameplayDbContext` InMemory, quando `SaveOnceAsync` é chamado duas vezes seguidas e depois 20 vezes em p | PASS | `dotnet test` 76/76 no CI (dotnet-ci) do PR #4 |
| SPEC-0045:IT-02 | **Regressão (falha antes da correção):** dado dois `Home` no bUnit assinados na mesma partida online, quando a | PASS | `dotnet test` 76/76 no CI (dotnet-ci) do PR #4 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #4 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

Nenhuma. Minors sem correção: janela estreita entre o teste de fim de partida e a marcação; IT-02 com espera fixa de 500 ms; sem teste de empate em `SaveOnceAsync`.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0045`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
