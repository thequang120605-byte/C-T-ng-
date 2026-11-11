using System.ComponentModel.DataAnnotations;

namespace ChessCore.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 30 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_-]{3,30}$", ErrorMessage = "Tên đăng nhập chỉ gồm chữ cái, chữ số, gạch dưới (_) hoặc gạch ngang (-), viết liền không dấu")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }
    }
}
