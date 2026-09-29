# ✕ ◯ Jogo da Velha Multiplayer

Um jogo da velha multiplayer em tempo real construído com **.NET 10**, **Blazor Server**, **.NET Aspire** e arquitetura **Modular Monolith**, desenvolvido com metodologia **Spec-Driven Development (SDD)**.

---

## 🚀 Tecnologias e Arquitetura

- **Frontend & Real-Time:** [.NET Blazor Server](https://learn.microsoft.com/aspnet/core/blazor/) com renderização interativa (`InteractiveServer`) e eventos reativos em memória (sem polling).
- **Orquestração e Observabilidade:** [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) gerenciando containers e service discovery.
- **Banco de Dados:** SQL Server containerizado via Aspire com [Entity Framework Core](https://learn.microsoft.com/ef/core/).
- **Inteligência Artificial:** Algoritmo clássico **Minimax** no módulo de Gameplay para jogo solo invencível.
- **Arquitetura Modular Monolith:**
  - `TicTacToe.Modules.Gameplay`: Domínio de regras do jogo, IA Minimax, entidade `MatchResult`, ranking acumulado e `GameplayDbContext` (schema `Gameplay`).
  - `TicTacToe.Modules.Matchmaking`: Fila de pareamento pública e gerenciamento de salas privadas por código curto (`ConcurrentDictionary`).
  - `TicTacToe.Web`: Frontend Blazor Server com tema escuro, efeitos visuais (confetes JS), leaderboard e injeção de dependências dos módulos.
  - `TicTacToe.AppHost`: Orquestrador Aspire que provisiona SQL Server e interliga os serviços.
  - `TicTacToe.ServiceDefaults`: Configurações de métricas, telemetria (OpenTelemetry) e health checks.
  - `TicTacToe.Tests`: 32 testes automatizados cobrindo todos os módulos e cenários com xUnit e EF Core InMemory.

---

## 🎨 Design system (Tailwind CSS standalone)

O visual "Cyber Arena" usa o **Tailwind CSS v4 standalone** (binário único, sem Node/npm; ADR-0008). Os tokens (cores, fontes, raios, espaçamento) vivem em `src/TicTacToe/TicTacToe.Web/Styles/cyber-arena.input.css` e o CSS gerado é **versionado** em `wwwroot/css/cyber-arena.css`, então `dotnet build` e os testes não precisam do binário. Só quem altera classes ou tokens precisa dele:

```bash
tools/tailwind/install.sh          # baixa a versão de tools/tailwind/version.txt e verifica o sha256
tools/tailwind/build.sh            # regenera wwwroot/css/cyber-arena.css (commite o resultado)
tools/tailwind/build.sh --check    # falha se o arquivo versionado estiver desatualizado (usado no CI)
```

O reset de CSS do Tailwind (*preflight*) está habilitado: elementos HTML chegam sem estilo do navegador, então tipografia, listas e botões são estilizados por utilitários dos tokens.

Os scripts são POSIX (macOS, Linux ou WSL/Git Bash no Windows). As fontes **Outfit** e **Space Grotesk** (licença SIL OFL 1.1, via Google Fonts; textos em `wwwroot/fonts/OFL-*.txt`) são auto-hospedadas em `wwwroot/fonts`. A referência visual do Stitch fica em `docs/design/stitch/`.

---

## 🎮 Funcionalidades

- **Matchmaking Instantâneo:** Jogadores entram na fila com seu apelido e são pareados automaticamente em tempo real via eventos em memória.
- **Salas Privadas com Código:** Crie uma sala protegida por código (ex: `SALA-7X9B`) e compartilhe com um amigo para jogarem juntos.
- **Modo Solo vs IA (Minimax):** Jogue contra o computador sem precisar de segundo jogador. A IA no modo difícil calcula todas as árvores de possibilidades e nunca perde.
- **Placar da Sessão:** Acompanhamento acumulado das vitórias entre os dois jogadores na sessão atual (`Thomas 2 ✕ 1 Ana`).
- **Efeito de Confetes na Vitória:** Comemoração visual com partículas de confetes na tela do vencedor via JS Interop.
- **Jogar Novamente (Rematch):** Reinicia o tabuleiro mantendo os mesmos oponentes e acumulando o placar.
- **Histórico Persistido:** Cada partida concluída é salva no SQL Server com nomes, vencedor e data/hora.
- **Classificação Geral / Leaderboard:** Página `/leaderboard` com o ranking dos maiores vencedores e medalhas (🥇, 🥈, 🥉).
- **Navegação Integrada:** Abas para **Jogar** (`/`), **Histórico** (`/history`) e **Ranking** (`/leaderboard`).

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
  - `SPEC-0013`: Evolução da Plataforma (Épico)
  - `SPEC-0014`: Placar da Sessão e Efeitos de Vitória
  - `SPEC-0015`: Modo Solo vs IA Minimax
  - `SPEC-0016`: Salas Privadas com Código
  - `SPEC-0017`: Leaderboard e Estatísticas
  - `SPEC-0018`: Sincronização Reativa por Eventos

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

### 2. Jogar
1. Acesse o Dashboard do Aspire e clique no endpoint do serviço `webfrontend`.
2. Para **Modo Solo**, digite seu apelido e clique em **Jogar vs Robô (IA) 🤖**.
3. Para **Salas Privadas**, clique em **Criar Sala Privada 🔒**, passe o código para um amigo na outra aba e clique em **Entrar 🔑**.
4. Para **Fila Pública**, clique em **Jogar Online 🌐** em duas abas para pareamento instantâneo.

---

## 🧪 Rodando os Testes

Execute a suíte com 32 testes unitários e de integração:

```bash
dotnet test
```
