---
id: SPEC-0023
title: Infraestrutura de Cobertura de Código e Testes com bUnit
tier: full
type: foundation
user_facing: false
status: approved
created: 2026-09-29
parent: SPEC-0021
depends_on: [SPEC-0022]
consumes_contract: []
contract_version: 1
touches: [tests/TicTacToe.Tests/**, docs/specs/sdd-config.yml]
adrs: [ADR-0006]
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0023 — Infraestrutura de Cobertura de Código e Testes com bUnit

## 1. Visão Geral
Equipar a suíte de testes com a biblioteca `bUnit` para possibilitar testes unitários de componentes Blazor em memória e integrar o coletor de cobertura de código `coverlet.collector`, registrando o comando de cobertura no `sdd-config.yml`.

## 2. Motivação & Escopo
**Motivação:** Atualmente a suíte de testes cobre regras de negócio de domínio em C#, mas não há mecanismo para testar os componentes de interface Razor sem subir um navegador completo. Além disso, a cobertura de testes não é quantificada automaticamente.

**Objetivos (dentro do escopo):**
- Adicionar referências aos pacotes `bunit` e `coverlet.collector` em `TicTacToe.Tests.csproj`.
- Configurar `coverage: "dotnet test --collect:\"XPlat Code Coverage\""` no `sdd-config.yml`.
- Criar o primeiro teste de componente bUnit validando renderização de componentes Blazor.

**Não-objetivos (fora do escopo):**
- Reescrever os componentes Blazor existentes (escopo da SPEC-0024).

## 3. Dependências
- **Implementações necessárias:** SPEC-0022 (compilação estrita e padronização da solução).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** Pacotes NuGet `bunit` e `coverlet.collector`.

## 4. Decisão Arquitetural
**Contexto:** Padrão documentado em ADR-0006.

**Decisão:** Usar `bUnit` como framework padrão de testes de componentes Blazor integrados ao xUnit e Coverlet para relatórios OpenCover/Cobertura.

**Justificativa:** Execução ultrarrápida em memória (< 100ms), sem necessidade de drivers de browser externos (como Playwright ou Selenium) para a grande maioria dos cenários de UI.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Playwright/Selenium descartados para testes unitários de componentes por serem ordens de grandeza mais lentos e frágeis em pipelines de CI.

**ADRs:** ADR-0006.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Execução da suíte completa de testes com cobertura em menos de 5 segundos.
- **Segurança:** N/A.
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** N/A.
- **Acessibilidade (UI):** N/A.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `tests/TicTacToe.Tests/` e `docs/specs/sdd-config.yml`

```yaml
coverage: "dotnet test --collect:\"XPlat Code Coverage\""
coverage_min_changed_lines: 80
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Suporte a testes bUnit | Executar teste herdando de `BunitContext` | Componente Razor é renderizado no DOM virtual | UT-01, IT-01 |
| Configuração de cobertura | `sdd-config.yml` inspecionado | Campo `coverage` contém comando com Cobertura/XPlat | UT-02 |
| Execução de cobertura | Executar comando de cobertura | Gera artefatos de cobertura com sucesso | IT-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — infraestrutura de testes.

### 7.2 Testes Unitários
- **UT-01** — Dado o projeto `TicTacToe.Tests`, quando os pacotes são inspecionados, contém referência para `bunit`.
- **UT-02** — Dado o arquivo `sdd-config.yml`, o campo `coverage` está preenchido com o comando de coleta.

### 7.3 Testes de Integração
- **IT-01** — Um teste de componente `bUnit` renderiza com sucesso um componente Blazor em memória e verifica elementos renderizados.

### 7.4 Testes de Contrato
- N/A.

### 7.5 Testes E2E
- N/A — user_facing: false.

### 7.6 Outros
- N/A.

**Dublês e dados de teste:** `BunitContext` em memória.

**Ambiente de execução:** xUnit via `dotnet test`.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto no branch principal.
- **Dados/schema:** N/A.
- **Compatibilidade:** N/A.
- **Observabilidade:** Logs do Coverlet e relatório do test runner.
- **Rollback:** `git revert`.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Aguardando aprovação humana.

## 11. Checklist de Implementação

**Fase 1: Testes (Red)**
- [ ] Escrever testes em `tests/TicTacToe.Tests/CoverageAndBUnitHarnessTests.cs` com tags `SPEC-0023:UT-01` e `SPEC-0023:UT-02`
- [ ] Confirmar que os testes falham antes da implementação (Red)

**Fase 2: Implementação (Green)**
- [ ] Adicionar referências de pacote `bunit` e `coverlet.collector` em `TicTacToe.Tests.csproj`
- [ ] Configurar `coverage: "dotnet test --collect:\"XPlat Code Coverage\""` no `sdd-config.yml`
- [ ] Escrever primeiro teste de componente bUnit (`SPEC-0023:IT-01`)
- [ ] Confirmar que todos os testes passam (Green)

**Fase 3: Refactor & Qualidade**
- [ ] Executar build completo e suíte de testes (`dotnet test`)
- [ ] Registrar evidências dos gates G1–G4

**Fase final: Integração e Entrega**
- [ ] Preencher Relatório de Entrega
- [ ] Fechar spec (G7) e atualizar INDEX.md

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate`: 0 erros | 2026-09-29 |
| G1 Red | PENDING | | |
| G2 Green | PENDING | | |
| G3 Arquitetura | PENDING | | |
| G4 Review | PENDING | | |
| G5 Integração & CI | PENDING | | |
| H2 Integração aprovada | PENDING | | |
| G6 Deploy | PENDING | | |
| G7 Pronto & Docs | PENDING | | |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue

### Como foi feito

### Prova de Correção
N/A — tipo foundation.

### Verificação
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

### Pendências

## 15. Emendas
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
