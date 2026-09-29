---
id: ADR-0004
title: Arquitetura
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
---

# ADR-0004 — Arquitetura Modular Monolith

## Contexto e Problema
A estruturação da solução impacta a facilidade de desenvolvimento, os testes e as futuras expansões. Precisamos escolher o padrão arquitetural que mantenha tudo "sem muito mimimi", mas evite um sistema espaguete.

## Direcionadores da Decisão
- Separação clara de responsabilidades (Clean Architecture).
- Simplicidade de deploy (um único binário/servidor a manter).
- Prevenir chamadas e acoplamentos diretos entre domínios distintos.

## Opções Consideradas
- Modular Monolith (Estilo Milan Jovanovic)
- Monolito Tradicional com N-Layers
- Microserviços

## Resultado da Decisão
**Opção escolhida:** Modular Monolith, porque isola o código de `Gameplay` e `Matchmaking` em projetos/namespaces separados, mantendo os benefícios de limites rígidos e testes isolados, ao passo que garante a simplicidade de deploy de um único projeto Web e o uso de chamadas de métodos (Interfaces) ou Eventos em Memória em vez de requisições HTTP entre serviços.

### Consequências
- **Boa**, porque a comunicação entre os módulos (ex: quando uma partida começa) é em-memória (rápida, atômica e segura).
- **Boa**, porque permite ter diferentes DbContexts para diferentes Módulos.
- **Ruim**, exige disciplina para não violar os limites dos módulos instanciando serviços de outro módulo indiretamente (garantido por testes de arquitetura NetArchTest).

### Confirmação (G3)
`ArchTests` com `NetArchTest.Rules` validarão que o projeto de `Matchmaking` não referencia o projeto de `Gameplay` diretamente, exceto por meio de Interfaces ou APIs públicas de módulo aprovadas.

## Mais Informações
Baseado nos ensinamentos sobre Modular Monoliths em .NET.
