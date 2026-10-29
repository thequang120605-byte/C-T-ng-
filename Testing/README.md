# HỆ THỐNG KIỂM THỬ TOÀN DIỆN (CHESSCORE TESTING SUITE)
## ĐỒ ÁN TỐT NGHIỆP: WEBSITE CỜ TƯỚNG TRỰC TUYẾN TÍCH HỢP TRÍ TUỆ NHÂN TẠO
### BỘ DỮ LIỆU ĐỒNG BỘ 137 TEST CASES & 17 LỖI THỰC TẾ (DEFECT LOG)

Thư mục này gom toàn bộ mã nguồn, kịch bản, dữ liệu và tài liệu báo cáo kiểm thử của cả nhóm 3 người vào một nơi duy nhất theo đúng chuẩn Công nghệ Phần mềm.

---

## 🧭 CẤU TRÚC THƯ MỤC KIỂM THỬ (TESTING SUITE)

> 📖 **Xem giải thích chi tiết cơ chế hoạt động của 4 folder tại:** [HUONG_DAN_HOAT_DONG_4_FOLDER_TESTING.md](HUONG_DAN_HOAT_DONG_4_FOLDER_TESTING.md)  
> 🔍 **Xem cẩm nang kiểm thử & tái hiện 17 lỗi tại:** [04_TaiLieu_Va_BaoCao_KiemThu/HUONG_DAN_KIEM_THU_TOAN_DIEN_137_TC_VA_17_BUGS.md](04_TaiLieu_Va_BaoCao_KiemThu/HUONG_DAN_KIEM_THU_TOAN_DIEN_137_TC_VA_17_BUGS.md)

```
Testing/
├── 01_Ban1_UnitTests_xUnit/              <-- BẠN 1: KIỂM THỬ ENGINE, LUẬT CỜ & AI (C#)
│   ├── ChessRulesAndAiTests.cs          (12 test cases kiểm tra MoveGenerator, FEN, Minimax, H1..H4)
│   └── ChessCore.Tests.csproj           (Project xUnit tích hợp Coverlet Code Coverage)
│
├── 02_Ban2_ApiTests_Postman/             <-- BẠN 2: KIỂM THỬ TỰ ĐỘNG HÓA API REST
│   ├── ChessCore_APIs.postman_collection.json (Collection API: Auth JWT, Game, Quests, Leaderboard)
│   └── ChessCore_Local.postman_environment.json (Biến môi trường: baseUrl, jwt_token, game_id)
│
├── 03_Ban3_E2ETests_Scripts/             <-- BẠN 3: KIỂM THỬ ĐẦU-CUỐI TOÀN TRÌNH (E2E)
│   └── run_suite_137.py                 (Công cụ chạy tự động toàn bộ 137 Test Cases, tự sync origin/main & ghi Excel)
│
├── 04_TaiLieu_Va_BaoCao_KiemThu/         <-- TÀI LIỆU VÀ MINH CHỨNG NGHIỆM THU
│   ├── HUONG_DAN_KIEM_THU_TOAN_DIEN_137_TC_VA_17_BUGS.md (Cẩm nang kiểm thử & tái hiện 17 bugs chi tiết)
│   └── E2E_SCREENSHOTS/                 (36 ảnh chụp màn hình minh chứng: 19 ca Pass + 17 ca Defect)
│
├── TestCases_KLCN_CoTuong (1).xlsx       <-- FILE EXCEL GỐC ĐỒNG BỘ 137 TEST CASES & 12_DEFECT_LOG
└── README.md                             (File hướng dẫn tổng quan này)
```

---

## 🚀 HƯỚNG DẪN THỰC THI KIỂM THỬ THEO VAI TRÒ

### 1. Dành cho BẠN 1 (Chạy Unit Test xUnit & Đo Code Coverage C#)
Mở PowerShell tại thư mục gốc của dự án và chạy:
```powershell
# Chạy 12 Unit Tests kiểm tra thuật toán
dotnet test Testing/01_Ban1_UnitTests_xUnit/ChessCore.Tests.csproj

# Đo độ bao phủ mã nguồn (Code Coverage) qua Coverlet
dotnet test Testing/01_Ban1_UnitTests_xUnit/ChessCore.Tests.csproj --collect:"XPlat Code Coverage"
```

### 2. Dành cho BẠN 2 (Chạy kiểm thử tự động API Postman & Newman)
Yêu cầu bật Web Server trước: `dotnet run --project ChessCore` (mở tại `http://localhost:5000`).
```powershell
# Chạy tự động toàn bộ collection API và kiểm tra Assertions
npx newman run Testing/02_Ban2_ApiTests_Postman/ChessCore_APIs.postman_collection.json -e Testing/02_Ban2_ApiTests_Postman/ChessCore_Local.postman_environment.json
```

### 3. Dành cho BẠN 3 (Chạy Bộ Master Automation Matrix 137 Test Cases)
```powershell
# Tự động fetch code mới nhất từ GitHub origin/main, kiểm thử và ghi kết quả vào Excel
python Testing/03_Ban3_E2ETests_Scripts/run_suite_137.py
```

---

## 📊 KẾT QUẢ NGHIỆM THU TỔNG QUAN

* **Tổng số ca kiểm thử**: 137 Test Cases
* **Đạt (Pass)**: **120 Test Cases (87.6%)**
* **Lỗi phát hiện (Fail)**: **17 Test Cases (12.4%)**
* **Defect Log Chu kỳ 1**: Ghi nhận chi tiết từ `BUG_001` đến `BUG_017` tại Sheet `12_Defect_Log`.
* **Kết quả Tái kiểm thử Chu kỳ 2 (Retest Round 2)**: Ghi nhận tại Sheet `13_Retest_Round2` sau khi tích hợp commit `65ae81d` (28/10/2026 22:24:20). Toàn bộ **17/17 lỗi (100.0%) đã được khắc phục triệt để và nghiệm thu đóng lỗi (Pass)**.
* **File Excel báo cáo**: [TestCases_KLCN_CoTuong (1).xlsx](TestCases_KLCN_CoTuong%20(1).xlsx) (13 Sheet hoàn chỉnh).
