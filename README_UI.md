# ChessCore – layout 100vh và AI đánh chắc

Đọc **THAY_DOI_CU_THE.md** trước: danh sách 6 file, CSS/HTML cốt lõi, các hệ số và cách tìm kiếm đã sửa.

# ChessCore – giao diện thi đấu đã refactor

Bản mã nguồn đầy đủ này tiếp tục từ bản đã ghép ngày 14/10/2026: ZIP ChessCore(5) + nhánh frontend-fix-by-trongnguyen tại f212819 (đã gồm backend-update).

## Chạy nhanh trong VS Code

1. Giải nén vào một thư mục mới, mở `ChessCore_Viewport_Strategic` bằng Open Folder.
2. Bật MySQL 8, chỉnh `ChessCore/appsettings.json` cho đúng user/password/database của máy.
3. Mở Terminal tại thư mục gốc có README này:

```powershell
dotnet restore .\ChessCore\ChessCore.csproj
dotnet run --project .\ChessCore\ChessCore.csproj --launch-profile http
```

Mở http://localhost:5000/Game. Chọn chế độ rồi bấm Bắt đầu.
Cần .NET SDK 9; không cần Node.js để chạy web. Ứng dụng kế thừa cơ chế tự áp dụng migrations khi khởi động.
Có thể tạo một database MySQL trống riêng và trỏ connection string vào đó để thử bản mới trước.
Nếu trang vẫn hiện giao diện cũ, nhấn Ctrl+F5. Razor dùng asp-append-version để đổi phiên bản URL CSS/JS.

## Thiết kế mới

- Bàn gỗ nổi bật, viền gỗ và bóng đổ mềm. Quân cờ vẫn dùng SVG thật và cơ chế đi quân hiện có.
- Thẻ hai bên đối xứng, đồng hồ dạng số, viền xanh khi đến lượt và viền đỏ khi bị chiếu.
- Khay “Đã ăn” ghi quân đối phương bị bên đó ăn. Dựng lại từ FEN ban đầu + lịch sử, hỗ trợ đi lại và tải lại ván, không nhầm quân vắng trong cờ thế là quân đã bị ăn.
- Một panel liên tục cho trạng thái, ưu thế, H1/H2/H3, lịch sử và thao tác; phòng/chat online giữ nguyên ID và sự kiện.
- Thanh ưu thế hiện tại, ba mini meter H1/H2/H3 và phần delta nước gần nhất tách riêng.
- H1/H2/H3 hiện tại theo góc nhìn Đỏ; tổng ba chỉ số bằng RedScore. Các thanh được chuẩn hóa để trực quan, không phải phần trăm thắng.
- Lịch sử chia cột Đỏ/Đen, đánh dấu đúng nước mới nhất, có vùng cuộn. Poll cùng lịch sử không kéo người dùng xuống cuối khi đang đọc nước cũ.
- Tọa độ hiển thị a–i và 1–10 (a1 ở góc dưới trái của bàn nhìn từ Đỏ), không đổi khi xoay. Tooltip giữ tọa độ mảng gốc để đối chiếu.
- Hàng thao tác Đi lại / Gợi ý / Xoay bàn / Đầu hàng đồng bộ; Gợi ý là nút chính, Đầu hàng là nút nguy hiểm và vẫn cần xác nhận.
- Desktop/tablet dùng hai cột trong một viewport; bàn cờ tự co theo cả chiều cao và chiều rộng. CSS arena-viewport.css nạp sau cùng thay bố cục cuộn của bản trước. Có focus bàn phím, trạng thái bằng chữ, reduced-motion và tương thích theme ban đêm.
- Font quân cờ nhỏ được đóng gói tại wwwroot/fonts, kèm giấy phép OFL. Font giao diện và icon CDN kế thừa từ ứng dụng.

## Các file chính đã thay đổi

| File trong ChessCore | Vai trò |
|---|---|
| Views/Game/Index.cshtml | Cấu trúc semantic của khu đấu và panel thống nhất; giữ ID điều khiển |
| wwwroot/css/arena.css | Design tokens, layout, thẻ, thanh điểm, lịch sử, responsive và accessibility |
| wwwroot/js/arena-ui.js | Lớp trình bày: chỉ số, quân bị ăn, lịch sử, trạng thái người chơi |
| wwwroot/js/game.js | Gọi lớp trình bày từ snapshot API; giữ logic chơi/SignalR/API |
| DTOs/GameDtos.cs | Evaluation H1/H2/H3 hiện tại và InitialFen |
| Controllers/GamesController.cs | Cung cấp breakdown từ chính bộ đánh giá của AI |
| Views/Shared/_Layout.cshtml | Loại bỏ dấu ``` thừa bị in ra sau HTML |
| ChessCore.csproj | Đồng bộ dependency EF Core 9.0.21 |
| wwwroot/fonts/* | Font subset các chữ trên bàn cờ và giấy phép |

`game.css` giữ phần nền tảng của màn chọn chế độ, modal và SVG; `arena.css` chỉ áp dụng trên trang chơi cờ để không đổi giao diện review/khóa học.
`arena-ui.js` chỉ đọc snapshot và hiển thị. Không sửa trạng thái game trên server.
Backend không thêm bảng hay migration mới cho lần refactor giao diện này.

## Đưa vào repo Git đang có

Bản ZIP đầy đủ không có lịch sử .git. Nếu muốn giữ repo đã clone, dùng bản vá `CHESSCORE_COMPLETE.patch` trong ZIP.
Bản vá đầy đủ gồm cả lần ghép AI/H1/H2/H3 trước đó và giao diện lần này; nền chính xác là f212819.
Trước khi đổi nhánh, lưu và commit phần code đang sửa nếu có. Chỉ áp dụng khi working tree sạch.

```powershell
cd D:\Desktop\ChessCore_GitHub
git fetch origin
git switch -c quang-ui-refactor f212819
```

Giả sử ZIP được giải nén thành `D:\Desktop\ChessCore_Viewport_Strategic`:

```powershell
git apply --check "D:\Desktop\ChessCore_Viewport_Strategic\CHESSCORE_COMPLETE.patch"
git apply "D:\Desktop\ChessCore_Viewport_Strategic\CHESSCORE_COMPLETE.patch"
dotnet run --project .\ChessCore\ChessCore.csproj --launch-profile http
```

Chỉ chạy git apply sau khi bước --check đạt. Nếu thư mục giải nén có thêm một lớp tên, chỉnh đường dẫn patch theo vị trí thực tế.
Sau khi chạy ổn, commit những file nguồn và tài liệu thay đổi; không add bin/obj. Không có thao tác push tự động.
Bản vá đã được kiểm tra áp dụng trên index sạch tại f212819.

## Kiểm tra lại

Engine/API contract (không cần MySQL):

```powershell
dotnet run --project .\Validation\ChessCore.Checks.csproj -c Release
```

Kiểm tra lớp trình bày (chỉ khi muốn chạy kiểm thử JavaScript, cần Node.js):

```powershell
cd ChessCore
npm install
npm run test:ui
```

Xem `KET_QUA_UI.md` để biết phạm vi đã kiểm tra và phần còn cần thử với database thực.
