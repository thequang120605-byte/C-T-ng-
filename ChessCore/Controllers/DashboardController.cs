using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ChessCore.Data;
using ChessCore.ViewModels;
using ChessCore.Enums;

namespace ChessCore.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
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

            // Calculate WinRate
            if (currentUser.GamesPlayed > 0)
            {
                vm.WinRate = System.Math.Round((double)currentUser.Wins / currentUser.GamesPlayed * 100, 1);
            }
            else
            {
                vm.WinRate = 0;
            }

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
                    .ToDictionaryAsync(u => u.Id, u => u.Username);

                int pzRank = 1;
                foreach (var pu in topPuzzleUsers)
                {
                    string uName = pzUsers.TryGetValue(pu.UserId, out var n) ? n : $"Kỳ thủ #{pu.UserId}";
                    if (pu.UserId == userId) uName += " (Bạn)";

                    vm.PuzzleLeaderboard.Add(new PuzzleLeaderboardItem
                    {
                        Rank = pzRank++,
                        Username = uName,
                        SolvedCount = pu.SolvedCount,
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
                bool isOnlinePvP = g.RedPlayerId.HasValue && g.BlackPlayerId.HasValue;

                string opponentName;
                int? opponentElo;
                string mode;

                if (isOnlinePvP)
                {
                    opponentName = opponent != null ? opponent.Username : "Người chơi khác";
                    opponentElo = opponent?.Elo ?? 1200;
                    mode = "PvP Online";
                }
                else
                {
                    opponentName = "Máy (AI)";
                    opponentElo = null; // Máy không có điểm Elo người chơi
                    mode = "Chơi với Máy";
                }

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
                    PlayerElo = currentUser.Elo,
                    GameMode = mode,
                    PlayedAt = g.CreatedAt,
                    TotalMoves = g.Moves.Count,
                    Result = resultClass,
                    ResultText = resultText
                });
            }

            return View(vm);
        }
    }
}
