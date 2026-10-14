using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Engine;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Controllers
{
    public class GameReviewItem
    {
        public int MoveOrder { get; set; }
        public string Notation { get; set; } = "";
        public string PieceColor { get; set; } = "";
        public string PieceType { get; set; } = "";
        public int FromX { get; set; }
        public int FromY { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }
        public int EvalScore { get; set; }
        public string Judgment { get; set; } = "Good"; // Brilliant, Best, Good, Inaccuracy, Mistake, Blunder
        public string Comment { get; set; } = "";
    }

    public class GameReviewViewModel
    {
        public int GameId { get; set; }
        public string RoomCode { get; set; } = "";
        public GameStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RedPlayerName { get; set; } = "Quân Đỏ";
        public string BlackPlayerName { get; set; } = "Quân Đen";
        public List<GameReviewItem> AnalyzedMoves { get; set; } = new();
        public int AccuracyRed { get; set; } = 85;
        public int AccuracyBlack { get; set; } = 82;
        public string InitialFen { get; set; } = "";
    }

    public class GameController : Controller
    {
        private readonly AppDbContext _context;

        public GameController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // =========================================================
        // GIAI ĐOẠN 2: XEM LẠI VÁN CỜ & PHÂN TÍCH NƯỚC ĐI BẰNG AI
        // =========================================================
        [HttpGet("Game/Review/{id}")]
        public async Task<IActionResult> Review(int id)
        {
            var game = await _context.Games
                .Include(g => g.RedPlayer)
                .Include(g => g.BlackPlayer)
                .Include(g => g.Moves)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                return NotFound("Không tìm thấy ván cờ yêu cầu.");
            }

            var vm = new GameReviewViewModel
            {
                GameId = game.Id,
                RoomCode = game.RoomCode,
                Status = game.Status,
                CreatedAt = game.CreatedAt,
                RedPlayerName = game.RedPlayer?.Username ?? "Quân Đỏ",
                BlackPlayerName = game.BlackPlayer?.Username ?? "Quân Đen",
                InitialFen = game.InitialFen ?? ""
            };

            var orderedMoves = game.Moves.OrderBy(m => m.MoveOrder).ToList();
            var board = GameRules.RebuildBoard(game.InitialFen, null, PieceColor.Red);
            var engine = new MinimaxEngine(depth: 2);

            int previousEval = 0;
            int redGoodCount = 0, redTotal = 0;
            int blackGoodCount = 0, blackTotal = 0;

            for (int i = 0; i < orderedMoves.Count; i++)
            {
                var move = orderedMoves[i];
                var piece = board.GetPieceAt(move.FromX, move.FromY);

                // Áp dụng nước đi lên bàn cờ mô phỏng
                board.Grid[move.FromX, move.FromY] = null;
                if (piece != null)
                {
                    board.Grid[move.ToX, move.ToY] = piece;
                }
                board.SetCurrentTurn((move.PieceColor == PieceColor.Red) ? PieceColor.Black : PieceColor.Red);

                // Tính điểm thế cờ sau nước đi (góc nhìn của Đỏ)
                int currentEval = engine.EvaluatePosition(board, PieceColor.Red);
                int evalDiff = (move.PieceColor == PieceColor.Red) ? (currentEval - previousEval) : (previousEval - currentEval);

                string judgment = "Good";
                string comment = "";

                if (evalDiff >= 200)
                {
                    judgment = "Brilliant";
                    comment = "Nước đi xuất sắc! Tạo đột biến lớn cho thế trận.";
                }
                else if (evalDiff >= 50)
                {
                    judgment = "Best";
                    comment = "Nước đi tối ưu và chính xác.";
                }
                else if (evalDiff >= -50)
                {
                    judgment = "Good";
                    comment = "Nước đi hợp lý và vững chắc.";
                }
                else if (evalDiff >= -200)
                {
                    judgment = "Inaccuracy";
                    comment = "Nước đi hơi thiếu chuẩn xác, có thể bị ép thế.";
                }
                else
                {
                    judgment = "Blunder";
                    comment = "Sai lầm nghiêm trọng! Đã để mất lợi thế hoặc mất quân lớn.";
                }

                if (move.PieceColor == PieceColor.Red)
                {
                    redTotal++;
                    if (judgment == "Brilliant" || judgment == "Best" || judgment == "Good") redGoodCount++;
                }
                else
                {
                    blackTotal++;
                    if (judgment == "Brilliant" || judgment == "Best" || judgment == "Good") blackGoodCount++;
                }

                previousEval = currentEval;

                vm.AnalyzedMoves.Add(new GameReviewItem
                {
                    MoveOrder = move.MoveOrder + 1,
                    PieceColor = move.PieceColor.ToString(),
                    PieceType = move.PieceType.ToString(),
                    FromX = move.FromX,
                    FromY = move.FromY,
                    ToX = move.ToX,
                    ToY = move.ToY,
                    Notation = $"{GetPieceVietnameseName(move.PieceType)} ({move.FromX},{move.FromY}) ➜ ({move.ToX},{move.ToY})",
                    EvalScore = currentEval,
                    Judgment = judgment,
                    Comment = comment
                });
            }

            vm.AccuracyRed = redTotal > 0 ? (int)Math.Round((double)redGoodCount * 100 / redTotal) : 100;
            vm.AccuracyBlack = blackTotal > 0 ? (int)Math.Round((double)blackGoodCount * 100 / blackTotal) : 100;

            return View(vm);
        }

        private static string GetPieceVietnameseName(PieceType type)
        {
            return type switch
            {
                PieceType.King => "Tướng",
                PieceType.Advisor => "Sĩ",
                PieceType.Elephant => "Tượng",
                PieceType.Horse => "Mã",
                PieceType.Rook => "Xe",
                PieceType.Cannon => "Pháo",
                PieceType.Pawn => "Binh",
                _ => "Quân"
            };
        }
    }
}