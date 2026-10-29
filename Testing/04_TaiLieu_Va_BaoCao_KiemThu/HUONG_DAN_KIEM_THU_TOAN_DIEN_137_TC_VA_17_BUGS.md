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

## 🔍 PHẦN 3: CẨM NANG TỰ TAY KIỂM THỬ, PHÂN LOẠI ĐIỀU KIỆN KÍCH HOẠT & HƯỚNG DẪN FIX 17 DEFECTS

> **BẢNG CHỈ DẪN KÍCH HOẠT NHANH 17 DEFECTS:**
> * 📡 **Cái nào TẮT WIFI mới hiện lỗi (1 lỗi)**: `BUG_008` (Mất mạng làm fail gửi mail SMTP -> kích hoạt khối code lộ mã OTP ra web).
> * 🖱️ **Cái nào THAO TÁC TRỰC TIẾP TRÊN GIAO DIỆN (UI) là thấy ngay (6 lỗi)**: `BUG_004`, `BUG_005`, `BUG_007`, `BUG_012`, `BUG_014`, `BUG_015`.
> * 💻 **Cái nào CHẠY SCRIPT / F12 CONSOLE / GỌI API mới hiện lỗi (10 lỗi)**: `BUG_001`, `BUG_002`, `BUG_003`, `BUG_006`, `BUG_009`, `BUG_010`, `BUG_011`, `BUG_013`, `BUG_016`, `BUG_017`.

---

### 📡 PHÂN NHÓM A: LỖI KÍCH HOẠT KHI TẮT WIFI / MẤT MẠNG INTERNET (1 LỖI)

#### 1. BUG_008 (`TC_AUTH_011`): Rò rỉ mã OTP bí mật trực tiếp lên giao diện khi lỗi SMTP (Critical Security)
* 🎯 **Điều kiện kích hoạt 100%**: **TẮT WIFI HOẶC BẬT CHẾ ĐỘ MÁY BAY (AIRPLANE MODE)** trên máy tính!
  *(Giải thích: Khi máy có mạng, tài khoản Gmail của dev gửi mail thành công nên hiện chữ xanh; chỉ khi bạn tắt Wifi, việc gửi mail bị fail, hệ thống mới kích hoạt khối code `else` làm lộ mã OTP ngay lập tức!)*
* 🖱️ **Thao tác chi tiết**:
  1. Ngắt kết nối Wifi / Mạng Internet trên máy tính.
  2. Vào trang web: `http://localhost:5000/Auth/ForgotPassword`
  3. Nhập email: `testB_idor@test.com` (hoặc email bất kỳ).
  4. Bấm nút **"Gửi mã OTP"**.
  5. 💥 **Lập tức màn hình chuyển sang trang ResetPassword và hiện KHUNG MÀU VÀNG VIỀN NÉT ĐỨT**:  
     👉 **`[DEV MODE] Mã OTP của bạn là: XXXXXX`** (Mã OTP 6 số ngẫu nhiên hiển thị to rõ trên web!).
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AuthController.cs`, dòng 315-319 trong hàm `ForgotPassword`.
  * **Cách Fix**: Xóa bỏ hoàn toàn việc gán `TempData["DevOtp"] = otp;`. Khi email gửi thất bại chỉ thông báo an toàn: *"Không thể gửi email lúc này, vui lòng thử lại sau"*.
* **Ảnh chứng cứ**: [TC_AUTH_011.png](E2E_SCREENSHOTS/TC_AUTH_011.png)

---

### 🖱️ PHÂN NHÓM B: CÁC LỖI THAO TÁC TRỰC TIẾP TRÊN GIAO DIỆN WEB (6 LỖI)

#### 2. BUG_004 (`TC_ONL_015`): Người chơi bấm "Xin hàng" nhưng ván cờ không chịu kết thúc (Blocker)
* 🎯 **Điều kiện kích hoạt 100%**: Thao tác trực tiếp trên giao diện thi đấu cờ Online (PvP)!
* 🖱️ **Thao tác chi tiết**:
  1. Vào ván cờ trực tuyến giữa 2 người chơi.
  2. Bên người chơi Đỏ bấm nút **"Xin hàng"** ở thanh điều khiển bên phải bàn cờ.
  3. Bấm **"Đồng ý"** trên modal xác nhận.
  4. 💥 **Lỗi hiện ra**: Modal đóng lại nhưng bàn cờ **vẫn tiếp tục cho phép kéo thả quân đi cờ bình thường**, không chịu xử thua và không cộng/trừ điểm Elo! *(Do file `game.js` dòng 2173 quên gửi header Token nên server từ chối 401)*.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/wwwroot/js/game.js`, dòng 2173: Thêm `'Authorization': 'Bearer ' + token` vào request fetch đầu hàng.
* **Ảnh chứng cứ**: [TC_ONL_015.png](E2E_SCREENSHOTS/TC_ONL_015.png)

#### 3. BUG_012 (`TC_ONL_016`): Bấm nút "Tái đấu" ép thẳng đối thủ vào ván mới mà không cần hỏi
* 🎯 **Điều kiện kích hoạt 100%**: Thao tác trực tiếp trên giao diện khi vừa kết thúc 1 ván cờ Online!
* 🖱️ **Thao tác chi tiết**:
  1. Hai người vừa đánh xong một ván cờ trên 2 tab trình duyệt.
  2. Người chơi A bấm nút **"Tái đấu"**.
  3. 💥 **Lỗi hiện ra**: Bàn cờ của Người chơi B **tự động bị xóa sạch và reset vào ván mới ngay lập tức**, không hề có bất kỳ popup hay lời mời hỏi xem Người B có đồng ý chơi tiếp hay không!
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/GamesController.cs`, dòng 441-480.
  * **Cách Fix**: Tách làm 2 pha: Gửi lời mời `OfferRematch` trước, chỉ tạo ván mới khi đối phương bấm `AcceptRematch`.
* **Ảnh chứng cứ**: [TC_ONL_016.png](E2E_SCREENSHOTS/TC_ONL_016.png)

#### 4. BUG_014 (`TC_ADM_005`): Trang Admin CMS hoàn toàn không có nút "Khóa tài khoản"
* 🎯 **Điều kiện kích hoạt 100%**: Xem trực tiếp trên màn hình Quản trị Admin!
* 🖱️ **Thao tác chi tiết**:
  1. Đăng nhập tài khoản Admin (`admin` / `Admin@123`).
  2. Vào menu **Quản trị người dùng** (`http://localhost:5000/Admin/Users`).
  3. 💥 **Lỗi hiện ra**: Tài liệu phân tích yêu cầu Admin có quyền khóa tài khoản vi phạm, nhưng trên giao diện **chỉ có 2 nút: "Đổi quyền Admin" và "Xóa"**, hoàn toàn KHÔNG CÓ nút Khóa tài khoản vì tính năng chưa được lập trình!
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Models/User.cs` & `AdminController.cs`: Thêm cột `IsLocked` vào CSDL và tạo action `ToggleLockUser`.
* **Ảnh chứng cứ**: [TC_ADM_005.png](E2E_SCREENSHOTS/TC_ADM_005.png)

#### 5. BUG_015 (`TC_ADM_008`): Form Admin CMS cho phép lưu bài cờ thế với chuỗi FEN rác
* 🎯 **Điều kiện kích hoạt 100%**: Thao tác trực tiếp trên form thêm cờ thế của Admin!
* 🖱️ **Thao tác chi tiết**:
  1. Vào menu **Quản trị cờ thế** (`http://localhost:5000/Admin/Puzzles`).
  2. Nhập Tiêu đề: *"Thế cờ rác"*, ô FEN nhập chuỗi rác: `"chuoi_fen_rac_123"`.
  3. Bấm nút **"Thêm thế cờ"**.
  4. 💥 **Lỗi hiện ra**: Giao diện báo xanh: *"Đã thêm thế cờ thành công!"*. Sau đó người dùng vào chơi thế cờ này thì bàn cờ bị crash trắng trang!
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AdminController.cs`, dòng 75-88: Bổ sung gọi `Board.TryParseFen` để validate cú pháp trước khi lưu vào CSDL.
* **Ảnh chứng cứ**: [TC_ADM_008.png](E2E_SCREENSHOTS/TC_ADM_008.png)

#### 6. BUG_005 (`TC_COU_009`): Chuyển hướng âm thầm (Silent Redirect) nuốt mất thông báo cảnh báo chứng chỉ
* 🎯 **Điều kiện kích hoạt 100%**: Gõ trực tiếp URL `/Course/Certificate` khi chưa đủ điều kiện!
* 🖱️ **Thao tác chi tiết**:
  1. Đăng nhập tài khoản mới (0% tiến độ học).
  2. Gõ trực tiếp: `http://localhost:5000/Course/Certificate` rồi bấm Enter.
  3. 💥 **Lỗi hiện ra**: Trang web **đột ngột bị đá văng về `/Course` trong im lặng**, hoàn toàn không hiện thông báo lỗi `TempData["CertificateLockMessage"]` vì giao diện `Views/Course/Index.cshtml` quên code thẻ hiển thị thông báo!
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Views/Course/Index.cshtml`: Bổ sung `@if (TempData["CertificateLockMessage"] != null)` để hiện banner cảnh báo màu vàng giải thích lý do cho người dùng.
* **Ảnh chứng cứ**: [TC_COU_009.png](E2E_SCREENSHOTS/TC_COU_009.png)

#### 7. BUG_007 (`TC_ADM_009`): Xóa bài cờ thế ở 2 tab không có thông báo lỗi 404
* 🎯 **Điều kiện kích hoạt 100%**: Mở 2 tab quản trị `/Admin/Puzzles` cùng lúc!
* 🖱️ **Thao tác chi tiết**:
  1. Mở 2 tab trình duyệt cùng xem trang `/Admin/Puzzles`.
  2. Ở Tab 1: Bấm nút "Xóa" một bài cờ thế bất kỳ.
  3. Sang Tab 2 (chưa F5): Bấm nút "Xóa" tiếp vào chính bài cờ vừa bị xóa.
  4. 💥 **Lỗi hiện ra**: Hệ thống không báo lỗi "Bài cờ thế không tồn tại (404)" mà tải lại trang bình thường như thể vừa xóa thành công lần 2.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AdminController.cs`, dòng 121-133: Thêm kiểm tra `if (puzzle == null) return NotFound();`.
* **Ảnh chứng cứ**: [TC_ADM_009.png](E2E_SCREENSHOTS/TC_ADM_009.png)

---

### 💻 PHÂN NHÓM C: CÁC LỖI PHẢI DÙNG SCRIPT / F12 CONSOLE / GỌI API MỚI HIỆN LỖI (10 LỖI)

#### 8. BUG_009 (`TC_AUTH_016`): Nhận thưởng nhiệm vụ không kiểm tra điều kiện & Cho phép spam (Tính năng Backend API)
* 🎯 **Điều kiện kích hoạt 100%**: Tính năng "Nhiệm vụ ngày" hiện mới chỉ được lập trình ở tầng REST API Backend (`/api/quests/daily` và `/api/quests/{id}/claim`) phục vụ Mobile App chứ **chưa gắn nút bấm lên giao diện Website**. Do đó lỗi này được kích hoạt qua F12 Console hoặc Postman!
* 💻 **Cách test**: Mở F12 Console trên bất kỳ trang web nào, dán lệnh:
  ```javascript
  fetch('/api/quests/ai/claim', {method: 'POST'})
    .then(r => r.json())
    .then(console.log);
  ```
* 💥 **Kết quả**: Server trả về `HTTP 200 OK: {"success":true,"questId":"ai","message":"Nhận thưởng thành công!"}` dù bạn chưa hề thắng trận AI nào (`current = 0`)! Và nếu dán gọi liên tục nhiều lần thì server vẫn trả 200 OK và cộng dồn điểm thưởng liên tục.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/QuestController.cs`, dòng 117-146 trong hàm `ClaimQuest`.
  * **Cách Fix**: Kiểm tra điều kiện nhiệm vụ trước khi cho nhận: Phải đạt `current >= target` và kiểm tra cờ `!isClaimed`. Khi nhận thưởng phải cập nhật trạng thái đã nhận trong một Transaction duy nhất.
* **Ảnh chứng cứ**: [TC_AUTH_016.png](E2E_SCREENSHOTS/TC_AUTH_016.png)

#### 9. BUG_001 (`TC_AUTH_004`): Đăng ký mật khẩu < 6 ký tự ("123")
* 🎯 **Điều kiện kích hoạt 100%**: Mở F12 Console tại `http://localhost:5000`, dán script:
  ```javascript
  fetch('/api/auth/register', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({username: 'u_p3_' + Date.now(), password: '123', email: 'p3_' + Date.now() + '@test.com'})
  }).then(r => r.json()).then(console.log);
  ```
* 💥 **Kết quả**: `HTTP 200 OK`, tạo tài khoản thành công với mật khẩu 3 ký tự (vượt qua HTML5 minlength của giao diện web).
* **Vị trí code & Hướng dẫn Fix**: `AuthController.cs` dòng 697-700: Thêm `if (request.Password.Length < 6) return BadRequest();`.
* **Ảnh chứng cứ**: [TC_AUTH_004.png](E2E_SCREENSHOTS/TC_AUTH_004.png)

#### 10. BUG_002 (`TC_AUTH_001`): Đăng ký Email rác không có domain
* 🎯 **Điều kiện kích hoạt 100%**: Mở F12 Console dán script gửi email không có `@` và domain:
  ```javascript
  fetch('/api/auth/register', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({username: 'u_bad_' + Date.now(), password: 'Password123!', email: 'chuoi_nay_khong_phai_email'})
  }).then(r => r.json()).then(console.log);
  ```
* 💥 **Kết quả**: `HTTP 200 OK`, lưu email rác vào CSDL (vượt qua input `type="email"` của giao diện web).
* **Vị trí code & Hướng dẫn Fix**: `AuthController.cs` dòng 703-713: Bổ sung Regex xác thực định dạng email.
* **Ảnh chứng cứ**: [TC_AUTH_001.png](E2E_SCREENSHOTS/TC_AUTH_001.png)

#### 11. BUG_006 (`TC_COU_008`): Gian lận điểm trắc nghiệm Quiz 999999%
* 🎯 **Điều kiện kích hoạt 100%**: Mở F12 Console dán script hack điểm:
  ```javascript
  fetch('/api/courses/progress', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({lessonId: 1, quizScore: 999999, quizAttempts: 1, lastPosition: 100})
  }).then(r => r.json()).then(console.log);
  ```
* 💥 **Kết quả**: `HTTP 200 OK`, server lưu điểm 999999% vào học bạ (giao diện web chỉ tự chấm max 100%).
* **Vị trí code & Hướng dẫn Fix**: `CourseController.cs` dòng 165-175: Thêm kiểm tra `Math.Clamp(quizScore, 0, 100)`.
* **Ảnh chứng cứ**: [TC_COU_008.png](E2E_SCREENSHOTS/TC_COU_008.png)

#### 12. BUG_010 (`TC_AUTH_010`): Tấn công Brute-Force mật khẩu không bị chặn
* 🎯 **Điều kiện kích hoạt 100%**: Mở F12 Console chạy script bắn 5 request sai liên tiếp trong 0.02s:
  ```javascript
  for(let i=0; i<5; i++) {
    fetch('/api/auth/login', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({username: 'admin', password: 'WrongPass' + i})
    }).then(r => console.log('Lần ' + (i+1) + ' Status:', r.status));
  }
  ```
* 💥 **Kết quả**: Cả 5 request đều trả về `HTTP 401` tức thì mà không bị delay, không có CAPTCHA, không khóa IP.
* **Vị trí code & Hướng dẫn Fix**: `Program.cs` & `AuthController.cs`: Tích hợp `AspNetCoreRateLimit` giới hạn 5 lần/phút/IP.
* **Ảnh chứng cứ**: [TC_AUTH_010.png](E2E_SCREENSHOTS/TC_AUTH_010.png)

#### 13. BUG_011 (`TC_GAME_013`): Tạo ván cờ với FEN rác nuốt lỗi
* 🎯 **Điều kiện kích hoạt 100%**: Mở F12 Console dán script:
  ```javascript
  fetch('/api/games?initialFen=fen_hong_sai_cu_phap_123', {method: 'POST'}).then(r => r.json()).then(console.log);
  ```
* 💥 **Kết quả**: `HTTP 201 Created`, backend nuốt lỗi và tự động gán bàn cờ mặc định ban đầu thay vì báo lỗi 400.
* **Vị trí code & Hướng dẫn Fix**: `GamesController.cs` dòng 538-549: Bổ sung `Board.TryParseFen` bắt lỗi cú pháp FEN.
* **Ảnh chứng cứ**: [TC_GAME_013.png](E2E_SCREENSHOTS/TC_GAME_013.png)

#### 14. BUG_013 (`TC_COU_006`): Khách vãng lai gửi LessonId rác 999999
* 🎯 **Điều kiện kích hoạt 100%**: Mở F12 Console dán script:
  ```javascript
  fetch('/api/courses/progress', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({lessonId: 999999, quizScore: 80, isCompleted: true})
  }).then(r => r.json()).then(console.log);
  ```
* 💥 **Kết quả**: `HTTP 200 OK`, server giả lập lưu tiến độ thành công cho bài học không hề tồn tại.
* **Vị trí code & Hướng dẫn Fix**: `CourseController.cs` dòng 155-165: Thêm kiểm tra `_context.CourseLessons.AnyAsync(...)`.
* **Ảnh chứng cứ**: [TC_COU_006.png](E2E_SCREENSHOTS/TC_COU_006.png)

#### 15. BUG_016 (`TC_AUTH_002`): Đăng ký Username chứa mã độc XSS `<script>`
* 🎯 **Điều kiện kích hoạt 100%**: Mở F12 Console dán script:
  ```javascript
  fetch('/api/auth/register', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({username: "<script>alert('XSS_ATTACK')</script>", password: 'Password123!', email: 'xss_' + Date.now() + '@test.com'})
  }).then(r => r.json()).then(console.log);
  ```
* 💥 **Kết quả**: `HTTP 200 OK`, tài khoản được tạo và lưu nguyên văn thẻ `<script>` vào database MySQL.
* **Vị trí code & Hướng dẫn Fix**: `AuthController.cs` dòng 702-705: Thêm Regex chỉ cho phép ký tự chữ và số.
* **Ảnh chứng cứ**: [TC_AUTH_002.png](E2E_SCREENSHOTS/TC_AUTH_002.png)

#### 16. BUG_017 (`TC_GAME_001`): Tạo ván cờ thời gian thi đấu âm (`timeControl: -999`)
* 🎯 **Điều kiện kích hoạt 100%**: Mở F12 Console dán script:
  ```javascript
  fetch('/api/games', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({gameMode: 'Custom', timeControl: -999})
  }).then(r => r.json()).then(console.log);
  ```
* 💥 **Kết quả**: `HTTP 201 Created`, ván cờ được tạo thành công với thời gian âm làm lỗi đồng hồ đếm ngược.
* **Vị trí code & Hướng dẫn Fix**: `GamesController.cs` dòng 538-549: Ràng buộc DTO `[Range(1, 180)]`.
* **Ảnh chứng cứ**: [TC_GAME_001.png](E2E_SCREENSHOTS/TC_GAME_001.png)

#### 17. BUG_003 (`TC_ADM_002`): Gửi request HTTP GET thẳng vào route quản trị `/Admin`
* 🎯 **Điều kiện kích hoạt 100%**: Dùng Postman hoặc script gửi `GET http://localhost:5000/Admin` không kèm cookie.
* 💥 **Kết quả**: Server trả về `HTTP 302 Found` redirect ngầm thay vì chặn `HTTP 403 Forbidden` chuẩn RESTful.
* **Vị trí code & Hướng dẫn Fix**: `Program.cs`: Bổ sung xử lý `OnRedirectToAccessDenied` trả về đúng 403.
* **Ảnh chứng cứ**: [TC_ADM_002.png](E2E_SCREENSHOTS/TC_ADM_002.png)

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

---

## 🏆 KẾT QUẢ TÁI KIỂM THỬ CHU KỲ 2 (DEFECT RE-TESTING ROUND 2)

> 📌 **Thông tin đồng bộ mã nguồn kiểm thử Chu kỳ 2**:
> * **Commit Hash**: `65ae81dd6baed2dc85923a3d7f4644fd8006b0e3` (Short: `65ae81d`)
> * **Thời gian Commit trên Cloud**: `28/10/2026 22:24:20 +07:00`
> * **Tác giả commit**: `nguyentrongnguyen3004-eng <nguyentrongnguyen3004@gmail.com>`
> * **Thời gian Re-testing thực nghiệm**: `28/10/2026 23:35 (UTC+7)`
> * **Sheet chi tiết trong Excel**: [`13_Retest_Round2`](../TestCases_KLCN_CoTuong%20(1).xlsx)

### Bảng tổng hợp kết quả Tái kiểm thử 17 Defects (Round 2)

| STT | Bug ID | TC ID | Phân hệ phát sinh | Tiêu đề lỗi tóm tắt | Trạng thái Lần 1 | Trạng thái Lần 2 | Kết luận nghiệm thu |
| :---: | :---: | :---: | :--- | :--- | :---: | :---: | :---: |
| 1 | `BUG_001` | `TC_AUTH_004` | Xác thực & Tài khoản | Chặn mật khẩu yếu < 6 ký tự | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 2 | `BUG_002` | `TC_AUTH_001` | Xác thực & Tài khoản | Kiểm tra Regex định dạng Email | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 3 | `BUG_003` | `TC_ADM_002` | Phân hệ Quản trị viên | Truy cập /Admin trái phép trả về 403 | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 4 | `BUG_004` | `TC_ONL_015` | Đấu trực tuyến SignalR | Xin hàng kết thúc ván & chặn đi quân | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 5 | `BUG_005` | `TC_COU_009` | Học viện & Kỳ phổ | Chặn chứng chỉ 0% & hiện Banner cảnh báo | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 6 | `BUG_006` | `TC_COU_008` | Học viện & Kỳ phổ | Giới hạn trần điểm Quiz tối đa 100% | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 7 | `BUG_007` | `TC_ADM_009` | Phân hệ Quản trị viên | Xóa cờ thế ID rác trả về HTTP 404 | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 8 | `BUG_008` | `TC_AUTH_011` | Xác thực & Tài khoản | Tuyệt đối không rò rỉ mã OTP bí mật | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 9 | `BUG_009` | `TC_AUTH_016` | Xác thực & Tài khoản | Kiểm tra điều kiện & chống spam nhận thưởng | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 10 | `BUG_010` | `TC_AUTH_010` | Xác thực & Tài khoản | Rate Limiting & Lockout chống Brute-force | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 11 | `BUG_011` | `TC_GAME_013` | Vận hành ván cờ & AI | Bắt lỗi chuỗi FEN rác khi tạo ván cờ | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 12 | `BUG_012` | `TC_ONL_016` | Đấu trực tuyến SignalR | Quy trình Tái đấu 2 pha có sự xác nhận | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 13 | `BUG_013` | `TC_COU_006` | Học viện & Kỳ phổ | Khách gửi Lesson ID rác trả về HTTP 404 | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 14 | `BUG_014` | `TC_ADM_005` | Phân hệ Quản trị viên | Tính năng Khóa / Mở khóa tài khoản (3 tầng) | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 15 | `BUG_015` | `TC_ADM_008` | Phân hệ Quản trị viên | Admin CMS kiểm tra FEN trước khi lưu | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 16 | `BUG_016` | `TC_AUTH_002` | Xác thực & Tài khoản | Chặn Username chứa mã độc XSS `<script>` | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |
| 17 | `BUG_017` | `TC_GAME_001` | Chế độ chơi & AI | Chặn thời gian thi đấu âm khi tạo game | Failed (Open) | 🟢 **FIXED** | Đóng lỗi |

> 🎯 **KẾT LUẬN NGHIỆM THU CHU KỲ 2**: **17/17 LỖI (100.0%) ĐÃ ĐƯỢC KHẮC PHỤC TOÀN DIỆN**. Toàn bộ hệ thống kiểm thử từ Unit Test, API Test đến E2E Automation đều đạt kết quả xuất sắc, hệ thống đạt độ ổn định và an toàn thông tin tối đa, sẵn sàng bảo vệ Khóa luận Tốt nghiệp.

