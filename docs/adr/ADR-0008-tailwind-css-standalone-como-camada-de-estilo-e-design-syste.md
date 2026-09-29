---
id: ADR-0008
title: Tailwind CSS standalone como camada de estilo e design system próprio
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
consulted: []
informed: []
supersedes: ADR-0007
superseded_by:
enforced_by: tests/TicTacToe.Tests/TailwindDesignSystemTests.cs
---

# ADR-0008 — Tailwind CSS standalone como camada de estilo e design system próprio

## Contexto e Problema
O ADR-0007 adotou o MudBlazor como design system (SPEC-0026). O projeto Stitch "Cyber Arena" define uma nova identidade (neon, vidro, brilho por jogador, tokens de tipografia e espaçamento) cujo HTML de referência foi gerado em Tailwind com valores arbitrários (`shadow-[...]`, gradientes radiais, `backdrop-blur`). O MudBlazor cobre estrutura e comportamento, mas a identidade visual exigiria uma camada grande de CSS próprio brigando com a especificidade dos estilos do Mud. Como implementar o redesign com máxima fidelidade ao Stitch sem reintroduzir Node/npm no projeto?

A objeção do ADR-0007 ao Tailwind era "exige pipeline Node/npm/PostCSS". O Tailwind CSS v4 distribui um **CLI standalone** (binário único por plataforma), o que remove essa objeção.

## Direcionadores da Decisão
- Fidelidade visual ao Stitch: o HTML de referência é Tailwind e pode ser traduzido quase 1:1 para Razor.
- Sem Node/npm/PostCSS no repositório, no CI nem na máquina de desenvolvimento.
- Um único conjunto de tokens (cores, fontes, raios, espaçamento) como fonte de verdade.
- Build reprodutível: versão pinada e binário verificado por checksum.
- Manter `main` sempre verde durante a migração (sem big-bang).
- Preservar o que já vale: CSS isolation e testes bUnit (ADR-0006), Blazor Server interativo.

## Opções Consideradas
- **A. Tailwind CSS v4 standalone CLI + componentes Razor próprios (`Components/Ui`).**
- **B. Manter o MudBlazor e criar uma camada Cyber Arena por cima (tema + componentes próprios + CSS).**
- **C. CSS puro com variáveis (tokens) + CSS isolation, sem framework de utilitários.**
- **D. Tailwind via Node/npm (PostCSS).** Descartada de saída: reintroduz a toolchain que o projeto evita.

## Resultado da Decisão
**Opção escolhida:** **A**, por decisão do responsável do produto (thomas, 2026-09-29), priorizando fidelidade ao Stitch e controle total da camada visual sobre o reaproveitamento de componentes prontos. O custo (reescrever os componentes que hoje usam MudBlazor) é aceito e tratado como migração incremental (strangler), não big-bang.

### Consequências
- **Boa**, porque o HTML do Stitch se traduz quase diretamente em Razor + classes utilitárias, com tokens centralizados em `@theme`.
- **Boa**, porque remove a dependência do pacote `MudBlazor` (projetos Web e de testes) e do Bootstrap ao final da migração.
- **Boa**, porque não há Node/npm: um binário fixo, licença MIT.
- **Ruim**, porque componentes hoje prontos (botões, cards, chips, alertas, progress) precisam ser reescritos como primitivos próprios, e a acessibilidade (foco, contraste, aria) passa a ser responsabilidade do projeto.
- **Ruim**, porque o build passa a depender de um binário nativo de **80–112 MB** que não deve ser versionado: é baixado por script, com versão pinada e verificação de `sha256`, e precisa de cache no CI.
- **Ruim**, porque durante a migração coexistem dois sistemas de estilo; o *preflight* (reset CSS) do Tailwind só é ativado quando o MudBlazor sair (SPEC-0034).
- **Ruim**, porque os testes bUnit que verificam classes `mud-*` precisam ser reescritos para os novos primitivos.

### Confirmação (G3)
`tests/TicTacToe.Tests/TailwindDesignSystemTests.cs` (criado na SPEC-0029 e endurecido na SPEC-0034):
- a versão do Tailwind está pinada em `tools/tailwind/version.txt` e o script de instalação verifica `sha256`;
- o CSS de entrada define os tokens Cyber Arena em `@theme`;
- páginas e componentes não têm blocos `<style>` inline (estilo especial vai para CSS isolation);
- após a SPEC-0034: nenhum `PackageReference` a `MudBlazor` nos `.csproj` e nenhum `mud-`/`Mud*` em `.razor`.

## Prós e Contras das Opções
| Critério | A. Tailwind standalone | B. MudBlazor + camada própria | C. CSS puro + tokens |
|---|---|---|---|
| Fidelidade ao HTML do Stitch | Alta (tradução quase 1:1) | Média (CSS extra contra o Mud) | Média (tudo escrito à mão) |
| Toolchain | Binário externo, sem Node | Nenhuma nova | Nenhuma nova |
| Custo de migração | Alto (reescreve componentes e testes) | Baixo | Alto |
| Componentes prontos e a11y | Nenhum (próprios) | Muitos (Dialog, Table, Snackbar) | Nenhum (próprios) |
| Consistência de tokens | Alta (`@theme`) | Média (Palette do Mud + variáveis extras) | Alta (variáveis CSS) |
| Risco de build/CI | Médio (download de binário) | Baixo | Baixo |

Sem pontuação numérica: a escolha reflete a prioridade declarada pelo responsável (fidelidade e controle), não um cálculo. A opção B era a de menor custo e risco e foi recomendada pelo Architect; foi recusada de forma consciente.

## Mais Informações
- Tailwind CSS `v4.3.3`, publicada em 2026-07-16, binários oficiais `tailwindcss-{macos-arm64,macos-x64,linux-x64,linux-arm64,windows-x64.exe}` e `sha256sums.txt` no release — `gh api repos/tailwindlabs/tailwindcss/releases/latest`, verificado em 2026-09-29.
- Licença MIT (Tailwind CSS e MudBlazor) — verificado em 2026-09-29.
- **Não verificado:** suporte do CLI standalone a plugins de terceiros. Não é requisito desta decisão (o projeto só usa o núcleo do Tailwind).
- Substitui o ADR-0007 para a camada de UI. O ADR-0007 só passa a `superseded` (com `superseded_by: ADR-0008`) quando a SPEC-0034 remover o MudBlazor; até lá ambos coexistem.
- Specs: SPEC-0028 (épico), SPEC-0029 (fundação) e SPEC-0034 (remoção do MudBlazor).
