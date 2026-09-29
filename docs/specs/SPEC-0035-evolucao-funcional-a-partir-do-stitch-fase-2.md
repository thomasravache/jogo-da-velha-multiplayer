---
id: SPEC-0035
title: Evolução funcional a partir do Stitch (Fase 2)
tier: epic
type: feature
status: proposed
created: 2026-09-29
depends_on: []
adrs: [ADR-0009]
external: []
approved_by:
approved_at:
---

# SPEC-0035 — Evolução funcional a partir do Stitch (Fase 2) (Épico)

## 1. Visão
Trazer para o app as funcionalidades que o Stitch desenha e que o app ainda não tem, na ordem em que sustentam umas às outras: **dados de partida mais ricos → identidade do jogador → histórico e ranking avançados → série MD5 → abandonar e revanche → W.O. por desconexão**. Cada elemento do Stitch omitido na Fase 1 (SPEC-0028) tem aqui uma spec, ou fica registrado no backlog com motivo, para que nada se perca nem entre como dado falso.

Cada spec desta fase entrega **a regra e a interface juntas**, usando o design system e as telas da Fase 1.

## 2. Escopo
**Objetivos (dentro do escopo):**
- Persistir o que as telas precisam: duração, motivo do fim, linha vencedora, modo de jogo e jogadores por identidade (SPEC-0036).
- Reconhecer o mesmo jogador entre visitas, sem login (SPEC-0037, ADR-0009).
- Histórico avançado: resultado do ponto de vista do jogador, filtros, busca, ordenação, paginação, duração, motivo e resumo pessoal (SPEC-0038).
- Ranking avançado: derrotas, empates, aproveitamento, sequência, paginação e posição do jogador (SPEC-0039).
- Gravação única do resultado por partida, correção de um defeito que distorceria histórico e ranking (SPEC-0045).
- Série melhor de 5: regras, pareamento por formato e persistência (SPEC-0040) e interface no lobby e na arena (SPEC-0044).
- Abandonar partida e pedir revanche com aceite (SPEC-0041).
- W.O. por desconexão do oponente (SPEC-0042).

**Não-objetivos (fora do escopo):** os itens de backlog da matriz abaixo (ELO/divisões/temporada, reações, log de lances, presença, atalhos e sons, convite por link, amigos). Cada um exige uma decisão de produto própria e vira épico ou spec quando for priorizado.

### Matriz de cobertura do Stitch
Legenda de destino: **SPEC-NNNN** = coberto por spec desta fase · **Backlog X** = fora desta fase, com código · **Descartado** = texto de protótipo sem valor de produto.

| Tela | Elemento do Stitch | Destino |
|---|---|---|
| Header | Status "ONLINE • SignalR", latência/ping, região | Backlog J (presença) |
| Header | ELO, divisão, nível (LVL), avatar | Backlog E (ELO, divisões e temporada) |
| Header | Botão de áudio | Backlog K (atalhos e sons) |
| Header | Item de menu "Arena" | Sem spec: a arena é a partida em curso em `/` (SPEC-0028) |
| Lobby | "Bem-vindo de volta, {nome}", apelido lembrado, "identificador salvo em cache local" | SPEC-0037 |
| Lobby | Painel "Seu desempenho" (partidas, vitórias, derrotas, winrate) | Reaproveita o resumo da SPEC-0038; exibição no lobby: Backlog N (painel no lobby) |
| Lobby | ELO/temporada, "Proteção de rebaixamento", "+25/−12 ELO" | Backlog E |
| Lobby | "Fila ~3s", "duelistas ativos" | Backlog J |
| Lobby | Formato "Melhor de 5 (MD5)" | SPEC-0040 (regras) e SPEC-0044 (seletor) |
| Lobby | Compartilhar link da sala e entrar por link | Backlog L (convite por link) |
| Lobby | "Solo: offline, não afeta o histórico da temporada" | Decisão em Questões em Aberto (solo × histórico/ranking) |
| Arena | Rodada N de 5, placar da série, "Match point" | SPEC-0040 (regras) e SPEC-0044 (interface) |
| Arena | Pedir revanche (com aceite), Abandonar | SPEC-0041 |
| Arena | Vitória por W.O. por desconexão ("Desconexão do oponente") | SPEC-0042 |
| Arena | Reações rápidas (emotes) e cooldown | Backlog G |
| Arena | Log de lances em tempo real | Backlog H (depende dos lances persistidos na SPEC-0036) |
| Arena | Espectadores, tickrate, "SignalR Sync", ID da arena, ping por jogador | Backlog J |
| Arena | Atalhos NumPad e Auto-Lock | Backlog K |
| Arena | ELO/nível dos jogadores | Backlog E |
| Histórico | "VOCÊ", resultado Vitória/Derrota do ponto de vista do jogador | SPEC-0037 + SPEC-0038 |
| Histórico | Duração, motivo do fim (W.O., desconexão), lance decisivo | Dados: SPEC-0036 · Exibição: SPEC-0038 |
| Histórico | Filtros (Todas/Vitórias/Derrotas/Empates/Por W.O.), busca por adversário, ordenação, paginação | SPEC-0038 |
| Histórico | Resumo (total, winrate, sequência, mais rápida, tempo/lance) | SPEC-0038 |
| Histórico | "Lance destaque" (mini tabuleiro da última partida) | Backlog H |
| Histórico | ELO por jogador, "Sincronizado há", "Logs EF", "Fila ranqueada" | Backlog E / Descartado |
| Ranking | Derrotas, empates, aproveitamento, sequência, paginação | SPEC-0039 |
| Ranking | "Sua posição" (VOCÊ) e destaque da própria linha | SPEC-0037 + SPEC-0039 |
| Ranking | ELO, divisões, temporada, "Ciclo encerra em", bônus de ELO | Backlog E |
| Ranking | Status online / "há X", total de duelistas ativos, região | Backlog J |
| Ranking | Filtros "Amigos" e "Minha divisão" | Backlog M (amigos) e Backlog E |
| Todas | Rodapé (".NET Aspire Engine", "Blazor WebAssembly", direitos 2024) | Descartado |

## 3. Arquitetura Alvo
**Contexto:** módulos `Gameplay` (`GameSession`, `MatchResult`, `GameResultService`, `GameplayDbContext` com EF Core e migrations) e `Matchmaking` (fila, salas privadas, nomes por conexão) em Blazor Server (ADR-0004); UI da Fase 1 (SPEC-0028). Não há identidade nem autenticação hoje.

```text
GameSession ──(fim de partida)──▶ GameResultService.SaveResultAsync ──▶ MatchResult (estendido, SPEC-0036)
     │ WinningLine (SPEC-0031)          duração · motivo do fim · linha vencedora · modo · PlayerXId/OId (SPEC-0037)
     │ série MD5 (SPEC-0040, interface na SPEC-0044)
     │ abandono / revanche (SPEC-0041) · desconexão (SPEC-0042)
Identidade (SPEC-0037)  PlayerId anônimo no navegador (ProtectedLocalStorage) ──▶ Matchmaking / Home
Consultas               GetHistoryAsync(filtros, página) (SPEC-0038) · GetLeaderboardAsync(página) (SPEC-0039)
```

**Decisões (ADRs):** ADR-0009 — Identidade anônima persistente do jogador (proposto; introduzido pela SPEC-0037).

**Regras de arquitetura a garantir (G3):**
- Módulos continuam sem referência direta entre si (ADR-0004) — teste de arquitetura existente.
- A identidade não é usada como credencial: nenhuma ação sensível a aceita como autorização — `PlayerIdentityTests` (SPEC-0037).
- Migrations somente aditivas (colunas anuláveis/com valor padrão), sem apagar dados — verificado por spec.

## 4. Decomposição
| Spec | Título | Tier | Tipo | Tamanho | Depende de | Consome contrato de |
|---|---|---|---|---|---|---|
| SPEC-0045 | Gravação única do resultado da partida (correção de defeito) | full | fix | S | — | — |
| SPEC-0036 | Persistência enriquecida de partidas | full | feature | M | arena (Fase 1), SPEC-0045 | — |
| SPEC-0037 | Identidade anônima do jogador | full | feature | M | SPEC-0036 | — |
| SPEC-0038 | Histórico avançado | full | feature | M | histórico (Fase 1), SPEC-0036, SPEC-0037 | — |
| SPEC-0039 | Ranking avançado | full | feature | M | ranking (Fase 1), SPEC-0037, SPEC-0038 | — |
| SPEC-0040 | Série melhor de 5: regras, pareamento e persistência | full | feature | M | arena (Fase 1), SPEC-0036 | — |
| SPEC-0044 | Série melhor de 5: interface no lobby e na arena | full | feature | M | lobby (Fase 1), SPEC-0040 | — |
| SPEC-0041 | Abandonar partida e pedir revanche | full | feature | M | SPEC-0044, SPEC-0045 | — |
| SPEC-0042 | W.O. por desconexão do oponente | full | feature | M | SPEC-0041 | — |

Nas dependências, "lobby", "arena", "histórico" e "ranking" são as telas redesenhadas na Fase 1 (épico da Fase 1); a fonte da verdade é o frontmatter de cada spec.

As dependências refletem arquivos e migrações compartilhados (`GameSession`, `MatchResult`, `GameResultService`, `Home.razor.cs`), não só lógica: as specs andam em sequência para evitar conflito de migration e de merge.

## 5. Estratégia de Entrega
- **Ambientes:** como na Fase 1 (pipeline do repositório; sem ambiente remoto configurado em `sdd-config.yml`).
- **Entrega por onda:** cada spec vai para a `main` por PR; migrations EF aplicadas no startup (`MigrateAsync`), sempre aditivas.
- **Feature flags:** série MD5 (SPEC-0040) atrás de uma opção de modo escolhida no lobby, nunca ativada à força; demais specs sem flag.
- **Rollback:** `git revert`; migrations aditivas não exigem reversão de dados (colunas novas ficam sem uso).
- **Métricas de sucesso pós-release:** suíte verde; histórico e ranking com o mesmo resultado que antes para partidas antigas (sem `PlayerId`); ausência de exceções não tratadas no log do Kestrel.

## 6. Riscos & Mitigações
- **Identidade falsificável** (o `PlayerId` vive no navegador) — tratada como pseudônimo, não como credencial; nenhuma ação sensível o aceita como autorização (ADR-0009).
- **Dados antigos sem `PlayerId`, duração ou motivo** — colunas anuláveis; consultas e telas tratam ausência (exibem "—" ou usam o apelido).
- **Migrations em banco em uso** — apenas colunas anuláveis/valor padrão, aplicadas no startup; teste de integração com EF InMemory e revisão manual do SQL gerado.
- **Detecção de desconexão em Blazor Server** — o descarte do circuito pode ocorrer por reconexão temporária; janela de tolerância e testes com relógio controlado (SPEC-0042).
- **Concorrência no `GameSession`** — todas as mudanças de estado passam pelo `lock` existente; testes de corrida nas jogadas e no abandono.
- **Crescimento de escopo (ELO, presença, chat)** — matriz de cobertura fecha o escopo; itens de backlog só entram por nova spec.

## 7. Critérios de Aceite do Épico
- [ ] A partida é persistida com duração, motivo do fim, lado e linha vencedora, modo e identidade dos jogadores — SPEC-0036:IT-01
- [ ] O jogador é reconhecido entre visitas (apelido lembrado e "VOCÊ") — SPEC-0037:E2E-01
- [ ] O histórico filtra, busca, ordena e pagina, e mostra o resumo pessoal — SPEC-0038:E2E-01
- [ ] O ranking mostra derrotas, empates, aproveitamento, sequência, paginação e a posição do jogador — SPEC-0039:E2E-01
- [ ] A série melhor de 5 é escolhida no lobby e acompanhada na arena — SPEC-0044:E2E-01
- [ ] Cada partida terminada é gravada uma única vez, mesmo com dois jogadores observando — SPEC-0045:IT-02
- [ ] O jogador pode abandonar e pedir revanche, e o oponente aceita ou recusa — SPEC-0041:E2E-01
- [ ] A desconexão do oponente resulta em vitória por W.O. após a janela de tolerância — SPEC-0042:E2E-01

## 8. Questões em Aberto
- [x] Partidas **contra o robô** devem continuar entrando no histórico e no ranking? Hoje entram (o robô aparece como jogador nos dois). O Stitch diz que o modo solo não afeta o histórico da temporada. Recomendado: solo continua no **histórico pessoal** (com o modo "Solo" registrado) e **sai do ranking global**. — Solo fica no histórico pessoal (modo Solo registrado) e sai do ranking global. (thomas, 2026-09-29)
- [x] Partidas antigas, sem `PlayerId`, continuam no histórico e no ranking **por apelido** (recomendado, é o comportamento de hoje) ou são ocultadas quando a identidade estiver ativa? — Partidas antigas continuam no histórico e no ranking por apelido. (thomas, 2026-09-29)

## 9. Aprovação (H1)
Uma aprovação humana cobre o épico e as specs filhas apresentadas junto com ele. Registrada no frontmatter (`approved_by`, `approved_at`) do épico e de cada filha.

## 10. Registro de Impedimentos
<!-- Problemas que envolvem várias filhas (conflito de integração da onda, CI quebrado, decisão transversal). Impedimento que trava uma filha específica vai na própria filha. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 11. Relatório de Entrega
<!-- Preenchido ao fechar o épico: o que foi entregue, como (ondas e deploys com versão/data), resultado dos critérios de aceite com os testes que os provam, métricas pós-release, pendências como novas specs. `spec_graph.py report SPEC-0035` ajuda a montar. -->

## 12. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda, aprovada pelo humano. -->
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
