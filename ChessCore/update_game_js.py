import re

path = 'wwwroot/js/game.js'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace the alert for creating room
pattern_create = r'alert\("Đã tạo phòng Online! ID Phòng của bạn là: " \+ gameId \+ "\\n\\nHãy gửi ID này cho người bạn muốn chơi cùng."\);'
replacement_create = r'''// alert replaced by UI
                  const roomBox = document.getElementById("room-info-box");
                  const roomCodeDisplay = document.getElementById("display-room-code");
                  if (roomBox && roomCodeDisplay) {
                      roomBox.classList.remove("hidden");
                      roomCodeDisplay.textContent = gameId;
                  }'''
content = re.sub(pattern_create, replacement_create, content)

# Replace the alert for joining room
pattern_join = r'alert\("Vào phòng thành công!"\);'
replacement_join = r'''// alert replaced by UI
                  const roomBox = document.getElementById("room-info-box");
                  const roomCodeDisplay = document.getElementById("display-room-code");
                  if (roomBox && roomCodeDisplay) {
                      roomBox.classList.remove("hidden");
                      roomCodeDisplay.textContent = gameId;
                  }'''
content = re.sub(pattern_join, replacement_join, content)

# Add event listener for copy button if not exists
if 'copy-room-code-btn' not in content:
    content += '''
document.addEventListener("DOMContentLoaded", function() {
    const copyBtn = document.getElementById("copy-room-code-btn");
    if (copyBtn) {
        copyBtn.addEventListener("click", function() {
            const code = document.getElementById("display-room-code").textContent;
            navigator.clipboard.writeText(code).then(() => {
                const originalText = copyBtn.innerHTML;
                copyBtn.innerHTML = '<i class="fa-solid fa-check"></i> Đã chép';
                copyBtn.style.color = '#10b981';
                setTimeout(() => {
                    copyBtn.innerHTML = originalText;
                    copyBtn.style.color = '';
                }, 2000);
            });
        });
    }
});
'''

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated game.js alerts to UI updates')
