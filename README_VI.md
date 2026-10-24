# Kỳ Đạo Pixel 1.2 — chơi với máy không cần server

## Vì sao bản trước không đi được cờ?
Ảnh báo “Connection failure” khi tạo ván: app cũ gọi backend nhưng backend/IP/cổng chưa hoạt động. Đổi Phone sang Pixel không giải quyết lỗi mạng. Bản này bổ sung engine luyện tập chạy ngay trên thiết bị; nút “Chơi ngay” không gọi HTTP và không cần đăng nhập.

## Chạy ngay trên Pixel
1. Giải nén vào một thư mục mới, mở cả thư mục CoTuong_Pixel_Offline_V3 trong VS Code.
2. Trong Android Studio > Device Manager > Create Virtual Device, chọn Pixel 6/7/8 và một system image x86_64 phù hợp máy tính. Có thể dùng image API 33 đã có; không bắt buộc tải image mới. Tên AVD nên chứa Pixel.
3. Đóng emulator Phone_19 nếu không cần để tiết kiệm RAM.
4. Trong PowerShell tại thư mục source, chạy: `.\Run-Pixel.ps1`
5. Script tìm Pixel đã tạo, bật máy ảo, chờ boot và chạy bản mới đúng thiết bị. Nếu SDK ở chỗ khác: `.\Run-Pixel.ps1 -AvdName Pixel6_API33 -SdkPath "D:\Android\Sdk"` (thay tên/path theo máy).
6. App mới tên **Kỳ Đạo Pixel**, version 1.2, package **com.chesscore.cotuong.pixel**. Sảnh màu giấy ngà, có dòng “KỲ ĐẠO · PIXEL 1.2”. Nếu vẫn thấy màn hình tối “Trang Chủ”, bạn đang mở app cũ.
7. Chọn độ khó rồi bấm Chơi với máy/Chơi ngay; bạn cầm Đỏ, máy cầm Đen. Chạm quân rồi chạm giao điểm được đánh dấu.

Có thể tự bật Pixel rồi chạy: `dotnet build .\ChessCore.Mobile.csproj -f net9.0-android -c Debug -t:Run`. Khi mở nhiều emulator hãy dùng Run-Pixel.ps1 để chọn đúng Pixel.

Nếu PowerShell báo chặn script, có thể chạy bằng một tiến trình riêng cho script đã kiểm tra: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Run-Pixel.ps1`. Cách này không đổi chính sách máy lâu dài. Bạn có thể dùng lệnh dotnet ở trên nếu không muốn chạy script.

## Bản này có gì?
- Giữ giao diện sảnh giấy ngà, gỗ, vàng nâu và xanh ngọc; nút chính ưu tiên chơi máy offline, thanh Android bỏ màu tím mặc định.
- Bàn cờ co giãn, lật góc, animation ngắn, đánh dấu lượt bạn bằng viền xanh, hai cột ở màn hình rộng, có giảm chuyển động.
- Tái sử dụng Board/MoveGenerator/BoardEvaluator/Minimax từ source ChessCore_H4_GitHub người dùng gửi, trong namespace Offline độc lập với backend.
- AI chạy qua Task.Run, có cancellation và ngân sách thời gian 250/650/1200/2200 ms theo độ khó. Dùng iterative deepening, giữ kết quả của độ sâu đã hoàn thành; không tuyên bố mọi lượt đều đạt depth 4. Thời gian thực tế có thể vượt ngân sách một phần do kiểm tra luật/evaluation.
- Quy tắc đi quân, tự chiếu, chiếu hết và hết nước hợp lệ; H1/H2/H3/H4 lấy từ engine. Ván offline không tăng/giảm Elo.
- Luật lặp offline là lặp cùng FEN ba lần thì hòa cho luyện tập; chưa phân xử đầy đủ chiếu dai/đuổi dai theo luật thi đấu.
- Lưu tối đa tám ván trong RAM. Đóng hẳn tiến trình sẽ mất ván luyện tập; chưa có lưu offline bền vững.
- Online, đăng nhập và hồ sơ server vẫn cần backend đúng BACKEND_CONTRACT.md. Kho cờ thế/khóa học mở bản web, không phải màn hình native offline.

## Build và kiểm tra
Giữ target net9.0-android để phù hợp SDK 9.0.305/workload 9 của người dùng. MAUI 9 đã hết hỗ trợ; nên nâng đồng bộ SDK/workload/project trước phát hành. Android SDK/OpenJDK vẫn cần được cấu hình trên máy.

- `dotnet run --project Checks/ChessCore.Mobile.Checks.csproj`
- `dotnet run --project Checks/ChessCore.Mobile.ViewModelChecks.csproj`
- `dotnet run --project Checks/ChessCore.Mobile.OfflineChecks.csproj`
- `python verify_source.py`

Build APK điện thoại ARM64 bằng Build-Android.ps1 -Architecture arm64. ZIP là source, không có APK mới đã kiểm chứng trên Android. Xem VALIDATION.md để biết giới hạn kiểm thử.
