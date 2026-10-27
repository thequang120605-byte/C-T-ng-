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

## 🔍 PHẦN 3: CẨM NANG TỰ TAY KIỂM THỬ, PHÂN TÍCH CƠ CHẾ HOẠT ĐỘNG & HƯỚNG DẪN FIX 17 DEFECTS

> **Yêu cầu môi trường**: Bật Web Server Kestrel bằng lệnh: `dotnet run --project ChessCore` (server chạy tại `http://localhost:5000`).

---

### 🔴 NHÓM 1: LỖI BẢO MẬT & XÁC THỰC TÀI KHOẢN (AUTH & PROFILE)

#### 1. BUG_001 (`TC_AUTH_004`): Đăng ký mật khẩu yếu dưới 6 ký tự qua API
* **Ngữ cảnh xuất hiện (Trigger Context)**: Trên giao diện Web MVC (`/Auth/Register`), thẻ input có thuộc tính HTML5 `minlength="6"` nên người dùng thông thường bị chặn. Lỗi **CHỈ XUẤT HIỆN khi người dùng hoặc kẻ tấn công cố tình Bypass giao diện** (mở F12 Console, dùng Postman, cURL, hoặc script tự động) gọi trực tiếp vào endpoint REST API `POST /api/auth/register`.
* **Cơ chế hoạt động ngầm (How it works)**: Controller `AuthController.ApiRegister` chỉ kiểm tra `string.IsNullOrWhiteSpace(request.Password)`. Khi nhận chuỗi `"123"`, backend không kiểm tra độ dài tối thiểu, thực hiện băm mật khẩu qua `HashPassword("123")`, ghi bản ghi vào bảng `Users`, cấp `PlayerId` và Bearer Token thành công với `HTTP 200 OK`.
* **Cách test tay**: Mở F12 Console tại `http://localhost:5000`, dán script:
  ```javascript
  fetch('/api/auth/register', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({username: 'u_p3_' + Date.now(), password: '123', email: 'p3_' + Date.now() + '@test.com'})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả thực tế**: `HTTP 200 OK`, tài khoản được tạo với mật khẩu 3 ký tự.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AuthController.cs`, dòng 697-700 trong hàm `ApiRegister`.
  * **Đoạn code cần sửa**:
    ```csharp
    if (request.Password.Length < 6)
    {
        return BadRequest(new { message = "Mật khẩu phải có độ dài tối thiểu 6 ký tự." });
    }
    ```
* **Ảnh chứng cứ**: [TC_AUTH_004.png](E2E_SCREENSHOTS/TC_AUTH_004.png)

---

#### 2. BUG_002 (`TC_AUTH_001`): Thiếu Regex kiểm tra định dạng Email khi đăng ký (Email rác)
* **Ngữ cảnh xuất hiện (Trigger Context)**: Trên giao diện web, thẻ `<input type="email">` của trình duyệt chặn các chuỗi không có `@`. Lỗi **XUẤT HIỆN khi client bypass giao diện** qua script/Postman, gửi email không có dấu `@` hoặc không có domain (ví dụ `"not_an_email_format"`).
* **Cơ chế hoạt động ngầm (How it works)**: Backend `AuthController.ApiRegister` chỉ kiểm tra `!string.IsNullOrEmpty(email)` và truy vấn `_context.Users.AnyAsync(u => u.Email == email)` để chống trùng. Hoàn toàn thiếu bộ lọc Regex. Email rác được ghi vĩnh viễn vào CSDL MySQL. Khi hệ thống gửi email OTP lấy lại mật khẩu sau này, dịch vụ SMTP sẽ bị crash hoặc fail.
* **Cách test tay**: Mở F12 Console, dán:
  ```javascript
  fetch('/api/auth/register', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({username: 'u_bad_' + Date.now(), password: 'Password123!', email: 'chuoi_nay_khong_phai_email'})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả thực tế**: `HTTP 200 OK`, lưu trực tiếp email rác vào CSDL.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AuthController.cs`, dòng 703-713 trong hàm `ApiRegister`.
  * **Đoạn code cần sửa**:
    ```csharp
    var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    if (!string.IsNullOrEmpty(email) && !emailRegex.IsMatch(email))
    {
        return BadRequest(new { message = "Địa chỉ email không đúng định dạng." });
    }
    ```
* **Ảnh chứng cứ**: [TC_AUTH_001.png](E2E_SCREENSHOTS/TC_AUTH_001.png)

---

#### 3. BUG_008 (`TC_AUTH_011`): Rò rỉ mã OTP đặt lại mật khẩu trực tiếp lên giao diện (Critical Security)
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện **trực tiếp trên giao diện Web người dùng thật** tại trang `/Auth/ForgotPassword` khi yêu cầu khôi phục mật khẩu mà máy chủ gặp sự cố không gửi được email (chưa cấu hình Gmail App Password hoặc mạng chặn cổng SMTP 587).
* **Cơ chế hoạt động ngầm (How it works)**: Trong `AuthController.cs` dòng 315-319, khi hàm `SendOtpEmailAsync` trả về `false`, lập trình viên đã dùng biến gỡ lỗi:
  `TempData["DevOtp"] = otp;` và `TempData["OtpMessage"] = $"Lỗi khi gửi email qua Gmail. MÃ OTP CỦA BẠN LÀ: {otp}";`
  Controller redirect sang `/Auth/ResetPassword`, và mã OTP 6 số hiển thị to rõ trên màn hình web. Bất kỳ ai chỉ cần biết email của đối phương là có thể lấy OTP và đổi mật khẩu mà không cần quyền truy cập hộp thư.
* **Cách test tay**:
  1. Vào trình duyệt, truy cập: `http://localhost:5000/Auth/ForgotPassword`
  2. Nhập email của tài khoản bất kỳ (ví dụ: `admin@cotuong.pro` hoặc email đã đăng ký)
  3. Bấm nút **Gửi mã OTP**.
* **Kết quả thực tế**: Màn hình hiển thị: *"Lỗi khi gửi email qua Gmail. MÃ OTP CỦA BẠN LÀ: 123456"*.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AuthController.cs`, dòng 315-319 trong hàm `ForgotPassword`.
  * **Đoạn code cần sửa**: Xóa bỏ hoàn toàn 2 dòng gán OTP vào TempData. Khi email thất bại chỉ báo thông báo an toàn:
    ```csharp
    TempData["ErrorMessage"] = "Không thể gửi email xác thực lúc này. Vui lòng liên hệ quản trị viên hoặc thử lại sau.";
    ```
* **Ảnh chứng cứ**: [TC_AUTH_011.png](E2E_SCREENSHOTS/TC_AUTH_011.png)

---

#### 4. BUG_009 (`TC_AUTH_016`): Nhận thưởng nhiệm vụ không kiểm tra điều kiện & Cho phép spam
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện khi người chơi bấm liên tục (spam click) vào nút "Nhận thưởng" nhiệm vụ hàng ngày trên giao diện, hoặc gọi API `POST /api/quests/{id}/claim` bằng tool tự động.
* **Cơ chế hoạt động ngầm (How it works)**: Endpoint `POST /api/quests/{id}/claim` không kiểm tra tiến độ người chơi (ví dụ: đã thắng ván AI nào trong ngày chưa) và không có cờ `IsClaimed = true` khóa bản ghi trong ngày. Cứ gửi request là backend trả `HTTP 200 OK` và cộng dồn điểm thưởng `PuzzleScore` vô hạn.
* **Cách test tay**: Mở F12 Console, chạy lệnh spam:
  ```javascript
  for(let i = 0; i < 5; i++) {
    fetch('/api/quests/ai/claim', {method: 'POST'}).then(r => r.json()).then(res => console.log('Lần ' + (i+1) + ':', res));
  }
  ```
* **Kết quả thực tế**: Cả 5 lần đều trả về `HTTP 200 OK: {"success":true,"message":"Nhận thưởng thành công!"}` và cộng dồn điểm liên tục.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/QuestsController.cs` (hoặc action claim trong AuthController).
  * **Đoạn code cần sửa**: Kiểm tra trong bảng `UserDailyQuestProgress`: 1. Điều kiện hoàn thành `CurrentProgress >= TargetCount`; 2. Điều kiện chưa nhận `!IsClaimed`. Khi phát thưởng phải set `IsClaimed = true` trong một Database Transaction duy nhất.
* **Ảnh chứng cứ**: [TC_AUTH_016.png](E2E_SCREENSHOTS/TC_AUTH_016.png)

---

#### 5. BUG_010 (`TC_AUTH_010`): Thiếu Rate Limiting và khóa tài khoản chống Brute-force
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện khi kẻ tấn công dùng công cụ tự động (Hydra, Burp Suite, script Python) gửi hàng loạt request đoán mật khẩu vào `POST /api/auth/login`.
* **Cơ chế hoạt động ngầm (How it works)**: Backend xử lý mỗi request login sai chỉ mất 0.01 - 0.02 giây và trả `HTTP 401 Unauthorized`. Không có middleware đếm số lần thất bại, không có khóa tạm thời (Lockout), không có CAPTCHA. Kẻ tấn công có thể thử hàng triệu mật khẩu trong thời gian ngắn mà không bị ngắt kết nối.
* **Cách test tay**: Mở F12 Console, chạy script gửi 5 request sai liên tiếp:
  ```javascript
  for(let i = 0; i < 5; i++) {
    fetch('/api/auth/login', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({username: 'admin', password: 'WrongPassword' + i})
    }).then(r => console.log('Lần ' + (i+1) + ' Status:', r.status));
  }
  ```
* **Kết quả thực tế**: Cả 5 request đều phản hồi `HTTP 401` tức thì mà không hề bị làm chậm hay khóa IP.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AuthController.cs`, dòng 626-660 & `Program.cs`.
  * **Đoạn code cần sửa**: Tích hợp thư viện `AspNetCoreRateLimit` trong `Program.cs` giới hạn 5 request/phút/IP. Thêm thuộc tính `AccessFailedCount` và `LockoutEnd` vào model `User` để khóa tài khoản tạm thời 15 phút nếu sai quá 5 lần.
* **Ảnh chứng cứ**: [TC_AUTH_010.png](E2E_SCREENSHOTS/TC_AUTH_010.png)

---

#### 6. BUG_016 (`TC_AUTH_002`): Cho phép đăng ký Username chứa mã độc XSS `<script>` (Stored XSS)
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện khi người dùng cố tình nhập chuỗi mã độc `<script>alert('XSS')</script>` vào ô Username khi đăng ký tài khoản (qua form web hoặc qua API REST).
* **Cơ chế hoạt động ngầm (How it works)**: `AuthController.ApiRegister` chỉ gọi `.Trim()` chuỗi username, hoàn toàn không gọi `HttpUtility.HtmlEncode()` và không dùng Regex kiểm tra ký tự an toàn. Chuỗi mã độc được ghi nguyên bản vào database MySQL. Khi tên người dùng này được hiển thị trên Bảng xếp hạng, danh sách kỳ thủ hoặc CMS Admin qua `@Html.Raw()` hoặc `innerHTML`, mã script độc hại sẽ tự động thực thi trên máy người xem.
* **Cách test tay**: Mở F12 Console, dán:
  ```javascript
  fetch('/api/auth/register', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({username: "<script>alert('XSS_ATTACK')</script>", password: 'Password123!', email: 'xss_' + Date.now() + '@test.com'})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả thực tế**: `HTTP 200 OK`, tài khoản được tạo và lưu nguyên văn `<script>` vào CSDL!
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AuthController.cs`, dòng 702-705 trong hàm `ApiRegister`.
  * **Đoạn code cần sửa**: Áp dụng Regex chỉ cho phép chữ cái và số (alphanumeric):
    ```csharp
    var usernameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9_\.]{3,20}$");
    if (!usernameRegex.IsMatch(username))
    {
        return BadRequest(new { message = "Tên người dùng chỉ được chứa chữ cái, số, dấu gạch dưới và dấu chấm (3-20 ký tự)." });
    }
    ```
* **Ảnh chứng cứ**: [TC_AUTH_002.png](E2E_SCREENSHOTS/TC_AUTH_002.png)

---

### 🔴 NHÓM 2: LỖI VẬN HÀNH TRẬN ĐẤU & ONLINE SIGNALR

#### 7. BUG_004 (`TC_ONL_015`): Vẫn nhận nước cờ sau khi người chơi đã Đầu hàng (Blocker)
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện **trực tiếp trên giao diện thi đấu Online (PvP)** khi 2 người chơi thật đang thi đấu, một bên bấm nút "Xin hàng" và bấm "Đồng ý" trên modal xác nhận.
* **Cơ chế hoạt động ngầm (How it works)**:
  1. Trong file [`wwwroot/js/game.js` dòng 2170-2175](file:///c:/Users/ADMIN/Desktop/C-T-ng-/ChessCore/wwwroot/js/game.js#L2170-L2175), hàm `resignGame()` thực hiện `fetch('/api/games/' + gameId + '/resign')` nhưng **quên đính kèm Header Authorization Bearer Token**.
  2. Backend [`GamesController.cs` dòng 1220](file:///c:/Users/ADMIN/Desktop/C-T-ng-/ChessCore/Controllers/GamesController.cs#L1220) kiểm tra `if (!TryGetCurrentUserId(out var resigningPlayerId)) return Unauthorized();`. Do thiếu token, server trả về `HTTP 401 Unauthorized`.
  3. Giao diện báo lỗi nhưng **không thay đổi trạng thái ván cờ trên server**. Trạng thái ván cờ trên database vẫn giữ nguyên `InProgress`. Người chơi vẫn tiếp tục kéo thả quân và gửi nước đi `POST /api/games/{id}/moves` thành công với `HTTP 200 OK`.
* **Cách test tay**: Tạo bàn cờ, gọi đầu hàng thiếu token rồi gửi nước đi hợp lệ:
  ```powershell
  # Kiểm tra qua API runner:
  python -c "import requests; r=requests.post('http://localhost:5000/api/games'); gid=r.json()['id']; r_surr=requests.post(f'http://localhost:5000/api/games/{gid}/surrender'); r_move=requests.post(f'http://localhost:5000/api/games/{gid}/moves', json={'fromX':7,'fromY':1,'toX':7,'toY':4}); print('Move Status:', r_move.status_code)"
  ```
* **Kết quả thực tế**: `Move Status: 200 OK`, ván cờ không bị khóa sau khi xin hàng!
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File 1**: `ChessCore/wwwroot/js/game.js`, dòng 2173: Bổ sung header xác thực `headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token }`.
  * **File 2**: `ChessCore/Controllers/GamesController.cs`, dòng 1220: Hỗ trợ fallback nhận dạng danh tính người chơi qua Cookie Claims của phiên web nếu request thiếu Bearer Token.
* **Ảnh chứng cứ**: [TC_ONL_015.png](E2E_SCREENSHOTS/TC_ONL_015.png)

---

#### 8. BUG_011 (`TC_GAME_013`): Nhập FEN hỏng nuốt lỗi và tự gán thế cờ mặc định
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện khi client hoặc API ngoài gọi `POST /api/games` truyền chuỗi `initialFen` sai cú pháp cờ tướng (thiếu quân, sai ký tự phân cách, số hàng không đủ 10).
* **Cơ chế hoạt động ngầm (How it works)**: Trong `GamesController.CreateGame`, khi gọi `GameRules.RebuildBoard` hoặc `Board.TryParseFen` thất bại, backend bắt ngoại lệ nhưng âm thầm nuốt lỗi và gán `initialFen = null`. Server trả về mã **`HTTP 201 Created`** và tự động dựng ra bàn cờ mặc định ban đầu thay vì báo lỗi `400 Bad Request`.
* **Cách test tay**: Mở F12 Console, gửi chuỗi FEN rác:
  ```javascript
  fetch('/api/games?initialFen=fen_hong_sai_cu_phap_123', {method: 'POST'}).then(r => r.json()).then(console.log);
  ```
* **Kết quả thực tế**: `HTTP 201 Created`, response trả về bàn cờ mặc định với `initialFen: null`.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/GamesController.cs`, dòng 538-549 trong hàm `CreateGame`.
  * **Đoạn code cần sửa**:
    ```csharp
    if (!string.IsNullOrWhiteSpace(initialFen) && !Board.TryParseFen(initialFen, out _, out string fenError))
    {
        return BadRequest(new { message = $"Chuỗi FEN khởi tạo không hợp lệ: {fenError}" });
    }
    ```
* **Ảnh chứng cứ**: [TC_GAME_013.png](E2E_SCREENSHOTS/TC_GAME_013.png)

---

#### 9. BUG_012 (`TC_ONL_016`): Tái đấu (Rematch) ép đối thủ vào ván mới không cần đồng ý
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện ở chế độ Đấu trực tuyến (PvP Online): Khi ván cờ kết thúc, một bên người chơi bấm nút "Tái đấu" hoặc gọi `POST /api/games/{id}/rematch`.
* **Cơ chế hoạt động ngầm (How it works)**: Trong `GamesController.CreateRematch` (dòng 441-475), khi nhận request rematch từ 1 phía, backend lập tức gọi `_gameService.CreateGameAsync(status: GameStatus.InProgress)`. Server đảo màu quân và phát tín hiệu SignalR `RematchStarted` ép cả 2 client tải bàn cờ mới ngay lập tức mà không có pha trung gian chờ đối thủ bấm Đồng ý / Từ chối.
* **Cách test tay**: Gọi `POST /api/games/1/rematch` qua Console hoặc Postman.
* **Kết quả thực tế**: `HTTP 200 OK`, bàn cờ mới được sinh ra ngay tức thì.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/GamesController.cs`, dòng 441-480.
  * **Đoạn code cần sửa**: Tách quy trình làm 2 bước: Bước 1 `OfferRematch` chỉ phát tín hiệu SignalR `RematchOffered` đến đối phương; Bước 2 `AcceptRematch` chỉ khi đối phương xác nhận đồng ý thì mới khởi tạo ván đấu mới.
* **Ảnh chứng cứ**: [TC_ONL_016.png](E2E_SCREENSHOTS/TC_ONL_016.png)

---

#### 10. BUG_017 (`TC_GAME_001`): API tạo ván cờ chấp nhận thời gian thi đấu âm (timeControl: -999)
* **Ngữ cảnh xuất hiện (Trigger Context)**: Trên giao diện web, modal chọn thời gian chỉ cho chọn giá trị dương (5, 10, 15 phút). Lỗi **CHỈ XUẤT HIỆN khi client bypass giao diện** qua Postman hoặc script gửi `POST /api/games` kèm `{"gameMode": "Custom", "timeControl": -999}`.
* **Cơ chế hoạt động ngầm (How it works)**: Hàm `CreateGame` không ràng buộc DTO có validation cho `timeControl`. Server chấp nhận request, trả về **`HTTP 201 Created`** và gán thời gian ván cờ là số âm. Khi đồng hồ đếm ngược kích hoạt, hàm tính thời gian `TimeSpan` bị lỗi toán học làm đồng hồ nhảy loạn hoặc xử thua ngay tức thì.
* **Cách test tay**: Mở F12 Console, chạy:
  ```javascript
  fetch('/api/games', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({gameMode: 'Custom', timeControl: -999})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả thực tế**: `HTTP 201 Created`, ván cờ được tạo thành công với thời gian -999 phút!
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/GamesController.cs`, dòng 538-549 và DTO tạo game.
  * **Đoạn code cần sửa**: Tạo DTO có DataAnnotation giới hạn biên:
    ```csharp
    [Range(1, 180, ErrorMessage = "Thời gian thi đấu phải từ 1 đến 180 phút.")]
    public int TimeControl { get; set; } = 10;
    ```
* **Ảnh chứng cứ**: [TC_GAME_001.png](E2E_SCREENSHOTS/TC_GAME_001.png)

---

### 🔴 NHÓM 3: LỖI HỌC VIỆN KHÓA HỌC & CẤP CHỨNG CHỈ (COURSE)

#### 11. BUG_005 (`TC_COU_009`): Cấp Chứng chỉ tốt nghiệp cho người chưa học bài nào (0% tiến độ)
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện khi người dùng mới tạo tài khoản (0% tiến độ học, chưa hoàn thành bài nào) gõ trực tiếp URL `/Course/Certificate` hoặc `/Course/Certificate?courseId=1` trên thanh địa chỉ trình duyệt.
* **Cơ chế hoạt động ngầm (How it works)**: Ban đầu tính năng E-Certificate kết xuất trực tiếp View mà không khóa chặt chẽ. Hệ thống vẫn kết xuất template chứng chỉ CERT-CT hoàn chỉnh kèm họ tên và mã số chứng nhận dù database `UserCourseProgresses` có 0 bản ghi hoàn thành.
* **Cách test tay**: Tạo tài khoản mới, sau đó truy cập: `http://localhost:5000/Course/Certificate`.
* **Kết quả thực tế**: Giao diện chứng chỉ tốt nghiệp điện tử CERT-CT hiển thị trang trọng với họ tên người học.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/CourseController.cs`, dòng 229-239 & `Views/Course/Certificate.cshtml`.
  * **Đoạn code cần sửa**: Khi `!isEligible`, Controller phải trả về `Forbid()` hoặc View thông báo từ chối cấp chứng chỉ (kèm tiến độ hiện tại), tuyệt đối không render layout chứng chỉ hoàn thành.
* **Ảnh chứng cứ**: [TC_COU_009.png](E2E_SCREENSHOTS/TC_COU_009.png)

---

#### 12. BUG_006 (`TC_COU_008`): Chấp nhận điểm trắc nghiệm Quiz vượt ngưỡng 100% (999999 điểm)
* **Ngữ cảnh xuất hiện (Trigger Context)**: Trên web, bài trắc nghiệm chỉ có 4 đáp án và tự tính điểm tối đa 100 điểm. Lỗi **XUẤT HIỆN khi học viên cố tình dùng F12 Console hoặc Postman** gọi API `POST /api/courses/progress` để gian lận điểm số.
* **Cơ chế hoạt động ngầm (How it works)**: Controller `CourseController.ExecuteSaveProgress` nhận DTO `CourseProgressRequest request`. DTO lấy trực tiếp `request.QuizScore` (số nguyên) gán thẳng vào `progress.BestQuizScore = request.QuizScore` mà không kiểm tra cận trên [0, 100]. Giá trị 999999 được lưu vĩnh viễn vào CSDL và trả về `HTTP 200 OK`.
* **Cách test tay**: Mở F12 Console, dán:
  ```javascript
  fetch('/api/courses/progress', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({lessonId: 1, quizScore: 999999, quizAttempts: 1, lastPosition: 100})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả thực tế**: `HTTP 200 OK`, response trả về `bestQuizScore: 999999`.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/CourseController.cs`, dòng 165-175 trong hàm `SaveProgress`.
  * **Đoạn code cần sửa**:
    ```csharp
    if (request.QuizScore < 0 || request.QuizScore > 100)
    {
        return BadRequest(new { message = "Điểm trắc nghiệm Quiz phải nằm trong khoảng từ 0 đến 100." });
    }
    ```
* **Ảnh chứng cứ**: [TC_COU_008.png](E2E_SCREENSHOTS/TC_COU_008.png)

---

#### 13. BUG_013 (`TC_COU_006`): API tiến độ của Khách trả về 200 cho Lesson ID không tồn tại
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện khi người dùng chưa đăng nhập (Khách vãng lai) học bài hoặc gọi API `POST /api/courses/progress` với `lessonId = 999999` (ID rác không có trong CSDL).
* **Cơ chế hoạt động ngầm (How it works)**: Trong `CourseController.ExecuteSaveProgress`, khi `userId is null` (khách chưa đăng nhập), code rơi vào nhánh xử lý cho Guest. Backend không truy vấn CSDL để kiểm tra bài học có tồn tại hay không mà lập tức trả về `Ok(new { progress = ..., isGuest = true })` với mã `HTTP 200 OK`.
* **Cách test tay**: Mở F12 Console, gửi request với lessonId rác:
  ```javascript
  fetch('/api/courses/progress', {
    method: 'POST',
    headers: {'Content-Type': 'application/json'},
    body: JSON.stringify({lessonId: 999999, quizScore: 80, isCompleted: true})
  }).then(r => r.json()).then(console.log);
  ```
* **Kết quả thực tế**: `HTTP 200 OK: {"progress":{"isCompleted":true,"bestQuizScore":80},"isGuest":true}`.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/CourseController.cs`, dòng 155-165 trong `ExecuteSaveProgress`.
  * **Đoạn code cần sửa**:
    ```csharp
    if (!await _context.CourseLessons.AnyAsync(l => l.Id == request.LessonId))
    {
        return NotFound(new { message = $"Bài học ID #{request.LessonId} không tồn tại trong hệ thống." });
    }
    ```
* **Ảnh chứng cứ**: [TC_COU_006.png](E2E_SCREENSHOTS/TC_COU_006.png)

---

### 🔴 NHÓM 4: LỖI PHÂN HỆ QUẢN TRỊ VIÊN (ADMIN SYSTEM)

#### 14. BUG_003 (`TC_ADM_002`): Truy cập /Admin khi chưa đăng nhập trả về 302 thay vì 403 Forbidden
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện khi client chưa đăng nhập hoặc tài khoản chỉ có role `Player` (hoặc bot crawler) truy cập trực tiếp đường dẫn `/Admin` trên trình duyệt hoặc gửi request HTTP GET.
* **Cơ chế hoạt động ngầm (How it works)**: `AdminController` có attribute `[Authorize]`. Mặc định của ASP.NET Core Cookie Authentication khi gặp request chưa xác thực sẽ tự động chuyển hướng `HTTP 302 Found` sang `/Auth/Login?ReturnUrl=%2FAdmin`. Theo chuẩn RESTful và OWASP, route quản trị nhạy cảm không được redirect ngầm mà phải trả mã lỗi `HTTP 403 Forbidden` hoặc `401 Unauthorized`.
* **Cách test tay**: Gửi request HTTP GET không kèm cookie phiên:
  ```powershell
  python -c "import requests; r=requests.get('http://localhost:5000/Admin', allow_redirects=False); print('Status:', r.status_code, 'Location:', r.headers.get('Location'))"
  ```
* **Kết quả thực tế**: `HTTP 302 Found`, redirect sang `/Auth/Login?ReturnUrl=%2FAdmin` thay vì trả `403 Forbidden`.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Program.cs` cấu hình Cookie Authentication.
  * **Đoạn code cần sửa**: Trong cấu hình `AddCookie` bổ sung xử lý `OnRedirectToAccessDenied` để trả về StatusCode 403 Forbidden.
* **Ảnh chứng cứ**: [TC_ADM_002.png](E2E_SCREENSHOTS/TC_ADM_002.png)

---

#### 15. BUG_007 (`TC_ADM_009`): Xóa bài cờ thế với ID không tồn tại trả về HTTP 200/302
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện khi Admin thực hiện xóa bài cờ thế mà bài đó đã bị xóa trước đó ở tab khác, hoặc Admin/kẻ tấn công gửi `POST /Admin/DeletePuzzle` với ID không tồn tại (`id = 999999`).
* **Cơ chế hoạt động ngầm (How it works)**: Hàm `AdminController.DeletePuzzle(int id)` tìm kiếm bài cờ theo id. Khi trả về `null`, hàm bỏ qua việc xóa nhưng không quăng lỗi 404 mà vẫn chuyển hướng `HTTP 302 Found` về trang danh sách, khiến client hiểu nhầm là thao tác thành công.
* **Cách test tay**: Gửi request xóa bài cờ thế ID 999999:
  ```powershell
  python -c "import requests; r=requests.post('http://localhost:5000/Admin/DeletePuzzle', data={'id': 999999}, allow_redirects=False); print('Status:', r.status_code)"
  ```
* **Kết quả thực tế**: `HTTP 302 Found` thay vì trả về `HTTP 404 Not Found`.
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AdminController.cs`, dòng 121-133 trong hàm `DeletePuzzle`.
  * **Đoạn code cần sửa**:
    ```csharp
    var puzzle = await _context.Puzzles.FindAsync(id);
    if (puzzle == null)
    {
        return NotFound(new { message = $"Không tìm thấy thế cờ ID #{id} để xóa." });
    }
    ```
* **Ảnh chứng cứ**: [TC_ADM_009.png](E2E_SCREENSHOTS/TC_ADM_009.png)

---

#### 16. BUG_014 (`TC_ADM_005`): Tính năng Khóa / Mở khóa tài khoản hoàn toàn chưa được code
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện **trực tiếp trên giao diện Admin CMS** tại trang Quản trị người dùng (`/Admin/Users`). Trong tài liệu đặc tả nghiệp vụ có yêu cầu Admin có quyền khóa tài khoản vi phạm.
* **Cơ chế hoạt động ngầm (How it works)**: Model [`User.cs`](file:///c:/Users/ADMIN/Desktop/C-T-ng-/ChessCore/Models/User.cs) hoàn toàn thiếu trường `IsLocked` hoặc `IsActive`. Controller [`AdminController.cs`](file:///c:/Users/ADMIN/Desktop/C-T-ng-/ChessCore/Controllers/AdminController.cs) chỉ có 2 action là `ToggleAdminRole` và `DeleteUser`. Tính năng khóa người dùng hoàn toàn chưa được lập trình (Missing Feature).
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File 1**: `ChessCore/Models/User.cs`: Thêm thuộc tính `public bool IsLocked { get; set; } = false;` và chạy `dotnet ef migrations add AddIsLockedToUser`.
  * **File 2**: `ChessCore/Controllers/AdminController.cs`: Viết thêm Action `ToggleLockUser(int userId)`.
  * **File 3**: `ChessCore/Controllers/AuthController.cs`: Kiểm tra `if (user.IsLocked) return BadRequest("Tài khoản đã bị khóa do vi phạm quy chế.");` khi đăng nhập.
* **Ảnh chứng cứ**: [TC_ADM_005.png](E2E_SCREENSHOTS/TC_ADM_005.png)

---

#### 17. BUG_015 (`TC_ADM_008`): Admin CMS cho phép thêm bài cờ thế với FEN rác vào CSDL
* **Ngữ cảnh xuất hiện (Trigger Context)**: Xuất hiện **trực tiếp trên giao diện Admin CMS** tại `/Admin/Puzzles` khi quản trị viên tạo bài cờ thế mới và nhập chuỗi FEN rác hoặc sai cú pháp cờ tướng (ví dụ `"fen_rac_123"`) rồi bấm "Lưu bài cờ thế".
* **Cơ chế hoạt động ngầm (How it works)**: Trong `AdminController.CreatePuzzle` dòng 75-88, hệ thống chỉ kiểm tra `string.IsNullOrWhiteSpace(puzzle.Fen)`. Hoàn toàn không gọi hàm `Board.TryParseFen(puzzle.Fen, out _, out _)`. Chuỗi FEN rác được lưu thẳng vào CSDL. Khi người chơi vào mục Cờ Thế mở bài học này, bàn cờ sẽ bị văng lỗi (Crash Exception / White screen).
* **Vị trí code & Hướng dẫn Fix cho Dev**:
  * **File**: `ChessCore/Controllers/AdminController.cs`, dòng 75-88 trong `CreatePuzzle`.
  * **Đoạn code cần sửa**:
    ```csharp
    if (!Board.TryParseFen(puzzle.Fen.Trim(), out _, out string fenError))
    {
        TempData["ErrorMessage"] = $"Chuỗi FEN không hợp lệ theo luật cờ tướng: {fenError}";
        return RedirectToAction(nameof(Puzzles));
    }
    ```
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

