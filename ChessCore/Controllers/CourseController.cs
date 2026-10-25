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

        // ==========================================
        // REST API CHO MOBILE APP & WEB INTEGRATION
        // ==========================================
        [AllowAnonymous]
        [HttpGet("/api/courses")]
        public async Task<IActionResult> GetCoursesApi()
        {
            try
            {
                var modules = await _context.CourseModules
                    .AsNoTracking()
                    .Include(m => m.Lessons.OrderBy(l => l.DisplayOrder))
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new
                    {
                        id = m.Id,
                        title = m.Title,
                        description = m.Description,
                        displayOrder = m.DisplayOrder,
                        totalLessons = m.Lessons.Count,
                        lessons = m.Lessons.Select(l => new
                        {
                            id = l.Id,
                            moduleId = l.ModuleId,
                            title = l.Title,
                            description = l.Description,
                            lessonType = l.LessonType,
                            boardPosition = l.BoardPosition,
                            displayOrder = l.DisplayOrder
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new { total = modules.Count, modules });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi nạp bài học kỳ đạo: " + ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("/api/courses/lessons/{id}")]
        public async Task<IActionResult> GetLessonDetailApi(int id)
        {
            try
            {
                var lesson = await _context.CourseLessons
                    .AsNoTracking()
                    .Include(l => l.QuizQuestions.OrderBy(q => q.DisplayOrder))
                    .SingleOrDefaultAsync(l => l.Id == id);

                if (lesson == null) return NotFound(new { message = "Không tìm thấy bài học." });

                return Ok(new
                {
                    id = lesson.Id,
                    moduleId = lesson.ModuleId,
                    title = lesson.Title,
                    description = lesson.Description,
                    content = lesson.Content,
                    lessonType = lesson.LessonType,
                    boardPosition = lesson.BoardPosition,
                    questions = lesson.QuizQuestions.Select(q => new
                    {
                        id = q.Id,
                        question = q.Question,
                        optionsJson = q.OptionsJson,
                        correctAnswer = q.CorrectAnswer,
                        explanation = q.Explanation
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi nạp chi tiết bài học: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProgress([FromBody] CourseProgressRequest request)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                // Khách chưa đăng nhập: Trả về trạng thái hoàn thành phiên học tạm thời, không lưu DB
                return Ok(new { progress = new { isCompleted = request.IsCompleted, bestQuizScore = request.QuizScore }, isGuest = true });
            }
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
