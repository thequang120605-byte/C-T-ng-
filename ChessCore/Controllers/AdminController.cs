using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Models;

namespace ChessCore.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<bool> IsAdminUserAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user != null && string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        // ==========================================
        // 1. DASHBOARD TỔNG QUAN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await IsAdminUserAsync())
            {
                return View("AccessDenied");
            }

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalGames = await _context.Games.CountAsync(),
                TotalPuzzles = await _context.Puzzles.CountAsync(),
                TotalModules = await _context.CourseModules.CountAsync(),
                TotalLessons = await _context.CourseLessons.CountAsync(),
                RecentUsers = await _context.Users.OrderByDescending(u => u.Id).Take(5).ToListAsync(),
                RecentPuzzles = await _context.Puzzles.OrderByDescending(p => p.Id).Take(5).ToListAsync()
            };

            return View(vm);
        }

        // ==========================================
        // 2. QUẢN LÝ CỜ THẾ (PUZZLES)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Puzzles()
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            var puzzles = await _context.Puzzles.OrderBy(p => p.Level).ThenBy(p => p.Id).ToListAsync();
            return View(puzzles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePuzzle(Puzzle puzzle)
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            if (string.IsNullOrWhiteSpace(puzzle.Title) || string.IsNullOrWhiteSpace(puzzle.Fen))
            {
                TempData["ErrorMessage"] = "Tiêu đề và chuỗi FEN không được để trống!";
                return RedirectToAction(nameof(Puzzles));
            }

            if (puzzle.Level <= 0)
            {
                var maxLevel = await _context.Puzzles.MaxAsync(p => (int?)p.Level) ?? 0;
                puzzle.Level = maxLevel + 1;
            }

            _context.Puzzles.Add(puzzle);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm thế cờ: {puzzle.Title} (Màn {puzzle.Level}) thành công!";
            return RedirectToAction(nameof(Puzzles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPuzzle(Puzzle puzzle)
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            var existing = await _context.Puzzles.FindAsync(puzzle.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thế cờ cần chỉnh sửa.";
                return RedirectToAction(nameof(Puzzles));
            }

            existing.Title = puzzle.Title;
            existing.Description = puzzle.Description ?? "";
            existing.Fen = puzzle.Fen;
            existing.Level = puzzle.Level;
            existing.MateIn = puzzle.MateIn;
            existing.Difficulty = puzzle.Difficulty;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật thế cờ ID #{puzzle.Id} thành công!";
            return RedirectToAction(nameof(Puzzles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePuzzle(int id)
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            var puzzle = await _context.Puzzles.FindAsync(id);
            if (puzzle != null)
            {
                _context.Puzzles.Remove(puzzle);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa thế cờ #{id} thành công!";
            }
            return RedirectToAction(nameof(Puzzles));
        }

        // ==========================================
        // 3. QUẢN LÝ KHÓA HỌC & BÀI HỌC (COURSES)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Courses()
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            var modules = await _context.CourseModules
                .Include(m => m.Lessons)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            return View(modules);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLesson(CourseLesson lesson)
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            if (string.IsNullOrWhiteSpace(lesson.Title) || lesson.ModuleId <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tiêu đề và chọn Module cho bài học.";
                return RedirectToAction(nameof(Courses));
            }

            if (lesson.DisplayOrder <= 0)
            {
                var maxOrder = await _context.CourseLessons
                    .Where(l => l.ModuleId == lesson.ModuleId)
                    .MaxAsync(l => (int?)l.DisplayOrder) ?? 0;
                lesson.DisplayOrder = maxOrder + 1;
            }

            _context.CourseLessons.Add(lesson);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm bài học mới: {lesson.Title} thành công!";
            return RedirectToAction(nameof(Courses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(CourseLesson lesson)
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            var existing = await _context.CourseLessons.FindAsync(lesson.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài học cần chỉnh sửa.";
                return RedirectToAction(nameof(Courses));
            }

            existing.Title = lesson.Title;
            existing.Description = lesson.Description ?? "";
            existing.Content = lesson.Content ?? "";
            existing.LessonType = lesson.LessonType;
            existing.BoardPosition = lesson.BoardPosition;
            existing.DisplayOrder = lesson.DisplayOrder;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật bài học #{lesson.Id} ({lesson.Title}) thành công!";
            return RedirectToAction(nameof(Courses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            var lesson = await _context.CourseLessons.FindAsync(id);
            if (lesson != null)
            {
                _context.CourseLessons.Remove(lesson);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa bài học #{id} thành công!";
            }
            return RedirectToAction(nameof(Courses));
        }

        // ==========================================
        // 4. PHÂN QUYỀN NHANH USER (Dành cho Dev / Test)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdminRole(int userId)
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Role = (user.Role == "Admin") ? "Player" : "Admin";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật quyền của {user.Username} thành: {user.Role}";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 5. ĐO LƯỜNG & THỰC NGHIỆM HIỆU NĂNG AI (KLCN)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AiBenchmark()
        {
            if (!await IsAdminUserAsync()) return View("AccessDenied");

            // Chạy kiểm thử mẫu trên thế cờ chuẩn với các độ sâu 1, 2, 3
            var sampleBoard = new ChessCore.Engine.Board();
            var benchmarkResults = new List<BenchmarkResultRow>();

            for (int d = 1; d <= 4; d++)
            {
                var engine = new ChessCore.Engine.MinimaxEngine(d);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var move = engine.FindBestMove(sampleBoard, ChessCore.Enums.PieceColor.Red);
                sw.Stop();

                var stats = engine.LastStats;
                string moveNotation = "N/A";
                if (move != null)
                {
                    var piece = sampleBoard.GetPieceAt(move.From.Row, move.From.Col);
                    string pieceName = piece != null ? piece.Type switch
                    {
                        ChessCore.Enums.PieceType.King => "Tướng",
                        ChessCore.Enums.PieceType.Advisor => "Sĩ",
                        ChessCore.Enums.PieceType.Elephant => "Tượng",
                        ChessCore.Enums.PieceType.Horse => "Mã",
                        ChessCore.Enums.PieceType.Rook => "Xe",
                        ChessCore.Enums.PieceType.Cannon => "Pháo",
                        ChessCore.Enums.PieceType.Pawn => "Tốt",
                        _ => "Quân"
                    } : "Quân";

                    moveNotation = $"{pieceName} ({move.From.Row},{move.From.Col}) -> ({move.To.Row},{move.To.Col})";
                }

                benchmarkResults.Add(new BenchmarkResultRow
                {
                    Depth = d,
                    BestMove = moveNotation,
                    NodesEvaluated = stats.NodesEvaluated,
                    TranspositionHits = stats.TranspositionHits,
                    BetaCutoffs = stats.BetaCutoffs,
                    ElapsedMs = sw.Elapsed.TotalMilliseconds
                });
            }

            var vm = new AiBenchmarkViewModel
            {
                Results = benchmarkResults,
                OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                ProcessorCount = Environment.ProcessorCount,
                ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                TotalMemoryAllocatedMb = Math.Round((double)GC.GetTotalMemory(false) / (1024 * 1024), 2),
                MaxMemoryLimitMb = Math.Round((double)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024), 0)
            };

            return View(vm);
        }
    }

    public class AiBenchmarkViewModel
    {
        public List<BenchmarkResultRow> Results { get; set; } = new();
        public string OsDescription { get; set; } = "";
        public string FrameworkDescription { get; set; } = "";
        public int ProcessorCount { get; set; }
        public string ProcessArchitecture { get; set; } = "";
        public double TotalMemoryAllocatedMb { get; set; }
        public double MaxMemoryLimitMb { get; set; }
    }

    public class BenchmarkResultRow
    {
        public int Depth { get; set; }
        public string BestMove { get; set; } = "";
        public long NodesEvaluated { get; set; }
        public long TranspositionHits { get; set; }
        public long BetaCutoffs { get; set; }
        public double ElapsedMs { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalGames { get; set; }
        public int TotalPuzzles { get; set; }
        public int TotalModules { get; set; }
        public int TotalLessons { get; set; }
        public List<User> RecentUsers { get; set; } = new();
        public List<Puzzle> RecentPuzzles { get; set; } = new();
    }
}
