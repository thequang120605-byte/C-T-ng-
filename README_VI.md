# 🏮 HƯỚNG DẪN DỰ ÁN CỜ TƯỚNG PIXEL OFFLINE V3 (DEMO READY)

Dự án ứng dụng di động **Cờ Tướng Pixel** được phát triển trên nền tảng **.NET MAUI 9 (C#)**, áp dụng mô hình kiến trúc **MVVM** cùng bộ vẽ bàn cờ đồ họa hiệu năng cao **`Microsoft.Maui.Graphics` (`ChessBoardDrawable`)**. Ứng dụng tích hợp động cơ AI cờ tướng ngoại tuyến 4 cấp độ, chế độ hai người cùng máy, 20 thế cờ tàn cuộc kinh điển, hệ thống học cờ lý thuyết & trắc nghiệm tương tác, cùng tính năng lưu trữ kỳ phổ xem lại ván cờ trên thiết bị.

---

## 🚀 1. HƯỚNG DẪN CHẠY DỰ ÁN TRÊN TERMINAL

### A. Môi trường yêu cầu
- **.NET SDK 9.0** (`net9.0-android`)
- **Android SDK & Platform-Tools** (có `adb.exe` và emulator `emulator.exe`)
- **Android Emulator**: Đã tạo AVD `Pixel_8` (API 34/35) hoặc kết nối thiết bị Android thật qua USB Debugging.

### B. Câu lệnh chạy trực tiếp (Khắc phục lỗi ExecutionPolicy)
Mở PowerShell tại thư mục gốc dự án (`d:\Desktop\CoTuong_Pixel_Offline_V3`) và thực hiện:

```powershell
# 1. Chạy tự động khởi động Emulator Pixel và build/deploy app:
powershell -ExecutionPolicy Bypass -File .\Run-Pixel.ps1

# 2. Hoặc nếu đã bật sẵn Emulator / cắm máy Android thật:
dotnet build ChessCore.Mobile.csproj -f net9.0-android -t:Run

# 3. Để chạy bộ kiểm thử tự động (All Suites PASS 100%):
dotnet run --project Checks/ChessCore.Mobile.Checks.csproj
dotnet run --project Checks/ChessCore.Mobile.OfflineChecks.csproj
dotnet run --project Checks/ChessCore.Mobile.ViewModelChecks.csproj
```

### C. Cài đặt trực tiếp file APK đã đóng gói
File APK hoàn chỉnh đã được build sẵn tại thư mục gốc:
👉 `CoTuong_Pixel_Offline_V3.apk`

Để cài đặt vào thiết bị qua adb:
```powershell
adb install -r CoTuong_Pixel_Offline_V3.apk
```

---

## 🎨 2. CHI TIẾT CẢI TIẾN GIAO DIỆN & TÍNH NĂNG (THEO 6 TIÊU CHÍ)

### 1. Thiết kế lại Sảnh chính (Home Page)
- **Thu gọn hồ sơ**: Thanh profile phía trên nhỏ gọn hiển thị Avatar chữ thư pháp, Tên kỳ thủ, Trạng thái Offline/Elo và nút Tài khoản.
- **Loại bỏ dữ liệu giả**: Đã xóa toàn bộ các nút trang trí không hoạt động (vàng ảo, ngọc ảo, hộp quà "sắp ra mắt").
- **4 chế độ chính nổi bật (Grid 2x2)**:
  1. 🤖 **Chơi với máy**: Nhấn vào sẽ hiển thị hộp thoại Modal chọn 4 cấp độ AI.
  2. 🌐 **Chơi online**: Kết nối sảnh phòng trực tuyến qua REST API & SignalR.
  3. 🧩 **Cờ thế**: Phá giải 20 thế cờ tàn cuộc kinh điển native.
  4. 📖 **Học cờ**: Khóa học luật cờ, sát pháp và trắc nghiệm kiến thức native.
- **Chế độ Hai người cùng máy (Pass & Play)**: Thẻ điều hướng riêng biệt, cho phép 2 người luân phiên đi quân ngay trên cùng 1 điện thoại mà không cần mạng.
- **Điều hướng phụ bên dưới**: Thanh menu nhanh truy cập **Kỳ phổ**, **Thống kê** và **Cài đặt**.

### 2. Màn hình Bàn cờ (Game Page)
- **Bàn cờ chiếm trọn không gian**: GraphicsView co giãn linh hoạt chiếm diện tích lớn nhất màn hình.
- **Thẻ đối thủ & người chơi nhỏ gọn (Slim Cards)**: Chiều cao chỉ ~40 DIP, có huy hiệu đổi màu xanh ngọc bích khi đến lượt bạn đi.
- **Đánh giá thế cờ Heuristic (H1–H4)**:
  - Mặc định thanh trạng thái chỉ hiển thị tổng điểm ưu thế (Ví dụ: `Điểm: +150`).
  - Nút **[Đánh giá]** mở hộp thoại popup giải thích rõ ràng 4 chỉ số:
    - **H1 (Quân lực)**: Giá trị vật chất các quân cờ (Tướng, Xe, Pháo, Mã, Tượng, Sĩ, Tốt).
    - **H2 (Vị trí)**: Bảng điểm vị trí kiểm soát trung lộ, qua sông, điểm hiểm.
    - **H3 (Độ cơ động)**: Số lượng nước đi hợp lệ phản ánh quyền chủ động.
    - **H4 (An toàn & Sát chiêu)**: An toàn cung Tướng và liên kết bảo vệ.
- **Chế độ Xem lại ván cờ (Replay Mode)**:
  - Thanh điều hướng chuyên dụng: `[⏮ Đầu]` `[◀ Trước]` `[Nước x/y]` `[Sau ▶]` `[Cuối ⏭]` và nút `[✕ Đóng xem lại]`.
  - Tự động tái hiện từng trạng thái bàn cờ qua từng nước đi.
- **Hộp thoại Kết thúc ván cờ**: Hiển thị rõ nguyên nhân (Chiếu hết, Hết nước đi, Xin thua, Lặp lại 3 lần) và cung cấp đầy đủ 3 nút hành động:
  1. `Chơi ván tiếp theo`
  2. `Xem lại ván cờ`
  3. `Trở về Sảnh`

### 3. Phân hệ Cờ thế (Native Puzzles)
- Gồm **20 bài thế cờ tàn cuộc kinh điển** (Mã Hậu Pháo, Thiết Môn Thuyên, Đơn Xa Sát Cục, Trọng Pháo, Song Xa Phá Thành, v.v.).
- Hiển thị tiến độ: `Đã giải: x / 20 bài thế`.
- Mục tiêu bài thế rõ ràng (Ví dụ: "Đỏ đi trước: Dùng Pháo mượn ngòi Mã chiếu bí").
- Nút bấm đầy đủ: `[◀ Bài trước]`, `[🔄 Làm lại]`, `[💡 Gợi ý]`, `[Bài tiếp ▶]`.
- Tự động nhận diện chiếu bí và lưu tiến độ vào bộ nhớ thiết bị.

### 4. Phân hệ Học cờ (Native Courses)
- Chia làm **3 Module bài bản**:
  - *Module 1*: Nhập môn cờ tướng, bàn cờ, sông và quy tắc di chuyển từng quân (Tướng, Sĩ, Tượng cản mắt, Xe, Pháo ngòi, Mã cản chân, Tốt qua sông).
  - *Module 2*: Các thế sát pháp căn bản (Mã Hậu Pháo, Thiết Môn Thuyên, Trọng Pháo, Song Xa).
  - *Module 3*: Khai cuộc và chiến thuật trung cuộc (Trung Pháo đối Bình Phong Mã, Thuận Pháo, Nghịch Pháo, Xuất Xe nhanh).
- **Trắc nghiệm tương tác**:
  - Khi chạm vào một đáp án, khung đáp án lập tức đổi màu vàng kim nổi bật (`#D4AF37`).
  - Sau khi bấm **[Nộp bài]**:
    - Nếu đúng: Đổi sang màu xanh ngọc lục bảo (`#2D6A4F`) và hiện lời khen.
    - Nếu sai: Đổi sang màu đỏ gạch (`#A34032`), đồng thời làm nổi bật đáp án đúng.
    - Hiển thị khung giải thích lý do cặn kẽ.

### 5. Kỳ phổ & Lưu trữ ván cờ (Native History)
- Toàn bộ ván cờ luyện tập được tự động lưu trữ xuống đĩa qua file `offline_saved_sessions.json`.
- Danh sách ván cờ phân định rõ ràng 2 nút hành động:
  - **[Tiếp tục ván]**: Vào lại ván cờ để tiếp tục đánh từ vị trí dở dang.
  - **[Xem lại ván]**: Mở bàn cờ ở chế độ Replay để tua từng nước đi.

### 6. Hiệu năng & Quy chuẩn UI/UX
- **Đồ họa mượt mà 60 FPS**: Bộ render `ChessBoardDrawable` sử dụng bảng màu tĩnh cached static, zero-allocation trong vòng lặp vẽ, hỗ trợ cả Tap và Kéo-Thả (Drag & Drop) quân cờ.
- **Phong cách cổ điển nhã nhặn**: Tông màu nền giấy ngà (`#F3EBDD`), viền gỗ mun (`#282B29`), nút vàng đồng (`#C6A052`), điểm nhấn xanh ngọc (`#2D6A4F`).
- **Touch Targets chuẩn Accessibility**: Toàn bộ nút bấm chính và ô chọn đều có chiều cao $\ge 48\text{ DIP}$.

---

## 🧪 3. KẾT QUẢ KIỂM THỬ (TEST SUITES)

| Suite | Số lượng bài test | Kết quả | Ghi chú |
|---|:---:|:---:|---|
| **ChessCore.Mobile.Checks** | 13/13 | **PASS (100%)** | Xác thực logic phòng, lượt đi, seat resolution |
| **ChessCore.Mobile.OfflineChecks** | 18/18 | **PASS (100%)** | AI Minimax, Alpha-Beta, MoveGenerator, Eviction |
| **ChessCore.Mobile.ViewModelChecks** | 18/18 | **PASS (100%)** | MVVM lifecycle, SignalR sync, Replay, Re-entrancy |
| **Checks.ExportSuite (Ground Truth)** | 20/20 Puzzles | **PASS (100%)** | Xác thực 20 thế cờ tàn cuộc chuẩn luật FEN |
| **Android Build net9.0-android** | 0 Error, 0 Warning | **BUILD SUCCESS** | Đã tạo APK hoàn chỉnh `CoTuong_Pixel_Offline_V3.apk` |

---
*Dự án hoàn thiện bởi Senior .NET MAUI / C# Developer & Mobile UI/UX Specialist.*
