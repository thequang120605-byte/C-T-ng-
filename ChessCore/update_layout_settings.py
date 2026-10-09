import re

path = 'Views/Shared/_Layout.cshtml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace Theme buttons with data-theme
# <button class="theme-btn active"> ... </button>
pattern_wood = r'<button class="theme-btn active">\s*<div class="theme-circle" style="background: linear-gradient\(135deg, #c99b62, #5c4033\);"></div>\s*<span>Gỗ Cổ Điển</span>\s*</button>'
replacement_wood = r'<button class="theme-btn active" data-theme="wood"><div class="theme-circle" style="background: linear-gradient(135deg, #c99b62, #5c4033);"></div><span>Gỗ Cổ Điển</span></button>'
content = re.sub(pattern_wood, replacement_wood, content)

pattern_night = r'<button class="theme-btn">\s*<div class="theme-circle" style="background: #1e293b;"></div>\s*<span>Night Mode</span>\s*</button>'
replacement_night = r'<button class="theme-btn" data-theme="night"><div class="theme-circle" style="background: #1e293b;"></div><span>Night Mode</span></button>'
content = re.sub(pattern_night, replacement_night, content)

pattern_light = r'<button class="theme-btn">\s*<div class="theme-circle" style="background: #e2e8f0; border-color: #94a3b8;"></div>\s*<span>Sáng Sủa</span>\s*</button>'
replacement_light = r'<button class="theme-btn" data-theme="light"><div class="theme-circle" style="background: #e2e8f0; border-color: #94a3b8;"></div><span>Sáng Sủa</span></button>'
content = re.sub(pattern_light, replacement_light, content)

# Add IDs to inputs
content = content.replace('<input type="checkbox" checked>', '<input type="checkbox" checked id="setting-sound">', 1)
content = content.replace('<input type="checkbox" checked>', '<input type="checkbox" checked id="setting-hint">', 1)
content = content.replace('<select class="setting-select">', '<select class="setting-select" id="setting-lang">')

# Add settings.js before </body>
content = content.replace('</body>', '    <script src="~/js/settings.js" asp-append-version="true"></script>\n</body>')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated _Layout.cshtml with logic IDs')
