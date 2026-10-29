using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ChessCore.Data;
using ChessCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChessCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new HomeViewModel();

            // 1. Thong tin nguoi dung hien tai (neu co dang nhap)
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                vm.CurrentUser = await _context.Users.FindAsync(userId);
                if (vm.CurrentUser != null)
                {
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
                    Wins = u.Wins,
                    GamesPlayed = u.GamesPlayed
                })
                .ToListAsync();

            // 4. Lay 1 the co ngau nhien hoac the co dau tien lam Daily Puzzle
            var dailyPuzzle = await _context.Puzzles
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();

            if (dailyPuzzle != null)
            {
                vm.DailyPuzzleId = dailyPuzzle.Id;
                vm.DailyPuzzleTitle = CleanDailyPuzzleTitle(dailyPuzzle.Title);
                vm.DailyPuzzleCategory = dailyPuzzle.MateIn > 0 ? $"Sát pháp {dailyPuzzle.MateIn} nước" : "Cờ thế căn bản";
                vm.DailyPuzzleFen = dailyPuzzle.Fen;
            }

            return View(vm);
        }

        private static string CleanDailyPuzzleTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "Sát cục kinh điển";
            var cleaned = title;
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*\([Tt]ướng đối mặt\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*\([Bb]ài\s*\d+\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return cleaned.Trim();
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
