import re

path = 'wwwroot/js/game.js'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# For .style.display = "block", add .classList.remove("hidden")
pattern_block = r'(\w+)\.style\.display\s*=\s*"block";'
replacement_block = r'\1.style.display = "block"; \1.classList.remove("hidden");'
content = re.sub(pattern_block, replacement_block, content)

# For .style.display = "none", add .classList.add("hidden")
pattern_none = r'(\w+)\.style\.display\s*=\s*"none";'
replacement_none = r'\1.style.display = "none"; \1.classList.add("hidden");'
content = re.sub(pattern_none, replacement_none, content)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated style.display logic in game.js')
