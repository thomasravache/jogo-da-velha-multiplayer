---
id: SPEC-0028
title: Redesign Cyber Arena — migração da UI para Tailwind (Fase 1 visual)
tier: epic
type: migration
status: proposed
created: 2026-09-29
depends_on: []
adrs: [ADR-0008]
external: []
approved_by:
approved_at:
---

# SPEC-0028 — Redesign Cyber Arena — migração da UI para Tailwind (Fase 1 visual) (Épico)

## 1. Visão
Levar o app inteiro (lobby, arena da partida, histórico e ranking) para a identidade **Cyber Arena** definida no projeto Stitch, trocando o MudBlazor por **Tailwind CSS standalone** com primitivos de UI próprios (ADR-0008). Ao final: layout responsivo (mobile com barra inferior, desktop com header), tokens únicos em um só lugar, nenhum resíduo de MudBlazor ou Bootstrap, e nenhuma regra de jogo alterada.

A fase 1 é **só visual**: cada tela mostra apenas o que o app tem hoje (dados e regras existentes). O que o Stitch desenha e o app não tem (ELO, ping, reações, filtros, série MD5 etc.) **não aparece** na Fase 1 e é coberto por specs da Fase 2 (SPEC-0035), com a matriz de cobertura completa naquele épico.

## 2. Escopo
**Objetivos (dentro do escopo):**
- Pipeline do Tailwind CSS standalone (versão pinada, binário verificado por checksum), tokens do `DESIGN.md` do Stitch e fontes Outfit e Space Grotesk auto-hospedadas (SPEC-0029).
- Shell responsivo (header/nav desktop, barra inferior mobile, modo imersivo da partida) e primitivos de UI reutilizáveis em `Components/Ui` (SPEC-0043).
- Redesign das quatro telas com os dados atuais: Lobby (SPEC-0030), Arena da partida (SPEC-0031), Histórico (SPEC-0032) e Ranking (SPEC-0033).
- Remoção do MudBlazor e do Bootstrap, limpeza de CSS/`<style>` legados e fechamento do ADR-0007 como superseded (SPEC-0034).

**Não-objetivos (fora do escopo):**
- Qualquer mudança em regras de jogo, matchmaking, banco de dados ou contratos de dados (exceto `GameSession.WinningLine`, propriedade derivada e pura, na SPEC-0031).
- Elementos do Stitch sem backend (ELO, divisões, temporada, ping, espectadores, reações, log de lances, série MD5, filtros e busca do histórico, colunas extras do ranking): cobertos pela SPEC-0035 ou registrados como backlog nela.
- Novo framework de testes de navegador (Playwright etc.); a verificação visual segue a estratégia descrita em Riscos.

## 3. Arquitetura Alvo
**Contexto:** Blazor Server interativo, módulos `Gameplay` e `Matchmaking` (ADR-0004), componentes decompostos com CSS isolation e testes bUnit (ADR-0006), UI hoje em MudBlazor (ADR-0007, SPEC-0026). Esta migração muda a **camada de UI** conforme o ADR-0008, sem tocar nos módulos de domínio.

```text
Styles/cyber-arena.input.css  ──(Tailwind CLI standalone)──▶  wwwroot/css/cyber-arena.css
   @theme (tokens Cyber Arena)                                   (gerado, versionado — ver SPEC-0029)
   @source Components/**

Components/
  Layout/MainLayout      header (desktop) · barra inferior (mobile) · modo imersivo (ShellState)
  Ui/                    Icon · NeonCard · PillButton · StatusChip · PageHeader · NeonInput · SegmentedControl
  Game/                  Lobby · GameBoard · Scoreboard/PlayerCard   (usam Ui/*)
  Pages/                 Home · History · Leaderboard                (usam Ui/*)
```

Coexistência durante a migração: MudBlazor e Tailwind convivem até a SPEC-0034. O Tailwind entra **sem preflight** (reset CSS) para não alterar as telas ainda em MudBlazor; o preflight é habilitado na SPEC-0034.

**Decisões (ADRs):** ADR-0008 — Tailwind CSS standalone como camada de estilo e design system próprio (supersede o ADR-0007 ao final da SPEC-0034).

**Regras de arquitetura a garantir (G3):**
- Versão do Tailwind pinada e instalação verificada por `sha256` — `TailwindDesignSystemTests` (SPEC-0029).
- Sem blocos `<style>` inline em páginas e componentes — `TailwindDesignSystemTests` (SPEC-0034 endurece).
- Após a SPEC-0034: sem `PackageReference` a MudBlazor e sem `mud-`/`Mud*` em `.razor` — `TailwindDesignSystemTests`.
- Módulos de domínio não referenciam projetos de UI (regra existente, ADR-0004).

## 4. Decomposição
| Spec | Título | Tier | Tipo | Tamanho | Depende de | Consome contrato de |
|---|---|---|---|---|---|---|
| SPEC-0029 | Pipeline Tailwind, tokens e fontes Cyber Arena | full | migration | M | — | — |
| SPEC-0043 | Shell e primitivos de UI Cyber Arena | full | feature | M | SPEC-0029 | — |
| SPEC-0030 | Lobby Cyber Arena | full | feature | M | SPEC-0043 | — |
| SPEC-0031 | Arena da partida Cyber Arena | full | feature | M | SPEC-0043 | — |
| SPEC-0032 | Histórico de partidas Cyber Arena | full | feature | S | SPEC-0043 | — |
| SPEC-0033 | Ranking Cyber Arena | full | feature | S | SPEC-0043 | — |
| SPEC-0034 | Remoção do MudBlazor e do Bootstrap | full | migration | M | SPEC-0030, SPEC-0031, SPEC-0032, SPEC-0033 | — |

Ordem esperada (`waves`): 0029 → 0043 → telas (0030, 0031, 0032, 0033; a paralelização real depende dos arquivos de teste compartilhados, ver `touches`) → 0034.

## 5. Estratégia de Entrega
- **Ambientes:** pipeline do repositório (`push` na `main`); `staging_url` e `production_url` ainda não existem em `sdd-config.yml`. G6 segue o precedente das specs anteriores (build e execução verificados, sem ambiente remoto) e fica registrado como tal.
- **Entrega por onda:** cada spec vai para a `main` por PR e deixa o app funcional: a `main` nunca fica com telas quebradas. Telas ainda não migradas continuam em MudBlazor.
- **Feature flags:** N/A — migração incremental por tela, sem flag.
- **Rollback:** `git revert` do PR da spec; como cada tela é independente, reverter uma não afeta as demais. O CSS gerado é versionado, então o rollback não depende do binário do Tailwind.
- **Métricas de sucesso pós-release:** suíte verde; console do navegador sem erros de recurso (fontes/CSS 404) nas 4 telas; revisão visual dos screenshots contra o Stitch aprovada no H2 de cada tela.

## 6. Riscos & Mitigações
- **Fidelidade visual não é verificável por bUnit** — cada spec de tela tem um checklist de revisão visual (viewport mobile 390px e desktop 1280px, comparado a `docs/design/stitch/*`), executado pelo humano no H2 com screenshots anexados ao PR. Testes automatizados cobrem estrutura, classes de token, acessibilidade e comportamento.
- **Binário do Tailwind de 80–112 MB no CI** — cache do binário por versão + checksum; o CSS gerado é versionado (SPEC-0029), então build e testes não dependem do binário; um job separado verifica *drift*.
- **Conflito de estilos MudBlazor × Tailwind durante a coexistência** — Tailwind sem preflight; tokens só via variáveis novas (`--color-*` do Tailwind); cada spec de tela verifica no navegador que as telas vizinhas não mudaram.
- **Testes acoplados a markup e CSS** (ex.: `Home.razor.css` contendo `.game-container`, textos como "Jogar Online") — cada spec de tela lista os testes existentes que altera e por quê; mudanças de teste depois do Red precisam de justificativa no G4.
- **Perda de acessibilidade ao sair do MudBlazor** — primitivos com foco visível, `aria-*`, contraste AA (par de cores validado por teste) e navegação por teclado; verificados por bUnit e checklist manual (Lighthouse/axe).
- **Dados de protótipo do Stitch tratados como requisito** — regra explícita de omissão (o que não tem backend não é renderizado) e matriz de cobertura na SPEC-0035.

## 7. Critérios de Aceite do Épico
- [ ] O pipeline gera o CSS Cyber Arena com todos os tokens a partir de um binário pinado e verificado — SPEC-0029:IT-01
- [ ] O shell exibe header/nav no desktop, barra inferior no mobile e modo imersivo na partida, com navegação acessível — SPEC-0043:E2E-01
- [ ] O Lobby permite jogar online, solo (com dificuldade) e criar/entrar em sala privada no visual Cyber Arena — SPEC-0030:E2E-01
- [ ] A Arena mostra jogadores, tabuleiro, timer, resultado (vitória, derrota, empate, W.O.) e revanche no visual Cyber Arena — SPEC-0031:E2E-01
- [ ] O Histórico lista as partidas recentes com resultado, jogadores e data no visual Cyber Arena — SPEC-0032:E2E-01
- [ ] O Ranking exibe pódio e tabela com o dado real do leaderboard no visual Cyber Arena — SPEC-0033:E2E-01
- [ ] Nenhuma referência a MudBlazor ou Bootstrap resta no código, nos pacotes ou nos testes — SPEC-0034:E2E-01

## 8. Questões em Aberto
Nenhuma neste nível; as decisões de cada spec estão nas suas próprias Questões em Aberto.

## 9. Aprovação (H1)
Uma aprovação humana cobre o épico e as specs filhas apresentadas junto com ele. Registrada no frontmatter (`approved_by`, `approved_at`) do épico e de cada filha.

## 10. Registro de Impedimentos
<!-- Problemas que envolvem várias filhas (conflito de integração da onda, CI quebrado, decisão transversal). Impedimento que trava uma filha específica vai na própria filha. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 11. Relatório de Entrega
<!-- Preenchido ao fechar o épico: o que foi entregue, como (ondas e deploys com versão/data), resultado dos critérios de aceite com os testes que os provam, métricas pós-release, pendências como novas specs. `spec_graph.py report SPEC-0028` ajuda a montar. -->

## 12. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda, aprovada pelo humano. -->
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
