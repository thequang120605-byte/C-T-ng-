import re
path = 'wwwroot/css/dashboard.css'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace('color: white;', 'color: var(--text-main);')
content = content.replace('color: #fff;', 'color: var(--text-main);')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
