# Índice de Specs

> Gerado por `spec_graph.py index` em 2026-09-29 — não edite manualmente.

## Saúde

- Validação (G0): **76 erro(s), 13 aviso(s)** — rode `spec_graph.py validate`
- Specs: implemented 9
- Impedimentos: **0 aberto(s)**, 0 resolvido(s)

## Plano de Execução

Nenhuma spec aberta.

## Épicos

| Épico | Título | Status | Progresso |
|---|---|---|---|
| SPEC-0001 | Fundação do Projeto | implemented | 3/3 implementadas |
| SPEC-0005 | Jogo da Velha | implemented | 3/3 implementadas |

## Todas as Specs

| ID | Título | Tier | Tipo | Status | Criada | Épico | Depende de | Consome contrato |
|---|---|---|---|---|---|---|---|---|
| [SPEC-0001](SPEC-0001-fundacao-do-projeto.md) | Fundação do Projeto | epic | foundation | implemented | 2026-09-29 | — | — | — |
| [SPEC-0002](SPEC-0002-repositorio-e-ferramental.md) | Repositório e Ferramental | full | foundation | implemented | 2026-09-29 | SPEC-0001 | — | — |
| [SPEC-0003](SPEC-0003-harness-de-testes.md) | Harness de Testes | full | foundation | implemented | 2026-09-29 | SPEC-0001 | SPEC-0002 | — |
| [SPEC-0004](SPEC-0004-walking-skeleton-blazor-aspire-sql.md) | Walking Skeleton (Blazor + Aspire + SQL) | full | foundation | implemented | 2026-09-29 | SPEC-0001 | SPEC-0003 | — |
| [SPEC-0005](SPEC-0005-jogo-da-velha.md) | Jogo da Velha | epic | feature | implemented | 2026-09-29 | — | SPEC-0004 | — |
| [SPEC-0006](SPEC-0006-matchmaking-module.md) | Matchmaking Module | full | feature | implemented | 2026-09-29 | SPEC-0005 | SPEC-0004 | — |
| [SPEC-0007](SPEC-0007-gameplay-module.md) | Gameplay Module | full | feature | implemented | 2026-09-29 | SPEC-0005 | SPEC-0004 | — |
| [SPEC-0008](SPEC-0008-ui-interativa.md) | UI Interativa | full | feature | implemented | 2026-09-29 | SPEC-0005 | SPEC-0006, SPEC-0007 | — |
| [SPEC-0009](SPEC-0009-nomes-de-jogador.md) | Nomes de Jogador | full | feature | implemented | 2026-09-29 | — | SPEC-0008 | — |
| [SPEC-0010](SPEC-0010-historico-de-partidas.md) | Histórico de Partidas | full | feature | implemented | 2026-09-29 | — | SPEC-0009 | — |
| [SPEC-0011](SPEC-0011-navegacao-entre-jogo-e-historico.md) | Navegação entre Jogo e Histórico | lite | feature | implemented | 2026-09-29 | — | SPEC-0010 | — |

## ADRs

| ID | Título | Status | Garantido por (G3) |
|---|---|---|---|
| [ADR-0001](../adr/ADR-0001-orquestracao-e-desenvolvimento-local.md) | Orquestração e Desenvolvimento Local | accepted | — |
| [ADR-0002](../adr/ADR-0002-frontend-e-ui.md) | Frontend e UI | accepted | — |
| [ADR-0003](../adr/ADR-0003-banco-de-dados.md) | Banco de Dados | accepted | — |
| [ADR-0004](../adr/ADR-0004-arquitetura.md) | Arquitetura | accepted | — |
