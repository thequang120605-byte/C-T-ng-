import re

path = 'wwwroot/css/game.css'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Let's remove everything from .chess-piece { to the end of the file and rewrite it
# Actually, it's safer to just find the start of .chess-piece { and replace up to /* MOVES */
start_idx = content.find('.chess-piece {')
end_idx = content.find('/* MOVES */')

if start_idx != -1 and end_idx != -1:
    new_css = '''
.chess-piece {
    cursor: pointer;
    transition: transform 0.2s ease, filter 0.2s ease;
}

.piece-inner-circle {
    fill: none;
    stroke: #F5DEB3;
    stroke-width: 3;
    pointer-events: none;
}

.piece-circle {
    fill: #E2C28E;
    stroke: #4a3022;
    stroke-width: 2;
    transition: all 0.2s ease;
    filter: drop-shadow(1px 2px 4px rgba(0,0,0,0.4));
}

.piece-text {
    font-size: 52px;
    font-weight: 900;
    font-family: "SimSun", "STSong", "Songti SC", serif;
    pointer-events: none;
}
.chess-piece.black .piece-text { fill: #111111; }
.chess-piece.red .piece-text { fill: #c00000; }

.chess-piece:hover .piece-circle {
    filter: drop-shadow(0 0 6px rgba(245, 230, 200, 0.8));
}

.chess-piece.selected .piece-circle {
    stroke: #10b981;
    stroke-width: 4;
    filter: drop-shadow(0 0 8px rgba(16, 185, 129, 0.6));
}

.chess-piece.in-check .piece-circle {
    stroke: #ef4444;
    stroke-width: 4;
    filter: drop-shadow(0 0 12px rgba(239, 68, 68, 0.8));
}

'''
    content = content[:start_idx] + new_css + content[end_idx:]
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)
    print('Fixed CSS')
