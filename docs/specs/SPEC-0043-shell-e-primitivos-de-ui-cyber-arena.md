---
id: SPEC-0043
title: Shell e primitivos de UI Cyber Arena
tier: full
type: feature
user_facing: true
status: approved
created: 2026-09-29
parent: SPEC-0028
depends_on: [SPEC-0029]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Layout/MainLayout.razor, src/TicTacToe/TicTacToe.Web/Components/Layout/MainLayout.razor.css, src/TicTacToe/TicTacToe.Web/Components/Ui/**, src/TicTacToe/TicTacToe.Web/wwwroot/img/**, src/TicTacToe/TicTacToe.Web/Program.cs, src/TicTacToe/TicTacToe.Web/wwwroot/css/**, src/TicTacToe/TicTacToe.Web/Styles/**, tests/TicTacToe.Tests/ShellLayoutTests.cs, tests/TicTacToe.Tests/UiPrimitivesTests.cs, tests/TicTacToe.Tests/MudBlazorIntegrationTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0043 — Shell e primitivos de UI Cyber Arena

## 1. Visão Geral
Substitui o `MudAppBar` por um **shell Cyber Arena** responsivo (header com navegação em pílulas no desktop, barra de navegação inferior fixa no mobile, fundo com gradientes radiais) e cria os **primitivos de UI** em `Components/Ui` que as quatro telas vão usar: `Icon`, `NeonCard`, `PillButton`, `StatusChip`, `PageHeader`, `NeonInput` e `SegmentedControl`. As telas ainda em MudBlazor continuam funcionando dentro do novo shell (coexistência).

## 2. Motivação & Escopo
**Motivação:** sem um shell e primitivos compartilhados, cada spec de tela reimplementaria cartão, botão, chip e ícone e o visual divergiria. O Stitch usa o mesmo cabeçalho e a mesma navegação em todas as telas.

**Objetivos (dentro do escopo):**
- `MainLayout` novo: marca (logo + nome), navegação **Jogar `/`**, **Histórico `/history`**, **Ranking `/leaderboard`** com estado ativo; barra inferior no mobile (abaixo de `lg`), pílulas no header a partir de `lg`; fundo `canvas` com gradientes radiais; mantém `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider` e o `#blazor-error-ui` (coexistência até a SPEC-0034).
- `ShellState` (serviço com escopo): `Immersive` e `Title`. Em modo imersivo o mobile esconde a barra inferior e mostra o título no header (usado pela Arena, SPEC-0031).
- Primitivos `Ui/*` com contratos abaixo, estilizados só com utilitários dos tokens da SPEC-0029.
- `Icon` com **conjunto fechado** de ícones SVG inline (lista no contrato), cobrindo as telas da Fase 1.
- Logo do XO Arena em `wwwroot/img` e nome "XO Arena" no header (os `<PageTitle>` de cada tela mudam nas specs das telas).
- Testes de contraste dos pares de cor dos tokens (WCAG AA).

**Não-objetivos (fora do escopo):**
- Conteúdo das telas (SPEC-0030 a 0033).
- Informações de jogador no header do Stitch (ELO, divisão, nível, avatar, ping, SignalR, áudio), pois não há backend; entram, se forem aprovadas, na Fase 2.
- Rodapé do Stitch (".NET Aspire Engine", "Blazor WebAssembly", direitos reservados de 2024): texto de protótipo, omitido.
- Botão "voltar" no header imersivo: não existe como sair da partida hoje (vem com "abandonar", SPEC-0041).
- Remoção do MudBlazor (SPEC-0034).

## 3. Dependências
- **Implementações necessárias:** SPEC-0029 — os tokens e o CSS gerado precisam existir para os utilitários funcionarem.
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A. Regeneração do CSS (`tools/tailwind/build.sh`) exige o binário instalado, conforme SPEC-0029.

## 4. Decisão Arquitetural
**Contexto:** componentes Blazor decompostos, com parâmetros/`EventCallback` e CSS isolation (ADR-0006), referência em `Components/Game/Lobby.razor` e `Scoreboard.razor`. Layout atual em `Components/Layout/MainLayout.razor`.

**Decisão:** primitivos em `Components/Ui` (namespace `TicTacToe.Web.Components.Ui`), sem lógica de negócio, estilizados por utilitários dos tokens; CSS isolation apenas para efeitos que utilitários não expressam (brilho, animações). `ShellState` como serviço `Scoped` (um por circuito Blazor).

**Justificativa:** mantém o padrão de componentes pequenos e testáveis com bUnit (ADR-0006) e concentra o design system em um lugar.

**Desvio do padrão existente:** substitui o `MudAppBar`/`MudLayout` do ADR-0007 pelo shell Cyber Arena (coberto pelo ADR-0008 e pela migração do épico SPEC-0028).

**Alternativas descartadas:** primitivos como `MarkupString`/helpers estáticos (perdem eventos e a11y); um componente por tela sem compartilhamento (divergência visual).

**ADRs:** ADR-0008.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** primitivos sem JS; troca de rota sem re-render do shell além do estado ativo do link; medido pelo teste de render único no bUnit (`UT-03`).
- **Segurança:** N/A — sem entrada de usuário além do `NeonInput`, que só faz bind de texto (Blazor escapa a saída).
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** ícone desconhecido lança `ArgumentOutOfRangeException` (falha nos testes, não em produção silenciosa).
- **Acessibilidade (UI):** WCAG 2.2 AA — contraste ≥ 4.5:1 nos pares de texto dos tokens (`UT-11`); foco visível em todo elemento interativo; `nav` com `aria-label`; link ativo com `aria-current="page"`; ícones decorativos com `aria-hidden`; alvos de toque ≥ 44px na barra inferior. Verificação manual com Lighthouse no PR.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `MainLayout`, `ShellState` e componentes `Ui/*`.

```text
ShellState (Scoped)
  bool Immersive { get; }   string? Title { get; }
  void Set(bool immersive, string? title = null)   void Reset()
  event Action? Changed

<Icon Name="play|history|leaderboard|globe|robot|lock|key|copy|check|timer|hourglass|close|
            handshake|timer-off|trophy|medal|refresh|warning|person"
      Size="16|20|24" Label="string?" />              // Label vazio => aria-hidden; nome inválido => ArgumentOutOfRangeException

<NeonCard Accent="None|X|O|Win" Class ChildContent />  // superfície de vidro (surface + blur + borda stroke); Accent aplica brilho da cor

<PillButton Variant="Primary|Outline|Ghost" Size="Md|Lg" Icon="string?" Disabled Loading
            Href="string?" OnClick="EventCallback" ChildContent />  // Href => <a>, senão <button type="button">

<StatusChip Tone="Neutral|Online|Waiting|Gold|Danger|Warning" Dot="bool" ChildContent />

<PageHeader Eyebrow="string?" Title="string" Subtitle="string?" ChildContent(ações) />

<NeonInput Value ValueChanged Label MaxLength Placeholder Id ShowCounter="bool" />  // label associado; contador "n/MaxLength"

<SegmentedControl TValue Items="IReadOnlyList<(TValue Value, string Label)>" Value ValueChanged AriaLabel />
                                                        // role="radiogroup"; itens role="radio" + aria-checked; setas movem a seleção
```

Layout (breakpoint `lg` = 1024px do Tailwind):

```text
< lg   : header simples (logo + nome)  +  <nav aria-label="Principal"> fixa no rodapé (Jogar · Histórico · Ranking)
>= lg  : header fixo (vidro) com <nav aria-label="Principal"> em pílulas; sem barra inferior
Immersive (mobile): sem barra inferior; header mostra ShellState.Title
Rotas ativas: "/" (correspondência exata), "/history", "/leaderboard"  → aria-current="page"
```

**Arquivos/módulos afetados:** ver `touches`. Logo: `wwwroot/img/xo-arena-logo.svg` (de `docs/design/stitch/assets`).

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Header desktop | Layout renderizado | Marca com logo e nome; `nav` "Principal" com Jogar, Histórico e Ranking; classes de exibição só em `lg` | UT-01 |
| Barra inferior mobile | Layout renderizado | Segunda navegação fixa, escondida em `lg`, com os mesmos três links e ícones | UT-02 |
| Link ativo | Rota `/history` (e `/`) | Só o link da rota atual tem `aria-current="page"`; `/` só é ativo em correspondência exata | UT-03 |
| Modo imersivo | `ShellState.Immersive = true` | Barra inferior ausente; título exibido no header mobile; desktop inalterado; `Reset()` volta ao normal | UT-04, IT-02 |
| Card | `Accent` None/X/O/Win | Classes de borda/brilho distintas por acento; conteúdo renderizado | UT-05 |
| Botão | Variantes, `Disabled`, `Loading`, `Href`, clique | Classes por variante; desabilitado bloqueia clique e expõe `disabled`; `Href` gera link; `Loading` mostra indicador e desabilita | UT-06 |
| Chip | Tons e `Dot` | Cor por tom; ponto animado apenas em `Waiting` | UT-07 |
| Campo de texto | Digitação com `MaxLength` | `ValueChanged` emitido; contador `n/max`; rótulo associado por `for`/`id` | UT-08 |
| Controle segmentado | Clique e teclado | `aria-checked` no selecionado; clique/setas emitem `ValueChanged` | UT-09 |
| Ícone | Nome válido/ inválido/ com `Label` | SVG inline; `aria-hidden` sem `Label`, `role="img"` com `Label`; nome inválido lança exceção | UT-10 |
| Contraste | Pares de texto dos tokens | Todos ≥ 4.5:1 | UT-11 |
| Coexistência | Tela ainda em MudBlazor dentro do shell | Renderiza sem exceção (providers mantidos) | IT-01 |
| Jornada do shell | Layout completo com corpo | Header + barra inferior + tokens presentes; sem `mud-appbar`/`mud-layout` | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — o `MainLayout` atual é coberto por `SPEC-0026:UT-02`, que é atualizado (item de Testes existentes afetados).

**Testes existentes afetados:** `MudBlazorIntegrationTests` — `SPEC-0026:UT-02` (afirma `mud-layout`/`mud-appbar`) passa a afirmar o novo shell e mantém `MudThemeProvider`; alteração justificada pela substituição do `MudAppBar` (ADR-0008).

### 7.2 Testes Unitários
- **UT-01** — Dado `MainLayout`, quando renderizado, então há link da marca para `/` com `img` do logo (com `alt`), e `nav[aria-label="Principal"]` com Jogar (`/`), Histórico (`/history`) e Ranking (`/leaderboard`), com classes que só exibem em `lg`.
- **UT-02** — Dado `MainLayout`, quando renderizado, então existe uma segunda navegação fixa com os mesmos três links e ícones, escondida em `lg`.
- **UT-03** — Dado `NavigationManager` em `/history` e depois em `/`, então apenas o link correspondente tem `aria-current="page"` e o link `/` não fica ativo em `/history`.
- **UT-04** — Dado `ShellState.Set(true, "Partida ativa")`, então a barra inferior não é renderizada e o título aparece no header; após `Reset()`, a barra volta; `Changed` dispara re-render.
- **UT-05** — Dado `NeonCard` com cada `Accent`, então as classes de acento são distintas e o `ChildContent` é renderizado.
- **UT-06** — Dado `PillButton` (Primary/Outline/Ghost; `Disabled`; `Loading`; `Href`), então cada variante tem classes próprias, desabilitado não dispara `OnClick` e tem `disabled`, `Loading` mostra indicador e desabilita, `Href` renderiza `<a>`.
- **UT-07** — Dado `StatusChip` em cada `Tone`, então a classe de cor muda e o ponto animado existe só em `Waiting`.
- **UT-08** — Dado `NeonInput` com `MaxLength=20` e `ShowCounter`, quando o usuário digita, então `ValueChanged` recebe o texto, o contador mostra `n/20` e o `label` está associado ao `input`.
- **UT-09** — Dado `SegmentedControl` com dois itens, então o selecionado tem `aria-checked="true"`, clicar no outro emite `ValueChanged` e as setas do teclado movem a seleção.
- **UT-10** — Dado `Icon`, então um nome válido gera `svg` com `aria-hidden` (sem `Label`) ou `role="img"` e `aria-label` (com `Label`), e um nome inválido lança `ArgumentOutOfRangeException`.
- **UT-11** — Dado os tokens de `cyber-arena.input.css`, então os pares `ink/canvas`, `ink/surface`, `ink-muted/surface`, `primary/canvas`, `secondary/canvas`, `success/canvas`, `gold/canvas` e `warning/canvas` têm razão de contraste ≥ 4.5.

### 7.3 Testes de Integração
- **IT-01** — Dado o `Lobby` atual (MudBlazor) renderizado sob o novo `MainLayout` com serviços do MudBlazor, então renderiza sem exceção (guarda: passa antes da mudança).
- **IT-02** — Dado o contêiner de DI do app, então `ShellState` está registrado como `Scoped`, e uma página que chama `Set(true, ...)` ao iniciar e `Reset()` ao descartar restaura o shell.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
- **E2E-01** — Jornada do shell (bUnit): renderizar `MainLayout` com um corpo simples, então header, navegação principal, barra inferior e fundo com tokens aparecem, e o markup não contém `mud-appbar` nem `mud-layout`.

### 7.6 Outros
- Acessibilidade: Lighthouse (categoria Acessibilidade ≥ 90) nas rotas `/`, `/history`, `/leaderboard` em 390px e 1280px, anexado ao PR.
- Revisão visual: header e barra inferior contra `docs/design/stitch/lobby-desktop.html` e `lobby-mobile.html` (checklist no H2).

**Dublês e dados de teste:** `BunitContext`, `FakeNavigationManager`, `AddMudServices()` e `JSInterop.Mode = Loose` (padrão de `MudBlazorIntegrationTests`).

**Ambiente de execução:** xUnit + bUnit local e no job `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; as telas ainda em MudBlazor renderizam dentro do novo shell.
- **Dados/schema:** N/A.
- **Compatibilidade:** rotas inalteradas (`/`, `/history`, `/leaderboard`).
- **Observabilidade:** console do navegador sem 404 de logo/CSS; revisão visual no PR.
- **Rollback:** `git revert` do PR (volta ao `MudAppBar`).
- **Etapas de migração/coexistência:** etapa 2 de 4 do strangler; o `MudAppBar` é removido aqui, os providers do Mud ficam até a SPEC-0034.

## 9. Questões em Aberto
- [x] O nome exibido no header deve ser **"XO Arena"** (como no Stitch e nos logos; recomendado) ou continuar **"Jogo da Velha Multiplayer"**? — "XO Arena". (thomas, 2026-09-29)
- [x] Os ícones devem ser **SVG inline** num componente `Icon` com conjunto fechado (recomendado: sem fonte externa, poucos KB) ou a **fonte Material Symbols** auto-hospedada como no protótipo? — SVG inline com conjunto fechado. (thomas, 2026-09-29)
- [x] O `<PageTitle>` das páginas (aba do navegador) também muda para o novo nome, se o nome for "XO Arena"? — Sim, os `<PageTitle>` passam a usar "XO Arena" (cada tela ajusta o seu). (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [ ] N/A — sem testes de caracterização neste plano (área já coberta ou nova)

**Fase 1: Testes (Red)**
- [ ] Escrever os testes `SPEC-0043:UT-01`, `SPEC-0043:UT-02`, `SPEC-0043:UT-03`, `SPEC-0043:UT-04`, `SPEC-0043:UT-05`, `SPEC-0043:UT-06`, `SPEC-0043:UT-07`, `SPEC-0043:UT-08`, `SPEC-0043:UT-09`, `SPEC-0043:UT-10`, `SPEC-0043:UT-11`, `SPEC-0043:IT-01`, `SPEC-0043:IT-02`, `SPEC-0043:E2E-01` com a tag `SPEC-0043:<ID>`, em commits `test(...)` com `Refs: SPEC-0043`, tocando só `test_paths`
- [ ] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [ ] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0043`)

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [ ] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [ ] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [ ] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [ ] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável
- [ ] PR com `spec_graph.py pr SPEC-0043`, CI verde (G5) e aprovação do merge (H2)
- [ ] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` das 18 specs: 0 erros, 0 avisos | 2026-09-29 |
| G1 Red | PENDING | | |
| G2 Green | PENDING | | |
| G3 Arquitetura | PENDING | | |
| G4 Review | PENDING | | |
| G5 Integração & CI | PENDING | | |
| H2 Integração aprovada | PENDING | | |
| G6 Deploy | PENDING | | |
| G7 Pronto & Docs | PENDING | | |

## 13. Registro de Impedimentos
<!-- Toda parada é registrada pelo Architect com `spec_graph.py impede` e fechada com `resolve` — não edite à mão. Tipos: spec (spec errada/incompleta → resolve com Emenda) | decisão (só o humano decide → resposta ou ADR) | trabalho (falta algo que exige código → SPEC-NNNN nova) | externo (acesso, ambiente, terceiro → ação tomada) | falha (3 FAILs seguidos no mesmo gate → diagnóstico e decisão). Com impedimento aberto a spec aparece como parada no INDEX e não pode ser fechada. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega
<!-- Preenchido no CLOSE (G7). Diz o que foi feito, como, e prova que foi resolvido. Para status implemented o validate exige todas as subseções preenchidas, todo teste do plano com PASS + evidência e a Definição de Pronto toda marcada. -->

### O que foi entregue
<!-- comportamento entregue do ponto de vista do usuário/sistema -->

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->
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
<!-- ambiente(s), versão/tag, data, estratégia, estado da feature flag, execução do pipeline -->

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0043`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
