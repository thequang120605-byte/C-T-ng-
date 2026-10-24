using ChessCore.Mobile.Models;
using Microsoft.AspNetCore.SignalR.Client;
namespace ChessCore.Mobile.Services;

public sealed class ChatMessageDto
{
    public string GameId { get; set; } = "";
    public string Sender { get; set; } = "";
    public string Message { get; set; } = "";
    public string SentAt { get; set; } = "";
}
public interface ISignalRService
{
    bool IsConnected { get; }
    int? CurrentGameId { get; }
    event Action<GameResponseDto>? GameUpdated;
    event Action? Reconnected;
    event Action<string>? ConnectionStateChanged;
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task JoinGameAsync(int gameId, CancellationToken cancellationToken = default);
    Task LeaveGameAsync(int gameId);
}
public sealed class SignalRService : ISignalRService
{
    private readonly IConfigService _config;
    private readonly IAuthService _auth;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private HubConnection? _hub;
    public int? CurrentGameId { get; private set; }
    public bool IsConnected => _hub?.State == HubConnectionState.Connected;
    public event Action<GameResponseDto>? GameUpdated;
    public event Action? Reconnected;
    public event Action<string>? ConnectionStateChanged;
    public SignalRService(IConfigService config, IAuthService auth)
    { _config = config; _auth = auth; _config.BaseUrlChanged += OnServerChanged; }
    private async void OnServerChanged()
    { try { await DisconnectAsync(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); } }
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _config.ServerChangedToken);
        await _gate.WaitAsync(linked.Token);
        try
        {
            if (IsConnected) return;
            if (_hub?.State == HubConnectionState.Reconnecting)
                throw new InvalidOperationException("Đang nối lại phòng. Vui lòng chờ hoặc thử đồng bộ lại.");
            if (_hub != null) { await _hub.DisposeAsync(); _hub = null; }
            var generation = _config.Generation;
            var hub = new HubConnectionBuilder().WithUrl($"{_config.BaseUrl}/gameHub", options =>
            { options.AccessTokenProvider = () => _auth.GetAccessTokenAsync(); })
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) }).Build();
            _hub = hub;
            hub.On<GameResponseDto>("GameUpdated", dto =>
            { if (ReferenceEquals(_hub, hub) && generation == _config.Generation) GameUpdated?.Invoke(dto); });
            hub.Reconnecting += error => { ConnectionStateChanged?.Invoke("Mất kết nối · đang nối lại"); return Task.CompletedTask; };
            hub.Reconnected += async id =>
            {
                if (!ReferenceEquals(_hub, hub) || generation != _config.Generation) return;
                try
                {
                    if (CurrentGameId is int gameId) await hub.InvokeAsync("JoinGame", gameId.ToString());
                    ConnectionStateChanged?.Invoke("Đã kết nối");
                    Reconnected?.Invoke();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); ConnectionStateChanged?.Invoke("Chưa vào lại phòng · bấm Đồng bộ"); }
            };
            hub.Closed += error => { ConnectionStateChanged?.Invoke("Ngoại tuyến · bấm Đồng bộ"); return Task.CompletedTask; };
            try { await hub.StartAsync(linked.Token); }
            catch { await hub.DisposeAsync(); if (ReferenceEquals(_hub, hub)) _hub = null; throw; }
            ConnectionStateChanged?.Invoke("Đã kết nối");
        }
        finally { _gate.Release(); }
    }
    public async Task JoinGameAsync(int gameId, CancellationToken cancellationToken = default)
    {
        if (gameId <= 0) throw new ArgumentOutOfRangeException(nameof(gameId));
        await ConnectAsync(cancellationToken);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_hub == null || !IsConnected) throw new InvalidOperationException("Chưa kết nối phòng.");
            if (CurrentGameId is int oldId && oldId != gameId) await _hub.InvokeAsync("LeaveGame", oldId.ToString(), cancellationToken);
            await _hub.InvokeAsync("JoinGame", gameId.ToString(), cancellationToken);
            CurrentGameId = gameId;
        }
        finally { _gate.Release(); }
    }
    public async Task LeaveGameAsync(int gameId)
    {
        await _gate.WaitAsync();
        try
        {
            if (CurrentGameId != gameId) return;
            CurrentGameId = null;
            if (_hub != null && IsConnected)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                try { await _hub.InvokeAsync("LeaveGame", gameId.ToString(), cts.Token); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            }
        }
        finally { _gate.Release(); }
    }
    public async Task DisconnectAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var old = _hub; _hub = null; CurrentGameId = null;
            if (old != null) await old.DisposeAsync();
            ConnectionStateChanged?.Invoke("Chưa kết nối");
        }
        finally { _gate.Release(); }
    }
}
