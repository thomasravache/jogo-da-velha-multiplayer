---
id: ADR-0006
title: Componentização Blazor, CSS Isolation e Testes com bUnit
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
consulted: []
informed: []
supersedes:
superseded_by:
enforced_by: Testes de componentes bUnit
---

# ADR-0006 — Componentização Blazor, CSS Isolation e Testes com bUnit

## Contexto e Problema
A página `Home.razor` acumulou mais de 450 linhas, concentrando estilização CSS global embutida, marcação HTML de 4 modos de jogo diferentes e mais de 200 linhas de manipulação de timers e lógica C#. Isso dificulta testes unitários de interface e viola o princípio de responsabilidade única. Como decompor a interface sem quebrar o comportamento reativo do jogo?

## Direcionadores da Decisão
- Separação clara entre apresentação (UI), estilos e regras de negócio.
- Capacidade de testar o ciclo de vida e renderização de componentes sem necessidade de browser headless pesado (ex: Selenium).
- Manutenibilidade e facilidade de evolução de novas telas e recursos visuais.

## Opções Consideradas
- **Opção A:** CSS Isolation nativo (`Home.razor.css`) + subcomponentes Blazor (`Lobby.razor`, `Scoreboard.razor`, `GameBoard.razor`) + testes com `bUnit`
- **Opção B:** Frameworks externos de UI (ex: MudBlazor, Radzen)
- **Opção C:** Manter componente monolítico único em `Home.razor`

## Resultado da Decisão
**Opção escolhida:** Opção A (Subcomponentes Blazor + CSS Isolation + bUnit), porque mantém o design customizado atual do jogo, não adiciona dependências pesadas de terceiros e viabiliza testes rápidos de UI em memória no pipeline de testes unitários.

### Consequências
- **Boa**, porque cada componente passa a ter escopo pequeno, focado e facilmente testável.
- **Boa**, porque estilos não vazam para outras telas graças ao CSS Isolation do Blazor.
- **Boa**, porque `bUnit` permite testar cliques e renderização em milissegundos.
- **Ruim**, porque exige comunicação via parâmetros (`[Parameter]`, `EventCallback`) entre o componente pai e os filhos.

### Confirmação (G3)
Validado através da suíte de testes `bUnit` que verifica a renderização e o binding de eventos dos componentes isolados.

## Prós e Contras das Opções
| Critério (peso 1-5) | Opção A (Componentes + bUnit) | Opção B (Frameworks de terceiros) | Opção C (Monolítico) |
|---|---|---|---|
| Controle e design sob medida (5) | 5 (Total controle) | 2 (Opiniado) | 5 |
| Velocidade de testes de UI (5) | 5 (bUnit em memória) | 3 | 1 (Sem testes de UI) |
| Simplicidade de manutenção (4) | 5 (Modular) | 3 | 2 (Arquivo monolítico) |
| **Total ponderado** | **70** | **38** | **38** |

## Mais Informações
- Microsoft Learn: [ASP.NET Core Blazor CSS isolation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation)
- bUnit documentation: [bUnit for Blazor](https://bunit.dev/)
