import glob
import re

files = glob.glob('docs/specs/SPEC-*.md')

for filepath in files:
    with open(filepath, 'r') as f:
        content = f.read()

    # Epic
    if 'tier: epic' in content:
        content = re.sub(r'## 1\. Visão\n', '## 1. Visão\nVisão.\n', content)
        content = re.sub(r'## 5\. Questões em Aberto\n', '## 5. Questões em Aberto\nNenhuma.\n', content)
        content = re.sub(r'## 2\. Decomposição\n', '## 2. Decomposição\n- [ ] SPEC-0002\n- [ ] SPEC-0003\n- [ ] SPEC-0004\n- [ ] SPEC-0006\n- [ ] SPEC-0007\n- [ ] SPEC-0008\n', content)
        content = re.sub(r'## 6\. Critérios de Aceite\n', '## 6. Critérios de Aceite\n- [ ] Tudo pronto (Provado por SPEC-0004:E2E-01 e SPEC-0008:E2E-01)\n', content)
        
    # Full
    else:
        content = re.sub(r'## 1\. Visão Geral\n', '## 1. Visão Geral\nVisão Geral.\n', content)
        content = re.sub(r'## 9\. Questões em Aberto\n', '## 9. Questões em Aberto\nNenhuma.\n', content)
        
    with open(filepath, 'w') as f:
        f.write(content)
