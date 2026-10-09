import re

path = 'wwwroot/js/game.js'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Inside selectGameMode, add logic to clear playerVsOnlineButton
pattern = r'(if\s*\(playerVsComputerButton\)\s*\{\s*playerVsComputerButton\.classList\.remove\("selected"\);\s*playerVsComputerButton\.setAttribute\("aria-pressed",\s*"false"\);\s*\})'
replacement = r'''\1

    if (playerVsOnlineButton) {
      playerVsOnlineButton.classList.remove("selected");
      playerVsOnlineButton.setAttribute("aria-pressed", "false");
    }'''

if 'playerVsOnlineButton.classList.remove("selected")' not in content[:content.find('if (isComputer)')]:
    content = re.sub(pattern, replacement, content, count=1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated selectGameMode to clear online button')
