---
id: ADR-0007
title: Adoção do MudBlazor como Design System e Componentes
status: superseded
origin: decision
date: 2026-09-29
decision_makers: [thomas]
consulted: []
informed: []
supersedes: ADR-0002
superseded_by: ADR-0008
enforced_by: tests/TicTacToe.Tests/MudBlazorIntegrationTests.cs
---

# ADR-0007 — Adoção do MudBlazor como Design System e Componentes

## Contexto e Problema
A interface da aplicação combinava o template básico corporativo gerado pelo ASP.NET Core (sidebar lateral roxa de dashboard e barra superior com fundo claro) com componentes customizados de jogo. Com a decomposição e isolamento de CSS, o container externo manteve o fundo claro padrão do Bootstrap, gerando baixo contraste e problemas visuais graves ("tela branca"). Precisamos de uma biblioteca de componentes e design system profissional que forneça tema escuro nativo consistente, componentes de UI responsivos e modernos sem dependências externas complexas.

## Direcionadores da Decisão
- Suporte nativo a Tema Escuro consistente em todo o ciclo de renderização do Blazor.
- Ecossistema 100% C#/.NET sem necessidade de toolchains externas (Node.js/npm).
- Componentes modernos prontos para jogos casuais (Cards, Botões com ícones, Toasts/Snackbars, Modais para salas privadas).
- Performance de renderização rápida e integração limpa com Blazor Server interativo.

## Opções Consideradas
- **MudBlazor (Opção Escolhida):** Biblioteca de componentes Material Design mais madura e ativa do ecossistema Blazor (.NET 10 compatível), com `MudThemeProvider` nativo para dark mode, snackbars e ícones integrados.
- **Tailwind CSS + DaisyUI:** Excelente flexibilidade visual, mas exige configuração de pipeline Node/npm/PostCSS e desenvolvimento manual de cada componente Blazor.
- **Bootstrap 5 (Status Quo):** Padrão do template inicial, porém propenso a conflitos de contraste em páginas customizadas e com aparência corporativa.

## Resultado da Decisão
**Opção escolhida:** MudBlazor, porque unifica o design system em C# puro, resolve nativamente o gerenciamento de tema escuro com `MudThemeProvider`, substitui o layout lateral por um `MudAppBar` superior imersivo e fornece componentes profissionais para formulários, botões e notificações.

### Consequências
- **Boa**, porque elimina de vez problemas de contraste e telas brancas, fornecendo suporte a tema escuro centralizado e consistente.
- **Boa**, porque substitui a sidebar corporativa por uma barra superior imersiva e moderna, centralizando a partida e melhorando a usabilidade no mobile.
- **Boa**, porque permite adicionar Snackbars e diálogos elegantes sem JavaScript adicional.
- **Ruim**, porque adiciona dependência do pacote NuGet `MudBlazor` no projeto Web e nos testes com bUnit.

### Confirmação (G3)
Garantido por testes automatizados em `tests/TicTacToe.Tests/MudBlazorIntegrationTests.cs` validando que `MudThemeProvider`, `MudLayout` e componentes MudBlazor são instanciados e configurados corretamente nos serviços e no container de renderização.

## Prós e Contras das Opções
| Critério (peso) | MudBlazor | Tailwind + DaisyUI | Bootstrap 5 (Atual) |
|---|---|---|---|
| Suporte nativo Blazor / C# (Alta) | Alta (10/10) | Baixa (4/10) | Média (6/10) |
| Suporte e consistência a Dark Mode (Alta) | Alta (10/10) | Alta (9/10) | Baixa (3/10) |
| Zero dependências de Node/npm (Média) | Sim | Não | Sim |
| Produtividade e componentes prontos (Alta) | Alta (9/10) | Média (6/10) | Média (5/10) |

## Mais Informações
- MudBlazor versão estável 9.x / 8.x no NuGet.org.
- Documentação oficial: https://mudblazor.com
- Suplanta o padrão de UI inicial registrado em ADR-0002.
- Implementado pela SPEC-0026.
