# Referência de design — Stitch "Blazor Tic-Tac-Toe Multiplayer"

Exportação de **2026-09-29** do projeto Stitch `8020509426269957433` (design system "Cyber Arena Tic-Tac-Toe", tema escuro, Outfit + Space Grotesk). Este diretório é a **referência visual versionada**: as specs citam estes arquivos, nunca o projeto externo.

> Os HTMLs foram gerados pelo Stitch com Tailwind via CDN (`cdn.tailwindcss.com`) e ícones Material Symbols. **Não são código de produção**: são fonte de tokens, hierarquia e composição. O texto dentro deles é dado de protótipo (nomes, ELO, ping, contagens) e não define comportamento.

## Arquivos

| Tela | Mobile (390px) | Desktop (1280px+) | Rota atual |
|---|---|---|---|
| Lobby & Modos de Jogo | `lobby-mobile.html` / `.png` | `lobby-desktop.html` / `.png` | `/` (sem partida) |
| Arena da Partida | `arena-mobile.html` / `.png` | `arena-desktop.html` / `.png` | `/` (com partida) |
| Histórico de Partidas | `historico-mobile.html` / `.png` | `historico-desktop.html` / `.png` | `/history` |
| Ranking Geral | `ranking-mobile.html` / `.png` | `ranking-desktop.html` / `.png` | `/leaderboard` |
| Logos | `assets/cyber-arena-logo.svg`, `assets/xo-arena-logo.svg` | | |

Os `.png` são miniaturas (~512px de largura) entregues pela API do Stitch; para medidas exatas, use os HTMLs.

## Tokens (fonte de verdade)

O `DESIGN.md` do projeto tem duas paletas que divergem:

- **Prosa do documento (adotada):** primário/X `#ff4757`, secundário/O `#00d2d3`, vitória `#10b981`, ouro `#f39c12`, âmbar `#fbbf24`, superfícies `#1a1a2e` (base), `#16213e`, `#0f3460`, borda `rgba(255,255,255,0.08)`.
- **Front matter Material 3 gerado (não adotado):** `primary #ffb3b2`, `surface #111125` etc. Aparece nos HTMLs via `tailwind.config`, mas contradiz a prosa e a marca (logo em `#ff4757`).

A decisão e os tokens completos ficam na SPEC-0029 e no ADR-0008.

## Como a referência é usada

Cada spec de tela lista, na seção de escopo, o que do Stitch **entra** (existe dado/regra hoje) e o que fica **omitido** com a spec futura que o traz. A matriz completa de cobertura está na SPEC-0035.
