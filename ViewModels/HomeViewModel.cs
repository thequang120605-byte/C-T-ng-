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
    private bool _isDifficultyModalVisible;

    public HomeViewModel(IAuthService auth, IApiService api, IConfigService config, ISignalRService signalR)
    {
        _auth = auth;
        _api = api;
        _config = config;
        _signalR = signalR;

        ShowDifficultyPickerCommand = new Command(() => IsDifficultyModalVisible = true);
        CloseDifficultyPickerCommand = new Command(() => IsDifficultyModalVisible = false);
        StartAiWithDifficultyCommand = new AsyncCommand<int>(StartAiGameAsync, Error);
        PlayPassAndPlayCommand = new AsyncCommand(StartPassAndPlayAsync, Error);
        GoToLobbyCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("//lobby"), Error);
        GoToProfileCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("//profile"), Error);
        GoToSettingsCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("settings"), Error);
        GoToPuzzlesCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("puzzle"), Error);
        GoToCoursesCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("course"), Error);
        GoToHistoryCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("history"), Error);
        LogoutCommand = new AsyncCommand(AccountAsync, Error);
    }

    public string PlayerName => _user?.Username ?? "Kỳ thủ khách";
    public string PlayerIdDisplay => _user?.PlayerId ?? "Tập sự";
    public string AvatarLetter => string.IsNullOrWhiteSpace(_user?.Username) ? "K" : _user.Username[..1].ToUpperInvariant();
    public string EloDisplay => _user?.Elo > 0 ? $"Elo: {_user.Elo:N0}" : "Offline";
    public string AccountText => _user == null ? "Đăng nhập" : "Đăng xuất";

    public bool IsDifficultyModalVisible
    {
        get => _isDifficultyModalVisible;
        set => SetProperty(ref _isDifficultyModalVisible, value);
    }

    public int SelectedDifficulty
    {
        get => _selectedDifficulty;
        set
        {
            if (SetProperty(ref _selectedDifficulty, Math.Clamp(value, 1, 4)))
                OnPropertyChanged(nameof(DifficultyName));
        }
    }

    public string DifficultyName => SelectedDifficulty switch
    {
        1 => "Tập sự (Dễ)",
        2 => "Kỳ thủ (Vừa)",
        3 => "Kiện tướng (Khó)",
        _ => "Đại sư (Rất khó)"
    };

    public ICommand ShowDifficultyPickerCommand { get; }
    public ICommand CloseDifficultyPickerCommand { get; }
    public ICommand StartAiWithDifficultyCommand { get; }
    public ICommand PlayPassAndPlayCommand { get; }
    public ICommand GoToLobbyCommand { get; }
    public ICommand GoToProfileCommand { get; }
    public ICommand GoToSettingsCommand { get; }
    public ICommand GoToPuzzlesCommand { get; }
    public ICommand GoToCoursesCommand { get; }
    public ICommand GoToHistoryCommand { get; }
    public ICommand LogoutCommand { get; }

    public async Task LoadUserAsync()
    {
        _user = await _auth.GetCurrentUserAsync();
        foreach (var p in new[] { nameof(PlayerName), nameof(PlayerIdDisplay), nameof(AvatarLetter), nameof(EloDisplay), nameof(AccountText) })
            OnPropertyChanged(p);
    }

    private async Task StartAiGameAsync(int difficulty)
    {
        if (IsBusy) return;
        IsDifficultyModalVisible = false;
        SelectedDifficulty = difficulty;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var dto = await _api.PostAsync<GameResponseDto>("api/offline/games");
            if (dto is null || dto.Id == 0) throw new InvalidOperationException("Không thể tạo ván cờ.");
            await Shell.Current.GoToAsync($"game?gameId={dto.Id}&difficulty={difficulty}&mode=PvE&seat=Red");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StartPassAndPlayAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var dto = await _api.PostAsync<GameResponseDto>("api/offline/games?mode=PassAndPlay&title=Hai người cùng máy");
            if (dto is null || dto.Id == 0) throw new InvalidOperationException("Không thể tạo ván cờ.");
            await Shell.Current.GoToAsync($"game?gameId={dto.Id}&mode=PassAndPlay&seat=Red");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AccountAsync()
    {
        await _signalR.DisconnectAsync();
        if (_user != null) await _auth.LogoutAsync();
        _user = null;
        await Shell.Current.GoToAsync("login");
    }

    private void Error(Exception ex) => ErrorMessage = ex.Message;
}
