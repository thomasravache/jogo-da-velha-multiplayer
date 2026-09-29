---
id: ADR-0011
title: Robô de xadrez próprio atrás de uma abstração IChessBot
status: proposed
origin: decision
date: 2026-09-29
decision_makers: [thomas]
consulted: []
informed: []
supersedes:
superseded_by:
enforced_by: tests/TicTacToe.Tests/ChessBotTests.cs
---

# ADR-0011 — Robô de xadrez próprio atrás de uma abstração IChessBot

## Contexto e Problema
O usuário quer jogar xadrez contra um robô, com níveis de dificuldade. O jogo da velha já tem `AiPlayer` (Minimax e aleatório). No xadrez, a força do robô vem de um motor de busca. Devemos integrar um motor pronto (Stockfish, via protocolo UCI) ou escrever um motor simples e limitá-lo por profundidade e aleatoriedade?

## Direcionadores da Decisão
- Dificuldade graduável e previsível (fácil a médio agora; espaço para difícil depois).
- Sem processo externo ou binário nativo por plataforma no contêiner e no desenvolvimento local (Aspire, macOS/Linux).
- Licença compatível: Stockfish é GPL v3 e exige distribuir o código-fonte do binário que for distribuído.
- Testes determinísticos e sem I/O de processo.
- O robô é recurso secundário do épico; não deve arrastar a arquitetura.

## Opções Consideradas
- **A. Motor próprio** (negamax com poda alfa-beta, aprofundamento por profundidade máxima, avaliação por material e tabelas de posição), atrás de uma abstração `IChessBot`, com nível = profundidade + ruído controlado.
- **B. Stockfish via UCI** (processo filho; nível por `Skill Level` ou `UCI_LimitStrength`/`UCI_Elo`), atrás da mesma abstração.
- **C. Wrapper NuGet de UCI** (`Stockfish.NET`, MIT, última atualização em 2020, ou `pax.uciChessEngine`, https://www.nuget.org/packages/pax.uciChessEngine) sobre um binário do Stockfish.

## Resultado da Decisão
**Opção escolhida:** **A** para a primeira entrega, sempre atrás de `IChessBot`. Motivos: força suficiente para treino (fácil e médio), zero processo externo, zero problema de licença, testes determinísticos com semente fixa, e o mesmo padrão do `AiPlayer` já existente. A abstração `IChessBot` deixa o Stockfish (opção B) como implementação futura, em uma spec e ADR próprias, se o produto pedir um nível "difícil" de força real.

### Consequências
- **Boa**, porque o robô roda em processo, sem binário nativo, e os testes independem de relógio e de I/O.
- **Boa**, porque o custo de trocar ou acrescentar o Stockfish depois é baixo (nova implementação de `IChessBot`).
- **Ruim**, porque a força máxima do motor próprio é limitada (a força real é desconhecida e será medida por partidas do robô contra ele mesmo e contra um jogador aleatório; não há meta de nível de mestre); não há nível "impossível" como no jogo da velha.
- **Ruim**, porque a força relativa dos níveis precisa ser medida em partidas do robô contra ele mesmo e contra um jogador aleatório, dentro dos testes da spec.

### Confirmação (G3)
`tests/TicTacToe.Tests/ChessBotTests.cs`: todo robô só produz lances legais, nunca joga fora da vez, respeita o limite de tempo e o nível mais forte vence o mais fraco em placar agregado com sementes fixas.

## Prós e Contras das Opções
| Critério | A. Motor próprio | B. Stockfish (UCI) | C. Wrapper NuGet + Stockfish |
|---|---|---|---|
| Força máxima | Média | Muito alta | Muito alta |
| Dependências de execução | Nenhuma | Binário por plataforma e processo filho | Binário + pacote sem manutenção (2020) |
| Licença | Do projeto | GPL v3 (distribuir fonte do binário) | GPL v3 + MIT do wrapper |
| Testes determinísticos | Sim | Difícil (processo, tempo) | Difícil |
| Custo de implementação | Médio | Médio (protocolo, ciclo de vida do processo) | Médio |

## Mais Informações
- Stockfish: `Skill Level` e `UCI_Elo` enfraquecem o jogo escolhendo lances sub-ótimos; UCI_Elo tem precedência; licença GPL v3 obriga a acompanhar o código-fonte — https://official-stockfish.github.io/docs/stockfish-wiki/UCI-Protocol-and-Stockfish-Commands.html e https://official-stockfish.github.io/docs/stockfish-wiki/Stockfish-FAQ.html — resultados de busca consultados em 2026-09-29; a faixa exata de `UCI_Elo` e o texto de licença **não foram lidos na íntegra**.
- `Stockfish.NET`, MIT, última atualização em 2020-11-15 — https://www.nuget.org/packages/Stockfish.NET — verificado em 2026-09-29.
- Specs: SPEC-0054 (robô) e SPEC-0058 (treino solo).
