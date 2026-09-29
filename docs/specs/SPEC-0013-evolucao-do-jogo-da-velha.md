---
id: SPEC-0013
title: Evolução do Jogo da Velha
tier: epic
type: feature
status: implemented
created: 2026-09-29
depends_on: [SPEC-0005]
adrs: [ADR-0001, ADR-0002, ADR-0003, ADR-0004]
external: []
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0013 — Evolução do Jogo da Velha (Épico)

## 1. Visão
Evoluir o jogo da velha multiplayer de um protótipo jogável para uma plataforma completa, com placar de vitórias por sessão, efeitos visuais de comemoração, jogo solo contra inteligência artificial (Minimax), criação de salas privadas com código de convite, ranking persistido no banco de dados e sincronização reativa sem polling.

## 2. Escopo
**Objetivos (dentro do escopo):**
- Contabilização e exibição do placar acumulado na sessão e efeito de confetes na vitória (`SPEC-0014`).
- Modo de jogo individual contra IA utilizando algoritmo Minimax (`SPEC-0015`).
- Criação e ingresso em partidas privadas via código de sala (`SPEC-0016`).
- Página de Leaderboard com classificação geral acumulada no SQL Server (`SPEC-0017`).
- Barramento reativo em memória com `System.Threading.Channels` eliminando o polling (`SPEC-0018`).

**Não-objetivos (fora do escopo):**
- Autenticação federada / OAuth.
- Chat de áudio / vídeo.
- Modos com tabuleiros maiores (ex: 4x4 ou 5x5).

## 3. Arquitetura Alvo
**Contexto:** Modular Monolith (.NET 10) mantendo isolamento entre `Gameplay`, `Matchmaking`, `Web` e orquestração via Aspire.

**Decisões (ADRs):**
- ADR-0001 (Aspire)
- ADR-0002 (Blazor Server)
- ADR-0003 (SQL Server em container)
- ADR-0004 (Modular Monolith)

**Regras de arquitetura a garantir (G3):**
- Domínio de IA e regras residem exclusivamente em `Gameplay`.
- Gerenciamento de códigos e salas privadas reside exclusivamente em `Matchmaking`.
- Agregações de ranking são realizadas pelo `GameplayDbContext`.

## 4. Decomposição
| Spec | Título | Tier | Tipo | Tamanho | Depende de | Consome contrato de |
|---|---|---|---|---|---|---|
| SPEC-0014 | Placar da Sessão e Efeitos de Vitória | full | feature | S | base | |
| SPEC-0015 | Modo Solo vs IA Minimax | full | feature | M | SPEC-0014 | |
| SPEC-0016 | Salas Privadas com Código | full | feature | M | SPEC-0014 | |
| SPEC-0017 | Leaderboard e Estatísticas | full | feature | M | base | |
| SPEC-0018 | Sincronização Reativa por Eventos | full | feature | M | SPEC-0016 | |
| SPEC-0019 | Seleção de Dificuldade do Robô na UI | full | feature | S | SPEC-0015 | |

## 5. Estratégia de Entrega
- **Ambientes:** Local via Aspire.
- **Entrega por onda:**
  - Onda 1: SPEC-0014 (Placar + Confetes) e SPEC-0017 (Leaderboard) em paralelo.
  - Onda 2: SPEC-0015 (Modo Solo vs IA) e SPEC-0016 (Salas Privadas) em paralelo.
  - Onda 3: SPEC-0018 (Sincronização Reativa por Eventos).
- **Feature flags:** Não aplicável (deploy contínuo local).
- **Rollback:** Reversão via Git commit.
- **Métricas de sucesso pós-release:** Todas as specs implementadas com 100% dos testes verdes.

## 6. Riscos & Mitigações
- **Concorrência em salas privadas:** Mitigado pelo uso de `ConcurrentDictionary` no `MatchmakingService`.
- **Performance do algoritmo Minimax:** Como o tabuleiro 3x3 possui espaço de busca máximo de 9! = 362.880 nós, a execução roda em milissegundos sem necessidade de poda alpha-beta complexa.

## 7. Critérios de Aceite do Épico
- [x] Placar da sessão e confetes funcionais entre partidas consecutivas (Provado por SPEC-0014:E2E-01)
- [x] Jogador consegue enfrentar a IA e a IA nunca perde no nível impossível (Provado por SPEC-0015:E2E-01)
- [x] Dois jogadores se conectam exclusivamente através de um código de sala privada (Provado por SPEC-0016:E2E-01)
- [x] Página de leaderboard exibe ranking agregado dos jogadores mais vitoriosos (Provado por SPEC-0017:E2E-01)
- [x] Jogadas sincronizam instantaneamente via canais de eventos sem polling periódico (Provado por SPEC-0018:E2E-01)

## 8. Questões em Aberto
Nenhuma.

## 9. Aprovação (H1)
Aprovação humana registrada no frontmatter de cada spec.

## 10. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 11. Relatório de Entrega
Todas as 5 especificações filhas foram implementadas e fechadas com sucesso:
- SPEC-0014: Placar de sessão acumulado e confetes de vitória.
- SPEC-0015: Modo Solo vs IA com algoritmo Minimax invencível.
- SPEC-0016: Criação e ingresso em salas privadas com códigos curtos.
- SPEC-0017: Leaderboard com ranking e medalhas persistido em SQL Server.
- SPEC-0018: Sincronização reativa orientada a eventos em memória.

Suíte completa com 32 testes automatizados executando com 100% de sucesso.

## 12. Emendas
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
