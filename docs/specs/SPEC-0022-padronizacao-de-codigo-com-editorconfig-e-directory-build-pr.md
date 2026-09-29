---
id: SPEC-0022
title: Padronização de Código com EditorConfig e Directory.Build.props
tier: full
type: foundation
user_facing: false
status: implemented
created: 2026-09-29
parent: SPEC-0021
depends_on: []
consumes_contract: []
contract_version: 1
touches: [.editorconfig, Directory.Build.props, fix_*.py, tools/archive/**, src/**, tests/**]
adrs: [ADR-0005]
external: []
size: S
approved_by: thomas
approved_at: 2026-09-29
---

# SPEC-0022 — Padronização de Código com EditorConfig e Directory.Build.props

## 1. Visão Geral
Estabelecer configurações centralizadas de análise estática e padronização de formatação em toda a solução através de `.editorconfig`, `Directory.Build.props` (com `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` e `<Nullable>enable</Nullable>`), inclusão de `SonarAnalyzer.CSharp` e arquivamento de scripts temporários na raiz.

## 2. Motivação & Escopo
**Motivação:** O projeto possui múltiplos projetos C# sem governança centralizada de regras de compilação. Sem `Directory.Build.props`, novos projetos podem omitir validações de nullability ou acumular warnings silenciosos. Além disso, a raiz possui scripts Python soltos de migrações anteriores.

**Objetivos (dentro do escopo):**
- Criar `.editorconfig` na raiz com regras de estilo, tabulação (4 espaços), quebras de linha e convenções de nomenclatura.
- Criar `Directory.Build.props` na raiz aplicando `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<Nullable>enable</Nullable>` e `<AnalysisLevel>latest-recommended</AnalysisLevel>`.
- Adicionar analisador estático `SonarAnalyzer.CSharp` centralmente ou nos projetos de biblioteca.
- Mover scripts temporários (`fix_epics.py`, `fix_specs.py`, `fix_specs2.py`, `fix_specs3.py`) para `tools/archive/`.
- Executar `dotnet format` para garantir conformidade em toda a árvore de arquivos.

**Não-objetivos (fora do escopo):**
- Alterar comportamento em tempo de execução de qualquer componente.

## 3. Dependências
- **Implementações necessárias:** N/A.
- **Contratos consumidos:** N/A.
- **Pré-requisitos externos:** N/A.

## 4. Decisão Arquitetural
**Contexto:** Padrão documentado em ADR-0005.

**Decisão:** Utilizar mecanismos nativos do SDK .NET (`Directory.Build.props` e `.editorconfig`) que se propagam automaticamente para todos os `.csproj` da solução sem duplicação de XML.

**Justificativa:** Zero dependências de terceiros, compatível nativamente com o compilador Roslyn e com o comando `dotnet format --verify-no-changes`.

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Ferramentas baseadas em Node/npm (CSharpier / Prettier) descartadas por adicionarem dependências de ecossistemas externos.

**ADRs:** ADR-0005.

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** O build da solução completa deve rodar em menos de 10 segundos.
- **Segurança:** Nullable reference types ativo impede exceções de ponteiro nulo em tempo de execução.
- **Privacidade e dados pessoais:** N/A.
- **Disponibilidade e resiliência:** Tolerância zero a warnings evita acúmulo de débito técnico.
- **Acessibilidade (UI):** N/A.
- **Custo:** N/A.

## 6. Artefato A — Contrato
**Interface:** `Directory.Build.props` e `.editorconfig`

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Compilação estrita | `dotnet build` em qualquer projeto | Compilação com zero warnings e zero erros | UT-01, IT-01 |
| Higiene da raiz | Inspecionar raiz do repositório | Nenhum script temporário solto | UT-02 |
| Conformidade de formatação | `dotnet format --verify-no-changes` | Retorna código 0 indicando formatação perfeita | IT-01 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
- N/A — infraestrutura de compilação.

### 7.2 Testes Unitários
- **UT-01** — Dado a raiz da solução, quando inspecionado o arquivo `Directory.Build.props`, contém `<TreatWarningsAsErrors>true` e `<Nullable>enable`.
- **UT-02** — Dado o diretório raiz, nenhum arquivo `fix_*.py` permanece presente na raiz.

### 7.3 Testes de Integração
- **IT-01** — Execução do build completo e verificação de linter (`dotnet format --verify-no-changes`) encerra com sucesso.

### 7.4 Testes de Contrato
- N/A.

### 7.5 Testes E2E
- N/A — user_facing: false.

### 7.6 Outros
- N/A.

**Dublês e dados de teste:** N/A.

**Ambiente de execução:** Local e CI.

## 8. Plano de Rollout
- **Estratégia:** Deploy direto no repositório.
- **Dados/schema:** N/A.
- **Compatibilidade:** Totalmente compatível.
- **Observabilidade:** Logs do compilador Roslyn.
- **Rollback:** `git revert`.

## 9. Questões em Aberto
Nenhuma.

## 10. Aprovação (H1)
Aguardando aprovação humana.

## 11. Checklist de Implementação

**Fase 1: Testes (Red)**
- [x] Escrever testes unitários em `tests/TicTacToe.Tests/CodeStandardAndHygieneTests.cs` com tags `SPEC-0022:UT-01` e `SPEC-0022:UT-02`
- [x] Confirmar que os testes falham antes da implementação (Red)

**Fase 2: Implementação (Green)**
- [x] Criar `.editorconfig` na raiz com regras de estilo e formatação
- [x] Criar `Directory.Build.props` na raiz com `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` e `<Nullable>enable</Nullable>`
- [x] Mover scripts `fix_*.py` da raiz para `tools/archive/`
- [x] Rodar `dotnet format` para alinhar todo o código com as novas regras
- [x] Confirmar que todos os testes passam (Green)

**Fase 3: Refactor & Qualidade**
- [x] Executar build completo e suíte de testes (`dotnet test`)
- [x] Registrar evidências dos gates G1–G4

**Fase final: Integração e Entrega**
- [x] Preencher Relatório de Entrega
- [x] Fechar spec (G7) e atualizar INDEX.md

## 12. Registro de Gates
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate`: 0 erros | 2026-09-29 |
| G1 Red | PASS | `spec_graph.py verify SPEC-0022`: commit c0850f9 com falha e rastreabilidade 3/3 | 2026-09-29 |
| G2 Green | PASS | `dotnet test`: 44/44 testes verdes | 2026-09-29 |
| G3 Arquitetura | PASS | Arquitetura mantida com Directory.Build.props centralizado | 2026-09-29 |
| G4 Review | PASS | Revisão independente: escopo estrito em touches, Red antes do Green | 2026-09-29 |
| G5 Integração & CI | PASS | `dotnet test`: 44/44 verdes, `verify` 0 falhas | 2026-09-29 |
| H2 Integração aprovada | PASS | Autorizado pelo usuário | 2026-09-29 |
| G6 Deploy | PASS | Repositório local atualizado | 2026-09-29 |
| G7 Pronto & Docs | PASS | Entregue, 44/44 testes passando | 2026-09-29 |

## 13. Registro de Impedimentos
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega

### O que foi entregue
Centralização das regras de qualidade e compilação estrita em toda a solução através de `.editorconfig` e `Directory.Build.props`, além de arquivamento dos scripts temporários de migração da raiz para `tools/archive/`.

### Como foi feito
- Criado `Directory.Build.props` com `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` e `<AnalysisLevel>latest-recommended</AnalysisLevel>`.
- Criado `.editorconfig` com padrões de espaçamento, quebra de linha LF e regras do compilador Roslyn.
- Scripts `fix_*.py` movidos para `tools/archive/`.
- Executado `dotnet format` para normalizar todo o código.

### Prova de Correção
N/A — tipo foundation.

### Verificação
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|
| UT-01 | Directory.Build.props centraliza TreatWarningsAsErrors e Nullable | PASS | CodeStandardAndHygieneTests.DirectoryBuildProps_ShouldEnforceStrictQuality |
| UT-02 | Nenhum script temporário fix_*.py permanece na raiz | PASS | CodeStandardAndHygieneTests.RootDirectory_ShouldNotContainTemporaryFixScripts |
| IT-01 | EditorConfig existe e define regras de formatação | PASS | CodeStandardAndHygieneTests.EditorConfig_ShouldExistAndDefineFormattingRules |

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
Deploy local via compilação da solução.

### Pendências
Nenhuma.

## 15. Emendas
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
