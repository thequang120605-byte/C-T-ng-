import re

path = 'Views/Shared/_Layout.cshtml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# 1. Add settings.css after site.css
content = content.replace('<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />', '<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />\n    <link rel="stylesheet" href="~/css/settings.css" asp-append-version="true" />')

# 2. Update 'Cài đặt' nav-item to open modal
# <a href="#" class="nav-item">
#     <i class="fa-solid fa-gear"></i>
#     <span>Cài đặt</span>
# </a>
pattern_nav = r'<a href="#" class="nav-item">\s*<i class="fa-solid fa-gear"></i>\s*<span>Cài đặt</span>\s*</a>'
replacement_nav = r'''<a href="#" class="nav-item" id="nav-settings-btn" onclick="event.preventDefault(); document.getElementById('settings-modal').classList.add('show');">
                    <i class="fa-solid fa-gear"></i>
                    <span>Cài đặt</span>
                </a>'''
content = re.sub(pattern_nav, replacement_nav, content)

# 3. Inject settings modal before </body>
modal_html = '''
    <!-- SETTINGS MODAL -->
    <div id="settings-modal" class="settings-modal">
        <div class="settings-content">
            <div class="settings-header">
                <h2><i class="fa-solid fa-sliders"></i> Cài đặt hệ thống</h2>
                <button class="close-settings" onclick="document.getElementById('settings-modal').classList.remove('show');"><i class="fa-solid fa-xmark"></i></button>
            </div>
            
            <div class="settings-body">
                <!-- GIAO DIỆN BÀN CỜ -->
                <div class="setting-group">
                    <div class="setting-title">Giao diện (Đang phát triển)</div>
                    <div class="theme-options">
                        <button class="theme-btn active">
                            <div class="theme-circle" style="background: linear-gradient(135deg, #c99b62, #5c4033);"></div>
                            <span>Gỗ Cổ Điển</span>
                        </button>
                        <button class="theme-btn">
                            <div class="theme-circle" style="background: #1e293b;"></div>
                            <span>Night Mode</span>
                        </button>
                        <button class="theme-btn">
                            <div class="theme-circle" style="background: #e2e8f0; border-color: #94a3b8;"></div>
                            <span>Sáng Sủa</span>
                        </button>
                    </div>
                </div>

                <!-- TÙY CHỈNH CHUNG -->
                <div class="setting-group">
                    <div class="setting-title">Tùy chỉnh chung</div>
                    
                    <div class="setting-item">
                        <div class="setting-label">
                            <i class="fa-solid fa-volume-high"></i> Âm thanh trò chơi
                        </div>
                        <label class="switch">
                            <input type="checkbox" checked>
                            <span class="slider"></span>
                        </label>
                    </div>
                    
                    <div class="setting-item">
                        <div class="setting-label">
                            <i class="fa-solid fa-language"></i> Ngôn ngữ
                        </div>
                        <select class="setting-select">
                            <option value="vi" selected>Tiếng Việt</option>
                            <option value="en">English</option>
                            <option value="zh">中文 (Chinese)</option>
                        </select>
                    </div>

                    <div class="setting-item">
                        <div class="setting-label">
                            <i class="fa-solid fa-chess-knight"></i> Hiện gợi ý nước đi
                        </div>
                        <label class="switch">
                            <input type="checkbox" checked>
                            <span class="slider"></span>
                        </label>
                    </div>
                </div>
            </div>

            <div class="settings-footer">
                <button class="btn-save-settings" onclick="document.getElementById('settings-modal').classList.remove('show');">Lưu cài đặt</button>
            </div>
        </div>
    </div>
'''

content = content.replace('</body>', modal_html + '\n</body>')

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated _Layout.cshtml with Settings Modal')
