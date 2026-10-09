import re

path = 'Views/Shared/_Layout.cshtml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add backdrop before sidebar
pattern_sidebar = r'(<aside class="app-sidebar" id="app-sidebar">|<aside class="app-sidebar">)'
replacement_sidebar = r'''<div class="sidebar-backdrop" id="sidebar-backdrop"></div>
        <aside class="app-sidebar" id="app-sidebar">'''
if 'id="sidebar-backdrop"' not in content:
    content = re.sub(pattern_sidebar, replacement_sidebar, content)

# Add toggle id to mobile-menu-toggle
pattern_toggle = r'<div class="mobile-menu-toggle">'
replacement_toggle = r'<div class="mobile-menu-toggle" id="mobile-menu-toggle">'
if 'id="mobile-menu-toggle"' not in content:
    content = content.replace(pattern_toggle, replacement_toggle)

# Add JS script at the end
script_js = '''
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            const toggleBtn = document.getElementById('mobile-menu-toggle');
            const sidebar = document.getElementById('app-sidebar');
            const backdrop = document.getElementById('sidebar-backdrop');
            
            if (toggleBtn && sidebar && backdrop) {
                toggleBtn.addEventListener('click', function() {
                    sidebar.classList.add('open');
                    backdrop.classList.add('show');
                });
                
                backdrop.addEventListener('click', function() {
                    sidebar.classList.remove('open');
                    backdrop.classList.remove('show');
                });
            }
        });
    </script>
'''

if 'mobile-menu-toggle' in script_js and 'document.getElementById(\'mobile-menu-toggle\')' not in content:
    content = content.replace('</body>', script_js + '\n</body>')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated _Layout.cshtml for sidebar')
