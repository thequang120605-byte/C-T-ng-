using System;

namespace ChessCore.Models
{
    public class MasterGame
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RedPlayer { get; set; } = string.Empty;
        public string BlackPlayer { get; set; } = string.Empty;
        public string? Event { get; set; }
        public DateTime? EventDate { get; set; }
        public string Result { get; set; } = "1/2-1/2"; // 1-0, 0-1, 1/2-1/2
        public string? OpeningName { get; set; }
        public string? InitialFen { get; set; }
        public string? PgnContent { get; set; }
        public string? MovesJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
