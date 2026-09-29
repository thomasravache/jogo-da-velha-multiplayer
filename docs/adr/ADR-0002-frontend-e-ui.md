---
id: ADR-0002
title: Frontend e UI
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
---

# ADR-0002 — Frontend e UI

## Contexto e Problema
O Jogo da Velha requer uma interface de usuário iterativa e em tempo real, onde as ações de um jogador são refletidas quase instantaneamente na tela do outro jogador. Precisamos escolher a tecnologia de frontend.

## Direcionadores da Decisão
- Sincronização em tempo real de estado do tabuleiro.
- Minimizar a complexidade de gerenciar APIs REST para interações simples.
- Rápido desenvolvimento (stack C# de ponta a ponta).

## Opções Consideradas
- Blazor Server (Web App Interactive Server)
- Blazor WebAssembly + SignalR Hub
- React / Vue com SignalR Hub
- Vanilla JS com SignalR Hub

## Resultado da Decisão
**Opção escolhida:** Blazor Server (Web App Interactive Server), porque aproveita a conexão SignalR pré-existente (Circuits) para atualizar o DOM, o que significa que o estado do jogo pode ser sincronizado facilmente através de eventos em memória e injeção de dependência (Singletons) sem a necessidade de escrever explicitamente endpoints de API ou Hubs SignalR separados.

### Consequências
- **Boa**, porque permite manter todo o código em C#, sem a necessidade de contexto JavaScript.
- **Boa**, porque a reatividade é gerenciada automaticamente através do SignalR connection do Blazor.
- **Ruim**, porque aumenta a latência percebida em redes ruins, já que cada interação com a UI exige uma ida ao servidor, mas para um jogo da velha simples isso é irrelevante.

### Confirmação (G3)
Revisão no G4. O projeto de interface será um `Blazor Web App`.

## Mais Informações
Verificado com ASP.NET Core 8 Blazor docs (Set 2026).
