using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ChessCore.Data;
using ChessCore.Services;
using ChessCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChessCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly StatisticsService _statistics;

        public HomeController(AppDbContext context, StatisticsService statistics)
        {
            _context = context;
            _statistics = statistics;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new HomeViewModel();

            // 1. Thong tin nguoi dung hien tai (neu co dang nhap)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                vm.CurrentUser = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
                if (vm.CurrentUser != null)
                {
            var modes = await _statistics.ModesAsync(vm.CurrentUser.Id);
            var playable = modes.Where(r => r.Mode == GameModes.Online || r.Mode == GameModes.PvE).ToList();
            vm.CurrentUser.GamesPlayed = playable.Sum(r => r.Finished);
            vm.CurrentUser.Wins = playable.Sum(r => r.Wins);
            vm.CurrentUser.Losses = playable.Sum(r => r.Losses);
            vm.CurrentUser.Draws = playable.Sum(r => r.Draws);
                    vm.PuzzlesSolved = await _context.PuzzleProgresses
                        .CountAsync(p => p.UserId == userId && p.CompletedAt != null);

                    vm.LessonsCompleted = await _context.UserCourseProgresses
                        .CountAsync(p => p.UserId == userId && p.IsCompleted);

                    if (vm.CurrentUser.GamesPlayed > 0)
                    {
                        vm.WinRate = Math.Round((double)vm.CurrentUser.Wins / vm.CurrentUser.GamesPlayed * 100, 1);
                    }
                }
            }

            // 2. Thong ke tong the he thong
            vm.TotalUsers = await _context.Users.CountAsync();
            vm.TotalGames = await _context.Games.CountAsync();
            vm.TotalPuzzles = await _context.Puzzles.CountAsync();
            vm.TotalLessons = await _context.CourseLessons.CountAsync();

            // 3. Bang xep hang Top 5 ky thu
            vm.TopPlayers = await _context.Users
                .OrderByDescending(u => u.Elo)
                .ThenBy(u => u.Id)
                .Take(5)
                .Select(u => new TopPlayerDto
                {
                    Username = u.Username,
                    Elo = u.Elo,
                    Wins = _context.Games.Count(g => g.GameMode == GameModes.Online && ((g.RedPlayerId == u.Id && g.Status == ChessCore.Enums.GameStatus.RedWins) || (g.BlackPlayerId == u.Id && g.Status == ChessCore.Enums.GameStatus.BlackWins))),
                    GamesPlayed = _context.Games.Count(g => g.GameMode == GameModes.Online && (g.RedPlayerId == u.Id || g.BlackPlayerId == u.Id) && (g.Status == ChessCore.Enums.GameStatus.RedWins || g.Status == ChessCore.Enums.GameStatus.BlackWins || g.Status == ChessCore.Enums.GameStatus.Draw))
                })
                .ToListAsync();

            // 4. Lay 1 the co ngau nhien hoac the co dau tien lam Daily Puzzle
            var dailyPuzzle = await _context.Puzzles
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();

            if (dailyPuzzle != null)
            {
                vm.DailyPuzzleId = dailyPuzzle.Id;
                vm.DailyPuzzleTitle = dailyPuzzle.Title;
                vm.DailyPuzzleCategory = dailyPuzzle.MateIn > 0 ? $"Sát pháp {dailyPuzzle.MateIn} nước" : "Cờ thế căn bản";
                vm.DailyPuzzleFen = dailyPuzzle.Fen;
            }

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? id)
        {
            var statusCode = id ?? HttpContext.Response.StatusCode;
            if (statusCode == 0 || statusCode == 200) statusCode = 404;

            var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error != null)
            {
                ViewBag.ErrorMessage = $"{exceptionFeature.Error.GetType().Name}: {exceptionFeature.Error.Message}";
            }

            ViewBag.StatusCode = statusCode;
            return View("~/Views/Shared/Error.cshtml");
        }
    }

    public class HomeViewModel
    {
        public User? CurrentUser { get; set; }
        public int PuzzlesSolved { get; set; }
        public int LessonsCompleted { get; set; }
        public double WinRate { get; set; }

        public int TotalUsers { get; set; }
        public int TotalGames { get; set; }
        public int TotalPuzzles { get; set; }
        public int TotalLessons { get; set; }

        public List<TopPlayerDto> TopPlayers { get; set; } = new();

        public int DailyPuzzleId { get; set; }
        public string DailyPuzzleTitle { get; set; } = "Sát cục kinh điển";
        public string DailyPuzzleCategory { get; set; } = "Sát cục";
        public string DailyPuzzleFen { get; set; } = "";
    }

    public class TopPlayerDto
    {
        public string Username { get; set; } = "";
        public int Elo { get; set; }
        public int Wins { get; set; }
        public int GamesPlayed { get; set; }
    }
}
