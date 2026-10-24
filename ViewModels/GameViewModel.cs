using System.Windows.Input;
using ChessCore.Mobile.Models;
using ChessCore.Mobile.Services;
namespace ChessCore.Mobile.ViewModels;

public sealed class GameViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IApiService _api;
    private readonly ISignalRService _signalR;
    private readonly IAuthService _auth;
    private readonly SemaphoreSlim _operation = new(1, 1);
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private CancellationTokenSource? _lifetime;
    private long _activation, _boardEpoch;
    private long? _revision;
    private bool _active, _attached;
    private int _gameId, _difficulty = 2;
    private string _gameMode = "PvE", _seatHint = "", _myPlayerColor = "";
    private GameResponseDto? _game;
    private int? _selectedRow, _selectedCol;
    private List<ValidMoveDto> _validMoves = new();
    private (int Row, int Col)? _lastMoveFrom, _lastMoveTo;
    private bool _isAiThinking, _isFlipped, _isGameOver;
    private string _statusMessage = "Đang chuẩn bị bàn cờ…", _connectionText = "Chưa kết nối";
    private string _gameOverTitle = "", _gameOverMessage = "";
    private AuthUserDto? _user;

    public GameViewModel(IApiService api, ISignalRService signalR, IAuthService auth)
    {
        _api = api; _signalR = signalR; _auth = auth;
        CellTappedCommand = new Command<(int Row, int Col)>(async p => await OnCellTappedAsync(p.Row, p.Col));
        FlipBoardCommand = new Command(() => { IsFlipped = !IsFlipped; BoardStateChanged?.Invoke(); });
        ResignCommand = new AsyncCommand(ResignAsync, ReportError);
        NewGameCommand = new AsyncCommand(NewGameAsync, ReportError);
        BackToHomeCommand = new AsyncCommand(async () => { await DeactivateAsync(); if (Shell.Current != null) await Shell.Current.GoToAsync("//home"); }, ReportError);
        ConfirmExitCommand = new AsyncCommand(ConfirmExitAsync, ReportError);
        RefreshCommand = new AsyncCommand(ReconnectAndReloadAsync, ReportError);
        RetryAiCommand = new AsyncCommand(RetryAiAsync, ReportError);
    }
    public event Action? BoardStateChanged;
    public ICommand CellTappedCommand { get; }
    public ICommand FlipBoardCommand { get; }
    public ICommand ResignCommand { get; }
    public ICommand NewGameCommand { get; }
    public ICommand BackToHomeCommand { get; }
    public ICommand ConfirmExitCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand RetryAiCommand { get; }
    public int GameId => _gameId;
    public int Difficulty => _difficulty;
    public string GameMode => _gameMode;
    public GameResponseDto? Game => _game;
    public string MyPlayerColor => _myPlayerColor;
    public int? SelectedRow => _selectedRow;
    public int? SelectedCol => _selectedCol;
    public List<ValidMoveDto> ValidMoves => _validMoves;
    public (int Row, int Col)? LastMoveFrom => _lastMoveFrom;
    public (int Row, int Col)? LastMoveTo => _lastMoveTo;
    public bool IsAiThinking { get => _isAiThinking; private set { SetProperty(ref _isAiThinking, value); OnPropertyChanged(nameof(CanRetryAi)); } }
    public bool IsFlipped { get => _isFlipped; set => SetProperty(ref _isFlipped, value); }
    public bool IsGameOver => _isGameOver;
    public string GameOverTitle => _gameOverTitle;
    public string GameOverMessage => _gameOverMessage;
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string ConnectionText { get => _connectionText; private set => SetProperty(ref _connectionText, value); }
    public bool IsMyTurn => GameSnapshotRules.IsMyTurn(Game, MyPlayerColor);
    public bool CanRetryAi => _active && GameMode == "PvE" && Game is { Status: "InProgress" } &&
        string.Equals(Game.CurrentTurn, "Black", StringComparison.OrdinalIgnoreCase) && !IsAiThinking && !IsBusy;
    public bool IsCheck => Game?.IsCheck ?? false;
    public int H1Score => Game?.Evaluation?.H1 ?? 0;
    public int H2Score => Game?.Evaluation?.H2 ?? 0;
    public int H3Score => Game?.Evaluation?.H3 ?? 0;
    public int H4Score => Game?.Evaluation?.H4 ?? 0;
    public int TotalScore => Game?.Evaluation?.Total ?? 0;
    public string RoomText => GameId < 0 ? "LUYỆN CỜ · NGOẠI TUYẾN" : GameMode == "Online" ? $"PHÒNG {Game?.RoomCode}" : $"LUYỆN CỜ · VÁN #{GameId}";
    public string PlayerName => _user?.Username ?? "Khách";
    public string OpponentName => GameMode == "PvE" ? $"Máy · Cấp {_difficulty}" :
        (MyPlayerColor == "Black" ? Game?.RedPlayerUsername : Game?.BlackPlayerUsername) ?? "Đối thủ";
    public string MySideText => MyPlayerColor == "Red" ? "QUÂN ĐỎ" : MyPlayerColor == "Black" ? "QUÂN ĐEN" : "ĐANG XEM";
    public string OpponentSideText => MyPlayerColor == "Black" ? "QUÂN ĐỎ" : "QUÂN ĐEN";
    public string CurrentTurnText => Game == null ? "Đang tải ván cờ" : IsGameOver ? GameOverTitle :
        Game.Status == "WaitingForPlayers" ? "Đang chờ kỳ thủ vào phòng" :
        $"Lượt quân {(string.Equals(Game.CurrentTurn, "Red", StringComparison.OrdinalIgnoreCase) ? "Đỏ" : "Đen")}{(IsMyTurn ? " · Bạn đi" : "")}";
    public string SelectionText => SelectedRow.HasValue ? $"Đã chọn hàng {SelectedRow + 1}, cột {SelectedCol + 1}" : "Chạm quân cờ để xem nước đi";
    public string MoveCountText => $"{Game?.Moves?.Count ?? 0} nước đã đi";
    public string HistoryText => Game?.Moves is { Count: > 0 } moves
        ? string.Join("\n", moves.OrderByDescending(m => m.MoveOrder).Take(10).Select(m => $"{m.MoveOrder + 1:00}. {m.PieceColor} · ({m.FromX + 1},{m.FromY + 1}) → ({m.ToX + 1},{m.ToY + 1})"))
        : "Những nước đi gần nhất sẽ xuất hiện ở đây.";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("gameId", out var id) && int.TryParse(id?.ToString(), out var parsed) && parsed != 0) _gameId = parsed;
        if (query.TryGetValue("difficulty", out var diff) && int.TryParse(diff?.ToString(), out var depth)) _difficulty = Math.Clamp(depth, 1, 4);
        _gameMode = query.TryGetValue("mode", out var mode) && mode?.ToString() == "Online" ? "Online" : "PvE";
        // Explicit successful create/join flow only. Never silently infer Red for an arbitrary game.
        _seatHint = query.TryGetValue("seat", out var seat) && seat?.ToString() is "Red" or "Black" ? seat!.ToString()! : "";
    }
    public async Task ActivateAsync()
    {
        await _lifecycle.WaitAsync();
        try
        {
            if (_active) return;
            _active = true;
            var activation = ++_activation;
            _lifetime = new CancellationTokenSource();
            _revision = null;
            _user = await _auth.GetCurrentUserAsync();
            if (!IsCurrent(activation)) return;
            if (GameId > 0 && !_attached)
            {
                _signalR.GameUpdated += OnGameUpdated;
                _signalR.Reconnected += OnReconnected;
                _signalR.ConnectionStateChanged += OnConnectionState;
                _attached = true;
            }
            IsBusy = true;
            try
            {
                // Join first, then load a snapshot to close the subscription gap.
                try
                {
                    if (GameId > 0) await _signalR.JoinGameAsync(GameId, _lifetime.Token);
                    else ConnectionText = "Trên thiết bị · không cần mạng";
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                { ConnectionText = "Chưa đồng bộ trực tiếp · bấm Đồng bộ"; System.Diagnostics.Debug.WriteLine(ex); }
                await ReloadAsync(_lifetime.Token);
            }
            finally { if (IsCurrent(activation)) SetBusy(false); }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ReportError(ex); }
        finally { _lifecycle.Release(); }
    }
    public async Task DeactivateAsync()
    {
        // Detach synchronously even when Android Back/another tab does not await us.
        _active = false;
        ++_activation;
        if (_attached)
        {
            _signalR.GameUpdated -= OnGameUpdated;
            _signalR.Reconnected -= OnReconnected;
            _signalR.ConnectionStateChanged -= OnConnectionState;
            _attached = false;
        }
        _lifetime?.Cancel();
        var old = _lifetime;
        _lifetime = null;
        SetBusy(false);
        IsAiThinking = false;
        try { await _signalR.LeaveGameAsync(GameId); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        finally { old?.Dispose(); }
    }
    private bool IsCurrent(long activation) => _active && activation == _activation;
    private void OnGameUpdated(GameResponseDto dto)
    {
        var activation = _activation;
        MainThread.BeginInvokeOnMainThread(() => { if (IsCurrent(activation) && dto.Id == GameId) ApplyGameUpdate(dto); });
    }
    private void OnConnectionState(string text)
    {
        var activation = _activation;
        MainThread.BeginInvokeOnMainThread(() => { if (IsCurrent(activation)) ConnectionText = text; });
    }
    private void OnReconnected() => MainThread.BeginInvokeOnMainThread(async () =>
    {
        try { if (_active && _lifetime != null) await ReloadAsync(_lifetime.Token); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ReportError(ex); }
    });
    private async Task ReloadAsync(CancellationToken token)
    {
        var activation = _activation;
        var id = GameId;
        var epoch = _boardEpoch;
        var dto = await _api.GetAsync<GameResponseDto>($"api/games/{id}", token);
        if (dto != null && IsCurrent(activation) && id == GameId && (dto.Revision.HasValue || epoch == _boardEpoch)) ApplyGameUpdate(dto);
    }
    private void DetermineSide(GameResponseDto dto)
    {
        var color = GameSnapshotRules.ResolveColor(dto, _user, GameMode, _seatHint);
        if (color != _myPlayerColor)
        {
            _myPlayerColor = color;
            IsFlipped = color == "Black";
            OnPropertyChanged(nameof(MyPlayerColor));
        }
    }
    private static bool Matches(string? a, string b) => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private void ApplyGameUpdate(GameResponseDto dto)
    {
        if (!_active || dto.Id != GameId) return;
        if (dto.Revision.HasValue && _revision.HasValue && dto.Revision.Value < _revision.Value) return;
        _revision = dto.Revision ?? _revision;
        ++_boardEpoch;
        _game = dto; // All dependent properties now read the same snapshot.
        DetermineSide(dto);
        _selectedRow = _selectedCol = null;
        _validMoves = new();
        var last = dto.Moves?.OrderByDescending(m => m.MoveOrder).FirstOrDefault();
        _lastMoveFrom = last == null ? null : (last.FromX, last.FromY);
        _lastMoveTo = last == null ? null : (last.ToX, last.ToY);
        _isGameOver = dto.Status is "RedWins" or "BlackWins" or "Draw";
        if (_isGameOver)
        {
            var winner = dto.Status == "RedWins" ? "Red" : "Black";
            _gameOverTitle = dto.Status == "Draw" ? "VÁN CỜ HÒA" : string.IsNullOrEmpty(MyPlayerColor) ? "VÁN CỜ KẾT THÚC" : winner == MyPlayerColor ? "CHIẾN THẮNG" : "HẸN VÁN TIẾP THEO";
            _gameOverMessage = dto.EndReason switch { "Checkmate" => "Ván cờ kết thúc do chiếu hết.", "NoLegalMoves" => "Bên đến lượt không còn nước đi hợp lệ.", "Repetition" => "Thế cờ lặp lại ba lần. Ván luyện tập kết thúc hòa.", "Resigned" => "Bạn đã xin thua ván luyện tập.", _ => "Ván cờ đã kết thúc. Bạn có thể bắt đầu một ván mới." };
        }
        StatusMessage = _isGameOver ? _gameOverTitle : CurrentTurnText;
        NotifySnapshot();
        BoardStateChanged?.Invoke();
    }
    private void NotifySnapshot()
    {
        foreach (var name in new[] { nameof(Game), nameof(GameId), nameof(IsMyTurn), nameof(IsGameOver), nameof(GameOverTitle), nameof(GameOverMessage),
            nameof(H1Score), nameof(H2Score), nameof(H3Score), nameof(H4Score), nameof(TotalScore), nameof(IsCheck), nameof(CurrentTurnText),
            nameof(RoomText), nameof(PlayerName), nameof(OpponentName), nameof(MySideText), nameof(OpponentSideText), nameof(SelectedRow), nameof(SelectedCol),
            nameof(ValidMoves), nameof(LastMoveFrom), nameof(LastMoveTo), nameof(SelectionText), nameof(MoveCountText), nameof(HistoryText), nameof(CanRetryAi) }) OnPropertyChanged(name);
    }
    private void SetBusy(bool value) { IsBusy = value; OnPropertyChanged(nameof(CanRetryAi)); }
    public async Task OnCellTappedAsync(int row, int col)
    {
        if (row is < 0 or > 9 || col is < 0 or > 8 || !await _operation.WaitAsync(0)) return;
        var activation = _activation;
        try
        {
            if (!_active || IsBusy || IsAiThinking || !IsMyTurn || _lifetime == null) return;
            var token = _lifetime.Token;
            if (_selectedRow == row && _selectedCol == col) { ClearSelection(); return; }
            if (_selectedRow.HasValue && _selectedCol.HasValue && _validMoves.Any(m => m.Row == row && m.Col == col))
            { await MakeMoveAsync(_selectedRow.Value, _selectedCol.Value, row, col, token); return; }
            var piece = Game?.BoardPieces?.FirstOrDefault(p => p.Row == row && p.Col == col);
            if (piece == null || !Matches(piece.Color, MyPlayerColor)) { ClearSelection(); return; }
            _selectedRow = row; _selectedCol = col; _validMoves = new();
            OnPropertyChanged(nameof(SelectionText)); BoardStateChanged?.Invoke();
            var epoch = _boardEpoch;
            var moves = await _api.GetAsync<List<ValidMoveDto>>($"api/games/{GameId}/valid-moves?row={row}&col={col}", token);
            if (IsCurrent(activation) && epoch == _boardEpoch && _selectedRow == row && _selectedCol == col)
            { _validMoves = moves ?? new(); BoardStateChanged?.Invoke(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (IsCurrent(activation)) ReportError(ex); }
        finally { _operation.Release(); }
    }
    private async Task MakeMoveAsync(int fromRow, int fromCol, int row, int col, CancellationToken token)
    {
        SetBusy(true); ClearSelection();
        var activation = _activation;
        var id = GameId;
        var epoch = _boardEpoch;
        try
        {
            var dto = await _api.PostAsync<MakeMoveRequestDto, GameResponseDto>($"api/games/{id}/moves",
                new() { FromX = fromRow, FromY = fromCol, ToX = row, ToY = col }, token);
            if (!IsCurrent(activation) || id != GameId) return;
            if (dto != null && (dto.Revision.HasValue || epoch == _boardEpoch)) ApplyGameUpdate(dto);
            await ReloadAsync(token);
            if (GameMode == "PvE" && Game is { Status: "InProgress", CurrentTurn: "Black" }) await TriggerAiAsync(token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!IsCurrent(activation)) return;
            await TryReloadAsync(token);
            StatusMessage = $"Chưa xác nhận nước đi. {ex.Message}";
        }
        finally { if (IsCurrent(activation)) SetBusy(false); }
    }
    private async Task TriggerAiAsync(CancellationToken token)
    {
        var activation = _activation;
        var epoch = _boardEpoch;
        IsAiThinking = true; StatusMessage = "Máy đang suy nghĩ…";
        try
        {
            var dto = await _api.PostAsync<GameResponseDto>($"api/games/{GameId}/computer-move?difficulty={_difficulty}&computerColor=black", token);
            if (IsCurrent(activation) && dto != null && (dto.Revision.HasValue || epoch == _boardEpoch)) ApplyGameUpdate(dto);
            if (IsCurrent(activation)) await ReloadAsync(token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!IsCurrent(activation)) return;
            await TryReloadAsync(token);
            StatusMessage = "Đã kiểm tra lại bàn cờ. Nếu máy chưa đi, bấm Tiếp tục lượt máy. " + ex.Message;
        }
        finally { if (IsCurrent(activation)) IsAiThinking = false; }
    }
    private async Task TryReloadAsync(CancellationToken token)
    { try { await ReloadAsync(token); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); } }
    private void ClearSelection()
    { _selectedRow = _selectedCol = null; _validMoves = new(); OnPropertyChanged(nameof(SelectionText)); BoardStateChanged?.Invoke(); }
    private async Task ReconnectAndReloadAsync()
    {
        if (!_active || _lifetime == null || !await _operation.WaitAsync(0)) return;
        var activation = _activation;
        try
        {
            SetBusy(true);
            if (GameId > 0) await _signalR.JoinGameAsync(GameId, _lifetime.Token);
            await ReloadAsync(_lifetime.Token);
        }
        finally { if (IsCurrent(activation)) SetBusy(false); _operation.Release(); }
    }
    private async Task RetryAiAsync()
    {
        if (!CanRetryAi || _lifetime == null || !await _operation.WaitAsync(0)) return;
        var activation = _activation;
        try
        {
            SetBusy(true);
            await ReloadAsync(_lifetime.Token);
            if (Game is { Status: "InProgress", CurrentTurn: "Black" }) await TriggerAiAsync(_lifetime.Token);
        }
        finally { if (IsCurrent(activation)) SetBusy(false); _operation.Release(); }
    }
    public async Task ConfirmExitAsync()
    {
        if (Game is { Status: "InProgress" } && !IsGameOver)
        {
            bool confirm = true;
            try
            {
                if (Shell.Current != null)
                {
                    confirm = await Shell.Current.DisplayAlert(
                        "Rời ván cờ",
                        "Ván cờ đang diễn ra. Bạn có chắc muốn rời phòng và quay về Sảnh?",
                        "Rời phòng",
                        "Ở lại");
                }
            }
            catch { }
            if (!confirm) return;
        }
        await DeactivateAsync();
        if (Shell.Current != null) await Shell.Current.GoToAsync("//home");
    }
    private async Task ResignAsync()
    {
        if (!_active || IsBusy || IsGameOver || string.IsNullOrEmpty(MyPlayerColor) || _lifetime == null) return;
        if (!await Shell.Current.DisplayAlert("Xin thua", "Bạn muốn kết thúc ván cờ này?", "Xin thua", "Chơi tiếp")) return;
        if (!await _operation.WaitAsync(0)) return;
        var activation = _activation;
        try
        {
            if (!IsCurrent(activation) || _lifetime == null) return;
            SetBusy(true);
            var epoch = _boardEpoch;
            var dto = await _api.PostAsync<GameResponseDto>($"api/games/{GameId}/resign", _lifetime.Token);
            if (IsCurrent(activation) && dto != null && (dto.Revision.HasValue || epoch == _boardEpoch)) ApplyGameUpdate(dto);
            if (IsCurrent(activation)) await ReloadAsync(_lifetime.Token);
        }
        finally { if (IsCurrent(activation)) SetBusy(false); _operation.Release(); }
    }
    private async Task NewGameAsync()
    {
        if (GameMode == "Online") { await DeactivateAsync(); await Shell.Current.GoToAsync("//lobby"); return; }
        if (!_active || IsBusy || _lifetime == null || !await _operation.WaitAsync(0)) return;
        var activation = _activation;
        try
        {
            SetBusy(true);
            var dto = await _api.PostAsync<GameResponseDto>(GameId < 0 ? "api/offline/games" : "api/games?mode=PvE", _lifetime.Token);
            if (!IsCurrent(activation) || (dto is null || dto.Id == 0)) return;
            var old = GameId;
            await _signalR.LeaveGameAsync(old);
            if (!IsCurrent(activation)) return;
            _gameId = dto.Id; _revision = null; _seatHint = "Red";
            ApplyGameUpdate(dto);
            if (GameId > 0) await _signalR.JoinGameAsync(GameId, _lifetime.Token);
            await ReloadAsync(_lifetime.Token);
        }
        finally { if (IsCurrent(activation)) SetBusy(false); _operation.Release(); }
    }
    private void ReportError(Exception ex)
    { if (ex is not OperationCanceledException && _active) StatusMessage = ex.Message; }
}
