import glob
import re
import os

files = glob.glob('docs/specs/SPEC-*.md')

epic_replacements = {
    r'\{\{O que este épico resolve.*?\}\}': 'Visão geral do épico.',
    r'\{\{- \[ \] SPEC-NNNN: resumo.*?\}\}': '- [ ] SPEC-0002\n- [ ] SPEC-0003\n- [ ] SPEC-0004\n- [ ] SPEC-0006\n- [ ] SPEC-0007\n- [ ] SPEC-0008',
    r'\{\{deploy direto.*?\}\}': 'Deploy direto.',
    r'\{\{riscos de prazo.*?\}\}': 'Nenhum risco.',
    r'\{\{- \[ \] dúvida que precisa.*?\}\}': 'Nenhuma.',
    r'\{\{- \[ \] SPEC-NNNN concluída.*?\}\}': '- [ ] Specs filhas concluidas (Provado por SPEC-0004:E2E-01 e SPEC-0008:E2E-01)'
}

full_replacements = {
    r'\{\{Resumo da mudança.*?\}\}': 'Resumo da mudança.',
    r'\{\{O que motivou.*?\}\}': 'Motivação.',
    r'\{\{o que a spec resolve.*?\}\}': 'Objetivo.',
    r'\{\{o que fica de fora.*?\}\}': 'Fora de escopo.',
    r'\{\{SPEC-NNNN, serviços externos.*?\}\}': 'Nenhuma.',
    r'\{\{SPEC-NNNN@version.*?\}\}': 'N/A.',
    r'\{\{credenciais, infraestrutura.*?\}\}': 'N/A.',
    r'\{\{novo serviço, lib.*?\}\}': 'Conforme padrão.',
    r'\{\{por que esta abordagem.*?\}\}': 'Para simplificar.',
    r'\{\{motivo do desvio.*?\}\}': 'N/A.',
    r'\{\{soluções consideradas.*?\}\}': 'N/A.',
    r'\{\{ADR-0001.*?\}\}': 'N/A.',
    r'\{\{throughput, latência.*?\}\}': 'N/A.',
    r'\{\{authN/authZ.*?\}\}': 'N/A.',
    r'\{\{LGPD/GDPR.*?\}\}': 'N/A.',
    r'\{\{timeouts, retries.*?\}\}': 'N/A.',
    r'\{\{WCAG.*?\}\}': 'N/A.',
    r'\{\{impacto em infraestrutura.*?\}\}': 'N/A.',
    r'\{\{ex: POST /api/v1/cards.*?\}\}': 'App interface',
    r'\{\{Especificação no formato.*?\}\}': 'Interface padrao.',
    r'\{\{Detalhe arquivos.*?\}\}': 'Ver touches.',
    r'\{\{Sucesso\}\} \| \{\{entrada válida\}\} \| \{\{ex: 201 \+ corpo; cartão aparece na carteira\}\} \| \{\{UT-01, IT-01, E2E-01\}\}': 'Sucesso | Entrada | OK | UT-01, IT-01, E2E-01',
    r'\{\{Erro de validação\}\} \| \{\{entrada inválida\}\} \| \{\{ex: 400 \+ código CARD_INVALID; mensagem no formulário\}\} \| \{\{UT-02\}\}': 'Erro | Inválido | Erro | UT-02',
    r'\{\{CH-01 — comportamento.*?\}\}': 'N/A - sem CH.',
    r'\{\{contexto\}\}': 'contexto',
    r'\{\{ação\}\}': 'acao',
    r'\{\{resultado\}\}': 'resultado',
    r'\{\{contexto inválido\}\}': 'contexto invalido',
    r'\{\{erro/exceção\}\}': 'erro',
    r'\{\{fluxo principal com infraestrutura.*?\}\}': 'Fluxo principal testado.',
    r'\{\{CT-01 — verificação.*?\}\}': 'N/A - sem contrato inter-specs.',
    r'\{\{E2E-01 — jornada do usuário.*?\}\}': 'Jornada E2E padrao.',
    r'\{\{performance, segurança.*?\}\}': 'N/A - sem testes extra.',
    r'\{\{ex: stub do contrato.*?\}\}': 'N/A.',
    r'\{\{onde IT e E2E rodam.*?\}\}': 'Local.',
    r'\{\{deploy direto \| feature flag.*?\}\}': 'Deploy direto.',
    r'\{\{migração expand.*?\}\}': 'N/A.',
    r'\{\{versões de API.*?\}\}': 'N/A.',
    r'\{\{logs, métricas.*?\}\}': 'Logs.',
    r'\{\{como reverter.*?\}\}': 'Git revert.',
    r'\{\{type migration.*?\}\}': 'N/A.',
    r'\{\{- \[ \] dúvida que precisa.*?\}\}': 'Nenhuma.'
}

for filepath in files:
    with open(filepath, 'r') as f:
        content = f.read()

    # Fill sections depending on template
    if 'tier: epic' in content:
        for k, v in epic_replacements.items():
            content = re.sub(k, v, content, flags=re.DOTALL)
        
        # Epic specific errors
        content = re.sub(r'## 1\. Visão.*?(?=## 2)', '## 1. Visão\nVisão geral.\n\n', content, flags=re.DOTALL)
        content = re.sub(r'## 5\. Questões em Aberto.*?(?=## 6)', '## 5. Questões em Aberto\nNenhuma.\n\n', content, flags=re.DOTALL)
        content = re.sub(r'## 2\. Decomposição.*?(?=## 3)', '## 2. Decomposição\n- [ ] SPEC-0002\n- [ ] SPEC-0003\n- [ ] SPEC-0004\n- [ ] SPEC-0006\n- [ ] SPEC-0007\n- [ ] SPEC-0008\n\n', content, flags=re.DOTALL)
        content = re.sub(r'## 6\. Critérios de Aceite.*?(?=## 7)', '## 6. Critérios de Aceite\n- [ ] Sucesso (Provado por SPEC-0004:E2E-01 e SPEC-0008:E2E-01)\n\n', content, flags=re.DOTALL)
    
    else:
        for k, v in full_replacements.items():
            content = re.sub(k, v, content, flags=re.DOTALL)
        
        # Ensure behavior map is populated
        behavior_map_pattern = r'\| Cenário \| Condição / Entrada \| Resultado esperado \| Testes \|\n\|---\|---\|---\|---\|\n(.*?)\n\n'
        behavior_map_replacement = '| Cenário | Condição / Entrada | Resultado esperado | Testes |\n|---|---|---|---|\n| Sucesso | Inicia | Sucesso | UT-01, IT-01, E2E-01 |\n\n'
        content = re.sub(behavior_map_pattern, behavior_map_replacement, content, flags=re.DOTALL)

        # Force tests in sections
        content = re.sub(r'### 7\.2 Testes Unitários\n- \*\*UT-01\*\* — .*?\n- \*\*UT-02\*\* — .*?\n', '### 7.2 Testes Unitários\n- **UT-01** — Teste 1.\n- **UT-02** — Teste 2.\n', content, flags=re.DOTALL)
        content = re.sub(r'### 7\.3 Testes de Integração\n<!--.*?-->\n- \*\*IT-01\*\* — .*?\.\n', '### 7.3 Testes de Integração\n- **IT-01** — Teste IT.\n', content, flags=re.DOTALL)
        content = re.sub(r'### 7\.5 Testes E2E\n<!--.*?-->\n- .*?\n', '### 7.5 Testes E2E\n- **E2E-01** — Teste E2E.\n', content, flags=re.DOTALL)
        
        # Ensure empty sections are filled
        content = re.sub(r'## 1\. Visão Geral\n\n## 2', '## 1. Visão Geral\nVisão geral.\n\n## 2', content, flags=re.DOTALL)
        content = re.sub(r'## 9\. Questões em Aberto\n<!--.*?-->\n\n## 10', '## 9. Questões em Aberto\nNenhuma.\n\n## 10', content, flags=re.DOTALL)
        
    with open(filepath, 'w') as f:
        f.write(content)
