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

            var board =
                new Board();

            var response =
                BuildGameResponse(
                    game,
                    board
                );

            return CreatedAtAction(
                nameof(GetGameById),
                new
                {
                    id = game.Id
                },
                response
            );
        }


        // =====================================================
        // GET /api/games/{id}
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
                return NotFound(
                    new
                    {
                        message =
                            $"Không tìm thấy ván cờ với ID {id}"
                    }
                );
            }

            var board =
                ReconstructBoard(
                    game.Moves
                );

            var response =
                BuildGameResponse(
                    game,
                    board
                );

            return Ok(response);
        }


        // =====================================================
        // GET VALID MOVES
        // =====================================================

        [HttpGet("{id}/valid-moves")]
        public async Task<ActionResult<List<ValidMoveDto>>>
            GetValidMoves(
                int id,
                [FromQuery] int row,
                [FromQuery] int col
            )
        {
            var game =
                await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            $"Không tìm thấy ván cờ với ID {id}"
                    }
                );
            }

            if (
                game.Status !=
                GameStatus.InProgress
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Ván cờ đã kết thúc."
                    }
                );
            }

            var board =
                ReconstructBoard(
                    game.Moves
                );

            var piece =
                board.GetPieceAt(
                    row,
                    col
                );

            if (piece == null)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Không có quân cờ tại vị trí này."
                    }
                );
            }

            if (
                piece.Color !=
                game.CurrentTurn
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Chưa tới lượt của quân này."
                    }
                );
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
                .Select(
                    move =>
                    {
                        var targetPiece =
                            board.GetPieceAt(
                                move.To.Row,
                                move.To.Col
                            );

                        return new ValidMoveDto
                        {
                            Row =
                                move.To.Row,

                            Col =
                                move.To.Col,

                            IsCapture =
                                targetPiece != null
                        };
                    }
                )
                .ToList();

            return Ok(result);
        }


        // =====================================================
        // POST /api/games/{id}/moves
        // =====================================================

        [HttpPost("{id}/moves")]
        public async Task<ActionResult<GameResponseDto>>
            MakeMove(
                int id,
                [FromBody]
                MakeMoveRequestDto request
            )
        {
            // =============================================
            // LẤY GAME
            // =============================================

            var game =
                await _gameService.GetGameByIdAsync(
                    id
                );

            if (game == null)
            {
                return NotFound(
                    new
                    {
                        message =
                            $"Không tìm thấy ván cờ với ID {id}"
                    }
                );
            }


            // =============================================
            // KIỂM TRA GAME
            // =============================================

            if (
                game.Status !=
                GameStatus.InProgress
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Ván đấu đã kết thúc."
                    }
                );
            }


            // =============================================
            // TÁI TẠO BÀN CỜ
            // =============================================

            var board =
                ReconstructBoard(
                    game.Moves
                );


            // =============================================
            // QUÂN ĐANG DI CHUYỂN
            // =============================================

            var movingPiece =
                board.GetPieceAt(
                    request.FromX,
                    request.FromY
                );

            if (movingPiece == null)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Không có quân cờ ở vị trí xuất phát."
                    }
                );
            }


            // =============================================
            // KIỂM TRA LƯỢT
            // =============================================

            if (
                movingPiece.Color !=
                game.CurrentTurn
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            $"Chưa tới lượt đi của quân {movingPiece.Color}."
                    }
                );
            }


            // =============================================
            // VỊ TRÍ
            // =============================================

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


            // =============================================
            // LẤY NƯỚC ĐI HỢP LỆ
            // =============================================

            var validMoves =
                MoveGenerator.GetValidMoves(
                    board,
                    from
                );


            bool isValid =
                validMoves.Any(
                    move =>
                        move.To.Row ==
                        request.ToX
                        &&
                        move.To.Col ==
                        request.ToY
                );


            if (!isValid)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Nước đi không hợp lệ theo luật Cờ Tướng."
                    }
                );
            }


            // =============================================
            // KIỂM TRA QUÂN BỊ ĂN
            // =============================================

            var capturedPiece =
                board.GetPieceAt(
                    request.ToX,
                    request.ToY
                );


            bool isKingCaptured =
                capturedPiece != null
                &&
                capturedPiece.Type ==
                PieceType.King;


            // =============================================
            // LƯU NƯỚC ĐI
            // =============================================

            await _moveService.RecordMoveAsync(
                id,

                request.FromX,
                request.FromY,

                request.ToX,
                request.ToY,

                movingPiece.Type,
                movingPiece.Color
            );


            // =============================================
            // XỬ LÝ TRẠNG THÁI
            // =============================================

            GameStatus newStatus;

            PieceColor nextTurn;


            // =============================================
            // ĂN TƯỚNG
            // =============================================

            if (isKingCaptured)
            {
                if (
                    movingPiece.Color ==
                    PieceColor.Red
                )
                {
                    newStatus =
                        GameStatus.RedWins;

                    nextTurn =
                        PieceColor.Red;
                }
                else
                {
                    newStatus =
                        GameStatus.BlackWins;

                    nextTurn =
                        PieceColor.Black;
                }
            }


            // =============================================
            // TIẾP TỤC GAME
            // =============================================

            else
            {
                newStatus =
                    GameStatus.InProgress;

                nextTurn =
                    game.CurrentTurn ==
                    PieceColor.Red

                    ?

                    PieceColor.Black

                    :

                    PieceColor.Red;
            }


            // =============================================
            // CẬP NHẬT GAME
            // =============================================

            await
                _gameService
                .UpdateGameTurnAndStatusAsync(
                    id,
                    nextTurn,
                    newStatus
                );


            // =============================================
            // LOAD GAME MỚI
            // =============================================

            var updatedGame =
                await
                _gameService
                .GetGameByIdAsync(
                    id
                );


            // =============================================
            // TÁI TẠO BOARD
            // =============================================

            var updatedBoard =
                ReconstructBoard(
                    updatedGame.Moves
                );


            // =============================================
            // RETURN
            // =============================================

            return Ok(
                BuildGameResponse(
                    updatedGame,
                    updatedBoard
                )
            );
        }


        // =====================================================
        // TÁI TẠO BÀN CỜ
        // =====================================================

        private static Board ReconstructBoard(
            IEnumerable<Move> moves
        )
        {
            var board =
                new Board();


            var sortedMoves =
                moves
                .OrderBy(
                    move =>
                        move.MoveOrder
                );


            foreach (
                var move
                in sortedMoves
            )
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
                        $"({move.FromX}, {move.FromY})"
                    );

                    continue;
                }


                // =============================================
                // QUÂN TẠI Ô ĐÍCH
                // =============================================

                var capturedPiece =
                    board.GetPieceAt(
                        move.ToX,
                        move.ToY
                    );


                // =============================================
                // ĂN QUÂN
                // =============================================

                if (capturedPiece != null)
                {
                    Console.WriteLine(
                        $"{piece.Color} " +
                        $"{piece.Type} " +
                        $"ăn " +
                        $"{capturedPiece.Color} " +
                        $"{capturedPiece.Type}"
                    );
                }


                // =============================================
                // DI CHUYỂN
                // Ghi đè quân địch nếu có
                // =============================================

                board.Grid[
                    move.ToX,
                    move.ToY
                ]
                =
                piece;


                // =============================================
                // XÓA VỊ TRÍ CŨ
                // =============================================

                board.Grid[
                    move.FromX,
                    move.FromY
                ]
                =
                null;
            }


            return board;
        }


        // =====================================================
        // BUILD RESPONSE
        // =====================================================

        private static GameResponseDto
            BuildGameResponse(
                Game game,
                Board board
            )
        {
            var pieces =
                new List<PieceDto>();


            for (
                int row = 0;
                row < 10;
                row++
            )
            {
                for (
                    int col = 0;
                    col < 9;
                    col++
                )
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
                                    piece.Type
                                    .ToString(),

                                Color =
                                    piece.Color
                                    .ToString()
                            }
                        );
                    }
                }
            }


            Console.WriteLine(
                $"Game ID: {game.Id}"
            );

            Console.WriteLine(
                $"Status: {game.Status}"
            );

            Console.WriteLine(
                $"Số quân còn lại: {pieces.Count}"
            );


            return new GameResponseDto
            {
                Id =
                    game.Id,

                Status =
                    game.Status
                    .ToString(),

                CurrentTurn =
                    game.CurrentTurn
                    .ToString(),

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

                    ??

                    new List<MoveHistoryDto>()
            };
        }
    }
}