def get_star_marks():
    # Helper to draw a single cross-hair mark
    def draw_mark(x, y, is_left, is_right, is_top, is_bottom):
        paths = []
        d = 5  # distance from center
        l = 15 # length of the line
        
        # Top Left
        if is_left and is_top:
            paths.append(f'M {x-d} {y-d-l} L {x-d} {y-d} L {x-d-l} {y-d}')
        # Top Right
        if is_right and is_top:
            paths.append(f'M {x+d} {y-d-l} L {x+d} {y-d} L {x+d+l} {y-d}')
        # Bottom Left
        if is_left and is_bottom:
            paths.append(f'M {x-d} {y+d+l} L {x-d} {y+d} L {x-d-l} {y+d}')
        # Bottom Right
        if is_right and is_bottom:
            paths.append(f'M {x+d} {y+d+l} L {x+d} {y+d} L {x+d+l} {y+d}')
            
        return ' '.join(paths)

    marks = []
    # Pawns
    for y in [350, 650]:
        for x in [50, 250, 450, 650, 850]:
            left = x > 50
            right = x < 850
            marks.append(draw_mark(x, y, left, right, True, True))
            
    # Cannons
    for y in [250, 750]:
        for x in [150, 750]:
            marks.append(draw_mark(x, y, True, True, True, True))
            
    return f'<path d=\"{" ".join(marks)}\" stroke=\"#4a3022\" stroke-width=\"2\" fill=\"none\" />'

marks_svg = get_star_marks()
print(marks_svg)
