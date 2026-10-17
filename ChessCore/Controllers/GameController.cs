using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Services;
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
        public int EvaluationLoss { get; set; }
        public int H1 { get; set; }
        public int H2 { get; set; }
        public int H3 { get; set; }
        public int H4 { get; set; }
        public string Judgment { get; set; } = "Good"; // Brilliant, Best, Good, Inaccuracy, Mistake, Blunder
        public string Comment { get; set; } = "";
    }

    public class GameReviewViewModel
    {
        public int GameId { get; set; }
        public string MatchTitle { get; set; } = "";
        public string GameMode { get; set; } = "Chơi với Máy";
        public string RoomCode { get; set; } = "";
        public GameStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RedPlayerName { get; set; } = "Quân Đỏ";
        public string BlackPlayerName { get; set; } = "Quân Đen";
        public string RedRank { get; set; } = "Kỳ Thủ";
        public string BlackRank { get; set; } = "Kỳ Thủ";
        public string ResultDescription { get; set; } = "Trận đấu kịch tính";
        public List<GameReviewItem> AnalyzedMoves { get; set; } = new();
        public int AccuracyRed { get; set; }
        public int AccuracyBlack { get; set; }
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

            bool isPvP = game.GameMode == GameModes.Online || game.GameMode == GameModes.Local;
            string redName = game.GameMode == GameModes.AiVsAi ? "Máy Đỏ" : game.RedPlayer?.Username ?? "Quân Đỏ";
            string blackName = game.GameMode == GameModes.PvE || game.GameMode == GameModes.AiVsAi
                ? "Máy (AI)" : game.BlackPlayer?.Username ?? "Quân Đen";
            string mode = GameModes.Label(game.GameMode);
            string matchTitle = $"{redName} vs. {blackName}";
            string redRank = "Không lưu rating tại thời điểm bắt đầu ván";
            string blackRank = "Không lưu rating tại thời điểm bắt đầu ván";

            string resultDesc = game.Status == GameStatus.RedWins ? "Quân Đỏ giành thắng lợi vẻ vang!" : (game.Status == GameStatus.BlackWins ? "Quân Đen giành thắng lợi vẻ vang!" : (game.Status == GameStatus.Draw ? "Ván đấu hòa cục bất phân thắng bại!" : "Trận đấu hấp dẫn và kịch tính"));

            var vm = new GameReviewViewModel
            {
                GameId = game.Id,
                MatchTitle = matchTitle,
                GameMode = mode,
                RoomCode = game.RoomCode,
                Status = game.Status,
                CreatedAt = game.CreatedAt,
                RedPlayerName = redName,
                BlackPlayerName = blackName,
                RedRank = redRank,
                BlackRank = blackRank,
                ResultDescription = resultDesc,
                InitialFen = game.InitialFen ?? ""
            };

            var orderedMoves = game.Moves.OrderBy(m => m.MoveOrder).ToList();
            var board = string.IsNullOrWhiteSpace(game.InitialFen) ? new Board() : Board.FromFen(game.InitialFen);
            var engine = new MinimaxEngine(depth: 2);

            var continuationEngine = new MinimaxEngine(depth: 1);
            int redGoodCount = 0, redTotal = 0;
            int blackGoodCount = 0, blackTotal = 0;

            for (int i = 0; i < orderedMoves.Count; i++)
            {
                var move = orderedMoves[i];
                var piece = board.GetPieceAt(move.FromX, move.FromY);

                if (piece == null) return Problem("Lịch sử ván cờ không hợp lệ.");
                // Same search horizon: best at depth 2, played move + depth 1.
                int bestAvailable = engine.EvaluatePosition(board, move.PieceColor);
                // Áp dụng nước đi lên bàn cờ mô phỏng
                board.Grid[move.FromX, move.FromY] = null;
                if (piece != null)
                {
                    board.Grid[move.ToX, move.ToY] = piece;
                }
                board.SetCurrentTurn((move.PieceColor == PieceColor.Red) ? PieceColor.Black : PieceColor.Red);

                int actual = continuationEngine.EvaluatePosition(board, move.PieceColor);
                int loss = Math.Max(0, bestAvailable - actual);
                int currentEval = move.PieceColor == PieceColor.Red ? actual : -actual;
                string judgment = loss <= 10 ? "Best" : loss <= 50 ? "Good"
                    : loss <= 150 ? "Inaccuracy" : loss <= 300 ? "Mistake" : "Blunder";
                string comment = $"Mất {loss} điểm heuristic so với lựa chọn tốt nhất ở độ sâu 2. Đánh giá nội bộ, không phải độ chính xác WXF.";
                var redScores = BoardEvaluator.GetSideScores(board, PieceColor.Red);
                var blackScores = BoardEvaluator.GetSideScores(board, PieceColor.Black);

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
                    EvaluationLoss = loss,
                    H1 = redScores.H1 - blackScores.H1,
                    H2 = redScores.H2 - blackScores.H2,
                    H3 = redScores.H3 - blackScores.H3,
                    H4 = redScores.H4 - blackScores.H4,
                    Judgment = judgment,
                    Comment = comment + $" H1={redScores.H1-blackScores.H1}, H2={redScores.H2-blackScores.H2}, H3={redScores.H3-blackScores.H3}, H4={redScores.H4-blackScores.H4}."
                });
            }

            vm.AccuracyRed = redTotal > 0 ? (int)Math.Round((double)redGoodCount * 100 / redTotal) : 0;
            vm.AccuracyBlack = blackTotal > 0 ? (int)Math.Round((double)blackGoodCount * 100 / blackTotal) : 0;

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