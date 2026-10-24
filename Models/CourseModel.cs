namespace ChessCore.Mobile.Models;

public sealed class CourseQuizOption
{
    public int Index { get; init; }
    public string Text { get; init; } = "";
}

public sealed class CourseLesson
{
    public int Id { get; init; }
    public int ModuleId { get; init; }
    public string ModuleTitle { get; init; } = "";
    public string LessonTitle { get; init; } = "";
    public string Content { get; init; } = "";
    public string Question { get; init; } = "";
    public List<CourseQuizOption> Options { get; init; } = new();
    public int CorrectOptionIndex { get; init; }
    public string Explanation { get; init; } = "";
}
