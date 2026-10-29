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
        public async Task<IActionResult> Export()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
            return File(StatisticsService.Csv(await _statistics.ModesAsync(userId), false, await _statistics.LearningAsync(userId)),
                "text/csv; charset=utf-8", "thong-ke-ca-nhan.csv");
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
            int totalFinished = currentUser.Wins + currentUser.Draws + currentUser.Losses;
            vm.WinRate = totalFinished > 0 
                ? Math.Round((double)currentUser.Wins / totalFinished * 100, 1) 
                : (currentUser.GamesPlayed > 0 ? Math.Round((double)currentUser.Wins / currentUser.GamesPlayed * 100, 1) : 0);

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

            // Fetch Recent Games (Toàn bộ lịch sử ván đấu đã tham gia)
            var recentGames = await _context.Games
                .Include(g => g.RedPlayer)
                .Include(g => g.BlackPlayer)
                .Include(g => g.Moves)
                .Where(g => (g.RedPlayerId == userId || g.BlackPlayerId == userId) && g.Status != GameStatus.WaitingForPlayers)
                .OrderByDescending(g => g.CreatedAt)
                .Take(100)
                .ToListAsync();

            foreach (var g in recentGames)
            {
                bool isRed = g.RedPlayerId == userId;
                var opponent = isRed ? g.BlackPlayer : g.RedPlayer;
                string opponentName = (g.GameMode == GameModes.Online || g.GameMode.StartsWith("Online"))
                    ? opponent?.Username ?? "Người chơi khác"
                    : g.GameMode == GameModes.PvE ? "Máy (AI)" : GameModes.Label(g.GameMode);
                int? opponentElo = null; // Historical opponent rating was not stored.
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

                vm.RecentGames.Add(new GameHistoryItem
                {
                    GameId = g.Id,
                    OpponentName = opponentName,
                    OpponentElo = opponentElo,
                    PlayerElo = null,
                    GameMode = mode,
                    PlayedAt = g.CreatedAt,
                    TotalMoves = g.Moves.Count,
                    Result = resultClass,
                    ResultText = resultText
                });
            }

            var recentIds = vm.RecentGames.Select(g => g.GameId).ToArray();
            var ratingForGames = await _context.UserEloHistories.AsNoTracking()
                .Where(h => h.UserId == userId && h.GameId.HasValue && recentIds.Contains(h.GameId.Value))
                .OrderBy(h => h.Id).ToListAsync();
            foreach (var item in vm.RecentGames)
            {
                var rating = ratingForGames.LastOrDefault(h => h.GameId == item.GameId);
                item.PlayerElo = rating?.NewElo;
                item.EloChange = rating?.EloChange;
            }
            var eloHistories = await _context.UserEloHistories.AsNoTracking()
                .Where(h => h.UserId == userId).OrderByDescending(h => h.RecordedAt)
                .ThenByDescending(h => h.Id).Take(30).ToListAsync();

            if (eloHistories.Count > 0)
            {
                var sorted = eloHistories.OrderBy(h => h.RecordedAt).ThenBy(h => h.Id).ToList();
                vm.EloHistory.Add(new EloPointDto
                {
                    DateLabel = "Khởi đầu",
                    Elo = sorted.First().OldElo,
                    Change = 0,
                    Reason = "Mốc Elo khởi đầu"
                });

                foreach (var h in sorted)
                {
                    vm.EloHistory.Add(new EloPointDto
                    {
                        DateLabel = h.RecordedAt.ToString("dd/MM HH:mm"),
                        Elo = h.NewElo,
                        Change = h.EloChange,
                        Reason = h.Reason ?? "Ván đấu Online"
                    });
                }
            }
            else
            {
                vm.EloHistory.Add(new EloPointDto
                {
                    DateLabel = "Hiện tại",
                    Elo = currentUser.Elo,
                    Change = 0,
                    Reason = "Chưa có lịch sử biến động được ghi nhận"
                });
            }

            return View(vm);
        }
    }
}
