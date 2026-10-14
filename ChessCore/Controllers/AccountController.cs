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
                CreatedAt = user.CreatedAt
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
