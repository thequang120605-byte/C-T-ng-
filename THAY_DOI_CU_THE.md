# Sửa layout 1 khung hình và AI đánh chắc

Bản này tiếp tục từ ChessCore_UI_Refactored, không cần ghép tay nếu chạy toàn bộ source trong ZIP.

## 1. Những file cần thay nếu đang dùng bản UI trước

Chép nguyên file theo đúng đường dẫn trong `ChessCore`:

1. `Views/Game/Index.cshtml`
2. `wwwroot/css/arena-viewport.css` (file mới)
3. `wwwroot/js/game.js`
4. `wwwroot/js/arena-ui.js`
5. `Engine/BoardEvaluator.cs`
6. `Engine/MinimaxEngine.cs`

Giữ `game.css`, `arena.css`, font và các file nền của bản UI trước. Nếu đang dùng bản rất cũ, nên chạy toàn bộ bản ZIP này thay vì chỉ chép 6 file.
Không cần đổi schema database cho những chỉnh sửa lần này.

## 2. HTML: CSS viewport phải nạp sau cùng

Đầu `Views/Game/Index.cshtml`:

```cshtml
<link rel="stylesheet" href="~/css/game.css" asp-append-version="true" />
<link rel="stylesheet" href="~/css/arena.css" asp-append-version="true" />
<link rel="stylesheet" href="~/css/arena-viewport.css" asp-append-version="true" />
```

Không tự thêm `height:100vh` cho cả `.app-main` rồi cộng navbar; như vậy tổng chiều cao lại vượt màn hình.
Cấu trúc của bản có sẵn:

```html
<div class="app-container">
  <aside class="app-sidebar">...</aside>
  <div class="main-wrapper">
    <header class="app-navbar">...</header>
    <main class="app-main">
      <div class="game-view-container arena-page">
        <section class="game-layout">
          <header class="arena-heading">...</header>
          <div class="board-column">
            <div class="player-bar player-black">...</div>
            <div class="board-wrapper"><div class="xiangqi-board">...SVG...</div></div>
            <div class="player-bar player-red">...</div>
          </div>
          <aside class="arena-panel">
            ...trạng thái và đánh giá...
            <section class="move-history-box">
              ...tiêu đề...
              <div id="move-history-container" class="move-list">...</div>
            </section>
            <div class="action-box">...</div>
          </aside>
        </section>
      </div>
    </main>
  </div>
</div>
```

Đây là sơ đồ rút gọn, không thay toàn bộ HTML thật bằng dấu `...`.

## 3. CSS cốt lõi – file đầy đủ là arena-viewport.css

```css
body:has(.arena-page) .app-container { height: 100vh; height: 100dvh; overflow: hidden; }
body:has(.arena-page) .main-wrapper { height: 100%; min-height: 0; overflow: hidden; }
body:has(.arena-page) .app-navbar { flex: 0 0 var(--navbar-height, 70px); }
body:has(.arena-page) .app-main { flex: 1 1 0; min-height: 0; overflow: hidden; }
.arena-page { height: 100%; min-height: 0; overflow: hidden; }
.arena-page .game-layout {
  height: 100%; min-height: 0;
  grid-template-columns: minmax(0, 1fr) clamp(320px, 31vw, 380px);
  grid-template-rows: 40px minmax(0, 1fr);
}
.arena-page .board-column {
  height: 100%; min-height: 0; display: grid;
  grid-template-rows: 68px minmax(0, 1fr) 68px;
}
.arena-page .board-wrapper {
  height: 100%; min-height: 0; container-type: size;
  display: grid; place-items: center;
}
.arena-page .xiangqi-board {
  width: min(calc(100cqw - 18px), calc((100cqh - 18px) * .9));
  height: auto; aspect-ratio: 9 / 10;
}
.arena-page .arena-panel {
  height: 100%; min-height: 0; display: flex;
  flex-direction: column; overflow: hidden;
}
.arena-panel .move-history-box {
  flex: 1 1 0; min-height: 0; display: flex;
  flex-direction: column; overflow: hidden;
}
.arena-panel .move-list {
  flex: 1 1 0; min-height: 0; height: auto;
  overflow-y: auto; overflow-x: hidden;
}
```

Điểm quyết định là `min-height:0` xuyên suốt các lớp Grid/Flex, không chỉ `overflow:hidden` ở body. `cqw/cqh` lấy chiều rộng/cao thực tế của vùng bàn cờ. CSS đầy đủ còn có padding, card, media query cho màn hình thấp và các popover.

Trong lúc đấu, lịch sử là vùng cuộn thường trực. Thông tin phòng và chat online mở thành popover để không đẩy panel cao thêm. Chat vẫn có vùng cuộn tin nhắn riêng khi được mở. Màn chọn chế độ có thể cuộn nội bộ khi cần; body/trang đấu không cuộn.
Màn thấp <=650px chuyển chi tiết nước gần nhất sang nút “Nước vừa đi”; điểm H1/H2/H3 hiện tại và nút hành động vẫn hiện. Bố cục desktop/tablet đã thử từ 768px ngang và 600px cao.

## 4. BoardEvaluator.cs: các hệ số chỉnh phong cách

Tìm các hằng số đầu class, không thay giá trị quân H1 thành những con số quá thấp:

```csharp
public const int AdvisorPairBonus = 70;
public const int ElephantPairBonus = 55;
public const int MissingGuardPenalty = 35;
public const int GuardHomeBonus = 12;
public const int ElephantAdvancePenalty = 35;
public const int PalaceDangerPenalty = 24;
public const int HangingPiecePercent = 55;
public const int PrematureRaidPenalty = 55;
public const int RookMobilityWeight = 3;
public const int HorseMobilityWeight = 6;
public const int CannonMobilityWeight = 2;
public const int ImportantFileBonus = 18;
public const int CentralRookBonus = 28;
public const int RiverRookBonus = 22;
public const int BottomCannonBonus = 32;
```

| Đặc trưng | Điểm/cách tính |
|---|---|
| Đủ đôi Sĩ | +70 H3 |
| Đủ đôi Tượng | +55 H3 |
| Sĩ/Tượng ở ba hàng nhà | +12 mỗi quân |
| Thiếu Sĩ/Tượng | −35 × (50 + opening) / 100 mỗi quân thiếu |
| Tượng tiến hàng 4 quá sớm | −35 × opening / 100 |
| Ô cung bị địch kiểm soát | −24 × số quân tấn công, tối đa 3, mỗi ô |
| Tướng đang bị chiếu | −90 H3 |
| Quân bị tấn công, không được giữ | −55% giá trị quân |
| Quân có giữ nhưng bị quân rẻ hơn nhắm | −max(0, giá trị quân − quân tấn công rẻ nhất) / 5 |
| Xe/Mã/Pháo cơ động | Số nước giả hợp lệ × 3/6/2; cộng 2 cho mỗi ô đích chưa bị địch kiểm soát |
| Lộ 4/6 | +18 cho quân chủ lực ở cột mảng 3/5 |
| Xe trục giữa / tuần hà | +28 / +22 H2 |
| Xuất Xe trong sân nhà | +18 |
| Pháo nhòm đáy | +32 khi không bị tấn công hoặc đang được giữ |
| Quân chủ lực qua sông sớm, thiếu bảo vệ | −55 × opening / 100 |
| Lên Sĩ / gác Tượng giữa | Bonus H3 theo giai đoạn |

`opening` nằm trong [0,100], ước lượng từ số quân chủ lực còn ở vị trí chưa khai triển. Khi đã khai triển hoặc vào tàn cuộc, phạt lao quân sớm giảm dần. Đây không phải số ply thật của ván.

Công thức tổng không đổi:

```csharp
int net = red.H1 + red.H2 + red.H3 - black.H1 - black.H2 - black.H3;
return perspective == PieceColor.Red ? net : -net;
```

Bản đầy đủ dựng bản đồ kiểm soát một lần cho mỗi evaluation, gồm cả ô có quân cùng màu. Mobility/control dùng pseudo attacks; đây là xấp xỉ heuristic, không phải số nước hợp lệ tuyệt đối của quân bị ghim.
Thưởng phòng thủ không có nghĩa khóa cứng Sĩ/Tượng: chiếu bí/hết nước vẫn có điểm thắng-thua lớn hơn mọi bonus; vào tàn cuộc vẫn có thể khai thác Tướng và Tượng linh hoạt.

## 5. MinimaxEngine.cs: Alpha-Beta và bẫy ăn quân

Không có tham số “già rơ” trong Alpha-Beta. Thay đổi đánh giá làm đổi phong cách; thay đổi tìm kiếm giúp nhận ra hậu quả chiến thuật.

Dòng quan trọng ở đầu Search:

```csharp
if (depth == 0)
    return Quiescence(board, alpha, beta, perspective, ply, 0);
```

Thay cho việc trả `BoardEvaluator.Evaluate(...)` ngay khi depth về 0.

Quiescence trong file đầy đủ:

- Nếu hết nước hợp lệ: trả thua theo luật Cờ Tướng, kể cả không bị chiếu.
- Nếu không bị chiếu: dùng stand-pat, rồi xét tiếp các nước ăn quân tối đa 4 ply chiến thuật.
- Nếu đang bị chiếu: xét mọi nước thoát chiếu, không được coi đứng yên là lựa chọn hợp lệ.
- Chặn tổng độ dài quiescence ở 8 ply để chuỗi chiếu lặp không kéo dài vô hạn. Tại giới hạn cứng vẫn phải đánh giá tĩnh, nên chiến thuật dài hơn giới hạn có thể bị bỏ sót.
- Không delta-pruning các nước hy sinh theo giá trị vật chất đơn thuần.

Cải tiến hiệu năng/độ ổn định:

1. Iterative deepening 1 → depth yêu cầu; `CompletedDepth` báo độ sâu đã hoàn tất. Không âm thầm hạ depth 4 xuống depth 2.
2. PV/TT move trước, MVV-LVA, killer moves và history heuristic để tăng cutoffs.
3. Make/undo có finally; không tạo clone cho từng node.
4. TT riêng mỗi engine, khóa gồm vị trí và depth; chuẩn hóa điểm mate theo ply.
5. Không thêm null-move/futility pruning loại bỏ sớm các nước củng cố yên tĩnh.

Ăn quân được tìm trước trong move ordering không có nghĩa được cộng điểm vào quyết định cuối. Quyết định dựa trên minimax + H1/H2/H3 và quiescence. Bộ máy này có thiên hướng chắc chắn hơn; “giăng bẫy” không phải một luật nhận diện mọi bẫy hay bảo đảm trình độ cao thủ.

## 6. Chạy

```powershell
dotnet run --project .\ChessCore\ChessCore.csproj --launch-profile http
```

Mở http://localhost:5000/Game. Bật MySQL và chỉnh connection string như bản trước. Nếu thử bản mới trong thư mục repo cũ, bỏ bin/obj được sinh từ bản cũ rồi restore/build lại; không mang bin/obj từ thư mục khác sang.

Kiểm tra engine/API:

```powershell
dotnet run --project .\Validation\ChessCore.Checks.csproj -c Release
```

Bản vá CHESSCORE_COMPLETE.patch vẫn dùng nền Git f212819; nó chứa toàn bộ thay đổi tính từ nền đó, không áp dụng chồng lên bản đã sửa. Với bản UI trước đã có trên máy, chép 6 file liệt kê ở đầu hoặc chạy bản ZIP mới là cách trực tiếp.
