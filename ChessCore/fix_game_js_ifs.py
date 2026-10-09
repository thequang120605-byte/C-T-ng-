import re

path = 'wwwroot/js/game.js'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix the broken if statements: if (var) var.style.display = "none"; var.classList.add("hidden");
# become: if (var) { var.style.display = "none"; var.classList.add("hidden"); }
pattern_broken_none = r'if\s*\(\s*(\w+)\s*\)\s*\1\.style\.display\s*=\s*"none";\s*\1\.classList\.add\("hidden"\);'
replacement_none = r'if (\1) { \1.style.display = "none"; \1.classList.add("hidden"); }'
content = re.sub(pattern_broken_none, replacement_none, content)

pattern_broken_block = r'if\s*\(\s*(\w+)\s*\)\s*\1\.style\.display\s*=\s*"block";\s*\1\.classList\.remove\("hidden"\);'
replacement_block = r'if (\1) { \1.style.display = "block"; \1.classList.remove("hidden"); }'
content = re.sub(pattern_broken_block, replacement_block, content)

# Also fix the multi-line ones that DO have curly braces, my regex might have replaced them cleanly, but they are already fine.
# Wait, let's just do a broad replacement: if (something) something.style.display = ...; something.classList...;
# Actually, the regex above exactly targets if (var) var.style.display = "none"; var.classList.add("hidden");

# But what if there was NO if statement? Like difficultySelection.style.display = "block"; difficultySelection.classList.remove("hidden");
# They are fine.

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Fixed game.js if statements')
