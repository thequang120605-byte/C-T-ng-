const i18n = {
    'vi': {
        'menu_home': 'Trang chủ',
        'menu_play': 'Chơi cờ',
        'menu_puzzle': 'Cờ thế',
        'menu_stats': 'Thống kê',
        'menu_course': 'Khóa học',
        'menu_settings': 'Cài đặt',
        'settings_title': 'Cài đặt hệ thống',
        'settings_theme_title': 'Giao diện màu sắc',
        'theme_wood': 'Gỗ Cổ Điển',
        'theme_night': 'Night Mode',
        'theme_light': 'Sáng Sủa',
        'settings_general': 'Tùy chỉnh chung',
        'setting_sound': 'Âm thanh trò chơi',
        'setting_lang': 'Ngôn ngữ',
        'setting_hint': 'Hiện gợi ý nước đi',
        'settings_save': 'Lưu cài đặt'
    },
    'en': {
        'menu_home': 'Home',
        'menu_play': 'Play Chess',
        'menu_puzzle': 'Puzzles',
        'menu_stats': 'Statistics',
        'menu_course': 'Courses',
        'menu_settings': 'Settings',
        'settings_title': 'System Settings',
        'settings_theme_title': 'Theme',
        'theme_wood': 'Classic Wood',
        'theme_night': 'Night Mode',
        'theme_light': 'Light Mode',
        'settings_general': 'General Settings',
        'setting_sound': 'Game Sound',
        'setting_lang': 'Language',
        'setting_hint': 'Show Move Hints',
        'settings_save': 'Save Settings'
    },
    'zh': {
        'menu_home': '主页',
        'menu_play': '下棋',
        'menu_puzzle': '残局',
        'menu_stats': '统计',
        'menu_course': '课程',
        'menu_settings': '设置',
        'settings_title': '系统设置',
        'settings_theme_title': '主题',
        'theme_wood': '经典木质',
        'theme_night': '夜间模式',
        'theme_light': '明亮模式',
        'settings_general': '常规设置',
        'setting_sound': '游戏声音',
        'setting_lang': '语言',
        'setting_hint': '显示移动提示',
        'settings_save': '保存设置'
    }
};

function updateLanguageText(lang) {
    if(!i18n[lang]) lang = 'vi';
    const dict = i18n[lang];
    
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const key = el.getAttribute('data-i18n');
        if(dict[key]) {
            // Check if it has an icon inside (like settings headers)
            if (el.children.length > 0 && el.firstElementChild.tagName === 'I') {
                const iconHtml = el.firstElementChild.outerHTML;
                el.innerHTML = iconHtml + ' ' + dict[key];
            } else {
                el.textContent = dict[key];
            }
        }
    });
}
