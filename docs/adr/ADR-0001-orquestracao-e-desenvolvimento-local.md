---
id: ADR-0001
title: Orquestração e Desenvolvimento Local
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
enforced_by: "N/A — histórico"
---

# ADR-0001 — Orquestração e Desenvolvimento Local

## Contexto e Problema
Precisamos de uma forma simples para rodar o projeto localmente e garantir que as dependências de infraestrutura (como Banco de Dados) subam de forma fluida para os desenvolvedores, sem a necessidade de instalação manual de serviços na máquina ou o gerenciamento de arquivos `docker-compose` complexos, mantendo o ambiente de produção isolado e padronizado.

## Direcionadores da Decisão
- "Sem mimimi": A experiência de desenvolvimento local deve ser de um clique.
- Não poluir a máquina local do desenvolvedor com instalações globais de bancos de dados.
- Integração natural com o ecossistema .NET.

## Opções Consideradas
- .NET Aspire
- Docker Compose convencional com `docker-compose.yml`
- Instalação manual de banco de dados na máquina

## Resultado da Decisão
**Opção escolhida:** .NET Aspire, porque automatiza totalmente o provisionamento de containers locais (ex: SQL Server) durante o `F5` / `dotnet run`, injetando as strings de conexão e fornecendo um dashboard nativo de telemetria sem configuração adicional.

### Consequências
- **Boa**, porque não precisamos criar e manter `docker-compose.yml` ou scripts de configuração para rodar localmente.
- **Boa**, porque já ganhamos OpenTelemetry, Logs e Traces out-of-the-box.
- **Ruim**, porque amarra a orquestração de desenvolvimento à ferramenta da Microsoft, porém é o padrão moderno da plataforma.

### Confirmação (G3)
Revisão no G4. (Garantido estruturalmente pelos projetos `.AppHost` e `.ServiceDefaults`).

## Mais Informações
Verificado na documentação oficial do .NET 8 Aspire (Set 2026).
