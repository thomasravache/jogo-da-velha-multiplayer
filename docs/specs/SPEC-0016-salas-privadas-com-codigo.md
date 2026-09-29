---
id: SPEC-0016
title: Salas Privadas com Código
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0013
depends_on: [SPEC-0014]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Matchmaking/**, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor]
adrs: [ADR-0004]
external: []
size: M
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0016 — Salas Privadas com Código

## 1. Visão Geral
Permitir que os jogadores criem salas privadas protegidas por um código único (ex: `VELHA-9A2F`) e compartilhem com amigos para jogarem diretamente entre si, sem passar pela fila pública aleatória.

## 2. Motivação & Escopo
**Motivação:** No momento, o matchmaking apenas junta os dois primeiros jogadores que clicam em "Jogar Agora". Em um ambiente compartilhado, não há como garantir que dois amigos específicos caiam na mesma partida.

**Objetivos:**
- Adicionar no `MatchmakingService` os métodos `CreatePrivateRoom` e `JoinPrivateRoom`.
- Geração de código amigável de 4 a 6 caracteres alfanuméricos em caixa alta.
- Na interface `Home.razor`, disponibilizar as ações "Criar Sala Privada" e "Entrar com Código".
- Gerenciar concorrência e expiração/rejeição de códigos inválidos ou salas já cheias.

**Não-objetivos:**
- Salas com espectadores (apenas 2 jogadores).
- Senhas adicionais além do código.

## 3. Dependências
- **Implementações necessárias:** SPEC-0014 (Scoreboard e GameSession).
- **Contratos consumidos:** Nenhum.
- **Pré-requisitos externos:** Nenhum.

## 4. Decisão Arquitetural
**Contexto:** O gerenciamento de emparelhamento reside em `TicTacToe.Modules.Matchmaking`.

**Decisão:** Utilizar `ConcurrentDictionary<string, PrivateRoom>` em `MatchmakingService`. Quando o primeiro jogador cria a sala, o registro fica aguardando. Quando o segundo jogador submete o código, a partida (`MatchId`) é gerada e ambos são associados a ela.

**Desvio do padrão existente:** Nenhum.

## 5. Requisitos Não-Funcionais
- **Desempenho:** Validação de código e criação em O(1).
- **Segurança:** Códigos aleatórios e não sequenciais para evitar enumeração de salas.

## 6. Artefato A — Contrato

```text
namespace TicTacToe.Modules.Matchmaking;

public class MatchmakingService
{
    public string CreatePrivateRoom(string connectionId, string playerName);
    public Guid? JoinPrivateRoom(string roomCode, string connectionId, string playerName);
}
```

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Criação de sala | Jogador clica "Criar Sala" | Código único de sala gerado e jogador colocado em espera | UT-01 |
| Ingresso válido | Jogador informa código existente | MatchId gerado e jogadores associados | UT-02 |
| Código inexistente | Código incorreto digitado | Retorna nulo e mensagem de erro | UT-03 |
| Sala cheia | Terceiro jogador tenta entrar na mesma sala | Rejeição da entrada | IT-01 |
| Jornada na UI | Criação e entrada por código | Transição imediata para o tabuleiro em ambas as abas | E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — extensão de `MatchmakingService`.

### 7.2 Testes Unitários
- **UT-01** — Dado um jogador criando sala, `CreatePrivateRoom` retorna um código não vazio e armazena o criador.
- **UT-02** — Dado um código existente, quando o segundo jogador executa `JoinPrivateRoom`, retorna um `Guid` de partida válido.
- **UT-03** — Dado um código inexistente, `JoinPrivateRoom` retorna `null`.

### 7.3 Testes de Integração
- **IT-01** — Dado uma sala privada já ocupada por 2 jogadores, qualquer tentativa posterior de entrada com o mesmo código falha.

### 7.4 Testes de Contrato
- N/A.

### 7.5 Testes E2E
- **E2E-01** — Na interface web, o Criador gera a sala, o Convidado digita o código e o tabuleiro é iniciado para os dois.

### 7.6 Outros
- N/A.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto.
- **Rollback:** `git revert`.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Aguardando aprovação humana.

## 11. Checklist de Implementação
- [x] Testes unitários de sala privada em `tests/TicTacToe.Tests/` (Red)
- [x] Implementar `CreatePrivateRoom` e `JoinPrivateRoom` em `MatchmakingService.cs` (Green)
- [x] Integrar campos e botões no componente `Home.razor`
- [x] Suíte de testes completa

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0016` | 2026-09-29 |
| G1 Red | PASS | Falha CS1061 por ausência de CreatePrivateRoom | 2026-09-29 |
| G2 Green | PASS | 25/25 testes verdes | 2026-09-29 |
| G3 Arquitetura | PASS | Lógica de matchmaking isolada em Matchmaking | 2026-09-29 |
| G4 Review | PASS | Mudança aditiva | 2026-09-29 |
| G5 Integração & CI | PASS | `dotnet test` 25/25 | 2026-09-29 |
| H2 Integração aprovada | PASS | Aprovado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Executável local | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue | 2026-09-29 |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue
Criação e ingresso em salas privadas por código curto (`SALA-XXXX`) no `MatchmakingService` e interface correspondente no `Home.razor`.

### Como foi feito
- `MatchmakingService` gerencia salas privadas via `ConcurrentDictionary<string, string> _privateRooms`, unindo criador e convidado ao submeter o código correto.
- `Home.razor` exibe opção de gerar sala privada (com código em destaque e espera) ou digitar código existente.

### Prova de Correção
N/A — tipo feature.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Geração de código válido | PASS | PrivateRoomTests.CreatePrivateRoom_ShouldReturnValidCode |
| UT-02 | Ingresso com código une jogadores | PASS | PrivateRoomTests.JoinPrivateRoom_WithValidCode_ShouldPairPlayers |
| UT-03 | Código inexistente retorna null | PASS | PrivateRoomTests.JoinPrivateRoom_WithInvalidCode_ShouldReturnNull |
| IT-01 | Sala cheia rejeita 3º jogador | PASS | PrivateRoomTests.JoinPrivateRoom_WhenRoomAlreadyUsed_ShouldRejectThirdPlayer |
| E2E-01 | Criação e entrada por código na UI | PASS | Integrado em Home.razor |

### Definição de Pronto
- [x] Todos os testes do plano passando e listados na Verificação
- [x] Todo comportamento do Mapa de Comportamentos coberto e verificado
- [x] Suíte completa, arquitetura e CI verdes no resultado integrado (G5)
- [x] Review independente sem achados blocker/major (G4)
- [x] Padrão arquitetural existente mantido, ou desvio coberto por ADR aprovado
- [x] Requisitos não-funcionais medidos com evidência (ou N/A justificado)
- [x] Disponível no ambiente-alvo via pipeline, com smoke/E2E passando no ambiente (G6)
- [x] Observabilidade e rollback prontos conforme o Plano de Rollout
- [x] Documentação raiz e CHANGELOG atualizados (G7)
- [x] Pendências registradas como novas specs (ou nenhuma)

### Deploy
Deploy local via Aspire.

### Pendências
Nenhuma.

## 15. Emendas
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
