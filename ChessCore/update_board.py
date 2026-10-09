import re

def update_svg(path):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    if 'board-inner-border' in content:
        return
        
    pattern = r'(<rect x="20" y="20" width="860" height="960" class="board-background" />)'
    replacement = r'''<rect x="0" y="0" width="900" height="1000" class="board-outer-bg" fill="#3E2723" />
                        <rect x="15" y="15" width="870" height="970" class="board-background" fill="#E2C28E" />
                        <rect x="45" y="45" width="810" height="910" class="board-inner-border" fill="none" stroke="#5D4037" stroke-width="4" />'''
    
    new_content = re.sub(pattern, replacement, content)
    
    with open(path, 'w', encoding='utf-8') as f:
        f.write(new_content)

update_svg('Views/Game/Index.cshtml')
update_svg('Views/Puzzle/Index.cshtml')
