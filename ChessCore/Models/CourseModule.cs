namespace ChessCore.Models
{
    public class CourseModule
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-book-open";
        public int DisplayOrder { get; set; }

        public ICollection<CourseLesson> Lessons { get; set; } = new List<CourseLesson>();
    }
}
