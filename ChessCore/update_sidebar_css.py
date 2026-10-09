import re

path = 'wwwroot/css/site.css'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace the responsive section
pattern = r'(@media \(max-width: 900px\) \{.*?)(?=\n$|\Z)'
replacement = r'''@media (max-width: 900px) {
    .app-sidebar {
        transform: translateX(-100%);
        transition: transform 0.3s ease;
        position: fixed;
        left: 0;
        top: 0;
        bottom: 0;
        z-index: 1000;
        box-shadow: 5px 0 15px rgba(0,0,0,0.5);
    }
    
    .app-sidebar.open {
        transform: translateX(0);
    }
    
    .mobile-menu-toggle {
        display: block;
    }
    
    .app-navbar {
        padding: 0 16px;
    }
}

.sidebar-backdrop {
    display: none;
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0,0,0,0.6);
    z-index: 999;
    opacity: 0;
    transition: opacity 0.3s ease;
}

.sidebar-backdrop.show {
    display: block;
    opacity: 1;
}
'''
content = re.sub(pattern, replacement, content, flags=re.DOTALL)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated site.css for sidebar')
