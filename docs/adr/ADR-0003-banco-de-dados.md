---
id: ADR-0003
title: Banco de Dados
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
---

# ADR-0003 — Banco de Dados

## Contexto e Problema
Precisamos decidir onde e como armazenar os dados de partidas em andamento e as filas de matchmaking, garantindo que possam sobreviver a restarts do servidor.

## Direcionadores da Decisão
- Separação de módulos: o banco deve suportar uma divisão lógica de esquemas para não misturar os domínios do Modular Monolith (Matchmaking e Gameplay).
- Facilidade no desenvolvimento local sem instalações nativas pesadas.

## Opções Consideradas
- SQL Server (via container)
- SQLite
- PostgreSQL (via container)

## Resultado da Decisão
**Opção escolhida:** SQL Server, porque, combinado com o .NET Aspire, sobe automaticamente sem esforço e fornece suporte robusto a **Schemas** de banco de dados (`Matchmaking.X`, `Gameplay.Y`), o que é ideal para aplicar a separação de domínios (isolation) recomendada na arquitetura de Modular Monolith. 

### Consequências
- **Boa**, porque permite isolar completamente as tabelas de diferentes módulos no mesmo banco físico.
- **Boa**, porque não necessita de configurações de Docker por parte do desenvolvedor (o Aspire cuida disso).
- **Ruim**, porque consome mais recursos de memória rodando no Docker localmente comparado a um simples arquivo SQLite.

### Confirmação (G3)
Revisão de schema no G4 e testes de arquitetura garantindo que os DbContexts pertençam a schemas estritos.

## Mais Informações
Ef Core 8 suporta SQL Server nativamente.
