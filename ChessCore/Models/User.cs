using System;

namespace ChessCore.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        
        // Định danh hiển thị (Ví dụ: CT-12345)
        public string PlayerId { get; set; }
        
        // Phân quyền (Player, Admin, Expert)
        public string Role { get; set; } = "Player";
        
        // Thống kê
        public int Elo { get; set; } = 1200;
        public int GamesPlayed { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Draws { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
