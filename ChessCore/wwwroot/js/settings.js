document.addEventListener('DOMContentLoaded', function() {
    const themeBtns = document.querySelectorAll('.theme-options:not(.board-skin-options):not(.piece-skin-options) .theme-btn');
    const boardSkinBtns = document.querySelectorAll('.board-skin-btn');
    const pieceSkinBtns = document.querySelectorAll('.piece-skin-btn');
    const soundToggle = document.getElementById('setting-sound');
    const hintToggle = document.getElementById('setting-hint');
    const langSelect = document.getElementById('setting-lang');

    // Tải cấu hình từ localStorage
    const savedTheme = localStorage.getItem('chess_theme') || 'wood';
    const savedBoardSkin = localStorage.getItem('chess_board_skin') || 'vector';
    const savedPieceSkin = localStorage.getItem('chess_piece_skin') || 'traditional';
    const savedSound = localStorage.getItem('chess_sound') !== 'false';
    const savedHint = localStorage.getItem('chess_hint') !== 'false';
    const savedLang = localStorage.getItem('chess_lang') || 'vi';

    // Xóa triệt để các key wallpaper cũ nếu có trong localStorage
    localStorage.removeItem('chess_wallpaper');
    localStorage.removeItem('chess_custom_wallpaper');
    document.body.classList.remove('has-custom-wallpaper');
    document.documentElement.style.removeProperty('--game-bg-image');

    // Áp dụng cấu hình ban đầu
    applyTheme(savedTheme);
    applyBoardSkin(savedBoardSkin);
    applyPieceSkin(savedPieceSkin);
    if (typeof updateLanguageText === 'function') { updateLanguageText(savedLang); }
    if(soundToggle) soundToggle.checked = savedSound;
    if(hintToggle) hintToggle.checked = savedHint;
    if(langSelect) langSelect.value = savedLang;

    // Bắt sự kiện Theme Màu Nền
    themeBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const theme = this.getAttribute('data-theme');
            applyTheme(theme);
            localStorage.setItem('chess_theme', theme);
        });
    });

    // Bắt sự kiện Chất liệu Bàn Cờ
    boardSkinBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const skin = this.getAttribute('data-board-skin');
            applyBoardSkin(skin);
            localStorage.setItem('chess_board_skin', skin);
        });
    });

    // Bắt sự kiện Kiểu Quân Cờ
    pieceSkinBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const skin = this.getAttribute('data-piece-skin');
            applyPieceSkin(skin);
            localStorage.setItem('chess_piece_skin', skin);
        });
    });

    // Bắt sự kiện Sound & Hint
    const voiceToggle = document.getElementById('setting-voice');
    const savedVoice = localStorage.getItem('chess_voice') !== 'false';
    if(voiceToggle) voiceToggle.checked = savedVoice;

    if(soundToggle) {
        soundToggle.addEventListener('change', function() {
            localStorage.setItem('chess_sound', this.checked);
        });
    }

    if(voiceToggle) {
        voiceToggle.addEventListener('change', function() {
            localStorage.setItem('chess_voice', this.checked);
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
        themeBtns.forEach(btn => {
            if(btn.getAttribute('data-theme') === theme) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
        document.body.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-theme', theme);
    }

    function applyBoardSkin(skin) {
        boardSkinBtns.forEach(btn => {
            if(btn.getAttribute('data-board-skin') === skin) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });

        const textureImg = document.getElementById('board-image-texture');
        const boardLines = document.querySelectorAll('.board-lines, .river-text, .palace-lines, .board-inner-border');

        let src = "";
        if (skin === 'wood') {
            src = "/images/boards/Wood-grain Chessboard.png";
        } else if (skin === 'stone') {
            src = "/images/boards/Stone-textured Chessboard.png";
        } else if (skin === 'cardboard') {
            src = "/images/boards/Simple Brown Cardboard Chessboard.png";
        } else if (skin === 'minimalist') {
            src = "/images/boards/Minimalist White Chessboard.png";
        }

        if (src && textureImg) {
            textureImg.setAttribute('href', src);
            textureImg.setAttributeNS('http://www.w3.org/1999/xlink', 'xlink:href', src);
            textureImg.style.display = 'block';
            // Khi dùng ảnh bàn cờ có sẵn lưới và chữ Hán, ẩn hoàn toàn lưới vector để không bị đè nét, lệch hàng hay lộ nét đứt
            boardLines.forEach(el => {
                el.style.opacity = '0';
                el.style.display = 'none';
            });
        } else {
            if (textureImg) textureImg.style.display = 'none';
            // Hiện lại lưới vector chuẩn nét
            boardLines.forEach(el => {
                el.style.opacity = '1';
                el.style.display = '';
            });
        }
    }

    function applyPieceSkin(skin) {
        pieceSkinBtns.forEach(btn => {
            if(btn.getAttribute('data-piece-skin') === skin) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });

        window.currentPieceSkin = skin;
        // Phát sự kiện để game.js và puzzle.js tự động re-render quân cờ ngay lập tức
        window.dispatchEvent(new CustomEvent('chess-skin-changed', { detail: { skin: skin } }));
    }
});
