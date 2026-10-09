import re

path = 'wwwroot/css/game.css'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Update piece-circle
content = re.sub(r'\.piece-circle\s*\{[^}]+\}', '.piece-circle {\n    fill: #E2C28E;\n    stroke: #4a3022;\n    stroke-width: 2;\n    transition: all 0.2s ease;\n    filter: drop-shadow(2px 4px 6px rgba(0,0,0,0.4));\n}', content, count=1)

# Add inner circle
if '.piece-inner-circle' not in content:
    content = content.replace('.piece-circle {', '.piece-inner-circle {\n    fill: none;\n    stroke: #F5DEB3;\n    stroke-width: 3;\n    pointer-events: none;\n}\n\n.piece-circle {')

# Update piece-text
content = re.sub(r'\.piece-text\s*\{[^}]+\}', '.piece-text {\n    font-size: 52px;\n    font-weight: 900;\n    font-family: "SimSun", "STSong", "Songti SC", serif;\n    pointer-events: none;\n}', content)

content = re.sub(r'\.chess-piece\.black \.piece-text\s*\{[^}]+\}', '.chess-piece.black .piece-text { fill: #0f0f0f; }', content)
content = re.sub(r'\.chess-piece\.red \.piece-text\s*\{[^}]+\}', '.chess-piece.red .piece-text { fill: #c00000; text-shadow: 0 0 1px rgba(192,0,0,0.5); }', content)

# Also fix the hover/selected states for piece-circle
content = re.sub(r'\.chess-piece:hover \.piece-circle\s*\{[^}]+\}', '.chess-piece:hover .piece-circle {\n    filter: drop-shadow(0 0 10px rgba(226,194,142,1));\n}', content)
content = re.sub(r'\.chess-piece\.selected \.piece-circle\s*\{[^}]+\}', '.chess-piece.selected .piece-circle {\n    stroke: #10b981;\n    stroke-width: 4;\n    filter: drop-shadow(0 0 15px rgba(16, 185, 129, 0.8));\n}', content)
content = re.sub(r'\.chess-piece\.in-check \.piece-circle\s*\{[^}]+\}', '.chess-piece.in-check .piece-circle {\n    stroke: #ef4444;\n    stroke-width: 4;\n    filter: drop-shadow(0 0 15px rgba(239, 68, 68, 0.8));\n}', content)


with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated css')
