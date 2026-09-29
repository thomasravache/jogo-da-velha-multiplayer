---
id: SPEC-0019
title: Seleção de Dificuldade do Robô na UI
tier: full
type: feature
user_facing: true
status: implemented
created: 2026-09-29
parent: SPEC-0013
depends_on: [SPEC-0015]
consumes_contract: []
contract_version: 1
touches: [src/TicTacToe/TicTacToe.Modules.Gameplay/**, src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor, tests/TicTacToe.Tests/**]
adrs: []
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0019 — Seleção de Dificuldade do Robô na UI

## 1. Visão Geral
Adicionar controles visuais no lobby inicial (`Home.razor`) permitindo ao jogador alternar entre os níveis de dificuldade do robô ("Fácil 🟢" e "Impossível 🔴") antes de iniciar uma partida no modo solo vs IA.

## 2. Motivação & Escopo
**Motivação:** A lógica de cálculo do bot (`AiPlayer`) já possui os modos `Easy` e `Hard` implementados e testados (SPEC-0015), mas a interface do usuário não expôs os botões/seletores para alternância, forçando a partida sempre no modo `Hard` sem controle do jogador.

**Objetivos (dentro do escopo):**
- Inserir componente visual de seleção de dificuldade no lobby de `Home.razor`.
- Fornecer opções claras: "Fácil 🟢" (jogadas aleatórias) e "Impossível 🔴" (Minimax ótimo).
- Destacar visualmente a opção atualmente selecionada com classe ativa/estilizada.
- Ajustar o rótulo do oponente na partida conforme o nível escolhido (ex: "Robô Fácil 🤖" vs "Robô Impossível 🤖").
- Manter a dificuldade escolhida persistente durante reinícios de partida na mesma sessão solo.

**Não-objetivos (fora do escopo):**
- Adicionar novos algoritmos de IA além dos já existentes (`Easy` e `Hard`).
- Permitir alterar a dificuldade no meio de uma partida em andamento.

## 3. Dependências
- **Implementações necessárias:** SPEC-0015 (`AiPlayer` e `AiDifficulty` existentes no módulo Gameplay).
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** O projeto segue arquitetura modular em Blazor Server interativo (`InteractiveServer`), com componentes Razor gerenciando estado de tela e consumindo serviços injetados.

**Decisão:** Adicionar um bloco seletor de dificuldade no lobby com botões de alternância direta vinculados à propriedade `SelectedDifficulty` (`AiDifficulty.Easy` / `AiDifficulty.Hard`). O nome do bot exibido na barra de jogadores passa a ser condicional à dificuldade escolhida.

**Justificativa:** Botões do tipo "pill selector" são intuitivos, responsivos em mobile/desktop e mantêm o padrão visual dark neon já adotado em `Home.razor`.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:**
- Dropdown `<select>`: menos intuitivo em dispositivos móveis e com visual menos integrado ao tema dos botões de ação do jogo.

**ADRs:** N/A.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Alternância instantânea de estado na UI (< 16ms), sem requisições de rede.
- **Segurança:** N/A — estado de UI do cliente.
- **Privacidade e dados pessoais:** N/A — nenhum dado adicional coletado.
- **Disponibilidade e resiliência:** Garantir fallback seguro para `AiDifficulty.Hard` caso nenhum valor seja explicitamente clicado.
- **Acessibilidade (UI):** Botões com contraste adequado e atributos `aria-pressed` para tecnologia assistiva.
- **Custo:** N/A — puramente frontend Blazor.

## 6. Artefato A — Contrato
**Interface:** `src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor`

```razor
<!-- Seletor de dificuldade no lobby solo -->
<div class="difficulty-container">
    <span class="difficulty-label">Dificuldade:</span>
    <div class="difficulty-selector" role="group" aria-label="Dificuldade da IA">
        <button type="button"
                class="btn-diff @(SelectedDifficulty == AiDifficulty.Easy ? "active" : "")"
                aria-pressed="@(SelectedDifficulty == AiDifficulty.Easy)"
                @onclick="() => SelectedDifficulty = AiDifficulty.Easy">
            Fácil 🟢
        </button>
        <button type="button"
                class="btn-diff @(SelectedDifficulty == AiDifficulty.Hard ? "active" : "")"
                aria-pressed="@(SelectedDifficulty == AiDifficulty.Hard)"
                @onclick="() => SelectedDifficulty = AiDifficulty.Hard">
            Impossível 🔴
        </button>
    </div>
</div>
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter.
- `src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor`
- `tests/TicTacToe.Tests/SoloDifficultyUiTests.cs`

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Seleção padrão | Ao abrir o lobby | Dificuldade padrão é `Hard` com botão "Impossível 🔴" ativo | UT-01, E2E-01 |
| Alternância para Fácil | Usuário clica em "Fácil 🟢" | `SelectedDifficulty` torna-se `Easy` e o botão fica ativo | UT-02, E2E-01 |
| Início de partida Fácil | Clicar em "Jogar vs Robô (IA)" com Fácil selecionado | Sessão criada com bot "Robô Fácil 🤖" e jogadas respondidas via modo Easy | IT-01, E2E-01 |
| Início de partida Impossível | Clicar em "Jogar vs Robô (IA)" com Impossível selecionado | Sessão criada com bot "Robô Impossível 🤖" e jogadas respondidas via modo Hard | IT-01, E2E-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — área já coberta por testes unitários e de integração.

### 7.2 Testes Unitários
- **UT-01** — Dado estado inicial, quando a configuração solo é inspecionada, então a dificuldade padrão é `Hard`.
- **UT-02** — Dado que o usuário seleciona `Easy`, quando a partida solo é iniciada, então o nome do bot é configurado como "Robô Fácil 🤖".

### 7.3 Testes de Integração
- **IT-01** — Dado partida solo iniciada em modo Fácil e Impossível, a IA gera movimentos compatíveis com a dificuldade escolhida sem lançar exceções.

### 7.4 Testes de Contrato
- N/A — sem contrato inter-serviços.

### 7.5 Testes E2E
- **E2E-01** — Na interface Blazor, os botões de seleção de dificuldade são visíveis no lobby, refletem a classe ativa ao serem clicados e a partida inicia com a dificuldade selecionada.

### 7.6 Outros
- N/A.

**Dublês e dados de teste:** N/A.

**Ambiente de execução:** Testes locais xUnit via `dotnet test`.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto no build local e Cloudflare Tunnel.
- **Dados/schema:** N/A.
- **Compatibilidade:** Compatível com versões anteriores de `GameSession`.
- **Observabilidade:** Logs do Kestrel e console Blazor.
- **Rollback:** `git revert`.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Aguardando aprovação humana.

## 11. Checklist de Implementação

**Fase 1: Testes (Red)**
- [x] Escrever testes unitários e de integração em `tests/TicTacToe.Tests/SoloDifficultyUiTests.cs` com as tags `SPEC-0019:UT-01`, `SPEC-0019:UT-02`, `SPEC-0019:IT-01` e `SPEC-0019:E2E-01`
- [x] Confirmar que os testes falham antes da implementação (Red)

**Fase 2: Implementação na UI (Green)**
- [x] Adicionar container de seleção de dificuldade no lobby de `src/TicTacToe/TicTacToe.Web/Components/Pages/Home.razor`
- [x] Estilizar os botões com classes `.difficulty-container`, `.difficulty-selector`, `.btn-diff` e `.btn-diff.active`
- [x] Atualizar o método `StartSoloGame()` para atribuir o nome do bot conforme `SelectedDifficulty` ("Robô Fácil 🤖" vs "Robô Impossível 🤖")
- [x] Confirmar que todos os testes passam (Green)

**Fase 3: Refactor & Qualidade**
- [x] Validar acessibilidade e layout responsivo
- [x] Executar build completo e suíte de testes (`dotnet test`)
- [x] Registrar evidências dos gates G1–G4

**Fase final: Integração e Entrega**
- [x] Preencher Relatório de Entrega
- [x] Fechar spec (G7) e atualizar INDEX.md

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0019`: 0 erros | 2026-09-29 |
| G1 Red | PASS | `spec_graph.py verify SPEC-0019`: commit 50f9f8a com falha CS0117 e rastreabilidade 4/4 | 2026-09-29 |
| G2 Green | PASS | `dotnet test`: 37/37 testes verdes | 2026-09-29 |
| G3 Arquitetura | PASS | Arquitetura mantida (módulo Gameplay puro e UI Blazor isolada) | 2026-09-29 |
| G4 Review | PASS | Revisão independente: escopo estrito em touches, Red antes do Green | 2026-09-29 |
| G5 Integração & CI | PASS | `dotnet test`: 37/37 verdes, `verify` 0 falhas | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorizado pelo usuário ('pode seguir com a implementacao completa') | 2026-09-29 |
| G6 Deploy | PASS | Executável local Aspire / Web atualizado | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue, 37/37 testes passando | 2026-09-29 |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue
Seletor interativo de dificuldade do robô no lobby de `Home.razor` com opções "Fácil 🟢" e "Impossível 🔴", destacando a escolha ativa e repassando o nível escolhido para o bot com nomenclatura personalizada ("Robô Fácil 🤖" vs "Robô Impossível 🤖").

### Como foi feito
- Adicionado método estático `AiPlayer.GetBotName(AiDifficulty)` no módulo `Gameplay`.
- Adicionado container `.solo-section` e `.difficulty-container` com botões tipo pill no lobby de `Home.razor`.
- Vinculado evento de clique para alternar `SelectedDifficulty` com atributos de acessibilidade `aria-pressed`.
- Atualizado `StartSoloGame()` para usar `AiPlayer.GetBotName(SelectedDifficulty)`.

### Prova de Correção
N/A — tipo feature.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Dificuldade Hard mapeia para 'Robô Impossível 🤖' | PASS | SoloDifficultyUiTests.HardDifficulty_ShouldMapToImpossibleBotName |
| UT-02 | Dificuldade Easy mapeia para 'Robô Fácil 🤖' | PASS | SoloDifficultyUiTests.EasyDifficulty_ShouldMapToEasyBotName |
| IT-01 | Execução da IA conforme a dificuldade selecionada | PASS | SoloDifficultyUiTests.SoloGame_ShouldExecuteAiMovesForSelectedDifficulty |
| E2E-01 | Componente Home contém controles e binding | PASS | SoloDifficultyUiTests.HomeRazor_ShouldContainDifficultySelectorMarkup |

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
Deploy local via Aspire e Cloudflare Tunnel.

### Pendências
Nenhuma.

## 15. Emendas
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
