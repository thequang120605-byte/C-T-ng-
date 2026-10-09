using Microsoft.AspNetCore.SignalR;
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
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<Hubs.GameHub> _hubContext;

        public GamesController(
            IGameService gameService,
            IMoveService moveService,
            Microsoft.AspNetCore.SignalR.IHubContext<Hubs.GameHub> hubContext
        )
        {
            _gameService = gameService;
            _moveService = moveService;
            _hubContext = hubContext;
        }

        // =====================================================
        // POST /api/games
        // Táº O VÃN Cá»œ Má»šI
        // =====================================================

        [HttpPost]
        public async Task<ActionResult<GameResponseDto>> CreateGame([FromQuery] string initialFen = null)
        {
            var game =
                await _gameService.CreateGameAsync(initialFen);

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
        // Láº¤Y THÃ”NG TIN VÃN Cá»œ
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
                        $"KhÃ´ng tÃ¬m tháº¥y vÃ¡n cá» vá»›i ID {id}"
                });
            }

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
        // Láº¤Y CÃC NÆ¯á»šC ÄI Há»¢P Lá»†
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
                        $"KhÃ´ng tÃ¬m tháº¥y vÃ¡n cá» vá»›i ID {id}"
                });
            }

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "VÃ¡n cá» Ä‘Ã£ káº¿t thÃºc."
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
                        "KhÃ´ng cÃ³ quÃ¢n cá» táº¡i vá»‹ trÃ­ nÃ y."
                });
            }

            if (piece.Color != game.CurrentTurn)
            {
                return BadRequest(new
                {
                    message =
                        "ChÆ°a tá»›i lÆ°á»£t cá»§a quÃ¢n nÃ y."
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
        // NGÆ¯á»œI CHÆ I THá»°C HIá»†N NÆ¯á»šC ÄI
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
                        $"KhÃ´ng tÃ¬m tháº¥y vÃ¡n cá» vá»›i ID {id}"
                });
            }

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "VÃ¡n Ä‘áº¥u Ä‘Ã£ káº¿t thÃºc."
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
                        "KhÃ´ng cÃ³ quÃ¢n cá» á»Ÿ vá»‹ trÃ­ xuáº¥t phÃ¡t."
                });
            }

            if (movingPiece.Color != game.CurrentTurn)
            {
                return BadRequest(new
                {
                    message =
                        $"ChÆ°a tá»›i lÆ°á»£t Ä‘i cá»§a quÃ¢n {movingPiece.Color}."
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
                        "NÆ°á»›c Ä‘i khÃ´ng há»£p lá»‡ theo luáº­t Cá» TÆ°á»›ng."
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
            // LÆ¯U NÆ¯á»šC ÄI
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
            // Cáº¬P NHáº¬T TRáº NG THÃI
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
            // Láº¤Y GAME Má»šI NHáº¤T
            // =================================================

            var updatedGame =
                await _gameService.GetGameByIdAsync(id);

            if (updatedGame == null)
            {
                return NotFound(new
                {
                    message =
                        "KhÃ´ng thá»ƒ táº£i láº¡i vÃ¡n cá»."
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
        // MÃY THá»°C HIá»†N NÆ¯á»šC ÄI
        // =====================================================

        [HttpPost("{id}/computer-move")]
        public async Task<ActionResult<GameResponseDto>> ComputerMove(
            int id,
            [FromQuery] int difficulty = 2,
            [FromQuery] string computerColor = "black"
        )
        {
            // =================================================
            // 1. Láº¤Y GAME
            // =================================================

            var game =
                await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message =
                        $"KhÃ´ng tÃ¬m tháº¥y vÃ¡n cá» vá»›i ID {id}"
                });
            }

            // =================================================
            // 2. KIá»‚M TRA TRáº NG THÃI
            // =================================================

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "VÃ¡n cá» Ä‘Ã£ káº¿t thÃºc."
                });
            }

            // =================================================
            // 3. KIá»‚M TRA LÆ¯á»¢T
            // =================================================

            PieceColor engineColor = computerColor.ToLower() == "red" ? PieceColor.Red : PieceColor.Black;

            if (game.CurrentTurn != engineColor)
            {
                return BadRequest(new
                {
                    message =
                        "Hiá»‡n táº¡i chÆ°a tá»›i lÆ°á»£t cá»§a mÃ¡y."
                });
            }

            // =================================================
            // 4. GIá»šI Háº N Äá»˜ KHÃ“
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
            // 5. TÃI Táº O BÃ€N Cá»œ
            // =================================================

            var board =
                GameRules.RebuildBoard(
                    game.InitialFen,
                    game.Moves,
                    game.CurrentTurn
                );

            // =================================================
            // 6. Táº O COMPUTER PLAYER
            // =================================================

            var computer =
                new ComputerPlayer(
                    engineColor,
                    difficulty
                );

            // =================================================
            // 7. TÃŒM NÆ¯á»šC ÄI
            // =================================================

            var computerMove =
                computer.GetMove(board);

            if (computerMove == null)
            {
                return BadRequest(new
                {
                    message =
                        "MÃ¡y khÃ´ng tÃ¬m Ä‘Æ°á»£c nÆ°á»›c Ä‘i há»£p lá»‡."
                });
            }

            // =================================================
            // 8. Láº¤Y QUÃ‚N Cá»œ
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
                        "KhÃ´ng tÃ¬m tháº¥y quÃ¢n cá» cá»§a mÃ¡y."
                });
            }

            // =================================================
            // 9. KIá»‚M TRA QUÃ‚N Bá»Š Ä‚N
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
            // 10. LÆ¯U NÆ¯á»šC ÄI
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
            // 11. Cáº¬P NHáº¬T BÃ€N Cá»œ MEMORY
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
            // 12. XÃC Äá»ŠNH TRáº NG THÃI
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
            // 13. Cáº¬P NHáº¬T GAME
            // =================================================

            await _gameService.UpdateGameTurnAndStatusAsync(
                id,
                nextTurn,
                newStatus
            );

            // =================================================
            // 14. Láº¤Y GAME Má»šI NHáº¤T
            // =================================================

            var updatedGame =
                await _gameService.GetGameByIdAsync(id);

            if (updatedGame == null)
            {
                return NotFound(new
                {
                    message =
                        "KhÃ´ng thá»ƒ táº£i láº¡i vÃ¡n cá» sau khi mÃ¡y Ä‘i."
                });
            }

            // =================================================
            // 15. TÃI Táº O BÃ€N Cá»œ
            // =================================================

            var updatedBoard =
                GameRules.RebuildBoard(
                    updatedGame.InitialFen,
                    updatedGame.Moves,
                    updatedGame.CurrentTurn
                );

            // =================================================
            // 16. TRáº¢ Káº¾T QUáº¢
            // =================================================

            var response = BuildGameResponse(updatedGame, updatedBoard);
            await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", response);
            return Ok(response);
        }

        // =====================================================
        // POST /api/games/{id}/undo
        // ÄI Láº I (UNDO)
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
                return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y vÃ¡n cá»." });
            }

            var success = await _gameService.UndoMoveAsync(id, isPvE);
            if (!success)
            {
                return BadRequest(new { message = "KhÃ´ng thá»ƒ Ä‘i láº¡i (chÆ°a Ä‘á»§ nÆ°á»›c Ä‘i)." });
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
        // Äáº¦U HÃ€NG
        // =====================================================

        [HttpPost("{id}/resign")]
        public async Task<ActionResult<GameResponseDto>> Resign(
            int id
        )
        {
            // =================================================
            // 1. Láº¤Y GAME
            // =================================================

            var game =
                await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(new
                {
                    message =
                        $"KhÃ´ng tÃ¬m tháº¥y vÃ¡n cá» vá»›i ID {id}"
                });
            }

            // =================================================
            // 2. KIá»‚M TRA GAME ÄÃƒ Káº¾T THÃšC CHÆ¯A
            // =================================================

            if (game.Status != GameStatus.InProgress)
            {
                return BadRequest(new
                {
                    message =
                        "VÃ¡n cá» Ä‘Ã£ káº¿t thÃºc."
                });
            }

            // =================================================
            // 3. XÃC Äá»ŠNH NGÆ¯á»œI Äáº¦U HÃ€NG
            //
            // Red Ä‘áº§u hÃ ng  -> Black tháº¯ng
            // Black Ä‘áº§u hÃ ng -> Red tháº¯ng
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
            // 4. Cáº¬P NHáº¬T GAME VÃ€O DATABASE
            //
            // Giá»¯ CurrentTurn hiá»‡n táº¡i vÃ¬ vÃ¡n Ä‘Ã£ káº¿t thÃºc.
            // Status má»›i sáº½ quyáº¿t Ä‘á»‹nh ngÆ°á»i tháº¯ng.
            // =================================================

            await _gameService.UpdateGameTurnAndStatusAsync(
                id,
                game.CurrentTurn,
                newStatus
            );

            // =================================================
            // 5. Láº¤Y GAME SAU KHI UPDATE
            // =================================================

            var updatedGame =
                await _gameService.GetGameByIdAsync(id);

            if (updatedGame == null)
            {
                return NotFound(new
                {
                    message =
                        "KhÃ´ng thá»ƒ táº£i láº¡i vÃ¡n cá» sau khi Ä‘áº§u hÃ ng."
                });
            }

            // =================================================
            // 6. TÃI Táº O BÃ€N Cá»œ
            // =================================================

            var updatedBoard =
                GameRules.RebuildBoard(
                    updatedGame.InitialFen,
                    updatedGame.Moves,
                    updatedGame.CurrentTurn
                );

            // =================================================
            // 7. TRáº¢ RESPONSE GIá»NG CÃC API KHÃC
            // =================================================

            var response = BuildGameResponse(updatedGame, updatedBoard);
            await _hubContext.Clients.Group(id.ToString()).SendAsync("GameUpdated", response);
            return Ok(response);
        }

        // =====================================================
        // GET /api/games/{id}/fen
        // Láº¤Y FEN Cá»¦A VÃN Cá»œ HIá»†N Táº I
        // =====================================================
        [HttpGet("{id}/fen")]
        public async Task<ActionResult<string>> GetFen(int id)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y vÃ¡n cá» vá»›i ID {id}" });
            }

            var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
            var fen = board.ToFen();
            return Ok(new { fen = fen });
        }

        // =====================================================
        // GET /api/games/{id}/eval
        // Láº¤Y ÄIá»‚M ÄÃNH GIÃ (EVALUATION BAR)
        // =====================================================
        [HttpGet("{id}/eval")]
        public async Task<ActionResult<int>> GetEval(int id)
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null) return NotFound();

            var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
            
            // Láº¥y Ä‘iá»ƒm theo gÃ³c nhÃ¬n cá»§a quÃ¢n Äá» (DÆ°Æ¡ng = Äá» Æ°u tháº¿, Ã‚m = Äen Æ°u tháº¿)
            var engine = new MinimaxEngine(3); // Depth 3 for analysis
            var eval = engine.EvaluatePosition(board, PieceColor.Red);
            
            return Ok(new { eval = eval });
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
            // Láº¤Y TOÃ€N Bá»˜ QUÃ‚N Cá»œ TRÃŠN BÃ€N
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
            // KIá»‚M TRA CHIáº¾U TÆ¯á»šNG
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
            // TRáº¢ RESPONSE
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
