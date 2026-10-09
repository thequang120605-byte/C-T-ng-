using System.ComponentModel.DataAnnotations;

namespace ChessCore.Models
{
    /// <summary>
    /// Tiến độ Cờ Thế của người dùng đã đăng nhập: mỗi bài một dòng.
    /// CompletedAt != null nghĩa là đã giải xong (ghi đúng một lần).
    /// </summary>
    public class PuzzleProgress
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        /// <summary>Khóa bài, ví dụ "basic:12".</summary>
        [Required, MaxLength(40)]
        public string PuzzleKey { get; set; } = string.Empty;

        public int Attempts { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
