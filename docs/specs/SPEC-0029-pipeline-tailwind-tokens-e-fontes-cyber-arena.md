---
id: SPEC-0029
title: Pipeline Tailwind, tokens e fontes Cyber Arena
tier: full
type: migration
user_facing: false
status: approved
created: 2026-09-29
parent: SPEC-0028
depends_on: []
consumes_contract: []
contract_version: 1
touches: [tools/tailwind/**, src/TicTacToe/TicTacToe.Web/Styles/**, src/TicTacToe/TicTacToe.Web/wwwroot/css/**, src/TicTacToe/TicTacToe.Web/wwwroot/fonts/**, src/TicTacToe/TicTacToe.Web/Components/App.razor, .github/workflows/dotnet.yml, .gitignore, README.md, tests/TicTacToe.Tests/TailwindDesignSystemTests.cs]
adrs: [ADR-0008]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0029 — Pipeline Tailwind, tokens e fontes Cyber Arena

## 1. Visão Geral
Instala a base técnica do redesign: o **Tailwind CSS v4 standalone** (sem Node) com versão pinada e verificação de checksum, o arquivo de tokens Cyber Arena em `@theme`, as fontes Outfit e Space Grotesk auto-hospedadas e o CSS gerado publicado em `wwwroot/css`. Nenhuma tela muda: o CSS entra **sem preflight** e só adiciona variáveis e utilitários, então o app continua idêntico até as specs de tela usarem os novos tokens.

## 2. Motivação & Escopo
**Motivação:** o ADR-0008 escolheu Tailwind standalone. Sem um pipeline reprodutível, cada spec de tela teria de resolver build, versão e tokens por conta própria e divergiria.

**Objetivos (dentro do escopo):**
- `tools/tailwind/version.txt` (versão pinada) e `tools/tailwind/install.sh` (baixa o binário da plataforma do release oficial e valida `sha256` contra o `sha256sums.txt` do release; recusa binário divergente).
- `tools/tailwind/build.sh` (gera o CSS a partir do arquivo de entrada com o binário instalado; modo `--check` compara com o arquivo versionado e falha em *drift*).
- `Styles/cyber-arena.input.css` com `@theme` (cores, fontes, raios, espaçamento, tamanhos do tabuleiro, animações) e `@source` dos componentes; sem preflight.
- `wwwroot/css/cyber-arena.css` gerado e versionado; `App.razor` passa a referenciá-lo (mantendo MudBlazor e o `app.css` atual).
- Fontes Outfit (400, 600, 700, 800, 900) e Space Grotesk (500, 600, 700) em `wwwroot/fonts` (woff2) com `@font-face` e `font-display: swap`.
- CI: passo de cache/instalação do binário e passo de verificação de *drift* em `.github/workflows/dotnet.yml`.
- Documentação do fluxo em `README.md` (instalar, regenerar, quando regenerar).
- `TailwindDesignSystemTests` (ADR-0008, G3).

**Não-objetivos (fora do escopo):**
- Habilitar o preflight, remover MudBlazor/Bootstrap ou tocar em qualquer `.razor` além do link em `App.razor` (SPEC-0034).
- Componentes, layout e ícones (SPEC-0043).
- Suporte a Windows nativo no script (usar WSL/Git Bash; documentado no README).
- Plugins de terceiros do Tailwind.

## 3. Dependências
- **Implementações necessárias:** N/A.
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** release `v4.3.3` do Tailwind CSS no GitHub (binário e `sha256sums.txt`); arquivos woff2 das fontes Outfit e Space Grotesk (licença OFL, a confirmar e registrar no README na implementação).

## 4. Decisão Arquitetural
**Contexto:** UI em Blazor Server com CSS isolation (ADR-0006) e MudBlazor (ADR-0007). O repositório mantém ferramentas de processo em `tools/` (`tools/sdd/spec_graph.py`) e workflows em `.github/workflows/`, e é a referência para onde colocar `tools/tailwind`.

**Decisão:** pipeline em `tools/tailwind` com scripts POSIX; saída em `wwwroot/css/cyber-arena.css` servida como asset estático pelo `MapStaticAssets` já existente; tokens definidos uma única vez em `@theme` e consumidos como utilitários (`bg-primary`, `font-display`, `rounded-card`) e como variáveis CSS (`var(--color-primary)`).

**Justificativa:** ADR-0008. O CSS gerado é versionado para que `dotnet build`, testes e `publish` não dependam do binário de 80–112 MB; só quem altera classes/tokens precisa instalá-lo, e o CI garante que o arquivo versionado corresponde à fonte.

**Desvio do padrão existente:** o uso do Tailwind (ferramenta de build externa) desvia do ADR-0007; coberto pelo ADR-0008 e pelo `type: migration` desta spec.

**Alternativas descartadas:** gerar o CSS a cada `dotnet build` com um alvo MSBuild (exige o binário em toda máquina e em todo build do CI); Tailwind via Node (descartado no ADR-0008); Google Fonts como fonte das fontes (ver Questões em Aberto).

**ADRs:** ADR-0008.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** CSS gerado minificado ≤ 60 KB (gzip ≤ 15 KB) nesta spec e fontes servidas com `font-display: swap`; medido pelo tamanho do arquivo em `IT-01`.
- **Segurança:** binário baixado apenas de `github.com/tailwindlabs/tailwindcss/releases/download/<versão>/`, com `sha256` verificado antes de marcar como executável; script falha em qualquer divergência; sem `curl | sh` e sem executar código remoto além do binário verificado. Verificado por `UT-01`.
- **Privacidade e dados pessoais:** fontes auto-hospedadas, sem requisição a terceiros em tempo de execução (nenhum IP de visitante enviado ao Google) — verificado por `UT-03`.
- **Disponibilidade e resiliência:** o app não depende do binário em execução; se o download falhar, `install.sh` sai com erro claro e o CSS versionado continua válido.
- **Acessibilidade (UI):** N/A — sem mudança visual nesta spec (contraste dos pares de cores é validado na SPEC-0043).
- **Custo:** cache do binário no CI por versão para não baixar 80–112 MB a cada execução; sem custo de infraestrutura adicional.

## 6. Artefato A — Contrato
**Interface:** scripts `tools/tailwind/install.sh` e `tools/tailwind/build.sh`, arquivo `Styles/cyber-arena.input.css` e artefato `wwwroot/css/cyber-arena.css`.

```text
tools/tailwind/version.txt            → "4.3.3"
tools/tailwind/install.sh             → baixa tailwindcss-<os>-<arch> da versão pinada
                                        baixa sha256sums.txt do mesmo release
                                        valida sha256; grava em tools/tailwind/bin/tailwindcss (ignorado no git)
                                        exit 0 = ok · exit 1 = checksum divergente ou plataforma sem binário
tools/tailwind/build.sh               → gera wwwroot/css/cyber-arena.css (minificado)
tools/tailwind/build.sh --check       → gera em arquivo temporário e compara; exit 1 se diferir (drift)

Styles/cyber-arena.input.css          → theme + utilities (sem preflight), @theme, @source, @font-face
wwwroot/css/cyber-arena.css           → artefato versionado
wwwroot/fonts/{outfit,space-grotesk}-*.woff2
```

Tokens (fonte: prosa do `DESIGN.md`, ver `docs/design/stitch/README.md`):

```css
@theme {
  /* marca / jogadores */
  --color-primary: #ff4757;          /* jogador X, CTA, derrota */
  --color-secondary: #00d2d3;        /* jogador O, ações secundárias */
  --color-success: #10b981;          /* vitória, online, timer seguro */
  --color-gold: #f39c12;             /* ranking 1–3, troféus */
  --color-warning: #fbbf24;          /* timer < 50%, aviso */
  /* superfícies (escuro) */
  --color-canvas: #1a1a2e;           /* fundo base */
  --color-surface: #16213e;          /* cards */
  --color-surface-high: #0f3460;     /* células, elementos ativos */
  --color-stroke: rgb(255 255 255 / 0.08);
  --color-stroke-active: rgb(0 210 211 / 0.25);
  --color-ink: #e2e0fc;              /* texto principal */
  --color-ink-muted: #e4bdbc;        /* texto secundário */
  /* tipografia */
  --font-display: "Outfit", ui-sans-serif, system-ui, sans-serif;
  --font-numeric: "Space Grotesk", ui-monospace, monospace;
  /* raios */
  --radius-cell: 1rem;  --radius-cell-lg: 1.5rem;  --radius-card: 1rem;  --radius-pill: 9999px;
  /* espaçamento */
  --spacing-gutter: 1rem;  --spacing-gutter-desktop: 1.5rem;
  --spacing-margin: 1rem;  --spacing-margin-desktop: 2rem;
  /* tabuleiro */
  --size-board-mobile: min(90vw, 360px);  --size-board-desktop: 480px;  --size-frame: 960px;
  /* animações */
  --animate-win-pulse: win-pulse 1.5s ease-in-out infinite;
}
```

Escala tipográfica (utilitários `text-*`): `display-hero 56/64 800`, `display-hero-mobile 36/44 800`, `headline-lg 32/40 700`, `headline-lg-mobile 26/34 700`, `headline-md 24/32 600`, `title-sm 18/24 600`, `body-lg 16/24 400`, `body-md 14/20 400`, `label-numeric 14/18 600 (Space Grotesk)`, `label-badge 12/16 700 (Space Grotesk, +0.08em)`, `board-mark 72/72 900`, `board-mark-mobile 48/48 900`.

**Arquivos/módulos afetados:** ver `touches`. `App.razor` recebe apenas `<link rel="stylesheet" href="@Assets["css/cyber-arena.css"]" />` após o `app.css`; `.gitignore` ignora `tools/tailwind/bin/`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Versão pinada | Leitura de `version.txt` | Uma versão semver `X.Y.Z`; `install.sh` usa exatamente essa versão | UT-01 |
| Instalação verificada | `install.sh` | Baixa binário + `sha256sums.txt` e compara; nunca marca como executável sem casar o hash | UT-01 |
| Tokens completos | `cyber-arena.input.css` | `@theme` define todos os tokens do contrato e não importa preflight | UT-02 |
| Fontes locais | `@font-face` | Outfit e Space Grotesk apontam para arquivos existentes em `wwwroot/fonts`, com `font-display: swap`, sem Google Fonts | UT-03 |
| CSS gerado | Arquivo versionado | Contém as variáveis dos tokens e os `@font-face`; tamanho dentro do orçamento | IT-01 |
| Coexistência | `App.razor` | Referencia `cyber-arena.css` e mantém MudBlazor e `app.css` (nenhuma tela muda) | IT-02 |
| Drift no CI | Workflow | Existe passo que roda `build.sh --check` e passo de cache do binário por versão | IT-03 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — área nova; nenhum comportamento existente é alterado.

### 7.2 Testes Unitários
- **UT-01** — Dado `tools/tailwind/version.txt` e `install.sh`, então a versão é semver, o script referencia `sha256sums.txt`, compara o hash antes de `chmod +x`, aborta com `exit 1` em divergência e baixa apenas de `github.com/tailwindlabs/tailwindcss/releases/download`.
- **UT-02** — Dado `Styles/cyber-arena.input.css`, então `@theme` define cada token do contrato (cores, fontes, raios, espaçamento, tabuleiro, animação), define `@source` para `Components`, e não contém import de preflight.
- **UT-03** — Dado o CSS de entrada, então cada `@font-face` aponta para um arquivo woff2 existente em `wwwroot/fonts`, usa `font-display: swap`, e o CSS de entrada e o CSS gerado não referenciam `fonts.googleapis.com` nem `fonts.gstatic.com`. (O `App.razor` mantém o link da fonte Roboto usado pelo MudBlazor até a SPEC-0034; não é escopo deste teste.)

### 7.3 Testes de Integração
- **IT-01** — Dado o arquivo versionado `wwwroot/css/cyber-arena.css`, então contém `--color-primary: #ff4757`, `--font-display`, `--font-numeric` e os `@font-face`, e pesa no máximo 60 KB.
- **IT-02** — Dado `App.razor`, então referencia `css/cyber-arena.css` depois de `app.css` e continua referenciando `MudBlazor.min.css` e `MudBlazor.min.js` (guarda: passa antes da mudança apenas nos itens do MudBlazor).
- **IT-03** — Dado `.github/workflows/dotnet.yml`, então contém passo de cache do binário chaveado pela versão de `version.txt` e passo que executa `tools/tailwind/build.sh --check`.

### 7.4 Testes de Contrato
N/A — não há contrato entre specs.

### 7.5 Testes E2E
N/A — `user_facing: false`; o app não muda visualmente.

### 7.6 Outros
- Verificação manual no PR: `tools/tailwind/install.sh && tools/tailwind/build.sh --check` sem diff; `dotnet build`/`dotnet test` sem o binário instalado (prova de que o CSS versionado basta).

**Dublês e dados de teste:** os testes leem arquivos do repositório (mesmo padrão de `MudBlazorIntegrationTests`, que resolve a raiz a partir de `AppContext.BaseDirectory`); nenhum acesso à rede.

**Ambiente de execução:** xUnit local e no job `build-and-test`; `--check` no CI em `ubuntu-latest` com o binário `linux-x64`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; o CSS novo só adiciona variáveis e utilitários sem preflight, então nenhuma tela existente muda.
- **Dados/schema:** N/A.
- **Compatibilidade:** MudBlazor, Bootstrap e `app.css` permanecem carregados; a ordem dos `<link>` mantém a precedência atual.
- **Observabilidade:** o passo de *drift* do CI reprova PR com CSS desatualizado; console do navegador sem 404 de fonte/CSS.
- **Rollback:** `git revert` do PR (remove link, CSS, fontes e scripts sem efeito nas telas).
- **Etapas de migração/coexistência:** etapa 1 de 4 do strangler (pipeline) → 0043 (shell e primitivos) → telas → 0034 (remoção do MudBlazor e habilitação do preflight).

## 9. Questões em Aberto
- [x] O CSS gerado deve ser **versionado no git** (recomendado: build e testes não dependem do binário; o CI só confere *drift*) ou **gerado a cada build** (sempre atual, mas exige o binário de 80–112 MB em toda máquina e em todo build)? — Versionado no git; o CI confere *drift*. (thomas, 2026-09-29)
- [x] As fontes **Outfit e Space Grotesk** devem ser **auto-hospedadas** em `wwwroot/fonts` (recomendado: sem requisição a terceiros, funciona offline, privacidade/LGPD) ou carregadas do Google Fonts como no protótipo do Stitch? — Auto-hospedadas em `wwwroot/fonts`. (thomas, 2026-09-29)
- [x] O script de instalação é **só POSIX** (macOS/Linux/WSL, documentado no README; recomendado) ou precisa também de versão PowerShell para Windows nativo? — Só POSIX (macOS/Linux/WSL), documentado no README. (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0029`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
