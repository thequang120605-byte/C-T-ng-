# CẨM NANG HƯỚNG DẪN KIỂM THỬ TOÀN DIỆN & TÁI HIỆN 17 DEFECTS
## ĐỒ ÁN TỐT NGHIỆP: HỆ THỐNG WEBSITE CỜ TƯỚNG TRỰC TUYẾN CHESSCORE
### BỘ DỮ LIỆU ĐỒNG BỘ 137 TEST CASES & 17 LỖI THỰC TẾ (DEFECT LOG)

---

## 🧭 PHẦN 1: TỔNG QUAN KIẾN TRÚC KIỂM THỬ 4 FOLDER (TESTING PYRAMID)

Hệ thống kiểm thử của dự án được tổ chức khoa học theo mô hình **Kim tự tháp kiểm thử phần mềm (Software Testing Pyramid)** gồm 4 thư mục chuyên trách:

```
Testing/
├── 01_Ban1_UnitTests_xUnit/              <-- TẦNG ĐÁY: KIỂM THỬ ĐƠN VỊ & ENGINE THUẬT TOÁN (C#)
│   ├── ChessRulesAndAiTests.cs          (12 ca kiểm thử logic cờ, MoveGenerator, Minimax AI)
│   └── ChessCore.Tests.csproj           (Tích hợp xUnit & Coverlet Code Coverage)
│
├── 02_Ban2_ApiTests_Postman/             <-- TẦNG GIỮA: KIỂM THỬ TỰ ĐỘNG HÓA API TÍCH HỢP
│   ├── ChessCore_APIs.postman_collection.json (Bộ sưu tập toàn bộ kịch bản kiểm thử API)
│   └── ChessCore_Local.postman_environment.json (Biến môi trường: baseUrl, jwt_token, game_id)
│
├── 03_Ban3_E2ETests_Scripts/             <-- TẦNG NGỌN: TỰ ĐỘNG HÓA TOÀN TRÌNH MASTER MATRIX
│   └── run_suite_137.py                 (Công cụ chạy 137 TC, tự động pull Git Cloud và ghi Excel)
│
├── 04_TaiLieu_Va_BaoCao_KiemThu/         <-- KẾT QUẢ ĐẦU RA, BÁO CÁO NGHIỆM THU & BẰNG CHỨNG
│   ├── HUONG_DAN_KIEM_THU_TOAN_DIEN_137_TC_VA_17_BUGS.md (Tài liệu này)
│   └── E2E_SCREENSHOTS/                 (36 ảnh chụp minh chứng: 19 ca Pass + 17 ca Defect)
│
└── TestCases_KLCN_CoTuong (1).xlsx       <-- BẢNG KẾ HOẠCH & DEFECT LOG 137 TEST CASES CHÍNH THỨC
```

---

## 📊 PHẦN 2: THỐNG KÊ KẾT QUẢ 137 TEST CASES THEO PHÂN HỆ

| STT | Tên Sheet | Phân hệ Nghiệp vụ | Tổng số TC | Đạt (Pass) | Lỗi (Fail) | Tỷ lệ Đạt | Mã Bug phát hiện |
| :---: | :--- | :--- | :---: | :---: | :---: | :---: | :--- |
| 1 | `04_TC_Auth_Profile` | Xác thực & Tài khoản | 17 | 11 | 6 | 64.7% | BUG_001, BUG_002, BUG_008, BUG_009, BUG_010, **BUG_016** |
| 2 | `05_TC_Chess_Rules` | Bộ luật & Engine cờ tướng | 26 | 26 | 0 | 100.0% | *(Vận hành chuẩn xác)* |
| 3 | `06_TC_Game_Play` | Chế độ chơi & Đấu máy AI | 18 | 16 | 2 | 88.9% | BUG_011, **BUG_017** |
| 4 | `07_TC_Online_SignalR` | Đấu trực tuyến Realtime | 20 | 18 | 2 | 90.0% | BUG_004, BUG_012 |
| 5 | `08_TC_Puzzle_Cothe` | Cờ thế & Timerush | 16 | 16 | 0 | 100.0% | *(Vận hành chuẩn xác)* |
| 6 | `09_TC_Course_Master` | Học viện & Kỳ phổ | 16 | 13 | 3 | 81.3% | BUG_005, BUG_006, BUG_013 |
| 7 | `10_TC_Admin_System` | Phân hệ Quản trị viên | 12 | 8 | 4 | 66.7% | BUG_003, BUG_007, BUG_014, BUG_015 |
| 8 | `11_TC_Mobile_Specific` | Đặc thù Ứng dụng Di động | 12 | 12 | 0 | 100.0% | *(Giao diện Responsive chuẩn)* |
| **Tổng** | **Toàn bộ hệ thống** | **8 Phân hệ** | **137** | **120** | **17** | **87.6%** | **17 Real Defects (12.4% Fail)** |

---

## 🔍 PHẦN 3: CẨM NANG TỰ TAY KIỂM THỬ & TÁI HIỆN 17 LỖI THỰC TẾ (DEFECT LOG)

> **Yêu cầu trước khi test**: Bật Web Server Kestrel bằng lệnh: `dotnet run --project ChessCore` (mở tại `http://localhost:5000`).

---

### 🔴 NHÓM 1: LỖI BẢO MẬT & XÁC THỰC TÀI KHOẢN (AUTH & PROFILE)

#### 1. BUG_001 (`TC_AUTH_004`): Đăng ký mật khẩu yếu dưới 6 ký tự
* **Bản chất**: Backend không validate độ dài chuỗi mật khẩu (`request.Password.Length >= 6`).
* **Cách test tay**: Mở F12 Console tại `http://localhost:5000`, chạy:
  ```javascript
  fetch('/api/auth/register', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({username: 'user_p3_' + Date.now(), email: 'p3_' + Date.now() + '@gmail.com', password: '123'})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả**: HTTP 200 OK, tạo tài khoản thành công với mật khẩu 3 ký tự `"123"`.
* **Ảnh chứng cứ**: [TC_AUTH_004.png](E2E_SCREENSHOTS/TC_AUTH_004.png)

#### 2. BUG_002 (`TC_AUTH_001`): Thiếu Regex kiểm tra định dạng Email khi đăng ký
* **Bản chất**: Backend thiếu biểu thức chính quy Regex để loại bỏ chuỗi không phải email.
* **Cách test tay**: Mở F12 Console, chạy:
  ```javascript
  fetch('/api/auth/register', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({username: 'user_bad_' + Date.now(), email: 'chuoi_nay_khong_phai_email', password: 'Password123!'})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả**: HTTP 200 OK, lưu trực tiếp email rác vào CSDL SQLite.
* **Ảnh chứng cứ**: [TC_AUTH_001.png](E2E_SCREENSHOTS/TC_AUTH_001.png)

#### 3. BUG_008 (`TC_AUTH_011`): Rò rỉ mã OTP đặt lại mật khẩu trực tiếp lên giao diện (Critical Security)
* **Bản chất**: Khi SMTP Gmail gặp sự cố, controller gán nguyên văn mã OTP vào `TempData["OtpMessage"]`.
* **Cách test tay**:
  1. Vào trình duyệt, truy cập: `http://localhost:5000/Auth/ForgotPassword`
  2. Nhập email của tài khoản bất kỳ (ví dụ: `admin@cotuong.pro` hoặc email của bạn)
  3. Bấm **Gửi mã OTP**.
* **Kết quả**: Màn hình hiển thị: *"Lỗi khi gửi email qua Gmail. MÃ OTP CỦA BẠN LÀ: 123456"*.
* **Ảnh chứng cứ**: [TC_AUTH_011.png](E2E_SCREENSHOTS/TC_AUTH_011.png)

#### 4. BUG_009 (`TC_AUTH_016`): Nhận thưởng nhiệm vụ không check điều kiện & Cho phép spam
* **Bản chất**: `QuestController.cs` không kiểm tra tiến độ người chơi và không chặn nhận thưởng lặp.
* **Cách test tay**: Mở F12 Console, chạy lệnh spam nhận thưởng:
  ```javascript
  for(let i = 0; i < 5; i++) {
    fetch('/api/quests/ai/claim', {method: 'POST'}).then(r => r.json()).then(res => console.log('Lần ' + (i+1) + ':', res));
  }
  ```
* **Kết quả**: Cả 5 lần đều trả về `HTTP 200 OK: {"success":true,"message":"Nhận thưởng thành công!"}` và cộng dồn điểm `PuzzleScore` liên tục.
* **Ảnh chứng cứ**: [TC_AUTH_016.png](E2E_SCREENSHOTS/TC_AUTH_016.png)

#### 5. BUG_010 (`TC_AUTH_010`): Thiếu Rate Limiting chống Brute-force mật khẩu
* **Bản chất**: `AuthController.Login` không có bộ đếm thử sai, không có lockout tài khoản hay CAPTCHA.
* **Ảnh chứng cứ**: [TC_AUTH_010.png](E2E_SCREENSHOTS/TC_AUTH_010.png)

#### 6. BUG_016 (`TC_AUTH_002`): Cho phép đăng ký Username chứa mã độc XSS `<script>` (Stored XSS)
* **Bản chất**: `POST /api/auth/register` không lọc các thẻ HTML/Script trong Username, lưu thẳng mã độc vào bảng Users trong CSDL.
* **Cách test tay**: Mở F12 Console, dán:
  ```javascript
  fetch('/api/auth/register', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({username: "<script>alert('XSS_ATTACK')</script>", password: 'Password123!', email: 'xss_' + Date.now() + '@test.com'})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả**: HTTP 200 OK, tài khoản được tạo và lưu nguyên văn `<script>alert('XSS_ATTACK')</script>` vào CSDL!
* **Ảnh chứng cứ**: [TC_AUTH_002.png](E2E_SCREENSHOTS/TC_AUTH_002.png)

---

### 🔴 NHÓM 2: LỖI VẬN HÀNH TRẬN ĐẤU & ONLINE SIGNALR

#### 7. BUG_004 (`TC_ONL_015`): Vẫn nhận nước cờ sau khi người chơi đã Đầu hàng (Blocker)
* **Bản chất**: Ván cờ sau khi `Resign` không khóa triệt để luồng nhận nước cờ `POST /api/games/{id}/moves`.
* **Ảnh chứng cứ**: [TC_ONL_015.png](E2E_SCREENSHOTS/TC_ONL_015.png)

#### 8. BUG_011 (`TC_GAME_013`): Nhập FEN hỏng nuốt lỗi và tự gán thế cờ mặc định
* **Bản chất**: Khi `Board.TryParseFen` thất bại, backend không báo lỗi mà âm thầm gán `initialFen = null`.
* **Ảnh chứng cứ**: [TC_GAME_013.png](E2E_SCREENSHOTS/TC_GAME_013.png)

#### 9. BUG_012 (`TC_ONL_016`): Tái đấu (Rematch) ép đối thủ vào ván mới không cần đồng ý
* **Bản chất**: `GamesController.CreateRematch` không có cơ chế `OfferRematch` chờ đối thủ xác nhận.
* **Ảnh chứng cứ**: [TC_ONL_016.png](E2E_SCREENSHOTS/TC_ONL_016.png)

#### 10. BUG_017 (`TC_GAME_001`): API tạo ván cờ chấp nhận thời gian thi đấu âm (timeControl: -999)
* **Bản chất**: `POST /api/games` không kiểm tra giá trị biên `timeControl > 0`, tạo ra ván cờ với thời gian âm làm lỗi đồng hồ đếm ngược.
* **Cách test tay**: Mở F12 Console, chạy:
  ```javascript
  fetch('/api/games', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({gameMode: 'Custom', timeControl: -999})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả**: HTTP 201 Created với ván đấu có thời gian -999 phút!
* **Ảnh chứng cứ**: [TC_GAME_001.png](E2E_SCREENSHOTS/TC_GAME_001.png)

---

### 🔴 NHÓM 3: LỖI HỌC VIỆN KHÓA HỌC & CẤP CHỨNG CHỈ (COURSE)

#### 11. BUG_005 (`TC_COU_009`): Cấp Chứng chỉ tốt nghiệp cho người chưa học bài nào (0% tiến độ)
* **Ảnh chứng cứ**: [TC_COU_009.png](E2E_SCREENSHOTS/TC_COU_009.png)

#### 12. BUG_006 (`TC_COU_008`): Chấp nhận điểm trắc nghiệm Quiz vượt ngưỡng 100% (999999 điểm)
* **Ảnh chứng cứ**: [TC_COU_008.png](E2E_SCREENSHOTS/TC_COU_008.png)

#### 13. BUG_013 (`TC_COU_006`): API tiến độ của Khách trả về 200 cho Lesson ID không tồn tại
* **Ảnh chứng cứ**: [TC_COU_006.png](E2E_SCREENSHOTS/TC_COU_006.png)

---

### 🔴 NHÓM 4: LỖI PHÂN HỆ QUẢN TRỊ VIÊN (ADMIN SYSTEM)

#### 14. BUG_003 (`TC_ADM_002`): Truy cập /Admin khi chưa đăng nhập trả về 302 thay vì 403 Forbidden
* **Ảnh chứng cứ**: [TC_ADM_002.png](E2E_SCREENSHOTS/TC_ADM_002.png)

#### 15. BUG_007 (`TC_ADM_009`): Xóa bài cờ thế với ID không tồn tại trả về HTTP 200
* **Ảnh chứng cứ**: [TC_ADM_009.png](E2E_SCREENSHOTS/TC_ADM_009.png)

#### 16. BUG_014 (`TC_ADM_005`): Tính năng Khóa / Mở khóa tài khoản hoàn toàn chưa được code
* **Ảnh chứng cứ**: [TC_ADM_005.png](E2E_SCREENSHOTS/TC_ADM_005.png)

#### 17. BUG_015 (`TC_ADM_008`): Admin CMS cho phép thêm bài cờ thế với FEN rác vào CSDL
* **Ảnh chứng cứ**: [TC_ADM_008.png](E2E_SCREENSHOTS/TC_ADM_008.png)

---

## 🛠️ PHẦN 4: HƯỚNG DẪN THỰC THI CÁC CÔNG CỤ ĐỒ ÁN TỐT NGHIỆP

```powershell
# 1. Kiểm thử Đơn vị & Đo Code Coverage qua Coverlet (Folder 1)
dotnet test Testing/01_Ban1_UnitTests_xUnit/ChessCore.Tests.csproj --collect:"XPlat Code Coverage"

# 2. Tự động hóa kiểm thử API qua Newman CLI (Folder 2)
npx newman run Testing/02_Ban2_ApiTests_Postman/ChessCore_APIs.postman_collection.json -e Testing/02_Ban2_ApiTests_Postman/ChessCore_Local.postman_environment.json

# 3. Bộ Master Test Matrix 137 Test Cases đồng bộ Cloud Git (Folder 3)
python Testing/03_Ban3_E2ETests_Scripts/run_suite_137.py
```

---

## 📁 PHẦN 5: DANH MỤC 36 ẢNH CHỨNG CỨ TRONG THƯ MỤC `E2E_SCREENSHOTS/`

Thư mục [Testing/04_TaiLieu_Va_BaoCao_KiemThu/E2E_SCREENSHOTS](E2E_SCREENSHOTS) chứa **chính xác 36 ảnh chụp minh chứng**, khớp tỷ lệ 1:1 tuyệt đối giữa kịch bản kiểm thử và nhật ký lỗi:

### 1. Danh sách 19 ảnh chụp kịch bản Thành công (Pass Screenshots)
1. `01_TrangChu_ChuaDangNhap.png` - Giao diện Trang chủ khi chưa đăng nhập
2. `02_DangKy_TaiKhoan.png` - Form đăng ký tài khoản mới
3. `03_XacThuc_OTP_Email.png` - Giao diện nhập mã OTP xác thực email
4. `04_DangNhap_HeThong.png` - Form đăng nhập hệ thống
5. `05_TrangChu_DaDangNhap.png` - Giao diện Trang chủ sau khi đăng nhập thành công
6. `06_BangXepHang_Dashboard.png` - Bảng xếp hạng kỳ thủ Elo toàn hệ thống
7. `07_SanhDau_CheDo_PvP_PvE.png` - Sảnh chờ lựa chọn chế độ chơi
8. `08_BanCo_ThiDau_PvE.png` - Bàn cờ thi đấu với AI tích hợp Eval bar
9. `09_KhaiCuoc_Explorer_Modal.png` - Modal tra cứu biến thể khai cuộc ECCO
10. `09_SaBan_ChienDich_CoThe.png` - Sa bàn bản đồ chiến dịch cờ thế
11. `10_KhoaHoc_GiaoTrinh_LyThuyet.png` - Giao trình học viện 4 Module lý thuyết
12. `11_ChungChi_DienTu_CERT_CT.png` - Mẫu chứng chỉ cờ tướng điện tử CERT-CT
13. `12_TuDien_KyPho_DanhThu.png` - Từ điển tra cứu kỳ phổ các danh thủ giang hồ
14. `13_Replay_VanCo_DanhThu.png` - Trình phát lại ván cờ danh thủ với cây nước đi PGN
15. `14_BangDieuKhien_Admin.png` - Bảng điều khiển quản trị tổng quan CMS
16. `15_QuanTri_NguoiDung.png` - CMS Quản trị danh sách người dùng và phân quyền
17. `16_QuanTri_KhoBaiTap_CoThe.png` - CMS Quản lý kho bài cờ thế và FEN
18. `17_QuanTri_KhoaHoc.png` - CMS Quản trị module bài học lý thuyết
19. `18_DoKiem_AI_Benchmark.png` - Công cụ đo kiểm hiệu năng AI Depth 1-5

### 2. Danh sách 17 ảnh chụp chứng cứ lỗi tương ứng 17 Bugs (Defect Cards)
1. `TC_AUTH_004.png` $\rightarrow$ `BUG_001`: Đăng ký mật khẩu yếu 3 ký tự ("123")
2. `TC_AUTH_001.png` $\rightarrow$ `BUG_002`: Đăng ký email không có domain
3. `TC_ADM_002.png` $\rightarrow$ `BUG_003`: Truy cập /Admin trả về 302 thay vì 403
4. `TC_ONL_015.png` $\rightarrow$ `BUG_004`: Nhận nước cờ sau khi người chơi đã đầu hàng
5. `TC_COU_009.png` $\rightarrow$ `BUG_005`: Cấp chứng chỉ cho user 0% tiến độ học
6. `TC_COU_008.png` $\rightarrow$ `BUG_006`: Chấp nhận điểm trắc nghiệm Quiz 999999%
7. `TC_ADM_009.png` $\rightarrow$ `BUG_007`: Xóa cờ thế với ID rác trả về HTTP 200/302
8. `TC_AUTH_011.png` $\rightarrow$ `BUG_008`: Rò rỉ mã OTP quên mật khẩu trên màn hình
9. `TC_AUTH_016.png` $\rightarrow$ `BUG_009`: Nhận thưởng nhiệm vụ không check điều kiện & spam claim
10. `TC_AUTH_010.png` $\rightarrow$ `BUG_010`: Thiếu Rate Limiting / Lockout chống Brute-force
11. `TC_GAME_013.png` $\rightarrow$ `BUG_011`: Nhập FEN hỏng nuốt lỗi và tự gán thế mặc định
12. `TC_ONL_016.png` $\rightarrow$ `BUG_012`: Tái đấu ép đối thủ vào trận không cần đồng ý
13. `TC_COU_006.png` $\rightarrow$ `BUG_013`: Khách gửi Lesson ID rác 999999 vẫn trả về HTTP 200
14. `TC_ADM_005.png` $\rightarrow$ `BUG_014`: Tính năng Khóa / Mở khóa user chưa được code
15. `TC_ADM_008.png` $\rightarrow$ `BUG_015`: Admin CMS cho phép lưu FEN rác vào database
16. `TC_AUTH_002.png` $\rightarrow$ `BUG_016`: Đăng ký Username chứa mã độc XSS `<script>`
17. `TC_GAME_001.png` $\rightarrow$ `BUG_017`: API tạo ván cờ chấp nhận thời gian thi đấu âm (-999)

