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
        public async Task<ActionResult<GameResponseDto>> CreateGame([FromBody] CreateGameRequestDto request)
        {
            var game = await _gameService.CreateGameAsync(
                request?.RedPlayerId,
                request?.BlackPlayerId,
                request?.GameType ?? GameType.Standard
            );

            var board = new Board();
            var response = BuildGameResponse(game, board);

            return CreatedAtAction(
                nameof(GetGameById),
                new { id = game.Id },
                response
            );
        }

        // =====================================================
        // GET /api/games/{id}
        // =====================================================

        [HttpGet("{id}")]
        public async Task<ActionResult<GameResponseDto>> GetGameById(int id)
        {
            var game = await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message = $"Không tìm thấy ván cờ với ID {id}"
                });
            }

            var board = ReconstructBoard(game.Moves);
            var response = BuildGameResponse(game, board);

            return Ok(response);
        }

        // =====================================================
        // GET VALID MOVES
        // =====================================================

        [HttpGet("{id}/valid-moves")]
        public async Task<ActionResult<List<ValidMoveDto>>> GetValidMoves(
            int id,
            [FromQuery] int row,
            [FromQuery] int col
        )
        {
            var game = await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message = $"Không tìm thấy ván cờ với ID {id}"
                });
            }

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message = "Ván cờ đã kết thúc."
                });
            }

            var board = ReconstructBoard(game.Moves);
            var piece = board.GetPieceAt(row, col);

            if (piece == null)
            {
                return BadRequest(new
                {
                    message = "Không có quân cờ tại vị trí này."
                });
            }

            if (piece.Color != game.CurrentTurn)
            {
                return BadRequest(new
                {
                    message = "Chưa tới lượt của quân này."
                });
            }

            var from = new Position(row, col);
            var validMoves = MoveGenerator.GetValidMoves(board, from);

            var result = validMoves
                .Select(move =>
                {
                    var targetPiece = board.GetPieceAt(move.To.Row, move.To.Col);
                    return new ValidMoveDto
                    {
                        Row = move.To.Row,
                        Col = move.To.Col,
                        IsCapture = targetPiece != null
                    };
                })
                .ToList();

            return Ok(result);
        }

        // =====================================================
        // POST /api/games/{id}/moves
        // =====================================================

        [HttpPost("{id}/moves")]
        public async Task<ActionResult<GameResponseDto>> MakeMove(
            int id,
            [FromBody] MakeMoveRequestDto request
        )
        {
            var game = await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message = $"Không tìm thấy ván cờ với ID {id}"
                });
            }

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message = "Ván đấu đã kết thúc."
                });
            }

            var board = ReconstructBoard(game.Moves);
            var movingPiece = board.GetPieceAt(request.FromX, request.FromY);

            if (movingPiece == null)
            {
                return BadRequest(new
                {
                    message = "Không có quân cờ ở vị trí xuất phát."
                });
            }

            if (movingPiece.Color != game.CurrentTurn)
            {
                return BadRequest(new
                {
                    message = $"Chưa tới lượt đi của quân {movingPiece.Color}."
                });
            }

            var from = new Position(request.FromX, request.FromY);
            var to = new Position(request.ToX, request.ToY);

            var validMoves = MoveGenerator.GetValidMoves(board, from);

            bool isValid = validMoves.Any(move =>
                move.To.Row == request.ToX &&
                move.To.Col == request.ToY
            );

            if (!isValid)
            {
                return BadRequest(new
                {
                    message = "Nước đi không hợp lệ theo luật Cờ Tướng."
                });
            }

            var capturedPiece = board.GetPieceAt(request.ToX, request.ToY);
            bool isKingCaptured = capturedPiece != null && capturedPiece.Type == PieceType.King;

            // Lưu nước đi
            await _moveService.RecordMoveAsync(
                id,
                request.FromX,
                request.FromY,
                request.ToX,
                request.ToY,
                movingPiece.Type,
                movingPiece.Color
            );

            // Xử lý trạng thái ván đấu
            GameStatus newStatus;
            PieceColor nextTurn;

            if (isKingCaptured)
            {
                newStatus = movingPiece.Color == PieceColor.Red ? GameStatus.RedWins : GameStatus.BlackWins;
                nextTurn = movingPiece.Color;
                await _gameService.FinishGameAsync(id, movingPiece.Color, newStatus);
            }
            else
            {
                newStatus = GameStatus.InProgress;
                nextTurn = game.CurrentTurn == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
                await _gameService.UpdateGameTurnAndStatusAsync(id, nextTurn, newStatus);
            }

            // Tải lại game mới nhất
            var updatedGame = await _gameService.GetGameByIdAsync(id);
            var updatedBoard = ReconstructBoard(updatedGame.Moves);

            return Ok(BuildGameResponse(updatedGame, updatedBoard));
        }

        // =====================================================
        // TÁI TẠO BÀN CỜ
        // =====================================================

        private static Board ReconstructBoard(IEnumerable<Move> moves)
        {
            var board = new Board();
            var sortedMoves = moves.OrderBy(move => move.MoveOrder);

            foreach (var move in sortedMoves)
            {
                var piece = board.GetPieceAt(move.FromX, move.FromY);

                if (piece == null)
                {
                    Console.WriteLine($"Không tìm thấy quân tại ({move.FromX}, {move.FromY})");
                    continue;
                }

                board.Grid[move.ToX, move.ToY] = piece;
                board.Grid[move.FromX, move.FromY] = null!;
            }

            return board;
        }

        // =====================================================
        // BUILD RESPONSE
        // =====================================================

        private static GameResponseDto BuildGameResponse(Game game, Board board)
        {
            var pieces = new List<PieceDto>();

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    var piece = board.GetPieceAt(row, col);
                    if (piece != null)
                    {
                        pieces.Add(new PieceDto
                        {
                            Row = row,
                            Col = col,
                            Type = piece.Type.ToString(),
                            Color = piece.Color.ToString()
                        });
                    }
                }
            }

            // Kiểm tra xem bên đang đến lượt đi có bị đối phương chiếu tướng hay không
            bool isCheck = false;
            if (game.Status == GameStatus.InProgress)
            {
                isCheck = MoveGenerator.CheckIfInCheck(board, game.CurrentTurn);
            }

            return new GameResponseDto
            {
                Id = game.Id,
                RedPlayerId = game.RedPlayerId,
                BlackPlayerId = game.BlackPlayerId,
                GameType = game.GameType.ToString(),
                Status = game.Status.ToString(),
                CurrentTurn = game.CurrentTurn.ToString(),
                Winner = game.Winner?.ToString(),
                IsCheck = isCheck,
                CreatedAt = game.CreatedAt,
                LastMoveAt = game.LastMoveAt,
                BoardPieces = pieces,
                Moves = game.Moves?
                    .OrderBy(move => move.MoveOrder)
                    .Select(move => new MoveHistoryDto
                    {
                        Id = move.Id,
                        FromX = move.FromX,
                        FromY = move.FromY,
                        ToX = move.ToX,
                        ToY = move.ToY,
                        PieceType = move.PieceType.ToString(),
                        PieceColor = move.PieceColor.ToString(),
                        MoveOrder = move.MoveOrder,
                        MovedAt = move.MovedAt
                    })
                    .ToList() ?? new List<MoveHistoryDto>()
            };
        }
    }
}