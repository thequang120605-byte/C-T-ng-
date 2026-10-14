# Kiểm tra bản viewport/strategic – 14/10/2026

- Build ASP.NET Core/Razor thành công, 0 lỗi; còn cảnh báo nullable kế thừa.
- 34 kiểm tra engine/API đạt. Có kiểm tra form Sĩ, Tượng không tiến sớm, khai triển Xe, đối xứng hai bên, mate ở chân trời tìm kiếm, bảo toàn bàn cờ và bỏ nước ăn Tốt có thể bị Xe bắt lại. Bài bẫy xác nhận nước ăn Tốt là nước hợp lệ trước khi kiểm tra AI từ chối.
- 13 kiểm tra lớp UI JavaScript đạt; cú pháp hai file JS đạt.
- Chromium với Razor render thật, CSS/JS thật và API mô phỏng: không tràn ngang/dọc của body/app-main ở 8 viewport; không chồng lấn; action-footer luôn nằm trong màn hình; lịch sử có vùng hiển thị >=40px.
- 160 nước lịch sử: scrollHeight > clientHeight; lăn chuột làm cuộn lịch sử, app-main.scrollTop vẫn bằng 0.
- Viewport đã thử: 1920x1080, 1440x900, 1366x768, 1280x720, 1024x768, 1024x600, 820x1180, 768x1024.
- Đã kiểm tra xoay bàn, khay quân ăn, mini meter, highlight, mở/hủy đầu hàng và theme ban đêm; không có JavaScript exception.

## Đo AI – Release, Linux .NET SDK 9.0.319

| Thế cờ | Depth hoàn tất | ms | Nodes | Q-nodes | Cutoffs | TT hits |
|---|---:|---:|---:|---:|---:|---:|
| Xuất phát | 3 | 224 | 5947 | 5649 | 971 | 0 |
| Xuất phát | 4 | 628 | 31078 | 27106 | 6815 | 739 |
| Sau 4 nước khai triển | 4 | 536 | 25395 | 22619 | 5919 | 456 |

Đây là một lần đo mẫu, không phải giới hạn thời gian cho mọi thế cờ/máy.
Chưa chạy E2E với MySQL thực và hai trình duyệt online. Chưa có bộ tự đấu đủ lớn để chứng minh sức cờ tăng theo Elo; các bonus là điểm khởi đầu để hiệu chỉnh. Quiescence bị giới hạn để tránh chuỗi chiếu vô hạn.
