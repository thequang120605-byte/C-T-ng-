# RÀ SOÁT ĐIỂM, THỐNG KÊ VÀ H4 — 17/10/2026

Phạm vi: source trong C-T-ng--frontend-fix-by-trongnguyen.zip. Không có bản sao cơ sở dữ liệu vận hành; chưa kiểm toán các ván đã lưu trên máy/chủ hosting của nhóm. Bản sửa không tự sửa hay quy đổi số Elo cũ.

## 1. Các loại điểm cần phân biệt

| Loại | Ý nghĩa | Cộng / trừ trong bản sửa |
|---|---|---|
| H1/H2/H3/H4 | Lượng giá một thế cờ cho engine | Thay đổi theo vị trí, không ghi vào Elo |
| Elo online nội bộ | Sức mạnh tương đối trong nhóm người chơi của ứng dụng | Chỉ ván Online mới, có xếp hạng, hai người khác nhau, bàn xuất phát chuẩn |
| Điểm quiz | Tỉ lệ câu trả lời đúng | 0–100%, chấm ở server; lưu điểm tốt nhất, không trừ Elo |
| Tiến độ khóa học | Bài đã đạt đủ câu quiz | Chỉ hoàn thành khi đúng tất cả; điểm cũ tốt hơn không bị giảm |
| Cờ thế | Số bài duy nhất đã giải; lượt gửi lời giải | Bài giải xong lưu CompletedAt, không cộng Elo; gợi ý không trừ Elo |
| Time Rush | Điểm phiên luyện tập | Số bài đúng × 5; chỉ phiên trình duyệt, chưa có ledger/BXH server |
| Điểm kết quả giải đấu | Điểm theo điều lệ, ví dụ thắng 2 / hòa 1 / thua 0 | Chưa có phân hệ giải đấu; không được đồng nhất với Elo hoặc H1–H4 |

## 2. Vấn đề phát hiện và phần đã sửa

| Phần | Trước | Sau |
|---|---|---|
| Kết thúc ván | Thắng +15, thua −10, hòa +2 cho cả người đấu máy và online; làm phình điểm | Elo chỉ dành cho Online đủ điều kiện; K=32, expected score theo chênh lệch rating |
| Lịch sử Elo | Có model nhưng nhánh kết thúc ván không ghi ledger | Ghi OldElo, NewElo, EloChange, GameId và lý do cho cả hai bên |
| Xử lý kết quả lặp | Không có biên nhận | ResultsProcessedAt, transaction Serializable, retry với context mới; không quyết toán lần hai |
| Timeout | Worker không cập nhật Elo/thống kê; còn áp dụng nhầm PvE | Chỉ Online; cập nhật có điều kiện deadline/status; cùng dùng dịch vụ quyết toán; phục hồi kết quả chưa quyết toán |
| Mời người chơi | Tự bắt đầu ván khi chủ phòng nhập ID đối thủ | Chỉ giữ chỗ; đối thủ phải tham gia mới chạy thời gian và chơi xếp hạng |
| Vào phòng | Có thể mở lại ván đã kết thúc | Tái kết nối không đổi kết quả; giành chỗ bằng cập nhật có điều kiện |
| Online | Người tham gia có thể đi cho phía đối thủ hoặc gọi máy/gợi ý | Kiểm tra đúng chủ lượt; chặn computer-move và hint cho Online |
| Undo | Có thể mở lại ván đã tính điểm | Chặn undo sau kết thúc/quyết toán; xác định PvE bằng mode lưu ở server |
| Chế độ chơi | Model có GameMode nhưng luồng tạo ván không gán đúng | Gán Online / PvE / PvP / AiVsAi; dữ liệu cũ thiếu căn cứ là Legacy |
| Dashboard cá nhân | Tổng kết lẫn chế độ; lịch sử dùng Elo hiện tại cho mọi ván | Bảng theo chế độ; tỷ lệ thắng dùng ván kết thúc; rating sau ván lấy ledger, không suy diễn |
| Biểu đồ Elo | Lấy 30 bản ghi đầu; thiếu history thì dựng mốc 1200 giả định | 30 bản ghi mới nhất theo thời gian/ID; thiếu history chỉ hiện số hiện tại |
| Admin | Tổng số ván đã tạo, chưa có chia mode/ledger | Bảng mode, học tập, cờ thế, 50 dòng cộng/trừ rating gần nhất, xuất CSV |
| Trang quản lý user | Dùng các bộ đếm tổng cũ có thể sai | Tổng hợp ván người chơi đã kết thúc từ bảng Games, không ghi đè số lịch sử |
| Khóa học | Tin QuizScore, QuizAttempts, IsCompleted do client gửi; đáp án nằm data-answer | Server chấm từ đáp án theo question ID, đếm lượt server và kiểm tra khóa bài; bỏ data-answer |
| Time Rush | Cộng Elo trên navbar bằng JS, không lưu DB | Bỏ cộng giả; hiển thị rõ điểm luyện tập của phiên |
| Xem lại ván | So điểm thế trước/sau, baseline mặc định 0, gắn nhãn Brilliant/Best chưa có căn cứ | So best depth 2 với nước đã đi + depth 1; điểm mất ≥0; nhãn/độ chính xác nội bộ; giải thích H1–H4 từng nước |
| Bảng vị trí phụ | UI gọi là mô hình NNUE dù là trọng số thủ công | Đổi nhãn UI thành Bảng vị trí phụ; giữ field/API cũ để tương thích |
| SMTP trong source | Mật khẩu ứng dụng viết trực tiếp | Chuyển sang Email:Sender / Email:AppPassword; không hiển thị OTP khi email lỗi |
| UI regression có sẵn | Thiếu ID khay quân và player-card nên test/UI không tìm được | Khôi phục ID nối với ArenaUI; bộ test giao diện cũ chạy lại được |

## 3. Elo online: chính sách cụ thể

E_R = 1 / (1 + 10^((ratingBlack − ratingRed) / 400))

Δ_R = round_away_from_zero(32 × (S_R − E_R)); Δ_B = −Δ_R.

S_R = 1 khi Đỏ thắng, 0,5 khi hòa, 0 khi Đỏ thua. Giới hạn chuyển điểm để không tạo rating âm; vẫn giữ tổng điểm hai bên. K=32 là lựa chọn của ứng dụng, không tuyên bố là hệ số xếp hạng WXF chính thức.

| Rating trước | Kết quả Đỏ | Đỏ | Đen |
|---|---|---:|---:|
| 1200 vs 1200 | Thắng | +16 | −16 |
| 1200 vs 1200 | Hòa | 0 | 0 |
| 1200 vs 1200 | Thua | −16 | +16 |
| 1000 vs 1400 | Thắng | +29 | −29 |
| 1000 vs 1400 | Hòa | +13 | −13 |
| 1000 vs 1400 | Thua | −3 | +3 |

Các ván mới đấu máy, hai người cùng máy, máy–máy, khóa học, cờ thế: Δ Elo = 0. Các ván online cũ giữ IsRated=false để không tự tính lại lịch sử. Không cộng điểm lại cho các ván đã kết thúc trước migration.

## 4. H1 / H2 / H3 có phải điểm luật thế giới?

**Không có thang H1/H2/H3/H4 bắt buộc của WXF.** Phải tách luật nước đi/kết quả, điểm giải đấu và hàm lượng giá AI.

H1 trong code phù hợp một bảng giá trị tương đối phổ biến được Xiangqi.com trình bày: Tốt 1 trước sông / 2 sau sông; Sĩ 2; Tượng 2; Mã 4; Pháo 4,5; Xe 9. Quy mô đồ án nhân 100: 100/200/200/400/450/900. Tướng không được dùng như quân có thể trao đổi; engine xử lý thua khi không còn nước hợp lệ.

- H1 = tổng giá trị quân mỗi bên. Ăn Mã tăng chênh lệch vật chất 400 cho bên ăn. Tốt qua sông tăng 100 theo quy ước của code, dù không ăn quân.
- H2 = cơ động + vị trí: trọng số nước giả hợp lệ Xe 3, Mã 6, Pháo 2; cơ động an toàn thêm 2; lộ 4/6 +18; Xe trung lộ +28; Xe tuyến sông +22; Xe đã phát triển +18; Pháo đáy +32; quân chủ lực ≤1 nước đi bị −35. Có thêm ưu tiên trung tâm/Tốt tiến sâu.
- H3 = an toàn/chiến thuật: đôi Sĩ +70, đôi Tượng +55; thiếu quân che tướng bị phạt theo giai đoạn; quân được bảo vệ +8; quân bị công không được bảo vệ bị phạt 55% giá trị; chiếu tướng −90; ô vùng cung bị khống chế phạt 24×min(3,số công); áp lực Pháo và phát triển Sĩ/Tượng có hệ số riêng.
- H2/H3 và các hệ số trên là thiết kế thủ công trong source. Chưa có bằng chứng hiệu chỉnh thống kê hoặc tự động học trọng số. Nghiên cứu của Tseng và cộng sự cho thấy trọng số lượng giá cờ tướng là đối tượng huấn luyện/hiệu chỉnh thực nghiệm, không phải luật thi đấu.

Góc nhìn: điểm toàn bàn luôn Đỏ − Đen, nên dương là Đỏ ưu thế. Delta của nước vừa đi tính theo bên vừa đi; nước tốt của Đen có thể làm điểm toàn bàn giảm nhưng Delta dương.

## 5. H4 — Tam tử / tứ tử đồng biên

Trong source gốc không tìm được một hàm nhận diện/tổng thưởng tam tử–tứ tử độc lập để đơn thuần di chuyển khỏi H3. Bản sửa bổ sung một heuristic H4 riêng, giữ nguyên các hạng H3 khác để không tự ý đổi công thức phòng thủ.

Tiêu chí xấp xỉ công khai:

1. Chỉ xét quân Xe, Mã, Pháo cùng bên, đã qua sông.
2. Cùng cánh trái (cột 0–3) hoặc phải (cột 5–8); cột giữa 4 không tính vào cánh.
3. Có ít nhất hai quân trong nhóm khống chế hình học ít nhất một ô vùng cung đối phương. Mỗi quân chỉ đếm một lần.
4. Đúng 3 quân đủ điều kiện: +60 cho bên đó; 4 quân trở lên: +100, không cộng chồng thành 160.
5. H4 bàn = H4 Đỏ − H4 Đen. Tổng heuristic = H1 + H2 + H3 + H4.

H4 không xác nhận chiếu bí hoặc tấn công cưỡng bức. Bản đồ khống chế là hình học, có thể chứa quân bị ghim; cần tập thế gán nhãn/đánh giá chuyên gia để tinh chỉnh. Không gọi +60/+100 là mức điểm WXF. Đã nối vào Minimax, DTO H4/DeltaH4, API ván, SignalR timeout, thẻ H4/thanh chỉ số, tổng điểm và phân tích ván.

## 6. Thống kê / báo cáo

- Dashboard cá nhân và admin có hàng riêng cho Online, PvE, PvP local, AiVsAi và Legacy.
- Ván đang chơi/phòng chờ không nằm trong mẫu số tỷ lệ thắng.
- Thắng/thua cá nhân chỉ có ý nghĩa ở PvE và Online; local/máy–máy hiển thị dấu —.
- Khóa học: số bài hoàn thành, số lượt quiz, trung bình điểm tốt nhất của bài đã làm quiz.
- Cờ thế: bài duy nhất đã giải, tổng lượt gửi lời giải; đây không phải tỷ lệ thành công của các phiên hoàn chỉnh.
- Admin đếm bản ghi người–bài; một bài được hai người giải là hai bản ghi, không phải hai bài khác nhau trong kho.
- Phân biệt kho Puzzles do admin quản lý với kho chiến dịch JSON đã được engine xác minh. Chưa gộp hai nguồn dữ liệu.
- Báo cáo CSV UTF-8 có thống kê chế độ và phần học tập. Có nút tải tại Dashboard và Admin.
- Admin có ledger ghi điểm: rating trước, thay đổi, sau, người chơi, ván, lý do.

## 7. Cập nhật và kiểm tra

Sao lưu database và source trước khi áp dụng trên môi trường thật. Bản này có migration mới `20261017055617_ScoringModesAndSettlement` và SQL nâng cấp từ migration liền trước `AddThesisRequiredSchema` trong `ChessCore/Database/ScoringModesAndSettlement.sql`. Chọn một cách cập nhật migration; không chạy tùy tiện cả SQL lẫn một script thay thế ngoài lịch sử EF.

Từ thư mục gốc source:

```powershell
dotnet restore ChessCore/ChessCore.csproj
dotnet build ChessCore/ChessCore.csproj
dotnet ef database update --project ChessCore/ChessCore.csproj
dotnet run --project ChessCore/ChessCore.csproj
```

Nếu chưa cài EF CLI: `dotnet tool install --global dotnet-ef --version 9.0.21`. Startup hiện vẫn gọi Migrate theo source gốc; nên chủ động triển khai migration trước khi mở nhận request. Migration chuyển ván cũ có đủ hai người khác nhau thành Online; trường hợp không có căn cứ là Legacy. Giữ Elo hiện tại làm baseline, không dựng lại điểm cũ hay cấp thưởng bù.

SMTP dùng biến môi trường `Email__Sender`, `Email__AppPassword`; credential cũ phải được chủ tài khoản thu hồi. Không ghi mật khẩu mới vào source.

Chạy kiểm thử:

```powershell
dotnet run --project Validation/ScoringChecks.csproj
dotnet run --project Validation/ChessCore.Checks.csproj
cd ChessCore
npm ci
npm run test:ui
```

Validation/ScoringChecks dùng SQLite tạm và SignalR giả lập, không kết nối database của người dùng. Các test gọi trực tiếp controller kiểm tra nghiệp vụ; chưa phải HTTP integration test middleware/auth/cookie thật.

## 8. Phần còn phải nâng cấp

1. Việc ghi Move và đổi trạng thái Game vẫn là hai lần SaveChanges. Quyết toán Elo đã có transaction riêng, nhưng chưa làm toàn bộ luồng đi cờ nguyên tử; cần command service + concurrency/version/idempotency toàn luồng trước Production.
2. Chưa kiểm thử cạnh tranh đa instance trên MySQL thật hoặc lỗi commit không rõ kết quả trên đường truyền. Serializable/retry cần thử dưới tải thực tế và theo dõi deadlock.
3. Elo cũ có thể đã trộn điểm thắng máy; không có lịch sử đầy đủ để sửa đáng tin cậy. Muốn reset mùa giải hoặc tính lại phải có quyết định riêng và dữ liệu DB.
4. Time Rush chưa có phiên server, thời gian chuẩn server, ledger thưởng, lưu kết quả từng lượt hoặc chống sửa JS. Bản sửa ghi rõ đây là điểm phiên, không giả vờ đồng bộ Elo.
5. Cờ thế vẫn có bộ đếm Attempts dựa trên request và các catch DbUpdateException quá rộng từ code cũ; cần phân biệt lỗi trùng khóa với lỗi DB khác và lưu phiên/assistance nếu muốn BXH thi đấu.
6. Quiz đã chấm ở server, nhưng quiz gửi lại trùng vẫn tính thêm lượt; cần submission ID/concurrency nếu đòi thống kê lượt chính xác dưới retry/tải đồng thời. Chứng chỉ/tiến độ lịch sử có thể chứa kết quả client đã gửi từ bản cũ.
7. Chưa có mode online không xếp hạng trong UI, chống farm điểm/collusion, season, rating uncertainty, time-control riêng hoặc snapshot rating hai bên ở lúc bắt đầu. Rating trước/sau của bản ghi mới có ledger; rating lịch sử không được suy diễn.
8. Luật lặp/chiếu dai chưa triển khai đầy đủ mọi trường hợp WXF; H4 cần hiệu chỉnh và kiểm chứng sức mạnh qua bộ thế/đấu engine.
9. Các vấn đề xác thực đã nêu ở review trước vẫn cần xử lý riêng: nhánh đăng nhập/reset admin cố định, seed admin, SHA256 mật khẩu, quyền SignalR, antiforgery/rate limit. Bản này không phải chứng nhận Production.
10. Còn nullable warnings trong code gốc. Chưa thử UI trên trình duyệt thật/responsive hay chạy ứng dụng end-to-end với MySQL của nhóm.

## 9. Nguồn đối chiếu

- World Xiangqi Federation. *World Xiangqi Rules (2018).* https://www.wxf-xiangqi.org/images/wxf-rules/2018_World_XiangQi_Rules_English2018.pdf
- WXF. *2014 World Xiangqi Open Intro.* Ví dụ điều lệ điểm 2/1/0, không phải bảng trọng số quân: https://www.wxf-xiangqi.org/index.php?Itemid=377&catid=167&id=782:2014-world-xiangqi-open-intro&lang=en&option=com_content&view=article
- Xiangqi.com. *The Value of the Pieces in Xiangqi.* Giá trị tương đối/ước lượng, không phải mức bắt buộc WXF: https://www.xiangqi.com/articles/the-value-of-the-pieces-in-xiangqi-chinese-chess
- Xiangqi.com. *How to change your Xiangqi Ratings.* Rating trong một pool người chơi: https://www.xiangqi.com/help/rating
- Tseng, W.-J., Chen, J.-C., Wu, I.-C., & Wei, T. (2018). *Comparison Training for Computer Chinese Chess.* https://arxiv.org/abs/1801.07411


Kết quả chạy thực tế: xem `KET_QUA_KIEM_THU.md` và các log cùng thư mục.
