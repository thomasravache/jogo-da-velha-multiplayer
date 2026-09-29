# Índice de Specs

> Gerado por `spec_graph.py index` em 2026-09-29 — não edite manualmente.

## Saúde

- Validação (G0): **76 erro(s), 14 aviso(s)** — rode `spec_graph.py validate`
- Specs: approved 4, implemented 17
- Impedimentos: **0 aberto(s)**, 0 resolvido(s)

## Plano de Execução

**Em andamento:** —  
**Paradas por impedimento:** —  
**Próximo lote:** SPEC-0022

| Onda | Spec | Título | Tier/Tam. | Status | Prontidão | Observação |
|---|---|---|---|---|---|---|
| 1 | SPEC-0022 | Padronização de Código com EditorConfig e Directory.Build.props | full/S | approved | ✅ pronta |  |
| 2 | SPEC-0023 | Infraestrutura de Cobertura de Código e Testes com bUnit | full/S | approved | ⛔ aguarda implementação de SPEC-0022 |  |
| 3 | SPEC-0024 | Decomposição do Componente Home e Isolamento de CSS | full/M | approved | ⛔ aguarda implementação de SPEC-0023 |  |
| 4 | SPEC-0025 | Eventos em Tempo Real sem Polling e EF Core Migrations | full/M | approved | ⛔ aguarda implementação de SPEC-0024 |  |

## Épicos

| Épico | Título | Status | Progresso |
|---|---|---|---|
| SPEC-0001 | Fundação do Projeto | implemented | 3/3 implementadas |
| SPEC-0005 | Jogo da Velha | implemented | 3/3 implementadas |
| SPEC-0013 | Evolução do Jogo da Velha | implemented | 6/6 implementadas |
| SPEC-0021 | Engenharia de Qualidade e Refatoração Arquitetural | approved | 0/4 implementadas |

## Grafo de Dependências

Seta contínua: depende da implementação. Seta tracejada: consome contrato.

```mermaid
flowchart LR
  subgraph E0021["SPEC-0021 · Engenharia de Qualidade e Refatoração Arquitetural"]
    S0022["SPEC-0022<br/>Padronização de Código com EditorConfig…"]:::approved
    S0023["SPEC-0023<br/>Infraestrutura de Cobertura de Código e…"]:::approved
    S0024["SPEC-0024<br/>Decomposição do Componente Home e Isola…"]:::approved
    S0025["SPEC-0025<br/>Eventos em Tempo Real sem Polling e EF …"]:::approved
  end
  S0022 --> S0023
  S0023 --> S0024
  S0024 --> S0025
  classDef proposed fill:#fef3c7,stroke:#d97706,color:#111
  classDef approved fill:#dbeafe,stroke:#2563eb,color:#111
  classDef inprogress fill:#ede9fe,stroke:#7c3aed,color:#111
  classDef implemented fill:#dcfce7,stroke:#16a34a,color:#111
  classDef deprecated fill:#f3f4f6,stroke:#9ca3af,color:#6b7280
```

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
| [SPEC-0012](SPEC-0012-jogar-novamente.md) | Jogar Novamente | lite | feature | implemented | 2026-09-29 | — | SPEC-0010, SPEC-0011 | — |
| [SPEC-0013](SPEC-0013-evolucao-do-jogo-da-velha.md) | Evolução do Jogo da Velha | epic | feature | implemented | 2026-09-29 | — | SPEC-0005 | — |
| [SPEC-0014](SPEC-0014-placar-da-sessao-e-efeitos-de-vitoria.md) | Placar da Sessão e Efeitos de Vitória | full | feature | implemented | 2026-09-29 | SPEC-0013 | SPEC-0012 | — |
| [SPEC-0015](SPEC-0015-modo-solo-vs-ia-minimax.md) | Modo Solo vs IA Minimax | full | feature | implemented | 2026-09-29 | SPEC-0013 | SPEC-0014 | — |
| [SPEC-0016](SPEC-0016-salas-privadas-com-codigo.md) | Salas Privadas com Código | full | feature | implemented | 2026-09-29 | SPEC-0013 | SPEC-0014 | — |
| [SPEC-0017](SPEC-0017-leaderboard-e-estatisticas.md) | Leaderboard e Estatísticas | full | feature | implemented | 2026-09-29 | SPEC-0013 | SPEC-0010 | — |
| [SPEC-0018](SPEC-0018-sincronizacao-reativa-por-eventos.md) | Sincronização Reativa por Eventos | full | feature | implemented | 2026-09-29 | SPEC-0013 | SPEC-0016 | — |
| [SPEC-0019](SPEC-0019-selecao-de-dificuldade-do-robo-na-ui.md) | Seleção de Dificuldade do Robô na UI | full | feature | implemented | 2026-09-29 | SPEC-0013 | SPEC-0015 | — |
| [SPEC-0020](SPEC-0020-correcao-de-inversao-de-nomes-em-salas-privadas-e-partidas.md) | Correção de Inversão de Nomes em Salas Privadas e Partidas | lite | fix | implemented | 2026-09-29 | — | — | — |
| [SPEC-0021](SPEC-0021-engenharia-de-qualidade-e-refatoracao-arquitetural.md) | Engenharia de Qualidade e Refatoração Arquitetural | epic | feature | approved | 2026-09-29 | — | — | — |
| [SPEC-0022](SPEC-0022-padronizacao-de-codigo-com-editorconfig-e-directory-build-pr.md) | Padronização de Código com EditorConfig e Directory.Build.props | full | foundation | approved | 2026-09-29 | SPEC-0021 | — | — |
| [SPEC-0023](SPEC-0023-infraestrutura-de-cobertura-de-codigo-e-testes-com-bunit.md) | Infraestrutura de Cobertura de Código e Testes com bUnit | full | foundation | approved | 2026-09-29 | SPEC-0021 | SPEC-0022 | — |
| [SPEC-0024](SPEC-0024-decomposicao-do-componente-home-e-isolamento-de-css.md) | Decomposição do Componente Home e Isolamento de CSS | full | refactor | approved | 2026-09-29 | SPEC-0021 | SPEC-0023 | — |
| [SPEC-0025](SPEC-0025-eventos-em-tempo-real-sem-polling-e-ef-core-migrations.md) | Eventos em Tempo Real sem Polling e EF Core Migrations | full | refactor | approved | 2026-09-29 | SPEC-0021 | SPEC-0024 | — |

## ADRs

| ID | Título | Status | Garantido por (G3) |
|---|---|---|---|
| [ADR-0001](../adr/ADR-0001-orquestracao-e-desenvolvimento-local.md) | Orquestração e Desenvolvimento Local | accepted | — |
| [ADR-0002](../adr/ADR-0002-frontend-e-ui.md) | Frontend e UI | accepted | — |
| [ADR-0003](../adr/ADR-0003-banco-de-dados.md) | Banco de Dados | accepted | — |
| [ADR-0004](../adr/ADR-0004-arquitetura.md) | Arquitetura | accepted | — |
| [ADR-0005](../adr/ADR-0005-padronizacao-de-analise-estatica-e-compilacao-estrita.md) | Padronização de Análise Estática e Compilação Estrita | accepted | Directory.Build.props e dotnet format |
| [ADR-0006](../adr/ADR-0006-componentizacao-blazor-css-isolation-e-testes-com-bunit.md) | Componentização Blazor, CSS Isolation e Testes com bUnit | accepted | Testes de componentes bUnit |
