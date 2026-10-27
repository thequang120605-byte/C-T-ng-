# HỆ THỐNG WEBSITE CỜ TƯỚNG TRỰC TUYẾN TÍCH HỢP TRÍ TUỆ NHÂN TẠO (CHESSCORE)
## ĐỒ ÁN TỐT NGHIỆP CÔNG NGHỆ THÔNG TIN - ĐẠI HỌC CÔNG THƯƠNG TP.HCM (HUIT)

---

## 📌 1. TỔNG QUAN DỰ ÁN

**ChessCore** là nền tảng cờ tướng trực tuyến toàn diện, kết hợp công nghệ web thời gian thực (.NET 9, SignalR) và Trí tuệ Nhân tạo (Minimax, Alpha-Beta Pruning, Đánh giá thế cờ Heuristic đa tầng) phục vụ thi đấu, luyện tập thế cờ và đào tạo kỹ nghệ cờ tướng.

* **Backend**: ASP.NET Core 9.0 (C#), Entity Framework Core 9.0, SignalR Core, JWT Authentication.
* **Database**: MySQL 8.0+ (Tự động khởi tạo và áp dụng 18 Migrations khi khởi chạy).
* **Frontend**: Razor Pages / MVC, HTML5 Canvas, Tailwind CSS, Bootstrap Icons, Web Audio API.
* **AI Engine**: Thuật toán Minimax kết hợp cắt tỉa Alpha-Beta, Quiescence Search, Bảng chuyển vị (Transposition Table), Hàm lượng giá vị trí quân cờ (PST).

---

## 🚀 2. HƯỚNG DẪN CÀI ĐẶT & KHỞI CHẠY (QUICK START)

### 2.1. Yêu cầu môi trường
* **.NET SDK**: Phiên bản 9.0 trở lên.
* **Cơ sở dữ liệu**: MySQL 8.0+ (Khuyên dùng [Laragon](https://laragon.org/) hoặc XAMPP).
* **Node.js**: Phiên bản 18+ (Dùng để chạy các kịch bản kiểm thử tự động E2E).

### 2.2. Khởi tạo Cơ sở Dữ liệu
1. Khởi động dịch vụ MySQL (cổng mặc định `3306`).
2. Tạo cơ sở dữ liệu rỗng có tên: `chess_db`.
3. Kiểm tra cấu hình kết nối tại `ChessCore/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Port=3306;Database=chess_db;User=root;Password=;"
   }
   ```
   *(Nếu MySQL của bạn có mật khẩu `root`, hãy điền vào mục `Password=`)*.

### 2.3. Khởi chạy ứng dụng
Mở terminal (PowerShell / Command Prompt) tại thư mục gốc của dự án:
```powershell
# Bước 1: Khôi phục các thư viện NuGet
dotnet restore

# Bước 2: Khởi chạy Web Server
dotnet run --project ChessCore
```
> [!NOTE]
> Hệ thống được lập trình cơ chế **Auto-Migration**. Ngay khi ứng dụng khởi chạy, hệ thống sẽ tự động quét và áp dụng đầy đủ toàn bộ 18 Migration, tự động nạp bảng phân quyền và dữ liệu hạt giống (Seed Data) mà bạn không cần phải gõ lệnh migration thủ công.

Truy cập website trên trình duyệt: **`http://localhost:5000`**

---

## 📂 3. CẤU TRÚC THƯ MỤC DỰ ÁN

```text
C-T-ng-/
├── ChessCore/                             <-- Mã nguồn chính của ứng dụng Web ASP.NET Core
│   ├── Controllers/                       <-- Xử lý nghiệp vụ API & MVC
│   ├── Data/                              <-- AppDbContext, Seed Data, Kho bài tập cờ thế (Puzzles)
│   ├── Engine/                            <-- Bộ não luật cờ, MoveGenerator, AI Minimax
│   ├── Hubs/                              <-- SignalR Hub thi đấu thời gian thực
│   ├── Migrations/                        <-- Toàn bộ 18 bản thiết kế lược đồ CSDL
│   ├── Models/ & ViewModels/              <-- Mô hình thực thể & DTO
│   ├── Views/                             <-- Giao diện người dùng Razor MVC
│   └── wwwroot/                           <-- Tài nguyên tĩnh (Âm thanh, Bàn cờ HD, Quân cờ PNG)
│
├── docs/                                  <-- Toàn bộ tài liệu phân tích thiết kế
│   ├── CHƯƠNG_3_PHAN_TICH_VA_THIET_KE_HE_THONG_CHESSCORE.docx  <-- Thuyết minh đồ án chuẩn HUIT
│   ├── 01_SO_DO_USE_CASE_NGHIEP_VU/       <-- Sơ đồ UCD nghiệp vụ, Hoạt động, Tuần tự, Cộng tác
│   ├── 02_USE_CASE_HE_THONG/              <-- Sơ đồ Use Case toàn hệ thống
│   ├── 03_SO_DO_LOP_MUC_PHAN_TICH/        <-- Sơ đồ lớp phân tích (BCE)
│   └── 04_SO_DO_LOP_THIET_KE/             <-- Sơ đồ lớp thiết kế & Mô hình CSDL quan hệ (ERD)
│
├── Testing/                               <-- TOÀN BỘ HỆ THỐNG KIỂM THỬ (TEST SUITE & REPORT)
│   ├── 01_Ban1_UnitTests_xUnit/           <-- [CODE TEST] 12 Unit Test kiểm tra luật cờ & AI (C#)
│   ├── 02_Ban2_ApiTests_Postman/          <-- [CODE TEST] Bộ kịch bản kiểm thử API RESTful
│   ├── 03_Ban3_E2ETests_Scripts/          <-- [CODE TEST] Kịch bản tự động hóa E2E (Master Matrix)
│   └── 04_TaiLieu_Va_BaoCao_KiemThu/      <-- [KẾT QUẢ TEST] Bảng Excel 50 Test Cases, Word & Ảnh
│
├── Validation/                            <-- Script kiểm tra giao diện Responsive
├── ChessCore.sln                          <-- Solution Visual Studio
└── HUONG_DAN_CAI_DAT_VA_CHAY.txt          <-- Hướng dẫn nhanh dạng text
```

---

## 🧪 4. HỆ THỐNG KIỂM THỬ TOÀN DIỆN (TESTING SUITE)

> 📖 **Xem tài liệu phân tích chi tiết cơ chế hoạt động của 4 folder tại:** [HUONG_DAN_HOAT_DONG_4_FOLDER_TESTING.md](Testing/HUONG_DAN_HOAT_DONG_4_FOLDER_TESTING.md)

Hệ thống kiểm thử được thiết kế theo mô hình Kim tự tháp kiểm thử phần mềm (Testing Pyramid) và phân chia rõ ràng giữa **MÃ NGUỒN KIỂM THỬ** và **KẾT QUẢ ĐẦU RA**:

### 4.1. Mã nguồn thực thi kiểm thử (Folders 1, 2, 3)

#### 🔹 Folder 1: `Testing/01_Ban1_UnitTests_xUnit` (Kiểm thử đơn vị - Unit Test)
* **Ngôn ngữ**: C# / xUnit Framework.
* **Phạm vi**: Kiểm tra sâu thuật toán sinh nước đi, luật di chuyển hợp lệ của 7 loại quân cờ, luật cản chân mã, cản tượng, luật mặt tướng và thuật toán AI Minimax.
* **Lệnh chạy**:
  ```powershell
  dotnet test
  ```
  *(Kết quả: 12/12 Passed - 100%)*.

#### 🔹 Folder 2: `Testing/02_Ban2_ApiTests_Postman` (Kiểm thử giao tiếp - API Test)
* **Công cụ**: Postman / Newman.
* **Phạm vi**: 11 luồng API (Xác thực JWT Token, Tạo phòng đấu cờ, Lấy danh sách nước đi hợp lệ, Đánh cờ qua API, Nhiệm vụ ngày, Bảng xếp hạng).
* **Cách dùng**: Import `ChessCore_APIs.postman_collection.json` và `ChessCore_Local.postman_environment.json` vào Postman rồi nhấn **Run Collection**.

#### 🔹 Folder 3: `Testing/03_Ban3_E2ETests_Scripts` (Kiểm thử đầu-cuối - E2E Automation)
* **Ngôn ngữ**: Node.js Script.
* **Phạm vi**: Giả lập toàn bộ thao tác của người dùng từ Đăng ký -> Xác thực mã OTP -> Đăng nhập -> Tạo bàn thi đấu -> Ăn quân -> Giải cờ thế -> Làm bài thi chứng chỉ.
* **Lệnh chạy Master Test Matrix (50 Test Cases)**:
  *(Đảm bảo Kestrel Server đang chạy ở cổng 5000)*
  ```powershell
  node Testing/03_Ban3_E2ETests_Scripts/run_master_test_matrix.cjs
  ```
  *(Kết quả: 50/50 Passed - 100%)*.

---

### 4.2. Báo cáo & Bằng chứng nghiệm thu kết quả (Folder 4)

Toàn bộ kết quả đầu ra sau khi chạy các bộ test trên được tổng hợp tại:  
👉 **`Testing/04_TaiLieu_Va_BaoCao_KiemThu/`**

1. **`BANG_TONG_HOP_TEST_CASES_CHESSCORE.xlsx`**:
   * **Sheet 1 (Test_Cases_Matrix)**: Bảng chi tiết 50 Test Cases chuẩn quốc tế (Mã ca kiểm thử `RULE-01` đến `QUEST-05`, mục tiêu, dữ liệu đầu vào, kết quả mong đợi, kết quả thực tế, trạng thái Pass/Fail qua 2 chu kỳ kiểm thử).
   * **Sheet 2 (Defect_Bug_Log)**: Nhật ký ghi nhận 6 lỗi phát hiện trong Cycle 1 và lịch sử vá lỗi thành công ở Cycle 2.
2. **`KICH_BAN_KIEM_THU_END_TO_END_CHESSCORE.docx`**:
   * Báo cáo kịch bản kiểm thử hoàn chỉnh định dạng Word theo quy chuẩn đồ án tốt nghiệp khoa CNTT - HUIT, có nhúng đầy đủ các bảng dữ liệu và 19 hình ảnh minh chứng.
3. **`E2E_SCREENSHOTS/`**:
   * Thư mục chứa toàn bộ ảnh chụp màn hình độ nét cao làm bằng chứng nghiệm thu cho từng ca kiểm thử.

---

## 👥 5. PHÂN CÔNG VAI TRÒ NHÓM KIỂM THỬ

| Thành viên | Phụ trách chính | Sản phẩm bàn giao |
| :--- | :--- | :--- |
| **Bạn 1** | Kiểm thử Đơn vị & Thuật toán Cờ (Unit Test) | `01_Ban1_UnitTests_xUnit` (12 xUnit Tests cho Engine, PST & Minimax) |
| **Bạn 2** | Kiểm thử Dịch vụ Tích hợp & API (API Test) | `02_Ban2_ApiTests_Postman` (Postman Collection 11 luồng API REST) |
| **Bạn 3** | Kiểm thử Đầu-Cuối & Nghiệm thu (E2E & Report) | `03_Ban3_E2ETests_Scripts` & `04_TaiLieu_Va_BaoCao_KiemThu` (50 E2E Tests, Excel, Word) |

---

## 📄 6. THÔNG TIN BẢN QUYỀN
Đồ án Tốt nghiệp Đại học ngành Công nghệ Thông tin — Trường Đại học Công Thương TP.HCM (HUIT).  
Tất cả các tài liệu và mã nguồn được tổ chức hoàn chỉnh, sẵn sàng cho việc nghiệm thu trước Hội đồng chấm Đồ án.
