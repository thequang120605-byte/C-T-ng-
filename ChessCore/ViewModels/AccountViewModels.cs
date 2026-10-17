using System.ComponentModel.DataAnnotations;
using ChessCore.Services;

namespace ChessCore.ViewModels
{
    public class AccountProfileViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public string Role { get; set; } = "Player";
        public int Elo { get; set; } = 1200;
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double WinRate { get; set; }
        public int PuzzlesSolved { get; set; }
        public int CompletedCourses { get; set; }
        public int PuzzleScore { get; set; } = 500;
        public PuzzleLevelInfo? PuzzleLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AccountBadgeItem> Badges { get; set; } = new();
    }

    public class AccountBadgeItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
        public bool IsUnlocked { get; set; }
        public string ProgressText { get; set; } = "";
    }

    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "Tên hiển thị không được để trống")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Tên hiển thị từ 3 đến 30 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_\.\s\p{L}]+$", ErrorMessage = "Tên không chứa ký tự đặc biệt")]
        [Display(Name = "Tên hiển thị mới")]
        public string Username { get; set; } = "";
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmNewPassword { get; set; } = "";
    }
}
