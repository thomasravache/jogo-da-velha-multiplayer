---
id: SPEC-0034
title: Remoção do MudBlazor e do Bootstrap
tier: full
type: migration
user_facing: true
status: proposed
created: 2026-09-29
parent: SPEC-0028
depends_on: [SPEC-0030, SPEC-0031, SPEC-0032, SPEC-0033]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/TicTacToe.Web.csproj, tests/TicTacToe.Tests/TicTacToe.Tests.csproj, src/TicTacToe/TicTacToe.Web/Program.cs, src/TicTacToe/TicTacToe.Web/Components/_Imports.razor, src/TicTacToe/TicTacToe.Web/Components/App.razor, src/TicTacToe/TicTacToe.Web/Components/Layout/**, src/TicTacToe/TicTacToe.Web/Components/Pages/Counter.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Error.razor, src/TicTacToe/TicTacToe.Web/wwwroot/app.css, src/TicTacToe/TicTacToe.Web/wwwroot/lib/**, src/TicTacToe/TicTacToe.Web/Styles/**, src/TicTacToe/TicTacToe.Web/wwwroot/css/**, tests/TicTacToe.Tests/MudBlazorIntegrationTests.cs, tests/TicTacToe.Tests/TailwindDesignSystemTests.cs, README.md, CHANGELOG.md]
adrs: [ADR-0008, ADR-0007]
external: []
size: M
approved_by:
approved_at:
---

# SPEC-0034 — Remoção do MudBlazor e do Bootstrap

## 1. Visão Geral
Fecha a migração: remove o pacote **MudBlazor** (app e testes), o **Bootstrap** e todo CSS/`<style>` legado, habilita o **preflight** do Tailwind (reset CSS) e reestiliza os últimos resquícios de template (`Error`, `#blazor-error-ui`). Ao final, o app usa somente o design system Cyber Arena. É a etapa 4 de 4 do strangler e a que ativa a regra de arquitetura do ADR-0008.

## 2. Motivação & Escopo
**Motivação:** durante a coexistência o app carrega duas bibliotecas de estilo e um pacote inútil; o preflight não podia ser ativado enquanto houvesse telas em MudBlazor. Sem esta spec a migração fica incompleta e o ADR-0007 continua ativo.

**Objetivos (dentro do escopo):**
- Remover `PackageReference` a `MudBlazor` de `TicTacToe.Web.csproj` e `TicTacToe.Tests.csproj`; remover `AddMudServices()` de `Program.cs` e `@using MudBlazor` de `_Imports.razor`.
- Remover de `MainLayout.razor` os providers e o tema Mud (`MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`, `_theme`).
- Remover de `App.razor` o CSS/JS do MudBlazor, o link do Bootstrap e o link de Roboto (Google Fonts).
- Apagar `wwwroot/lib/bootstrap`, reduzir `wwwroot/app.css` ao que sobrar necessário (validação de formulário, `blazor-error-boundary`) e limpar variáveis/regras legadas (`.mud-main-content`, `--x-color`, etc.).
- Habilitar o preflight no CSS de entrada, regenerar `wwwroot/css/cyber-arena.css` e conferir as quatro telas.
- Reestilizar `Error.razor` e `#blazor-error-ui` com os tokens.
- Remover páginas e componentes de template sem uso: `Counter.razor` (rota `/counter`) e `NavMenu.razor`/`NavMenu.razor.css`.
- Substituir `MudBlazorIntegrationTests` por testes do novo estado e endurecer `TailwindDesignSystemTests` (regras de arquitetura do ADR-0008). Os demais testes já foram migrados pelas specs de cada tela (SPEC-0030 a 0033); esta spec só ajusta o que ainda registrar serviços do MudBlazor.
- README (stack de UI e fluxo do Tailwind) e CHANGELOG.

**Não-objetivos (fora do escopo):**
- Novas telas ou comportamentos.
- Alterar o status dos ADRs (0007 → `superseded`, 0008 → `accepted`): feito pelo Architect no H1 e no fechamento (G7), como manda o processo.
- Trocar o CSS isolation ou o pipeline do Tailwind (SPEC-0029).

## 3. Dependências
- **Implementações necessárias:** SPEC-0030, SPEC-0031, SPEC-0032 e SPEC-0033 — nenhuma tela pode ainda usar componentes Mud; SPEC-0043 e SPEC-0029 vêm por transitividade.
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** binário do Tailwind instalado (SPEC-0029) para regenerar o CSS com preflight.

## 4. Decisão Arquitetural
**Contexto:** ADR-0007 (MudBlazor) e a migração para o ADR-0008; estado final descrito no épico SPEC-0028.

**Decisão:** remover a biblioteca e habilitar o reset num único PR reversível, com testes de arquitetura que impedem a volta do MudBlazor/Bootstrap e de `<style>` inline.

**Justificativa:** o preflight só é seguro quando nenhum componente depende do reset do Mud; agrupar remoção e preflight evita um estado intermediário com CSS duplicado.

**Desvio do padrão existente:** revoga o ADR-0007 (coberto pelo ADR-0008 e pelo `type: migration`).

**Alternativas descartadas:** manter o pacote "por precaução" (dependência morta, cobrança futura de atualização); remover Bootstrap em spec separada (as duas bibliotecas resetam o CSS de formas diferentes; trocar em um passo só).

**ADRs:** ADR-0008 (decisão), ADR-0007 (a ser superseded).

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** o app deixa de baixar CSS/JS do MudBlazor, CSS do Bootstrap e a fonte Roboto; o total de CSS servido cai em relação à coexistência (medido no PR pelo tamanho dos assets e pelo painel de rede).
- **Segurança:** menos dependências de terceiros; `dotnet list package --vulnerable` sem achados (security scan do `sdd-config.yml`).
- **Privacidade e dados pessoais:** sem requisições ao Google Fonts (a fonte Roboto sai); fontes locais desde a SPEC-0029.
- **Disponibilidade e resiliência:** `#blazor-error-ui` continua funcional e legível; a página `Error` continua respondendo a `/Error`.
- **Acessibilidade (UI):** contraste, foco e `aria-*` mantidos; preflight não pode remover indicadores de foco (verificado por checklist e Lighthouse ≥ 90 nas quatro telas).
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** estado final da camada de UI.

```text
TicTacToe.Web.csproj / TicTacToe.Tests.csproj : sem PackageReference a MudBlazor
Program.cs                                    : sem AddMudServices()
App.razor                                     : <link> apenas para css/cyber-arena.css (+ app.css reduzido);
                                                sem Mud, Bootstrap ou fontes remotas
Styles/cyber-arena.input.css                  : com preflight
Código-fonte (.razor .cs .css .js)            : sem "MudBlazor", "Mud*", "mud-", "bootstrap"
.razor                                        : sem tag <style>
Rotas removidas                               : /counter
```

**Arquivos/módulos afetados:** ver `touches`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Pacotes | Leitura dos `.csproj` | Nenhum `PackageReference` a MudBlazor (Web e Tests) | UT-01 |
| Código limpo | Varredura de `.razor/.cs/.css/.js` de `src` | Nenhuma referência a MudBlazor/`mud-`/Bootstrap | UT-02 |
| Sem estilo inline | Varredura de `.razor` | Nenhuma tag `<style>` | UT-03 |
| Preflight | CSS de entrada e gerado | Preflight presente (reset de caixa e tipografia base) | UT-04 |
| Documento base | `App.razor` | Só o CSS Cyber Arena (+ `app.css`); sem Mud, Bootstrap ou fonte remota | UT-05 |
| Bootstrap e template | Sistema de arquivos | `wwwroot/lib/bootstrap`, `Counter.razor`, `NavMenu.razor` inexistentes | UT-06 |
| Erro | `Error.razor` e `#blazor-error-ui` | Usam tokens/primitivos; sem classes do Bootstrap | UT-07 |
| App sem Mud | Contêiner de DI sem `AddMudServices` | Layout, Lobby, Arena, Histórico e Ranking renderizam sem exceção | IT-01 |
| Jornada completa | Percorrer as quatro telas | Shell e conteúdo corretos, sem Mud | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado o app atual, então as quatro telas renderizam seus elementos-chave no bUnit (lobby, arena, histórico, ranking); os testes das SPEC-0030 a 0033 já fixam isso, e este CH consolida uma renderização de cada tela para servir de rede de segurança durante a remoção (passa antes da mudança).

**Testes existentes afetados:** `MudBlazorIntegrationTests` inteiro (testa `AddMudServices`, `MudLayout`, componentes Mud) é removido e substituído pelos testes desta spec; qualquer teste que registre `AddMudServices()` no contexto bUnit é ajustado para não registrar. Justificativa: o MudBlazor deixa de existir (ADR-0008).

### 7.2 Testes Unitários
- **UT-01** — Dado `TicTacToe.Web.csproj` e `TicTacToe.Tests.csproj`, então nenhum contém `MudBlazor`.
- **UT-02** — Dado todos os `.razor`, `.cs`, `.css` e `.js` de `src/TicTacToe/TicTacToe.Web`, então nenhum contém `MudBlazor`, `Mud` como prefixo de componente, `mud-` nem `bootstrap`.
- **UT-03** — Dado todos os `.razor`, então nenhum contém a tag `<style`.
- **UT-04** — Dado `Styles/cyber-arena.input.css` e `wwwroot/css/cyber-arena.css`, então contêm o reset do preflight (por exemplo `box-sizing: border-box` no seletor universal).
- **UT-05** — Dado `App.razor`, então referencia `css/cyber-arena.css`, não referencia `MudBlazor`, `bootstrap`, `fonts.googleapis.com` nem `fonts.gstatic.com`.
- **UT-06** — Dado o sistema de arquivos, então `wwwroot/lib/bootstrap`, `Components/Pages/Counter.razor` e `Components/Layout/NavMenu.razor` não existem.
- **UT-07** — Dado `Error.razor` e o `#blazor-error-ui`, então usam classes dos tokens e nenhuma classe do Bootstrap (`text-danger`, `btn`, etc.).

### 7.3 Testes de Integração
- **IT-01** — Dado um contêiner de DI sem serviços do MudBlazor, quando `MainLayout`, `Lobby`, `Scoreboard`, `GameBoard`, `History` e `Leaderboard` são renderizados no bUnit, então nenhum lança exceção.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
- **E2E-01** — Jornada completa (bUnit): renderizar o layout com cada uma das quatro telas e verificar o shell (navegação, sem `mud-`) e o conteúdo-chave de cada uma.

### 7.6 Outros
- Suíte completa, `dotnet format --verify-no-changes` e `dotnet list package --vulnerable` verdes.
- Revisão visual (H2): as quatro telas em 390px e 1280px, com atenção às diferenças de reset (títulos, margens, formulários, foco).
- `tools/tailwind/build.sh --check` sem *drift* no CI.

**Dublês e dados de teste:** varredura de arquivos a partir da raiz do repositório (padrão de `MudBlazorIntegrationTests`), `BunitContext`, EF InMemory.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto em um único PR.
- **Dados/schema:** N/A.
- **Compatibilidade:** rotas `/`, `/history`, `/leaderboard` inalteradas; `/counter` deixa de existir (página de exemplo do template, sem link no app).
- **Observabilidade:** console do navegador sem 404 de CSS/JS/fonte nas quatro telas.
- **Rollback:** `git revert` do PR restaura o pacote, o Bootstrap e o estado de coexistência.
- **Etapas de migração/coexistência:** etapa 4 de 4. Critério de virada: SPEC-0030 a 0033 implementadas. Critério de remoção do caminho antigo: `UT-01`/`UT-02` verdes. Pós-merge (Architect, G7): ADR-0007 → `superseded` (`superseded_by: ADR-0008`) e ADR-0008 → `accepted`, documentados no Relatório de Entrega.

## 9. Questões em Aberto
- [x] Pode remover a rota de exemplo `/counter` e o menu lateral legado `NavMenu` (sobras do template, sem uso no app)? — Sim, remover `/counter` e `NavMenu`. (thomas, 2026-09-29)

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0034`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
