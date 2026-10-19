using Microsoft.AspNetCore.SignalR;
using ChessCore.DTOs;
using ChessCore.Engine;
using ChessCore.Enums;
using ChessCore.Models;
using ChessCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ChessCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessCore.Controllers
{
    public class InvitePlayerRequest
    {
        public string PlayerId { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly IMoveService _moveService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<Hubs.GameHub> _hubContext;
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _cache;

        public GamesController(
            IGameService gameService,
            IMoveService moveService,
            Microsoft.AspNetCore.SignalR.IHubContext<Hubs.GameHub> hubContext,
            AppDbContext dbContext,
            IMemoryCache cache
        )
        {
            _gameService = gameService;
            _moveService = moveService;
            _hubContext = hubContext;
            _dbContext = dbContext;
            _cache = cache;
        }

        // Danh sách phòng chờ online công khai
        [Authorize]
        [HttpGet("online/rooms")]
        public async Task<IActionResult> GetOpenRooms()
        {
            var openRooms = await _dbContext.Games
                .Where(g => g.Status == GameStatus.WaitingForPlayers && g.GameMode == "OnlinePublic" && g.RedPlayerId != null && g.BlackPlayerId == null)
                .OrderByDescending(g => g.CreatedAt)
                .Take(10)
                .Select(g => new
                {
                    id = g.Id,
                    roomCode = g.RoomCode,
                    createdAt = g.CreatedAt,
                    creatorUsername = _dbContext.Users.Where(u => u.Id == g.RedPlayerId).Select(u => u.Username).FirstOrDefault() ?? "Người chơi",
                    creatorElo = _dbContext.Users.Where(u => u.Id == g.RedPlayerId).Select(u => u.Elo).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(openRooms);
        }

        // Danh sách các trận đang đấu trực tiếp (Dành cho Khán giả / Spectator)
        [HttpGet("online/live")]
        public async Task<IActionResult> GetLiveMatches()
        {
            var liveMatches = await _dbContext.Games
                .Where(g => g.Status == GameStatus.InProgress && g.RedPlayerId != null && g.BlackPlayerId != null)
                .OrderByDescending(g => g.LastMoveAt ?? g.CreatedAt)
                .Take(10)
                .Select(g => new
                {
                    id = g.Id,
                    roomCode = g.RoomCode,
                    createdAt = g.CreatedAt,
                    redPlayerName = _dbContext.Users.Where(u => u.Id == g.RedPlayerId).Select(u => u.Username).FirstOrDefault() ?? "Quân Đỏ",
                    redElo = _dbContext.Users.Where(u => u.Id == g.RedPlayerId).Select(u => u.Elo).FirstOrDefault(),
                    blackPlayerName = _dbContext.Users.Where(u => u.Id == g.BlackPlayerId).Select(u => u.Username).FirstOrDefault() ?? "Quân Đen",
                    blackElo = _dbContext.Users.Where(u => u.Id == g.BlackPlayerId).Select(u => u.Elo).FirstOrDefault(),
                    moveCount = g.Moves.Count
                })
                .ToListAsync();

            return Ok(liveMatches);
        }

        // Online endpoints require a signed-in player. Generic game endpoints
        // remain available for local/offline play.
        [Authorize]
        [HttpPost("online")]
        public async Task<ActionResult<GameResponseDto>> CreateOnlineGame([FromQuery] bool isPublic = false)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            var game = await _gameService.CreateGameAsync(status: GameStatus.WaitingForPlayers);
            game.RedPlayerId = userId;
            game.GameMode = isPublic ? "OnlinePublic" : "OnlinePrivate";
            await _dbContext.SaveChangesAsync();

            if (isPublic)
            {
                var creatorUser = await _dbContext.Users.FindAsync(userId);
                await _hubContext.Clients.Group("lobby").SendAsync("RoomCreated", new
                {
                    id = game.Id,
                    roomCode = game.RoomCode,
                    createdAt = game.CreatedAt,
                    creatorUsername = creatorUser?.Username ?? "Người chơi",
                    creatorElo = creatorUser?.Elo ?? 1200
                });
            }

            var board = GameRules.RebuildBoard(game.InitialFen, null, game.CurrentTurn);
            var response = BuildGameResponse(game, board);
            return CreatedAtAction(nameof(GetGameById), new { id = game.Id }, response);
        }

        [Authorize]
        [HttpPost("{roomCode}/join")]
        public async Task<ActionResult<GameResponseDto>> JoinOnlineGame(string roomCode)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(roomCode))
                return BadRequest(new { message = "Vui lòng nhập mã bàn." });

            var normalizedRoomCode = roomCode.Trim().ToUpperInvariant();
            var gameId = await _dbContext.Games
                .Where(game => game.RoomCode == normalizedRoomCode)
                .Select(game => (int?)game.Id)
                .FirstOrDefaultAsync();
            if (!gameId.HasValue)
                return NotFound(new { message = "Không tìm thấy bàn với mã này." });

            var game = await _gameService.GetGameByIdAsync(gameId.Value);
            if (game == null) return NotFound(new { message = "Không tìm thấy bàn cờ." });
            if (game.RedPlayerId == userId)
                return BadRequest(new { message = "Bạn đã là người tạo bàn này." });
            if (game.BlackPlayerId.HasValue && game.BlackPlayerId != userId)
                return Conflict(new { message = "Bàn này đã được dành cho người chơi khác." });
            if (game.Status != GameStatus.WaitingForPlayers && game.BlackPlayerId != userId)
                return Conflict(new { message = "Bàn này không còn chờ người chơi." });

            game.BlackPlayerId = userId;
            game.Status = GameStatus.InProgress;
            game.LastMoveAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            // Ràng buộc phòng kín khi đủ 2 người: Phát tín hiệu ẩn/khóa phòng khỏi sảnh chờ công khai
            await _hubContext.Clients.Group("lobby").SendAsync("RoomLocked", new
            {
                id = game.Id,
                roomCode = game.RoomCode
            });

            var opponent = game.RedPlayerId.HasValue
                ? await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == game.RedPlayerId.Value)
                : null;
            var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
            var response = BuildGameResponse(game, board);
            await _hubContext.Clients.Group(game.Id.ToString()).SendAsync("GameUpdated", response);
            return Ok(new { game = response, opponentUsername = opponent?.Username });
        }

        [Authorize]
        [HttpPost("{id}/invite")]
        public async Task<IActionResult> InvitePlayer(int id, [FromBody] InvitePlayerRequest request)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            if (string.IsNullOrWhiteSpace(request?.PlayerId))
                return BadRequest(new { message = "Vui lòng nhập ID người chơi." });

            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null) return NotFound(new { message = "Không tìm thấy bàn cờ." });
            if (game.RedPlayerId != userId)
                return Forbid();
            if (game.Status != GameStatus.WaitingForPlayers)
                return Conflict(new { message = "Bàn này đã bắt đầu hoặc đã kết thúc." });

            var opponent = await _dbContext.Users
                .FirstOrDefaultAsync(user => user.PlayerId == request.PlayerId.Trim());
            if (opponent == null)
                return NotFound(new { message = "Không tìm thấy người chơi với ID này." });
            if (opponent.Id == userId)
                return BadRequest(new { message = "Bạn không thể thêm chính mình vào bàn." });
            if (game.BlackPlayerId.HasValue && game.BlackPlayerId != opponent.Id)
                return Conflict(new { message = "Bàn này đã có đối thủ khác." });

            game.BlackPlayerId = opponent.Id;
            game.Status = GameStatus.InProgress;
            game.LastMoveAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
            var response = BuildGameResponse(game, board);
            await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", response);
            return Ok(new { playerId = opponent.PlayerId, username = opponent.Username, game = response });
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        private bool CanAccessOnlineGame(Game game, bool readOnly = false)
        {
            if (!game.RedPlayerId.HasValue && !game.BlackPlayerId.HasValue) return true;
            if (readOnly) return true; // Cho phép khán giả xem ván cờ (Spectator Mode)
            return TryGetCurrentUserId(out var userId) &&
                (game.RedPlayerId == userId || game.BlackPlayerId == userId);
        }

        // =====================================================
        // POST /api/games
        // TẠO VÁN CỜ MỚI
        // =====================================================

        [HttpPost]
        public async Task<ActionResult<GameResponseDto>> CreateGame([FromQuery] string initialFen = null)
        {
            var game =
                await _gameService.CreateGameAsync(initialFen);

            if (TryGetCurrentUserId(out int userId))
            {
                game.RedPlayerId = userId;
                await _dbContext.SaveChangesAsync();
            }

            var board = GameRules.RebuildBoard(game.InitialFen, null, game.CurrentTurn);

            var response =
                BuildGameResponse(
                    game,
                    board
                );

            return CreatedAtAction(
                nameof(GetGameById),
                new { id = game.Id },
                response
            );
        }

        // =====================================================
        // GET /api/games/{id}
        // LẤY THÔNG TIN VÁN CỜ
        // =====================================================

        [HttpGet("{id}")]
        public async Task<ActionResult<GameResponseDto>> GetGameById(
            int id
        )
        {
            var game =
                await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy ván cờ với ID {id}"
                });
            }

            if (!CanAccessOnlineGame(game, readOnly: true)) return Unauthorized();

            var board =
                GameRules.RebuildBoard(
                    game.InitialFen,
                    game.Moves,
                    game.CurrentTurn
                );

            var response =
                BuildGameResponse(
                    game,
                    board
                );

            await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", response);
            return Ok(response);
        }

        // =====================================================
        // GET /api/games/{id}/valid-moves
        // LẤY CÁC NƯỚC ĐI HỢP LỆ
        // =====================================================

        [HttpGet("{id}/valid-moves")]
        public async Task<ActionResult<List<ValidMoveDto>>> GetValidMoves(
            int id,
            [FromQuery] int row,
            [FromQuery] int col
        )
        {
            var game =
                await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy ván cờ với ID {id}"
                });
            }

            if (!CanAccessOnlineGame(game)) return Unauthorized();

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "Ván cờ đã kết thúc."
                });
            }

            var board =
                GameRules.RebuildBoard(
                    game.InitialFen,
                    game.Moves,
                    game.CurrentTurn
                );

            var piece =
                board.GetPieceAt(
                    row,
                    col
                );

            if (piece == null)
            {
                return BadRequest(new
                {
                    message =
                        "Không có quân cờ tại vị trí này."
                });
            }

            if (piece.Color != game.CurrentTurn)
            {
                return BadRequest(new
                {
                    message =
                        "Chưa tới lượt của quân này."
                });
            }

            var from =
                new Position(
                    row,
                    col
                );

            var validMoves =
                MoveGenerator.GetValidMoves(
                    board,
                    from
                );

            var result =
                validMoves
                    .Select(move =>
                    {
                        var targetPiece =
                            board.GetPieceAt(
                                move.To.Row,
                                move.To.Col
                            );

                        return new ValidMoveDto
                        {
                            Row = move.To.Row,
                            Col = move.To.Col,
                            IsCapture =
                                targetPiece != null
                        };
                    })
                    .ToList();

            return Ok(result);
        }

        // =====================================================
        // POST /api/games/{id}/moves
        // NGƯỜI CHƠI THỰC HIỆN NƯỚC ĐI
        // =====================================================

        [HttpPost("{id}/moves")]
        public async Task<ActionResult<GameResponseDto>> MakeMove(
            int id,
            [FromBody] MakeMoveRequestDto request
        )
        {
            var game =
                await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy ván cờ với ID {id}"
                });
            }

            if (!CanAccessOnlineGame(game)) return Unauthorized();

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "Ván đấu đã kết thúc."
                });
            }

            // Kiểm tra giới hạn thời gian 3 phút (cộng 2s dung sai độ trễ mạng)
            var lastActionTime = game.LastMoveAt ?? game.CreatedAt;
            if ((game.RedPlayerId.HasValue || game.BlackPlayerId.HasValue) &&
                DateTime.UtcNow - lastActionTime > TimeSpan.FromSeconds(182))
            {
                game.Status = game.CurrentTurn == PieceColor.Red
                    ? GameStatus.BlackWins
                    : GameStatus.RedWins;

                _dbContext.Games.Update(game);
                await _dbContext.SaveChangesAsync();

                var currentBoard = GameRules.RebuildBoard(
                    game.InitialFen,
                    game.Moves,
                    game.CurrentTurn
                );
                var timeoutResponse = BuildGameResponse(game, currentBoard);
                await _hubContext.Clients.Group(game.Id.ToString()).SendAsync("GameUpdated", timeoutResponse);

                return BadRequest(new
                {
                    message = "Đã hết thời gian 3 phút! Bạn đã bị xử thua vì rụng kim.",
                    game = timeoutResponse
                });
            }

            var board =
                GameRules.RebuildBoard(
                    game.InitialFen,
                    game.Moves,
                    game.CurrentTurn
                );

            var movingPiece =
                board.GetPieceAt(
                    request.FromX,
                    request.FromY
                );

            if (movingPiece == null)
            {
                return BadRequest(new
                {
                    message =
                        "Không có quân cờ ở vị trí xuất phát."
                });
            }

            if (movingPiece.Color != game.CurrentTurn)
            {
                return BadRequest(new
                {
                    message =
                        $"Chưa tới lượt đi của quân {movingPiece.Color}."
                });
            }

            var from =
                new Position(
                    request.FromX,
                    request.FromY
                );

            var to =
                new Position(
                    request.ToX,
                    request.ToY
                );

            var validMoves =
                MoveGenerator.GetValidMoves(
                    board,
                    from
                );

            bool isValid =
                validMoves.Any(
                    move =>
                        move.To.Row == request.ToX &&
                        move.To.Col == request.ToY
                );

            if (!isValid)
            {
                return BadRequest(new
                {
                    message =
                        "Nước đi không hợp lệ theo luật Cờ Tướng."
                });
            }

            // =================================================
            // LƯU NƯỚC ĐI
            // =================================================

            await _moveService.RecordMoveAsync(
                id,
                request.FromX,
                request.FromY,
                request.ToX,
                request.ToY,
                movingPiece.Type,
                movingPiece.Color
            );

            // =================================================
            // CẬP NHẬT TRẠNG THÁI
            // =================================================

            board.Grid[request.ToX, request.ToY] = movingPiece;
            board.Grid[request.FromX, request.FromY] = null;
            PieceColor nextTurn = GameRules.Opponent(game.CurrentTurn);
            board.SetCurrentTurn(nextTurn);
            GameStatus newStatus = GameRules.StatusAfterMove(board);

            // Kiểm tra luật lặp lại thế cờ và luật Chiếu dai
            var repCheck = GameRules.CheckRepetitionRule(game.InitialFen, game.Moves);
            if (repCheck.HasViolation)
            {
                newStatus = repCheck.ResultStatus;
            }

            await _gameService.UpdateGameTurnAndStatusAsync(
                id,
                nextTurn,
                newStatus
            );

            if (newStatus != GameStatus.InProgress)
            {
                await UpdateUserStatsOnGameEndAsync(game, newStatus);
            }

            // =================================================
            // LẤY GAME MỚI NHẤT
            // =================================================

            var updatedGame =
                await _gameService.GetGameByIdAsync(id);

            if (updatedGame == null)
            {
                return NotFound(new
                {
                    message =
                        "Không thể tải lại ván cờ."
                });
            }

            var updatedBoard =
                GameRules.RebuildBoard(
                    updatedGame.InitialFen,
                    updatedGame.Moves,
                    updatedGame.CurrentTurn
                );

            var response = BuildGameResponse(updatedGame, updatedBoard);
            await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", response);
            return Ok(response);
        }

        // =====================================================
        // POST /api/games/{id}/computer-move
        // MÁY THỰC HIỆN NƯỚC ĐI
        // =====================================================

        [HttpPost("{id}/computer-move")]
        public async Task<ActionResult<GameResponseDto>> ComputerMove(
            int id,
            [FromQuery] int difficulty = 2,
            [FromQuery] string computerColor = "black"
        )
        {
            // =================================================
            // 1. LẤY GAME
            // =================================================

            var game =
                await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy ván cờ với ID {id}"
                });
            }

            if (!CanAccessOnlineGame(game)) return Unauthorized();

            // =================================================
            // 2. KIỂM TRA TRẠNG THÁI
            // =================================================

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "Ván cờ đã kết thúc."
                });
            }

            // =================================================
            // 3. KIỂM TRA LƯỢT
            // =================================================

            PieceColor engineColor = computerColor.ToLower() == "red" ? PieceColor.Red : PieceColor.Black;

            if (game.CurrentTurn != engineColor)
            {
                return BadRequest(new
                {
                    message =
                        "Hiện tại chưa tới lượt của máy."
                });
            }

            // =================================================
            // 4. GIỚI HẠN ĐỘ KHÓ
            // =================================================

            if (difficulty < 1)
            {
                difficulty = 1;
            }

            if (difficulty > 4)
            {
                difficulty = 4;
            }

            // =================================================
            // 5. TÁI TẠO BÀN CỜ
            // =================================================

            var board =
                GameRules.RebuildBoard(
                    game.InitialFen,
                    game.Moves,
                    game.CurrentTurn
                );

            // =================================================
            // 6. TẠO COMPUTER PLAYER
            // =================================================

            var computer =
                new ComputerPlayer(
                    engineColor,
                    difficulty
                );

            // =================================================
            // 7. TÌM NƯỚC ĐI
            // =================================================

            var computerMove =
                computer.GetMove(board);

            if (computerMove == null)
            {
                var terminalStatus = engineColor == PieceColor.Red ? GameStatus.BlackWins : GameStatus.RedWins;
                await _gameService.UpdateGameTurnAndStatusAsync(id, engineColor, terminalStatus);
                game.Status = terminalStatus;
                var terminalResponse = BuildGameResponse(game, board);
                await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", terminalResponse);
                return Ok(terminalResponse);
            }

            // =================================================
            // 8. LẤY QUÂN CỜ
            // =================================================

            var movingPiece =
                board.GetPieceAt(
                    computerMove.From.Row,
                    computerMove.From.Col
                );

            if (movingPiece == null)
            {
                return BadRequest(new
                {
                    message =
                        "Không tìm thấy quân cờ của máy."
                });
            }

            // =================================================
            // 9. KIỂM TRA QUÂN BỊ ĂN
            // =================================================

            // =================================================
            // 10. LƯU NƯỚC ĐI
            // =================================================

            await _moveService.RecordMoveAsync(
                id,
                computerMove.From.Row,
                computerMove.From.Col,
                computerMove.To.Row,
                computerMove.To.Col,
                movingPiece.Type,
                movingPiece.Color
            );

            // =================================================
            // 11. CẬP NHẬT BÀN CỜ MEMORY
            // =================================================

            board.Grid[
                computerMove.To.Row,
                computerMove.To.Col
            ] = movingPiece;

            board.Grid[
                computerMove.From.Row,
                computerMove.From.Col
            ] = null;

            // =================================================
            // 12. XÁC ĐỊNH TRẠNG THÁI
            // =================================================

            PieceColor nextTurn = GameRules.Opponent(engineColor);
            board.SetCurrentTurn(nextTurn);
            GameStatus newStatus = GameRules.StatusAfterMove(board);

            // Kiểm tra luật lặp lại thế cờ và luật Chiếu dai
            var repCheck = GameRules.CheckRepetitionRule(game.InitialFen, game.Moves);
            if (repCheck.HasViolation)
            {
                newStatus = repCheck.ResultStatus;
            }

            await _gameService.UpdateGameTurnAndStatusAsync(
                id,
                nextTurn,
                newStatus
            );

            if (newStatus != GameStatus.InProgress)
            {
                await UpdateUserStatsOnGameEndAsync(game, newStatus);
            }

            // =================================================
            // 14. LẤY GAME MỚI NHẤT
            // =================================================

            var updatedGame =
                await _gameService.GetGameByIdAsync(id);

            if (updatedGame == null)
            {
                return NotFound(new
                {
                    message =
                        "Không thể tải lại ván cờ sau khi máy đi."
                });
            }

            // =================================================
            // 15. TÁI TẠO BÀN CỜ
            // =================================================

            var updatedBoard =
                GameRules.RebuildBoard(
                    updatedGame.InitialFen,
                    updatedGame.Moves,
                    updatedGame.CurrentTurn
                );

            // =================================================
            // 16. TRẢ KẾT QUẢ
            // =================================================

            var response = BuildGameResponse(updatedGame, updatedBoard);
            await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", response);
            return Ok(response);
        }

        // =====================================================
        // POST /api/games/{id}/undo
        // ĐI LẠI (UNDO)
        // =====================================================

        [HttpPost("{id}/undo")]
        public async Task<ActionResult<GameResponseDto>> Undo(
            int id,
            [FromQuery] bool isPvE = true
        )
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { message = "Không tìm thấy ván cờ." });
            }

            if (!CanAccessOnlineGame(game)) return Unauthorized();

            // Chơi online giữa 2 người chơi khác nhau không cho phép đi lại
            if (game.RedPlayerId != null && game.BlackPlayerId != null && game.RedPlayerId != game.BlackPlayerId)
            {
                return BadRequest(new { message = "Chế độ chơi Online không cho phép đi lại nước cờ." });
            }

            var success = await _gameService.UndoMoveAsync(id, isPvE);
            if (!success)
            {
                return BadRequest(new { message = "Không thể đi lại (chưa đủ nước đi)." });
            }

            // Refetch after undo
            game = await _gameService.GetGameByIdAsync(id);

            var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);

            var response = BuildGameResponse(game, board);
            await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", response);
            return Ok(response);
        }

        // =====================================================
        // POST /api/games/{id}/resign
        // ĐẦU HÀNG
        // =====================================================

        [HttpPost("{id}/resign")]
        public async Task<ActionResult<GameResponseDto>> Resign(
            int id
        )
        {
            // =================================================
            // 1. LẤY GAME
            // =================================================

            var game =
                await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message =
                        $"Không tìm thấy ván cờ với ID {id}"
                });
            }

            if (!CanAccessOnlineGame(game)) return Unauthorized();

            // =================================================
            // 2. KIỂM TRA GAME ĐÃ KẾT THÚC CHƯA
            // =================================================

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "Ván cờ đã kết thúc."
                });
            }

            // =================================================
            // 3. XÁC ĐỊNH NGƯỜI ĐẦU HÀNG
            //
            // Red đầu hàng  -> Black thắng
            // Black đầu hàng -> Red thắng
            // =================================================

            GameStatus newStatus;

            if (game.RedPlayerId.HasValue || game.BlackPlayerId.HasValue)
            {
                if (!TryGetCurrentUserId(out var resigningPlayerId)) return Unauthorized();
                newStatus = game.RedPlayerId == resigningPlayerId
                    ? GameStatus.BlackWins
                    : GameStatus.RedWins;
            }
            else if (game.CurrentTurn == PieceColor.Red)
            {
                newStatus =
                    GameStatus.BlackWins;
            }
            else
            {
                newStatus =
                    GameStatus.RedWins;
            }

            // =================================================
            // 4. CẬP NHẬT GAME VÀO DATABASE
            //
            // Giữ CurrentTurn hiện tại vì ván đã kết thúc.
            // Status mới sẽ quyết định người thắng.
            // =================================================

            await _gameService.UpdateGameTurnAndStatusAsync(
                id,
                game.CurrentTurn,
                newStatus
            );

            // Cập nhật điểm Elo và thống kê cho người chơi nếu kết thúc ván
            await UpdateUserStatsOnGameEndAsync(game, newStatus);

            // =================================================
            // 5. LẤY GAME SAU KHI UPDATE
            // =================================================

            var updatedGame =
                await _gameService.GetGameByIdAsync(id);

            if (updatedGame == null)
            {
                return NotFound(new
                {
                    message =
                        "Không thể tải lại ván cờ sau khi đầu hàng."
                });
            }

            // =================================================
            // 6. TÁI TẠO BÀN CỜ
            // =================================================

            var updatedBoard =
                GameRules.RebuildBoard(
                    updatedGame.InitialFen,
                    updatedGame.Moves,
                    updatedGame.CurrentTurn
                );

            // =================================================
            // 7. TRẢ RESPONSE GIỐNG CÁC API KHÁC
            // =================================================

            var response = BuildGameResponse(updatedGame, updatedBoard);
            await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", response);
            return Ok(response);
        }

        // =====================================================
        // POST /api/games/{id}/draw/offer  &  /api/games/{id}/offer-draw  &  /api/match/{id}/offer-draw
        // XIN HÒA CỜ (OFFER DRAW)
        // =====================================================
        [HttpPost("{id}/draw/offer")]
        [HttpPost("{id}/offer-draw")]
        [HttpPost("/api/match/{id}/offer-draw")]
        public async Task<IActionResult> OfferDraw(int id)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { message = "Không tìm thấy ván cờ." });
            }

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new { message = "Ván đấu không trong trạng thái đang diễn ra." });
            }

            int currentUserId = 0;
            bool isOnline = game.RedPlayerId.HasValue || game.BlackPlayerId.HasValue;
            if (isOnline)
            {
                if (!TryGetCurrentUserId(out currentUserId))
                {
                    return Unauthorized();
                }

                if (game.RedPlayerId != currentUserId && game.BlackPlayerId != currentUserId)
                {
                    return Forbid();
                }
            }

            // --- NGHIỆP VỤ CHỐNG SPAM ---
            string countKey = $"DrawOfferCount:{id}:{currentUserId}";
            string cooldownKey = $"DrawOfferCooldown:{id}:{currentUserId}";
            string pendingKey = $"DrawOfferPending:{id}";

            int offerCount = _cache.Get<int>(countKey);
            if (offerCount >= 2)
            {
                return BadRequest(new { message = "Bạn đã dùng hết số lần xin hòa trong ván cờ này (tối đa 2 lần)." });
            }

            if (_cache.TryGetValue(cooldownKey, out _))
            {
                return BadRequest(new { message = "Bạn vừa gửi đề nghị hòa. Vui lòng đợi trong giây lát trước khi thử lại." });
            }

            if (_cache.TryGetValue(pendingKey, out _))
            {
                return BadRequest(new { message = "Đang có lời đề nghị hòa chờ phản hồi." });
            }

            // Xử lý khi Chơi với Máy (VS COMPUTER)
            if (!isOnline)
            {
                var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
                int eval = BoardEvaluator.Evaluate(board, PieceColor.Black);
                bool computerAgrees = Math.Abs(eval) <= 150 || (game.Moves != null && game.Moves.Count >= 40);

                if (computerAgrees)
                {
                    game.Status = GameStatus.Draw;
                    _dbContext.Games.Update(game);
                    await _dbContext.SaveChangesAsync();

                    await UpdateUserStatsOnGameEndAsync(game, GameStatus.Draw);

                    var drawResponse = BuildGameResponse(game, board);
                    await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", drawResponse);
                    await _hubContext.Clients.Group(id.ToString()).SendAsync("MatchEnded_Draw", new { gameId = id, message = "Máy đã đồng ý hòa ván cờ!" });

                    return Ok(new { accepted = true, message = "Máy đã đồng ý hòa ván cờ!", game = drawResponse });
                }
                else
                {
                    _cache.Set(cooldownKey, true, TimeSpan.FromSeconds(30));
                    _cache.Set(countKey, offerCount + 1, TimeSpan.FromHours(2));
                    return Ok(new { accepted = false, message = "Máy từ chối lời xin hòa! Thế cờ vẫn đang tiếp diễn." });
                }
            }

            // Xử lý khi Chơi Online:
            _cache.Set(pendingKey, currentUserId, TimeSpan.FromSeconds(15));
            _cache.Set(countKey, offerCount + 1, TimeSpan.FromHours(2));
            _cache.Set(cooldownKey, true, TimeSpan.FromSeconds(30));

            string offeringColor = game.RedPlayerId == currentUserId ? "Red" : "Black";
            var offerPayload = new
            {
                gameId = id,
                fromUserId = currentUserId,
                fromColor = offeringColor,
                timeoutSeconds = 15,
                message = "Đối thủ đang đề nghị hòa cờ. Bạn có đồng ý không?"
            };

            await _hubContext.Clients.Group(id.ToString()).SendAsync("DrawOffered", offerPayload);
            return Ok(new { success = true, message = "Đã gửi đề nghị hòa tới đối thủ (chờ trong 15s).", timeoutSeconds = 15 });
        }

        // =====================================================
        // POST /api/games/{id}/draw/respond  &  /api/games/{id}/respond-draw  &  /api/match/{id}/respond-draw
        // PHẢN HỒI XIN HÒA (RESPOND DRAW)
        // =====================================================
        [HttpPost("{id}/draw/respond")]
        [HttpPost("{id}/respond-draw")]
        [HttpPost("/api/match/{id}/respond-draw")]
        public async Task<IActionResult> RespondDraw(int id, [FromBody] RespondDrawRequestDto request)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { message = "Không tìm thấy ván cờ." });
            }

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new { message = "Ván đấu không trong trạng thái đang diễn ra." });
            }

            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Unauthorized();
            }

            if (game.RedPlayerId != currentUserId && game.BlackPlayerId != currentUserId)
            {
                return Forbid();
            }

            string pendingKey = $"DrawOfferPending:{id}";
            if (!_cache.TryGetValue(pendingKey, out int offeringUserId))
            {
                return BadRequest(new { message = "Lời đề nghị xin hòa đã hết hạn hoặc không tồn tại." });
            }

            if (offeringUserId == currentUserId)
            {
                return BadRequest(new { message = "Bạn không thể tự phản hồi đề nghị của chính mình." });
            }

            _cache.Remove(pendingKey);

            bool isAccepted = request != null && (request.Accepted || request.IsAccepted);
            if (isAccepted)
            {
                game.Status = GameStatus.Draw;
                _dbContext.Games.Update(game);
                await _dbContext.SaveChangesAsync();

                await UpdateUserStatsOnGameEndAsync(game, GameStatus.Draw);

                var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
                var drawResponse = BuildGameResponse(game, board);

                await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", drawResponse);
                await _hubContext.Clients.Group(id.ToString()).SendAsync("MatchEnded_Draw", new { gameId = id, message = "Hai bên đã đồng ý hòa ván cờ!" });

                return Ok(new { accepted = true, message = "Đã đồng ý hòa ván cờ thành công.", game = drawResponse });
            }
            else
            {
                await _hubContext.Clients.Group(id.ToString()).SendAsync("DrawDeclined", new { gameId = id, declinedBy = currentUserId, message = "Đối thủ đã từ chối lời xin hòa." });
                return Ok(new { accepted = false, message = "Đã từ chối lời xin hòa." });
            }
        }

        // =====================================================
        // GET /api/games/{id}/fen
        // LẤY FEN CỦA VÁN CỜ HIỆN TẠI
        // =====================================================
        [HttpGet("{id}/fen")]
        public async Task<ActionResult<string>> GetFen(int id)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { message = $"Không tìm thấy ván cờ với ID {id}" });
            }

            if (!CanAccessOnlineGame(game, readOnly: true)) return Unauthorized();

            var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
            var fen = board.ToFen();
            return Ok(new { fen = fen });
        }

        // =====================================================
        // GET /api/games/{id}/eval
        // LẤY ĐIỂM ĐÁNH GIÁ (EVALUATION BAR)
        // =====================================================
        [HttpGet("{id}/eval")]
        public async Task<ActionResult<int>> GetEval(int id)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null) return NotFound();
            if (!CanAccessOnlineGame(game, readOnly: true)) return Unauthorized();

            var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
            
            // Lấy điểm theo góc nhìn của quân Đỏ (Dương = Đỏ ưu thế, Âm = Đen ưu thế)
            var engine = new MinimaxEngine(3); // Depth 3 for analysis
            var eval = engine.EvaluatePosition(board, PieceColor.Red);
            
            return Ok(new { eval = eval });
        }

        // =====================================================
        // GET /api/games/{id}/hint
        // GỢI Ý NƯỚC ĐI TỐI ƯU CHO LƯỢT HIỆN TẠI (AI HINT)
        // =====================================================
        [HttpGet("{id}/hint")]
        public async Task<IActionResult> GetHint(int id, [FromQuery] string mode = "full")
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null) return NotFound(new { message = "Không tìm thấy ván cờ." });
            if (!CanAccessOnlineGame(game)) return Unauthorized();

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new { message = "Ván cờ đã kết thúc, không thể gợi ý." });
            }

            var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
            var engine = new MinimaxEngine(3);
            var bestMove = engine.FindBestMove(board, game.CurrentTurn);

            if (bestMove == null)
            {
                return BadRequest(new { message = "Không tìm thấy nước đi gợi ý phù hợp." });
            }

            int? currentElo = null;
            if (TryGetCurrentUserId(out int userId))
            {
                var user = await _dbContext.Users.FindAsync(userId);
                if (user != null)
                {
                    currentElo = user.Elo;
                }
            }

            var movingPiece = board.GetPieceAt(bestMove.From.Row, bestMove.From.Col);

            return Ok(new
            {
                fromX = bestMove.From.Row,
                fromY = bestMove.From.Col,
                toX = bestMove.To.Row,
                toY = bestMove.To.Col,
                pieceType = movingPiece?.Type.ToString(),
                pieceColor = movingPiece?.Color.ToString(),
                currentTurn = game.CurrentTurn.ToString(),
                pointsDeducted = 0,
                remainingElo = currentElo
            });
        }

        // =====================================================
        // BUILD RESPONSE
        // =====================================================

        private static GameResponseDto BuildGameResponse(
            Game game,
            Board board
        )
        {
            var pieces =
                new List<PieceDto>();

            // =================================================
            // LẤY TOÀN BỘ QUÂN CỜ TRÊN BÀN
            // =================================================

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    var piece =
                        board.GetPieceAt(
                            row,
                            col
                        );

                    if (piece != null)
                    {
                        pieces.Add(
                            new PieceDto
                            {
                                Row = row,
                                Col = col,
                                Type =
                                    piece.Type.ToString(),
                                Color =
                                    piece.Color.ToString()
                            }
                        );
                    }
                }
            }

            // =================================================
            // KIỂM TRA CHIẾU TƯỚNG
            // =================================================

            bool isCheck = MoveGenerator.CheckIfInCheck(board, game.CurrentTurn);
            bool noMoves = !GameRules.HasLegalMove(board, game.CurrentTurn);
            bool decisive = game.Status == GameStatus.RedWins || game.Status == GameStatus.BlackWins;
            string endReason = decisive ? (noMoves ? (isCheck ? "Checkmate" : "NoLegalMoves") : "GameEnded") : "";
            var redEvaluation = BoardEvaluator.GetSideScores(board, PieceColor.Red);
            var blackEvaluation = BoardEvaluator.GetSideScores(board, PieceColor.Black);
            int redScore = redEvaluation.Total - blackEvaluation.Total;
            LastMoveEvaluationDto? lastMoveEval = null;
            var orderedMoves = game.Moves?.OrderBy(m => m.MoveOrder).ToList() ?? new List<Move>();
            if (orderedMoves.Count > 0)
            {
                var last = orderedMoves[^1];
                var before = GameRules.RebuildBoard(game.InitialFen, orderedMoves.Take(orderedMoves.Count - 1), last.PieceColor);
                var delta = BoardEvaluator.GetSideScores(board, last.PieceColor);
                var previous = BoardEvaluator.GetSideScores(before, last.PieceColor);
                var opponentBefore = BoardEvaluator.GetSideScores(before, GameRules.Opponent(last.PieceColor));
                var opponentAfter = BoardEvaluator.GetSideScores(board, GameRules.Opponent(last.PieceColor));
                lastMoveEval = new LastMoveEvaluationDto
                {
                    PieceName = last.PieceType.ToString(),
                    MoveDescription = $"({last.FromX},{last.FromY}) -> ({last.ToX},{last.ToY})",
                    DeltaH1 = (delta.H1 - opponentAfter.H1) - (previous.H1 - opponentBefore.H1),
                    DeltaH2 = (delta.H2 - opponentAfter.H2) - (previous.H2 - opponentBefore.H2),
                    DeltaH3 = (delta.H3 - opponentAfter.H3) - (previous.H3 - opponentBefore.H3),
                    DeltaH4 = (delta.H4 - opponentAfter.H4) - (previous.H4 - opponentBefore.H4)
                };
            }

            // =================================================
            // TRẢ RESPONSE
            // =================================================

            var lastAction = game.LastMoveAt ?? game.CreatedAt;
            var remainingSeconds = game.Status == GameStatus.InProgress
                ? Math.Max(0, 180 - (int)(DateTime.UtcNow - lastAction).TotalSeconds)
                : 0;
            var neuralScore = NeuralNetworkEvaluator.EvaluateNeural(board, PieceColor.Red);

            return new GameResponseDto
            {
                    Id =
                        game.Id,

                    RoomCode =
                        game.RoomCode,

                    Status =
                        game.Status.ToString(),

                    CurrentTurn =
                        game.CurrentTurn.ToString(),

                    IsCheck = isCheck,
                    HasNoLegalMoves = noMoves,
                    IsCheckmate = isCheck && noMoves,
                    EndReason = endReason,
                    InitialFen = game.InitialFen,
                    Evaluation = new HeuristicEvaluationDto
                    {
                        H1 = redEvaluation.H1 - blackEvaluation.H1,
                        H2 = redEvaluation.H2 - blackEvaluation.H2,
                        H3 = redEvaluation.H3 - blackEvaluation.H3,
                        H4 = redEvaluation.H4 - blackEvaluation.H4
                    },
                    RedScore = redScore,
                    BlackScore = -redScore,
                    NeuralScore = neuralScore,
                    LastMoveEval = lastMoveEval,

                CreatedAt =
                    game.CreatedAt,

                LastMoveAt =
                    game.LastMoveAt,

                RemainingSeconds =
                    remainingSeconds,

                BoardPieces =
                    pieces,

                Moves =
                    game.Moves?
                        .OrderBy(
                            move =>
                                move.MoveOrder
                        )
                        .Select(
                            move =>
                                new MoveHistoryDto
                                {
                                    Id =
                                        move.Id,

                                    FromX =
                                        move.FromX,

                                    FromY =
                                        move.FromY,

                                    ToX =
                                        move.ToX,

                                    ToY =
                                        move.ToY,

                                    PieceType =
                                        move.PieceType
                                            .ToString(),

                                    PieceColor =
                                        move.PieceColor
                                            .ToString(),

                                    MoveOrder =
                                        move.MoveOrder,

                                    MovedAt =
                                        move.MovedAt
                                }
                        )
                        .ToList()
                    ?? new List<MoveHistoryDto>()
            };
        }

        private async Task UpdateUserStatsOnGameEndAsync(Game game, GameStatus status)
        {
            if (status == GameStatus.InProgress || status == GameStatus.WaitingForPlayers) return;

            if (game.RedPlayerId.HasValue)
            {
                var redUser = await _dbContext.Users.FindAsync(game.RedPlayerId.Value);
                if (redUser != null)
                {
                    redUser.GamesPlayed++;
                    if (status == GameStatus.RedWins)
                    {
                        redUser.Wins++;
                        redUser.Elo += 15;
                    }
                    else if (status == GameStatus.BlackWins)
                    {
                        redUser.Losses++;
                        redUser.Elo = Math.Max(400, redUser.Elo - 10);
                    }
                    else if (status == GameStatus.Draw)
                    {
                        redUser.Draws++;
                        redUser.Elo += 2;
                    }
                }
            }

            if (game.BlackPlayerId.HasValue)
            {
                var blackUser = await _dbContext.Users.FindAsync(game.BlackPlayerId.Value);
                if (blackUser != null)
                {
                    blackUser.GamesPlayed++;
                    if (status == GameStatus.BlackWins)
                    {
                        blackUser.Wins++;
                        blackUser.Elo += 15;
                    }
                    else if (status == GameStatus.RedWins)
                    {
                        blackUser.Losses++;
                        blackUser.Elo = Math.Max(400, blackUser.Elo - 10);
                    }
                    else if (status == GameStatus.Draw)
                    {
                        blackUser.Draws++;
                        blackUser.Elo += 2;
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
