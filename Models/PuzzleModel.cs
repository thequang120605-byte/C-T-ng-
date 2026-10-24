namespace ChessCore.Mobile.Models;

public sealed class PuzzleItem
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Goal { get; init; } = "";
    public string Fen { get; init; } = "";
    public string Hint { get; init; } = "";
    public bool IsSolved { get; set; }
}
