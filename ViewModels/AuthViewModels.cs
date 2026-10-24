using System.Windows.Input;
using ChessCore.Mobile.Services;
namespace ChessCore.Mobile.ViewModels;
public sealed class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _auth;
    private string _username = "", _password = "";
    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
        LoginCommand = new AsyncCommand(LoginAsync, ex => ErrorMessage = ex.Message);
        GoToRegisterCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("register"), ex => ErrorMessage = ex.Message);
        PlayAsGuestCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("//home"), ex => ErrorMessage = ex.Message);
    }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public ICommand LoginCommand { get; }
    public ICommand GoToRegisterCommand { get; }
    public ICommand PlayAsGuestCommand { get; }
    private async Task LoginAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password)) { ErrorMessage = "Nhập tài khoản và mật khẩu."; return; }
        IsBusy = true; ErrorMessage = "";
        try
        {
            var result = await _auth.LoginAsync(Username, Password);
            if (!result.Success) { ErrorMessage = result.Message; return; }
            Password = "";
            await Shell.Current.GoToAsync("//home");
        }
        finally { IsBusy = false; }
    }
}
public sealed class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _auth;
    private string _username = "", _email = "", _password = "", _confirmPassword = "";
    public RegisterViewModel(IAuthService auth)
    {
        _auth = auth;
        RegisterCommand = new AsyncCommand(RegisterAsync, ex => ErrorMessage = ex.Message);
        GoToLoginCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync(".."), ex => ErrorMessage = ex.Message);
    }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
    public ICommand RegisterCommand { get; }
    public ICommand GoToLoginCommand { get; }
    private async Task RegisterAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password)) { ErrorMessage = "Nhập đầy đủ thông tin đăng ký."; return; }
        if (Password != ConfirmPassword) { ErrorMessage = "Mật khẩu xác nhận chưa khớp."; return; }
        IsBusy = true; ErrorMessage = "";
        try
        {
            var result = await _auth.RegisterAsync(Username, Email, Password);
            if (!result.Success) { ErrorMessage = result.Message; return; }
            Password = ConfirmPassword = "";
            await Shell.Current.GoToAsync("//home");
        }
        finally { IsBusy = false; }
    }
}
