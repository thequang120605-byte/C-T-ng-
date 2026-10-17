# KẾT QUẢ KIỂM TRA — 17/10/2026

- Build ASP.NET Core: 0 lỗi, 39 nullable warnings còn lại.
- Kiểm tra điểm/nghiệp vụ: 41 PASS. Dùng SQLite tạm; gọi trực tiếp controller/service và SignalR giả lập.
- Kiểm tra engine/luật hiện có trong Validation/Program.cs: 34 PASS, có tìm kiếm depth 1–4.
- UI regression hiện có: 13 PASS. Thêm bài kiểm tra H4/UI/quiz contract: 12 assertions PASS.
- Kiểm tra cú pháp Node cho game.js, course.js, puzzle.js: PASS.
- EF model: không có thay đổi model chưa nằm trong migration.
- Đã sinh script MySQL nâng cấp từ AddThesisRequiredSchema đến ScoringModesAndSettlement.

Giới hạn: chưa apply migration lên MySQL thật; chưa kiểm thử HTTP middleware/SignalR thật, đa instance, tải đồng thời hoặc screenshot/responsive. Bộ test JavaScript ở TestSuites gọi website công khai không được chạy.

UI regression gốc thất bại vì thiếu ID khay quân/player-card trong View. Bản sửa đã khôi phục các ID để nối lại ArenaUI và test.

Timing depth 4 trong log chỉ phản ánh máy kiểm tra và thế cờ đó, không phải cam kết thời gian khi triển khai.
