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

            // Fetch Recent Games
            var recentGames = await _context.Games
                .Include(g => g.RedPlayer)
                .Include(g => g.BlackPlayer)
                .Include(g => g.Moves)
                .Where(g => (g.RedPlayerId == userId || g.BlackPlayerId == userId) && g.Status != GameStatus.WaitingForPlayers)
                .OrderByDescending(g => g.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var g in recentGames)
            {
                bool isRed = g.RedPlayerId == userId;
                var opponent = isRed ? g.BlackPlayer : g.RedPlayer;

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
                    OpponentName = opponent != null ? opponent.Username : "Đối thủ vô danh",
                    OpponentElo = opponent?.Elo,
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
