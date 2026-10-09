import re

path = 'wwwroot/js/settings.js'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

pattern_lang = r"// Có thể reload trang để áp dụng đa ngôn ngữ từ Server\s*// window\.location\.reload\(\);"
replacement_lang = r"alert('Ngôn ngữ đã được lưu: ' + this.value + '. Vui lòng tải lại trang để áp dụng!');\n            window.location.reload();"
content = re.sub(pattern_lang, replacement_lang, content)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
