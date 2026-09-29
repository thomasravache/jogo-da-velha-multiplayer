---
id: SPEC-0021
title: Engenharia de Qualidade e Refatoração Arquitetural
tier: epic
type: feature
status: implemented
created: 2026-09-29
depends_on: []
adrs: [ADR-0005, ADR-0006]
external: []
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0021 — Engenharia de Qualidade e Refatoração Arquitetural (Épico)

## 1. Visão
Elevar a base de código do Jogo da Velha para um padrão de qualidade enterprise através de padronização estrita de código (Linter, .editorconfig, Directory.Build.props), métricas automatizadas de cobertura e testes de UI com bUnit, componentização desacoplada de Home.razor com CSS Isolation e arquitetura reativa livre de polling com migrações formais de banco.

## 2. Escopo
**Objetivos (dentro do escopo):**
- Centralizar análise estática com `.editorconfig`, `Directory.Build.props` (`TreatWarningsAsErrors=true`) e `SonarAnalyzer.CSharp`.
- Limpar scripts de migração antigos da raiz do repositório.
- Configurar coleta de cobertura de código com Coverlet no `sdd-config.yml`.
- Adicionar biblioteca `bUnit` para testes unitários de componentes Blazor.
- Decompor o componente `Home.razor` em subcomponentes reutilizáveis (`Lobby.razor`, `Scoreboard.razor`, `GameBoard.razor`) e code-behind `Home.razor.cs`.
- Mover estilos inline para `Home.razor.css` utilizando CSS Isolation nativo.
- Eliminar o temporizador `_pollTimer` em favor de eventos reativos em tempo real.
- Criar migração formal inicial do Entity Framework Core substituindo `EnsureCreatedAsync()`.

**Não-objetivos (fora do escopo):**
- Alterar as regras ou mecânica do jogo da velha.
- Substituir o .NET Aspire ou SQL Server por outra tecnologia.

## 3. Arquitetura Alvo
**Contexto:** Monólito modular em .NET 10 com Aspire, Blazor Server e SQL Server. A refatoração mantém a separação de módulos (`Gameplay`, `Matchmaking`), aprimora o isolamento da camada de apresentação Blazor e profissionaliza a infraestrutura de engenharia e CI.

```text
src/TicTacToe/
├── TicTacToe.Modules.Gameplay/        # Regras puras de jogo, IA Minimax e DbContext
├── TicTacToe.Modules.Matchmaking/     # Fila pública e salas privadas determinísticas
├── TicTacToe.Web/                     # Blazor Server interativo
│   ├── Components/
│   │   ├── Game/
│   │   │   ├── Lobby.razor            # Subcomponente de lobby e opções
│   │   │   ├── Scoreboard.razor       # Subcomponente de placar e tags
│   │   │   └── GameBoard.razor        # Subcomponente de grade 3x3
│   │   └── Pages/
│   │       ├── Home.razor             # Orquestrador fino da página
│   │       ├── Home.razor.cs          # Code-behind isolado
│   │       └── Home.razor.css         # Estilos isolados por escopo
└── tests/TicTacToe.Tests/             # Testes xUnit de domínio + testes de componentes bUnit
```

**Decisões (ADRs):**
- ADR-0005 — Padronização de Análise Estática e Compilação Estrita
- ADR-0006 — Componentização Blazor, CSS Isolation e Testes com bUnit

**Regras de arquitetura a garantir (G3):**
- Compilação com zero warnings em todos os projetos (`TreatWarningsAsErrors`).
- Componentes de UI sem dependências diretas de banco de dados (apenas serviços injetados).
- Ausência de polling cíclico (`System.Threading.Timer`) no fluxo de renderização.

## 4. Decomposição
| Spec | Título | Tier | Tipo | Tamanho | Depende de | Consome contrato de |
|---|---|---|---|---|---|---|
| SPEC-0022 | Padronização de Código com EditorConfig e Directory.Build.props | full | foundation | S | — | — |
| SPEC-0023 | Infraestrutura de Cobertura de Código e Testes com bUnit | full | foundation | S | SPEC-0022 | — |
| SPEC-0024 | Decomposição do Componente Home e Isolamento de CSS | full | refactor | M | SPEC-0023 | — |
| SPEC-0025 | Eventos em Tempo Real sem Polling e EF Core Migrations | full | refactor | M | SPEC-0024 | — |

## 5. Estratégia de Entrega
- **Ambientes:** Deploy local via Aspire e Cloudflare Tunnel.
- **Entrega por onda:**
  - Onda 1: SPEC-0022 (Compilação estrita, `.editorconfig`, higiene de scripts)
  - Onda 2: SPEC-0023 (Coverlet, comando de cobertura e harness do bUnit)
  - Onda 3: SPEC-0024 (Componentização de UI e CSS Isolation)
  - Onda 4: SPEC-0025 (Eventos reativos puros e Migrações EF Core)
- **Feature flags:** N/A.
- **Rollback:** `git revert`.
- **Métricas de sucesso pós-release:** Build 100% livre de warnings, suíte completa de testes verde incluindo testes de UI bUnit, cobertura medida automaticamente.

## 6. Riscos & Mitigações
- **Risco:** Quebra de estilos com CSS Isolation.  
  *Mitigação:* Manter variáveis CSS customizadas (`--bg`, `--x-color`, etc.) no escopo `:root` acessível globalmente.
- **Risco:** Regressão no ciclo de vida Blazor ao extrair subcomponentes.  
  *Mitigação:* Testes de componentes com `bUnit` validando o fluxo de renderização e callbacks.

## 7. Critérios de Aceite do Épico
- [x] Compilação limpa com TreatWarningsAsErrors ativo em todos os projetos — SPEC-0022:UT-01
- [x] Limpeza dos scripts temporários da raiz do repositório — SPEC-0022:UT-02
- [x] Coleta de cobertura automatizada configurada no sdd-config.yml — SPEC-0023:UT-01
- [x] Testes de componentes bUnit cobrindo renderização do Lobby e Tabuleiro — SPEC-0023:IT-01
- [x] Componente Home decomposto com CSS Isolation e zero regressões funcionais — SPEC-0024:E2E-01
- [x] Polling por timer eliminado em favor de eventos reativos de sessão — SPEC-0025:UT-01
- [x] Migração inicial do EF Core aplicada no startup sem EnsureCreatedAsync — SPEC-0025:IT-01

## 8. Questões em Aberto
Nenhuma.

## 9. Aprovação (H1)
Uma aprovação humana cobre o épico e as specs filhas apresentadas junto com ele. Registrada no frontmatter (`approved_by`, `approved_at`) do épico e de cada filha.

## 10. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 11. Relatório de Entrega

### O que foi entregue
O Épico SPEC-0021 foi concluído integralmente através da entrega de quatro ondas de implementação coordenadas:
1. **Onda 1 (SPEC-0022):** Padronização estrita de código com `.editorconfig`, `Directory.Build.props` ativando `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `Nullable` e análise estática recomendada pelo Roslyn, além do arquivamento de scripts legados para `tools/archive/`.
2. **Onda 2 (SPEC-0023):** Configuração de cobertura de testes via Coverlet no `sdd-config.yml` e inclusão do framework `bUnit` para testes unitários de componentes Blazor em memória.
3. **Onda 3 (SPEC-0024):** Decomposição arquitetural do componente monolítico `Home.razor` em subcomponentes coesos (`Lobby.razor`, `Scoreboard.razor`, `GameBoard.razor`), isolamento de estilos com `Home.razor.css` e lógica de controle em `Home.razor.cs`.
4. **Onda 4 (SPEC-0025):** Eliminação definitiva do polling de timers em favor de eventos reativos C# puros (`OnPlayerMatched` e `OnStateChanged`), acompanhada da migração formal versionada do Entity Framework Core (`20260929130250_InitialCreate` e snapshot) e adoção de `MigrateAsync()`.

### Como foi feito
- Todas as 4 specs filhas seguiram a disciplina rigorosa do SDD (TDD Red -> Green -> Refactor -> Verify -> Gates G1-G7).
- Suíte expandida de 43 para 55 testes automatizados cobrindo domínio, higiene, bUnit e migrações.
- Zero advertências do compilador ou linter permitidas.

### Prova de Correção
- Todos os 55 testes passando com sucesso.
- Análise de formatação e linter via `dotnet format --verify-no-changes` com 0 violações.

### Verificação
| Filha | Descrição | Status |
|---|---|---|
| SPEC-0022 | Padronização e TreatWarningsAsErrors | IMPLEMENTED |
| SPEC-0023 | Cobertura de Código e bUnit | IMPLEMENTED |
| SPEC-0024 | Decomposição de UI e CSS Isolation | IMPLEMENTED |
| SPEC-0025 | Eventos Reativos e EF Core Migrations | IMPLEMENTED |

### Definição de Pronto
- [x] Todas as specs filhas implementadas e fechadas com G7
- [x] Critérios de aceite do épico satisfeitos e verificados por testes
- [x] Suíte de testes 100% verde (55/55 testes passando)
- [x] Zero avisos de compilação ou linter

## 12. Emendas
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
