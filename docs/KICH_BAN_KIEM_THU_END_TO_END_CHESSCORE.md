# KỊCH BẢN & MA TRẬN KIỂM THỬ TOÀN TRÌNH (END-TO-END TEST SPECIFICATION)
## HỆ THỐNG CỜ TƯỚNG TRỰC TUYẾN TÍCH HỢP TRÍ TUỆ NHÂN TẠO - CHESSCORE

---

## I. MỤC TIÊU & PHẠM VI KIỂM THỬ (TEST OBJECTIVE & SCOPE)
1. **Mục tiêu**: Đảm bảo toàn bộ chu trình nghiệp vụ (User Journey) từ giao diện người dùng (Front-end), các dịch vụ điều phối (Backend REST API & SignalR Hub), động cơ phân tích trí tuệ nhân tạo (AI Minimax Engine) đến cơ sở dữ liệu quan hệ (Database Persistence) hoạt động đồng bộ, chính xác và không có lỗi logic.
2. **Phạm vi**: 7 phân hệ nghiệp vụ cốt lõi tương ứng 7 Ca Sử Dụng (Use Cases) trong đề án:
   - **E2E-01**: Xác thực & Quản lý tài khoản (Auth, OTP, Profile & Elo)
   - **E2E-02**: Tổ chức & Thi đấu Cờ tướng (PvP, PvE, Move Rules, Matchmaking & Timeout)
   - **E2E-03**: Thẩm định & Phân tích ván cờ (AI Game Review & Advantage Eval Bar)
   - **E2E-04**: Luyện giải Cờ thế Sa bàn (Puzzle Campaign, 3-Star Rating & AI Defense)
   - **E2E-05**: Đào tạo Kỳ nghệ & Thi cấp chứng chỉ (Courses, Quiz & Certification)
   - **E2E-06**: Tra cứu Khai cuộc & Kỳ phổ danh thủ (Opening Explorer & Master Games Replay)
   - **E2E-07**: Quản trị Hệ thống & Đo kiểm AI (Admin Portal & Minimax Benchmark)

---

## II. MA TRẬN KỊCH BẢN KIỂM THỬ CHI TIẾT (E2E TEST MATRIX)

### Kịch bản E2E-01: Quy trình Xác thực & Quản lý Tài khoản (UC-01)
* **Tiền điều kiện**: Người dùng truy cập trang web, có hòm thư điện tử hợp lệ.
* **Các bước thực hiện**:
  1. Truy cập `/Auth/Register`, nhập Email, Username và Mật khẩu.
  2. Hệ thống sinh mã OTP 6 số ngẫu nhiên và gửi tới hòm thư người dùng.
  3. Người dùng mở email, lấy mã xác thực và nhập vào form xác nhận OTP.
  4. Hệ thống kiểm tra: Nếu đúng, băm mật khẩu SHA-256, khởi tạo tài khoản với Elo mặc định 1200.
  5. Người dùng đăng nhập tại `/Auth/Login`, hệ thống cấp Cookie xác thực phiên làm việc.
  6. Điều hướng tới `/Dashboard` xem hồ sơ, ảnh đại diện và đồ thị lịch sử Elo.
* **Kết quả mong đợi**: Tài khoản được lưu vào CSDL, đăng nhập thành công, phiên làm việc an toàn, đồ thị Elo hiển thị 1200 điểm.
* **Đánh giá**: **PASS (ĐẠT)**

---

### Kịch bản E2E-02: Quy trình Tổ chức & Thi đấu Cờ Tướng (UC-02)
* **Tiền điều kiện**: Kỳ thủ đã truy cập Sảnh thi đấu `/Game`.
* **Các bước thực hiện**:
  1. Kỳ thủ A tạo phòng đấu với mã phòng riêng biệt (Private Room) hoặc công khai (Public Lobby).
  2. Kỳ thủ B nhập mã phòng và tham gia; hệ thống SignalR khóa phòng (Strict 2-player lock) và phân màu quân.
  3. Kỳ thủ kéo thả nước đi trên bàn cờ ảo SVG:
     - Thử nước đi sai luật (Mã bị cản chân, Tượng vượt sông): Hệ thống từ chối nước đi, hiển thị cảnh báo và bắt đi lại.
     - Đi nước hợp lệ: Hệ thống ghi nhận nước đi vào biên bản PGN, cập nhật FEN và chuyển lượt đồng hồ đếm ngược.
  4. Nếu đấu luyện với máy (PvE): Sau mỗi nước đi của người chơi, Động cơ AI Minimax tự động tính toán và trả lời nước cờ trong vòng < 500ms.
  5. Ván cờ kết thúc (Chiếu bí / Hết giờ / Cầu hòa / Đầu hàng): Hệ thống công bố kết quả và tính toán biến động điểm Elo cho hai kỳ thủ.
* **Kết quả mong đợi**: Bàn cờ đồng bộ thời gian thực qua SignalR, bắt lỗi luật cờ 100% chính xác, AI phản hồi mượt mà, điểm Elo được cập nhật tự động.
* **Đánh giá**: **PASS (ĐẠT)**

---

### Kịch bản E2E-03: Thẩm định & Phân tích Ván Cờ (UC-03)
* **Tiền điều kiện**: Kỳ thủ đã hoàn thành ít nhất 1 ván đấu được lưu lại biên bản.
* **Các bước thực hiện**:
  1. Kỳ thủ truy cập danh sách lịch sử đấu, chọn xem lại ván cờ.
  2. Bấm nút "Phân tích ván đấu với AI" (`/Game/ReviewGame`).
  3. Động cơ AI Minimax duyệt tuần tự từng thế cờ FEN xuyên suốt ván đấu:
     - Tính điểm chênh lệch ưu thế (Eval Bar score).
     - Phân loại chất lượng nước đi: Tối ưu (Best), Tốt (Good), Sai lầm (Inaccuracy), Sai sót nặng (Blunder).
  4. Hệ thống tạo báo cáo thẩm định, vẽ đồ thị hình sóng ưu thế ván cờ.
  5. Kỳ thủ tương tác bấm từng nước cờ để xem AI gợi ý nước đi tối ưu thay thế.
* **Kết quả mong đợi**: Đồ thị Eval Bar hiển thị trực quan, các nước đi sai sót được tô màu cảnh báo kèm nước đi đề xuất chuẩn xác từ AI.
* **Đánh giá**: **PASS (ĐẠT)**

---

### Kịch bản E2E-04: Luyện giải Cờ thế Sa bàn Thông minh (UC-04)
* **Tiền điều kiện**: Truy cập phân hệ Cờ thế `/Puzzle`.
* **Các bước thực hiện**:
  1. Người chơi mở Sa bàn chiến dịch (Campaign Map) và chọn màn chơi (Level 1-500).
  2. Hệ thống tải thế cờ tàn cuộc FEN lên bàn cờ.
  3. Người chơi thực hiện nước đi giải cờ:
     - Nếu đi sai: Hệ thống báo "Nước đi chưa tối ưu", cho phép thử lại hoặc bấm gợi ý (Hint).
     - Nếu đi đúng: Động cơ AI tự động đi nước chống đỡ tối ưu, yêu cầu người chơi đi tiếp nước sát cục.
  4. Sau khi hoàn thành thế cờ: Hệ thống chấm sao (1 - 3 sao) dựa trên số lần thử lại, cộng điểm thành tích và tự động mở khóa màn chơi kế tiếp.
* **Kết quả mong đợi**: Thế cờ tải nhanh, AI phản đòn thông minh, hệ thống sao và mở khóa màn chơi hoạt động chính xác theo cơ chế tuyến tính.
* **Đánh giá**: **PASS (ĐẠT)**

---

### Kịch bản E2E-05: Đào tạo Kỳ nghệ & Thi Cấp Chứng chỉ (UC-05)
* **Tiền điều kiện**: Kỳ thủ truy cập cổng đào tạo `/Course`.
* **Các bước thực hiện**:
  1. Lựa chọn khóa học căn bản (Nhập môn, Khai cuộc, Trung cuộc, Tàn cuộc).
  2. Xem bài giảng lý thuyết kèm các thế cờ trực quan có thể tương tác nước đi.
  3. Đăng ký tham gia bài thi sát hạch cuối khóa:
     - Phần 1: Trắc nghiệm lý thuyết luật cờ và chiến thuật.
     - Phần 2: Giải 5 thế cờ thực hành sát cục.
  4. Nộp bài, hệ thống tự động chấm điểm:
     - Nếu đạt điểm $\ge 80\%$: Sinh mã chứng chỉ duy nhất dạng `CERT-CT-XXXXX`, hiển thị chứng chỉ điện tử kèm liên kết tải về.
     - Nếu $< 80\%$: Báo trượt, hiển thị các nội dung cần ôn tập và cho phép thi lại.
* **Kết quả mong đợi**: Giáo trình tải đầy đủ, chấm điểm tự động khách quan, chứng chỉ điện tử render sắc nét với mã chứng thực độc bản.
* **Đánh giá**: **PASS (ĐẠT)**

---

### Kịch bản E2E-06: Tra cứu Khai cuộc & Kỳ phổ Danh thủ (UC-06)
* **Tiền điều kiện**: Truy cập `/MasterGame` và `/Opening`.
* **Các bước thực hiện**:
  1. Người dùng chọn một thế trận khai cuộc (Thuận Pháo, Nghịch Pháo, Bình Phong Mã,...).
  2. Hệ thống truy vấn cây biến thể `OpeningBookMoves`, hiển thị các nước đi tiếp theo kèm tỷ lệ Thắng / Hòa / Bại thống kê từ hàng triệu ván cờ.
  3. Người dùng chuyển sang mục "Ván đấu Danh thủ", lọc theo tên kiện tướng (Lại Lý Huynh, Vương Thiên Nhất,...).
  4. Chọn một ván cờ đỉnh cao và bấm nút Phát lại (Replay): Hệ thống tự động phát từng nước đi hoặc cho phép người dùng bấm tới/lui theo ý muốn.
* **Kết quả mong đợi**: Cây biến thể phản hồi tức thì, danh sách ván đấu danh thủ đầy đủ biên bản PGN, tính năng Replay hoạt động trơn tru.
* **Đánh giá**: **PASS (ĐẠT)**

---

### Kịch bản E2E-07: Quản trị Hệ thống & Đo kiểm Hiệu năng AI (UC-07)
* **Tiền điều kiện**: Đăng nhập tài khoản với vai trò Quản trị viên (Admin).
* **Các bước thực hiện**:
  1. Truy cập Bảng điều khiển quản trị `/Admin`.
  2. Thực hiện quản trị người dùng: Khóa tài khoản vi phạm hoặc mở khóa.
  3. Quản lý nội dung: Thêm/sửa bài tập cờ thế và các khóa học.
  4. Chuyển sang công cụ Đo kiểm hiệu năng AI (Benchmark Tool):
     - Thiết lập chuỗi FEN kiểm thử và độ sâu tìm kiếm (Depth 1 đến 5).
     - Bấm "Bắt đầu đo kiểm": Động cơ AI Minimax thực thi tính toán và thu thập các chỉ số: Số nút duyệt (Nodes), Tốc độ duyệt (NPS - Nodes Per Second), Tỷ lệ cắt tỉa Alpha-Beta Pruning.
     - Xuất biểu đồ so sánh hiệu năng theo từng độ sâu.
* **Kết quả mong đợi**: Phân quyền Admin chặt chẽ, các thao tác quản trị cập nhật CSDL ngay lập tức, công cụ Benchmark AI đo đạc và vẽ biểu đồ chính xác.
* **Đánh giá**: **PASS (ĐẠT)**

---

## III. KẾT QUẢ THỰC THI KIỂM THỬ TỰ ĐỘNG HÓA (AUTOMATED RUN LOG)
* **File thực thi**: `TestSuites/test_e2e_full_system.cjs`
* **Công nghệ**: Node.js, Fetch API, Assertion Library, SignalR Protocol
* **Môi trường kiểm thử**:
  - Live Cloud: `https://co-tuong-pro.onrender.com`
  - Local Server: `http://localhost:5000`
* **Kết quả tổng hợp**:
  - Tổng số test case tự động: **20 / 20**
  - Số test case thành công (PASS): **20 (100%)**
  - Số test case thất bại (FAIL): **0 (0%)**
  - Thời gian phản hồi trung bình: **~200ms / request**
  - Kết luận: **HỆ THỐNG ĐẠT CHUẨN NGHIỆM THU TOÀN DIỆN (READY FOR PRODUCTION / DEFENSE)**.
