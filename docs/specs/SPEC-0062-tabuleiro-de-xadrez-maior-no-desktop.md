---
id: SPEC-0062
title: Tabuleiro de xadrez maior no desktop
tier: lite
type: fix
user_facing: false
status: implemented
created: 2026-09-29
parent:
depends_on: []
consumes_contract: []
touches: [src/TicTacToe/TicTacToe.Web/Components/Chess/ChessBoard.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/ChessHome.razor, src/TicTacToe/TicTacToe.Web/Styles/cyber-arena.input.css, src/TicTacToe/TicTacToe.Web/wwwroot/css/cyber-arena.css, tests/TicTacToe.Tests/ChessBoardSizeTests.cs]
adrs: []
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0062 — Tabuleiro de xadrez maior no desktop

<!-- Tier lite: bug fix ou mudança pequena, um módulo, sem mudança de contrato público, tamanho S. type: fix | feature | refactor. user_facing: true se o defeito aparece numa jornada do usuário (o teste de regressão deveria ser E2E). Se qualquer condição falhar, use o tier full. Substitua todos os marcadores com chaves duplas. -->

## 1. Problema
Na arena de xadrez em desktop o tabuleiro fica pequeno (o usuário relata "muito pequeno" na tela dele), bem menor que no mock do Stitch (`docs/design/stitch/chess/arena-desktop.png`), onde o tabuleiro ocupa cerca de 40% da largura e é o elemento dominante. Esperado: tabuleiro grande, com as colunas laterais (jogador, lances) ao redor.

## 2. Causa Raiz
`ChessHome.razor` envolve a arena num quadro de `max-w-[var(--size-frame)]` (960 px, pensado para o lobby e a velha). A `ChessArena` usa três colunas em `lg` (`18rem | 1fr | 18rem`), então a coluna central sobra com ≈ 960 − 576 − 32 = **352 px**, e `ChessBoard` ainda limita a `--size-board-desktop` (480 px, valor do tabuleiro 3×3). O mock usa um quadro largo (~1208 px) e um tabuleiro de ~500 px em tela de 1280 px, escalando em telas maiores.

## 3. Mudança Proposta
Dois tokens novos em `cyber-arena.input.css`: `--size-frame-chess: 80rem` (1280 px) e `--size-board-chess: min(100%, 42rem, calc(100dvh - 14rem))` (até 672 px, sem passar da altura útil da janela). `ChessHome` usa `--size-frame-chess` só na visão da partida (o lobby mantém 960 px) e `ChessBoard` usa `--size-board-chess` no lugar de `--size-board-desktop`. Em telas menores que `lg` nada muda (o tabuleiro continua `w-full`). NÃO muda: colunas laterais, ordem, comportamento, jogo da velha, `--size-board-desktop` e `--size-frame`. CSS gerado regenerado.

**Padrão seguido:** tokens `--size-*` em `cyber-arena.input.css` e `GameBoard.razor`/`Home.razor` (uso de `max-w-[var(--size-…)]`).

**Rollback:** revert do commit (o CSS gerado volta junto).

**Outras ocorrências:** Nenhuma (o tabuleiro 3×3 já usa 480 px, como no mock da velha).

## 4. Plano de Testes (TDD)
<!-- Mínimo: um teste de regressão que reproduz o problema e FALHA antes da correção, no nível em que o defeito aparece (IT para fronteiras de I/O, E2E para jornadas de usuário). Código sem cobertura: primeiro CH-xx fixando o comportamento atual. Tag no código: `SPEC-0062:<ID>`. Categoria sem teste: "N/A — motivo" SEM ID. -->
- Caracterização: N/A — `ChessBoardTests`, `ChessArenaTests` e `ChessHomeTests` já cobrem o comportamento; o teste novo só fixa as classes de tamanho.
- **UT-01** — Dado o `ChessBoard` renderizado, quando o teste inspeciona o elemento raiz `[data-board]`, então ele limita a largura por `--size-board-chess` (e não mais por `--size-board-desktop`).
- **UT-02** — Dado o `ChessHome` com uma partida em andamento, quando renderiza a arena, então o quadro usa `--size-frame-chess`; e com o lobby visível o quadro continua em `--size-frame`.
- **UT-03** — Dado `cyber-arena.input.css`, quando o teste lê os tokens, então `--size-frame-chess` é `80rem` e `--size-board-chess` limita a 42rem e à altura da janela, sem alterar `--size-board-desktop` (480px) nem `--size-frame` (960px).
- Integração/E2E: N/A — ajuste puramente visual de tamanho; a conferência real é visual no navegador (pendência já registrada do épico do xadrez).

## 5. Questões em Aberto
<!-- Dúvidas que impedem fechar a spec, respondidas ANTES do H1. Aberta: `- [x] pergunta (quem responde)`. Respondida: `- [x] pergunta — resposta (quem, data)`. Com alguma aberta o G0 reprova. Sem dúvidas: escreva "Nenhuma". Dúvida que surge depois da aprovação vira impedimento (seção Registro de Impedimentos). -->
- [x] Qual tamanho o usuário espera? — "como no Stitch": tabuleiro dominante no desktop; adotado até 672 px limitado pela altura da janela (Architect, 2026-09-29; ajustável no H1)

## 6. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado".

## 7. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. -->

**Fase 1: Tokens e classes de tamanho**
- [x] Red: escrever UT-01, UT-02, UT-03 com a tag `SPEC-0062:<ID>` e confirmar que falham pelo motivo certo
- [x] Green: tokens em `cyber-arena.input.css`, `ChessBoard` e `ChessHome` usando-os; regenerar `cyber-arena.css`
- [x] Validar: build + suíte completa + `tools/tailwind/build.sh --check` (G2/G3)

**Fase final: Integração, entrega e documentação**
- [x] Review independente (G4)
- [x] Integração + CI verde (G5) e aprovação (H2)
- [x] Relatório de Entrega e CHANGELOG (G7)

## 8. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência. -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0062`: 0 erros, 0 avisos; causa raiz confirmada no código (frame 960px − colunas 2×18rem → ~352px) | 2026-09-29 |
| G1 Red | PASS | Red confirmado: UT-01/02/03 falharam pelo motivo certo (99bd738); verify PASS | 2026-09-29 |
| G2 Green | PASS | dotnet test: 913 total, 912 passam, 1 pulado, 0 falhas em 3 execuções; format e tailwind --check limpos | 2026-09-29 |
| G3 Arquitetura | N/A | sem novas regras estruturais | 2026-09-29 |
| G4 Review | PASS | Review do diff pelo Architect (que não implementou): 4 linhas de src + CSS gerado, dentro do touches e do contrato; sem achados | 2026-09-29 |
| G5 Integração & CI | PASS | PR #62: Build, Format & Test e sdd verdes; mesclado na `main` | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorização permanente do usuário (2026-09-29): mesclar com CI verde; H1 do usuário em chat ("aprovado, pode fazer tudo") | 2026-09-29 |
| G6 Deploy | N/A | Sem ambiente remoto; aprovado pelo usuário em 2026-09-29 | 2026-09-29 |
| G7 Pronto & Docs | PASS | `spec_graph.py validate` limpo; Relatório de Entrega e CHANGELOG atualizados | 2026-09-29 |

## 9. Registro de Impedimentos
<!-- Toda parada é registrada pelo Architect com `spec_graph.py impede` e fechada com `resolve` — não edite à mão. Tipos: spec (spec errada/incompleta → resolve com Emenda) | decisão (só o humano decide → resposta ou ADR) | trabalho (falta algo que exige código → SPEC-NNNN nova) | externo (acesso, ambiente, terceiro → ação tomada) | falha (3 FAILs seguidos no mesmo gate → diagnóstico e decisão). Com impedimento aberto a spec aparece como parada no INDEX e não pode ser fechada. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 10. Relatório de Entrega
<!-- Preenchido no CLOSE (G7). Para status implemented o validate exige tudo preenchido, todo teste do plano com PASS + evidência e a Definição de Pronto marcada. -->

### O que foi entregue
Tabuleiro de xadrez maior no desktop: quadro da partida de 80rem (`--size-frame-chess`) e tabuleiro de até 42rem (672 px), limitado pela altura da janela (`--size-board-chess`). Lobby, mobile, colunas laterais e jogo da velha inalterados.


### Como foi feito
Dois tokens novos em `cyber-arena.input.css`; `ChessBoard` usa `--size-board-chess` e `ChessHome` usa `--size-frame-chess` só na visão da partida; CSS gerado regenerado; UT-01/02/03 fixam classes e tokens. PR #62.


### Prova de Correção
<!-- type fix: teste de regressão falhou antes (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->
UT-01, UT-02 e UT-03 falharam no commit Red (99bd738) e passam no commit da correção (5f43d1b).


### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| SPEC-0062:UT-01 | `ChessBoard` limita a largura por `--size-board-chess` | PASS | `dotnet test` 912/912 (1 pulado) no CI do PR #62 |
| SPEC-0062:UT-02 | `ChessHome` usa quadro largo na partida e `--size-frame` no lobby | PASS | idem |
| SPEC-0062:UT-03 | tokens no `cyber-arena.input.css` | PASS | idem |


### Definição de Pronto
- [x] Teste de regressão falhou antes e passa depois da correção
- [x] Suíte completa, arquitetura e CI verdes (G2, G3, G5)
- [x] Review independente sem achados blocker/major (G4)
- [x] Padrão existente mantido
- [x] Disponível no ambiente-alvo via pipeline (G6)
- [x] Documentação/CHANGELOG atualizados quando aplicável (G7)
- [x] Outras ocorrências registradas como novas specs (ou nenhuma)

### Deploy
G6 N/A (aprovado pelo usuário em 2026-09-29): sem ambiente remoto (`staging_url` vazio). A entrega é o merge na `main` pelo PR #62 com CI verde.


### Pendências
Conferência visual no navegador continua pendente (tamanho real de 672 px e o teto por altura da janela); ajustar os tokens se o valor não agradar.


## 11. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda, aprovada pelo humano. -->
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
