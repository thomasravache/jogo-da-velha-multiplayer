---
id: SPEC-0051
title: Relógio de xadrez com incremento
tier: full
type: feature
user_facing: false
status: implemented
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0049]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Chess/Clock/**, tests/TicTacToe.Tests/ChessClockTests.cs]
adrs: []
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0051 — Relógio de xadrez com incremento

## 1. Visão Geral
Cria o **relógio de xadrez**: controles de tempo (Bullet 1+0, Blitz 5+0, Rápida 10+5), tempo restante por cor, incremento por lance e queda de bandeira (tempo esgotado).

## 2. Motivação & Escopo
**Motivação:** No xadrez o tempo é da partida inteira, por jogador, com incremento; o jogo da velha tem só 15 s por turno. As telas do Stitch mostram o relógio de cada jogador e o aviso de tempo baixo.

**Objetivos (dentro do escopo):**
- `TimeControl` (id, nome, tempo inicial, incremento) e os três controles padrão; `TimeControl.FromId`.
- `ChessClock` com `TimeProvider` injetável: `Start`, `Press`, `Stop`, `Remaining`, `Flagged`.
- Incremento somado ao final do lance; bandeira cai quando o tempo restante chega a zero, avaliada de forma preguiçosa (por consulta, `Tick` ou `Press`).

**Não-objetivos (fora do escopo):**
- Timer em segundo plano e eventos para a interface (a sessão, SPEC-0052, chama `Tick`).
- Atraso (delay) de Bronstein/Fischer diferente do incremento simples, pausa e ajuste manual do relógio.
- Escolha de controle na interface (SPEC-0056).

## 3. Dependências
- **Implementações necessárias:** SPEC-0049 — apenas o projeto `TicTacToe.Modules.Chess` e `PieceColor`.
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `GameSession` já usa `TimeProvider` injetável (SPEC-0036) e o testes usam `ManualTime` (`SeriesRulesTests`); o módulo Chess é independente.

**Decisão:** Relógio puro em `Modules.Chess/Clock`, sem threads: calcula tempo restante a partir do `TimeProvider`.

**Justificativa:** Torna a queda de bandeira determinística nos testes e independente de timer real.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Timer interno com eventos (não determinístico, mais difícil de testar).

**ADRs:** N/A

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Cálculo O(1) por consulta.
- **Segurança:** N/A — sem entrada externa.
- **Privacidade e dados pessoais:** N/A — sem dados pessoais.
- **Disponibilidade e resiliência:** Após a queda de bandeira o relógio para e não volta; consultas repetidas são idempotentes.
- **Acessibilidade (UI):** N/A — sem interface.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `TicTacToe.Modules.Chess: TimeControl, ChessClock`

```text
record TimeControl(string Id, string Name, TimeSpan Initial, TimeSpan Increment)
  static TimeControl Bullet  // Id "bullet1+0",  Name "Bullet 1+0",  1 min, +0 s
  static TimeControl Blitz   // Id "blitz5+0",   Name "Blitz 5+0",   5 min, +0 s
  static TimeControl Rapid   // Id "rapida10+5", Name "Rápida 10+5", 10 min, +5 s
  static IReadOnlyList<TimeControl> All
  static TimeControl? FromId(string id)

sealed class ChessClock(TimeControl control, TimeProvider time)
  // o relógio começa parado: antes do primeiro lance das brancas nenhum tempo corre
  void Press(PieceColor mover)              // `mover` terminou o lance: para o dele (se corria), soma o incremento (se não caiu), inicia o do outro; o primeiro Press (brancas) soma o incremento e inicia o relógio das pretas
  void Stop()                               // partida encerrada: congela
  TimeSpan Remaining(PieceColor color)      // inclui o tempo correndo agora
  PieceColor? Running { get; }
  PieceColor? Flagged { get; }              // quem estourou o tempo (nunca volta)
  void Tick()                               // reavalia a bandeira com o relógio atual
Regras: nenhum tempo é descontado antes do primeiro lance das brancas (o primeiro lance é "de graça"; abortar partida parada fica para spec futura); Remaining nunca é negativo; ao chegar a zero de quem corre → Flagged = essa cor e o relógio para; Press de quem já caiu é ignorado.
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Controles padrão | Bullet, Blitz, Rápida e id desconhecido | Tempos e incrementos do contrato; `FromId` nulo para desconhecido | UT-01 |
| Contagem | Antes e depois do primeiro lance | Nada corre antes; depois só corre o relógio de quem joga | UT-02 |
| Incremento | Lance no controle 10+5 | Tempo do jogador soma 5 s ao fim do lance | UT-03 |
| Bandeira | Tempo esgotado antes do lance | `Flagged` na cor certa, relógio parado, sem incremento | UT-04 |
| Parar | `Stop` e `Press` depois | Congelado; `Press` ignorado | UT-05 |
| Consultas repetidas | Várias consultas no mesmo instante e sem avanço | Mesmos valores; sem efeito colateral | UT-06 |
| Fluxo de uma partida | Sequência de lances com relógio simulado | Tempos e bandeira coerentes do começo ao fim | IT-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — código novo.

### 7.2 Testes Unitários
- **UT-01** — Dado `TimeControl.All` e `FromId`, então há três controles com os tempos do contrato e `FromId("x")` retorna nulo.
- **UT-02** — Dado um relógio novo e 30 s passando, então os dois tempos seguem inteiros e `Running` é nulo; após `Press(White)` (primeiro lance) e mais 30 s, `Remaining(Black)` diminui 30 s e `Remaining(White)` não muda.
- **UT-03** — Dado o controle 10+5, o primeiro lance das brancas e, depois de 20 s, o das pretas, então `Remaining(Black)` = 10 min − 20 s + 5 s e o relógio das brancas passa a correr; o primeiro lance das brancas soma incremento também.
- **UT-04** — Dado um relógio em andamento e 1 minuto sem lance de quem joga, então `Flagged` é essa cor, `Remaining` é zero, o relógio para e um `Press` posterior não adiciona incremento.
- **UT-05** — Dado `Stop`, então o tempo não corre mais e `Press` é ignorado.
- **UT-06** — Dado consultas repetidas de `Remaining`, `Flagged` e `Tick` sem avanço do relógio, então os valores não mudam.

### 7.3 Testes de Integração
- **IT-01** — Dado um `ChessClock` com `ManualTime` e uma sequência de dez lances com durações variadas, então os tempos restantes finais somam o esperado (incluindo incrementos) e a bandeira cai só quando o tempo de quem joga acaba.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
N/A — `user_facing: false`.

### 7.6 Outros
- N/A

**Dublês e dados de teste:** `ManualTime` (já existente em `SeriesRulesTests`), sem timer real.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; sem consumidores até a SPEC-0052.
- **Dados/schema:** N/A
- **Compatibilidade:** N/A — código novo.
- **Observabilidade:** N/A
- **Rollback:** Reverter o PR.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Quais controles de tempo? — Bullet 1+0, Blitz 5+0 e Rápida 10+5, como no Stitch (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold**
- [x] Commit `chore(...)` só com assinaturas/tipos vazios do contrato (sem lógica), compilando

**Fase 1: Testes (Red)**
- [x] Escrever `SPEC-0051:IT-01`, `SPEC-0051:UT-01`, `SPEC-0051:UT-02`, `SPEC-0051:UT-03`, `SPEC-0051:UT-04`, `SPEC-0051:UT-05`, `SPEC-0051:UT-06` com a tag `SPEC-0051:<ID>` em commits `test(...)` com `Refs: SPEC-0051` e confirmar que falham pelo motivo certo

**Fase 2: Implementação (Green)**
- [x] Implementar o mínimo para passar, seguindo o padrão de referência e o `touches` da spec
- [x] Refactor mantendo tudo verde
- [x] Validar: `dotnet build`, suíte completa, `dotnet format --verify-no-changes` e `verify SPEC-0051 --base origin/main`

**Fase final: Integração, entrega e documentação**
- [x] Review independente (G4)
- [x] Integração + CI verde (G5) e aprovação (H2)
- [x] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
| G1 Red | PASS | verify SPEC-0051: Red antes do Green (a7bb7ef), 7/7 testes do plano rastreados | 2026-09-29 |
| G2 Green | PASS | dotnet test 521 verdes (1 pulado: perft pesado); format limpo; verify PASS | 2026-09-29 |
| G3 Arquitetura | N/A | fronteira de módulos já coberta por ChessModuleBoundaryTests (SPEC-0049) | 2026-09-29 |
| G4 Review | PASS | Review independente (subagente): PASS; achado maior (sem lock) e menor (dois GetUtcNow por operação) corrigidos com refactor e 7 testes de casos-limite e concorrência. Texto do contrato alinhado ao UT-03 (primeiro lance soma incremento). Dívida: o teste de concorrência é guarda de regressão, não prova o lock | 2026-09-29 |
| G5 Integração & CI | PASS | PR #36: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
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

Relógio de xadrez: controles Bullet 1+0, Blitz 5+0 e Rápida 10+5, tempo restante por cor, incremento por lance e queda de bandeira, com o relógio parado até o primeiro lance das brancas.

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

ChessClock puro com TimeProvider injetável, avaliação preguiçosa da bandeira, lock privado e um único instante por operação; TimeControl com ids estáveis e FromId.

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

N/A

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->

| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0051:UT-01 | Dado `TimeControl.All` e `FromId`, então há três controles com os tempos do contrato e `FromId("x")` retorna n | PASS | `dotnet test` 522/522 no CI (dotnet-ci) do PR #36 |
| SPEC-0051:UT-02 | Dado um relógio novo e 30 s passando, então os dois tempos seguem inteiros e `Running` é nulo; após `Press(Whi | PASS | `dotnet test` 522/522 no CI (dotnet-ci) do PR #36 |
| SPEC-0051:UT-03 | Dado o controle 10+5, o primeiro lance das brancas e, depois de 20 s, o das pretas, então `Remaining(Black)` = | PASS | `dotnet test` 522/522 no CI (dotnet-ci) do PR #36 |
| SPEC-0051:UT-04 | Dado um relógio em andamento e 1 minuto sem lance de quem joga, então `Flagged` é essa cor, `Remaining` é zero | PASS | `dotnet test` 522/522 no CI (dotnet-ci) do PR #36 |
| SPEC-0051:UT-05 | Dado `Stop`, então o tempo não corre mais e `Press` é ignorado. | PASS | `dotnet test` 522/522 no CI (dotnet-ci) do PR #36 |
| SPEC-0051:UT-06 | Dado consultas repetidas de `Remaining`, `Flagged` e `Tick` sem avanço do relógio, então os valores não mudam. | PASS | `dotnet test` 522/522 no CI (dotnet-ci) do PR #36 |
| SPEC-0051:IT-01 | Dado um `ChessClock` com `ManualTime` e uma sequência de dez lances com durações variadas, então os tempos res | PASS | `dotnet test` 522/522 no CI (dotnet-ci) do PR #36 |

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

G6 N/A (aprovado pelo usuário em 2026-09-29): o repositório não tem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #36 com CI verde (Build, Format & Test e sdd).

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

O teste de concorrência é guarda de regressão e não prova o lock; abortar partida parada antes do primeiro lance fica para spec futura.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0051`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
