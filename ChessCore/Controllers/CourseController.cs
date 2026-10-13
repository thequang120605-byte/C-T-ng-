using System.Security.Claims;
using ChessCore.Data;
using ChessCore.Models;
using ChessCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChessCore.Controllers
{
    [Authorize]
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;

        public CourseController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId is null) return RedirectToAction("Login", "Auth");

            var modules = await _context.CourseModules
                .AsNoTracking()
                .Include(m => m.Lessons.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.QuizQuestions.OrderBy(q => q.DisplayOrder))
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            var progress = await _context.UserCourseProgresses
                .Where(p => p.UserId == userId.Value)
                .ToDictionaryAsync(p => p.LessonId);

            var lessons = modules.SelectMany(m => m.Lessons).ToList();
            var vm = new CourseIndexViewModel
            {
                TotalLessons = lessons.Count,
                CompletedLessons = lessons.Count(l => progress.TryGetValue(l.Id, out var p) && p.IsCompleted),
                ContinueLessonId = lessons
                    .Where(l => !progress.TryGetValue(l.Id, out var p) || !p.IsCompleted)
                    .Select(l => (int?)l.Id)
                    .FirstOrDefault(),
                Modules = modules.Select(module => new CourseModuleViewModel
                {
                    Module = module,
                    CompletedLessons = module.Lessons.Count(l => progress.TryGetValue(l.Id, out var p) && p.IsCompleted),
                    Lessons = module.Lessons.Select(lesson => new CourseLessonViewModel
                    {
                        Lesson = lesson,
                        Progress = progress.GetValueOrDefault(lesson.Id),
                        Questions = lesson.QuizQuestions.ToList()
                    }).ToList()
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProgress([FromBody] CourseProgressRequest request)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();
            if (request.LessonId <= 0) return BadRequest("Bài học không hợp lệ.");

            var lessonExists = await _context.CourseLessons.AnyAsync(l => l.Id == request.LessonId);
            if (!lessonExists) return NotFound("Không tìm thấy bài học.");

            var progress = await _context.UserCourseProgresses
                .SingleOrDefaultAsync(p => p.UserId == userId.Value && p.LessonId == request.LessonId);

            if (progress is null)
            {
                progress = new UserCourseProgress
                {
                    UserId = userId.Value,
                    LessonId = request.LessonId
                };
                _context.UserCourseProgresses.Add(progress);
            }

            progress.IsCompleted = request.IsCompleted || progress.IsCompleted;
            progress.BestQuizScore = Math.Max(progress.BestQuizScore, Math.Clamp(request.QuizScore, 0, 100));
            progress.QuizAttempts = Math.Max(progress.QuizAttempts, Math.Max(0, request.QuizAttempts));
            progress.LastPosition = Math.Max(0, request.LastPosition);
            progress.UpdatedAt = DateTime.UtcNow;
            if (progress.IsCompleted && progress.CompletedAt is null)
                progress.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { progress.IsCompleted, progress.BestQuizScore });
        }

        private int? GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
