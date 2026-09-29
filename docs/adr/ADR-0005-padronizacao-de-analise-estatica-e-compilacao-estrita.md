---
id: ADR-0005
title: Padronização de Análise Estática e Compilação Estrita
status: accepted
origin: decision
date: 2026-09-29
decision_makers: [thomas]
consulted: []
informed: []
supersedes:
superseded_by:
enforced_by: Directory.Build.props e dotnet format
---

# ADR-0005 — Padronização de Análise Estática e Compilação Estrita

## Contexto e Problema
O repositório possui múltiplos projetos C# sem centralização de regras de compilação ou linter formal. Divergências de formatação (espaçamento, chaves, quebras de linha) e warnings de compilação podem passar despercebidos, enfraquecendo a qualidade e a segurança do código em produção. Como padronizar a análise estática e a formatação sem introduzir dependências pesadas?

## Direcionadores da Decisão
- Consistência de código entre desenvolvedores e agentes autônomos.
- Segurança contra referências nulas (`Nullable` habilitado em toda a solução).
- Tolerância zero a avisos de compilação (`TreatWarningsAsErrors`).
- Execução rápida tanto no ambiente de desenvolvimento local quanto no pipeline de CI.

## Opções Consideradas
- **Opção A:** `.editorconfig` + `Directory.Build.props` nativo do .NET + `SonarAnalyzer.CSharp`
- **Opção B:** Ferramentas externas de formatação (ex: CSharpier ou Prettier) com plugins de terceiros
- **Opção C:** Manter compilação padrão sem regras estritas

## Resultado da Decisão
**Opção escolhida:** Opção A (`.editorconfig` + `Directory.Build.props` + `SonarAnalyzer.CSharp`), porque utiliza as capacidades nativas do SDK do .NET 10, integra-se perfeitamente com `dotnet format` e qualquer IDE (VS Code, Visual Studio, Rider) sem requerer ferramentas externas no ambiente local ou CI.

### Consequências
- **Boa**, porque todo build trata warnings como erro, evitando degradação técnica gradual.
- **Boa**, porque `dotnet format --verify-no-changes` torna-se uma barreira automática e determinística de formatação.
- **Ruim**, porque desenvolvedores precisam rodar `dotnet format` ou corrigir estilos antes de submeter código que viole as regras.

### Confirmação (G3)
Confirmado automaticamente pelo compilador via `Directory.Build.props` (`TreatWarningsAsErrors=true`) e pelo comando de linter no CI (`dotnet format --verify-no-changes`).

## Prós e Contras das Opções
| Critério (peso 1-5) | Opção A (.editorconfig + props) | Opção B (Ferramenta externa) | Opção C (Padrão solto) |
|---|---|---|---|
| Zero dependências externas (5) | 5 (Nativo .NET) | 2 (Exige npm/global tool) | 5 |
| Rigor e detecção de bugs (4) | 5 (Roslyn + Sonar) | 3 (Só formatação) | 1 |
| Facilidade de CI/CD (4) | 5 (dotnet build/format) | 3 (setup adicional) | 5 |
| **Total ponderado** | **49** | **31** | **33** |

## Mais Informações
- Microsoft Learn: [Code analysis in .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
- SonarSource: [SonarAnalyzer.CSharp rules](https://rules.sonarsource.com/csharp)
