using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using ChessCore.Mobile.Models;
using ChessCore.Mobile.Services;
namespace ChessCore.Mobile.ViewModels;

public sealed class SettingsViewModel : BaseViewModel
{
    private readonly IConfigService _config;
    private readonly IApiService _api;
    private string _baseUrl, _connectionStatus = "Nhập địa chỉ máy chủ của bạn";
    private Color _statusColor = Color.FromArgb("#75634E");
    public SettingsViewModel(IConfigService config, IApiService api)
    {
        _config = config; _api = api; _baseUrl = config.BaseUrl;
        SaveCommand = new AsyncCommand(SaveAsync, Fail);
        TestConnectionCommand = new AsyncCommand(TestAsync, Fail);
        UseEmulatorCommand = new Command(() => BaseUrl = config.DefaultEmulatorUrl);
        UseLanCommand = new Command(() => BaseUrl = config.DefaultLanUrl);
        BackCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync(".."), Fail);
    }
    public string BaseUrl { get => _baseUrl; set => SetProperty(ref _baseUrl, value); }
    public string ConnectionStatus { get => _connectionStatus; private set => SetProperty(ref _connectionStatus, value); }
    public Color StatusColor { get => _statusColor; private set => SetProperty(ref _statusColor, value); }
    public bool ReduceMotion
    {
        get => Preferences.Default.Get("reduce_motion", false);
        set { Preferences.Default.Set("reduce_motion", value); OnPropertyChanged(); }
    }
    public ICommand SaveCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand UseEmulatorCommand { get; }
    public ICommand UseLanCommand { get; }
    public ICommand BackCommand { get; }
    private Task SaveAsync()
    {
        _config.BaseUrl = BaseUrl;
        BaseUrl = _config.BaseUrl;
        ConnectionStatus = "Đã lưu. Khi đổi máy chủ, hãy đăng nhập lại.";
        StatusColor = Color.FromArgb("#31765D");
        return Task.CompletedTask;
    }
    private async Task TestAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await SaveAsync();
            var ok = await _api.PingAsync();
            ConnectionStatus = ok ? "Máy chủ phản hồi. Bạn có thể quay lại sảnh." : "Chưa kết nối được. Kiểm tra địa chỉ và mạng Wi-Fi.";
            StatusColor = Color.FromArgb(ok ? "#31765D" : "#A34032");
        }
        finally { IsBusy = false; }
    }
    private void Fail(Exception ex) { ConnectionStatus = ex.Message; StatusColor = Color.FromArgb("#A34032"); }
}
public sealed class ProfileViewModel : BaseViewModel
{
    private readonly IAuthService _auth;
    private AuthUserDto? _user;
    public ProfileViewModel(IAuthService auth)
    { _auth = auth; BackCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("//home"), ex => ErrorMessage = ex.Message); }
    public AuthUserDto? User => _user;
    public string Username => User?.Username ?? "Kỳ thủ khách";
    public string Email => User?.Email ?? "Chưa đăng nhập";
    public string PlayerId => User?.PlayerId ?? "—";
    public string Elo => User?.Elo.ToString("N0") ?? "—";
    public int Wins => User?.Wins ?? 0;
    public int Losses => User?.Losses ?? 0;
    public int Draws => User?.Draws ?? 0;
    public int TotalGames => Wins + Losses + Draws;
    public double WinRate => TotalGames > 0 ? Math.Round(100d * Wins / TotalGames, 1) : 0;
    public int PuzzleScore => User?.PuzzleScore ?? 0;
    public ICommand BackCommand { get; }
    public async Task LoadProfileAsync()
    {
        _user = await _auth.FetchUserProfileAsync();
        foreach (var name in new[] { nameof(User), nameof(Username), nameof(Email), nameof(PlayerId), nameof(Elo), nameof(Wins), nameof(Losses), nameof(Draws), nameof(TotalGames), nameof(WinRate), nameof(PuzzleScore) }) OnPropertyChanged(name);
    }
}
public sealed class OnlineLobbyViewModel : BaseViewModel
{
    private readonly IApiService _api;
    private readonly IAuthService _auth;
    private ObservableCollection<RoomItemDto> _rooms = new();
    private string _roomCodeInput = "";
    public OnlineLobbyViewModel(IApiService api, IAuthService auth)
    {
        _api = api; _auth = auth;
        RefreshRoomsCommand = new AsyncCommand(LoadRoomsAsync, Fail);
        CreateRoomCommand = new AsyncCommand(CreateAsync, Fail);
        JoinByCodeCommand = new AsyncCommand(JoinAsync, Fail);
        BackCommand = new AsyncCommand(async () => await Shell.Current.GoToAsync("//home"), Fail);
    }
    public ObservableCollection<RoomItemDto> Rooms { get => _rooms; private set => SetProperty(ref _rooms, value); }
    public string RoomCodeInput { get => _roomCodeInput; set => SetProperty(ref _roomCodeInput, value); }
    public ICommand RefreshRoomsCommand { get; }
    public ICommand CreateRoomCommand { get; }
    public ICommand JoinByCodeCommand { get; }
    public ICommand BackCommand { get; }
    public async Task LoadRoomsAsync()
    {
        if (IsBusy) return;
        IsBusy = true; ErrorMessage = "";
        try { Rooms = new(await _api.GetAsync<List<RoomItemDto>>("api/games/online/rooms") ?? new()); }
        catch (Exception ex) { Fail(ex); }
        finally { IsBusy = false; }
    }
    private async Task<bool> RequireLoginAsync()
    {
        if (await _auth.IsAuthenticatedAsync()) return true;
        ErrorMessage = "Đăng nhập để tạo hoặc tham gia bàn online.";
        await Shell.Current.GoToAsync("login");
        return false;
    }
    private async Task CreateAsync()
    {
        if (IsBusy || !await RequireLoginAsync()) return;
        IsBusy = true; ErrorMessage = "";
        try
        {
            var game = await _api.PostAsync<GameResponseDto>("api/games/online");
            if (game is not { Id: > 0 }) throw new InvalidOperationException("Không tạo được phòng.");
            await Shell.Current.GoToAsync($"game?gameId={game.Id}&difficulty=1&mode=Online&seat=Red");
        }
        finally { IsBusy = false; }
    }
    private async Task JoinAsync()
    {
        if (IsBusy || !await RequireLoginAsync()) return;
        var rawCode = RoomCodeInput.Trim().ToUpperInvariant();
        if (rawCode.Length is < 1 or > 32 || !rawCode.All(char.IsLetterOrDigit)) { ErrorMessage = "Nhập mã phòng gồm chữ và số, tối đa 32 ký tự."; return; }
        IsBusy = true; ErrorMessage = "";
        try
        {
            var code = Uri.EscapeDataString(rawCode);
            // Deserialize the same response once. A parse error must never repeat a mutation.
            var root = await _api.PostAsync<JsonElement>($"api/games/{code}/join");
            var game = JoinResponseParser.Parse(root);
            await Shell.Current.GoToAsync($"game?gameId={game.Id}&difficulty=1&mode=Online&seat=Black");
        }
        finally { IsBusy = false; }
    }
    private void Fail(Exception ex) => ErrorMessage = ex.Message;
}
