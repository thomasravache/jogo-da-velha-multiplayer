---
id: SPEC-0047
title: Generalização multi-jogo (GameType, filtros e fila por chave)
tier: full
type: migration
user_facing: false
status: proposed
created: 2026-09-29
parent: SPEC-0046
depends_on: []
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/MatchResult.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/HistoryModels.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/LeaderboardModels.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameplayDbContext.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations/**, src/TicTacToe/TicTacToe.Modules.Matchmaking/MatchmakingService.cs, tests/TicTacToe.Tests/MultiGamePersistenceTests.cs, tests/TicTacToe.Tests/MultiGameMatchmakingTests.cs, tests/TicTacToe.Tests/HistoryAdvancedUiTests.cs]
adrs: [ADR-0012]
external: []
size: M
approved_by:
approved_at:
---

# SPEC-0047 — Generalização multi-jogo (GameType, filtros e fila por chave)

## 1. Visão Geral
Generaliza o modelo de partidas para mais de um jogo: `MatchResult` ganha `GameType` (jogo da velha por padrão), as consultas de histórico, resumo e ranking passam a filtrar por jogo e a fila do matchmaking aceita uma chave textual. Nenhuma tela muda e o jogo da velha se comporta exatamente como hoje; é o alicerce para o xadrez compartilhar identidade, histórico e ranking sem misturar resultados.

## 2. Motivação & Escopo
**Motivação:** Hoje todo `MatchResult` é jogo da velha por suposição. Sem um discriminador, as partidas de xadrez apareceriam no ranking e no histórico do jogo da velha, e a fila do matchmaking misturaria jogos e formatos.

**Objetivos (dentro do escopo):**
- `enum GameType { TicTacToe = 0, Chess = 1 }` e a coluna `MatchResult.GameType` (não nula, valor padrão 0) com índice, por migration aditiva `AddGameType`.
- `HistoryQuery`, `LeaderboardQuery` e `GetPlayerSummaryAsync` ganham o parâmetro opcional de jogo (padrão jogo da velha); `GetRecentAsync` e `GetLeaderboardAsync` passam a considerar só o jogo da velha.
- `SaveResultAsync` grava `GameType.TicTacToe` explicitamente.
- `MatchmakingService` aceita `queueKey` opcional em `JoinQueue` e `CreatePrivateRoom` e expõe `GetMatchQueueKey(matchId)`; sem chave, mantém o comportamento por `bestOf`.
- `JoinPrivateRoom` só entra em sala do jogo esperado (prefixo da chave; padrão `velha`), para que um código de sala de xadrez não abra partida de jogo da velha e vice-versa.
- `LeaveQueue(connectionId)` e `CancelPrivateRoom(connectionId)`: quem cancela a busca, sai da página ou fecha o circuito deixa de ser pareável (sem conexão fantasma na fila).

**Não-objetivos (fora do escopo):**
- Qualquer tela, rota ou componente (SPEC-0048 e as specs de xadrez).
- Colunas específicas do xadrez (controle de tempo, lances, FEN final): SPEC-0053.
- Regras do xadrez ou tipos do módulo Chess.

## 3. Dependências
- **Implementações necessárias:** N/A — só depende do código já entregue (SPEC-0036 a SPEC-0045).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `MatchResult` e `GameResultService` (SPEC-0036/0037/0038/0039); migrations aditivas por spec (AddMatchDetails, AddPlayerIdentity, AddSeriesInfo); `MatchmakingService` com filas por `bestOf` (SPEC-0040). Referência: `SeriesPersistenceTests` (migration aditiva) e `HistoryQueryTests` (consultas).

**Decisão:** Coluna `GameType` na tabela existente com padrão 0, parâmetro de jogo opcional nas consultas, chave textual de fila (ADR-0012).

**Justificativa:** É a menor mudança que separa os jogos sem migrar dados nem duplicar serviços; o padrão 0 mantém as linhas antigas como jogo da velha.

**Desvio do padrão existente:** Nenhum estrutural, mas é `type: migration` porque muda o modelo de dados compartilhado (ADR-0012 aprovado nesta spec).

**Alternativas descartadas:** Tabela separada por jogo (duplica serviços de histórico e ranking); JSON genérico por partida (consulta e índice ruins). Ver ADR-0012.

**ADRs:** ADR-0012

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Uma coluna inteira indexada; consultas de histórico e ranking mantêm os limites atuais (histórico p95 < 200 ms com 1.000 partidas, ranking < 500 ms com 10.000, smoke em EF InMemory).
- **Segurança:** O jogo vem de tipo enumerado, nunca de texto do usuário; a chave de fila é montada pelo servidor.
- **Privacidade e dados pessoais:** N/A — nenhum dado pessoal novo.
- **Disponibilidade e resiliência:** Migration aditiva com valor padrão: linhas antigas continuam válidas e a versão antiga do código continua lendo a tabela.
- **Acessibilidade (UI):** N/A — sem interface.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `MatchResult.GameType · HistoryQuery/LeaderboardQuery/GetPlayerSummaryAsync (parâmetro de jogo) · MatchmakingService.JoinQueue/CreatePrivateRoom/GetMatchQueueKey`

```text
enum GameType { TicTacToe = 0, Chess = 1 }

MatchResult            GameType GameType { get; set; } = GameType.TicTacToe     // coluna int NOT NULL DEFAULT 0 + índice IX_MatchResults_GameType
HistoryQuery           (..., int PageSize = 10, GameType Game = GameType.TicTacToe)   // parâmetro novo, no fim
LeaderboardQuery       (Guid? MyPlayerId, int Page, int PageSize = 10, GameType Game = GameType.TicTacToe)
GetPlayerSummaryAsync  (Guid playerId, GameType game = GameType.TicTacToe)   // virtual; os dublês de teste existentes (HistoryAdvancedUiTests) passam a sobrescrever a nova assinatura
GetRecentAsync / GetLeaderboardAsync    → só GameType.TicTacToe (comportamento legado preservado)
SaveResultAsync        → grava GameType.TicTacToe

MatchmakingService
  Guid? JoinQueue(string connectionId, string playerName = "", Guid? playerId = null, int bestOf = 1, string? queueKey = null)
  string CreatePrivateRoom(string connectionId, string playerName, Guid? playerId = null, int bestOf = 1, string? queueKey = null)
  Guid? JoinPrivateRoom(string roomCode, string connectionId, string playerName, Guid? playerId = null, string game = "velha")
       // entra só se a chave da sala começa com $"{game}:"; caso contrário devolve nulo (sala inválida)
  void LeaveQueue(string connectionId)          // remove a conexão de qualquer fila (idempotente); quem já foi pareado não é afetado
  void CancelPrivateRoom(string connectionId)   // remove salas criadas por essa conexão que ainda esperam
  string GetMatchQueueKey(Guid matchId)
  // chave efetiva = queueKey ?? $"velha:{bestOf}"; só se pareiam jogadores da mesma chave efetiva; GetMatchBestOf continua valendo

Migration AddGameType: AddColumn<int> GameType (nullable: false, defaultValue: 0) + CreateIndex IX_MatchResults_GameType. Sem DropColumn/AlterColumn/DropTable.
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Padrão legado | Linha sem GameType explícito (partida antiga ou SaveResultAsync) | Lida e gravada como jogo da velha | UT-01, IT-01 |
| Histórico por jogo | HistoryQuery com Game = Chess e com o padrão | Só as linhas do jogo pedido; padrão = jogo da velha; contagens e resumo idem | IT-02 |
| Ranking por jogo | LeaderboardQuery com Game = Chess e com o padrão | Só as partidas do jogo pedido entram na classificação | IT-02 |
| Consultas legadas | GetRecentAsync e GetLeaderboardAsync com partidas dos dois jogos | Devolvem só jogo da velha, mesma ordem de antes | CH-01, IT-02 |
| Fila por chave | Jogadores com chaves iguais e diferentes; sem chave | Só chaves iguais se pareiam; sem chave, comportamento por bestOf | UT-02 |
| Chave da partida | Partida pareada e sala privada com chave | GetMatchQueueKey devolve a chave efetiva | UT-02 |
| Sala do jogo errado | Código de sala de xadrez digitado no jogo da velha e vice-versa | Entrada recusada como sala inválida; a sala continua esperando | UT-03 |
| Cancelar busca | Conexão sai da fila ou da sala e outro jogador entra | Nunca pareia com a conexão que saiu; quem já pareou não é afetado | UT-03 |
| Migration aditiva | Migration AddGameType | Só AddColumn com valor padrão e CreateIndex; sem remoção | IT-03 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado partidas do jogo da velha e as consultas `GetRecentAsync(10)` e `GetLeaderboardAsync(10)`, então a ordem e o desempate seguem iguais (guarda: passa antes da mudança; a SPEC-0039 e a SPEC-0033 continuam verdes).

### 7.2 Testes Unitários
- **UT-01** — Dado um `MatchResult` novo, então `GameType` é `TicTacToe`, e `GameResultService.SaveResultAsync` grava `TicTacToe`.
- **UT-02** — Dado o `MatchmakingService`, quando dois jogadores entram com a mesma chave, então pareiam e `GetMatchQueueKey` devolve a chave; com chaves diferentes ficam esperando; sem chave o par se forma por `bestOf` e a chave efetiva é `velha:{bestOf}`; sala privada criada com chave entrega a chave a quem entra.
- **UT-03** — Dado uma sala criada com `xadrez:blitz5+0` e outra com o padrão, quando se tenta entrar na de xadrez como `velha` (e o inverso), então a entrada é recusada e a sala segue esperando; e dado `LeaveQueue` e `CancelPrivateRoom` de uma conexão que espera, quando outro jogador entra na mesma chave, então ele não pareia com a conexão que saiu.

### 7.3 Testes de Integração
- **IT-01** — Dado `GameplayDbContext` InMemory com linhas sem `GameType` explícito e com `Chess`, então as sem valor são lidas como `TicTacToe`, e a configuração do modelo declara o valor padrão 0 (`HasDefaultValue`).
- **IT-02** — Dado partidas dos dois jogos, então `GetHistoryAsync`, `GetPlayerSummaryAsync` e `GetLeaderboardPageAsync` filtram pelo jogo (padrão jogo da velha), e as contagens do histórico refletem só o jogo pedido.
- **IT-03** — Dada a migration `AddGameType`, então `Up` contém somente `AddColumn` com valor padrão 0 e `CreateIndex`, sem `DropColumn`, `AlterColumn` nem `DropTable`.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
N/A — `user_facing: false`; a jornada aparece nas specs de interface.

### 7.6 Outros
- Leitura do SQL da migration (`ADD` e `CREATE INDEX` apenas), conferida no G4.

**Dublês e dados de teste:** EF InMemory (`HistoryData`, `LeaderboardData` já existentes), `MatchmakingService` real.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; sem efeito visível.
- **Dados/schema:** Expand: coluna com valor padrão e índice. Sem migrate nem contract (não há remoção).
- **Compatibilidade:** Código antigo ignora a coluna nova; linhas antigas valem como jogo da velha.
- **Observabilidade:** Nenhuma nova; o log de erro existente cobre falhas de gravação.
- **Rollback:** Reverter o código; a coluna extra é inofensiva (Down remove índice e coluna se necessário).
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Partidas antigas viram jogo da velha sem migração de dados? — Sim, pelo valor padrão 0 (Architect, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate` limpo (0 erro, 0 aviso); checklist de julgamento do G0 feito pelo Architect | 2026-09-29 |
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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0047`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
