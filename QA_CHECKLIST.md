# Checklist nghiệm thu trên thiết bị

Chạy với backend thật, hai tài khoản khác nhau và APK build từ source trong ZIP này.

- [ ] ARM64: cài APK ARM64, cold start, nhập server, đăng nhập, tạo ván PvE.
- [ ] x86_64: emulator truy cập backend qua 10.0.2.2 đúng cổng.
- [ ] Xác nhận app 1.1/versionCode 2, không cài nhầm APK 1.0 từ bản cũ.
- [ ] Hai máy online: Đỏ và Đen nhận đúng phe, Đen nhìn từ phía mình, thắng/thua đúng.
- [ ] Ván WaitingForPlayers và ván kết thúc không nhận thao tác đi quân.
- [ ] Chạm liên tục khi request chậm không gửi lặp nước đi hoặc tạo phòng.
- [ ] Response join trực tiếp và response bọc game đều hoạt động, không gửi POST thứ hai vì lỗi parse.
- [ ] Tắt Wi-Fi 20 giây, bật lại, rejoin + snapshot khớp server; thử hết số lần reconnect rồi Đồng bộ thủ công.
- [ ] Timeout AI: tải lại server trước, chỉ thử tiếp nếu vẫn lượt Đen; không lặp POST tự động sau timeout.
- [ ] Back Android, chuyển tab, khóa màn hình 1 phút rồi mở: không tăng subscriber, không giữ ván cũ.
- [ ] Đổi server A sang B trong khi request đang chạy: không gửi token A sang B, không nhận kết quả A vào UI B.
- [ ] Hai request cùng 401: chỉ refresh token cần thiết; logout trong lúc refresh không khôi phục phiên đã thoát.
- [ ] Portrait 360×640, 390×844, tablet 800×1280, landscape 844×390 (đơn vị logic): quân biên đầy đủ, chữ không đè bàn.
- [ ] Font hệ thống 150–200%, TalkBack, navigation gestures, bàn phím: các nút truy cập được.
- [ ] Đổi góc và bật Giảm chuyển động: không animation sai tọa độ, không render animation sau khi rời trang.
- [ ] Lặp 50 lần tạo/rời ván, dùng profiler xem GameViewModel, GamePage và HubConnection; đo sau GC, không chỉ nhìn peak RAM.
- [ ] Dùng profiler Android đo cold start, frame time, CPU/battery; chưa có số đo được công bố từ môi trường chỉnh sửa.
- [ ] Release HTTPS không chấp nhận HTTP, bản LocalHttp chỉ dùng trong LAN thử nghiệm.
- [ ] API 21 trên ABI phù hợp, API 33/35 và môi trường page-size 16 KiB; không suy ra tương thích chỉ từ targetSDK.

Mục chưa đánh dấu là việc cần kiểm thử thực tế, không phải kết quả đã đạt.

## Bổ sung cho Pixel/offline
- [ ] App tên Kỳ Đạo Pixel 1.2, đúng package mới, không mở icon app cũ.
- [ ] Bật airplane mode vẫn tạo ván, đi quân Đỏ và nhận nước máy.
- [ ] Khóa màn hình khi AI đang nghĩ: search hủy, trở lại có thể bấm tiếp tục lượt máy nếu vẫn lượt Đen.
- [ ] Cấp 1–4 hiển thị và phản hồi phù hợp, không khóa UI.
- [ ] Chạm Ván mới tạo session offline mới; không phát HTTP/SignalR.
- [ ] Chơi online vẫn yêu cầu server, không giả lập đối thủ online.
