# Kiểm chứng bản Pixel 1.2

## Đã chạy
- 13 kiểm tra JSON/contract và nhận phe: PASS.
- 18 kiểm tra GameViewModel và tích hợp HybridApiService: PASS. Bao gồm một session offline thật, chạm quân Đỏ (6,0), đi tới (5,0), máy thực hiện nước đáp lại và trở về lượt Đỏ; không chuẩn bị auth request HTTP và không tham gia SignalR.
- 18 kiểm tra engine/service offline: PASS. Có kiểm tra 44 nước mở đầu, nước sai không làm đổi bàn, cancellation trước/trong search, search giữ nguyên bàn gốc, ngân sách tìm kiếm, lượt máy không lặp, snapshot độc lập, xin thua, hết nước hợp lệ là thua và giới hạn session trong RAM.
- Tổng 49 kiểm tra PASS trên .NET SDK 9.0.305 trong môi trường host. Đây không phải benchmark Android.
- Biên dịch Models/Offline/ViewModels/Services/Controls/code-behind với MAUI Controls 9.0.120 và SignalR 9.0.2: 0 error, 0 warning. Dùng stub InitializeComponent/named controls để kiểm tra C# độc lập; không phải build XAML/Android.
- Cú pháp C#: 51 file PASS. XML/SVG/project: 28 file PASS; kiểm tra tham chiếu binding, resource, image/event handler và manifest Release.
- ZIP có mục thư mục tường minh, không chứa APK cũ hoặc ZIP lồng nhau; giải nén kiểm tra lại sau đóng gói.

## Chưa chạy trong môi trường chỉnh sửa
- Chưa build APK mới bằng Android SDK/workload và chưa chạy UI thật trên Pixel/emulator.
- Chưa đo FPS, RAM, battery và thời gian phản hồi trên thiết bị.
- Chưa kiểm tra end-to-end online/auth với backend của người dùng.
- Run-Pixel.ps1 cần Windows/Android SDK/AVD thực tế; chưa được chạy trong môi trường Linux chỉnh sửa.

AI luyện tập được tái sử dụng từ source người dùng, không có chứng nhận engine thi đấu. Luật lặp offline giản lược cho luyện tập, chưa xử lý đầy đủ chiếu dai/đuổi dai. Ván offline không đổi Elo.
