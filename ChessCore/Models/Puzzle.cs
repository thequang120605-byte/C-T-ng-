using System.ComponentModel.DataAnnotations;

namespace ChessCore.Models
{
    /// <summary>
    /// Một bài Cờ thế (chiếu bí trong N nước). Đáp án không lưu cứng:
    /// server dùng MateSolver để kiểm tra nước đi của người chơi.
    /// </summary>
    public class Puzzle
    {
        public int Id { get; set; }

        /// <summary>Thứ tự trên bản đồ màn chơi (1, 2, 3, ...).</summary>
        public int Level { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Thế cờ (FEN). Bên đi trước là bên phải chiếu bí.</summary>
        [Required, MaxLength(200)]
        public string Fen { get; set; } = string.Empty;

        /// <summary>Số nước chiếu bí (của bên tấn công), 1 hoặc 2.</summary>
        public int MateIn { get; set; } = 1;

        /// <summary>1 = Dễ, 2 = Vừa, 3 = Khó.</summary>
        public int Difficulty { get; set; } = 1;
    }
}
