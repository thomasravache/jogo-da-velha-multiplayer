# Índice de Specs

> Gerado por `spec_graph.py index` em 2026-09-29 — não edite manualmente.

## Saúde

- Validação (G0): **0 erro(s), 0 aviso(s)**
- Specs: proposed 15, implemented 39
- Impedimentos: **0 aberto(s)**, 0 resolvido(s)

## Plano de Execução

**Em andamento:** —  
**Paradas por impedimento:** —  
**Próximo lote:** —

| Onda | Spec | Título | Tier/Tam. | Status | Prontidão | Observação |
|---|---|---|---|---|---|---|
| 1 | SPEC-0047 | Generalização multi-jogo (GameType, filtros e fila por chave) | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 1 | SPEC-0049 | Motor de xadrez: tabuleiro, lances legais e perft | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 2 | SPEC-0050 | Regras de partida do xadrez: lances, SAN e fim de jogo | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 2 | SPEC-0051 | Relógio de xadrez com incremento | full/S | proposed | ⏳ aguardando aprovação (H1) |  |
| 3 | SPEC-0052 | Sessão de xadrez compartilhada | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 3 | SPEC-0054 | Robô de xadrez com níveis de dificuldade | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 3 | SPEC-0055 | Peças SVG e tabuleiro interativo | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 4 | SPEC-0057 | Arena de xadrez: relógios, lances e fim de partida | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 4 | SPEC-0061 | Ciclo de vida da sessão de xadrez: abandono, revanche e presença | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 5 | SPEC-0053 | Pareamento e persistência do xadrez | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 6 | SPEC-0056 | Lobby de xadrez | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 7 | SPEC-0058 | Treino solo de xadrez contra o robô | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 8 | SPEC-0060 | Abandono, revanche e desconexão no xadrez | full/M | proposed | ⏳ aguardando aprovação (H1) |  |
| 9 | SPEC-0048 | Seleção de jogos e navegação por jogo | full/S | proposed | ⏳ aguardando aprovação (H1) |  |
| 10 | SPEC-0059 | Histórico e ranking do xadrez | full/M | proposed | ⏳ aguardando aprovação (H1) |  |

## Épicos

| Épico | Título | Status | Progresso |
|---|---|---|---|
| SPEC-0001 | Fundação do Projeto | implemented | 3/3 implementadas |
| SPEC-0005 | Jogo da Velha | implemented | 3/3 implementadas |
| SPEC-0013 | Evolução do Jogo da Velha | implemented | 6/6 implementadas |
| SPEC-0021 | Engenharia de Qualidade e Refatoração Arquitetural | implemented | 4/4 implementadas |
| SPEC-0028 | Redesign Cyber Arena — migração da UI para Tailwind (Fase 1 visual) | implemented | 7/7 implementadas |
| SPEC-0035 | Evolução funcional a partir do Stitch (Fase 2) | implemented | 9/9 implementadas |
| SPEC-0046 | Xadrez multiplayer | proposed | 0/15 implementadas |

## Grafo de Dependências

Seta contínua: depende da implementação. Seta tracejada: consome contrato.

```mermaid
flowchart LR
  subgraph E0046["SPEC-0046 · Xadrez multiplayer"]
    S0047["SPEC-0047<br/>Generalização multi-jogo (GameType, fil…"]:::proposed
    S0048["SPEC-0048<br/>Seleção de jogos e navegação por jogo"]:::proposed
    S0049["SPEC-0049<br/>Motor de xadrez: tabuleiro, lances lega…"]:::proposed
    S0050["SPEC-0050<br/>Regras de partida do xadrez: lances, SA…"]:::proposed
    S0051["SPEC-0051<br/>Relógio de xadrez com incremento"]:::proposed
    S0052["SPEC-0052<br/>Sessão de xadrez compartilhada"]:::proposed
    S0053["SPEC-0053<br/>Pareamento e persistência do xadrez"]:::proposed
    S0054["SPEC-0054<br/>Robô de xadrez com níveis de dificuldade"]:::proposed
    S0055["SPEC-0055<br/>Peças SVG e tabuleiro interativo"]:::proposed
    S0056["SPEC-0056<br/>Lobby de xadrez"]:::proposed
    S0057["SPEC-0057<br/>Arena de xadrez: relógios, lances e fim…"]:::proposed
    S0058["SPEC-0058<br/>Treino solo de xadrez contra o robô"]:::proposed
    S0059["SPEC-0059<br/>Histórico e ranking do xadrez"]:::proposed
    S0060["SPEC-0060<br/>Abandono, revanche e desconexão no xadr…"]:::proposed
    S0061["SPEC-0061<br/>Ciclo de vida da sessão de xadrez: aban…"]:::proposed
  end
  S0047 --> S0048
  S0058 --> S0048
  S0060 --> S0048
  S0049 --> S0050
  S0049 --> S0051
  S0050 --> S0052
  S0051 --> S0052
  S0047 --> S0053
  S0052 --> S0053
  S0055 --> S0053
  S0061 --> S0053
  S0050 --> S0054
  S0050 --> S0055
  S0053 --> S0056
  S0057 --> S0056
  S0052 --> S0057
  S0055 --> S0057
  S0054 --> S0058
  S0056 --> S0058
  S0057 --> S0058
  S0048 --> S0059
  S0053 --> S0059
  S0056 --> S0060
  S0057 --> S0060
  S0058 --> S0060
  S0061 --> S0060
  S0052 --> S0061
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
| [SPEC-0021](SPEC-0021-engenharia-de-qualidade-e-refatoracao-arquitetural.md) | Engenharia de Qualidade e Refatoração Arquitetural | epic | feature | implemented | 2026-09-29 | — | — | — |
| [SPEC-0022](SPEC-0022-padronizacao-de-codigo-com-editorconfig-e-directory-build-pr.md) | Padronização de Código com EditorConfig e Directory.Build.props | full | foundation | implemented | 2026-09-29 | SPEC-0021 | — | — |
| [SPEC-0023](SPEC-0023-infraestrutura-de-cobertura-de-codigo-e-testes-com-bunit.md) | Infraestrutura de Cobertura de Código e Testes com bUnit | full | foundation | implemented | 2026-09-29 | SPEC-0021 | SPEC-0022 | — |
| [SPEC-0024](SPEC-0024-decomposicao-do-componente-home-e-isolamento-de-css.md) | Decomposição do Componente Home e Isolamento de CSS | full | refactor | implemented | 2026-09-29 | SPEC-0021 | SPEC-0023 | — |
| [SPEC-0025](SPEC-0025-eventos-em-tempo-real-sem-polling-e-ef-core-migrations.md) | Eventos em Tempo Real sem Polling e EF Core Migrations | full | refactor | implemented | 2026-09-29 | SPEC-0021 | SPEC-0024 | — |
| [SPEC-0026](SPEC-0026-redesign-de-ui-e-tema-escuro-com-mudblazor.md) | Redesign de UI e Tema Escuro Imersivo com MudBlazor | full | migration | implemented | 2026-09-29 | — | — | — |
| [SPEC-0027](SPEC-0027-timer-de-turno-e-timeout-por-w-o.md) | Timer de turno e timeout por W.O. | full | feature | implemented | 2026-09-29 | — | — | — |
| [SPEC-0028](SPEC-0028-redesign-cyber-arena-migracao-da-ui-para-tailwind-fase-1-vis.md) | Redesign Cyber Arena — migração da UI para Tailwind (Fase 1 visual) | epic | migration | implemented | 2026-09-29 | — | — | — |
| [SPEC-0029](SPEC-0029-pipeline-tailwind-tokens-e-fontes-cyber-arena.md) | Pipeline Tailwind, tokens e fontes Cyber Arena | full | migration | implemented | 2026-09-29 | SPEC-0028 | — | — |
| [SPEC-0030](SPEC-0030-lobby-cyber-arena.md) | Lobby Cyber Arena | full | feature | implemented | 2026-09-29 | SPEC-0028 | SPEC-0043 | — |
| [SPEC-0031](SPEC-0031-arena-da-partida-cyber-arena.md) | Arena da partida Cyber Arena | full | feature | implemented | 2026-09-29 | SPEC-0028 | SPEC-0043 | — |
| [SPEC-0032](SPEC-0032-historico-de-partidas-cyber-arena.md) | Histórico de partidas Cyber Arena | full | feature | implemented | 2026-09-29 | SPEC-0028 | SPEC-0043 | — |
| [SPEC-0033](SPEC-0033-ranking-cyber-arena.md) | Ranking Cyber Arena | full | feature | implemented | 2026-09-29 | SPEC-0028 | SPEC-0043 | — |
| [SPEC-0034](SPEC-0034-remocao-do-mudblazor-e-do-bootstrap.md) | Remoção do MudBlazor e do Bootstrap | full | migration | implemented | 2026-09-29 | SPEC-0028 | SPEC-0030, SPEC-0031, SPEC-0032, SPEC-0033 | — |
| [SPEC-0035](SPEC-0035-evolucao-funcional-a-partir-do-stitch-fase-2.md) | Evolução funcional a partir do Stitch (Fase 2) | epic | feature | implemented | 2026-09-29 | — | — | — |
| [SPEC-0036](SPEC-0036-persistencia-enriquecida-de-partidas.md) | Persistência enriquecida de partidas | full | feature | implemented | 2026-09-29 | SPEC-0035 | SPEC-0031, SPEC-0045 | — |
| [SPEC-0037](SPEC-0037-identidade-anonima-do-jogador.md) | Identidade anônima do jogador | full | feature | implemented | 2026-09-29 | SPEC-0035 | SPEC-0036 | — |
| [SPEC-0038](SPEC-0038-historico-avancado.md) | Histórico avançado | full | feature | implemented | 2026-09-29 | SPEC-0035 | SPEC-0032, SPEC-0036, SPEC-0037 | — |
| [SPEC-0039](SPEC-0039-ranking-avancado.md) | Ranking avançado | full | feature | implemented | 2026-09-29 | SPEC-0035 | SPEC-0033, SPEC-0037, SPEC-0038 | — |
| [SPEC-0040](SPEC-0040-serie-melhor-de-5-md5.md) | Série melhor de 5 (MD5) | full | feature | implemented | 2026-09-29 | SPEC-0035 | SPEC-0031, SPEC-0036 | — |
| [SPEC-0041](SPEC-0041-abandonar-partida-e-pedir-revanche.md) | Abandonar partida e pedir revanche | full | feature | implemented | 2026-09-29 | SPEC-0035 | SPEC-0044, SPEC-0045 | — |
| [SPEC-0042](SPEC-0042-w-o-por-desconexao-do-oponente.md) | W.O. por desconexão do oponente | full | feature | implemented | 2026-09-29 | SPEC-0035 | SPEC-0041 | — |
| [SPEC-0043](SPEC-0043-shell-e-primitivos-de-ui-cyber-arena.md) | Shell e primitivos de UI Cyber Arena | full | feature | implemented | 2026-09-29 | SPEC-0028 | SPEC-0029 | — |
| [SPEC-0044](SPEC-0044-serie-melhor-de-5-interface-no-lobby-e-na-arena.md) | Série melhor de 5: interface no lobby e na arena | full | feature | implemented | 2026-09-29 | SPEC-0035 | SPEC-0030, SPEC-0040 | — |
| [SPEC-0045](SPEC-0045-gravacao-unica-do-resultado-da-partida.md) | Gravação única do resultado da partida | full | fix | implemented | 2026-09-29 | SPEC-0035 | — | — |
| [SPEC-0046](SPEC-0046-xadrez-multiplayer.md) | Xadrez multiplayer | epic | feature | proposed | 2026-09-29 | — | — | — |
| [SPEC-0047](SPEC-0047-generalizacao-multi-jogo-gametype-filtros-e-fila-por-chave.md) | Generalização multi-jogo (GameType, filtros e fila por chave) | full | migration | proposed | 2026-09-29 | SPEC-0046 | — | — |
| [SPEC-0048](SPEC-0048-selecao-de-jogos-e-navegacao-por-jogo.md) | Seleção de jogos e navegação por jogo | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0047, SPEC-0058, SPEC-0060 | — |
| [SPEC-0049](SPEC-0049-motor-de-xadrez-tabuleiro-lances-legais-e-perft.md) | Motor de xadrez: tabuleiro, lances legais e perft | full | foundation | proposed | 2026-09-29 | SPEC-0046 | — | — |
| [SPEC-0050](SPEC-0050-regras-de-partida-do-xadrez-lances-san-e-fim-de-jogo.md) | Regras de partida do xadrez: lances, SAN e fim de jogo | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0049 | — |
| [SPEC-0051](SPEC-0051-relogio-de-xadrez-com-incremento.md) | Relógio de xadrez com incremento | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0049 | — |
| [SPEC-0052](SPEC-0052-sessao-de-xadrez-compartilhada.md) | Sessão de xadrez compartilhada | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0050, SPEC-0051 | — |
| [SPEC-0053](SPEC-0053-pareamento-e-persistencia-do-xadrez.md) | Pareamento e persistência do xadrez | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0047, SPEC-0052, SPEC-0055, SPEC-0061 | — |
| [SPEC-0054](SPEC-0054-robo-de-xadrez-com-niveis-de-dificuldade.md) | Robô de xadrez com níveis de dificuldade | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0050 | — |
| [SPEC-0055](SPEC-0055-pecas-svg-e-tabuleiro-interativo.md) | Peças SVG e tabuleiro interativo | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0050 | — |
| [SPEC-0056](SPEC-0056-lobby-de-xadrez.md) | Lobby de xadrez | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0053, SPEC-0057 | — |
| [SPEC-0057](SPEC-0057-arena-de-xadrez-relogios-lances-e-fim-de-partida.md) | Arena de xadrez: relógios, lances e fim de partida | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0052, SPEC-0055 | — |
| [SPEC-0058](SPEC-0058-treino-solo-de-xadrez-contra-o-robo.md) | Treino solo de xadrez contra o robô | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0054, SPEC-0056, SPEC-0057 | — |
| [SPEC-0059](SPEC-0059-historico-e-ranking-do-xadrez.md) | Histórico e ranking do xadrez | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0048, SPEC-0053 | — |
| [SPEC-0060](SPEC-0060-abandono-revanche-e-desconexao-no-xadrez.md) | Abandono, revanche e desconexão no xadrez | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0056, SPEC-0057, SPEC-0058, SPEC-0061 | — |
| [SPEC-0061](SPEC-0061-ciclo-de-vida-da-sessao-de-xadrez-abandono-revanche-e-presen.md) | Ciclo de vida da sessão de xadrez: abandono, revanche e presença | full | feature | proposed | 2026-09-29 | SPEC-0046 | SPEC-0052 | — |

## ADRs

| ID | Título | Status | Garantido por (G3) |
|---|---|---|---|
| [ADR-0001](../adr/ADR-0001-orquestracao-e-desenvolvimento-local.md) | Orquestração e Desenvolvimento Local | accepted | N/A — histórico |
| [ADR-0002](../adr/ADR-0002-frontend-e-ui.md) | Frontend e UI | accepted | N/A — histórico |
| [ADR-0003](../adr/ADR-0003-banco-de-dados.md) | Banco de Dados | accepted | N/A — histórico |
| [ADR-0004](../adr/ADR-0004-arquitetura.md) | Arquitetura | accepted | N/A — histórico |
| [ADR-0005](../adr/ADR-0005-padronizacao-de-analise-estatica-e-compilacao-estrita.md) | Padronização de Análise Estática e Compilação Estrita | accepted | Directory.Build.props e dotnet format |
| [ADR-0006](../adr/ADR-0006-componentizacao-blazor-css-isolation-e-testes-com-bunit.md) | Componentização Blazor, CSS Isolation e Testes com bUnit | accepted | Testes de componentes bUnit |
| [ADR-0007](../adr/ADR-0007-adocao-do-mudblazor-como-design-system-e-componentes.md) | Adoção do MudBlazor como Design System e Componentes | superseded | tests/TicTacToe.Tests/MudBlazorIntegrationTests.cs |
| [ADR-0008](../adr/ADR-0008-tailwind-css-standalone-como-camada-de-estilo-e-design-syste.md) | Tailwind CSS standalone como camada de estilo e design system próprio | accepted | tests/TicTacToe.Tests/TailwindDesignSystemTests.cs |
| [ADR-0009](../adr/ADR-0009-identidade-anonima-persistente-do-jogador.md) | Identidade anônima persistente do jogador | accepted | tests/TicTacToe.Tests/PlayerIdentityTests.cs |
| [ADR-0010](../adr/ADR-0010-motor-de-regras-do-xadrez-proprio-validado-por-perft.md) | Motor de regras do xadrez próprio validado por perft | proposed | tests/TicTacToe.Tests/ChessPerftTests.cs |
| [ADR-0011](../adr/ADR-0011-robo-de-xadrez-proprio-atras-de-uma-abstracao-ichessbot.md) | Robô de xadrez próprio atrás de uma abstração IChessBot | proposed | tests/TicTacToe.Tests/ChessBotTests.cs |
| [ADR-0012](../adr/ADR-0012-generalizacao-multi-jogo-do-modelo-de-partidas.md) | Generalização multi-jogo do modelo de partidas | proposed | tests/TicTacToe.Tests/MultiGamePersistenceTests.cs |
