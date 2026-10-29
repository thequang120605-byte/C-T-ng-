import requests
import re
import zipfile
import xml.etree.ElementTree as ET

s = requests.Session()

# 1. Login
r_get = s.get('http://localhost:5000/Auth/Login')
m = re.search(r'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"', r_get.text)
token = m.group(1) if m else ''
print('Found anti-forgery token:', bool(token))

r_post = s.post('http://localhost:5000/Auth/Login', data={
    'Username': 'admin',
    'Password': 'Admin@123',
    '__RequestVerificationToken': token
}, allow_redirects=False)
print('Login post status:', r_post.status_code, 'Cookies:', list(s.cookies.keys()))

# 2. Test Admin Export Excel
r_admin = s.get('http://localhost:5000/Admin/ExportExcel')
print('/Admin/ExportExcel Status:', r_admin.status_code)
print('Content-Type:', r_admin.headers.get('Content-Type'))
print('Content-Disposition:', r_admin.headers.get('Content-Disposition'))
print('Size:', len(r_admin.content), 'bytes')

assert r_admin.status_code == 200, "Admin export must return 200"
assert len(r_admin.content) > 5000, "Admin export file size must be > 5KB"
with open('test_admin_export.xlsx', 'wb') as f:
    f.write(r_admin.content)

# Inspect Admin Excel ZIP contents
with zipfile.ZipFile('test_admin_export.xlsx') as z:
    file_list = z.namelist()
    print('Zip files:', [f for f in file_list if f.endswith('.xml')])
    assert 'xl/workbook.xml' in file_list, "Must contain xl/workbook.xml"
    assert 'xl/worksheets/sheet1.xml' in file_list, "Must contain sheet1.xml"
    assert 'xl/worksheets/sheet2.xml' in file_list, "Must contain sheet2.xml"
    assert 'xl/worksheets/sheet3.xml' in file_list, "Must contain sheet3.xml"
    
    # Check workbook sheet names
    wb_xml = z.read('xl/workbook.xml').decode('utf-8')
    print('Workbook XML snippet:', wb_xml[:300])
    assert 'Thống kê Ván đấu' in wb_xml
    assert 'Học tập &amp; Cờ thế' in wb_xml or 'Học tập & Cờ thế' in wb_xml
    assert 'Danh sách Kỳ thủ' in wb_xml

# 3. Test Dashboard Export Excel
r_user = s.get('http://localhost:5000/Dashboard/ExportExcel')
print('/Dashboard/ExportExcel Status:', r_user.status_code)
print('Content-Type:', r_user.headers.get('Content-Type'))
print('Content-Disposition:', r_user.headers.get('Content-Disposition'))
print('Size:', len(r_user.content), 'bytes')

assert r_user.status_code == 200, "User export must return 200"
assert len(r_user.content) > 3000, "User export file size must be > 3KB"
with open('test_user_export.xlsx', 'wb') as f:
    f.write(r_user.content)

with zipfile.ZipFile('test_user_export.xlsx') as z:
    file_list = z.namelist()
    assert 'xl/workbook.xml' in file_list
    wb_xml = z.read('xl/workbook.xml').decode('utf-8')
    assert 'Thành tích Thi đấu' in wb_xml

print('\nALL EXCEL EXPORT CHECKS PASSED PERFECTLY!')
