using System.Windows.Input;
using ChessCore.Mobile.Models;
using ChessCore.Mobile.Services;
namespace ChessCore.Mobile.ViewModels;
public sealed class HomeViewModel : BaseViewModel
{
    private readonly IAuthService _auth;
    private readonly IApiService _api;
    private readonly IConfigService _config;
    private readonly ISignalRService _signalR;
    private AuthUserDto? _user;
    private int _selectedDifficulty = 2;
    public HomeViewModel(IAuthService auth, IApiService api, IConfigService config, ISignalRService signalR)
    {
        _auth = auth; _api = api; _config = config; _signalR = signalR;
        PlayAiCommand = new AsyncCommand(PlayAsync, Error);
        GoToLobbyCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("//lobby"), Error);
        GoToProfileCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("//profile"), Error);
        GoToSettingsCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("settings"), Error);
        GoToPuzzlesCommand = new AsyncCommand(async () => await OpenWebAsync("Puzzle"), Error);
        GoToCoursesCommand = new AsyncCommand(async () => await OpenWebAsync("Course"), Error);
        LogoutCommand = new AsyncCommand(AccountAsync, Error);
    }
    public string PlayerName => _user?.Username ?? "Kỳ thủ mới";
    public string PlayerIdDisplay => _user?.PlayerId ?? "Chơi thử với máy hoặc đăng nhập";
    public string AvatarLetter => string.IsNullOrWhiteSpace(_user?.Username) ? "K" : _user.Username[..1].ToUpperInvariant();
    public string EloDisplay => _user?.Elo.ToString("N0") ?? "—";
    public int TotalGames => (_user?.Wins ?? 0) + (_user?.Draws ?? 0) + (_user?.Losses ?? 0);
    public string AccountText => _user == null ? "Đăng nhập" : "Đăng xuất";
    public int SelectedDifficulty { get => _selectedDifficulty; set { if (SetProperty(ref _selectedDifficulty, Math.Clamp(value, 1, 4))) OnPropertyChanged(nameof(DifficultyName)); } }
    public string DifficultyName => SelectedDifficulty switch { 1 => "Tập sự", 2 => "Kỳ thủ", 3 => "Kiện tướng", _ => "Đại sư" };
    public ICommand PlayAiCommand { get; }
    public ICommand GoToLobbyCommand { get; }
    public ICommand GoToProfileCommand { get; }
    public ICommand GoToSettingsCommand { get; }
    public ICommand GoToPuzzlesCommand { get; }
    public ICommand GoToCoursesCommand { get; }
    public ICommand LogoutCommand { get; }
    public async Task LoadUserAsync()
    {
        _user = await _auth.GetCurrentUserAsync();
        foreach (var p in new[] { nameof(PlayerName), nameof(PlayerIdDisplay), nameof(AvatarLetter), nameof(EloDisplay), nameof(TotalGames), nameof(AccountText) }) OnPropertyChanged(p);
    }
    private async Task PlayAsync()
    {
        if (IsBusy) return;
        IsBusy = true; ErrorMessage = "";
        try
        {
            var dto = await _api.PostAsync<GameResponseDto>("api/offline/games");
            if (dto is null || dto.Id == 0) throw new InvalidOperationException("Không thể tạo ván cờ.");
            await Shell.Current.GoToAsync($"game?gameId={dto.Id}&difficulty={SelectedDifficulty}&mode=PvE&seat=Red");
        }
        finally { IsBusy = false; }
    }
    private async Task AccountAsync()
    {
        await _signalR.DisconnectAsync();
        if (_user != null) await _auth.LogoutAsync();
        _user = null;
        await Shell.Current.GoToAsync("login");
    }
    private async Task OpenWebAsync(string route)
    {
        // MVC learning pages use their own browser cookie session, never a JWT query string.
        await Browser.Default.OpenAsync(new Uri($"{_config.BaseUrl}/{route}"), BrowserLaunchMode.SystemPreferred);
    }
    private void Error(Exception ex) => ErrorMessage = ex.Message;
}
