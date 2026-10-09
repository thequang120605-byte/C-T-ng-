import re

path = 'Views/Shared/_Layout.cshtml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace <a href="#" class="nav-item"> ... <span>Thống kê</span>
pattern = r'<a href="#" class="nav-item">\s*<i class="fa-solid fa-chart-simple"></i>\s*<span>Thống kê</span>\s*</a>'
replacement = r'<a href="/Dashboard" class="nav-item">\n                    <i class="fa-solid fa-chart-simple"></i>\n                    <span>Thống kê</span>\n                </a>'
content = re.sub(pattern, replacement, content)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated _Layout.cshtml with Dashboard link')
