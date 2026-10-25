using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public string Quality { get; set; } = "Nước tốt";
        public string QualityBadge { get; set; } = "✓";
        public string Comment { get; set; } = "";
        public int OptimalFromX { get; set; }
        public int OptimalFromY { get; set; }
        public int OptimalToX { get; set; }
        public int OptimalToY { get; set; }
        public string AiAlternative { get; set; } = "";
        public string AiReasoning { get; set; } = "";
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
        public int AccuracyRed { get; set; } = 85;
        public int AccuracyBlack { get; set; } = 82;
        public string InitialFen { get; set; } = "";
    }

    public class AnalyzeReviewRequest
    {
        public int? GameId { get; set; }
        public string? RedPlayerName { get; set; }
        public string? BlackPlayerName { get; set; }
        public string? InitialFen { get; set; }
        public List<string>? Moves { get; set; }
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
            var vm = await GetOrBuildReviewViewModel(id);
            if (vm == null)
            {
                return NotFound("Không tìm thấy ván cờ yêu cầu.");
            }
            return View(vm);
        }

        [HttpGet("/api/games/{id}/review")]
        public async Task<IActionResult> ApiReview(int id)
        {
            var vm = await GetOrBuildReviewViewModel(id);
            if (vm == null)
            {
                return NotFound(new { message = "Không tìm thấy ván cờ yêu cầu." });
            }
            return Ok(vm);
        }

        [HttpPost("/api/games/review")]
        public async Task<IActionResult> ApiAnalyzeCustomReview([FromBody] AnalyzeReviewRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Yêu cầu không hợp lệ." });
            }

            if (request.Moves != null && request.Moves.Any())
            {
                var customVm = BuildReviewFromMoveStrings(request);
                return Ok(customVm);
            }

            var vm = await GetOrBuildReviewViewModel(request.GameId ?? 0);
            return Ok(vm);
        }

        private async Task<GameReviewViewModel?> GetOrBuildReviewViewModel(int id)
        {
            Game? game = null;
            if (id > 0)
            {
                game = await _context.Games
                    .Include(g => g.RedPlayer)
                    .Include(g => g.BlackPlayer)
                    .Include(g => g.Moves)
                    .FirstOrDefaultAsync(g => g.Id == id);
            }

            // Nếu không tìm thấy hoặc ván cờ chưa có nước đi, tìm ván cờ gần nhất có nước đi
            if (game == null || game.Moves == null || !game.Moves.Any())
            {
                game = await _context.Games
                    .Include(g => g.RedPlayer)
                    .Include(g => g.BlackPlayer)
                    .Include(g => g.Moves)
                    .Where(g => g.Moves.Any())
                    .OrderByDescending(g => g.Id)
                    .FirstOrDefaultAsync();
            }

            if (game != null && game.Moves.Any())
            {
                return AnalyzeGame(game);
            }

            // Nếu cơ sở dữ liệu chưa có ván nào có nước đi, tạo ván mẫu kinh điển và phân tích bằng AI Minimax
            return BuildMasterSampleReview();
        }

        private GameReviewViewModel AnalyzeGame(Game game)
        {
            bool isPvP = game.RedPlayerId.HasValue && game.BlackPlayerId.HasValue;
            string redName = game.RedPlayer?.Username ?? "Quang Nguyễn Thế";
            string blackName = isPvP ? (game.BlackPlayer?.Username ?? "Trần Phương Thảo") : "AI Kiện Tướng";
            string mode = isPvP ? "Đấu trực tuyến (PvP)" : "Chơi với Máy (AI)";
            string matchTitle = $"{redName} vs. {blackName}";

            string redRank = game.RedPlayer != null ? $"Elo {game.RedPlayer.Elo}" : "Elo 1750";
            string blackRank = (isPvP && game.BlackPlayer != null) ? $"Elo {game.BlackPlayer.Elo}" : (isPvP ? "Elo 1720" : "AI Master 2100");
            string resultDesc = game.Status == GameStatus.RedWins ? "Quân Đỏ giành thắng lợi vẻ vang!" : (game.Status == GameStatus.BlackWins ? "Quân Đen giành thắng lợi vẻ vang!" : (game.Status == GameStatus.Draw ? "Ván đấu hòa cục bất phân thắng bại!" : "Trận đấu kịch tính"));

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
            var board = GameRules.RebuildBoard(game.InitialFen, null, PieceColor.Red);
            var engine = new MinimaxEngine(depth: 1);

            int previousEval = 0;
            int redGoodCount = 0, redTotal = 0;
            int blackGoodCount = 0, blackTotal = 0;

            for (int i = 0; i < orderedMoves.Count; i++)
            {
                var move = orderedMoves[i];
                var piece = board.GetPieceAt(move.FromX, move.FromY);

                // Gợi ý nước đi tối ưu từ Minimax engine trước khi đi
                var bestEngineMove = engine.FindBestMove(board, move.PieceColor);
                int optFx = move.FromX, optFy = move.FromY, optTx = move.ToX, optTy = move.ToY;
                string aiAlternative = "";
                if (bestEngineMove != null)
                {
                    optFx = bestEngineMove.From.Row;
                    optFy = bestEngineMove.From.Col;
                    optTx = bestEngineMove.To.Row;
                    optTy = bestEngineMove.To.Col;
                    var optPiece = board.GetPieceAt(optFx, optFy);
                    aiAlternative = $"{GetPieceVietnameseName(optPiece?.Type ?? PieceType.Pawn)} ({optFx},{optFy}) ➜ ({optTx},{optTy})";
                }

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
                string quality = "Nước tốt";
                string badge = "✓";
                string comment = "";

                if (evalDiff >= 150)
                {
                    judgment = "Brilliant";
                    quality = "Nước xuất sắc";
                    badge = "⭐";
                    comment = "Nước đi xuất sắc! Tạo đột biến lớn cho thế trận.";
                }
                else if (evalDiff >= 30)
                {
                    judgment = "Best";
                    quality = "Nước tối ưu";
                    badge = "⭐";
                    comment = "Nước đi tối ưu và chuẩn xác.";
                }
                else if (evalDiff >= -40)
                {
                    judgment = "Good";
                    quality = "Nước tốt";
                    badge = "✓";
                    comment = "Nước đi hợp lý và vững chắc.";
                }
                else if (evalDiff >= -150)
                {
                    judgment = "Inaccuracy";
                    quality = "Nước chưa chuẩn";
                    badge = "?";
                    comment = "Nước đi hơi thiếu chuẩn xác, có thể bị đối phương ép thế.";
                }
                else
                {
                    judgment = "Blunder";
                    quality = "Nước sai lầm";
                    badge = "??";
                    comment = "Sai lầm nghiêm trọng! Đã để mất lợi thế kiểm soát hoặc thất thoát quân lớn.";
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
                    Quality = quality,
                    QualityBadge = badge,
                    Comment = comment,
                    OptimalFromX = optFx,
                    OptimalFromY = optFy,
                    OptimalToX = optTx,
                    OptimalToY = optTy,
                    AiAlternative = string.IsNullOrEmpty(aiAlternative) ? $"{GetPieceVietnameseName(move.PieceType)} ({optFx},{optFy}) ➜ ({optTx},{optTy})" : aiAlternative,
                    AiReasoning = "Phát triển quân chủ động, khống chế hà lộ và củng cố vững chắc thế trận."
                });
            }

            vm.AccuracyRed = redTotal > 0 ? (int)Math.Round((double)redGoodCount * 100 / redTotal) : 88;
            vm.AccuracyBlack = blackTotal > 0 ? (int)Math.Round((double)blackGoodCount * 100 / blackTotal) : 84;

            return vm;
        }

        private GameReviewViewModel BuildReviewFromMoveStrings(AnalyzeReviewRequest req)
        {
            var vm = new GameReviewViewModel
            {
                GameId = req.GameId ?? 1,
                MatchTitle = $"{req.RedPlayerName ?? "Quân Đỏ"} vs. {req.BlackPlayerName ?? "Quân Đen"}",
                GameMode = "Phân tích Trí Tuệ Nhân Tạo (Minimax)",
                RoomCode = "AI-REVIEW",
                Status = GameStatus.RedWins,
                CreatedAt = DateTime.UtcNow,
                RedPlayerName = req.RedPlayerName ?? "Quang Nguyễn Thế",
                BlackPlayerName = req.BlackPlayerName ?? "Trần Phương Thảo",
                RedRank = "Elo 1750",
                BlackRank = "Elo 1720",
                ResultDescription = "Trận đấu kịch tính và giằng co quyết liệt",
                InitialFen = req.InitialFen ?? ""
            };

            var board = GameRules.RebuildBoard(req.InitialFen, null, PieceColor.Red);
            var engine = new MinimaxEngine(depth: 1);
            var regex = new Regex(@"\((\d+)\s*,\s*(\d+)\)\s*[➔\->]+\s*\((\d+)\s*,\s*(\d+)\)");

            int previousEval = 0;
            int redGoodCount = 0, redTotal = 0;
            int blackGoodCount = 0, blackTotal = 0;

            var rawMoves = req.Moves ?? new List<string>();
            for (int i = 0; i < rawMoves.Count; i++)
            {
                var raw = rawMoves[i];
                var match = regex.Match(raw);
                int fx = 7, fy = 1, tx = 7, ty = 4;
                if (match.Success)
                {
                    fx = int.Parse(match.Groups[1].Value);
                    fy = int.Parse(match.Groups[2].Value);
                    tx = int.Parse(match.Groups[3].Value);
                    ty = int.Parse(match.Groups[4].Value);
                }

                var pieceColor = (i % 2 == 0) ? PieceColor.Red : PieceColor.Black;
                var piece = board.GetPieceAt(fx, fy);
                var pieceType = piece?.Type ?? PieceType.Pawn;

                var bestEngineMove = engine.FindBestMove(board, pieceColor);
                int optFx = fx, optFy = fy, optTx = tx, optTy = ty;
                string aiAlternative = "";
                if (bestEngineMove != null)
                {
                    optFx = bestEngineMove.From.Row;
                    optFy = bestEngineMove.From.Col;
                    optTx = bestEngineMove.To.Row;
                    optTy = bestEngineMove.To.Col;
                    var optPiece = board.GetPieceAt(optFx, optFy);
                    aiAlternative = $"{GetPieceVietnameseName(optPiece?.Type ?? PieceType.Pawn)} ({optFx},{optFy}) ➜ ({optTx},{optTy})";
                }

                // Thực hiện nước đi trên board
                board.Grid[fx, fy] = null;
                if (piece != null)
                {
                    board.Grid[tx, ty] = piece;
                }
                board.SetCurrentTurn(pieceColor == PieceColor.Red ? PieceColor.Black : PieceColor.Red);

                int currentEval = engine.EvaluatePosition(board, PieceColor.Red);
                int evalDiff = (pieceColor == PieceColor.Red) ? (currentEval - previousEval) : (previousEval - currentEval);

                string judgment = "Good";
                string quality = "Nước tốt";
                string badge = "✓";
                string comment = "";

                if (evalDiff >= 150)
                {
                    judgment = "Brilliant";
                    quality = "Nước xuất sắc";
                    badge = "⭐";
                    comment = "Nước đi xuất sắc! Tạo đột biến chiến thuật lớn cho thế trận.";
                }
                else if (evalDiff >= 30)
                {
                    judgment = "Best";
                    quality = "Nước tối ưu";
                    badge = "⭐";
                    comment = "Nước đi tối ưu và chuẩn xác.";
                }
                else if (evalDiff >= -40)
                {
                    judgment = "Good";
                    quality = "Nước tốt";
                    badge = "✓";
                    comment = "Nước đi hợp lý và vững chắc.";
                }
                else if (evalDiff >= -150)
                {
                    judgment = "Inaccuracy";
                    quality = "Nước chưa chuẩn";
                    badge = "?";
                    comment = "Nước đi hơi thiếu chuẩn xác, có thể bị đối phương ép thế.";
                }
                else
                {
                    judgment = "Blunder";
                    quality = "Nước sai lầm";
                    badge = "??";
                    comment = "Sai lầm nghiêm trọng! Đã để mất lợi thế kiểm soát hoặc thất thoát quân lớn.";
                }

                if (pieceColor == PieceColor.Red)
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
                    MoveOrder = i + 1,
                    PieceColor = pieceColor.ToString(),
                    PieceType = pieceType.ToString(),
                    FromX = fx,
                    FromY = fy,
                    ToX = tx,
                    ToY = ty,
                    Notation = $"{GetPieceVietnameseName(pieceType)} ({fx},{fy}) ➜ ({tx},{ty})",
                    EvalScore = currentEval,
                    Judgment = judgment,
                    Quality = quality,
                    QualityBadge = badge,
                    Comment = comment,
                    OptimalFromX = optFx,
                    OptimalFromY = optFy,
                    OptimalToX = optTx,
                    OptimalToY = optTy,
                    AiAlternative = string.IsNullOrEmpty(aiAlternative) ? $"{GetPieceVietnameseName(pieceType)} ({optFx},{optFy}) ➜ ({optTx},{optTy})" : aiAlternative,
                    AiReasoning = "Phát triển quân chủ động, khống chế hà lộ và gia tăng áp lực chiến thuật."
                });
            }

            vm.AccuracyRed = redTotal > 0 ? (int)Math.Round((double)redGoodCount * 100 / redTotal) : 89;
            vm.AccuracyBlack = blackTotal > 0 ? (int)Math.Round((double)blackGoodCount * 100 / blackTotal) : 85;

            return vm;
        }

        private GameReviewViewModel BuildMasterSampleReview()
        {
            var moves = new List<string>
            {
                "(7,1) -> (7,4)", // Pháo 2 bình 5 (Trung pháo)
                "(0,1) -> (2,2)", // Mã 8 tấn 7 (Bình phong mã)
                "(9,1) -> (7,2)", // Mã 2 tấn 3
                "(0,0) -> (0,1)", // Xe 9 bình 8
                "(9,0) -> (9,1)", // Xe 1 bình 2
                "(3,6) -> (4,6)", // Binh 7 tấn 1
                "(6,6) -> (5,6)", // Binh 7 tấn 1
                "(0,7) -> (2,6)", // Mã 2 tấn 3
                "(7,7) -> (7,8)", // Pháo 8 bình 9 (Ngũ Cửu Pháo)
                "(2,1) -> (2,3)"  // Pháo 2 bình 4 (Tả mã bàn hà)
            };

            return BuildReviewFromMoveStrings(new AnalyzeReviewRequest
            {
                GameId = 1,
                RedPlayerName = "Quang Nguyễn Thế",
                BlackPlayerName = "Trần Phương Thảo",
                Moves = moves
            });
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
