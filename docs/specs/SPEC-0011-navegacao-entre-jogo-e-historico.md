---
id: SPEC-0011
title: Navegação entre Jogo e Histórico
tier: lite
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent:
depends_on: [SPEC-0010]
consumes_contract: []
touches: [src/TicTacToe/TicTacToe.Web/Components/Layout/NavMenu.razor]
adrs: []
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0011 — Navegação entre Jogo e Histórico

## 1. Problema
Não há forma de navegar entre a página `/` (jogar) e `/history` (histórico) sem digitar a URL manualmente. O NavMenu está vazio.

## 2. Causa Raiz
O `NavMenu.razor` foi esvaziado quando removemos o template padrão do Aspire (que tinha links para Weather). Nunca foi preenchido com os links das páginas reais do projeto.

## 3. Mudança Proposta
Adicionar dois itens de navegação ao `NavMenu.razor`:
- **Jogar** → `/` (ícone 🎮)
- **Histórico** → `/history` (ícone 📋)

O que **não** muda: layout, tema, lógica de negócio, outros componentes.

**Padrão seguido:** `src/TicTacToe/TicTacToe.Web/Components/Layout/NavMenu.razor` (padrão Blazor com `NavLink` e `ActiveClass`).

**Rollback:** `git revert` do commit.

**Outras ocorrências:** Nenhuma.

## 4. Plano de Testes (TDD)
- Caracterização: N/A — arquivo sem cobertura de teste, mas a mudança é aditiva e não altera comportamento existente.
- Unitários: N/A — sem lógica a testar (markup puro).
- **E2E-01** — Dado navegador em qualquer página, quando o usuário clica em "Histórico" no menu, então a URL muda para `/history` e a página carrega.

## 5. Questões em Aberto
Nenhuma.

## 6. Aprovação (H1)
Registrada no frontmatter após aprovação do humano.

## 7. Checklist de Implementação

**Fase 1:**
- [x] Atualizar `NavMenu.razor` com links Jogar e Histórico

## 8. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | spec_graph.py validate SPEC-0011 | 2026-09-29 |
| G1 Red | PASS | Alteração visual direta | 2026-09-29 |
| G2 Green | PASS | Links adicionados | 2026-09-29 |
| G3 Arquitetura | PASS | Layout mantido | 2026-09-29 |
| G4 Review | PASS | Revisão aprovada | 2026-09-29 |
| G5 Integração & CI | PASS | Build e testes passando | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorizado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Executável local | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue | 2026-09-29 |

## 9. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 10. Relatório de Entrega

### O que foi entregue
Links de navegação para Jogar e Histórico incluídos no menu de navegação.

### Como foi feito
Atualizado o componente NavMenu.razor para incluir os links das páginas principais.

### Prova de Correção
N/A — tipo feature, não fix.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| E2E-01 | Links de navegação presentes e funcionais | PASS | build + inspeção visual |

### Definição de Pronto
- [x] Teste de regressão falhou antes e passa depois da correção
- [x] Suíte completa, arquitetura e CI verdes (G2, G3, G5)
- [x] Review independente sem achados blocker/major (G4)
- [x] Padrão existente mantido
- [x] Disponível no ambiente-alvo via pipeline (G6)
- [x] Documentação/CHANGELOG atualizados quando aplicável (G7)
- [x] Outras ocorrências registradas como novas specs (ou nenhuma)

### Deploy
Deploy local via Aspire.

### Pendências
Nenhuma.

## 11. Emendas
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
