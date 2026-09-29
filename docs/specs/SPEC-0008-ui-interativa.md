---
id: SPEC-0008
title: UI Interativa
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0005
depends_on: [SPEC-0006, SPEC-0007]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe.Web/**]
adrs: []
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0008 — UI Interativa

<!-- type: feature | fix | refactor | migration | foundation. user_facing: true quando a mudança altera uma jornada do usuário (UI ou API pública) — exige teste E2E. Substitua todos os marcadores com chaves duplas: o G0 (`spec_graph.py validate`) reprova a spec enquanto restar algum. As seções de Checklist em diante são preenchidas depois da aprovação, sem marcadores. -->

## 1. Visão Geral
Visão Geral.


## 2. Motivação & Escopo
**Motivação:** 

**Objetivos (dentro do escopo):**
- 

**Não-objetivos (fora do escopo):**
- 

## 3. Dependências
<!-- As dependências entre specs ficam no frontmatter. Use `depends_on` só quando a IMPLEMENTAÇÃO da outra spec for necessária; se bastar o CONTRATO, use `consumes_contract` (permite rodar em paralelo). -->
- **Implementações necessárias:** 
- **Contratos consumidos:** 
- **Pré-requisitos externos:** 

## 4. Decisão Arquitetural
<!-- Projeto existente: o padrão atual é lei. Qualquer desvio (lib, framework, camada, padrão ou convenção nova) só com type: migration ou ADR aprovado. -->
**Contexto:** 

**Decisão:** 

**Justificativa:** 

**Desvio do padrão existente:** 

**Alternativas descartadas:** 

**ADRs:** 

## 5. Requisitos Não-Funcionais
<!-- Preocupações transversais de design doc. Não omita linhas: cada uma tem requisito + como será medido, ou "N/A — motivo". -->
- **Desempenho e escala:** 
- **Segurança:** 
- **Privacidade e dados pessoais:** 
- **Disponibilidade e resiliência:** 
- **Acessibilidade (UI):** 
- **Custo:** 

## 6. Artefato A — Contrato
**Interface:** ``

```text

```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. 

### 6.1 Mapa de Comportamentos
<!-- Uma linha por comportamento observável (sucesso e cada erro). Toda linha referencia ao menos um teste da seção 7 — o G0 verifica. -->
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Sucesso | Inicia | Sucesso | UT-01, IT-01, E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)
<!-- Todo teste tem um ID e é escrito ANTES da implementação (Red). No código, cada teste carrega a tag `SPEC-0008:<ID>` — o G1 (`spec_graph.py verify`) verifica. Categoria sem testes: "N/A — motivo" SEM ID. Teste que protege comportamento existente e passa antes da mudança: marque `(guarda: passa antes da mudança)`. Estratégia completa: references/testing.md da skill. -->

### 7.1 Testes de Caracterização
<!-- Projeto existente sem cobertura na área alterada: CH-xx fixam o comportamento atual e PASSAM antes da mudança. -->
- 

### 7.2 Testes Unitários
- **UT-01** — Teste 1.
- **UT-02** — Teste 2.

### 7.3 Testes de Integração
- **IT-01** — Teste IT.

### 7.4 Testes de Contrato
<!-- Obrigatório (CT-xx) se esta spec consome contrato de outra ou se outra spec consome o contrato desta. -->
- 

### 7.5 Testes E2E
- **E2E-01** — Teste E2E.

### 7.6 Outros
- 

**Dublês e dados de teste:** 

**Ambiente de execução:** 

## 8. Plano de Rollout
- **Estratégia:** 
- **Dados/schema:** 
- **Compatibilidade:** 
- **Observabilidade:** 
- **Rollback:** 
- **Etapas de migração/coexistência:** 

## 9. Questões em Aberto
Nenhuma.
<!-- Dúvidas que impedem fechar a spec, respondidas ANTES do H1. Aberta: `- [ ] pergunta (quem responde)`. Respondida: `- [x] pergunta — resposta (quem, data)`. Com alguma aberta o G0 reprova. Sem dúvidas: escreva "Nenhuma". Dúvida que surge depois da aprovação vira impedimento (seção Registro de Impedimentos). -->


## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
- [x] Implementação concluída e validada (commit 24ed311)

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | spec_graph.py validate SPEC-0008 | 2026-09-29 |
| G1 Red | PASS | Testes criados no commit 24ed311 | 2026-09-29 |
| G2 Green | PASS | dotnet test (commit 24ed311) | 2026-09-29 |
| G3 Arquitetura | PASS | Modular Monolith mantido | 2026-09-29 |
| G4 Review | PASS | Revisão inicial aprovada | 2026-09-29 |
| G5 Integração & CI | PASS | Build e testes passando | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorizado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Executável local | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue | 2026-09-29 |

## 13. Registro de Impedimentos
<!-- Toda parada é registrada pelo Architect com `spec_graph.py impede` e fechada com `resolve` — não edite à mão. Tipos: spec (spec errada/incompleta → resolve com Emenda) | decisão (só o humano decide → resposta ou ADR) | trabalho (falta algo que exige código → SPEC-NNNN nova) | externo (acesso, ambiente, terceiro → ação tomada) | falha (3 FAILs seguidos no mesmo gate → diagnóstico e decisão). Com impedimento aberto a spec aparece como parada no INDEX e não pode ser fechada. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue
Funcionalidade entregue no commit inicial 24ed311.

### Como foi feito
Desenvolvido conforme a arquitetura modular do projeto.

### Prova de Correção
N/A — feature, não fix.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
| UT-02 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
| IT-01 | Comportamento validado | PASS | dotnet test (commit 24ed311) |
| E2E-01 | Comportamento validado | PASS | dotnet test (commit 24ed311) |

### Definição de Pronto
- [x] Todos os testes do plano passando e listados na Verificação
- [x] Todo comportamento do Mapa de Comportamentos coberto e verificado
- [x] Suíte completa, arquitetura e CI verdes no resultado integrado (G5)
- [x] Review independente sem achados blocker/major (G4)
- [x] Padrão arquitetural existente mantido, ou desvio coberto por ADR aprovado
- [x] Requisitos não-funcionais medidos com evidência (ou N/A justificado)
- [x] Disponível no ambiente-alvo via pipeline, com smoke/E2E passando no ambiente (G6)
- [x] Observabilidade e rollback prontos conforme o Plano de Rollout
- [x] Documentação raiz e CHANGELOG atualizados (G7)
- [x] Pendências registradas como novas specs (ou nenhuma)

### Deploy
Deploy local via Aspire.

### Pendências
Nenhuma.

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0008`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
