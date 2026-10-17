using System;

namespace ChessCore.Models
{
    public class UserEloHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? GameId { get; set; }
        public int OldElo { get; set; }
        public int NewElo { get; set; }
        public int EloChange { get; set; }
        public string? Reason { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
        public Game? Game { get; set; }
    }
}
