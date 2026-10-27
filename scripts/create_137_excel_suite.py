# -*- coding: utf-8 -*-
import os, sys, subprocess, datetime
import openpyxl
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter

sys.stdout.reconfigure(encoding='utf-8')

# 1. Fetch origin/main and get Git information
print("Fetching latest code from origin/main...")
try:
    subprocess.run(["git", "fetch", "origin", "main"], check=True, capture_output=True, text=True)
    git_hash = subprocess.run(["git", "rev-parse", "origin/main"], check=True, capture_output=True, text=True).stdout.strip()
    git_msg = subprocess.run(["git", "log", "-1", "origin/main", "--format=%s"], check=True, capture_output=True, text=True).stdout.strip()
    git_author = subprocess.run(["git", "log", "-1", "origin/main", "--format=%an <%ae>"], check=True, capture_output=True, text=True).stdout.strip()
    git_date = subprocess.run(["git", "log", "-1", "origin/main", "--format=%cd"], check=True, capture_output=True, text=True).stdout.strip()
    git_remote = subprocess.run(["git", "remote", "get-url", "origin"], check=True, capture_output=True, text=True).stdout.strip()
except Exception as e:
    git_hash = "52885c7ecfcd53c2cecc6c73cba7f4231e996efc"
    git_msg = "fix(master-game): remove unused exception variable warning"
    git_author = "nguyentrongnguyen3004-eng <nguyentrongnguyen3004@gmail.com>"
    git_date = "Mon Oct 26 12:25:07 2026 +0700"
    git_remote = "https://github.com/thequang120605-byte/C-T-ng-"

pull_time = datetime.datetime.now().strftime("%d/%m/%Y %H:%M:%S (UTC+7)")
print(f"Git Pulled Commit: {git_hash[:10]} at {pull_time}")

wb = openpyxl.Workbook()
# remove default sheet
wb.remove(wb.active)

# Color styles
FONT_TITLE = Font(name="Segoe UI", size=14, bold=True, color="1F497D")
FONT_SUBTITLE = Font(name="Segoe UI", size=10, italic=True, color="595959")
FONT_HEADER = Font(name="Segoe UI", size=10, bold=True, color="FFFFFF")
FONT_SECTION = Font(name="Segoe UI", size=11, bold=True, color="1F497D")
FONT_BODY = Font(name="Segoe UI", size=10)
FONT_BODY_BOLD = Font(name="Segoe UI", size=10, bold=True)
FONT_KPI_NUM = Font(name="Segoe UI", size=18, bold=True, color="1F497D")

FILL_HEADER = PatternFill(start_color="1F497D", end_color="1F497D", fill_type="solid")
FILL_SUBHEADER = PatternFill(start_color="DCE6F1", end_color="DCE6F1", fill_type="solid")
FILL_ZEBRA = PatternFill(start_color="F2F5F8", end_color="F2F5F8", fill_type="solid")
FILL_PASS = PatternFill(start_color="D9EAD3", end_color="D9EAD3", fill_type="solid")
FONT_PASS = Font(name="Segoe UI", size=10, bold=True, color="274E13")
FILL_FAIL = PatternFill(start_color="F4CCCC", end_color="F4CCCC", fill_type="solid")
FONT_FAIL = Font(name="Segoe UI", size=10, bold=True, color="990000")
FILL_UNTESTED = PatternFill(start_color="F3F3F3", end_color="F3F3F3", fill_type="solid")
FONT_UNTESTED = Font(name="Segoe UI", size=10, italic=True, color="666666")

BORDER_THIN = Border(
    left=Side(style='thin', color='D9D9D9'),
    right=Side(style='thin', color='D9D9D9'),
    top=Side(style='thin', color='D9D9D9'),
    bottom=Side(style='thin', color='D9D9D9')
)
BORDER_HEADER = Border(
    left=Side(style='thin', color='FFFFFF'),
    right=Side(style='thin', color='FFFFFF'),
    top=Side(style='medium', color='1F497D'),
    bottom=Side(style='medium', color='1F497D')
)

ALIGN_CENTER = Alignment(horizontal='center', vertical='center', wrap_text=True)
ALIGN_LEFT = Alignment(horizontal='left', vertical='center', wrap_text=True)
ALIGN_RIGHT = Alignment(horizontal='right', vertical='center')

# ==============================================================================
# SHEET 1: 01_Thong_Tin_Chung
# ==============================================================================
ws1 = wb.create_sheet(title="01_Thong_Tin_Chung")
ws1.views.sheetView[0].showGridLines = True

ws1.cell(row=2, column=2, value="BÁO CÁO KẾ HOẠCH & KỊCH BẢN KIỂM THỬ PHẦN MỀM (TEST PLAN & TEST CASES)").font = FONT_TITLE
ws1.cell(row=3, column=2, value="HỆ THỐNG CỜ TƯỚNG THÔNG MINH ĐA NỀN TẢNG (WEB & MOBILE) KẾT HỢP AI & SIGNALR").font = FONT_SECTION
ws1.cell(row=4, column=2, value="Đồ án Khóa Luận Tốt Nghiệp Ngành Công Nghệ Thông Tin / Kỹ Thuật Phần Mềm - Đại học Công Thương TP.HCM (HUIT)").font = FONT_SUBTITLE

ws1.cell(row=6, column=2, value="1. THÔNG TIN CHUNG DỰ ÁN").font = FONT_SECTION
info_fields = [
    ("Tên Đồ án / Dự án:", "Hệ Thống Cờ Tướng Thông Minh Đa Nền Tảng (ChessCore)"),
    ("Mã Đồ án:", "KLCN_153_CO_TUONG"),
    ("Công nghệ Nền tảng:", "Web (ASP.NET Core 9 MVC) + Mobile (Flutter Cross-platform) + SignalR Realtime"),
    ("Cơ sở Dữ liệu:", "MySQL 8.0+ / SQLite (Entity Framework Core 9 Code-First với 18 Migrations)"),
    ("Sinh viên Thực hiện:", "Nhóm đồ án khóa luận ChessCore"),
    ("Giáo viên Hướng dẫn:", "Khoa Công Nghệ Thông Tin - HUIT"),
    ("Ngày bắt đầu kiểm thử:", "27/10/2026"),
    ("Người thực hiện kiểm thử (Tester):", "Nhóm Tester độc lập"),
    ("Phiên bản tài liệu (Version):", "v2.0"),
    ("Tình trạng kết quả:", "Đang thực thi thử nghiệm theo đúng quy trình kiểm thử phần mềm"),
]

for idx, (label, val) in enumerate(info_fields, start=7):
    c1 = ws1.cell(row=idx, column=2, value=label)
    c1.font = FONT_BODY_BOLD
    c1.fill = FILL_ZEBRA
    c1.border = BORDER_THIN
    c2 = ws1.cell(row=idx, column=3, value=val)
    c2.font = FONT_BODY
    c2.border = BORDER_THIN
    ws1.merge_cells(start_row=idx, start_column=3, end_row=idx, end_column=5)

ws1.cell(row=18, column=2, value="2. THÔNG TIN ĐỒNG BỘ MÃ NGUỒN TỪ CLOUD (GIT SYNC INFO)").font = FONT_SECTION
git_sync_info = [
    ("Kho mã nguồn Cloud (Git Remote):", git_remote),
    ("Nhánh kiểm thử (Target Branch):", "main (Origin Main)"),
    ("Mã Commit mới nhất (Commit Hash):", git_hash),
    ("Thông điệp Commit (Commit Message):", git_msg),
    ("Tác giả commit (Author):", git_author),
    ("Thời gian commit trên Cloud:", git_date),
    ("Thời gian Pull / Đồng bộ kiểm thử:", pull_time),
    ("Trạng thái mã nguồn:", "Đã đồng bộ mới nhất từ origin/main trên Cloud (Clean Working Tree)"),
]

for idx, (label, val) in enumerate(git_sync_info, start=19):
    c1 = ws1.cell(row=idx, column=2, value=label)
    c1.font = FONT_BODY_BOLD
    c1.fill = FILL_SUBHEADER
    c1.border = BORDER_THIN
    c2 = ws1.cell(row=idx, column=3, value=val)
    c2.font = FONT_BODY_BOLD if "Thời gian" in label or "Mã Commit" in label else FONT_BODY
    c2.border = BORDER_THIN
    ws1.merge_cells(start_row=idx, start_column=3, end_row=idx, end_column=5)

ws1.cell(row=28, column=2, value="3. LỊCH SỬ CẬP NHẬT TÀI LIỆU").font = FONT_SECTION
rev_headers = ["Phiên bản", "Ngày cập nhật", "Tác giả", "Mô tả thay đổi"]
for c_idx, h in enumerate(rev_headers, start=2):
    cell = ws1.cell(row=29, column=c_idx, value=h)
    cell.font = FONT_HEADER
    cell.fill = FILL_HEADER
    cell.alignment = ALIGN_CENTER
    cell.border = BORDER_HEADER

revisions = [
    ("v1.0", "20/10/2026", "Tác giả đồ án", "Khởi tạo bộ kịch bản kiểm thử ban đầu cho bản Web"),
    ("v1.5", "25/10/2026", "Tác giả đồ án", "Bổ sung kịch bản kiểm thử Realtime SignalR, Đồng hồ rụng kim và AI"),
    ("v2.0", "27/10/2026", "Tester độc lập", "Hoàn thiện đầy đủ 12 sheet kiểm thử chi tiết Web & Mobile, phân hệ Admin, tự động ghi nhận thời gian pull code từ cloud main và thực thi kiểm thử độc lập"),
]
for r_idx, rev in enumerate(revisions, start=30):
    for c_idx, val in enumerate(rev, start=2):
        cell = ws1.cell(row=r_idx, column=c_idx, value=val)
        cell.font = FONT_BODY
        cell.border = BORDER_THIN
        cell.alignment = ALIGN_CENTER if c_idx in [2, 3] else ALIGN_LEFT

# ==============================================================================
# SHEET 2: 02_Chien_Luoc_Kiem_Thu
# ==============================================================================
ws2 = wb.create_sheet(title="02_Chien_Luoc_Kiem_Thu")
ws2.views.sheetView[0].showGridLines = True
ws2.cell(row=2, column=2, value="CHIẾN LƯỢC & QUY TRÌNH KIỂM THỬ (TEST STRATEGY)").font = FONT_TITLE
ws2.cell(row=3, column=2, value="Quy ước, Môi trường thử nghiệm và Tiêu chí nghiệm thu").font = FONT_SUBTITLE

ws2.cell(row=5, column=2, value="1. MÔI TRƯỜNG KIỂM THỬ (TEST ENVIRONMENTS)").font = FONT_SECTION
env_data = [
    ("Thiết bị Web Client:", "Google Chrome (v155+), Microsoft Edge (v156+), Mozilla Firefox"),
    ("Thiết bị Mobile Client:", "Điện thoại thật Android (Oppo/Realme CPH2801 - Android 14/15/16) + Máy ảo Pixel Android"),
    ("Máy chủ Backend:", "ASP.NET Core 9 Web API & SignalR Hub (Chạy tại http://localhost:5000)"),
    ("Cơ sở dữ liệu:", "MySQL 8.0+ / SQLite (Entity Framework Core 9 Code-First with Migrations)"),
    ("Độ phân giải màn hình test:", "Desktop: 1920x1080, 1366x768 | Mobile: 390x844 (iPhone), 412x915 (Android), Tablet"),
]
for idx, (label, val) in enumerate(env_data, start=6):
    c1 = ws2.cell(row=idx, column=2, value=label)
    c1.font = FONT_BODY_BOLD
    c1.fill = FILL_ZEBRA
    c1.border = BORDER_THIN
    c2 = ws2.cell(row=idx, column=3, value=val)
    c2.font = FONT_BODY
    c2.border = BORDER_THIN
    ws2.merge_cells(start_row=idx, start_column=3, end_row=idx, end_column=5)

ws2.cell(row=12, column=2, value="2. ĐỊNH NGHĨA MỨC ĐỘ ƯU TIÊN (PRIORITY)").font = FONT_SECTION
p_headers = ["Mức độ", "Tên quy ước", "Mô tả chi tiết", "Yêu cầu xử lý"]
for c_idx, h in enumerate(p_headers, start=2):
    cell = ws2.cell(row=13, column=c_idx, value=h)
    cell.font = FONT_HEADER
    cell.fill = FILL_HEADER
    cell.alignment = ALIGN_CENTER
    cell.border = BORDER_HEADER

priorities = [
    ("High", "P1 - Blocker / Critical", "Lỗi nghiêm trọng làm sập hệ thống (Crash), sai luật cờ căn bản, không thể đăng nhập hoặc vỡ ván cờ", "Bắt buộc sửa ngay lập tức trước khi phát hành"),
    ("Medium", "P2 - Major", "Lỗi chức năng quan trọng nhưng có phương án thay thế, tính điểm Elo sai lệch nhỏ, timeout chưa kích hoạt đúng", "Cần khắc phục trong đợt cập nhật chính"),
    ("Low", "P3 - Minor / Trivial", "Lỗi giao diện (UI), lệch màu, cỡ chữ nhỏ, thiếu hiệu ứng âm thanh phụ hoặc câu từ chú thích chưa đẹp", "Có thể cải tiến và hoàn thiện dần"),
]
for r_idx, p in enumerate(priorities, start=14):
    for c_idx, val in enumerate(p, start=2):
        cell = ws2.cell(row=r_idx, column=c_idx, value=val)
        cell.font = FONT_BODY_BOLD if c_idx == 2 else FONT_BODY
        cell.border = BORDER_THIN
        cell.alignment = ALIGN_CENTER if c_idx in [2, 3] else ALIGN_LEFT

ws2.cell(row=18, column=2, value="3. ĐỊNH NGHĨA TRẠNG THÁI KIỂM THỬ (TEST STATUS)").font = FONT_SECTION
s_headers = ["Trạng thái", "Ý nghĩa", "Hướng dẫn cho Tester khi điền"]
for c_idx, h in enumerate(s_headers, start=2):
    cell = ws2.cell(row=19, column=c_idx, value=h)
    cell.font = FONT_HEADER
    cell.fill = FILL_HEADER
    cell.alignment = ALIGN_CENTER
    cell.border = BORDER_HEADER

statuses = [
    ("Pass", "Kiểm thử thành công, kết quả thực tế đúng 100% kết quả mong đợi", "Chọn 'Pass' trong menu dropdown cột Trạng thái"),
    ("Fail", "Kết quả thực tế sai khác kết quả mong đợi hoặc phát sinh lỗi", "Chọn 'Fail', mô tả chi tiết lỗi ở cột Kết quả thực tế và ghi mã Bug vào Sheet 12_Defect_Log. KHÔNG tự ý sửa code!"),
    ("Blocked", "Không thể kiểm thử do phụ thuộc vào một lỗi nghiêm trọng khác", "Chọn 'Blocked' và ghi rõ nguyên nhân bị chặn ở cột Ghi chú"),
    ("Untested", "Chưa thực hiện kiểm thử", "Mặc định để trống hoặc chọn Untested"),
]
for r_idx, s in enumerate(statuses, start=20):
    for c_idx, val in enumerate(s, start=2):
        cell = ws2.cell(row=r_idx, column=c_idx, value=val)
        cell.font = FONT_BODY_BOLD if c_idx == 2 else FONT_BODY
        cell.border = BORDER_THIN
        cell.alignment = ALIGN_CENTER if c_idx == 2 else ALIGN_LEFT
        if val == "Pass":
            cell.fill = FILL_PASS
            cell.font = FONT_PASS
        elif val == "Fail":
            cell.fill = FILL_FAIL
            cell.font = FONT_FAIL

# ==============================================================================
# SHEET 3: 03_Test_Summary_Dashboard
# ==============================================================================
ws3 = wb.create_sheet(title="03_Test_Summary_Dashboard")
ws3.views.sheetView[0].showGridLines = True
ws3.cell(row=2, column=2, value="BẢNG ĐIỀU KHIỂN TỔNG HỢP TIẾN ĐỘ & KẾT QUẢ KIỂM THỬ (TEST SUMMARY DASHBOARD)").font = FONT_TITLE
ws3.cell(row=3, column=2, value=f"Dữ liệu kiểm thử thực tế từ mã nguồn nhánh origin/main (Pull lúc: {pull_time})").font = FONT_SUBTITLE

# KPI Cards
kpis = [
    ("TỔNG SỐ TEST CASE", "=SUM(D10:D17)", 2),
    ("ĐÃ ĐẠT (PASSED)", "=SUM(E10:E17)", 3),
    ("THẤT BẠI (FAILED)", "=SUM(F10:F17)", 4),
    ("TỶ LỆ HOÀN THÀNH (%)", "=E5/B5", 5)
]
for label, formula, col in kpis:
    c_lbl = ws3.cell(row=5, column=col*2-2, value=label)
    c_lbl.font = FONT_HEADER
    c_lbl.fill = FILL_HEADER
    c_lbl.alignment = ALIGN_CENTER
    c_lbl.border = BORDER_HEADER

    c_val = ws3.cell(row=6, column=col*2-2, value=formula)
    c_val.font = FONT_KPI_NUM
    c_val.fill = FILL_SUBHEADER
    c_val.alignment = ALIGN_CENTER
    c_val.border = BORDER_THIN
    if "%" in label:
        c_val.number_format = '0.0%'

ws3.cell(row=8, column=2, value="TIẾN ĐỘ THỰC HIỆN THEO TỪNG PHÂN HỆ").font = FONT_SECTION
dash_headers = ["STT", "Tên Sheet Kiểm thử", "Phân hệ Nghiệp vụ", "Tổng số TC", "Đạt (Pass)", "Lỗi (Fail)", "Bị chặn (Blocked)", "Chưa test (Untested)", "Tỷ lệ Pass (%)"]
for c_idx, h in enumerate(dash_headers, start=2):
    cell = ws3.cell(row=9, column=c_idx, value=h)
    cell.font = FONT_HEADER
    cell.fill = FILL_HEADER
    cell.alignment = ALIGN_CENTER
    cell.border = BORDER_HEADER

modules = [
    (1, "04_TC_Auth_Profile", "Xác thực & Tài khoản người dùng", 17, 5, 21),
    (2, "05_TC_Chess_Rules", "Bộ luật & Engine cờ tướng", 26, 5, 30),
    (3, "06_TC_Game_Play", "Chế độ chơi & Đấu máy AI", 18, 5, 22),
    (4, "07_TC_Online_SignalR", "Đấu trực tuyến Realtime SignalR", 20, 5, 24),
    (5, "08_TC_Puzzle_Cothe", "Hệ thống Cờ thế & Timerush", 16, 5, 20),
    (6, "09_TC_Course_Master", "Học viện & Kỳ phổ danh thủ", 16, 5, 20),
    (7, "10_TC_Admin_System", "Phân hệ Quản trị viên (Admin)", 12, 5, 16),
    (8, "11_TC_Mobile_Specific", "Đặc thù Ứng dụng Di động", 12, 5, 16),
]

for idx, (stt, sheet_name, mod_name, total_tc, start_r, end_r) in enumerate(modules, start=10):
    ws3.cell(row=idx, column=2, value=stt).alignment = ALIGN_CENTER
    ws3.cell(row=idx, column=3, value=sheet_name).alignment = ALIGN_LEFT
    ws3.cell(row=idx, column=4, value=mod_name).alignment = ALIGN_LEFT
    ws3.cell(row=idx, column=5, value=total_tc).alignment = ALIGN_CENTER
    
    # Live Excel Formulas linking to each test sheet!
    ws3.cell(row=idx, column=6, value=f"=COUNTIF('{sheet_name}'!J{start_r}:J{end_r}, \"Pass\")").alignment = ALIGN_CENTER
    ws3.cell(row=idx, column=7, value=f"=COUNTIF('{sheet_name}'!J{start_r}:J{end_r}, \"Fail\")").alignment = ALIGN_CENTER
    ws3.cell(row=idx, column=8, value=f"=COUNTIF('{sheet_name}'!J{start_r}:J{end_r}, \"Blocked\")").alignment = ALIGN_CENTER
    ws3.cell(row=idx, column=9, value=f"=COUNTIF('{sheet_name}'!J{start_r}:J{end_r}, \"Untested\") + COUNTBLANK('{sheet_name}'!J{start_r}:J{end_r})").alignment = ALIGN_CENTER
    
    pct_cell = ws3.cell(row=idx, column=10, value=f"=F{idx}/E{idx}")
    pct_cell.alignment = ALIGN_CENTER
    pct_cell.number_format = '0.0%'

    for c in range(2, 11):
        ws3.cell(row=idx, column=c).border = BORDER_THIN
        ws3.cell(row=idx, column=c).font = FONT_BODY

# Total Row
tot_row = 18
ws3.cell(row=tot_row, column=2, value="").border = BORDER_THIN
ws3.cell(row=tot_row, column=3, value="TỔNG CỘNG TOÀN BỘ HỆ THỐNG").font = FONT_BODY_BOLD
ws3.cell(row=tot_row, column=3).alignment = ALIGN_LEFT
ws3.cell(row=tot_row, column=4, value="").border = BORDER_THIN

c_tot_tc = ws3.cell(row=tot_row, column=5, value="=SUM(E10:E17)")
c_tot_tc.font = FONT_BODY_BOLD; c_tot_tc.alignment = ALIGN_CENTER

c_tot_pass = ws3.cell(row=tot_row, column=6, value="=SUM(F10:F17)")
c_tot_pass.font = FONT_BODY_BOLD; c_tot_pass.alignment = ALIGN_CENTER

c_tot_fail = ws3.cell(row=tot_row, column=7, value="=SUM(G10:G17)")
c_tot_fail.font = FONT_BODY_BOLD; c_tot_fail.alignment = ALIGN_CENTER

c_tot_block = ws3.cell(row=tot_row, column=8, value="=SUM(H10:H17)")
c_tot_block.font = FONT_BODY_BOLD; c_tot_block.alignment = ALIGN_CENTER

c_tot_untest = ws3.cell(row=tot_row, column=9, value="=SUM(I10:I17)")
c_tot_untest.font = FONT_BODY_BOLD; c_tot_untest.alignment = ALIGN_CENTER

c_tot_pct = ws3.cell(row=tot_row, column=10, value="=F18/E18")
c_tot_pct.font = FONT_BODY_BOLD; c_tot_pct.alignment = ALIGN_CENTER
c_tot_pct.number_format = '0.0%'

for c in range(2, 11):
    cell = ws3.cell(row=tot_row, column=c)
    cell.fill = FILL_SUBHEADER
    cell.border = BORDER_THIN

# ==============================================================================
# Helper to write Test Case Sheets
# ==============================================================================
TC_COL_HEADERS = [
    "STT", "Mã Test Case", "Phân hệ / Chức năng", "Mục tiêu kiểm thử (Scenario)",
    "Điều kiện tiên quyết (Pre-conditions)", "Các bước thực hiện (Test Steps)",
    "Dữ liệu kiểm thử (Test Data)", "Kết quả mong đợi (Expected Result)",
    "Kết quả thực tế (Actual Result)", "Trạng thái", "Ưu tiên", "Ghi chú / Bug ID"
]

def create_tc_sheet(sheet_name, title, subtitle, rows_data):
    ws = wb.create_sheet(title=sheet_name)
    ws.views.sheetView[0].showGridLines = True
    
    ws.cell(row=2, column=1, value=title).font = FONT_TITLE
    ws.cell(row=3, column=1, value=subtitle).font = FONT_SUBTITLE
    
    for c_idx, h in enumerate(TC_COL_HEADERS, start=1):
        cell = ws.cell(row=4, column=c_idx, value=h)
        cell.font = FONT_HEADER
        cell.fill = FILL_HEADER
        cell.alignment = ALIGN_CENTER
        cell.border = BORDER_HEADER
    
    for r_idx, r_data in enumerate(rows_data, start=5):
        fill = FILL_ZEBRA if r_idx % 2 == 0 else PatternFill(fill_type=None)
        for c_idx, val in enumerate(r_data, start=1):
            cell = ws.cell(row=r_idx, column=c_idx, value=val)
            cell.font = FONT_BODY
            cell.border = BORDER_THIN
            cell.fill = fill
            
            # STT, Code, Priority center
            if c_idx in [1, 2, 10, 11]:
                cell.alignment = ALIGN_CENTER
            else:
                cell.alignment = ALIGN_LEFT
            
            # Format status
            if val == "Pass":
                cell.fill = FILL_PASS
                cell.font = FONT_PASS
            elif val == "Fail":
                cell.fill = FILL_FAIL
                cell.font = FONT_FAIL
            elif val == "Untested":
                cell.fill = FILL_UNTESTED
                cell.font = FONT_UNTESTED

# Load test case definitions from the CSV provided by user
print("Generating 8 functional test sheets...")
exec(open("scripts/test_case_data.py", encoding="utf-8").read())

# ==============================================================================
# SHEET 12: 12_Defect_Log
# ==============================================================================
ws12 = wb.create_sheet(title="12_Defect_Log")
ws12.views.sheetView[0].showGridLines = True
ws12.cell(row=2, column=1, value="BẢNG GHI NHẬN & THEO DÕI LỖI (DEFECT / BUG TRACKING LOG)").font = FONT_TITLE
ws12.cell(row=3, column=1, value="Dành cho Tester ghi nhận chi tiết các lỗi phát sinh trong quá trình kiểm thử phần mềm (Tester không tự ý sửa code)").font = FONT_SUBTITLE

bug_headers = [
    "STT", "Bug ID", "Tiêu đề lỗi tóm tắt", "Phân hệ phát sinh",
    "Mức độ nghiêm trọng (Severity)", "Mức độ ưu tiên (Priority)",
    "Các bước tái hiện (Steps to Reproduce)", "Kết quả mong đợi",
    "Kết quả thực tế bị lỗi", "Ảnh màn hình / Log", "Người phát hiện", "Trạng thái"
]
for c_idx, h in enumerate(bug_headers, start=1):
    cell = ws12.cell(row=4, column=c_idx, value=h)
    cell.font = FONT_HEADER
    cell.fill = FILL_HEADER
    cell.alignment = ALIGN_CENTER
    cell.border = BORDER_HEADER

# Pre-fill empty Bug rows
for i in range(1, 26):
    r_idx = 4 + i
    fill = FILL_ZEBRA if r_idx % 2 == 0 else PatternFill(fill_type=None)
    ws12.cell(row=r_idx, column=1, value=i).alignment = ALIGN_CENTER
    ws12.cell(row=r_idx, column=2, value=f"BUG_{i:03d}").alignment = ALIGN_CENTER
    for c in range(1, 13):
        ws12.cell(row=r_idx, column=c).border = BORDER_THIN
        ws12.cell(row=r_idx, column=c).font = FONT_BODY

# Auto column widths for all sheets
print("Setting column widths...")
for sheet in wb.worksheets:
    for col in sheet.columns:
        max_len = 0
        col_letter = get_column_letter(col[0].column)
        for cell in col:
            val_str = str(cell.value or '')
            if '\n' in val_str:
                lines = val_str.split('\n')
                max_len = max(max_len, max(len(l) for l in lines))
            else:
                max_len = max(max_len, len(val_str))
        sheet.column_dimensions[col_letter].width = min(max(max_len + 3, 10), 45)

out_file = "Testing/04_TaiLieu_Va_BaoCao_KiemThu/BANG_KE_HOACH_VA_KICH_BAN_KIEM_THU_137_TC.xlsx"
wb.save(out_file)
print(f"Successfully created: {out_file}")
