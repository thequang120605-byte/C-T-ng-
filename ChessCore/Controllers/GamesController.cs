using ChessCore.DTOs;
using ChessCore.Engine;
using ChessCore.Enums;
using ChessCore.Models;
using ChessCore.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;
        private readonly IMoveService _moveService;

        public GamesController(
            IGameService gameService,
            IMoveService moveService
        )
        {
            _gameService = gameService;
            _moveService = moveService;
        }

        // =====================================================
        // POST /api/games
        // TẠO VÁN CỜ MỚI
        // =====================================================

        [HttpPost]
        public async Task<ActionResult<GameResponseDto>> CreateGame()
        {
            var game =
                await _gameService.CreateGameAsync();

            var board = new Board();

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

            var board =
                ReconstructBoard(
                    game.Moves,
                    game.CurrentTurn
                );

            var response =
                BuildGameResponse(
                    game,
                    board
                );

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

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "Ván cờ đã kết thúc."
                });
            }

            var board =
                ReconstructBoard(
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

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "Ván đấu đã kết thúc."
                });
            }

            var board =
                ReconstructBoard(
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

            var capturedPiece =
                board.GetPieceAt(
                    request.ToX,
                    request.ToY
                );

            bool isKingCaptured =
                capturedPiece != null &&
                capturedPiece.Type == PieceType.King;

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

            GameStatus newStatus;

            PieceColor nextTurn;

            if (isKingCaptured)
            {
                newStatus =
                    movingPiece.Color == PieceColor.Red
                        ? GameStatus.RedWins
                        : GameStatus.BlackWins;

                nextTurn =
                    movingPiece.Color;
            }
            else
            {
                newStatus =
                    GameStatus.InProgress;

                nextTurn =
                    game.CurrentTurn == PieceColor.Red
                        ? PieceColor.Black
                        : PieceColor.Red;
            }

            await _gameService.UpdateGameTurnAndStatusAsync(
                id,
                nextTurn,
                newStatus
            );

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
                ReconstructBoard(
                    updatedGame.Moves,
                    updatedGame.CurrentTurn
                );

            return Ok(
                BuildGameResponse(
                    updatedGame,
                    updatedBoard
                )
            );
        }

        // =====================================================
        // POST /api/games/{id}/computer-move
        // MÁY THỰC HIỆN NƯỚC ĐI
        // =====================================================

        [HttpPost("{id}/computer-move")]
        public async Task<ActionResult<GameResponseDto>> ComputerMove(
            int id,
            [FromQuery] int difficulty = 2
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
            // 3. MÁY CHỈ ĐÁNH QUÂN ĐEN
            // =================================================

            if (game.CurrentTurn != PieceColor.Black)
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
                ReconstructBoard(
                    game.Moves,
                    game.CurrentTurn
                );

            // =================================================
            // 6. TẠO COMPUTER PLAYER
            // =================================================

            var computer =
                new ComputerPlayer(
                    PieceColor.Black,
                    difficulty
                );

            // =================================================
            // 7. TÌM NƯỚC ĐI
            // =================================================

            var computerMove =
                computer.GetMove(board);

            if (computerMove == null)
            {
                return BadRequest(new
                {
                    message =
                        "Máy không tìm được nước đi hợp lệ."
                });
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

            var capturedPiece =
                board.GetPieceAt(
                    computerMove.To.Row,
                    computerMove.To.Col
                );

            bool isKingCaptured =
                capturedPiece != null &&
                capturedPiece.Type == PieceType.King;

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

            GameStatus newStatus;

            PieceColor nextTurn;

            if (isKingCaptured)
            {
                newStatus =
                    GameStatus.BlackWins;

                nextTurn =
                    PieceColor.Black;
            }
            else
            {
                newStatus =
                    GameStatus.InProgress;

                nextTurn =
                    PieceColor.Red;
            }

            // =================================================
            // 13. CẬP NHẬT GAME
            // =================================================

            await _gameService.UpdateGameTurnAndStatusAsync(
                id,
                nextTurn,
                newStatus
            );

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
                ReconstructBoard(
                    updatedGame.Moves,
                    updatedGame.CurrentTurn
                );

            // =================================================
            // 16. TRẢ KẾT QUẢ
            // =================================================

            return Ok(
                BuildGameResponse(
                    updatedGame,
                    updatedBoard
                )
            );
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

            if (game.CurrentTurn == PieceColor.Red)
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
                ReconstructBoard(
                    updatedGame.Moves,
                    updatedGame.CurrentTurn
                );

            // =================================================
            // 7. TRẢ RESPONSE GIỐNG CÁC API KHÁC
            // =================================================

            return Ok(
                BuildGameResponse(
                    updatedGame,
                    updatedBoard
                )
            );
        }

        // =====================================================
        // TÁI TẠO BÀN CỜ TỪ LỊCH SỬ NƯỚC ĐI
        // =====================================================

        private static Board ReconstructBoard(
            IEnumerable<Move> moves,
            PieceColor currentTurn
        )
        {
            var board =
                new Board();

            if (moves == null)
            {
                return board;
            }

            var sortedMoves =
                moves
                    .OrderBy(
                        move => move.MoveOrder
                    )
                    .ToList();

            foreach (var move in sortedMoves)
            {
                var piece =
                    board.GetPieceAt(
                        move.FromX,
                        move.FromY
                    );

                if (piece == null)
                {
                    Console.WriteLine(
                        $"Không tìm thấy quân tại " +
                        $"({move.FromX}, {move.FromY}) " +
                        $"ở MoveOrder = {move.MoveOrder}"
                    );

                    continue;
                }

                // =================================================
                // DI CHUYỂN QUÂN
                // =================================================

                board.Grid[
                    move.ToX,
                    move.ToY
                ] = piece;

                // =================================================
                // XÓA VỊ TRÍ CŨ
                // =================================================

                board.Grid[
                    move.FromX,
                    move.FromY
                ] = null;

                // =================================================
                // ĐỔI LƯỢT
                // =================================================

                board.SwitchTurn();
            }

            // =================================================
            // ĐỒNG BỘ CURRENT TURN VỚI DATABASE
            // =================================================

            while (board.CurrentTurn != currentTurn)
            {
                board.SwitchTurn();
            }

            return board;
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

            bool isCheck = false;

            if (
                game.Status ==
                GameStatus.InProgress
            )
            {
                isCheck =
                    MoveGenerator.CheckIfInCheck(
                        board,
                        game.CurrentTurn
                    );
            }

            // =================================================
            // TRẢ RESPONSE
            // =================================================

            return new GameResponseDto
            {
                Id =
                    game.Id,

                Status =
                    game.Status.ToString(),

                CurrentTurn =
                    game.CurrentTurn.ToString(),

                IsCheck =
                    isCheck,

                CreatedAt =
                    game.CreatedAt,

                LastMoveAt =
                    game.LastMoveAt,

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
    }
}