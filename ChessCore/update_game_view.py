import re

path = 'Views/Game/Index.cshtml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add the online button if not already there
if 'player-vs-online-btn' not in content:
    pattern_btn = r'(<button id="player-vs-computer-btn".*?</button>)'
    replacement_btn = r'''\1

                <!-- CHƠI QUA MẠNG -->
                <button id="player-vs-online-btn" class="mode-btn">
                    <i class="fa-solid fa-globe"></i>
                    <span class="mode-title">Chơi qua mạng</span>
                    <span class="mode-subtitle">Tạo phòng chơi với bạn bè</span>
                </button>'''
    content = re.sub(pattern_btn, replacement_btn, content, flags=re.DOTALL)

# Add the lobby panel right after the difficulty-selection panel
if 'online-lobby-panel' not in content:
    pattern_panel = r'(</div>\s*</div>\s*<!-- THÔNG TIN ĐÃ CHỌN -->)'
    replacement_panel = r'''</div>

            <!-- ONLINE LOBBY -->
            <div id="online-lobby-panel" class="difficulty-panel hidden">
                <h3><i class="fa-solid fa-network-wired"></i> Sảnh chờ Online</h3>
                <div class="lobby-actions" style="display:flex; flex-direction:column; gap:12px; margin-top:10px;">
                    <button id="create-room-btn" class="btn-primary" style="width:100%;">
                        <i class="fa-solid fa-plus"></i> Tạo phòng mới
                    </button>
                    <div style="text-align:center; color:var(--text-muted); font-size:14px;">Hoặc tham gia phòng</div>
                    <div style="display:flex; gap:8px;">
                        <input type="text" id="join-room-code" placeholder="Nhập mã" style="flex:1; padding:10px; border-radius:8px; border:1px solid #475569; background:#1e293b; color:white; font-size:16px; text-transform:uppercase; text-align:center;" maxlength="6" />
                        <button id="join-room-btn" class="btn-primary" style="background:#10b981; border:none; padding:10px 16px;">Vào</button>
                    </div>
                </div>
            </div>
            
            <!-- WAITING FOR OPPONENT SCREEN -->
            <div id="waiting-room-panel" class="difficulty-panel hidden" style="text-align:center;">
                <h3><i class="fa-solid fa-spinner fa-spin"></i> Đang chờ đối thủ</h3>
                <p style="margin-top:10px; color:var(--text-muted);">Mã phòng của bạn:</p>
                <div style="font-size:32px; font-weight:bold; color:var(--accent-gold); letter-spacing:6px; margin:10px 0;" id="current-room-code">----</div>
                <button id="copy-room-btn" class="btn-secondary" style="font-size:12px; padding:6px 12px; margin-bottom:12px; width:auto; display:inline-block;">
                    <i class="fa-regular fa-copy"></i> Sao chép
                </button>
                <p class="mode-desc" style="font-size:12px; margin-bottom:10px;">Gửi mã này cho bạn bè để tham gia trận đấu.</p>
                <button id="cancel-room-btn" class="btn-secondary" style="width:100%; margin-top:10px;">
                    Hủy phòng
                </button>
            </div>

            <!-- THÔNG TIN ĐÃ CHỌN -->'''
    content = re.sub(pattern_panel, replacement_panel, content)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated Views/Game/Index.cshtml')
