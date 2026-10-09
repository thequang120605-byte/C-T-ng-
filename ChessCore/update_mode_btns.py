import re

path = 'wwwroot/css/game.css'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

content = re.sub(r'(\.mode-buttons\s*\{.*?grid-template-columns:\s*)1fr 1fr(;\s*gap:\s*16px;.*?})', r'\1repeat(auto-fit, minmax(150px, 1fr))\2', content, flags=re.DOTALL)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated game.css for mode-buttons grid')
