import re

path = 'wwwroot/css/game.css'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Update piece styling
content = re.sub(r'\.piece-circle\s*\{[^}]+\}', '.piece-circle {\n    fill: #f7e8cf;\n    stroke: #764b30;\n    stroke-width: 4;\n    transition: all 0.2s ease;\n    filter: drop-shadow(0 4px 4px rgba(0,0,0,0.3));\n}', content)

content = re.sub(r'\.piece-text\s*\{[^}]+\}', '.piece-text {\n    font-size: 52px;\n    font-weight: 800;\n    font-family: "KaiTi", "STKaiti", serif;\n    pointer-events: none;\n}', content)

content = re.sub(r'\.chess-piece\.black \.piece-text\s*\{[^}]+\}', '.chess-piece.black .piece-text { fill: #111111; }', content)
content = re.sub(r'\.chess-piece\.red \.piece-text\s*\{[^}]+\}', '.chess-piece.red .piece-text { fill: #cc0000; }', content)

# Also update the board container border to avoid double borders since we added an outer rect in SVG
content = re.sub(r'border: 10px solid var\(--wood-dark\);', 'border: none;', content)
content = re.sub(r'background: var\(--wood-light\);', 'background: transparent;', content)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated game.css')
