# Referência de design — Xadrez (Stitch)

Exportação de **2026-09-29** do mesmo projeto Stitch `8020509426269957433` (design system Cyber Arena), telas de **xadrez**. Como no README da pasta acima, este diretório é a referência visual versionada; as specs citam estes arquivos, nunca o projeto externo. O texto dentro das imagens é dado de protótipo (nomes, rating, latência) e não define comportamento.

| Tela | Arquivo | Observação |
|---|---|---|
| Seleção de jogos | `selecao-desktop.png` | Escolha entre Jogo da Velha e Xadrez |
| Lobby de xadrez | `lobby-desktop.png` | Tempo (Bullet 1+0, Blitz 5+0, Rápida 10+5), cor, fila, sala privada |
| Arena de xadrez | `arena-desktop.png`, `arena-mobile.png` | Tabuleiro, relógios, lances, promoção, fim de partida |
| Histórico de xadrez | `historico-desktop.png` | Cor, controle, motivo, lances, duração |
| Ranking de xadrez | `ranking-desktop.png` | Pódio, tabela, "Sua posição" |
| Peças | `pecas.md`, `pecas-folha.png` | 12 SVGs |

Os HTMLs do Stitch exigem login do Google para baixar e não foram versionados; as PNGs e o `pecas.md` bastam como referência. As telas mobile de seleção, lobby, histórico e ranking existem no projeto Stitch (não exportadas); o layout mobile segue o padrão já implementado nas telas do jogo da velha.

## O que do Stitch NÃO entra (dado de protótipo ou fora de escopo)
Rating/ELO e "+N pontos de rating"; "Ranked Arena", temporadas e títulos ("Grande Campeão Supremo", "Elite Xadrez"); selos de troféu; "Anti-cheat engine"; latência em ms e "SignalR live"; nível ("LVL 42"); nome da abertura ("Ruy Lopez"); "Propor empate" e "Acordo mútuo" (decisão do produto: fora do escopo); vantagem de material (`+3`) como número autoritativo; avatar por foto; "Powered by".

## Defeitos do mock que as specs corrigem
- Peças pretas quase invisíveis sobre casas escuras: a SPEC-0055 exige contraste mínimo verificado por teste.
- Tabuleiro mobile incompleto (só a primeira fileira): a arena mobile é definida pela spec (SPEC-0057), não pela imagem.
- Fonte com serifa nos títulos das telas novas: mantém-se Outfit/Space Grotesk do design system (ADR-0008).
