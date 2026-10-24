using System.Collections.ObjectModel;
using System.Windows.Input;
using ChessCore.Mobile.Models;
using ChessCore.Mobile.Services;

namespace ChessCore.Mobile.ViewModels;

public sealed class HistoryItemViewModel
{
    private readonly GameResponseDto _dto;

    public HistoryItemViewModel(GameResponseDto dto, ICommand resumeCommand, ICommand replayCommand)
    {
        _dto = dto;
        ResumeCommand = resumeCommand;
        ReplayCommand = replayCommand;
    }

    public int Id => _dto.Id;
    public string Title => !string.IsNullOrEmpty(_dto.RoomCode) ? $"Phòng {_dto.RoomCode}" :
        _dto.GameMode == "PassAndPlay" ? "Hai người cùng máy" :
        $"Luyện cờ với máy #{Math.Abs(_dto.Id)}";

    public string DateDisplay => _dto.CreatedAt != default ? _dto.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Vừa xong";
    public string MovesDisplay => $"{_dto.Moves?.Count ?? 0} nước đã đi";

    public string StatusDisplay => _dto.Status switch
    {
        "RedWins" => "Đỏ thắng",
        "BlackWins" => "Đen thắng",
        "Draw" => "Hòa cờ",
        _ => "Đang chơi"
    };

    public Color StatusColor => _dto.Status switch
    {
        "RedWins" => Color.FromArgb("#9F422F"),
        "BlackWins" => Color.FromArgb("#342B23"),
        "Draw" => Color.FromArgb("#B48440"),
        _ => Color.FromArgb("#2D6A4F")
    };

    public string Mode => _dto.GameMode ?? "PvE";
    public bool CanResume => _dto.Status == "InProgress";
    public bool CanReplay => (_dto.Moves?.Count ?? 0) > 0;

    public ICommand ResumeCommand { get; }
    public ICommand ReplayCommand { get; }
}

public sealed class HistoryViewModel : BaseViewModel
{
    private readonly IApiService _api;

    public HistoryViewModel(IApiService api)
    {
        _api = api;
        ResumeGameCommand = new AsyncCommand<HistoryItemViewModel>(ResumeGameAsync, Error);
        ReplayGameCommand = new AsyncCommand<HistoryItemViewModel>(ReplayGameAsync, Error);
        RefreshCommand = new AsyncCommand(LoadHistoryAsync, Error);
        BackCommand = new AsyncCommand(async () =>
        {
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//home");
        }, Error);
    }

    public ObservableCollection<HistoryItemViewModel> Items { get; } = new();

    public ICommand ResumeGameCommand { get; }
    public ICommand ReplayGameCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand BackCommand { get; }

    public bool HasNoItems => Items.Count == 0 && !IsBusy;

    public async Task LoadHistoryAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var list = await _api.GetAsync<List<GameResponseDto>>("api/offline/history");
            Items.Clear();
            if (list != null)
            {
                foreach (var g in list)
                {
                    Items.Add(new HistoryItemViewModel(
                        g,
                        new AsyncCommand(async () => await ResumeGameAsync(g), Error),
                        new AsyncCommand(async () => await ReplayGameAsync(g), Error)
                    ));
                }
            }
            OnPropertyChanged(nameof(HasNoItems));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResumeGameAsync(GameResponseDto dto)
    {
        await Shell.Current.GoToAsync($"game?gameId={dto.Id}&mode={dto.GameMode ?? "PvE"}&seat=Red");
    }

    private async Task ReplayGameAsync(GameResponseDto dto)
    {
        await Shell.Current.GoToAsync($"game?gameId={dto.Id}&mode={dto.GameMode ?? "PvE"}&replay=true&seat=Red");
    }

    private async Task ResumeGameAsync(HistoryItemViewModel? item)
    {
        if (item == null) return;
        await Shell.Current.GoToAsync($"game?gameId={item.Id}&mode={item.Mode}&seat=Red");
    }

    private async Task ReplayGameAsync(HistoryItemViewModel? item)
    {
        if (item == null) return;
        await Shell.Current.GoToAsync($"game?gameId={item.Id}&mode={item.Mode}&replay=true&seat=Red");
    }

    private void Error(Exception ex) => ErrorMessage = ex.Message;
}
