using System;

namespace ChessCore.Models
{
    public class GameAnalysis
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int AccuracyRed { get; set; } = 0;
        public int AccuracyBlack { get; set; } = 0;
        public int? TurningPointMoveOrder { get; set; }
        public int BrilliantCount { get; set; } = 0;
        public int BestCount { get; set; } = 0;
        public int GoodCount { get; set; } = 0;
        public int InaccuracyCount { get; set; } = 0;
        public int BlunderCount { get; set; } = 0;
        public string? SummaryReviewJson { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Game Game { get; set; } = null!;
    }
}
