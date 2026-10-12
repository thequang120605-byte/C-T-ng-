using System.ComponentModel.DataAnnotations;

namespace ChessCore.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;
    }
}
