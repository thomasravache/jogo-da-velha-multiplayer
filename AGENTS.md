<!-- sdd:start -->
## Processo de desenvolvimento (SDD)

Este projeto usa Spec-Driven Development. Vale para qualquer mudança de comportamento (feature, bug, refactor, migração), qualquer agente e qualquer modelo:

1. **Carregue a skill `sdd-management` antes de agir.** Sem ela disponível, siga `docs/specs/INDEX.md` e as regras abaixo.
2. **Não dependa da memória da conversa.** Antes de cada passo rode `python3 tools/sdd/spec_graph.py status` (ou `status SPEC-NNNN`) e execute o próximo passo indicado.
3. **Nada é implementado sem spec aprovada pelo humano (H1).** Dúvidas vão para "Questões em Aberto"; paradas, para o "Registro de Impedimentos" (`impede` / `resolve`).
4. **Testes antes do código.** Commit `test(...)` antes de `feat|fix|refactor(...)`. Mensagens em Conventional Commits com rodapé `Refs: SPEC-NNNN`.
5. **Arquitetura, padrões e bibliotecas existentes são mantidos.** Desvio só com spec `type: migration` ou ADR aprovado.
6. **Só o Architect edita `docs/specs/` e `docs/adr/`.** Subagentes reportam; não editam specs.
7. **Branch e Pull Request obrigatórios:** É expressamente proibido commitar ou fazer push direto na branch `main`. Todo desenvolvimento e correção devem seguir o fluxo de PR:
   - **Branch dedicada:** Crie uma branch a partir da `main` no padrão `<tipo>/<SPEC-ID>-<slug>` (ex: `feat/SPEC-0027-novo-recurso` ou `fix/SPEC-0028-ajuste-layout`).
   - **Ciclo TDD:** Desenvolva na branch respeitando Red -> Green -> Refactor com commits `test(...)` antes de `feat(...)`/`fix(...)`.
   - **Abertura do PR:** Concluídos os gates locais G0–G4, envie a branch (`git push origin <branch>`) e abra o PR para a `main` via `gh pr create` com corpo gerado por `python3 tools/sdd/spec_graph.py pr SPEC-NNNN`.
   - **Validação de CI (G5):** O CI executa `validate`, `pr-check`, compilação e suíte de testes.
   - **Merge (H2):** O agente NUNCA faz merge automático. O merge para a `main` só ocorre após CI verde e autorização explícita do usuário (H2).
8. **Board:** definido em `tracker` no `docs/specs/sdd-config.yml` (`none` = só o repositório).
<!-- sdd:end -->
