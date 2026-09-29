---
id: ADR-0010
title: Motor de regras do xadrez próprio validado por perft
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
consulted: []
informed: []
supersedes:
superseded_by:
enforced_by: tests/TicTacToe.Tests/ChessPerftTests.cs
---

# ADR-0010 — Motor de regras do xadrez próprio validado por perft

## Contexto e Problema
O xadrez precisa de um motor de regras: geração de lances legais (xeque, roque, en passant, promoção), detecção de xeque-mate, afogamento, repetição, regra dos 50 lances e material insuficiente. O projeto é .NET 10 com módulos independentes (ADR-0004) e análise estática estrita (ADR-0005). Devemos usar uma biblioteca de regras ou escrever o motor?

## Direcionadores da Decisão
- Correção verificável: o motor precisa provar que gera exatamente os lances legais (contagens de perft conhecidas).
- Biblioteca mantida, com licença compatível e suporte a .NET 10; sem risco de abandono.
- Controle do modelo de domínio (SAN, histórico, repetição por posição, relógio) e futura extensão a Chess960.
- Nenhuma dependência nova sem necessidade (regra do projeto: mudança de biblioteca exige ADR).
- Escala pequena: partidas humanas e um robô fraco/médio; desempenho de bitboards não é requisito.

## Opções Consideradas
- **A. Motor próprio no módulo `TicTacToe.Modules.Chess`**, representação simples (tabuleiro 0x88 ou array de 64 casas), validado por perft contra contagens publicadas.
- **B. `ChessRealms.Engine`** (NuGet, MIT, .NET 10): bitboards, lances legais, FEN, xeque-mate, afogamento e empates básicos.
- **C. `Rudzoft.ChessLib`** (NuGet): geração completa de lances, Chess960 e perft segundo a descrição do pacote e do repositório.

## Resultado da Decisão
**Opção escolhida:** **A**, porque nenhuma biblioteca atende aos direcionadores hoje: a opção B foi publicada em 2026-09-24 (versão 1.0.0, 42 downloads) e, segundo o README e o NuGet, não traz SAN, Chess960 nem relógio, além de não ter histórico de manutenção; a opção C tem como última versão publicada no NuGet a 0.0.3 de 2022-10-15, para .NET 6 (a atividade recente do repositório não foi avaliada). As regras do xadrez são estáveis e bem documentadas, o volume de código é pequeno (estimativa do Architect: algumas centenas de linhas) e o perft dá uma prova objetiva de correção. O motor próprio também mantém o domínio sob o padrão do projeto (sem dependência externa, testável com xUnit).

### Consequências
- **Boa**, porque não há dependência externa nova, licença ou abandono a gerenciar, e o modelo (histórico, SAN, repetição) é desenhado para as telas.
- **Boa**, porque o perft prova a geração de lances e as regras de fim de jogo têm testes por cenário conhecido.
- **Ruim**, porque o custo inicial é maior que adotar uma biblioteca, e erros sutis (en passant com xeque descoberto, roque através de casa atacada) só aparecem com boa suíte de perft.
- **Ruim**, porque desempenho é menor que o de bitboards; aceito na escala do projeto e revisitável dentro da abstração do módulo.

### Confirmação (G3)
`tests/TicTacToe.Tests/ChessPerftTests.cs` valida as contagens de perft das posições de referência (posição inicial, Kiwipete e as posições 3 a 5 da chessprogramming wiki); `tests/TicTacToe.Tests/ChessModuleBoundaryTests.cs` (`Category=Architecture`) garante que `TicTacToe.Modules.Chess` não referencia Gameplay, Matchmaking nem Web.

## Prós e Contras das Opções
| Critério | A. Motor próprio | B. ChessRealms.Engine | C. Rudzoft.ChessLib |
|---|---|---|---|
| Maturidade e manutenção | Sob controle do projeto | Muito nova (1.0.0 em 2026-09-24, 42 downloads) | Última versão no NuGet em 2022 (0.0.3); repositório não avaliado |
| Suporte a .NET 10 | Nativo | Sim | Compatível por cálculo; alvo net6 |
| SAN, histórico, relógio | Desenhados para o projeto | Sem SAN nem relógio (README) | Não avaliado; o pacote descreve só dados e geração de lances |
| Chess960 | Extensível por desenho | Não | Sim |
| Prova de correção | Perft do próprio projeto | Dependeria de confiar na biblioteca | Perft próprio da biblioteca |
| Custo inicial | Maior | Menor | Menor |

## Mais Informações
- Rudzoft.ChessLib 0.0.3, publicada em 2022-10-15, alvo .NET 6, sem avaliação nem busca — https://www.nuget.org/packages/Rudzoft.ChessLib — verificado em 2026-09-29.
- ChessRealms.Engine 1.0.0, MIT, .NET 10, 42 downloads, sem SAN, UCI, Chess960 nem relógios — https://www.nuget.org/packages/ChessRealms.Engine e https://github.com/ChessRealms/Engine — verificado em 2026-09-29.
- Referência de perft: https://www.chessprogramming.org/Perft_Results. As contagens usadas nas specs (posição inicial, Kiwipete e posições 3 a 6) foram conferidas por revisão independente contra conhecimento prévio; o implementador as reconfere na fonte ao escrever os testes.
- Specs: SPEC-0049 (motor), SPEC-0050 (regras de partida).
