namespace ChessCore.Services
{
    public class PuzzleLevelInfo
    {
        public int Level { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = string.Empty;
        public int CurrentScore { get; set; }
        public int MinScoreForCurrentLevel { get; set; }
        public int ScoreForNextLevel { get; set; }
        public int PointsInCurrentLevel { get; set; }
        public int PointsNeededForCurrentLevel { get; set; }
        public int ProgressPercent { get; set; }
        public bool IsMaxLevel { get; set; }
    }

    /// <summary>
    /// Hệ thống tính điểm và cấp bậc (Level) cho chế độ Cờ Thế:
    /// - Thế cờ càng khó (Cấp 1 -> 5) càng nhận nhiều điểm.
    /// - Dùng điểm tích lũy để sử dụng gợi ý (Gợi ý quân cờ: 150 điểm, Gợi ý nước đi: 300 điểm).
    /// </summary>
    public static class PuzzleScoringService
    {
        public const int HintCostPiece = 150;
        public const int HintCostMove = 300;

        /// <summary>
        /// Điểm thưởng khi giải xong thế cờ theo độ khó (Cấp 1 -> 5).
        /// Thế cờ càng khó thì điểm thưởng càng cao.
        /// </summary>
        public static int GetRewardPoints(int level, bool isFirstClear)
        {
            if (isFirstClear)
            {
                return level switch
                {
                    1 => 100,  // Cấp 1 (Làm quen - 1 nước)
                    2 => 180,  // Cấp 2 (Cơ bản - 1-2 nước)
                    3 => 300,  // Cấp 3 (Trung cấp - 2 nước)
                    4 => 450,  // Cấp 4 (Nâng cao - 2-3 nước)
                    5 => 700,  // Cấp 5 (Chuyên sâu - 3 nước)
                    _ => 150
                };
            }
            else
            {
                // Điểm khuyến khích khi luyện lại bài đã từng giải thành công
                return level switch
                {
                    1 => 20,
                    2 => 30,
                    3 => 50,
                    4 => 75,
                    5 => 100,
                    _ => 25
                };
            }
        }

        /// <summary>
        /// Lấy chi phí điểm của loại gợi ý.
        /// </summary>
        public static int GetHintCost(string mode)
        {
            return string.Equals(mode, "piece", StringComparison.OrdinalIgnoreCase) 
                ? HintCostPiece 
                : HintCostMove;
        }

        /// <summary>
        /// Tính cấp độ (Level) và danh hiệu cờ thế dựa trên tổng điểm tích lũy.
        /// </summary>
        public static PuzzleLevelInfo CalculateLevel(int totalScore)
        {
            if (totalScore < 0) totalScore = 0;

            if (totalScore < 500)
            {
                int min = 0;
                int max = 500;
                int inLevel = totalScore - min;
                int needed = max - min;
                int pct = (int)Math.Clamp((double)inLevel / needed * 100, 0, 100);

                return new PuzzleLevelInfo
                {
                    Level = 1,
                    Title = "Nhập Môn Cờ Thế",
                    Icon = "fa-seedling",
                    BadgeColor = "#94a3b8",
                    CurrentScore = totalScore,
                    MinScoreForCurrentLevel = min,
                    ScoreForNextLevel = max,
                    PointsInCurrentLevel = inLevel,
                    PointsNeededForCurrentLevel = needed,
                    ProgressPercent = pct,
                    IsMaxLevel = false
                };
            }
            else if (totalScore < 1200)
            {
                int min = 500;
                int max = 1200;
                int inLevel = totalScore - min;
                int needed = max - min;
                int pct = (int)Math.Clamp((double)inLevel / needed * 100, 0, 100);

                return new PuzzleLevelInfo
                {
                    Level = 2,
                    Title = "Sơ Cấp Kỳ Thủ",
                    Icon = "fa-chess-pawn",
                    BadgeColor = "#38bdf8",
                    CurrentScore = totalScore,
                    MinScoreForCurrentLevel = min,
                    ScoreForNextLevel = max,
                    PointsInCurrentLevel = inLevel,
                    PointsNeededForCurrentLevel = needed,
                    ProgressPercent = pct,
                    IsMaxLevel = false
                };
            }
            else if (totalScore < 2500)
            {
                int min = 1200;
                int max = 2500;
                int inLevel = totalScore - min;
                int needed = max - min;
                int pct = (int)Math.Clamp((double)inLevel / needed * 100, 0, 100);

                return new PuzzleLevelInfo
                {
                    Level = 3,
                    Title = "Trung Cấp Sát Cục",
                    Icon = "fa-chess-knight",
                    BadgeColor = "#a855f7",
                    CurrentScore = totalScore,
                    MinScoreForCurrentLevel = min,
                    ScoreForNextLevel = max,
                    PointsInCurrentLevel = inLevel,
                    PointsNeededForCurrentLevel = needed,
                    ProgressPercent = pct,
                    IsMaxLevel = false
                };
            }
            else if (totalScore < 4500)
            {
                int min = 2500;
                int max = 4500;
                int inLevel = totalScore - min;
                int needed = max - min;
                int pct = (int)Math.Clamp((double)inLevel / needed * 100, 0, 100);

                return new PuzzleLevelInfo
                {
                    Level = 4,
                    Title = "Cao Thủ Tàn Cuộc",
                    Icon = "fa-chess-rook",
                    BadgeColor = "#f59e0b",
                    CurrentScore = totalScore,
                    MinScoreForCurrentLevel = min,
                    ScoreForNextLevel = max,
                    PointsInCurrentLevel = inLevel,
                    PointsNeededForCurrentLevel = needed,
                    ProgressPercent = pct,
                    IsMaxLevel = false
                };
            }
            else
            {
                return new PuzzleLevelInfo
                {
                    Level = 5,
                    Title = "Đại Sư Cờ Thế",
                    Icon = "fa-crown",
                    BadgeColor = "#ef4444",
                    CurrentScore = totalScore,
                    MinScoreForCurrentLevel = 4500,
                    ScoreForNextLevel = 4500,
                    PointsInCurrentLevel = totalScore - 4500,
                    PointsNeededForCurrentLevel = 0,
                    ProgressPercent = 100,
                    IsMaxLevel = true
                };
            }
        }
    }
}
