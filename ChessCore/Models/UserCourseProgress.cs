namespace ChessCore.Models
{
    public class UserCourseProgress
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int LessonId { get; set; }
        public bool IsCompleted { get; set; }
        public int BestQuizScore { get; set; }
        public int QuizAttempts { get; set; }
        public int LastPosition { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public CourseLesson Lesson { get; set; } = null!;
    }
}
