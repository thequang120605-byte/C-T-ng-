import re

path = 'wwwroot/js/game.js'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

content = content.replace('.difficulty-option', '.diff-btn')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Fixed difficulty button class in game.js')
