---
id: SPEC-0005
title: Jogo da Velha
tier: epic
type: feature
status: implemented
created: 2026-09-29
depends_on: [SPEC-0004]
adrs: []
external: []
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0005 — Jogo da Velha (Épico)

<!-- type: feature | migration | foundation (produto novo: ver references/greenfield.md). O épico não é executado: define visão, decisões, decomposição e entrega. Quem executa são as specs filhas (tier full ou lite, tamanho S/M, `parent: SPEC-0005`). A ordem de execução é calculada a partir do frontmatter das filhas (`spec_graph.py waves`). -->

## 1. Visão
Visão.
Visão geral.

## 2. Escopo
**Objetivos (dentro do escopo):**
- 

**Não-objetivos (fora do escopo):**
- 

## 3. Arquitetura Alvo
**Contexto:** 



**Decisões (ADRs):** 

**Regras de arquitetura a garantir (G3):** 

## 4. Decomposição
<!-- Uma linha por spec filha. As colunas de dependência são informativas: a fonte da verdade é o frontmatter de cada filha. -->
| Spec | Título | Tier | Tipo | Tamanho | Depende de | Consome contrato de |
|---|---|---|---|---|---|---|
| SPEC-0006 | Titulo | full | feature | S | | |
| SPEC-0007 | Titulo | full | feature | S | | |
| SPEC-0008 | Titulo | full | feature | S | | |

## 5. Estratégia de Entrega
- **Ambientes:** 
- **Entrega por onda:** 
- **Feature flags:** 
- **Rollback:** 
- **Métricas de sucesso pós-release:** 

## 6. Riscos & Mitigações
- 

## 7. Critérios de Aceite do Épico
- [x] Sucesso (SPEC-0008:E2E-01)

## 8. Questões em Aberto
Nenhuma.

## 9. Aprovação (H1)
Uma aprovação humana cobre o épico e as specs filhas apresentadas junto com ele. Registrada no frontmatter (`approved_by`, `approved_at`) do épico e de cada filha.

## 10. Registro de Impedimentos
<!-- Problemas que envolvem várias filhas (conflito de integração da onda, CI quebrado, decisão transversal). Impedimento que trava uma filha específica vai na própria filha. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 11. Relatório de Entrega
Jogo da Velha multiplayer com matchmaking e gameplay interativo entregue no commit 24ed311.

## 12. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda, aprovada pelo humano. -->
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
