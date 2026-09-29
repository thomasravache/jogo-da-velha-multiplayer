---
id: SPEC-0032
title: Histórico de partidas Cyber Arena
tier: full
type: feature
user_facing: true
status: in-progress
created: 2026-09-29
parent: SPEC-0028
depends_on: [SPEC-0043]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Web/Components/Pages/History.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/History.razor.css, src/TicTacToe/TicTacToe.Web/Components/Ui/HistoryDateFormatter.cs, tests/TicTacToe.Tests/HistoryCyberArenaTests.cs]
adrs: [ADR-0008]
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0032 — Histórico de partidas Cyber Arena

## 1. Visão Geral
Redesenha a página `/history` no visual Cyber Arena com o dado que existe hoje: as **10 partidas mais recentes** com jogadores (✕ e ◯), resultado (vitória de quem, ou empate) e data. A tabela vira uma lista de linhas em vidro no desktop e cartões empilhados no mobile, com estados de carregamento e vazio, e um botão **Novo duelo**. Toda a estilização inline da página (`<style>`) sai.

## 2. Motivação & Escopo
**Motivação:** a página usa um bloco `<style>` inline com paleta antiga (`#e74c3c`, `#3498db`) e tabela crua; não acompanha o novo shell nem o tema.

**Objetivos (dentro do escopo):**
- Referências: `docs/design/stitch/historico-desktop.html` (linhas em 12 colunas: resultado, duelo, data) e `historico-mobile.html` (cartões).
- Cabeçalho (`PageHeader`): eyebrow "Partidas", título "Histórico de Partidas", subtítulo "Últimas {n} partidas"; ação **Novo duelo** (link para `/`).
- Uma linha por partida: chip de resultado (**Vitória de {nome}** com destaque de ouro, ou **Deu velha**), duelo `✕ {X} vs ◯ {O}` com o vencedor destacado, e data.
- Data legível: "Hoje, 19:42", "Ontem, 22:10" ou `dd/MM HH:mm`, no fuso local (`HistoryDateFormatter`).
- Estados: carregando e vazio ("Nenhuma partida registrada ainda" + botão **Jogar agora**).
- `<PageTitle>` "Histórico de Partidas · XO Arena".
- Remoção do `<style>` inline; estilo por utilitários e, se necessário, `History.razor.css`.

**Não-objetivos (fora do escopo):**
- Elementos do Stitch sem backend, **omitidos**: "VOCÊ" e resultado do ponto de vista do jogador (Vitória/Derrota), ELO por jogador, duração, lance decisivo, motivo de fim (W.O., desconexão), filtros (Todas/Vitórias/Derrotas/Empates/Por W.O.), busca, ordenação, "Sincronizado há", "Logs EF", lance destaque (mini tabuleiro), resumo de desempenho, "Fila ranqueada". Destino: SPEC-0036 (dados), SPEC-0037 (identidade), SPEC-0038 (filtros, resumo) e matriz da SPEC-0035.
- Paginar (a consulta continua `GetRecentAsync(10)`).
- Mudanças no `GameResultService` ou no banco.

## 3. Dependências
- **Implementações necessárias:** SPEC-0043 — `PageHeader`, `NeonCard`, `PillButton`, `StatusChip`, `Icon`.
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** `Components/Pages/History.razor` (`@rendermode InteractiveServer`, injeta `GameResultService`, lista `MatchResult` com `PlayerXName`, `PlayerOName`, `WinnerName`, `PlayedAt`); padrão de páginas do projeto.

**Decisão:** mesma página e mesma consulta; a lógica de rótulo de data vira uma função pura `HistoryDateFormatter.Format(DateTime playedAtUtc, DateTime nowLocal)` para testes sem relógio real.

**Justificativa:** mantém o comportamento e isola o único cálculo novo.

**Desvio do padrão existente:** substitui o bloco `<style>` e a tabela crua por primitivos Cyber Arena (ADR-0008).

**Alternativas descartadas:** injetar `TimeProvider` (exigiria registro em `Program.cs`, fora do escopo); manter a tabela HTML pura no mobile (ilegível em 390px).

**ADRs:** ADR-0008.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** mesma consulta de 10 linhas; sem chamadas novas.
- **Segurança:** nomes de jogador são texto livre e escapados pelo Blazor; nenhuma marcação HTML dos nomes é interpretada.
- **Privacidade e dados pessoais:** N/A — mesmos dados já exibidos.
- **Disponibilidade e resiliência:** erro ao carregar não deve derrubar a página (a consulta atual não trata exceção; o comportamento não muda aqui e fica registrado como pendência se ocorrer).
- **Acessibilidade (UI):** semântica de tabela (`th scope`) no desktop; no mobile a leitura por cartões mantém a ordem do conteúdo; resultado nunca só por cor (texto + ícone); contraste AA; Lighthouse ≥ 90.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** rota `/history`.

```text
Dados (inalterados): MatchResult { PlayerXName, PlayerOName, WinnerName?, PlayedAt(UTC) }
Consulta (inalterada): GameResultService.GetRecentAsync(10)

HistoryDateFormatter.Format(DateTime playedAtUtc, DateTime nowLocal) : string
  mesmo dia local    → "Hoje, HH:mm"
  dia local anterior → "Ontem, HH:mm"
  demais             → "dd/MM HH:mm"

Linha
  chip:   WinnerName != null → "Vitória de {WinnerName}" (ouro)   WinnerName == null → "Deu velha" (neutro)
  duelo:  "✕ {PlayerXName}" vs "◯ {PlayerOName}"; o vencedor em destaque (ícone de troféu);
          se PlayerXName == PlayerOName e há vencedor, nenhum lado é destacado (não há como distinguir)
Estados: carregando · vazio ("Nenhuma partida registrada ainda." + "Jogar agora")
```

**Arquivos/módulos afetados:** ver `touches`.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Data | Hoje / ontem / antes | "Hoje, HH:mm" / "Ontem, HH:mm" / `dd/MM HH:mm` no fuso local | UT-01 |
| Vitória | `WinnerName` = X ou O | Chip "Vitória de {nome}"; vencedor destacado no duelo | UT-02 |
| Empate | `WinnerName` nulo | Chip "Deu velha"; nenhum lado destacado | UT-02 |
| Homônimos | X e O com o mesmo nome e vencedor | Chip com o nome; nenhum lado destacado | UT-03 |
| Carregando | Consulta pendente | Indicador de carregamento acessível | UT-04 |
| Vazio | Nenhuma partida | Mensagem e botão "Jogar agora" para `/` | UT-04 |
| Cabeçalho | Com N partidas | "Últimas N partidas" e botão "Novo duelo" para `/` | UT-05 |
| Sem legado | Página em qualquer estado | Sem `<style>` inline e sem `mud-` | UT-06 |
| Dados reais | 11 partidas no banco | Só as 10 mais recentes, mais recente primeiro | IT-01 |
| Jornada | Abrir `/history` com dados | Linhas, chips, datas e ação "Novo duelo" | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado `GameResultService.GetRecentAsync(10)` com 11 partidas, então retorna 10 em ordem decrescente de `PlayedAt` (a ordenação já é coberta pelos testes da SPEC-0010; este teste fixa o contrato consumido pela página e passa antes da mudança).

**Testes existentes afetados:** nenhum (a página não tinha testes de UI; `MatchHistoryTests` cobre só o serviço).

### 7.2 Testes Unitários
- **UT-01** — Dado `nowLocal` fixo e `PlayedAt` no mesmo dia, no dia anterior e há 3 dias, então `Format` retorna "Hoje, 19:42", "Ontem, 22:10" e "26/09 18:30", incluindo a virada de dia em fuso local.
- **UT-02** — Dado uma partida com vencedor X e outra empatada, então a primeira exibe "Vitória de {nome}" com o vencedor destacado e a segunda exibe "Deu velha" sem destaque.
- **UT-03** — Dado `PlayerXName == PlayerOName` com vencedor, então o chip cita o nome e nenhum lado recebe o destaque.
- **UT-04** — Dado a página carregando e depois sem dados, então há um indicador acessível de carregamento e, no vazio, a mensagem e o link "Jogar agora" para `/`.
- **UT-05** — Dado N partidas, então o subtítulo diz "Últimas N partidas" e há o link "Novo duelo" para `/`.
- **UT-06** — Dado `History.razor` em qualquer estado, então o markup não contém `mud-`, o arquivo não contém a tag `<style>` e declara `<PageTitle>` com "Histórico de Partidas · XO Arena".

### 7.3 Testes de Integração
- **IT-01** — Dado um `GameplayDbContext` InMemory com 11 partidas, quando a página é renderizada, então exibe exatamente 10 linhas, a mais recente primeiro.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs.

### 7.5 Testes E2E
- **E2E-01** — Jornada do histórico (bUnit): página com partidas semeadas mostra uma linha por partida com chip, duelo e data, e a ação "Novo duelo".

### 7.6 Outros
- Revisão visual (H2): 390px e 1280px contra `historico-mobile.png`/`historico-desktop.png`; lista de omitidos conferida.
- Lighthouse Acessibilidade ≥ 90 em `/history`.

**Dublês e dados de teste:** `GameplayDbContext` EF InMemory (padrão de `MatchHistoryTests`), relógio passado como parâmetro.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto.
- **Dados/schema:** N/A.
- **Compatibilidade:** rota e consulta inalteradas.
- **Observabilidade:** revisão visual e console sem erros.
- **Rollback:** `git revert` do PR.
- **Etapas de migração/coexistência:** etapa 3 de 4 (tela 3 de 4).

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [ ] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [ ] Escrever os testes `SPEC-0032:CH-01`, `SPEC-0032:UT-01`, `SPEC-0032:UT-02`, `SPEC-0032:UT-03`, `SPEC-0032:UT-04`, `SPEC-0032:UT-05`, `SPEC-0032:UT-06`, `SPEC-0032:IT-01`, `SPEC-0032:E2E-01` com a tag `SPEC-0032:<ID>`, em commits `test(...)` com `Refs: SPEC-0032`, tocando só `test_paths`
- [ ] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [ ] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0032`)

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [ ] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [ ] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [ ] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [ ] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável
- [ ] PR com `spec_graph.py pr SPEC-0032`, CI verde (G5) e aprovação do merge (H2)
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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0032`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
