document.addEventListener('DOMContentLoaded', function() {
    const themeBtns = document.querySelectorAll('.theme-btn');
    const soundToggle = document.getElementById('setting-sound');
    const hintToggle = document.getElementById('setting-hint');
    const langSelect = document.getElementById('setting-lang');

    // Tải cấu hình từ localStorage
    const savedTheme = localStorage.getItem('chess_theme') || 'light';
    const savedSound = localStorage.getItem('chess_sound') !== 'false';
    const savedHint = localStorage.getItem('chess_hint') !== 'false';
    const savedLang = localStorage.getItem('chess_lang') || 'vi';

    // Áp dụng cấu hình ban đầu
    applyTheme(savedTheme);
    if (typeof updateLanguageText === 'function') { updateLanguageText(savedLang); }
    if(soundToggle) soundToggle.checked = savedSound;
    if(hintToggle) hintToggle.checked = savedHint;
    if(langSelect) langSelect.value = savedLang;

    // Bắt sự kiện Theme
    themeBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const theme = this.getAttribute('data-theme');
            applyTheme(theme);
            localStorage.setItem('chess_theme', theme);
        });
    });

    // Bắt sự kiện Sound & Hint
    if(soundToggle) {
        soundToggle.addEventListener('change', function() {
            localStorage.setItem('chess_sound', this.checked);
        });
    }

    if(hintToggle) {
        hintToggle.addEventListener('change', function() {
            localStorage.setItem('chess_hint', this.checked);
            
            // Xử lý ẩn hiện chấm gợi ý trên bàn cờ nếu đang ở trang chơi game
            const validMoves = document.querySelectorAll('.valid-move-marker');
            if(!this.checked) {
                validMoves.forEach(m => m.style.display = 'none');
            }
        });
    }

    // Bắt sự kiện Ngôn ngữ
    if(langSelect) {
        langSelect.addEventListener('change', function() {
            localStorage.setItem('chess_lang', this.value);
            if (typeof updateLanguageText === 'function') { updateLanguageText(this.value); }
        });
    }

    function applyTheme(theme) {
        // Cập nhật UI button
        themeBtns.forEach(btn => {
            if(btn.getAttribute('data-theme') === theme) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });

        // Áp dụng CSS variables
        document.body.setAttribute('data-theme', theme);
    }
});
