using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ChessCore.Data;
using ChessCore.Services;
using ChessCore.ViewModels;
using ChessCore.Enums;

namespace ChessCore.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly StatisticsService _statistics;

        public DashboardController(AppDbContext context, StatisticsService statistics)
        {
            _context = context;
            _statistics = statistics;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Export([FromQuery] string? userIds = null)
        {
            var ids = (userIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out int v) ? v : 0)
                .Where(v => v > 0)
                .Distinct()
                .ToList();

            if (ids.Count <= 1)
            {
                int targetId = ids.Count == 1 ? ids[0] : 0;
                if (targetId == 0)
                {
                    if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out targetId)) return Unauthorized();
                }
                return File(StatisticsService.Csv(await _statistics.ModesAsync(targetId), false, await _statistics.LearningAsync(targetId)),
                    "text/csv; charset=utf-8", "thong-ke-ca-nhan.csv");
            }

            var modes = await _statistics.ModesForUsersAsync(ids);
            var learning = await _statistics.LearningForUsersAsync(ids);
            return File(StatisticsService.Csv(modes, true, learning), "text/csv; charset=utf-8", $"thong-ke-nhom-{ids.Count}-tai-khoan.csv");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExportExcel([FromQuery] string? userIds = null)
        {
            var ids = (userIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out int v) ? v : 0)
                .Where(v => v > 0)
                .Distinct()
                .ToList();

            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            var user = userId > 0 ? await _context.Users.FindAsync(userId) : null;
            var username = user?.Username ?? User.Identity?.Name ?? "Admin";

            if (ids.Count <= 1)
            {
                int targetId = ids.Count == 1 ? ids[0] : userId;
                if (targetId <= 0) return Unauthorized();
                var targetUser = targetId == userId ? user : await _context.Users.FindAsync(targetId);
                var targetName = targetUser?.Username ?? username;
                int elo = targetUser?.Elo ?? 1200;

                var modes = await _statistics.ModesAsync(targetId);
                var learning = await _statistics.LearningAsync(targetId);

                var fileBytes = ExcelExportService.ExportPersonalReport(modes, learning, targetName, elo);
                var fileName = $"Bao-Cao-Thanh-Tich-{targetName}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            else
            {
                var advStats = await _statistics.GetAdvancedStatsAsync(ids);
                var fileBytes = ExcelExportService.ExportAdvancedReport(advStats.ModeStats, advStats.Learning, advStats.Users, username);
                var fileName = $"Bao-Cao-Nhom-{ids.Count}-TaiKhoan-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AdvancedStats([FromBody] AdvancedStatsRequest request)
        {
            var userIds = request?.UserIds ?? new List<int>();
            if (userIds.Count == 0 && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var curId))
            {
                userIds.Add(curId);
            }
            var stats = await _statistics.GetAdvancedStatsAsync(userIds, HttpContext.RequestAborted);
            return Ok(stats);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> AdvancedStats([FromQuery] string? userIds)
        {
            var ids = (userIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out int v) ? v : 0)
                .Where(v => v > 0)
                .Distinct()
                .ToList();
            if (ids.Count == 0 && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var curId))
            {
                ids.Add(curId);
            }
            var stats = await _statistics.GetAdvancedStatsAsync(ids, HttpContext.RequestAborted);
            return Ok(stats);
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null) return RedirectToAction("Login", "Auth");

            var vm = new DashboardViewModel
            {
                CurrentUser = currentUser
            };

            vm.ModeStats = await _statistics.ModesAsync(userId, HttpContext.RequestAborted);
            vm.Learning = await _statistics.LearningAsync(userId, HttpContext.RequestAborted);
            vm.AiDepthStats = await _statistics.GetAiDepthStatsAsync(new[] { userId }, HttpContext.RequestAborted);
            await _statistics.ApplyDisplayCountersAsync(new[] { currentUser });

            var rankedModes = vm.ModeStats.Where(m => m.Mode != "AiVsAi").ToList();
            int rankedFinished = rankedModes.Sum(m => m.Finished);
            int rankedWins = rankedModes.Sum(m => m.Wins);
            vm.WinRate = rankedFinished > 0 
                ? Math.Round((double)rankedWins * 100.0 / rankedFinished, 1) 
                : 0;

            // Puzzles solved
            vm.PuzzlesSolved = await _context.PuzzleProgresses
                .CountAsync(p => p.UserId == userId && p.CompletedAt != null);

            // Fetch Leaderboard
            var topUsers = await _context.Users
                .OrderByDescending(u => u.Elo)
                .ThenBy(u => u.Id)
                .Take(10)
                .ToListAsync();

            int rank = 1;
            bool userInTop = false;
            foreach (var u in topUsers)
            {
                vm.Leaderboard.Add(new LeaderboardItem
                {
                    Rank = rank,
                    Username = u.Username,
                    Elo = u.Elo,
                    IsCurrentUser = (u.Id == userId)
                });
                if (u.Id == userId)
                {
                    userInTop = true;
                    vm.RankIndex = rank;
                }
                rank++;
            }

            // Nạp danh sách toàn bộ tài khoản phục vụ Thống kê nâng cao
            var allUsers = await _context.Users.AsNoTracking()
                .OrderByDescending(u => u.Elo)
                .ThenBy(u => u.Id)
                .ToListAsync();

            await _statistics.ApplyDisplayCountersAsync(allUsers);

            var puzzleSolvedCounts = await _context.PuzzleProgresses.AsNoTracking()
                .Where(p => p.CompletedAt != null)
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var lessonCompletedCounts = await _context.UserCourseProgresses.AsNoTracking()
                .Where(p => p.IsCompleted)
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var allUserIds = allUsers.Select(u => u.Id).ToList();
            var allPveGames = await _context.Games.AsNoTracking()
                .Where(g => (g.GameMode == GameModes.PvE || g.GameMode == "PvE")
                         && ((g.RedPlayerId.HasValue && allUserIds.Contains(g.RedPlayerId.Value))
                          || (g.BlackPlayerId.HasValue && allUserIds.Contains(g.BlackPlayerId.Value))))
                .Select(g => new {
                    g.Id,
                    g.Status,
                    g.RedPlayerId,
                    g.BlackPlayerId,
                    AiDepth = (g.AiDepth.HasValue && g.AiDepth.Value >= 1 && g.AiDepth.Value <= 4) ? g.AiDepth.Value : 1
                })
                .ToListAsync();

            int allRankCounter = 1;
            vm.AllAccounts = allUsers.Select(u =>
            {
                double uWinRate = u.GamesPlayed > 0 ? Math.Round((double)u.Wins * 100.0 / u.GamesPlayed, 1) : 0;
                string rankTitle = u.Elo switch
                {
                    >= 1500 => "Đặc cấp Đại sư",
                    >= 1400 => "Kiện tướng",
                    >= 1250 => "Kỳ thủ Giỏi",
                    >= 1150 => "Kỳ thủ Nghiệp dư",
                    >= 1050 => "Người mới chơi",
                    _ => "Tập sự"
                };

                var uPve = allPveGames.Where(g => g.RedPlayerId == u.Id || g.BlackPlayerId == u.Id).ToList();
                int d1Games = uPve.Count(g => g.AiDepth == 1);
                int d1Wins = uPve.Count(g => g.AiDepth == 1 && ((g.RedPlayerId == u.Id && g.Status == GameStatus.RedWins) || (g.BlackPlayerId == u.Id && g.Status == GameStatus.BlackWins)));
                int d1Losses = uPve.Count(g => g.AiDepth == 1 && ((g.RedPlayerId == u.Id && g.Status == GameStatus.BlackWins) || (g.BlackPlayerId == u.Id && g.Status == GameStatus.RedWins)));
                int d1Draws = uPve.Count(g => g.AiDepth == 1 && g.Status == GameStatus.Draw);
                int d1Fin = d1Wins + d1Losses + d1Draws;

                int d2Games = uPve.Count(g => g.AiDepth == 2);
                int d2Wins = uPve.Count(g => g.AiDepth == 2 && ((g.RedPlayerId == u.Id && g.Status == GameStatus.RedWins) || (g.BlackPlayerId == u.Id && g.Status == GameStatus.BlackWins)));
                int d2Losses = uPve.Count(g => g.AiDepth == 2 && ((g.RedPlayerId == u.Id && g.Status == GameStatus.BlackWins) || (g.BlackPlayerId == u.Id && g.Status == GameStatus.RedWins)));
                int d2Draws = uPve.Count(g => g.AiDepth == 2 && g.Status == GameStatus.Draw);
                int d2Fin = d2Wins + d2Losses + d2Draws;

                int d3Games = uPve.Count(g => g.AiDepth == 3);
                int d3Wins = uPve.Count(g => g.AiDepth == 3 && ((g.RedPlayerId == u.Id && g.Status == GameStatus.RedWins) || (g.BlackPlayerId == u.Id && g.Status == GameStatus.BlackWins)));
                int d3Losses = uPve.Count(g => g.AiDepth == 3 && ((g.RedPlayerId == u.Id && g.Status == GameStatus.BlackWins) || (g.BlackPlayerId == u.Id && g.Status == GameStatus.RedWins)));
                int d3Draws = uPve.Count(g => g.AiDepth == 3 && g.Status == GameStatus.Draw);
                int d3Fin = d3Wins + d3Losses + d3Draws;

                int d4Games = uPve.Count(g => g.AiDepth == 4);
                int d4Wins = uPve.Count(g => g.AiDepth == 4 && ((g.RedPlayerId == u.Id && g.Status == GameStatus.RedWins) || (g.BlackPlayerId == u.Id && g.Status == GameStatus.BlackWins)));
                int d4Losses = uPve.Count(g => g.AiDepth == 4 && ((g.RedPlayerId == u.Id && g.Status == GameStatus.BlackWins) || (g.BlackPlayerId == u.Id && g.Status == GameStatus.RedWins)));
                int d4Draws = uPve.Count(g => g.AiDepth == 4 && g.Status == GameStatus.Draw);
                int d4Fin = d4Wins + d4Losses + d4Draws;

                int pveWins = d1Wins + d2Wins + d3Wins + d4Wins;
                int pveLosses = d1Losses + d2Losses + d3Losses + d4Losses;
                int pveDraws = d1Draws + d2Draws + d3Draws + d4Draws;
                int pveFin = pveWins + pveLosses + pveDraws;

                return new UserAccountOptionDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    PlayerId = u.PlayerId,
                    Elo = u.Elo,
                    Title = rankTitle,
                    GamesPlayed = u.GamesPlayed,
                    Wins = u.Wins,
                    Losses = u.Losses,
                    Draws = u.Draws,
                    WinRate = uWinRate,
                    PuzzlesSolved = puzzleSolvedCounts.TryGetValue(u.Id, out int pCount) ? pCount : 0,
                    LessonsCompleted = lessonCompletedCounts.TryGetValue(u.Id, out int lCount) ? lCount : 0,
                    IsCurrentUser = (u.Id == userId),
                    Rank = allRankCounter++,

                    TotalPveGames = uPve.Count,
                    PveWins = pveWins,
                    PveLosses = pveLosses,
                    PveDraws = pveDraws,
                    PveWinRate = pveFin > 0 ? Math.Round((double)pveWins * 100.0 / pveFin, 1) : 0,

                    Depth1Games = d1Games,
                    Depth1Wins = d1Wins,
                    Depth1Losses = d1Losses,
                    Depth1WinRate = d1Fin > 0 ? Math.Round((double)d1Wins * 100.0 / d1Fin, 1) : 0,

                    Depth2Games = d2Games,
                    Depth2Wins = d2Wins,
                    Depth2Losses = d2Losses,
                    Depth2WinRate = d2Fin > 0 ? Math.Round((double)d2Wins * 100.0 / d2Fin, 1) : 0,

                    Depth3Games = d3Games,
                    Depth3Wins = d3Wins,
                    Depth3Losses = d3Losses,
                    Depth3WinRate = d3Fin > 0 ? Math.Round((double)d3Wins * 100.0 / d3Fin, 1) : 0,

                    Depth4Games = d4Games,
                    Depth4Wins = d4Wins,
                    Depth4Losses = d4Losses,
                    Depth4WinRate = d4Fin > 0 ? Math.Round((double)d4Wins * 100.0 / d4Fin, 1) : 0
                };
            }).ToList();

            // If user is not in top 10, find their actual rank
            if (!userInTop)
            {
                var higherEloCount = await _context.Users.CountAsync(u => u.Elo > currentUser.Elo);
                var sameEloButOlderCount = await _context.Users.CountAsync(u => u.Elo == currentUser.Elo && u.Id < currentUser.Id);
                vm.RankIndex = higherEloCount + sameEloButOlderCount + 1;

                vm.Leaderboard.Add(new LeaderboardItem
                {
                    Rank = vm.RankIndex,
                    Username = currentUser.Username + " (Bạn)",
                    Elo = currentUser.Elo,
                    IsCurrentUser = true
                });
            }
            else
            {
                // Update username text for UI
                var lbItem = vm.Leaderboard.First(i => i.IsCurrentUser);
                lbItem.Username += " (Bạn)";
            }

            // Fetch Puzzle Leaderboard (Top người giải cờ thế nhiều nhất)
            var topPuzzleUsers = await _context.PuzzleProgresses
                .Where(p => p.CompletedAt != null)
                .GroupBy(p => p.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    SolvedCount = g.Count()
                })
                .OrderByDescending(x => x.SolvedCount)
                .Take(10)
                .ToListAsync();

            if (topPuzzleUsers.Any())
            {
                var userIds = topPuzzleUsers.Select(x => x.UserId).ToList();
                var pzUsers = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u);

                int pzRank = 1;
                foreach (var pu in topPuzzleUsers)
                {
                    bool found = pzUsers.TryGetValue(pu.UserId, out var u);
                    string uName = found && u != null ? u.Username : $"Kỳ thủ #{pu.UserId}";
                    if (pu.UserId == userId) uName += " (Bạn)";
                    int uScore = found && u != null ? u.PuzzleScore : 500;
                    var lvInfo = PuzzleScoringService.CalculateLevel(uScore);

                    vm.PuzzleLeaderboard.Add(new PuzzleLeaderboardItem
                    {
                        Rank = pzRank++,
                        Username = uName,
                        SolvedCount = pu.SolvedCount,
                        Score = uScore,
                        LevelTitle = $"Cấp {lvInfo.Level}",
                        IsCurrentUser = (pu.UserId == userId)
                    });
                }
            }

            var allUserRatings = await _context.UserEloHistories.AsNoTracking()
                .Where(h => h.UserId == userId)
                .OrderBy(h => h.RecordedAt)
                .ThenBy(h => h.Id)
                .ToListAsync();

            var gameRatingMap = allUserRatings
                .Where(h => h.GameId.HasValue)
                .GroupBy(h => h.GameId!.Value)
                .ToDictionary(g => g.Key, g => g.Last());

            var ratedGameIds = allUserRatings
                .Where(h => h.GameId.HasValue)
                .Select(h => h.GameId!.Value)
                .ToHashSet();

            // Fetch Recent Games (Toàn bộ lịch sử ván đấu đã tham gia)
            var recentGames = await _context.Games
                .Include(g => g.RedPlayer)
                .Include(g => g.BlackPlayer)
                .Include(g => g.Moves)
                .Where(g => (g.RedPlayerId == userId || g.BlackPlayerId == userId) && g.Status != GameStatus.WaitingForPlayers)
                .OrderByDescending(g => g.ResultsProcessedAt ?? g.LastMoveAt ?? g.CreatedAt)
                .Take(250)
                .ToListAsync();

            // Đảm bảo tất cả các ván đấu được biểu diễn trên Biểu đồ phong độ Elo luôn luôn có mặt trong danh sách lịch sử đấu
            var loadedGameIds = recentGames.Select(g => g.Id).ToHashSet();
            var missingRatedIds = ratedGameIds.Where(id => !loadedGameIds.Contains(id)).ToList();
            if (missingRatedIds.Count > 0)
            {
                var additionalRatedGames = await _context.Games
                    .Include(g => g.RedPlayer)
                    .Include(g => g.BlackPlayer)
                    .Include(g => g.Moves)
                    .Where(g => missingRatedIds.Contains(g.Id))
                    .ToListAsync();
                recentGames.AddRange(additionalRatedGames);
                recentGames = recentGames
                    .OrderByDescending(g => g.ResultsProcessedAt ?? g.LastMoveAt ?? g.CreatedAt)
                    .ToList();
            }

            foreach (var g in recentGames)
            {
                bool isRed = g.RedPlayerId == userId;
                var opponent = isRed ? g.BlackPlayer : g.RedPlayer;
                bool isOnline = g.GameMode == GameModes.Online || g.GameMode.StartsWith("Online", StringComparison.OrdinalIgnoreCase);
                string opponentName = isOnline
                    ? opponent?.Username ?? "Người chơi khác"
                    : g.GameMode == GameModes.PvE ? "Máy (AI)" : GameModes.Label(g.GameMode);
                int? opponentElo = isOnline ? opponent?.Elo : null;
                string mode = GameModes.Label(g.GameMode);

                string resultClass = "draw";
                string resultText = "Hòa";

                if (g.Status == GameStatus.RedWins)
                {
                    if (isRed) { resultClass = "win"; resultText = "Thắng"; }
                    else { resultClass = "loss"; resultText = "Thua"; }
                }
                else if (g.Status == GameStatus.BlackWins)
                {
                    if (!isRed) { resultClass = "win"; resultText = "Thắng"; }
                    else { resultClass = "loss"; resultText = "Thua"; }
                }
                else if (g.Status == GameStatus.InProgress)
                {
                    resultClass = "draw"; resultText = "Đang chơi";
                }

                bool isGameRated = gameRatingMap.TryGetValue(g.Id, out var rating);
                int? playerElo = isGameRated ? rating!.NewElo : (int?)null;
                int? oldElo = isGameRated ? rating!.OldElo : (int?)null;
                int? eloChange = isGameRated ? rating!.EloChange : (int?)null;
                DateTime rawPlayedAt = isGameRated 
                    ? rating!.RecordedAt 
                    : (g.ResultsProcessedAt ?? g.LastMoveAt ?? g.CreatedAt);
                DateTime playedAtVn = rawPlayedAt.AddHours(7);

                string subModeBadge = "Không tính Elo";
                if (g.Status == GameStatus.InProgress)
                {
                    subModeBadge = "Đang thi đấu";
                }
                else if (isOnline)
                {
                    if (isGameRated) subModeBadge = "Online xếp hạng";
                    else if (g.IsRated) subModeBadge = "Chờ quyết toán";
                    else subModeBadge = "Online giao hữu";
                }
                else if (g.GameMode == GameModes.PvE)
                {
                    subModeBadge = "Tập luyện (với AI)";
                }
                else if (g.GameMode == GameModes.Local || g.GameMode == "Local" || g.GameMode == "PvP")
                {
                    subModeBadge = "Hai người cùng máy";
                }
                else if (g.GameMode == GameModes.AiVsAi)
                {
                    subModeBadge = "Máy với máy";
                }

                vm.RecentGames.Add(new GameHistoryItem
                {
                    GameId = g.Id,
                    OpponentName = opponentName,
                    OpponentElo = opponentElo,
                    PlayerElo = playerElo,
                    OldElo = oldElo,
                    NewElo = playerElo,
                    EloChange = eloChange,
                    IsRated = isGameRated,
                    GameMode = mode,
                    SubModeBadge = subModeBadge,
                    PlayedAt = playedAtVn,
                    TotalMoves = g.Moves.Count,
                    Result = resultClass,
                    ResultText = resultText
                });
            }

            var eloHistories = allUserRatings
                .OrderByDescending(h => h.RecordedAt)
                .ThenByDescending(h => h.Id)
                .Take(30)
                .ToList();

            if (eloHistories.Count > 0)
            {
                var sorted = eloHistories.OrderBy(h => h.RecordedAt).ThenBy(h => h.Id).ToList();
                string firstLabel = allUserRatings.Count > 30 ? "Trước giai đoạn hiển thị" : "Mốc khởi đầu";
                vm.EloHistory.Add(new EloPointDto
                {
                    DateLabel = firstLabel,
                    Elo = sorted.First().OldElo,
                    OldElo = sorted.First().OldElo,
                    Change = 0,
                    GameId = null,
                    Reason = allUserRatings.Count > 30 ? "Điểm Elo trước 30 ván gần nhất" : "Mốc Elo khởi đầu"
                });

                foreach (var h in sorted)
                {
                    string reason = h.Reason ?? "Ván đấu Online";
                    if (reason.Contains("?"))
                    {
                        reason = h.EloChange > 0 ? "Thắng trận PvP Online" : (h.EloChange < 0 ? "Thua trận PvP Online" : "Hòa trận PvP Online");
                    }

                    vm.EloHistory.Add(new EloPointDto
                    {
                        DateLabel = h.RecordedAt.AddHours(7).ToString("dd/MM HH:mm"),
                        Elo = h.NewElo,
                        OldElo = h.OldElo,
                        Change = h.EloChange,
                        GameId = h.GameId,
                        Reason = reason
                    });
                }
            }
            else
            {
                vm.EloHistory.Add(new EloPointDto
                {
                    DateLabel = "Hiện tại",
                    Elo = currentUser.Elo,
                    OldElo = currentUser.Elo,
                    Change = 0,
                    GameId = null,
                    Reason = "Chưa có lịch sử biến động được ghi nhận"
                });
            }

            return View(vm);
        }
    }
}
