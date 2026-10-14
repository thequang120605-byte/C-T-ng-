using System.Security.Claims;
using ChessCore.Data;
using ChessCore.Engine;
using ChessCore.Enums;
using ChessCore.Models;
using ChessCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChessCore.Controllers
{
    public class PuzzleMoveDto
    {
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
    }

    public class PuzzleMovesRequest
    {
        /// <summary>Các nước đi của người chơi từ đầu bài (nước đáp trả do server tự sinh lại).</summary>
        public List<PuzzleMoveDto> Moves { get; set; } = new();
    }

    /// <summary>
    /// API riêng cho chế độ luyện giải Cờ Thế (vượt màn). Không đụng tới API chơi cờ /api/games.
    /// Không lưu trạng thái ván ở server: mỗi lần gọi, server dựng lại thế cờ từ FEN + các nước đã đi.
    /// </summary>
    [ApiController]
    [Route("api/puzzle-run")]
    public class PuzzleRunController : ControllerBase
    {
        private readonly PuzzleCampaignService _campaign;
        private readonly AppDbContext _db;

        public PuzzleRunController(PuzzleCampaignService campaign, AppDbContext db)
        {
            _campaign = campaign;
            _db = db;
        }

        private int? CurrentUserId()
        {
            string? raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out int id) ? id : null;
        }

        private async Task<HashSet<string>> LoadCompleted(int? userId)
        {
            if (userId == null) return new HashSet<string>();

            var keys = await _db.PuzzleProgresses
                .Where(p => p.UserId == userId && p.CompletedAt != null)
                .Select(p => p.PuzzleKey)
                .ToListAsync();

            return new HashSet<string>(keys);
        }

        // =====================================================
        // CATALOG
        // =====================================================

        [HttpGet("catalog")]
        public async Task<IActionResult> Catalog()
        {
            int? userId = CurrentUserId();

            if (!_campaign.Ready)
            {
                return Ok(new
                {
                    ready = false,
                    state = _campaign.AnalysisState,
                    analyzed = _campaign.Analyzed,
                    toAnalyze = _campaign.ToAnalyze,
                });
            }

            var done = await LoadCompleted(userId);

            var levels = PuzzleCampaignService.Levels.Select(def =>
            {
                var items = _campaign.InLevel(def.Level);
                int completed = items.Count(p => done.Contains(p.Key));
                return new
                {
                    level = def.Level,
                    name = def.Name,
                    rule = def.Rule,
                    total = items.Count,
                    required = PuzzleCampaignService.RequiredToUnlockNext(items.Count),
                    completed,
                    keys = items.Select(p => p.Key).ToList(),
                };
            }).ToList();

            return Ok(new
            {
                ready = true,
                loggedIn = userId != null,
                unlockPercent = (int)(PuzzleCampaignService.UnlockRatio * 100),
                levels,
                completed = done,
                stats = _campaign.Stats,
                rejected = _campaign.RejectReasons,
            });
        }

        // =====================================================
        // PUZZLE DETAIL
        // =====================================================

        [HttpGet("puzzles/{key}")]
        public async Task<IActionResult> GetPuzzle(string key)
        {
            var entry = _campaign.Ready ? _campaign.Get(key) : null;
            if (entry == null || entry.Level == 0)
                return NotFound(new { message = "Không tìm thấy bài Cờ Thế này." });

            int? userId = CurrentUserId();
            if (userId != null)
            {
                var done = await LoadCompleted(userId);
                if (!_campaign.IsLevelUnlocked(entry.Level, done))
                    return StatusCode(403, new { message = "Cấp độ này chưa được mở." });
            }

            Board board = Board.FromFen(entry.Fen);
            int levelTotal = _campaign.InLevel(entry.Level).Count;

            return Ok(new
            {
                key = entry.Key,
                source = entry.Source,
                originalId = entry.OriginalIndex + 1,
                originalName = entry.OriginalName,
                title = entry.Title,
                category = entry.Category,
                level = entry.Level,
                levelName = PuzzleCampaignService.Levels[entry.Level - 1].Name,
                number = entry.Number,
                levelTotal,
                mateIn = entry.MateIn,
                attacker = entry.Attacker.ToString(),
                goal = $"{(entry.Attacker == PieceColor.Red ? "Đỏ" : "Đen")} đi trước, chiếu bí trong {entry.MateIn} nước",
                verified = "Lời giải đã được engine xác minh (chiếu bí cưỡng bức trong tối đa " + entry.MateIn + " nước).",
                state = BuildState(board, entry.Attacker, entry.MateIn),
            });
        }

        // =====================================================
        // MOVE
        // =====================================================

        [HttpPost("puzzles/{key}/move")]
        public async Task<IActionResult> Move(string key, [FromBody] PuzzleMovesRequest request)
        {
            var entry = _campaign.Ready ? _campaign.Get(key) : null;
            if (entry == null || entry.Level == 0)
                return NotFound(new { message = "Không tìm thấy bài Cờ Thế này." });

            if (request?.Moves == null || request.Moves.Count == 0 || request.Moves.Count > entry.MateIn)
                return BadRequest(new { message = "Danh sách nước đi không hợp lệ." });

            PieceColor attacker = entry.Attacker;
            Board board = Board.FromFen(entry.Fen);
            int remaining = entry.MateIn;

            // Phát lại các nước đã đi (đúng) trước nước cuối cùng.
            for (int i = 0; i < request.Moves.Count - 1; i++)
            {
                var step = Evaluate(board, attacker, remaining, request.Moves[i]);
                if (step.Result != "correct")
                    return BadRequest(new { message = "Chuỗi nước đi không khớp với bài." });

                board = step.Board!;
                remaining = step.Remaining;
            }

            var last = Evaluate(board, attacker, remaining, request.Moves[^1]);

            int? userId = CurrentUserId();
            bool newlyCompleted = false;

            if (userId != null)
            {
                if (last.Result == "wrong")
                    await RecordAttempt(userId.Value, key);
                else if (last.Result == "solved")
                    newlyCompleted = await RecordCompleted(userId.Value, key);
            }

            string message = last.Result switch
            {
                "illegal" => "Nước đi này không hợp lệ.",
                "wrong" => last.DefenseExplanation ?? "Chưa đúng! Đối phương đã phản đòn hóa giải thế cờ. Hãy thử lại hoặc xem gợi ý.",
                "correct" => "Chính xác! Tiếp tục tìm nước đi kế tiếp.",
                _ => "Chiếu bí! Bạn đã hoàn thành bài này.",
            };

            return Ok(new
            {
                result = last.Result,
                message,
                newlyCompleted,
                reply = last.Reply,
                state = last.Result == "illegal"
                    ? null
                    : (last.Board != null ? BuildState(last.Board, attacker, last.Remaining, last.Result == "solved" || last.Result == "wrong") : null),
                lastMove = request.Moves[^1],
            });
        }

        // =====================================================
        // HINT
        // =====================================================

        [HttpPost("puzzles/{key}/hint")]
        public async Task<IActionResult> Hint(string key, [FromBody] PuzzleMovesRequest request, [FromQuery] string mode = "move")
        {
            var entry = _campaign.Ready ? _campaign.Get(key) : null;
            if (entry == null || entry.Level == 0)
                return NotFound(new { message = "Không tìm thấy bài Cờ Thế này." });

            PieceColor attacker = entry.Attacker;
            Board board = Board.FromFen(entry.Fen);
            int remaining = entry.MateIn;

            foreach (var mv in request?.Moves ?? new List<PuzzleMoveDto>())
            {
                var step = Evaluate(board, attacker, remaining, mv);
                if (step.Result != "correct")
                    return BadRequest(new { message = "Chuỗi nước đi không khớp với bài." });

                board = step.Board!;
                remaining = step.Remaining;
            }

            EngineMove? hint = MateSolver.FindMateMove(board, attacker, remaining);
            if (hint == null)
                return Ok(new { available = false, message = "Chưa có gợi ý cho thế cờ này." });

            // Trừ điểm gợi ý (5 điểm cho gợi ý quân, 10 điểm cho gợi ý nước đi) nếu người dùng đã đăng nhập
            int pointsDeducted = mode == "piece" ? 5 : 10;
            int? userId = CurrentUserId();
            int? currentElo = null;

            if (userId != null)
            {
                var user = await _db.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    user.Elo = Math.Max(0, user.Elo - pointsDeducted);
                    await _db.SaveChangesAsync();
                    currentElo = user.Elo;
                }
            }

            Piece piece = board.Grid[hint.From.Row, hint.From.Col];
            Piece? targetPiece = board.Grid[hint.To.Row, hint.To.Col];
            return Ok(new
            {
                available = true,
                fromRow = hint.From.Row,
                fromCol = hint.From.Col,
                toRow = hint.To.Row,
                toCol = hint.To.Col,
                pieceType = piece.Type.ToString(),
                targetPieceType = targetPiece?.Type.ToString(),
                pointsDeducted,
                remainingElo = currentElo,
                message = mode == "piece"
                    ? $"Gợi ý quân cờ (-{pointsDeducted}đ): hãy xem xét quân {PieceName(piece.Type)} tại ({hint.From.Row}, {hint.From.Col})."
                    : $"Gợi ý nước đi (-{pointsDeducted}đ): hãy đi {PieceName(piece.Type)} từ ({hint.From.Row}, {hint.From.Col}) ➜ ({hint.To.Row}, {hint.To.Col}).",
            });
        }


        // =====================================================
        // HELPERS
        // =====================================================

        private class StepResult
        {
            public string Result { get; set; } = "illegal";
            public Board? Board { get; set; }
            public int Remaining { get; set; }
            public PuzzleMoveDto? Reply { get; set; }
            public string? DefenseExplanation { get; set; }
        }

        /// <summary>
        /// Chấm một nước của bên tấn công. Đúng nếu hợp lệ theo luật VÀ vẫn còn chiếu bí cưỡng bức
        /// trong số nước còn lại — nên mọi biến thể đúng đều được chấp nhận, không chỉ một đáp án cố định.
        /// Khi người chơi đi sai (wrong): AI bên phòng thủ sẽ tự động phản đòn để hóa giải/ăn quân.
        /// </summary>
        private static StepResult Evaluate(Board board, PieceColor attacker, int remaining, PuzzleMoveDto mv)
        {
            PieceColor defender = GameRules.Opponent(attacker);

            bool legal = GameRules.GetAllLegalMoves(board, attacker).Any(m =>
                m.From.Row == mv.FromRow && m.From.Col == mv.FromCol &&
                m.To.Row == mv.ToRow && m.To.Col == mv.ToCol);

            if (!legal) return new StepResult { Result = "illegal" };

            Board next = board.Clone();
            next.ApplyMove(new Position(mv.FromRow, mv.FromCol), new Position(mv.ToRow, mv.ToCol));

            var defenderMoves = GameRules.GetAllLegalMoves(next, defender);
            if (defenderMoves.Count == 0)
                return new StepResult { Result = "solved", Board = next, Remaining = 0 };

            int left = remaining - 1;
            bool isMate = left >= 1 && MateSolver.IsForcedMate(next, attacker, left);

            if (!isMate)
            {
                // NƯỚC ĐI SAI: AI phòng thủ phản đòn (ăn quân hoặc hóa giải tối ưu)
                var counterMove = MateSolver.PickBestCounterAttack(next, defender, new Position(mv.ToRow, mv.ToCol));
                if (counterMove != null)
                {
                    Board afterCounter = next.Clone();
                    Piece? capturedPiece = next.GetPieceAt(counterMove.To.Row, counterMove.To.Col);
                    Piece? defenderPiece = next.GetPieceAt(counterMove.From.Row, counterMove.From.Col);
                    afterCounter.ApplyMove(counterMove.From, counterMove.To);

                    string defenseMsg = "Máy đã phản đòn hóa giải nước đi của bạn.";
                    if (counterMove.To.Row == mv.ToRow && counterMove.To.Col == mv.ToCol && defenderPiece != null)
                    {
                        defenseMsg = $"Chưa chính xác! {PieceName(defenderPiece.Type)} đối phương đã lập tức ăn lại quân của bạn.";
                    }
                    else if (capturedPiece != null && defenderPiece != null)
                    {
                        defenseMsg = $"Chưa chính xác! {PieceName(defenderPiece.Type)} đối phương đã chớp thời cơ ăn {PieceName(capturedPiece.Type)} của bạn.";
                    }
                    else if (MoveGenerator.CheckIfInCheck(afterCounter, attacker) && defenderPiece != null)
                    {
                        defenseMsg = $"Chưa chính xác! {PieceName(defenderPiece.Type)} đối phương đã phản chiếu Tướng của bạn.";
                    }

                    return new StepResult
                    {
                        Result = "wrong",
                        Board = afterCounter,
                        Remaining = left,
                        Reply = new PuzzleMoveDto
                        {
                            FromRow = counterMove.From.Row,
                            FromCol = counterMove.From.Col,
                            ToRow = counterMove.To.Row,
                            ToCol = counterMove.To.Col
                        },
                        DefenseExplanation = defenseMsg
                    };
                }

                return new StepResult { Result = "wrong" };
            }

            EngineMove? best = MateSolver.PickBestDefense(next, attacker, left);
            if (best == null)
                return new StepResult { Result = "wrong" };

            Board afterReply = next.Clone();
            afterReply.ApplyMove(best.From, best.To);

            return new StepResult
            {
                Result = "correct",
                Board = afterReply,
                Remaining = left,
                Reply = new PuzzleMoveDto
                {
                    FromRow = best.From.Row, FromCol = best.From.Col,
                    ToRow = best.To.Row, ToCol = best.To.Col,
                },
            };
        }

        private static object BuildState(Board board, PieceColor attacker, int remaining, bool finished = false)
        {
            var pieces = new List<object>();
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? p = board.Grid[r, c];
                    if (p == null) continue;
                    pieces.Add(new { row = r, col = c, type = p.Type.ToString(), color = p.Color.ToString() });
                }
            }

            var legal = finished
                ? new List<object>()
                : GameRules.GetAllLegalMoves(board, attacker).Select(m => (object)new
                {
                    fromRow = m.From.Row,
                    fromCol = m.From.Col,
                    toRow = m.To.Row,
                    toCol = m.To.Col,
                    isCapture = board.Grid[m.To.Row, m.To.Col] != null,
                }).ToList();

            PieceColor defender = GameRules.Opponent(attacker);

            return new
            {
                pieces,
                legalMoves = legal,
                remaining,
                checkedColor = MoveGenerator.CheckIfInCheck(board, defender)
                    ? defender.ToString()
                    : MoveGenerator.CheckIfInCheck(board, attacker) ? attacker.ToString() : null,
            };
        }

        private static string PieceName(PieceType type) => type switch
        {
            PieceType.King => "Tướng",
            PieceType.Advisor => "Sĩ",
            PieceType.Elephant => "Tượng",
            PieceType.Rook => "Xe",
            PieceType.Cannon => "Pháo",
            PieceType.Horse => "Mã",
            PieceType.Pawn => "Tốt",
            _ => "quân cờ",
        };

        private async Task RecordAttempt(int userId, string key)
        {
            var row = await _db.PuzzleProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.PuzzleKey == key);
            if (row == null)
            {
                row = new PuzzleProgress { UserId = userId, PuzzleKey = key };
                _db.PuzzleProgresses.Add(row);
            }

            row.Attempts++;
            row.UpdatedAt = DateTime.UtcNow;

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException) { /* ghi đồng thời: bỏ qua, chỉ là thống kê */ }
        }

        /// <summary>Ghi hoàn thành đúng một lần. Trả về true nếu đây là lần hoàn thành đầu tiên.</summary>
        private async Task<bool> RecordCompleted(int userId, string key)
        {
            var row = await _db.PuzzleProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.PuzzleKey == key);
            if (row != null && row.CompletedAt != null) return false;

            if (row == null)
            {
                row = new PuzzleProgress { UserId = userId, PuzzleKey = key };
                _db.PuzzleProgresses.Add(row);
            }

            row.CompletedAt = DateTime.UtcNow;
            row.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                // Yêu cầu song song đã ghi trước: coi như đã hoàn thành, không đếm lại.
                return false;
            }
        }
    }
}
