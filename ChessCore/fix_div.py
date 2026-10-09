import re

path = 'Views/Game/Index.cshtml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Find the end of diff-buttons and add a closing </div> for difficulty-selection
# Currently:
#                 <div class="diff-buttons">
#                     <button class="diff-btn" data-difficulty="1"> ... </button>
#                     ...
#                     <button class="diff-btn" data-difficulty="4"> ... </button>
#                 </div>
#
#             <!-- ONLINE LOBBY -->

pattern = r'(<button class="diff-btn" data-difficulty="4">.*?</button>\s*</div>)'
replacement = r'\1\n            </div>'

content = re.sub(pattern, replacement, content, flags=re.DOTALL)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Fixed unclosed div in Index.cshtml')
