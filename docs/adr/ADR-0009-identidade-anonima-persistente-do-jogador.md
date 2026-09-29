---
id: ADR-0009
title: Identidade anônima persistente do jogador
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
consulted: []
informed: []
supersedes:
superseded_by:
enforced_by: tests/TicTacToe.Tests/PlayerIdentityTests.cs
---

# ADR-0009 — Identidade anônima persistente do jogador

## Contexto e Problema
Hoje o jogador é só um apelido livre digitado no lobby (`Home.razor.cs`: `PlayerName`), sem identidade persistente: `MatchResult` guarda apenas `PlayerXName`, `PlayerOName` e `WinnerName`. Duas pessoas com o mesmo apelido se confundem no ranking e no histórico, e o app não sabe qual linha é "você". O Stitch mostra "VOCÊ" no histórico, posição do usuário no ranking, filtros "Vitórias/Derrotas" e resumo pessoal, o que exige reconhecer o mesmo jogador entre sessões. Não existe login e não é objetivo criar um agora.

## Direcionadores da Decisão
- Reconhecer o mesmo jogador entre visitas sem cadastro nem senha.
- Manter o fluxo atual (digitar apelido e jogar).
- Privacidade: identificador pseudônimo, sem dado pessoal além do apelido; retenção definida.
- Blazor Server interativo: o estado do navegador só é acessível após a renderização interativa.
- Sobreviver a reinícios e deploys do servidor: a identidade não pode depender de chaves ou estado em memória do servidor.
- Não bloquear evolução futura para login real.

## Opções Consideradas
- **A. `PlayerId` (GUID) gerado no primeiro acesso e guardado no `localStorage` do navegador (via JS interop), enviado à gravação da partida; o apelido segue como nome de exibição.**
- **B. Autenticação real (ASP.NET Core Identity ou OAuth).** Mais segura, mas exige cadastro, telas e política de dados; fora do escopo do redesign.
- **C. Manter o apelido como identidade (status quo).** Sem mudança, mas o "VOCÊ" fica impreciso e homônimos se fundem.

## Resultado da Decisão
**Opção escolhida:** **A**, porque entrega o reconhecimento do jogador (direcionador principal) sem cadastro, com custo baixo e caminho aberto para B. Confirmada pelo responsável (thomas, 2026-09-29) na SPEC-0037; o ADR passa a `accepted` quando a spec for aprovada (H1).

### Consequências
- **Boa**, porque histórico, ranking e "VOCÊ" passam a funcionar por identidade e não por texto.
- **Boa**, porque não muda o fluxo do lobby.
- **Boa**, porque `localStorage` simples não depende das chaves do Data Protection do ASP.NET Core: `ProtectedLocalStorage` foi considerado, mas se as chaves não forem persistidas entre reinícios/deploys, todo jogador perderia a identidade a cada deploy, e cifrar um identificador que não é segredo não traz benefício.
- **Ruim**, porque a identidade vive no navegador: limpar os dados do site ou trocar de dispositivo cria um novo jogador; não há recuperação.
- **Ruim**, porque não é autenticação: quem copiar o `PlayerId` se passa pelo jogador. O ID é tratado como pseudônimo, nunca como credencial, e não autoriza nenhuma ação sensível.
- **Ruim**, porque `MatchResult` ganha colunas de identidade (migration EF), e partidas antigas ficam sem `PlayerId` (permanecem só por apelido).
- Dado pessoal: o `PlayerId` é um identificador pseudônimo associado a um apelido (LGPD). Tratamento e retenção são definidos na SPEC-0037.

### Confirmação (G3)
`tests/TicTacToe.Tests/PlayerIdentityTests.cs` (criado na SPEC-0037): o `PlayerId` é GUID válido, persiste entre leituras do serviço de identidade, nunca é exibido para outros jogadores e não é usado como credencial (nenhum endpoint ou comando aceita o ID como autorização).

## Prós e Contras das Opções
| Critério | A. GUID no navegador | B. Autenticação real | C. Apelido |
|---|---|---|---|
| Reconhece o jogador entre visitas | Sim (mesmo navegador) | Sim (qualquer dispositivo) | Não confiável |
| Esforço | Baixo | Alto | Nenhum |
| Fricção para o jogador | Nenhuma | Alta (cadastro) | Nenhuma |
| Resistência a falsificação | Baixa | Alta | Nenhuma |
| Privacidade | Pseudônimo, retenção simples | Dados de conta | Sem identificador |

## Mais Informações
- `localStorage` acessado por JS interop só após a renderização interativa (`OnAfterRenderAsync`), padrão do Blazor Server. **Não verificado** contra a documentação oficial nesta sessão; a SPEC-0037 exige a confirmação no .NET 10 do projeto antes do Red.
- `ProtectedLocalStorage` descartado pelo motivo acima (dependência de chaves do Data Protection persistidas).
- Specs: SPEC-0037 (introduz), SPEC-0038 e SPEC-0039 (consomem).
