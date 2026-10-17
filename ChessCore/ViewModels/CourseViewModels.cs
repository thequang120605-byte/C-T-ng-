using ChessCore.Models;

namespace ChessCore.ViewModels
{
    public class CourseIndexViewModel
    {
        public IReadOnlyList<CourseModuleViewModel> Modules { get; init; } = [];
        public int CompletedLessons { get; init; }
        public int TotalLessons { get; init; }
        public int ProgressPercent => TotalLessons == 0 ? 0 : (int)Math.Round(CompletedLessons * 100d / TotalLessons);
        public int? ContinueLessonId { get; init; }
        public int CompletedModulesCount => Modules.Count(m => m.Lessons.Count > 0 && m.CompletedLessons >= m.Lessons.Count);
        public bool CanUnlockCertificate => Modules.Count >= 4 && Modules.All(m => m.Lessons.Count > 0 && m.CompletedLessons >= m.Lessons.Count);
    }

    public class CourseModuleViewModel
    {
        public CourseModule Module { get; init; } = null!;
        public IReadOnlyList<CourseLessonViewModel> Lessons { get; init; } = [];
        public int CompletedLessons { get; init; }
    }

    public class CourseLessonViewModel
    {
        public CourseLesson Lesson { get; init; } = null!;
        public UserCourseProgress? Progress { get; init; }
        public IReadOnlyList<CourseQuizQuestion> Questions { get; init; } = [];
        public bool IsLocked { get; init; }
    }

    public class CourseProgressRequest
    {
        public int LessonId { get; set; }
        public Dictionary<int, int> Answers { get; set; } = new();
        public bool IsCompleted { get; set; }
        public int QuizScore { get; set; }
        public int QuizAttempts { get; set; }
        public int LastPosition { get; set; }
    }
}
