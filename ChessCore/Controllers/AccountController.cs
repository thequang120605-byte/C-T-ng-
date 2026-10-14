using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Models;
using ChessCore.ViewModels;

namespace ChessCore.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                return await _context.Users.FindAsync(userId);
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Đếm số cờ thế đã giải
            int puzzlesSolved = await _context.PuzzleProgresses
                .CountAsync(p => p.UserId == user.Id && p.CompletedAt != null);

            // Đếm số module khóa học hoàn thành
            var completedLessonIds = await _context.UserCourseProgresses
                .Where(p => p.UserId == user.Id && p.IsCompleted)
                .Select(p => p.LessonId)
                .ToListAsync();

            var modules = await _context.CourseModules
                .Include(m => m.Lessons)
                .ToListAsync();

            int completedCourses = modules.Count(m => m.Lessons.Count > 0 && m.Lessons.All(l => completedLessonIds.Contains(l.Id)));

            double winRate = user.GamesPlayed > 0 ? Math.Round((double)user.Wins / user.GamesPlayed * 100, 1) : 0;

            // Hệ thống Danh hiệu & Huy hiệu Thành tựu (Achievements & Badges)
            var badges = new List<AccountBadgeItem>
            {
                new AccountBadgeItem
                {
                    Id = "first_game",
                    Title = "Khai Cục Đầu Tiên",
                    Description = "Hoàn thành ván cờ đầu tiên trên hệ thống",
                    Icon = "fa-chess-board",
                    Color = "#3b82f6",
                    IsUnlocked = user.GamesPlayed >= 1,
                    ProgressText = $"{Math.Min(user.GamesPlayed, 1)}/1 ván"
                },
                new AccountBadgeItem
                {
                    Id = "veteran",
                    Title = "Kỳ Thủ Dày Dạn",
                    Description = "Tham gia từ 10 ván cờ trở lên",
                    Icon = "fa-shield-halved",
                    Color = "#6366f1",
                    IsUnlocked = user.GamesPlayed >= 10,
                    ProgressText = $"{Math.Min(user.GamesPlayed, 10)}/10 ván"
                },
                new AccountBadgeItem
                {
                    Id = "first_win",
                    Title = "Khởi Hoàn Ca",
                    Description = "Giành chiến thắng ván cờ đầu tiên",
                    Icon = "fa-trophy",
                    Color = "#eab308",
                    IsUnlocked = user.Wins >= 1,
                    ProgressText = $"{Math.Min(user.Wins, 1)}/1 chiến thắng"
                },
                new AccountBadgeItem
                {
                    Id = "grandmaster",
                    Title = "Chiến Tướng Bất Bại",
                    Description = "Đạt từ 5 chiến thắng trở lên",
                    Icon = "fa-crown",
                    Color = "#f59e0b",
                    IsUnlocked = user.Wins >= 5,
                    ProgressText = $"{Math.Min(user.Wins, 5)}/5 chiến thắng"
                },
                new AccountBadgeItem
                {
                    Id = "puzzle_solver",
                    Title = "Nhập Môn Phá Trận",
                    Description = "Giải thành công ít nhất 1 thế cờ",
                    Icon = "fa-puzzle-piece",
                    Color = "#10b981",
                    IsUnlocked = puzzlesSolved >= 1,
                    ProgressText = $"{Math.Min(puzzlesSolved, 1)}/1 thế cờ"
                },
                new AccountBadgeItem
                {
                    Id = "puzzle_master",
                    Title = "Bậc Thầy Cờ Thế",
                    Description = "Hóa giải thành công 5 thế cờ hóc búa",
                    Icon = "fa-wand-magic-sparkles",
                    Color = "#8b5cf6",
                    IsUnlocked = puzzlesSolved >= 5,
                    ProgressText = $"{Math.Min(puzzlesSolved, 5)}/5 thế cờ"
                },
                new AccountBadgeItem
                {
                    Id = "course_graduate",
                    Title = "Hàn Lâm Kỳ Viện",
                    Description = "Hoàn thành toàn bộ 4 Module khóa học căn bản",
                    Icon = "fa-graduation-cap",
                    Color = "#ec4899",
                    IsUnlocked = completedCourses >= 4,
                    ProgressText = $"{completedCourses}/4 Module"
                },
                new AccountBadgeItem
                {
                    Id = "elo_expert",
                    Title = "Danh Bất Hư Truyền",
                    Description = "Đạt cột mốc 1300 điểm Elo danh giá",
                    Icon = "fa-star",
                    Color = "#f97316",
                    IsUnlocked = user.Elo >= 1300,
                    ProgressText = $"{user.Elo}/1300 Elo"
                }
            };

            var model = new AccountProfileViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                PlayerId = user.PlayerId,
                Role = user.Role,
                Elo = user.Elo,
                GamesPlayed = user.GamesPlayed,
                Wins = user.Wins,
                Losses = user.Losses,
                Draws = user.Draws,
                WinRate = winRate,
                PuzzlesSolved = puzzlesSolved,
                CompletedCourses = completedCourses,
                CreatedAt = user.CreatedAt,
                Badges = badges
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUsername(UpdateProfileViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                TempData["AccountError"] = "Tên hiển thị không hợp lệ (từ 3 đến 30 ký tự, không chứa ký tự đặc biệt).";
                return RedirectToAction("Index");
            }

            var newUsername = model.Username.Trim();
            if (string.Equals(user.Username, newUsername, StringComparison.OrdinalIgnoreCase))
            {
                TempData["AccountInfo"] = "Tên hiển thị không thay đổi.";
                return RedirectToAction("Index");
            }

            // Kiểm tra trùng username
            bool exists = await _context.Users.AnyAsync(u => u.Id != user.Id && u.Username == newUsername);
            if (exists)
            {
                TempData["AccountError"] = "Tên hiển thị này đã được người khác sử dụng. Vui lòng chọn tên khác.";
                return RedirectToAction("Index");
            }

            user.Username = newUsername;
            await _context.SaveChangesAsync();

            // Làm mới Cookie Authentication với Username mới
            await RefreshSignInCookieAsync(user);

            TempData["AccountSuccess"] = "Cập nhật tên hiển thị thành công!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                TempData["AccountError"] = "Thông tin đổi mật khẩu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Index");
            }

            // Kiểm tra mật khẩu hiện tại
            if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                TempData["AccountError"] = "Mật khẩu hiện tại không chính xác.";
                return RedirectToAction("Index");
            }

            user.PasswordHash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["AccountSuccess"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }

        private async Task RefreshSignInCookieAsync(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "Player"),
                new Claim("PlayerId", user.PlayerId),
                new Claim("Elo", user.Elo.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            return HashPassword(inputPassword) == storedHash;
        }
    }
}
