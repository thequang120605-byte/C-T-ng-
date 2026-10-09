import re

files = [
    'Views/Auth/Login.cshtml',
    'Views/Auth/Register.cshtml',
    'Views/Auth/VerifyOtp.cshtml'
]

for path in files:
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()

    # We want to replace the whole @section Styles block, or just the <style>...</style> block
    # It's easier to use a regex to match @section Styles { <style> ... </style> }
    pattern = r'@section Styles\s*\{\s*<style>.*?</style>\s*\}'
    
    replacement = r'''@section Styles {
    <link rel="stylesheet" href="~/css/auth.css" asp-append-version="true" />
}'''

    content = re.sub(pattern, replacement, content, flags=re.DOTALL)

    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

print('Updated Auth views to use auth.css')
