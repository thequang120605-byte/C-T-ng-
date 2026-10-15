using System.Collections.Generic;

namespace ChessCore.Models
{
    public class Opening
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // e.g. "ECCO-01", "C60"
        public string Name { get; set; } = string.Empty; // e.g. "Pháo Đầu", "Bình Phong Mã"
        public string? Description { get; set; }
        public string StartingFen { get; set; } = string.Empty;

        // Navigation property
        public List<OpeningBookMove> BookMoves { get; set; } = new();
    }

    public class OpeningBookMove
    {
        public int Id { get; set; }
        public int OpeningId { get; set; }
        public string Fen { get; set; } = string.Empty;
        public string MoveNotation { get; set; } = string.Empty; // e.g. "Pháo 2 bình 5" / "C2.5"
        public int MoveOrder { get; set; }
        public int WinCount { get; set; } = 0;
        public int DrawCount { get; set; } = 0;
        public int LossCount { get; set; } = 0;
        public double WinRate { get; set; } = 0.0;
        public string? Explanation { get; set; }

        // Navigation property
        public Opening Opening { get; set; } = null!;
    }
}
