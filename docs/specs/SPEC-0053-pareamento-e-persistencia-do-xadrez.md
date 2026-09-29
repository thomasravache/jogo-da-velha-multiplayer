---
id: SPEC-0053
title: Pareamento e persistência do xadrez
tier: full
type: feature
user_facing: false
status: proposed
created: 2026-09-29
parent: SPEC-0046
depends_on: [SPEC-0047, SPEC-0052, SPEC-0055, SPEC-0061]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/MatchResult.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameplayDbContext.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations/**, src/TicTacToe/TicTacToe.Modules.Matchmaking/MatchmakingService.cs, src/TicTacToe/TicTacToe.Modules.Chess/Session/ColorAssignment.cs, src/TicTacToe/TicTacToe.Web/Program.cs, src/TicTacToe/TicTacToe.Web/Services/Chess/**, tests/TicTacToe.Tests/ChessMatchmakingTests.cs, tests/TicTacToe.Tests/ChessPersistenceTests.cs, tests/TicTacToe.Tests/ChessResultRecorderTests.cs, tests/TicTacToe.Tests/ChessMatchRegistryTests.cs]
adrs: [ADR-0012]
external: []
size: M
approved_by:
approved_at:
---

# SPEC-0053 — Pareamento e persistência do xadrez

## 1. Visão Geral
Liga a sessão de xadrez ao resto do app: **pareamento** por controle de tempo com preferência de cor (fila e sala privada), **atribuição de cores**, e **persistência** da partida em `MatchResult` (controle de tempo, lances em SAN, FEN final) com gravação única por partida.

## 2. Motivação & Escopo
**Motivação:** Para haver histórico e ranking do xadrez, cada partida precisa ser pareada e gravada uma vez, com os dados que as telas do Stitch mostram (cor, controle, lances, motivo).

**Objetivos (dentro do escopo):**
- Chave de fila do xadrez `xadrez:{idDoControle}` (por exemplo `xadrez:blitz5+0`), usando o `queueKey` da SPEC-0047; sala privada herda o controle de quem cria.
- Preferência de cor (`Brancas`, `Pretas`, `Aleatória`) guardada no pareamento **antes** de entrar na fila ou na sala (o evento de pareamento dispara dentro de `JoinQueue`/`JoinPrivateRoom`); `ColorAssignment.AssignFirst` no módulo Chess: preferências compatíveis são respeitadas, iguais ou ambas aleatórias sorteiam. A preferência **não** influencia o pareamento (quem escolheu a mesma cor pode se parear e o sorteio decide).
- `MatchResult` ganha `TimeControl`, `MovesSan` (texto sem limite, `nvarchar(max)`), `FinalFen` (anuláveis) e `EndReason` ganha os motivos de xadrez; migration aditiva `AddChessInfo`.
- `GameResultService.SaveChessAsync(ChessMatchRecord)` e o `ChessResultRecorder` na Web (mapeia `ChessSession` → registro, uma gravação por partida via `TryMarkResultRecorded`).
- Brancas gravadas como lado X e pretas como O (`WinnerSide`), `GameType = Chess`.
- `ChessMatchRegistry` (Web, singleton): cria a sessão da partida **uma única vez** por `matchId` (sorteio de cores dentro do `GetOrAdd`), guarda a associação `connectionId → assento` e permite descobrir o assento e a cor corrente de cada conexão; remove a sessão.
- Registro no DI: `ChessMatchRegistry` singleton e `ChessResultRecorder` scoped (a referência do módulo Chess ao projeto Web vem da SPEC-0055).

**Não-objetivos (fora do escopo):**
- Interface do lobby e da arena (SPEC-0056, 0057).
- Histórico e ranking de xadrez (SPEC-0059) — apenas dados gravados aqui.
- Motivo de abandono ou W.O. diferente dos existentes; ELO.

## 3. Dependências
- **Implementações necessárias:** SPEC-0047 (`GameType`, `queueKey`, `LeaveQueue`), SPEC-0052 e SPEC-0061 (`ChessSession` completa, com os motivos de abandono e desconexão) e SPEC-0055 (que já adiciona a referência do módulo Chess ao projeto Web).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** N/A

## 4. Decisão Arquitetural
**Contexto:** `GameResultService.SaveResultAsync` mapeia `GameSession` → `MatchResult` (SPEC-0036/0037/0040) e `SaveOnceAsync` garante gravação única (SPEC-0045); `MatchmakingService` guarda nomes e identidades por conexão (SPEC-0037). Referência: `MatchDetailsPersistenceTests`, `SingleResultRecordingTests`.

**Decisão:** `Gameplay` recebe só um registro simples (`ChessMatchRecord`) e não referencia o módulo Chess; o mapeamento `ChessSession` → registro vive na Web (`ChessResultRecorder`), única camada que conhece os dois módulos. Matchmaking guarda a preferência como texto opaco.

**Justificativa:** Preserva a independência dos módulos (ADR-0004, ADR-0012) e reaproveita a gravação e o histórico existentes.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Gameplay referenciar Chess (acopla módulos); tabela própria de partidas de xadrez (ADR-0012).

**ADRs:** ADR-0012

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Gravação de uma linha por partida; `MovesSan` cresce com a partida (sem limite), `FinalFen` até 100 caracteres.
- **Segurança:** O resultado é montado no servidor a partir da sessão; nenhum dado do cliente vira coluna sem validação; consulta parametrizada.
- **Privacidade e dados pessoais:** Mesmos nomes já exibidos hoje e `PlayerId` pseudônimo (ADR-0009); lances não identificam pessoas além da partida.
- **Disponibilidade e resiliência:** Falha de gravação é registrada em log e não interrompe a partida (como no jogo da velha); gravação única mesmo com dois circuitos observando.
- **Acessibilidade (UI):** N/A — sem interface.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `MatchmakingService (preferência) · Chess.ColorAssignment · MatchResult (colunas) · GameResultService.SaveChessAsync · Web.ChessResultRecorder`

```text
enum ColorPreference { Random, White, Black }        // no módulo Chess
static class ColorAssignment
  static PieceColor AssignFirst(ColorPreference first, ColorPreference second, Func<bool> coinFlip)
  // devolve a cor do PRIMEIRO jogador: (White, Black|Random) → White; (Black, White|Random) → Black; (Random, White) → Black; (Random, Black) → White;
  // (White, White), (Black, Black) e (Random, Random) → sorteio por coinFlip (true = White)

MatchmakingService (uso pelo xadrez; API da SPEC-0047)
  JoinQueue(..., queueKey: "xadrez:blitz5+0")      CreatePrivateRoom(..., queueKey: ...)
  void SetMatchPreference(string connectionId, string preference)   string? GetPreference(string connectionId)   // texto opaco

MatchResult   string? TimeControl  (nvarchar(16))   string? MovesSan (nvarchar(max))   string? FinalFen (nvarchar(100))
              MoveCount (já existente) guarda os MEIOS-LANCES; as telas mostram lances completos (⌈meios/2⌉)
EndReason     acrescenta Checkmate, Stalemate, Insufficient, FiftyMoves, Repetition            // <= 16 caracteres; armazenado como texto
Migration AddChessInfo: só AddColumn anulável (3 colunas). Sem DropColumn, AlterColumn nem DropTable.

record ChessMatchRecord(string WhiteName, string BlackName, Guid? WhiteId, Guid? BlackId, string? WinnerSide /*"X"=brancas|"O"=pretas|null; WinnerName sai do lado*/,
                        EndReason Reason, int MoveCount, int DurationSeconds, string TimeControl, string MovesSan, string FinalFen, GameMode Mode)
GameResultService.SaveChessAsync(ChessMatchRecord)     // grava GameType=Chess; falha → log, sem exceção
Web.ChessMatchRegistry (singleton)
  ChessMatch GetOrCreate(Guid matchId, Func<ChessMatch> factory)   // ChessMatch = Session + Seats(connectionId → assento 0|1); o factory roda uma vez (sorteio único de cores)
  bool TryGet(Guid matchId, out ChessMatch match)   int? SeatOf(Guid matchId, string connectionId)   bool Remove(Guid matchId)
  MyColor de qualquer circuito = session.ColorOf(SeatOf(matchId, connectionId)), relido a cada estado (as cores trocam na revanche)
Web.ChessResultRecorder.SaveOnceAsync(ChessSession)    // TryMarkResultRecorded + mapeamento + SaveChessAsync; devolve verdadeiro se gravou
Mapeamento de motivos: Checkmate→Checkmate, Stalemate→Stalemate, InsufficientMaterial→Insufficient, FiftyMoveRule→FiftyMoves,
   ThreefoldRepetition→Repetition, Timeout→Timeout, Resignation e Abandon→Abandon, Disconnect→Disconnect
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. N/A

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Pareamento por controle | Jogadores com `xadrez:blitz5+0` e `xadrez:bullet1+0` | Só se pareiam controles iguais; sala privada herda | UT-01 |
| Cores | Combinações de preferências | Regras do contrato; sorteio só nos empates de preferência | UT-02 |
| Preferência no matchmaking | Guardar e ler texto opaco por conexão | Devolvido ao montar a partida; ausente → nulo | UT-03 |
| Cor única entre circuitos | Dois circuitos criam a mesma partida com sorteio que devolveria valores diferentes | A sessão nasce uma vez; as duas conexões leem cores opostas | UT-05 |
| Mapeamento de motivos | Todos os motivos de `ChessEndReason` | Motivo de gravação conforme o contrato | UT-04 |
| Gravação de partida | Partida de xadrez encerrada | Uma linha com jogo, cores, controle, lances, FEN, motivo, duração | IT-01 |
| Empate e W.O. | Empate por afogamento, vitória por tempo, abandono, desconexão | `WinnerSide` e motivo corretos | IT-01 |
| Migration aditiva | Migration AddChessInfo | Só AddColumn anulável; linhas antigas legíveis | IT-02 |
| Gravação única | Dois circuitos chamam o recorder; falha simulada | Uma linha; falha não lança | IT-03 |
| Partida longa | 300 meios-lances | Lances gravados por inteiro | IT-04 |
| Isolamento | Jogo da velha após as mudanças | Fila, gravação e leituras do jogo da velha inalteradas | CH-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado o pareamento por `bestOf` e `SaveResultAsync` do jogo da velha, então o comportamento continua o mesmo (guarda: passa antes da mudança).

### 7.2 Testes Unitários
- **UT-01** — Dado o `MatchmakingService`, quando dois jogadores entram com `xadrez:blitz5+0`, um com `xadrez:bullet1+0` e outro com `velha:1`, então só os de controle igual se pareiam; uma sala privada criada com `xadrez:rapida10+5` entrega essa chave a quem entra.
- **UT-02** — Dado `ColorAssignment.AssignFirst` para todas as 9 combinações de preferência e um `coinFlip` controlado, então as regras do contrato valem e o sorteio só é consultado nos empates de preferência.
- **UT-03** — Dado `SetMatchPreference`/`GetPreference`, então o texto é devolvido por conexão e ausente devolve nulo.
- **UT-04** — Dado cada `ChessEndReason`, então o `EndReason` gravado segue o mapeamento do contrato e todo valor novo tem no máximo 16 caracteres.
- **UT-05** — Dado `ChessMatchRegistry.GetOrCreate` chamado por dois circuitos em paralelo com um `coinFlip` que devolve valores diferentes a cada chamada, então o factory roda uma só vez, as duas conexões recebem assentos e cores opostas e `SeatOf`/`ColorOf` devolvem sempre o mesmo par; após a revanche a cor de cada assento troca.

### 7.3 Testes de Integração
- **IT-01** — Dado `GameplayDbContext` InMemory, quando `ChessResultRecorder` grava uma vitória por mate, um empate por afogamento, uma vitória por tempo, um abandono e uma desconexão, então cada linha tem `GameType=Chess`, `TimeControl`, `MovesSan`, `FinalFen`, brancas como X e pretas como O, `WinnerSide`, motivo e duração corretos.
- **IT-02** — Dada a migration `AddChessInfo`, então `Up` só tem `AddColumn` anulável (3 colunas) e linhas antigas continuam legíveis por `GetRecentAsync` e `GetLeaderboardAsync`.
- **IT-03** — Dado dois chamadores concorrentes de `SaveOnceAsync` para a mesma partida, então há uma única linha; e dado um `GameResultService` de teste cujo `SaveChessAsync` lança (subclasse que sobrescreve o método), então o recorder registra o erro em log e não propaga exceção.
- **IT-04** — Dado uma partida de 300 meios-lances, então `MovesSan` é gravado inteiro (sem truncamento) e `MoveCount` = 300.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
N/A — `user_facing: false`; a jornada do usuário é coberta nas specs de interface.

### 7.6 Outros
- N/A

**Dublês e dados de teste:** EF InMemory, `ManualTime`, sessões reais do módulo Chess.

**Ambiente de execução:** xUnit (+ bUnit nas specs de interface) local e no `build-and-test` do CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto; sem interface ainda.
- **Dados/schema:** Expand: 3 colunas anuláveis e novos valores de texto em `EndReason`; sem migrate nem contract.
- **Compatibilidade:** Código antigo ignora as colunas novas; valores novos de `EndReason` só aparecem em partidas de xadrez (o histórico do jogo da velha filtra por jogo).
- **Observabilidade:** Log de informação na gravação de partida de xadrez (id da sessão, motivo) e de erro na falha.
- **Rollback:** Reverter o código; colunas extras e linhas de xadrez ficam inofensivas.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- - [x] Brancas são o lado X e pretas o O na gravação? — Sim, mantém o modelo de duas colunas de jogador (Architect, 2026-09-29)

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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0053`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
