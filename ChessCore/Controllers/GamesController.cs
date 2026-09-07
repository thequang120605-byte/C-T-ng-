using ChessCore.DTOs;
using ChessCore.Engine;
using ChessCore.Enums;
using ChessCore.Models;
using ChessCore.Services;
using Microsoft.AspNetCore.Mvc;
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

        public GamesController(IGameService gameService, IMoveService moveService)
        {
            _gameService = gameService;
            _moveService = moveService;
        }

        /// <summary>
        /// POST /api/games: Khởi tạo một trận đấu mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<GameResponseDto>> CreateGame()
        {
            var game = await _gameService.CreateGameAsync();
            var board = new Board(); // Khởi tạo bàn cờ mặc định

            var response = BuildGameResponse(game, board);
            return CreatedAtAction(nameof(GetGameById), new { id = game.Id }, response);
        }

        /// <summary>
        /// GET /api/games/{id}: Lấy thông tin chi tiết và trạng thái bàn cờ của ván đấu
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<GameResponseDto>> GetGameById(int id)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { message = $"Không tìm thấy ván cờ với ID {id}" });
            }

            // Tái hiện bàn cờ theo lịch sử các nước đi
            var board = ReconstructBoard(game.Moves);
            var response = BuildGameResponse(game, board);

            return Ok(response);
        }

        /// <summary>
        /// POST /api/games/{id}/moves: Ghi nhận nước đi mới của người chơi và cập nhật lịch sử nước đi
        /// </summary>
        [HttpPost("{id}/moves")]
        public async Task<ActionResult<GameResponseDto>> MakeMove(int id, [FromBody] MakeMoveRequestDto request)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { message = $"Không tìm thấy ván cờ với ID {id}" });
            }

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new { message = "Ván đấu đã kết thúc hoặc chưa sẵn sàng." });
            }

            // Tái hiện trạng thái bàn cờ hiện tại
            var board = ReconstructBoard(game.Moves);

            var movingPiece = board.GetPieceAt(request.FromX, request.FromY);
            if (movingPiece == null)
            {
                return BadRequest(new { message = "Không có quân cờ ở vị trí xuất phát." });
            }

            if (movingPiece.Color != game.CurrentTurn)
            {
                return BadRequest(new { message = $"Chưa tới lượt đi của quân {movingPiece.Color}." });
            }

            // Thực hiện nước đi qua Engine logic
            var legacyMove = new ChessCore.Models.Position(request.FromX, request.FromY);
            var legacyTarget = new ChessCore.Models.Position(request.ToX, request.ToY);

            var validMoves = MoveGenerator.GetValidMoves(board, legacyMove);
            bool isValid = validMoves.Any(m => m.To.Row == request.ToX && m.To.Col == request.ToY);

            if (!isValid)
            {
                return BadRequest(new { message = "Nước đi không hợp lệ theo luật Cờ Tướng." });
            }

            // Bắt quân đích nếu có
            var capturedPiece = board.GetPieceAt(request.ToX, request.ToY);

            // Ghi nhận nước đi vào Database
            var recordedMove = await _moveService.RecordMoveAsync(
                id,
                request.FromX,
                request.FromY,
                request.ToX,
                request.ToY,
                movingPiece.Type,
                movingPiece.Color
            );

            // Kiểm tra điều kiện thắng (nếu bắt Tướng đối phương)
            var nextTurn = game.CurrentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            var newStatus = GameStatus.InProgress;

            if (capturedPiece != null && capturedPiece.Type == PieceType.King)
            {
                newStatus = movingPiece.Color == PieceColor.Red ? GameStatus.RedWins : GameStatus.BlackWins;
            }

            await _gameService.UpdateGameTurnAndStatusAsync(id, nextTurn, newStatus);

            // Load lại game mới nhất
            var updatedGame = await _gameService.GetGameByIdAsync(id);
            var updatedBoard = ReconstructBoard(updatedGame.Moves);

            return Ok(BuildGameResponse(updatedGame, updatedBoard));
        }

        private static Board ReconstructBoard(IEnumerable<Move> moves)
        {
            var board = new Board();
            var sortedMoves = moves.OrderBy(m => m.MoveOrder);

            foreach (var m in sortedMoves)
            {
                var piece = board.GetPieceAt(m.FromX, m.FromY);
                if (piece != null)
                {
                    board.Grid[m.ToX, m.ToY] = piece;
                    board.Grid[m.FromX, m.FromY] = null;
                }
            }

            return board;
        }

        private static GameResponseDto BuildGameResponse(Game game, Board board)
        {
            var pieces = new List<PieceDto>();
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    var p = board.GetPieceAt(r, c);
                    if (p != null)
                    {
                        pieces.Add(new PieceDto
                        {
                            Row = r,
                            Col = c,
                            Type = p.Type.ToString(),
                            Color = p.Color.ToString()
                        });
                    }
                }
            }

            return new GameResponseDto
            {
                Id = game.Id,
                Status = game.Status.ToString(),
                CurrentTurn = game.CurrentTurn.ToString(),
                CreatedAt = game.CreatedAt,
                LastMoveAt = game.LastMoveAt,
                BoardPieces = pieces,
                Moves = game.Moves?.OrderBy(m => m.MoveOrder).Select(m => new MoveHistoryDto
                {
                    Id = m.Id,
                    FromX = m.FromX,
                    FromY = m.FromY,
                    ToX = m.ToX,
                    ToY = m.ToY,
                    PieceType = m.PieceType.ToString(),
                    PieceColor = m.PieceColor.ToString(),
                    MoveOrder = m.MoveOrder,
                    MovedAt = m.MovedAt
                }).ToList() ?? new List<MoveHistoryDto>()
            };
        }
    }
}
