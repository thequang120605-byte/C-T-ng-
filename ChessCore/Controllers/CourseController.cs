using System.Security.Claims;
using ChessCore.Data;
using ChessCore.Models;
using ChessCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChessCore.Controllers
{
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;

        public CourseController(AppDbContext context) => _context = context;

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();

            var modules = await _context.CourseModules
                .AsNoTracking()
                .Include(m => m.Lessons.OrderBy(l => l.DisplayOrder))
                    .ThenInclude(l => l.QuizQuestions.OrderBy(q => q.DisplayOrder))
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            var progress = userId.HasValue
                ? await _context.UserCourseProgresses
                    .Where(p => p.UserId == userId.Value)
                    .ToDictionaryAsync(p => p.LessonId)
                : new Dictionary<int, UserCourseProgress>();

            var lessons = modules.SelectMany(m => m.Lessons).ToList();
            var moduleTwo = modules.SingleOrDefault(m => m.Id == 2);
            var moduleTwoBasicsComplete = moduleTwo is not null &&
                moduleTwo.Lessons.Where(l => l.DisplayOrder <= 9)
                    .All(l => progress.TryGetValue(l.Id, out var p) && p.IsCompleted);
            var moduleTwoSynthesisComplete = moduleTwo is not null &&
                moduleTwo.Lessons.Where(l => l.DisplayOrder >= 10 && l.DisplayOrder <= 12)
                    .All(l => progress.TryGetValue(l.Id, out var p) && p.IsCompleted);
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
                        ,IsLocked = module.Id == 2 &&
                            ((lesson.DisplayOrder >= 10 && lesson.DisplayOrder <= 12 && !moduleTwoBasicsComplete) ||
                             (lesson.DisplayOrder >= 13 && !moduleTwoSynthesisComplete))
                    }).ToList()
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProgress([FromBody] CourseProgressRequest request)
        {
            if (request == null || request.LessonId <= 0 || request.Answers == null)
                return BadRequest("Bài học hoặc đáp án không hợp lệ.");
            var lesson = await _context.CourseLessons.AsNoTracking()
                .Include(l => l.QuizQuestions).SingleOrDefaultAsync(l => l.Id == request.LessonId);
            if (lesson == null) return NotFound("Không tìm thấy bài học.");
            var questions = lesson.QuizQuestions.OrderBy(q => q.DisplayOrder).ToList();
            if (questions.Count == 0) return BadRequest("Bài học chưa có bài kiểm tra.");
            if (request.Answers.Count != questions.Count || questions.Any(q => !request.Answers.ContainsKey(q.Id)))
                return BadRequest("Vui lòng trả lời đủ các câu hỏi.");
            foreach (var q in questions)
            {
                var options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new();
                if (request.Answers[q.Id] < 0 || request.Answers[q.Id] >= options.Count)
                    return BadRequest("Lựa chọn không hợp lệ.");
            }
            var userId = GetUserId();
            // Mirror the module-2 unlock policy on the server, not only in the UI.
            if (userId.HasValue && lesson.ModuleId == 2 && lesson.DisplayOrder >= 10)
            {
                var required = await _context.CourseLessons.AsNoTracking()
                    .Where(l => l.ModuleId == 2 && (lesson.DisplayOrder <= 12
                        ? l.DisplayOrder <= 9 : l.DisplayOrder <= 12))
                    .Select(l => l.Id).ToListAsync();
                var completed = await _context.UserCourseProgresses.AsNoTracking()
                    .Where(p => p.UserId == userId && p.IsCompleted && required.Contains(p.LessonId))
                    .CountAsync();
                if (required.Count == 0 || completed != required.Count) return Forbid();
            }
            int correct = questions.Count(q => request.Answers[q.Id] == q.CorrectAnswer);
            int score = (int)Math.Round(100d * correct / questions.Count);
            bool complete = correct == questions.Count;
            object feedback = questions.Select(q => new { questionId = q.Id, correctAnswer = q.CorrectAnswer,
                isCorrect = request.Answers[q.Id] == q.CorrectAnswer, explanation = q.Explanation }).ToArray();
            if (!userId.HasValue)
                return Ok(new { isGuest = true, score, correct, total = questions.Count,
                    isCompleted = complete, feedback, bestQuizScore = score });
            var progress = await _context.UserCourseProgresses
                .SingleOrDefaultAsync(p => p.UserId == userId.Value && p.LessonId == request.LessonId);
            if (progress == null)
            {
                progress = new UserCourseProgress { UserId = userId.Value, LessonId = request.LessonId };
                _context.UserCourseProgresses.Add(progress);
            }
            progress.QuizAttempts++; // Server counted, never trust a browser counter.
            progress.BestQuizScore = Math.Max(progress.BestQuizScore, score);
            progress.IsCompleted |= complete;
            progress.LastPosition = Math.Max(0, request.LastPosition);
            progress.UpdatedAt = DateTime.UtcNow;
            if (progress.IsCompleted && progress.CompletedAt == null) progress.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { isGuest = false, score, correct, total = questions.Count,
                progress.IsCompleted, progress.BestQuizScore, feedback });
        }

        // =========================================================
        // GIAI ĐOẠN 3: CHỨNG CHỈ HOÀN THÀNH KHÓA HỌC (E-CERTIFICATE)
        // =========================================================
        [HttpGet("Course/Certificate")]
        public async Task<IActionResult> Certificate()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            var modules = await _context.CourseModules
                .Include(m => m.Lessons)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            var totalLessons = modules.SelectMany(m => m.Lessons).Count();
            var completedLessonIds = await _context.UserCourseProgresses
                .Where(p => p.UserId == userId.Value && p.IsCompleted)
                .Select(p => p.LessonId)
                .ToListAsync();

            var completedSet = new HashSet<int>(completedLessonIds);

            // Kiểm tra ràng buộc hoàn thành đủ cả 4 module
            bool allModulesCompleted = modules.Count >= 4 && modules.All(m => 
                m.Lessons.Count > 0 && m.Lessons.All(l => completedSet.Contains(l.Id)));

            bool isEligible = allModulesCompleted && totalLessons > 0;

            if (!isEligible)
            {
                TempData["CertificateLockMessage"] = "Bạn cần hoàn thành đầy đủ cả 4 Module để được cấp Chứng chỉ hoàn thành.";
                return RedirectToAction("Index");
            }

            var vm = new CourseCertificateViewModel
            {
                StudentName = user.Username,
                PlayerId = user.PlayerId,
                Email = user.Email,
                TotalLessons = totalLessons,
                CompletedLessons = completedSet.Count,
                IsEligible = true,
                IssueDate = DateTime.Now,
                CertificateCode = $"CERT-CT-{user.Id:D4}-{DateTime.Now.Year}"
            };

            return View(vm);
        }

        private int? GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public class CourseCertificateViewModel
    {
        public string StudentName { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public string Email { get; set; } = "";
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public bool IsEligible { get; set; }
        public DateTime IssueDate { get; set; }
        public string CertificateCode { get; set; } = "";
    }
}
