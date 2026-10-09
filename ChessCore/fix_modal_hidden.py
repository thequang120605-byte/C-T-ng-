import re

path = 'wwwroot/js/game.js'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace('.classList.add("show")', '.classList.remove("hidden")')
content = content.replace('.classList.remove("show")', '.classList.add("hidden")')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Fixed modal hidden/show logic in game.js')
