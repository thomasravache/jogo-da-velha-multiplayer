---
id: SPEC-0026
title: Redesign de UI e Tema Escuro Imersivo com MudBlazor
tier: full
type: migration
user_facing: true
status: in-progress
created: 2026-09-29
parent:
depends_on: []
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/**, tests/TicTacToe.Tests/**]
adrs: [ADR-0007]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0026 — Redesign de UI e Tema Escuro Imersivo com MudBlazor

## 1. Visão Geral
Modernizar e profissionalizar o frontend do Jogo da Velha adotando a biblioteca de componentes e design system **MudBlazor**. A migração substitui o layout corporativo legado (`MainLayout` com sidebar lateral e barra branca de dashboard) por um layout imersivo focado em jogos (`MudLayout` com `MudAppBar` superior transparente/escuro), centraliza a gestão de tema escuro com `MudThemeProvider` (resolvendo em definitivo problemas de contraste e telas brancas) e aprimora os subcomponentes de jogo com Cards, Botões e Inputs elegantes do MudBlazor.

## 2. Motivação & Escopo
**Motivação:** A interface combinava a estrutura corporativa padrão do template ASP.NET Core com estilização ad-hoc do jogo. Ao isolar os estilos no Blazor CSS Isolation, os elementos externos (sidebar, main e top-row do Bootstrap) mantiveram fundos claros, quebrando o contraste visual ("tela branca") e tornando textos e controles quase invisíveis. A adoção do MudBlazor entrega um design system de alta qualidade nativo em C#, suporte robusto a temas escuros e componentes modernos.

**Objetivos (dentro do escopo):**
- Adicionar o pacote NuGet `MudBlazor` em `src/TicTacToe/TicTacToe.Web/TicTacToe.Web.csproj` e `tests/TicTacToe.Tests/TicTacToe.Tests.csproj`.
- Registrar serviços MudBlazor (`AddMudServices()`) no `Program.cs`.
- Importar scripts, folhas de estilo e fontes do MudBlazor em `App.razor`.
- Refatorar `MainLayout.razor` para utilizar `MudThemeProvider` (Dark Mode ativado), `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider` e `MudAppBar` superior, eliminando a sidebar lateral corporativa e links genéricos.
- Atualizar `Lobby.razor`, `Scoreboard.razor` e a página `Home.razor` para utilizar os componentes e tema escuro consistente do MudBlazor.
- Adicionar cobertura de testes unitários e de integração com bUnit em `tests/TicTacToe.Tests/MudBlazorIntegrationTests.cs`.

**Não-objetivos (fora do escopo):**
- Alterar as regras de negócio de jogadas (`GameplayModule`) ou pareamento de partidas (`MatchmakingModule`).
- Alterar o banco de dados SQL Server ou contratos de migração.

## 3. Dependências
- **Implementações necessárias:** SPEC-0024 (componentes decompostos) e SPEC-0025 (eventos reativos).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** Pacote `MudBlazor` v9+.

## 4. Decisão Arquitetural
**Contexto:** Padrão documentado em ADR-0007.

**Decisão:** Substituir o Bootstrap corporativo e layout tradicional pelo MudBlazor como biblioteca oficial de componentes e design system do Blazor Server, com tema escuro nativo configurado via `MudThemeProvider`.

**Justificativa:** Fornece consistência visual total, suporte a tema escuro sem falhas de contraste, componentes ricos de formulário e notificações em toast sem dependência de JavaScript externo.

**Desvio do padrão existente:** Migração arquitetural de UI coberta pelo ADR-0007 (suplanta ADR-0002).

**Alternativas descartadas:** Tailwind CSS (rejeitado por exigir Node/npm e desenvolvimento manual de cada componente); Bootstrap Dark manual (rejeitado por manter aparência de template corporativo).

**ADRs:** ADR-0007.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Renderização Blazor Server fluida com tempos de resposta de UI < 16ms.
- **Segurança:** Sanitização de inputs gerenciada nativamente pelos componentes MudBlazor.
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** Layout totalmente responsivo com suporte idêntico para telas móveis e desktop.
- **Acessibilidade (UI):** Contraste em conformidade com WCAG AA (texto claro sobre fundo escuro de alto contraste).
- **Custo:** N/A — biblioteca open source com licença MIT.

## 6. Artefato A — Contrato

### 6.1 Layout Estrutural (`MainLayout.razor`)
```razor
<MudThemeProvider Theme="_customTheme" IsDarkMode="true" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="2" Dense="true" Color="Color.Surface">
        <MudText Typo="Typo.h6" Class="d-flex align-center font-weight-bold">
            <MudIcon Icon="@Icons.Material.Filled.SportsEsports" Class="mr-2" Color="Color.Primary" />
            Jogo da Velha Multiplayer
        </MudText>
        <MudSpacer />
        <MudButton Href="/" StartIcon="@Icons.Material.Filled.PlayArrow" Color="Color.Inherit">Jogar</MudButton>
        <MudButton Href="/history" StartIcon="@Icons.Material.Filled.History" Color="Color.Inherit">Histórico</MudButton>
        <MudButton Href="/leaderboard" StartIcon="@Icons.Material.Filled.Leaderboard" Color="Color.Inherit">Ranking</MudButton>
    </MudAppBar>

    <MudMainContent Class="d-flex justify-center align-center min-vh-100 pa-4 game-main-canvas">
        @Body
    </MudMainContent>
</MudLayout>
```

### 6.2 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Inicialização de Tema Escuro | Aplicação carrega no navegador | MudThemeProvider ativa IsDarkMode com paleta escura (#1a1a2e / #16213e) | UT-01, E2E-01 |
| Navegação Imersiva | Visualização do Header superior | MudAppBar exibe marca, ícones e links de navegação sem sidebar lateral | UT-02, E2E-01 |
| Renderização do Lobby com MudBlazor | Página Home sem partida ativa | Lobby renderiza MudCard escuro com MudTextField, MudButton e seletores estilizados | IT-01, E2E-01 |
| Tabuleiro Responsivo | Partida ativa | Grade 3x3 perfeitamente centralizada e contrastada no canvas escuro | IT-02, E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — cobertura funcional já existente e mantida.

### 7.2 Testes Unitários
- **UT-01** — Dado `Program.cs`, registra os serviços MudBlazor (`AddMudServices`) e `App.razor` carrega os assets do MudBlazor.
- **UT-02** — Dado componente `MainLayout` renderizado via bUnit, inicializa `MudThemeProvider` com tema escuro e exibe o `MudAppBar` superior.

### 7.3 Testes de Integração
- **IT-01** — Dado componente `Lobby` com MudBlazor, renderiza o campo de apelido e botões de jogo no tema escuro.
- **IT-02** — Dado componente `GameBoard`, renderiza as 9 células jogáveis e processa cliques de jogada sem regressão.

### 7.4 Testes de Contrato
- N/A — front-end interno.

### 7.5 Testes E2E
- **E2E-01** — Jornada do usuário: o container principal possui fundo escuro, alto contraste visual e zero telas brancas ou elementos corporativos legados.

### 7.6 Outros
- N/A.

**Dublês e dados de teste:** Contexto `bUnit` com serviços MudBlazor registrados em memória.

**Ambiente de execução:** xUnit com bUnit e execução local no Kestrel/Cloudflare.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto no branch principal.
- **Dados/schema:** N/A.
- **Compatibilidade:** 100% retrocompatível com regras e rotas existentes (`/`, `/history`, `/leaderboard`).
- **Observabilidade:** Logs do Kestrel e console Blazor.
- **Rollback:** `git revert`.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado".

## 11. Checklist de Implementação

**Fase 1: Testes (Red)**
- [ ] Escrever testes de integração bUnit para MudBlazor em `tests/TicTacToe.Tests/MudBlazorIntegrationTests.cs` com tags `SPEC-0026:UT-01`, `SPEC-0026:UT-02`, `SPEC-0026:IT-01`, `SPEC-0026:IT-02` e `SPEC-0026:E2E-01`
- [ ] Confirmar que os testes falham antes da implementação (Red)

**Fase 2: Implementação (Green)**
- [ ] Adicionar pacote `MudBlazor` em `TicTacToe.Web.csproj` e `TicTacToe.Tests.csproj`
- [ ] Registrar `AddMudServices()` em `Program.cs` e `@using MudBlazor` em `_Imports.razor`
- [ ] Adicionar folhas de estilo e script do MudBlazor em `App.razor`
- [ ] Refatorar `MainLayout.razor` para `MudLayout`, `MudAppBar` e `MudThemeProvider` com tema escuro imersivo
- [ ] Atualizar `Lobby.razor`, `Scoreboard.razor` e `Home.razor.css` para a paleta de cores do MudBlazor
- [ ] Confirmar que todos os testes passam (Green)

**Fase 3: Refactor & Qualidade**
- [ ] Executar build e suíte de testes completa (`dotnet test`)
- [ ] Validar conformidade de formatação com `dotnet format --verify-no-changes`
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
