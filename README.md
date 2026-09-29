# ✕ ◯ Jogo da Velha Multiplayer

Um jogo da velha multiplayer em tempo real construído com **.NET 10**, **Blazor Server**, **.NET Aspire** e arquitetura **Modular Monolith**, desenvolvido com metodologia **Spec-Driven Development (SDD)**.

---

## 🚀 Tecnologias e Arquitetura

- **Frontend & Real-Time:** [.NET Blazor Server](https://learn.microsoft.com/aspnet/core/blazor/) com renderização interativa (`InteractiveServer`).
- **Orquestração e Observabilidade:** [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) gerenciando containers e service discovery.
- **Banco de Dados:** SQL Server containerizado via Aspire com [Entity Framework Core](https://learn.microsoft.com/ef/core/).
- **Arquitetura Modular Monolith:**
  - `TicTacToe.Modules.Gameplay`: Domínio puro de regras do jogo, turnos, validação de vitórias/empates, entidade `MatchResult` e `GameplayDbContext` (schema `Gameplay`).
  - `TicTacToe.Modules.Matchmaking`: Fila de pareamento in-memory concorrente (`ConcurrentQueue` / `ConcurrentDictionary`).
  - `TicTacToe.Web`: Frontend Blazor Server com tema escuro e injeção de dependências dos módulos.
  - `TicTacToe.AppHost`: Orquestrador Aspire que provisiona SQL Server e liga as aplicações.
  - `TicTacToe.ServiceDefaults`: Configurações de métricas, telemetria (OpenTelemetry) e health checks.
  - `TicTacToe.Tests`: Bateria de testes automatizados com xUnit e EF Core InMemory.

---

## 🎮 Funcionalidades

- **Matchmaking Instantâneo:** Jogadores entram na fila com seu apelido e são pareados automaticamente em tempo real.
- **Jogo em Tempo Real:** Sincronização entre navegadores via circuitos Blazor Server.
- **Jogar Novamente (Rematch):** Botão que reinicia o tabuleiro mantendo os mesmos oponentes sem recarregar a página.
- **Histórico Persistido:** Cada partida concluída é salva no SQL Server com nomes, vencedor e data/hora.
- **Navegação Integrada:** Aba `/` para jogar e `/history` para consultar as partidas recentes.

---

## 📋 Metodologia SDD (Spec-Driven Development)

O projeto foi inteiramente concebido e implementado utilizando o ciclo de qualidade do **SDD**:
- **ADRs (Architecture Decision Records):**
  - `ADR-0001`: Orquestração e Desenvolvimento Local com .NET Aspire
  - `ADR-0002`: Frontend Interativo com Blazor Server
  - `ADR-0003`: Banco de Dados SQL Server via containers
  - `ADR-0004`: Arquitetura Modular Monolith
- **Especificações (Specs) Implementadas:**
  - `SPEC-0001`: Fundação do Projeto (Épico)
  - `SPEC-0002`: Repositório e Ferramental
  - `SPEC-0003`: Harness de Testes
  - `SPEC-0004`: Walking Skeleton (Aspire + Blazor + SQL)
  - `SPEC-0005`: Jogo da Velha (Épico)
  - `SPEC-0006`: Módulo de Matchmaking
  - `SPEC-0007`: Módulo de Gameplay
  - `SPEC-0008`: UI Interativa
  - `SPEC-0009`: Nomes de Jogador
  - `SPEC-0010`: Histórico de Partidas no SQL Server
  - `SPEC-0011`: Navegação entre Jogo e Histórico
  - `SPEC-0012`: Jogar Novamente (Rematch)

---

## 🏃 Como Rodar Localmente

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker Desktop](https://www.docker.com/) ou Podman (para o container do SQL Server gerenciado pelo Aspire)

### 1. Iniciar o Aspire AppHost
No diretório raiz do repositório:

```bash
dotnet run --project src/TicTacToe/TicTacToe.AppHost/TicTacToe.AppHost.csproj
```

O console exibirá o link para o **Dashboard do Aspire** (ex: `https://localhost:17228/login?t=...`).

### 2. Jogar Multiplayer
1. Acesse o Dashboard do Aspire e clique no endpoint do serviço `webfrontend`.
2. Abra a mesma URL em duas abas (ou em navegadores diferentes / aba anônima).
3. Na primeira janela, digite seu apelido e clique em **Jogar Agora 🎮**.
4. Na segunda janela, digite outro apelido e clique em **Jogar Agora 🎮**.
5. O pareamento ocorrerá na hora e a partida começará!

---

## 🧪 Rodando os Testes

Execute a suíte completa de testes unitários e de integração:

```bash
dotnet test
```
