---
id: ADR-0012
title: Generalização multi-jogo do modelo de partidas
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
consulted: []
informed: []
supersedes:
superseded_by:
enforced_by: tests/TicTacToe.Tests/MultiGamePersistenceTests.cs
---

# ADR-0012 — Generalização multi-jogo do modelo de partidas

## Contexto e Problema
O app guarda todas as partidas em `MatchResult` e assume um único jogo: histórico, ranking e matchmaking não distinguem o jogo. Com o xadrez, os dois jogos compartilham identidade, histórico e ranking mas não podem se misturar (uma vitória no xadrez não conta no ranking do jogo da velha). Como generalizar o modelo sem quebrar os dados nem os módulos existentes?

## Direcionadores da Decisão
- Migration aditiva e reversível; partidas antigas continuam válidas como jogo da velha.
- Um só lugar para partidas (histórico e ranking reaproveitam consultas e componentes).
- `Gameplay`, `Matchmaking` e `Chess` continuam independentes (ADR-0004): nenhum referencia o outro.
- Nenhuma mudança de comportamento visível no jogo da velha.

## Opções Consideradas
- **A. Coluna `GameType` em `MatchResult`** (inteiro, padrão 0 = jogo da velha), colunas específicas do xadrez anuláveis na mesma tabela, consultas filtradas por jogo e fila de matchmaking por chave textual.
- **B. Tabela separada para partidas de xadrez** com serviços paralelos de histórico e ranking.
- **C. Esquema genérico (JSON por partida)** com `GameType` e um documento de detalhes.

## Resultado da Decisão
**Opção escolhida:** **A**, porque é a menor mudança que atende aos direcionadores: uma coluna com valor padrão (aditiva, sem reescrever dados), consultas existentes ganham um parâmetro opcional de jogo (padrão jogo da velha, comportamento atual preservado), e o histórico e o ranking continuam sendo o mesmo código. A fila do matchmaking passa a aceitar uma chave textual (`"velha:1"`, `"xadrez:blitz5+0"`; a preferência de cor do xadrez é metadado separado, não faz parte da chave), mantendo o módulo sem conhecer tipos de jogo.

### Consequências
- **Boa**, porque partidas antigas viram jogo da velha sem migração de dados e o jogo da velha não muda de comportamento.
- **Boa**, porque histórico e ranking do xadrez reaproveitam a lógica e os componentes.
- **Ruim**, porque a tabela ganha colunas anuláveis específicas do xadrez (aceito; poucas e sem índice novo além de `GameType`).
- **Ruim**, porque a chave textual da fila é uma convenção, não um tipo; mitigada por testes de formato.

### Confirmação (G3)
`tests/TicTacToe.Tests/MultiGamePersistenceTests.cs` prova que a migration é só `AddColumn`/`CreateIndex` e que partidas sem `GameType` continuam sendo lidas como jogo da velha; `ChessModuleBoundaryTests` (SPEC-0049) mantém os módulos independentes.

## Prós e Contras das Opções
| Critério | A. Coluna GameType | B. Tabela separada | C. JSON genérico |
|---|---|---|---|
| Migração de dados | Nenhuma (valor padrão) | Nenhuma, mas duplica esquema | Reescrita ou coluna extra |
| Reuso de histórico e ranking | Total | Baixo (serviços paralelos) | Alto, com consulta em JSON |
| Consultas e índices | Simples | Simples | Difícil de filtrar e indexar |
| Risco para o jogo da velha | Baixo | Nenhum | Médio |

## Mais Informações
- Specs: SPEC-0047 (generalização), SPEC-0053 (persistência do xadrez), SPEC-0059 (histórico e ranking por jogo).
- Reutiliza o padrão de migrations aditivas das SPEC-0036, SPEC-0037 e SPEC-0040.
