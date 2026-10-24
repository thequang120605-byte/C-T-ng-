# Backend contract và giới hạn tương thích

Bản Pixel 1.2 chạy AI luyện tập bằng engine nội bộ, không cần các API dưới đây. Route api/offline/games và ID ván âm được HybridApiService xử lý trong tiến trình, không phải endpoint cần tạo trên server. Contract này dành cho đăng nhập/online và các chức năng web.

## Authentication (JSON + Bearer)
POST /api/auth/login: {username,password}
POST /api/auth/register: {username,email,password}
Response thành công: {token,refreshToken,user:{id,username,email,playerId,role,elo,wins,losses,draws,puzzleScore}}
POST /api/auth/refresh-token: {refreshToken} → {token,refreshToken}
POST /api/auth/logout: {refreshToken} → HTTP 2xx
GET /api/auth/me (Bearer): trả user, có thể kèm isAuthenticated. Nếu không có quyền phải trả 401, không chuyển hướng HTML.
GET /api/health: HTTP 200. App chỉ dùng GET / làm fallback kiểm tra reachability khi health trả 404.

Cookie của MVC và Bearer của native là hai cơ chế riêng. Đừng xóa cookie scheme khi thêm JWT. Refresh token phải được lưu dạng hash, có hạn và được thu hồi trên server; app không tự phát token.

## Games
GET /api/games/{id} → GameResponseDto
POST /api/games?mode=PvE → GameResponseDto
GET /api/games/{id}/valid-moves?row=...&col=... → [{row,col,isCapture}]
POST /api/games/{id}/moves: {fromX,fromY,toX,toY} → GameResponseDto
POST /api/games/{id}/computer-move?difficulty=1..4&computerColor=black → GameResponseDto
POST /api/games/{id}/resign → GameResponseDto
GET /api/games/online/rooms → [{id,roomCode,creatorUsername,creatorElo,createdAt}]
POST /api/games/online → GameResponseDto
POST /api/games/{roomCode}/join → {game:GameResponseDto,opponentUsername} hoặc GameResponseDto

Các tọa độ là hàng 0..9, cột 0..8. Status: WaitingForPlayers, InProgress, RedWins, BlackWins, Draw. CurrentTurn/Color: Red hoặc Black. BoardPieces: [{row,col,type,color}], Type: King,Advisor,Elephant,Horse,Rook,Cannon,Pawn.
Evaluation gồm h1,h2,h3,h4, quy ước Đỏ trừ Đen. Không dùng điểm heuristic làm Elo.

Nên bổ sung redUserId/blackUserId dạng số (User.Id), redPlayerUsername/blackPlayerUsername và revision dạng số tăng đơn điệu sau mỗi thay đổi trạng thái (nước đi, undo, kết quả, timeout). App hỗ trợ redPlayerId/blackPlayerId dạng số hoặc public ID dạng chuỗi. Không trộn hai loại ID trong cùng contract.

Nếu server chưa có revision, app loại HTTP response đến sau một event đã áp dụng, nhưng không thể bảo đảm thứ tự tuyệt đối giữa các broadcast server. Muốn đảm bảo đầy đủ phải thêm revision trong cùng transaction cập nhật ván. Không suy luận revision chỉ từ số nước vì undo/resign/timeout cũng đổi trạng thái.

## SignalR
Hub /gameHub, phương thức JoinGame(string gameId), LeaveGame(string gameId).
Event GameUpdated nhận đúng một GameResponseDto giống HTTP (bao gồm revision nếu có).
Server phải kiểm tra quyền tham gia phòng và thực hiện nước đi theo tài khoản, không tin seat truyền trên UI. Nhóm không thay thế authorization.

## Lưu ý với source web cũ
Bản backend cũ chưa có API JWT login/register/refresh/logout. DTO ván cũ thiếu danh tính người chơi. Mobile không thể tự bổ sung các dữ liệu này bằng binding. Chưa xác nhận backend đang chạy của người dùng đã triển khai contract trên.

Các trang /Puzzle và /Course là MVC mở qua browser. Không chuyển Bearer token sang querystring hoặc cookie giả.
