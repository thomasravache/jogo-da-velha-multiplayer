---
id: SPEC-0025
title: Eventos em Tempo Real sem Polling e EF Core Migrations
tier: full
type: refactor
user_facing: true
status: approved
created: 2026-09-29
parent: SPEC-0021
depends_on: [SPEC-0024]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/**, src/TicTacToe/TicTacToe.Web/**, tests/TicTacToe.Tests/**]
adrs: [ADR-0003, ADR-0004]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0025 — Eventos em Tempo Real sem Polling e EF Core Migrations

## 1. Visão Geral
Eliminar o temporizador redundante de polling (`_pollTimer`) em favor de sincronização 100% orientada a eventos (`OnStateChanged` e `OnPlayerMatched`) e substituir a criação dinâmica de banco (`EnsureCreatedAsync()`) por migrações versionadas do Entity Framework Core (`MigrateAsync()`).

## 2. Motivação & Escopo
**Motivação:** A UI do Blazor ainda mantém um `_pollTimer` disparando a cada 1 segundo como redundância, consumindo CPU desnecessária e adicionando latência. Além disso, o banco de dados SQL Server inicializa via `EnsureCreatedAsync()`, impedindo evolução de esquema e controle de versão em produção.

**Objetivos (dentro do escopo):**
- Remover todo uso de `System.Threading.Timer` de polling em `Home.razor.cs`.
- Garantir que todas as transições de tela (pareamento de matchmaking, jogadas, vitórias, reinícios) ocorram de forma 100% reativa via eventos C#.
- Gerar migração formal inicial (`InitialCreate`) do Entity Framework Core em `src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations/`.
- Substituir `EnsureCreatedAsync()` por `db.Database.MigrateAsync()` no startup do `Program.cs`.

**Não-objetivos (fora do escopo):**
- Alterar o esquema das tabelas existentes (`GameResults`).

## 3. Dependências
- **Implementações necessárias:** SPEC-0024 (componentes decompostos e code-behind).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** Pacote `Microsoft.EntityFrameworkCore.Design`.

## 4. Decisão Arquitetural
**Contexto:** Padrão documentado em ADR-0003 e ADR-0004.

**Decisão:** Arquitetura orientada a eventos puros no Blazor Server e migrações versionadas com histórico `__EFMigrationsHistory` no banco de dados.

**Justificativa:** Elimina overhead de I/O periódico e permite deploys previsíveis e seguros em ambientes de homologação e produção.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Polling cíclico descartado por desperdício de recursos e lentidão na experiência do usuário.

**ADRs:** ADR-0003, ADR-0004.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Notificação de jogadas e eventos reativos em menos de 10ms.
- **Segurança:** Banco de dados versionado sem comandos DDL ad-hoc.
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** Subscrição e cancelamento seguro de eventos via `IDisposable`.
- **Acessibilidade (UI):** N/A.
- **Custo:** Redução de consumo de CPU no servidor Kestrel.

## 6. Artefato A — Contrato
**Interface:** `src/TicTacToe/TicTacToe.Web/Program.cs` e `GameSession.cs`

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameplayDbContext>();
    await db.Database.MigrateAsync();
}
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Startup com Migrations | Inicialização da aplicação | Executa `MigrateAsync()` aplicando migrações pendentes | UT-02, IT-01 |
| Notificação Reativa | Jogada feita por oponente | Dispara `OnStateChanged` e renderiza sem depender de timers | UT-01, E2E-01 |
| Pareamento Reativo | Partida encontrada | Dispara `OnPlayerMatched` e transiciona tela instantaneamente | UT-01, E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — comportamento já coberto.

### 7.2 Testes Unitários
- **UT-01** — Dado componente Home, a sincronização de matchmaking e gameplay opera exclusivamente por eventos sem instanciar `System.Threading.Timer`.
- **UT-02** — Dado o assembly `TicTacToe.Modules.Gameplay`, contém migrações EF Core e `ModelSnapshot` registrados.

### 7.3 Testes de Integração
- **IT-01** — Execução do `MigrateAsync()` contra banco de dados real em container aplica histórico de migrações com sucesso.

### 7.4 Testes de Contrato
- N/A.

### 7.5 Testes E2E
- **E2E-01** — Partida multiplayer e solo completas funcionam de ponta a ponta com atualização instantânea na UI.

### 7.6 Outros
- N/A.

**Dublês e dados de teste:** Banco de dados SQL Server em container de teste.

**Ambiente de execução:** xUnit e Aspire local.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto no branch principal.
- **Dados/schema:** Migração EF Core aplicada automaticamente no startup.
- **Compatibilidade:** Totalmente compatível com dados existentes.
- **Observabilidade:** Logs do EF Core e logs de inicialização.
- **Rollback:** `git revert`.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Aguardando aprovação humana.

## 11. Checklist de Implementação

**Fase 1: Testes (Red)**
- [ ] Escrever testes em `tests/TicTacToe.Tests/ReactiveEventsAndMigrationsTests.cs` com tags `SPEC-0025:UT-01`, `SPEC-0025:UT-02`, `SPEC-0025:IT-01` e `SPEC-0025:E2E-01`
- [ ] Confirmar que os testes falham antes da implementação (Red)

**Fase 2: Implementação (Green)**
- [ ] Remover temporizadores `_pollTimer` em `Home.razor.cs` garantindo que toda a reatividade venha de `OnStateChanged` e `OnPlayerMatched`
- [ ] Adicionar migração inicial EF Core `InitialCreate` no módulo `Gameplay`
- [ ] Substituir `EnsureCreatedAsync()` por `MigrateAsync()` em `Program.cs`
- [ ] Confirmar que todos os testes passam (Green)

**Fase 3: Refactor & Qualidade**
- [ ] Executar build completo e suíte de testes (`dotnet test`)
- [ ] Registrar evidências dos gates G1–G4

**Fase final: Integração e Entrega**
- [ ] Preencher Relatório de Entrega
- [ ] Fechar spec (G7) e atualizar INDEX.md

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate`: 0 erros | 2026-09-29 |
| G1 Red | PENDING | | |
| G2 Green | PENDING | | |
| G3 Arquitetura | PENDING | | |
| G4 Review | PENDING | | |
| G5 Integração & CI | PENDING | | |
| H2 Integração aprovada | PENDING | | |
| G6 Deploy | PENDING | | |
| G7 Pronto & Docs | PENDING | | |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue

### Como foi feito

### Prova de Correção
N/A — tipo refactor.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|

### Definição de Pronto
- [ ] Todos os testes do plano passando e listados na Verificação
- [ ] Todo comportamento do Mapa de Comportamentos coberto e verificado
- [ ] Suíte completa, arquitetura e CI verdes no resultado integrado (G5)
- [ ] Review independente sem achados blocker/major (G4)
- [ ] Padrão arquitetural existente mantido, ou desvio coberto por ADR aprovado
- [ ] Requisitos não-funcionais medidos com evidência (ou N/A justificado)
- [ ] Disponível no ambiente-alvo via pipeline, com smoke/E2E passando no ambiente (G6)
- [ ] Observabilidade e rollback prontos conforme o Plano de Rollout
- [ ] Documentação raiz e CHANGELOG atualizados (G7)
- [ ] Pendências registradas como novas specs (ou nenhuma)

### Deploy

### Pendências

## 15. Emendas
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
