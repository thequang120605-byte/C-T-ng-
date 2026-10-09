import re
path = 'wwwroot/js/game.js'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# In game.js, showValidMoves creates the markers.
# function showValidMoves(moves) {
pattern_moves = r'(function showValidMoves\(moves\)\s*\{)'
replacement_moves = r'\1\n    if (localStorage.getItem("chess_hint") === "false") return;'

content = re.sub(pattern_moves, replacement_moves, content)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
