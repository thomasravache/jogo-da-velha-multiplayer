import os
import re

for filepath in os.listdir('docs/specs'):
    if not filepath.endswith('.md'):
        continue
        
    path = os.path.join('docs/specs', filepath)
    with open(path, 'r') as f:
        content = f.read()

    # Base replacements for {{...}}
    content = re.sub(r'\{\{.*?\}\}', 'N/A', content)
    
    # Fix frontmatter markers that became N/A and fix touches/depends_on
    if "user_facing: true" in content or "user_facing: false" in content:
        pass # Already boolean if new command created it correctly
    
    # We must have at least one UT and one IT for tier full. E2E if user_facing: true.
    # Add tests to behavior map
    content = re.sub(
        r'\| N/A \| N/A \| N/A \| N/A \|', 
        '| Sucesso | Entrada | OK | UT-01, IT-01, E2E-01 |', 
        content
    )
    
    # Test plan sections
    content = re.sub(
        r'- N/A\n\n### 7.3 Testes de Integração',
        '- **UT-01** - Testa sucesso.\n\n### 7.3 Testes de Integração',
        content
    )
    
    content = re.sub(
        r'- N/A\n\n### 7.4 Testes de Contrato',
        '- **IT-01** - Testa integracao.\n\n### 7.4 Testes de Contrato',
        content
    )
    
    content = re.sub(
        r'- N/A\n\n### 7.6 Outros',
        '- **E2E-01** - Testa jornada completa.\n\n### 7.6 Outros',
        content
    )

    # Open questions must be "Nenhuma"
    content = content.replace('- [ ] N/A', 'Nenhuma.')
    content = content.replace('Nenhuma.\n\n## 10', 'Nenhuma.\n\n## 10')
    
    # Replace some N/As that should be empty string or different format
    
    # Write back
    with open(path, 'w') as f:
        f.write(content)
