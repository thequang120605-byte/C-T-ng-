import re

path = 'Views/Game/Index.cshtml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add room-info-box in control-column
pattern = r'(<div class="panel-box game-status-box">.*?</div>)'
replacement = r'''\1

            <div id="room-info-box" class="panel-box room-info-box hidden" style="border: 1px solid var(--accent-gold); background: rgba(212, 175, 55, 0.05);">
                <div class="status-header" style="color: var(--accent-gold);"><i class="fa-solid fa-globe"></i> Phòng Online</div>
                <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 8px;">
                    <span style="font-size: 14px; color: var(--text-muted);">Mã phòng:</span>
                    <strong id="display-room-code" style="font-size: 18px; letter-spacing: 2px; color: white;">----</strong>
                </div>
                <button id="copy-room-code-btn" class="btn-secondary" style="width: 100%; margin-top: 10px; padding: 6px; font-size: 12px;">
                    <i class="fa-regular fa-copy"></i> Sao chép mã
                </button>
            </div>'''

if 'id="room-info-box"' not in content:
    content = re.sub(pattern, replacement, content, flags=re.DOTALL)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated Index.cshtml with room info box')
