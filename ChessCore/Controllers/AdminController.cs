using ChessCore.Services;
using ChessCore.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Models;
using ChessCore.Enums;

namespace ChessCore.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PuzzleCampaignService _campaign;
        private readonly StatisticsService _statistics;

        public AdminController(AppDbContext context, PuzzleCampaignService campaign, StatisticsService statistics)
        {
            _context = context;
            _campaign = campaign;
            _statistics = statistics;
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

            if (_campaign != null && _campaign.Ready && _campaign.Playable.Count > 0)
            {
                int pCount = await _context.Puzzles.CountAsync();
                if (pCount < 500)
                {
                    await SyncCampaignPuzzlesInternalAsync();
                }
            }

            var users = await _context.Users.OrderByDescending(u => u.Elo).ToListAsync();

            // Thống kê số lượng bài cờ thế đã giải thành công
            var puzzleSolvedCounts = await _context.PuzzleProgresses
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, Solved = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Solved);

            // Thống kê số bài học đã hoàn thành
            var lessonCompletedCounts = await _context.UserCourseProgresses
                .Where(c => c.IsCompleted)
                .GroupBy(c => c.UserId)
                .Select(g => new { UserId = g.Key, Completed = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Completed);

            var allUserDtos = users.Select(u =>
            {
                double winRate = u.GamesPlayed > 0 ? Math.Round((double)u.Wins * 100.0 / u.GamesPlayed, 1) : 0;
                string rankTitle = u.Elo switch
                {
                    >= 1500 => "Đặc cấp Đại sư",
                    >= 1400 => "Kiện tướng",
                    >= 1250 => "Kỳ thủ Giỏi",
                    >= 1150 => "Kỳ thủ Nghiệp dư",
                    >= 1050 => "Người mới chơi",
                    _ => "Tập sự"
                };

                return new AdminUserOverviewDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    PlayerId = u.PlayerId,
                    Elo = u.Elo,
                    Role = u.Role ?? "Player",
                    IsLocked = u.IsLocked,
                    CreatedAt = u.CreatedAt,
                    GamesPlayed = u.GamesPlayed,
                    Wins = u.Wins,
                    Losses = u.Losses,
                    Draws = u.Draws,
                    WinRate = winRate,
                    PuzzleScore = u.PuzzleScore,
                    PuzzlesSolved = puzzleSolvedCounts.TryGetValue(u.Id, out int pCount) ? pCount : 0,
                    LessonsCompleted = lessonCompletedCounts.TryGetValue(u.Id, out int lCount) ? lCount : 0,
                    Title = rankTitle
                };
            }).ToList();

            // 4 chế độ chơi chuẩn chính quy & thống kê chi tiết theo format Dashboard
            var allGames = await _context.Games.Select(g => new {
                g.GameMode,
                g.Status,
                g.RedPlayerId,
                g.BlackPlayerId
            }).ToListAsync();

            var modeStats = new List<AdminModeStatDto>();

            // 1. Online
            var onlineGames = allGames.Where(g => g.GameMode == "Online" || g.GameMode == "OnlinePublic").ToList();
            int onlineFinished = onlineGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw || g.Status == GameStatus.Resigned);
            int onlineWins = onlineGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Resigned);
            int onlineDraws = onlineGames.Count(g => g.Status == GameStatus.Draw);
            int onlineLosses = onlineWins; // Ván đấu có người thắng sẽ có người thua đối ứng
            modeStats.Add(new AdminModeStatDto
            {
                Mode = "Online",
                Label = "Chơi qua mạng (Online)",
                Created = onlineGames.Count,
                InProgress = onlineGames.Count(g => g.Status == GameStatus.InProgress),
                Waiting = onlineGames.Count(g => g.Status == GameStatus.WaitingForPlayers),
                Finished = onlineFinished,
                Wins = onlineWins,
                Losses = onlineLosses,
                Draws = onlineDraws,
                WinRate = onlineFinished > 0 ? Math.Round((double)onlineWins * 100.0 / onlineFinished, 1) : 0
            });

            // 2. PvE (Chơi với máy)
            var pveGames = allGames.Where(g => g.GameMode == "PvE").ToList();
            int pveFinished = pveGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw || g.Status == GameStatus.Resigned);
            int pveWins = pveGames.Count(g => g.Status == GameStatus.RedWins);
            int pveLosses = pveGames.Count(g => g.Status == GameStatus.BlackWins);
            int pveDraws = pveGames.Count(g => g.Status == GameStatus.Draw);
            modeStats.Add(new AdminModeStatDto
            {
                Mode = "PvE",
                Label = "Chơi với máy (PvE)",
                Created = pveGames.Count,
                InProgress = pveGames.Count(g => g.Status == GameStatus.InProgress),
                Waiting = 0,
                Finished = pveFinished,
                Wins = pveWins,
                Losses = pveLosses,
                Draws = pveDraws,
                WinRate = pveFinished > 0 ? Math.Round((double)pveWins * 100.0 / pveFinished, 1) : 0
            });

            // 3. PvP (Hai người cùng máy)
            var pvpGames = allGames.Where(g => g.GameMode == "PvP" || g.GameMode == "Local").ToList();
            int pvpFinished = pvpGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw);
            int pvpWins = pvpGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins);
            int pvpLosses = pvpWins;
            int pvpDraws = pvpGames.Count(g => g.Status == GameStatus.Draw);
            modeStats.Add(new AdminModeStatDto
            {
                Mode = "PvP",
                Label = "Hai người cùng máy (PvP)",
                Created = pvpGames.Count,
                InProgress = pvpGames.Count(g => g.Status == GameStatus.InProgress),
                Waiting = 0,
                Finished = pvpFinished,
                Wins = pvpWins,
                Losses = pvpLosses,
                Draws = pvpDraws,
                WinRate = pvpFinished > 0 ? Math.Round((double)pvpWins * 100.0 / pvpFinished, 1) : 0
            });

            // 4. AiVsAi (Máy với máy)
            var aivsaiGames = allGames.Where(g => g.GameMode == "AiVsAi").ToList();
            int aivsaiFinished = aivsaiGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw);
            int aivsaiWins = aivsaiGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins);
            int aivsaiLosses = aivsaiWins;
            int aivsaiDraws = aivsaiGames.Count(g => g.Status == GameStatus.Draw);
            modeStats.Add(new AdminModeStatDto
            {
                Mode = "AiVsAi",
                Label = "Máy với máy (AiVsAi)",
                Created = aivsaiGames.Count,
                InProgress = aivsaiGames.Count(g => g.Status == GameStatus.InProgress),
                Waiting = 0,
                Finished = aivsaiFinished,
                Wins = aivsaiWins,
                Losses = aivsaiLosses,
                Draws = aivsaiDraws,
                WinRate = aivsaiFinished > 0 ? Math.Round((double)aivsaiWins * 100.0 / aivsaiFinished, 1) : 0
            });

            var modeDist = new Dictionary<string, int>
            {
                ["Online"] = onlineGames.Count,
                ["PvE"] = pveGames.Count,
                ["PvP"] = pvpGames.Count,
                ["AiVsAi"] = aivsaiGames.Count
            };

            int totalMoves = await _context.Moves.CountAsync();
            int totalWins = users.Sum(u => u.Wins);
            int totalDraws = users.Sum(u => u.Draws);
            int totalLosses = users.Sum(u => u.Losses);
            double avgWinRate = (totalWins + totalDraws + totalLosses > 0)
                ? Math.Round((double)totalWins * 100.0 / (totalWins + totalDraws + totalLosses), 1)
                : (allUserDtos.Count > 0 ? Math.Round(allUserDtos.Average(u => u.WinRate), 1) : 0);

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = users.Count,
                TotalGames = await _context.Games.CountAsync(),
                TotalPuzzles = await _context.Puzzles.CountAsync(),
                TotalModules = await _context.CourseModules.CountAsync(),
                TotalLessons = await _context.CourseLessons.CountAsync(),
                TotalMoves = totalMoves,
                TotalWins = totalWins,
                TotalDraws = totalDraws,
                TotalLosses = totalLosses,
                AverageSystemWinRate = avgWinRate,
                GameModesDistribution = modeDist,
                ModeStats = modeStats,
                AllUsers = allUserDtos,
                RecentUsers = users.Take(5).ToList(),
                RecentPuzzles = await _context.Puzzles.OrderByDescending(p => p.Id).Take(5).ToListAsync()
            };

            return View(vm);
        }

        // ==========================================
        // 1B. XUẤT BÁO CÁO EXCEL TOÀN HỆ THỐNG
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            if (!await IsAdminUserAsync())
            {
                return View("AccessDenied");
            }

            var users = await _context.Users.OrderByDescending(u => u.Elo).ToListAsync();

            var puzzleSolvedCounts = await _context.PuzzleProgresses
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, Solved = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Solved);

            var lessonCompletedCounts = await _context.UserCourseProgresses
                .Where(c => c.IsCompleted)
                .GroupBy(c => c.UserId)
                .Select(g => new { UserId = g.Key, Completed = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Completed);

            var allUserDtos = users.Select(u =>
            {
                double winRate = u.GamesPlayed > 0 ? Math.Round((double)u.Wins * 100.0 / u.GamesPlayed, 1) : 0;
                string rankTitle = u.Elo switch
                {
                    >= 1500 => "Đặc cấp Đại sư",
                    >= 1400 => "Kiện tướng",
                    >= 1250 => "Kỳ thủ Giỏi",
                    >= 1150 => "Kỳ thủ Nghiệp dư",
                    >= 1050 => "Người mới chơi",
                    _ => "Tập sự"
                };

                return new AdminUserOverviewDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    PlayerId = u.PlayerId,
                    Elo = u.Elo,
                    Role = u.Role ?? "Player",
                    IsLocked = u.IsLocked,
                    CreatedAt = u.CreatedAt,
                    GamesPlayed = u.GamesPlayed,
                    Wins = u.Wins,
                    Losses = u.Losses,
                    Draws = u.Draws,
                    WinRate = winRate,
                    PuzzleScore = u.PuzzleScore,
                    PuzzlesSolved = puzzleSolvedCounts.TryGetValue(u.Id, out int pCount) ? pCount : 0,
                    LessonsCompleted = lessonCompletedCounts.TryGetValue(u.Id, out int lCount) ? lCount : 0,
                    Title = rankTitle
                };
            }).ToList();

            var allGames = await _context.Games.Select(g => new {
                g.GameMode,
                g.Status,
                g.RedPlayerId,
                g.BlackPlayerId
            }).ToListAsync();

            var modeStats = new List<AdminModeStatDto>();

            var onlineGames = allGames.Where(g => g.GameMode == "Online" || g.GameMode == "OnlinePublic").ToList();
            int onlineFinished = onlineGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw || g.Status == GameStatus.Resigned);
            int onlineWins = onlineGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Resigned);
            int onlineDraws = onlineGames.Count(g => g.Status == GameStatus.Draw);
            modeStats.Add(new AdminModeStatDto
            {
                Mode = "Online",
                Label = "Chơi qua mạng (Online)",
                Created = onlineGames.Count,
                InProgress = onlineGames.Count(g => g.Status == GameStatus.InProgress),
                Waiting = onlineGames.Count(g => g.Status == GameStatus.WaitingForPlayers),
                Finished = onlineFinished,
                Wins = onlineWins,
                Losses = onlineWins,
                Draws = onlineDraws,
                WinRate = onlineFinished > 0 ? Math.Round((double)onlineWins * 100.0 / onlineFinished, 1) : 0
            });

            var pveGames = allGames.Where(g => g.GameMode == "PvE").ToList();
            int pveFinished = pveGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw || g.Status == GameStatus.Resigned);
            int pveWins = pveGames.Count(g => g.Status == GameStatus.RedWins);
            int pveLosses = pveGames.Count(g => g.Status == GameStatus.BlackWins || g.Status == GameStatus.Resigned);
            int pveDraws = pveGames.Count(g => g.Status == GameStatus.Draw);
            modeStats.Add(new AdminModeStatDto
            {
                Mode = "PvE",
                Label = "Chơi với máy (PvE)",
                Created = pveGames.Count,
                InProgress = pveGames.Count(g => g.Status == GameStatus.InProgress),
                Waiting = 0,
                Finished = pveFinished,
                Wins = pveWins,
                Losses = pveLosses,
                Draws = pveDraws,
                WinRate = pveFinished > 0 ? Math.Round((double)pveWins * 100.0 / pveFinished, 1) : 0
            });

            var pvpGames = allGames.Where(g => g.GameMode == "PvP" || g.GameMode == "Local").ToList();
            int pvpFinished = pvpGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw || g.Status == GameStatus.Resigned);
            int pvpWins = pvpGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Resigned);
            int pvpDraws = pvpGames.Count(g => g.Status == GameStatus.Draw);
            modeStats.Add(new AdminModeStatDto
            {
                Mode = "PvP",
                Label = "Hai người cùng máy (PvP)",
                Created = pvpGames.Count,
                InProgress = pvpGames.Count(g => g.Status == GameStatus.InProgress),
                Waiting = 0,
                Finished = pvpFinished,
                Wins = pvpWins,
                Losses = pvpWins,
                Draws = pvpDraws,
                WinRate = pvpFinished > 0 ? Math.Round((double)pvpWins * 100.0 / pvpFinished, 1) : 0
            });

            var aivsaiGames = allGames.Where(g => g.GameMode == "AiVsAi").ToList();
            int aivsaiFinished = aivsaiGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw);
            int aivsaiWins = aivsaiGames.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins);
            int aivsaiDraws = aivsaiGames.Count(g => g.Status == GameStatus.Draw);
            modeStats.Add(new AdminModeStatDto
            {
                Mode = "AiVsAi",
                Label = "Máy với máy (AiVsAi)",
                Created = aivsaiGames.Count,
                InProgress = aivsaiGames.Count(g => g.Status == GameStatus.InProgress),
                Waiting = 0,
                Finished = aivsaiFinished,
                Wins = aivsaiWins,
                Losses = aivsaiWins,
                Draws = aivsaiDraws,
                WinRate = aivsaiFinished > 0 ? Math.Round((double)aivsaiWins * 100.0 / aivsaiFinished, 1) : 0
            });

            var learning = await _statistics.LearningAsync(null);
            var adminName = User.Identity?.Name ?? "Admin";

            var fileBytes = ExcelExportService.ExportAdminReport(modeStats, learning, allUserDtos, adminName);
            var fileName = $"Bao-Cao-Thong-Ke-He-Thong-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // ==========================================
        // 2. QUẢN LÝ CỜ THẾ (PUZZLES)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Puzzles()
        {
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

            if (_campaign != null && _campaign.Ready && _campaign.Playable.Count > 0)
            {
                int pCount = await _context.Puzzles.CountAsync();
                if (pCount < 500)
                {
                    await SyncCampaignPuzzlesInternalAsync();
                }
            }

            var puzzles = await _context.Puzzles.OrderBy(p => p.Level).ThenBy(p => p.Id).ToListAsync();
            return View(puzzles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncCampaignPuzzles()
        {
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

            if (_campaign == null || !_campaign.Ready || _campaign.Playable.Count == 0)
            {
                TempData["ErrorMessage"] = "Kho Cờ Thế Chiến Dịch chưa sẵn sàng hoặc đang nạp dữ liệu. Vui lòng thử lại sau giây lát!";
                return RedirectToAction(nameof(Puzzles));
            }

            int syncedCount = await SyncCampaignPuzzlesInternalAsync();
            TempData["SuccessMessage"] = $"Đã đồng bộ thành công toàn bộ {syncedCount} thế cờ sát pháp chiến dịch vào hệ thống quản lý!";
            return RedirectToAction(nameof(Puzzles));
        }

        private async Task<int> SyncCampaignPuzzlesInternalAsync()
        {
            if (_campaign == null || !_campaign.Ready || _campaign.Playable.Count == 0) return 0;

            var existingPuzzles = await _context.Puzzles.ToListAsync();
            if (existingPuzzles.Any())
            {
                _context.Puzzles.RemoveRange(existingPuzzles);
                await _context.SaveChangesAsync();
            }

            var list = new List<Puzzle>();
            int globalLevel = 1;
            foreach (var p in _campaign.Playable)
            {
                int difficulty = p.Level <= 2 ? 1 : (p.Level <= 4 ? 2 : 3);
                list.Add(new Puzzle
                {
                    Level = globalLevel++,
                    Title = !string.IsNullOrWhiteSpace(p.Title) ? p.Title : $"Thế cờ sát cục #{p.Number}",
                    Description = $"{p.Category} - {p.Pieces} quân ({p.Key})",
                    Fen = p.Fen,
                    MateIn = p.MateIn > 0 ? p.MateIn : 1,
                    Difficulty = difficulty
                });
            }

            await _context.Puzzles.AddRangeAsync(list);
            await _context.SaveChangesAsync();
            return list.Count;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePuzzle(Puzzle puzzle)
        {
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

            if (string.IsNullOrWhiteSpace(puzzle.Title) || string.IsNullOrWhiteSpace(puzzle.Fen))
            {
                TempData["ErrorMessage"] = "Tiêu đề và chuỗi FEN không được để trống!";
                return RedirectToAction(nameof(Puzzles));
            }

            // FIX BUG_015: Kiểm tra tính hợp lệ của chuỗi FEN trước khi thêm cờ thế
            if (!Board.TryParseFen(puzzle.Fen.Trim(), out _, out string fenError))
            {
                TempData["ErrorMessage"] = $"Chuỗi FEN không hợp lệ: {fenError}";
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
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

            var existing = await _context.Puzzles.FindAsync(puzzle.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thế cờ cần chỉnh sửa.";
                return RedirectToAction(nameof(Puzzles));
            }

            // FIX BUG_015: Kiểm tra tính hợp lệ của chuỗi FEN khi chỉnh sửa cờ thế
            if (!Board.TryParseFen(puzzle.Fen.Trim(), out _, out string editFenError))
            {
                TempData["ErrorMessage"] = $"Chuỗi FEN không hợp lệ: {editFenError}";
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
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

            var puzzle = await _context.Puzzles.FindAsync(id);
            if (puzzle == null)
            {
                // FIX BUG_007: Trả về thông báo lỗi 404 khi ID bài cờ thế không tồn tại
                TempData["ErrorMessage"] = $"Không tìm thấy thế cờ #{id} để xóa (404 Not Found).";
                return NotFound(new { message = $"Không tìm thấy thế cờ #{id} để xóa." });
            }

            _context.Puzzles.Remove(puzzle);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa thế cờ #{id} thành công!";
            return RedirectToAction(nameof(Puzzles));
        }

        // ==========================================
        // 3. QUẢN LÝ KHÓA HỌC & BÀI HỌC (COURSES)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Courses()
        {
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

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
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

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
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

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
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

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
        // 4. QUẢN LÝ NGƯỜI DÙNG (USERS)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

            var users = await _context.Users.OrderByDescending(u => u.Elo).ToListAsync();

            // Thống kê số lượng bài cờ thế đã giải thành công
            var puzzleSolvedCounts = await _context.PuzzleProgresses
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, Solved = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Solved);

            // Thống kê số bài học đã hoàn thành
            var lessonCompletedCounts = await _context.UserCourseProgresses
                .Where(c => c.IsCompleted)
                .GroupBy(c => c.UserId)
                .Select(g => new { UserId = g.Key, Completed = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Completed);

            var allUserDtos = users.Select(u =>
            {
                double winRate = u.GamesPlayed > 0 ? Math.Round((double)u.Wins * 100.0 / u.GamesPlayed, 1) : 0;
                string rankTitle = u.Elo switch
                {
                    >= 1500 => "Đặc cấp Đại sư",
                    >= 1400 => "Kiện tướng",
                    >= 1250 => "Kỳ thủ Giỏi",
                    >= 1150 => "Kỳ thủ Nghiệp dư",
                    >= 1050 => "Người mới chơi",
                    _ => "Tập sự"
                };

                return new AdminUserOverviewDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    PlayerId = u.PlayerId,
                    Elo = u.Elo,
                    Role = u.Role ?? "Player",
                    IsLocked = u.IsLocked,
                    CreatedAt = u.CreatedAt,
                    GamesPlayed = u.GamesPlayed,
                    Wins = u.Wins,
                    Losses = u.Losses,
                    Draws = u.Draws,
                    WinRate = winRate,
                    PuzzleScore = u.PuzzleScore,
                    PuzzlesSolved = puzzleSolvedCounts.TryGetValue(u.Id, out int pCount) ? pCount : 0,
                    LessonsCompleted = lessonCompletedCounts.TryGetValue(u.Id, out int lCount) ? lCount : 0,
                    Title = rankTitle
                };
            }).ToList();

            return View(allUserDtos);
        }

        // FIX BUG_014: Tính năng Khóa / Mở khóa tài khoản người dùng
        private IActionResult SafeRedirectBack(string defaultUrl = "/Admin/Users")
        {
            var referer = Request.Headers.Referer.ToString();
            if (string.IsNullOrWhiteSpace(referer))
            {
                return Redirect(defaultUrl);
            }
            return Redirect(referer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockUser(int userId)
        {
            if (!await IsAdminUserAsync())
            {
                Response.StatusCode = 403;
                return View("AccessDenied");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return SafeRedirectBack();
            }

            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Không thể khóa tài khoản có quyền Quản trị viên (Admin).";
                return SafeRedirectBack();
            }

            user.IsLocked = !user.IsLocked;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = user.IsLocked 
                ? $"Đã khóa tài khoản {user.Username} ({user.PlayerId}) thành công!" 
                : $"Đã mở khóa tài khoản {user.Username} ({user.PlayerId}) thành công!";

            return SafeRedirectBack();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdminRole(int userId)
        {
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

            var currentUsername = User.Identity?.Name;
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return SafeRedirectBack();
            }

            // 1. Chống tự hạ quyền chính mình
            if (string.Equals(user.Username, currentUsername, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Thao tác bị từ chối: Bạn không thể tự hạ quyền Quản trị viên (Admin) của chính mình để tránh mất quyền quản trị hệ thống!";
                return SafeRedirectBack();
            }

            // 2. Chống hạ quyền nếu đây là Admin duy nhất còn lại trong toàn hệ thống
            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin");
                if (adminCount <= 1)
                {
                    TempData["ErrorMessage"] = "Thao tác bị từ chối: Đây là tài khoản Quản trị viên (Admin) duy nhất còn lại. Hệ thống bắt buộc phải duy trì ít nhất 1 Quản trị viên!";
                    return SafeRedirectBack();
                }
            }

            // 3. Thực hiện chuyển đổi quyền
            user.Role = string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "Player" : "Admin";
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật quyền của {user.Username} thành: {user.Role}";

            return SafeRedirectBack();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

            TempData["ErrorMessage"] = "Chính sách hệ thống: Không xóa vĩnh viễn tài khoản người dùng để bảo toàn dữ liệu kỳ phổ và lịch sử thi đấu. Vui lòng sử dụng tính năng Khóa tài khoản!";
            return SafeRedirectBack();
        }

        // ==========================================
        // 5. ĐO LƯỜNG & THỰC NGHIỆM HIỆU NĂNG AI (KLCN)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AiBenchmark()
        {
            if (!await IsAdminUserAsync()) { Response.StatusCode = 403; return View("AccessDenied"); }

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

    public class AdminUserOverviewDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public int Elo { get; set; }
        public string Role { get; set; } = "Player";
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double WinRate { get; set; }
        public int PuzzleScore { get; set; }
        public int PuzzlesSolved { get; set; }
        public int LessonsCompleted { get; set; }
        public string Title { get; set; } = "Tập sự";
    }

    public class AdminModeStatDto
    {
        public string Mode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Created { get; set; }
        public int InProgress { get; set; }
        public int Waiting { get; set; }
        public int Finished { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double WinRate { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalGames { get; set; }
        public int TotalPuzzles { get; set; }
        public int TotalModules { get; set; }
        public int TotalLessons { get; set; }
        public int TotalMoves { get; set; }
        public double AverageSystemWinRate { get; set; }
        public int TotalWins { get; set; }
        public int TotalDraws { get; set; }
        public int TotalLosses { get; set; }
        public Dictionary<string, int> GameModesDistribution { get; set; } = new();
        public List<AdminModeStatDto> ModeStats { get; set; } = new();
        public List<AdminUserOverviewDto> AllUsers { get; set; } = new();
        public List<User> RecentUsers { get; set; } = new();
        public List<Puzzle> RecentPuzzles { get; set; } = new();
    }
}
