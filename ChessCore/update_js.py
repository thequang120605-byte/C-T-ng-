import re

def update_js_pieces(path):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    if 'innerCircle' in content:
        return
        
    # Find the piece creation logic
    pattern = r'(const circle = document\.createElementNS\(SVG_NS, "circle"\);\s*circle\.setAttribute\("cx", \w+\.x\);\s*circle\.setAttribute\("cy", \w+\.y\);\s*circle\.setAttribute\("r", 42\);\s*circle\.setAttribute\("class", "piece-circle"\);\s*pieceGroup\.appendChild\(circle\);)'
    
    # Replacement adds an inner circle
    replacement = r'''\1
    const innerCircle = document.createElementNS(SVG_NS, "circle");
    innerCircle.setAttribute("cx", position.x);
    innerCircle.setAttribute("cy", position.y);
    innerCircle.setAttribute("r", 36);
    innerCircle.setAttribute("class", "piece-inner-circle");
    pieceGroup.appendChild(innerCircle);'''
    
    new_content = re.sub(pattern, replacement, content)
    
    with open(path, 'w', encoding='utf-8') as f:
        f.write(new_content)

update_js_pieces('wwwroot/js/game.js')
update_js_pieces('wwwroot/js/puzzle.js')
print('Updated js')
