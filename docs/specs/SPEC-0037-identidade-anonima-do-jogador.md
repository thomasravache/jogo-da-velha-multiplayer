---
id: SPEC-0037
title: Identidade anônima do jogador
tier: full
type: feature
user_facing: true
status: in-progress
created: 2026-09-29
parent: SPEC-0035
depends_on: [SPEC-0036]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/MatchResult.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameSession.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameResultService.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/GameplayDbContext.cs, src/TicTacToe/TicTacToe.Modules.Gameplay/Migrations/**, src/TicTacToe/TicTacToe.Modules.Matchmaking/MatchmakingService.cs, src/TicTacToe/TicTacToe.Web/Services/PlayerIdentity/**, src/TicTacToe/TicTacToe.Web/Program.cs, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor.cs, src/TicTacToe/TicTacToe.Web/Components/Game/Lobby.razor, tests/TicTacToe.Tests/PlayerIdentityTests.cs, tests/TicTacToe.Tests/PlayerIdentityPersistenceTests.cs, tests/TicTacToe.Tests/SingleResultRecordingTests.cs, tests/TicTacToe.Tests/ArenaCyberArenaTests.cs, tests/TicTacToe.Tests/LobbyCyberArenaTests.cs]
adrs: [ADR-0009]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0037 — Identidade anônima do jogador

## 1. Visão Geral
Dá ao jogador uma **identidade anônima persistente**: um `PlayerId` (GUID) criado no primeiro acesso e guardado no `localStorage` do navegador, junto com o último apelido. O lobby passa a lembrar o apelido ("Bem-vindo de volta, Thomas") e as partidas gravadas ficam ligadas ao `PlayerId` de cada lado. É o alicerce do "VOCÊ" no Histórico e no Ranking (SPEC-0038 e SPEC-0039). Não há login nem senha (ADR-0009).

## 2. Motivação & Escopo
**Motivação:** hoje o jogador é só um apelido livre; dois jogadores com o mesmo nome se fundem e o app não sabe qual linha é "sua". O Stitch mostra "VOCÊ", "Bem-vindo de volta" e "Identificador de duelo salvo em cache local".

**Objetivos (dentro do escopo):**
- `PlayerIdentityService` (Scoped) sobre uma abstração de armazenamento (`IPlayerStorage`, implementação por `localStorage` via JS interop): cria/recupera o `PlayerId` e guarda/recupera o apelido.
- `Home` carrega a identidade após a primeira renderização interativa, pré-preenche o apelido e o salva ao iniciar qualquer modo de jogo.
- Lobby: título "Bem-vindo de volta, {apelido}" quando houver apelido salvo e a nota "Seu identificador de jogador fica salvo neste navegador."
- Matchmaking e `GameSession` carregam o `PlayerId` de cada jogador (o robô não tem `PlayerId`).
- `MatchResult.PlayerXId` e `PlayerOId` (anuláveis) + índices; migration **aditiva** (`AddPlayerIdentity`).

**Não-objetivos (fora do escopo):**
- Login, contas, recuperação de identidade entre dispositivos ou navegadores.
- Exibir "VOCÊ", histórico pessoal ou posição no ranking (SPEC-0038 e SPEC-0039).
- Usar o `PlayerId` como credencial ou para qualquer decisão de autorização (ADR-0009).
- Mostrar o `PlayerId` na interface ou expô-lo a outros jogadores.
- Vincular partidas antigas (sem `PlayerId`) a um jogador.

## 3. Dependências
- **Implementações necessárias:** SPEC-0036 — mesma tabela `MatchResult`, mesmos arquivos de domínio e migrations em sequência (evita conflito de migration).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** apelido em `Home.razor.cs` (`PlayerName`), `MatchmakingService` que guarda nomes por `connectionId`, `GameSession.SetPlayerName`, `MatchResult` (SPEC-0010, SPEC-0036), migrations EF (SPEC-0025). ADR-0009 define a identidade anônima.

**Decisão:** serviço de identidade na camada Web (`Services/PlayerIdentity`), acesso ao navegador atrás de `IPlayerStorage` (testável sem navegador), e o `PlayerId` viaja por parâmetros opcionais em `MatchmakingService` e por `GameSession.SetPlayerId`. Os módulos `Gameplay` e `Matchmaking` continuam sem referência entre si (ADR-0004).

**Justificativa:** mantém o fluxo atual do lobby, isola a dependência do navegador e evita acoplar os módulos.

**Desvio do padrão existente:** introduz a pasta `Services` no projeto Web (não havia serviços de aplicação na camada de UI); coberto pelo ADR-0009.

**Alternativas descartadas:** `ProtectedLocalStorage` (depende de chaves do Data Protection persistidas, ver ADR-0009); cookie do servidor (exige middleware e política de cookies sem ganho); autenticação real (fora do escopo).

**ADRs:** ADR-0009.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** uma leitura do `localStorage` por circuito, na primeira renderização; sem consulta ao banco; índices em `PlayerXId` e `PlayerOId` para as consultas das SPEC-0038/0039.
- **Segurança:** o `PlayerId` é pseudônimo e **não é credencial**: nenhuma ação o aceita como autorização; valor armazenado inválido (não-GUID, JSON corrompido) é descartado e substituído sem exceção; o apelido é limitado a 20 caracteres e escapado pelo Blazor. Verificado por `UT-02`, `UT-03` e `UT-06`.
- **Privacidade e dados pessoais:** LGPD — o `PlayerId` associado a um apelido é dado pseudonimizado. Base e finalidade: identificar a mesma pessoa entre partidas para histórico e ranking; o jogador é informado no lobby ("salvo neste navegador"); o servidor guarda o `PlayerId` apenas em `MatchResult`; retenção e direito de esquecimento: ver Questões em Aberto. Logs não registram o `PlayerId` junto do apelido.
- **Disponibilidade e resiliência:** sem `localStorage` (bloqueado/privado) o app funciona: gera um `PlayerId` de sessão e não persiste; falha de JS interop nunca impede jogar.
- **Acessibilidade (UI):** a nota de privacidade tem contraste AA e é lida por leitores de tela; nenhum elemento novo interativo.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `PlayerIdentityService`, `IPlayerStorage`, parâmetros de `MatchmakingService`, `GameSession` e `MatchResult`.

```text
IPlayerStorage
  ValueTask<string?> GetAsync(string key)        ValueTask SetAsync(string key, string value)
  // implementação: localStorage via JS interop; chave única "xo.player" com JSON { "id": "<guid>", "nick": "<texto>" }

PlayerIdentityService (Scoped)
  ValueTask<PlayerProfile> LoadAsync()           // cria PlayerId se ausente/ inválido; nunca lança
  ValueTask SaveNicknameAsync(string nickname)   // trim, máx. 20; vazio é ignorado
  record PlayerProfile(Guid PlayerId, string? Nickname)

MatchmakingService (parâmetros opcionais, compatíveis)
  JoinQueue(connectionId, playerName = "", Guid? playerId = null)
  CreatePrivateRoom(connectionId, playerName, Guid? playerId = null)
  JoinPrivateRoom(code, connectionId, playerName, Guid? playerId = null)
  (Guid? X, Guid? O)? GetMatchPlayerIds(Guid matchId)

GameSession   SetPlayerId(Player, Guid?)   Guid? GetPlayerId(Player)

MatchResult   Guid? PlayerXId · Guid? PlayerOId      // + índices; migration AddPlayerIdentity (2 colunas anuláveis)

Lobby         [Parameter] string? ReturningPlayerName   // "Bem-vindo de volta, {nome}" quando preenchido
```

**Arquivos/módulos afetados:** ver `touches`. O `Lobby` ganha `ReturningPlayerName` e a nota de privacidade (única alteração de API do componente na fase 2).

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Primeira visita | Armazenamento vazio | GUID criado e gravado; sem apelido | UT-01 |
| Visita seguinte | Armazenamento com identidade | Mesmo `PlayerId` e apelido devolvidos | UT-01 |
| Valor inválido | JSON corrompido ou id não-GUID | Novo `PlayerId` gerado, sem exceção | UT-02 |
| Salvar apelido | "  Thomas  " / vazio / 30 caracteres | Trim e limite de 20; vazio ignorado | UT-03 |
| Pareamento | Fila, sala privada criar/entrar, com e sem `playerId` | `GetMatchPlayerIds` devolve X e O corretos; nulos sem ID | UT-04 |
| Sessão | `SetPlayerId` por lado | `GetPlayerId` devolve o valor; robô sem ID | UT-05 |
| Sem exposição | Lobby/telas com um `PlayerId` conhecido | O GUID não aparece em nenhum markup | UT-06 |
| Gravação | Partida online e partida solo | `PlayerXId/OId` gravados; solo com O nulo; migration só adiciona colunas anuláveis e índices | IT-01 |
| Lobby lembra o jogador | Armazenamento com apelido | Lobby pré-preenchido; ao jogar o apelido é salvo e os IDs vão ao pareamento e à sessão | IT-02 |
| Jornada de retorno | Visita com identidade salva | "Bem-vindo de volta, {nome}", nota de privacidade, partida solo gravada com o mesmo `PlayerId` | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- **CH-01** — Dado o pareamento atual (`JoinQueue`, `CreatePrivateRoom`, `JoinPrivateRoom`) sem `playerId`, então os nomes e o pareamento seguem iguais (guarda: passa antes da mudança; confirma que os parâmetros opcionais são compatíveis).

### 7.2 Testes Unitários
- **UT-01** — Dado `IPlayerStorage` em memória vazio, quando `LoadAsync` é chamado, então gera e grava um GUID; numa segunda chamada devolve o mesmo GUID e o apelido salvo.
- **UT-02** — Dado JSON corrompido, um `id` que não é GUID e um valor vazio, então `LoadAsync` gera um novo `PlayerId` sem lançar exceção.
- **UT-03** — Dado `SaveNicknameAsync` com "  Thomas  ", "" e um texto de 30 caracteres, então grava "Thomas", ignora o vazio e limita a 20 caracteres.
- **UT-04** — Dado `MatchmakingService`, quando dois jogadores se pareiam com e sem `playerId` (fila e sala privada), então `GetMatchPlayerIds` devolve o par correto na ordem X/O e nulos onde não houve ID.
- **UT-05** — Dado `GameSession`, então `SetPlayerId(X, id)` e `GetPlayerId(X)` são consistentes e `GetPlayerId(O)` é nulo quando não definido.
- **UT-06** — Dado o `Lobby` e os componentes de partida renderizados com um `PlayerId` conhecido, então o GUID não aparece em nenhum markup.

### 7.3 Testes de Integração
- **IT-01** — Dado `GameplayDbContext` InMemory e a migration `AddPlayerIdentity`, quando `SaveResultAsync` grava uma partida online e uma solo, então `PlayerXId`/`PlayerOId` refletem a sessão (O nulo no solo); a migration só contém `AddColumn` anulável e `CreateIndex`.
- **IT-02** — Dado `Home` no bUnit com `IPlayerStorage` em memória contendo `{ id, nick: "Thomas" }`, quando renderiza, então o apelido chega pré-preenchido ao `Lobby`; ao iniciar o solo, o apelido é salvo e o `PlayerId` é atribuído ao jogador X da sessão.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (as SPEC-0038/0039 consomem os campos persistidos, cobertos por IT-01).

### 7.5 Testes E2E
- **E2E-01** — Jornada de retorno (bUnit): com identidade salva, o lobby mostra "Bem-vindo de volta, Thomas" e a nota de privacidade; iniciar o solo grava um `MatchResult` com o `PlayerId` de Thomas.

### 7.6 Outros
- Verificação manual no navegador: o `localStorage` guarda `xo.player`; limpar os dados do site gera uma nova identidade; modo privado não quebra o app.
- SQL da migration conferido (`ADD` e `CREATE INDEX` apenas).

**Dublês e dados de teste:** `IPlayerStorage` em memória, `BunitContext` com JSInterop, EF InMemory.

**Ambiente de execução:** xUnit + bUnit local e no `build-and-test`.

## 8. Plano de Rollout
- **Estratégia:** deploy direto; a migration roda no startup.
- **Dados/schema:** expand only — duas colunas anuláveis e dois índices; partidas antigas seguem sem `PlayerId`.
- **Compatibilidade:** parâmetros novos opcionais; fluxo do lobby inalterado para quem não tem identidade.
- **Observabilidade:** log de aviso quando o armazenamento do navegador falha (sem `PlayerId`).
- **Rollback:** `git revert`; colunas e índices ficam sem uso.
- **Etapas de migração/coexistência:** N/A.

## 9. Questões em Aberto
- [x] Confirmar a **Opção A do ADR-0009** (identidade anônima no navegador, sem login) como base do "VOCÊ". — Opção A confirmada (identidade anônima no navegador, sem login). (thomas, 2026-09-29)
- [x] Privacidade: além da nota "salvo neste navegador", o produto precisa oferecer uma ação **"Esquecer este dispositivo"** (apaga o identificador local; partidas já gravadas permanecem, sem vínculo com você) e/ou definir um **prazo de retenção** das partidas? Recomendado: nesta fase só a nota, e registrar a política de retenção como decisão de produto futura. — Nesta fase só a nota de privacidade; ação de esquecer e prazo de retenção ficam como decisão de produto futura. (thomas, 2026-09-29)

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->

**Fase 0: Caracterização**
- [ ] Escrever CH-01 e confirmar que passam no código atual, em commit `test(...)` próprio

**Fase 1: Testes (Red)**
- [ ] Escrever os testes `SPEC-0037:CH-01`, `SPEC-0037:UT-01`, `SPEC-0037:UT-02`, `SPEC-0037:UT-03`, `SPEC-0037:UT-04`, `SPEC-0037:UT-05`, `SPEC-0037:UT-06`, `SPEC-0037:IT-01`, `SPEC-0037:IT-02`, `SPEC-0037:E2E-01` com a tag `SPEC-0037:<ID>`, em commits `test(...)` com `Refs: SPEC-0037`, tocando só `test_paths`
- [ ] Scaffolding de contrato (tipos e assinaturas sem lógica) em commit `chore(...)` separado, se necessário
- [ ] Confirmar que cada teste novo falha pelo motivo certo (`spec_graph.py verify SPEC-0037`)

**Fase 2: Implementação (Green)**
- [ ] Implementar o mínimo para passar, seguindo o padrão de referência e os ADRs, dentro de `touches`
- [ ] Confirmar todos os testes verdes e a suíte completa (`dotnet build`, `dotnet test`)

**Fase 3: Refactor & Qualidade**
- [ ] Refatorar mantendo tudo verde; `dotnet format --verify-no-changes`
- [ ] Registrar evidências G1–G4 (Red, Green, arquitetura, review independente)

**Fase final: Integração, entrega e documentação**
- [ ] Revisão visual/acessibilidade do plano (seção 7.6), quando aplicável
- [ ] PR com `spec_graph.py pr SPEC-0037`, CI verde (G5) e aprovação do merge (H2)
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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0037`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
| 1 (contrato inalterado) | 2026-09-29 | `touches` inclui `SingleResultRecordingTests.cs`, `ArenaCyberArenaTests.cs` e `LobbyCyberArenaTests.cs` | A `Home` passa a injetar `PlayerIdentityService` (os contextos de teste da `Home` precisam registrá-lo) e o `Lobby` ganha o parâmetro `ReturningPlayerName`, que o guarda `SPEC-0030:CH-01` precisa listar | SPEC-0045, SPEC-0031, SPEC-0030 (testes) | thomas (autorização permanente, 2026-09-29) |
