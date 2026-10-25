# ChessCore Flutter (Kỳ Đạo Hoàng Gia 2.0 - Mobile & Web)

Ứng dụng di động & đa nền tảng của **Cờ Tướng ChessCore**, được thiết kế theo phong cách **Cổ Phong Hoàng Gia (Royal Classical Chinese Chess)**, tích hợp bộ máy cờ offline AI thuần Dart và đồng bộ 100% các tính năng thời gian thực từ nền tảng Web qua SignalR.

---

## 🌟 Điểm nổi bật & Tính năng toàn diện

### 1. Giao diện Cổ Phong Hoàng Gia (Classical Royal Design)
- **Sảnh chính Hoàng Gia:** Khung cảnh hoàng cung, cổng ngự uy nghi, các card chế độ chơi dạng 3D khối nổi với hiệu ứng ánh sáng kim sắc:
  - ⚔️ **Chơi Với Máy (PvE):** 4 cấp độ (Tập sự, Kỳ thủ, Kiện tướng, Đại sư).
  - 🌐 **Đối Kháng Online (PvP):** Đấu xếp hạng, phòng ngẫu nhiên và phòng riêng có mật khẩu.
  - 🗺️ **Bản Đồ Vượt Ải Cờ Thế:** Hệ thống bản đồ 10 ải quanh co, sương mù mở khóa từng ải, xếp hạng 3 sao hoàng gia.
  - 👑 **Bảng Xếp Hạng Hoàng Gia:** Thiết kế cuộn thư phong bì kim chỉ, vinh danh Top 1 - 3 với huy chương vàng, bạc, đồng, điểm Elo và tỷ lệ thắng.
- **Bàn cờ gỗ 3D cao cấp:**
  - Vân gỗ tế mộc cổ điển, đường chỉ vàng đồng, cửu cung sắc nét, dòng sông Sở Hà - Hán Giới thơ mộng.
  - Quân cờ chạm khắc chữ Hán phồn thể (帥/將, 仕/士, 相/象, 傌/馬, 俥/車, 炮/砲, 兵/卒) với đổ bóng chân thực.
  - Hỗ trợ cả **Chạm (Tap to Move)** và **Kéo thả (Drag & Drop)** mượt mà kèm hiệu ứng chuyển động và rung phản hồi (Haptics).
- **Thanh thế trận trực tiếp (Advantage Bar):**
  - Hiển thị cán cân điểm ưu thế (Đỏ vs Đen) chuyển động mượt mà theo từng nước đi của ván đấu.
- **Vòng tròn đếm ngược hoàng gia (Circular Turn Timer):**
  - Vòng kim sắc bao quanh avatar người chơi, đếm ngược trực quan từng giây lượt đánh với cảnh báo đổi màu khi sắp hết thời gian.
- **Màn hình Phân tích Ván đấu (Post-Game Review):**
  - Biểu đồ biến thiên thế trận toàn ván cờ.
  - Tự động phát hiện và thống kê các nước đi: Nước tối ưu, Tốt, Nghi vấn, Sai lầm, Bỏ lỡ.
  - Cho phép tua lại từng nước đi trên bàn cờ thu nhỏ để học hỏi và rút kinh nghiệm.

---

### 2. Bộ máy Cờ Offline thuần Dart (Pure Dart Chess Engine)
- **Thuật toán tìm kiếm sâu:** Minimax kết hợp cắt tỉa **Alpha-Beta Pruning**, **Iterative Deepening**, **Quiescence Search**, **Killer Moves**, **History Heuristics**, và bảng băm trạng thái **Zobrist Hashing (64-bit)**.
- **Chạy nền đa luồng (Background Isolate):** Tính toán nước đi ở luồng nền độc lập, đảm bảo UI duy trì ổn định 60-120fps không bao giờ bị giật lag.
- **Hệ thống đánh giá thế cờ H1 - H4:**
  - **H1:** Điểm vật chất cơ bản theo từng loại quân.
  - **H2:** Tính linh hoạt, độ kiểm soát lộ và vị trí chiến lược (PST - Piece-Square Tables).
  - **H3:** An toàn tướng lĩnh và hệ thống bảo vệ pháo mã.
  - **H4:** Thế trận tấn công cánh (Wing Attack - Tam tử/Tứ tử đồng biên).

---

### 3. Đồng bộ 100% Tính năng Web qua SignalR
- 💬 **Chat trực tiếp thời gian thực (In-Game Live Chat):** Giao diện tin nhắn hiện đại dạng bubble, tự động cuộn, hỗ trợ chat với đối thủ khi đang thi đấu.
- 🤝 **Xin hòa & Phản hồi (Draw Offer / Response):** Gửi lời xin hòa lên máy chủ, nhận thông báo đề nghị hòa từ đối phương với nút Chấp nhận / Từ chối.
- 🔄 **Đấu lại tức thì (Rematch):** Yêu cầu tái đấu ngay sau khi ván đấu kết thúc.
- ⏱️ **Xử thắng rụng kim (Claim Timeout):** Phát hiện và gửi yêu cầu xử thắng khi đối thủ cạn kiệt thời gian suy nghĩ.
- ↩️ **Xin đi lại (Undo / Takeback):** Hỗ trợ ván đấu offline và các chế độ luyện tập.
- 👁️ **Khán giả xem trực tiếp (Live Spectator):** Cho phép người xem gia nhập bàn đấu đang diễn ra để theo dõi trực tiếp các nước đi.
- 🏛️ **Tự động đồng bộ sảnh phòng (Live Lobby Sync):** Nhận tín hiệu tạo phòng, khóa phòng riêng, hủy phòng theo thời gian thực từ máy chủ.

---

## 🚀 Hướng dẫn chạy & Cài đặt

### 1. Chạy trên trình duyệt Chrome / Web
```bash
flutter run -d chrome --web-port 8088
```

### 2. Chạy trên thiết bị Android
```bash
flutter run -d android
```

### 3. Chạy trên máy tính Windows
```bash
flutter run -d windows
```

### 4. Build file Android APK
- **Bản thử nghiệm (Debug APK):**
  ```bash
  flutter build apk --debug
  ```
- **Bản phát hành tối ưu (Release APK):**
  ```bash
  flutter build apk --release
  ```
  File APK sẵn sàng cài đặt tại: `build/app/outputs/flutter-apk/app-release.apk` (dung lượng ~18 MB).

---

## 📁 Cấu trúc thư mục mã nguồn

```
lib/
├── main.dart                      # Điểm khởi chạy ứng dụng (Entrypoint)
├── models/
│   └── game_models.dart           # Models dữ liệu, FEN snapshot, Chat message, Room DTOs
├── offline/                       # Bộ máy AI và luật cờ tướng thuần Dart
│   ├── piece.dart                 # Định nghĩa quân cờ, vị trí, màu quân
│   ├── board.dart                 # Quản lý bàn cờ 10x9, FEN parser/generator
│   ├── move_generator.dart        # Sinh nước đi hợp lệ theo luật cờ tướng
│   ├── game_rules.dart            # Quy tắc chiếu bí, hòa cờ, rụng kim
│   ├── board_evaluator.dart       # Lượng giá thế trận theo mô hình H1-H4
│   ├── zobrist_hashing.dart       # Mã băm Zobrist 64-bit tối ưu bộ nhớ đệm
│   └── minimax_engine.dart        # Thuật toán tìm kiếm Minimax Alpha-Beta
├── screens/                       # Màn hình ứng dụng
│   ├── main_shell.dart            # Khung điều hướng đáy (Sảnh, Cờ thế, Xếp hạng, Cá nhân)
│   ├── home_screen.dart           # Sảnh chính hoàng gia & modal chọn thể thức đấu
│   ├── game_screen.dart           # Bàn đấu đầy đủ tính năng: Chat, Xin hòa, Đấu lại, Xử rụng kim
│   ├── game_review_screen.dart    # Xem lại ván đấu, phân tích sai lầm & biểu đồ ưu thế
│   ├── puzzle_map_screen.dart     # Bản đồ vượt ải 10 cửa cờ thế hoàng gia
│   ├── leaderboard_screen.dart    # Bảng xếp hạng cuộn thư hoàng gia Top 1-3
│   ├── online_lobby_screen.dart   # Sảnh chọn bàn 4 cấp cược & phòng riêng
│   ├── profile_screen.dart        # Thông tin kỳ thủ, thống kê ván đấu, huy hiệu
│   ├── login_screen.dart          # Màn hình đăng nhập tài khoản
│   ├── register_screen.dart       # Màn hình đăng ký tài khoản
│   └── settings_screen.dart       # Cấu hình máy chủ, địa chỉ IP, âm thanh, hiệu ứng
├── services/                      # Tầng giao tiếp mạng & dịch vụ nghiệp vụ
│   ├── app_services.dart          # Container cung cấp dịch vụ toàn ứng dụng
│   ├── config_service.dart        # Quản lý cấu hình kết nối mạng (Localhost / LAN / Remote)
│   ├── auth_service.dart          # Xác thực tài khoản JWT & duy trì phiên
│   ├── api_service.dart           # REST API client
│   ├── hybrid_api_service.dart    # Tự động chuyển đổi giữa Online API và Offline Fallback
│   ├── offline_game_service.dart  # Quản lý logic ván cờ offline trên thiết bị
│   └── signalr_service.dart       # Kết nối thời gian thực SignalR Hub (/gameHub)
├── theme/
│   └── app_theme.dart             # Hệ màu Cổ phong (Huyết chu, Hoàng kim, Mặc huyền, Tế mộc)
└── widgets/                       # Thành phần giao diện tái sử dụng
    ├── advantage_bar_widget.dart  # Thanh thế trận Advantage Bar thời gian thực
    ├── chess_board_widget.dart    # Bàn cờ Canvas Skia/Impeller cao cấp (Tap & Drag)
    ├── circular_turn_timer.dart   # Vòng tròn đếm ngược thời gian quanh avatar
    └── pixel_art_banners.dart     # Banner mỹ thuật Cổ phong & phù hiệu hoàng gia
```

---
*Phát triển bởi Trong Nguyen - Dự án Cờ Tướng Khóa Luận Tốt Nghiệp.*

