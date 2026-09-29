import re

def fix_epic(path, specs):
    with open(path, 'r') as f:
        content = f.read()
    
    content = re.sub(r'## 8\. Questões em Aberto\n.*?(?=\n## 9)', '## 8. Questões em Aberto\nNenhuma.\n', content, flags=re.DOTALL)
    
    table_rows = []
    for spec in specs:
        table_rows.append(f'| {spec} | Titulo | full | feature | S | | |')
    table_body = '\n'.join(table_rows)
    
    content = re.sub(r'\| Spec \| Título \| Tier \| Tipo \| Tamanho \| Depende de \| Consome contrato de \|\n\|---\|---\|---\|---\|---\|---\|---\|\n\|  \|  \|  \|  \|  \|  \|  \|',
                     f'| Spec | Título | Tier | Tipo | Tamanho | Depende de | Consome contrato de |\n|---|---|---|---|---|---|---|\n{table_body}', content)
                     
    content = re.sub(r'## 7\. Critérios de Aceite do Épico\n.*?(?=\n## 8)', f'## 7. Critérios de Aceite do Épico\n- [ ] Sucesso ({specs[-1]}:E2E-01)\n', content, flags=re.DOTALL)
    
    with open(path, 'w') as f:
        f.write(content)

fix_epic('docs/specs/SPEC-0001-fundacao-do-projeto.md', ['SPEC-0002', 'SPEC-0003', 'SPEC-0004'])
fix_epic('docs/specs/SPEC-0005-jogo-da-velha.md', ['SPEC-0006', 'SPEC-0007', 'SPEC-0008'])
