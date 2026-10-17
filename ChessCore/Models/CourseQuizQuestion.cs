namespace ChessCore.Models
{
    public class CourseQuizQuestion
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string OptionsJson { get; set; } = "[]";
        public int CorrectAnswer { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public CourseLesson Lesson { get; set; } = null!;
    }
}
