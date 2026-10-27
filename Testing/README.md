# HỆ THỐNG KIỂM THỬ TOÀN DIỆN (CHESSCORE TESTING SUITE)
## ĐỒ ÁN TỐT NGHIỆP: WEBSITE CỜ TƯỚNG TRỰC TUYẾN TÍCH HỢP TRÍ TUỆ NHÂN TẠO

Thư mục này gom toàn bộ mã nguồn, kịch bản, dữ liệu và tài liệu báo cáo kiểm thử của cả nhóm 3 người vào một nơi duy nhất theo đúng chuẩn Công nghệ Phần mềm.

---

## CẤU TRÚC THƯ MỤC KIỂM THỬ

> 📖 **Xem giải thích chi tiết cơ chế hoạt động của 4 folder tại:** [HUONG_DAN_HOAT_DONG_4_FOLDER_TESTING.md](HUONG_DAN_HOAT_DONG_4_FOLDER_TESTING.md)

```
Testing/
├── 01_Ban1_UnitTests_xUnit/              <-- BẠN 1: KIỂM THỬ ENGINE, LUẬT CỜ & AI (C#)
│   ├── ChessRulesAndAiTests.cs          (12 test cases kiểm tra MoveGenerator, FEN, Minimax, H1..H4)
│   └── ChessCore.Tests.csproj           (Project xUnit tích hợp trong ChessCore.sln)
│
├── 02_Ban2_ApiTests_Postman/             <-- BẠN 2: KIỂM THỬ TỰ ĐỘNG HÓA API REST
│   ├── ChessCore_APIs.postman_collection.json (11 luồng API: Auth JWT, Game, Quests, Leaderboard)
│   └── ChessCore_Local.postman_environment.json (Biến môi trường: baseUrl, jwt_token, game_id)
│
├── 03_Ban3_E2ETests_Scripts/             <-- BẠN 3: KIỂM THỬ ĐẦU-CUỐI TOÀN TRÌNH (E2E)
│   ├── run_master_test_matrix.cjs       (Chạy tự động toàn bộ 50 Test Cases khớp 1-1 với file Excel)
│   ├── run_e2e_full_test.cjs            (Chạy 9 kịch bản E2E toàn diện trên web thật)
│   ├── test_e2e_full_system.cjs         (Kịch bản 20 ca kiểm thử UI & SignalR)
│   ├── test_chess_rules.cjs             (Kịch bản kiểm tra nước đi cờ tướng)
│   ├── test_endpoints.cjs               (Kịch bản kiểm tra mã phản hồi HTTP 200/400/401)
│   ├── test_e2e_features.cjs            (Kịch bản kiểm thử tính năng cờ thế, khóa học)
│   └── load_test.cjs                    (Kịch bản đo tải & độ trễ: 25 người dùng đồng thời)
│
├── 04_TaiLieu_Va_BaoCao_KiemThu/         <-- TÀI LIỆU VÀ MINH CHỨNG NGHIỆM THU
│   ├── BANG_TONG_HOP_TEST_CASES_CHESSCORE.xlsx  (File Excel 2 Sheet: 50 Test Cases + Defect Bug Log)
│   ├── KICH_BAN_KIEM_THU_END_TO_END_CHESSCORE.docx (Bản Word chuẩn HUIT có 19 ảnh chụp màn hình)
│   ├── KICH_BAN_KIEM_THU_END_TO_END_CHESSCORE.md   (Đặc tả kịch bản định dạng Markdown)
│   └── E2E_SCREENSHOTS/                         (Thư mục chứa 19 ảnh chụp màn hình độ phân giải cao)
│
└── README.md                             (File hướng dẫn này)
```

---

## HƯỚNG DẪN THỰC THI KIỂM THỬ

### 1. Dành cho BẠN 1 (Chạy Unit Test xUnit C#)
Mở PowerShell tại thư mục gốc của dự án và chạy:
```powershell
dotnet test
```
*Hoặc chỉ định rõ project:*
```powershell
dotnet test Testing\01_Ban1_UnitTests_xUnit\ChessCore.Tests.csproj
```
* **Kết quả mong đợi:** `Passed! - Failed: 0, Passed: 12, Total: 12 (100% Pass)`.

---

### 2. Dành cho BẠN 2 (Chạy API Postman)
1. Mở ứng dụng **Postman** (hoặc **Katalon Studio**).
2. Bấm nút **Import** ở góc trên bên trái.
3. Chọn 2 file trong thư mục `Testing/02_Ban2_ApiTests_Postman/`:
   * `ChessCore_APIs.postman_collection.json`
   * `ChessCore_Local.postman_environment.json`
4. Chọn môi trường `ChessCore Local Environment`.
5. Bấm vào thư mục Collection $\rightarrow$ bấm **Run Collection** $\rightarrow$ bấm **Run ChessCore - E2E API Test Suite**.
* **Kết quả mong đợi:** 11 request đều trả về mã HTTP 200 OK, tự động trích xuất JWT Token và Game ID.

---

### 3. Dành cho BẠN 3 (Chạy E2E Web & Xuất Báo cáo)
1. Đảm bảo Web Server đang chạy tại `http://localhost:5000` (`dotnet run --project ChessCore`).
2. Chạy kịch bản Master Matrix (50 Test Cases khớp 1-1 với file Excel):
```powershell
node Testing/03_Ban3_E2ETests_Scripts/run_master_test_matrix.cjs
```
* **Kết quả mong đợi:** `50 / 50 PASSED (100%)`.
3. Hoặc chạy bộ kiểm thử luồng tổng quan 9 kịch bản:
```powershell
node Testing/03_Ban3_E2ETests_Scripts/run_e2e_full_test.cjs
```
4. Mở file [BANG_TONG_HOP_TEST_CASES_CHESSCORE.xlsx](04_TaiLieu_Va_BaoCao_KiemThu/BANG_TONG_HOP_TEST_CASES_CHESSCORE.xlsx) để nộp bảng ma trận 50 ca kiểm thử và 6 lỗi đã khắc phục.
5. Mở file [KICH_BAN_KIEM_THU_END_TO_END_CHESSCORE.docx](04_TaiLieu_Va_BaoCao_KiemThu/KICH_BAN_KIEM_THU_END_TO_END_CHESSCORE.docx) để in báo cáo nộp hội đồng.
