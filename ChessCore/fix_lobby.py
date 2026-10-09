import re

path = 'Views/Game/Index.cshtml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace my previous lobby panel with the one that matches game.js IDs
pattern = r'(<!-- ONLINE LOBBY -->).*?(<!-- THÔNG TIN ĐÃ CHỌN -->)'
replacement = r'''\1
            <div id="online-selection" class="difficulty-panel hidden">
                <h3><i class="fa-solid fa-network-wired"></i> Sảnh chờ Online</h3>
                <div class="lobby-actions" style="display:flex; flex-direction:column; gap:12px; margin-top:10px;">
                    <p style="font-size:14px; color:var(--text-muted);">Để trống và bấm nút để Tạo phòng, hoặc nhập mã để Vào phòng.</p>
                    <div style="display:flex; gap:8px;">
                        <input type="text" id="online-room-id" placeholder="Nhập mã phòng" style="flex:1; padding:10px; border-radius:8px; border:1px solid #475569; background:#1e293b; color:white; font-size:16px; text-align:center;" />
                        <button id="join-online-btn" class="btn-primary" style="background:#10b981; border:none; padding:10px 16px;">
                            <i class="fa-solid fa-paper-plane"></i> Đi
                        </button>
                    </div>
                </div>
            </div>

            \2'''
content = re.sub(pattern, replacement, content, flags=re.DOTALL)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Fixed IDs for Online Lobby')
