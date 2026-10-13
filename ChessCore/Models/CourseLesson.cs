namespace ChessCore.Models
{
    public class CourseLesson
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string LessonType { get; set; } = "theory";
        public string? BoardPosition { get; set; }
        public int DisplayOrder { get; set; }

        public CourseModule Module { get; set; } = null!;
        public ICollection<CourseQuizQuestion> QuizQuestions { get; set; } = new List<CourseQuizQuestion>();
        public ICollection<UserCourseProgress> Progresses { get; set; } = new List<UserCourseProgress>();
    }
}
