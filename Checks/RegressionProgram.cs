#pragma warning disable CS0067
using System.Reflection;
using ChessCore.Mobile.ViewModels;
using ChessCore.Mobile.Services;
using ChessCore.Mobile.Models;
int passed = 0;
void Check(bool v, string name) { if (!v) throw new Exception(name); Console.WriteLine("PASS " + name); passed++; }
var api = new FakeApi(); var hub = new FakeHub(); var auth = new FakeAuth();
var vm = new GameViewModel(api, hub, auth);
vm.ApplyQueryAttributes(new Dictionary<string,object> { ["gameId"] = 1, ["mode"] = "PvE" });
await vm.ActivateAsync();
Check(hub.Subscribers == 1, "activation subscribes once");
await vm.ActivateAsync();
Check(hub.Subscribers == 1, "repeat activation does not duplicate subscriptions");
var apply = typeof(GameViewModel).GetMethod("ApplyGameUpdate", BindingFlags.Instance | BindingFlags.NonPublic)!;
void Apply(GameResponseDto g) => apply.Invoke(vm, new object[] { g });
Apply(new() { Id=1, Status="InProgress", CurrentTurn="Red", Revision=1 });
Check(vm.IsMyTurn && vm.StatusMessage.Contains("Bạn đi"), "Red turn presentation uses current snapshot");
Apply(new() { Id=1, Status="InProgress", CurrentTurn="Black", Revision=2 });
Check(!vm.IsMyTurn && !vm.StatusMessage.Contains("Bạn đi"), "Black turn does not retain previous Red turn text");
Apply(new() { Id=1, Status="InProgress", CurrentTurn="Red", Revision=1 });
Check(vm.Game!.Revision == 2 && vm.Game.CurrentTurn == "Black", "older revision cannot roll back the board");
Apply(new() { Id=2, Status="RedWins", CurrentTurn="Red", Revision=99 });
Check(vm.Game!.Id == 1, "another game's response cannot replace active game");
await vm.DeactivateAsync();
Check(hub.Subscribers == 0 && hub.CurrentGameId == null, "deactivation removes subscriptions and leaves room");
await vm.ActivateAsync();
Check(hub.Subscribers == 1, "reactivation restores subscriptions exactly once");
await vm.DeactivateAsync();
vm.ApplyQueryAttributes(new Dictionary<string,object> { ["gameId"] = 1, ["mode"] = "Online", ["seat"] = "Red" });
api.State = new() { Id=1, Status="InProgress", CurrentTurn="Black", BlackUserId=22, RedUserId=11, Revision=3 };
await vm.ActivateAsync();
Check(vm.MyPlayerColor == "Black" && vm.IsFlipped && vm.IsMyTurn, "online identity correctly resolves Black over route hint");
Apply(new() { Id=1, Status="BlackWins", CurrentTurn="Red", BlackUserId=22, RedUserId=11, Revision=4 });
Check(vm.IsGameOver && vm.GameOverTitle == "CHIẾN THẮNG" && !vm.IsMyTurn, "Black winner sees correct result and cannot move");
await vm.DeactivateAsync();
Check(hub.Subscribers == 0, "final deactivation leaves no GameUpdated subscriber");
api.State = new() { Id=-1, Status="InProgress", CurrentTurn="Red", Revision=1 };
vm.ApplyQueryAttributes(new Dictionary<string,object> { ["gameId"] = -1, ["mode"] = "PvE", ["seat"] = "Red" });
await vm.ActivateAsync();
Check(vm.GameId == -1 && vm.IsMyTurn, "local negative game ID activates correctly");
Check(hub.Subscribers == 0 && hub.CurrentGameId == null, "offline game never joins SignalR");
Check(vm.ConnectionText.Contains("không cần mạng"), "offline status is explicit");
await vm.DeactivateAsync();
var localService = new OfflineGameService();
var remoteAuth = new FakeAuth();
var hybrid = new HybridApiService(new ApiService(new FakeConfig(), remoteAuth), localService);
var created = await hybrid.PostAsync<GameResponseDto>("api/offline/games");
Check(created is { Id: < 0, BoardPieces.Count: 32 }, "hybrid creates a real local board");
var localHub = new FakeHub();
var offlineVm = new GameViewModel(hybrid, localHub, remoteAuth);
offlineVm.ApplyQueryAttributes(new Dictionary<string,object> { ["gameId"] = created!.Id, ["mode"] = "PvE", ["difficulty"] = 1 });
await offlineVm.ActivateAsync();
Check(offlineVm.IsMyTurn && localHub.Subscribers == 0, "real offline session activates without connecting a hub");
await offlineVm.OnCellTappedAsync(6,0);
await offlineVm.OnCellTappedAsync(5,0);
Check(offlineVm.Game!.Moves.Count == 2 && offlineVm.Game.CurrentTurn == "Red", "tap-to-move integration includes a real AI response");
Check(remoteAuth.AccessCalls == 0, "offline play never prepares a remote HTTP auth request");
await offlineVm.DeactivateAsync();
Console.WriteLine($"{passed} ViewModel regression checks passed.");
class FakeApi : IApiService
{
 public GameResponseDto State = new() { Id=1, Status="InProgress", CurrentTurn="Red" };
 public Task<T?> GetAsync<T>(string e,CancellationToken t=default) => Task.FromResult((T?)(object)State);
 public Task<bool> PingAsync(CancellationToken t=default) => Task.FromResult(true);
 public Task<T?> PostAsync<T>(string e,CancellationToken t=default) => Task.FromResult(default(T));
 public Task<T?> PostAsync<R,T>(string e,R d,CancellationToken t=default) => Task.FromResult(default(T));
}
class FakeHub : ISignalRService
{
 public bool IsConnected=>true;
 public int? CurrentGameId {get;private set;}
 public event Action<GameResponseDto>? GameUpdated;
 public event Action? Reconnected;
 public event Action<string>? ConnectionStateChanged;
 public int Subscribers => GameUpdated?.GetInvocationList().Length ?? 0;
 public Task ConnectAsync(CancellationToken t=default)=>Task.CompletedTask;
 public Task DisconnectAsync() { CurrentGameId=null;return Task.CompletedTask; }
 public Task JoinGameAsync(int id,CancellationToken t=default) {CurrentGameId=id;return Task.CompletedTask;}
 public Task LeaveGameAsync(int id) {CurrentGameId=null;return Task.CompletedTask;}
}
class FakeAuth : IAuthService
{
 public Task<AuthUserDto?> GetCurrentUserAsync()=>Task.FromResult<AuthUserDto?>(new(){Id=22,Username="Black",PlayerId="CT-22"});
 public Task<AuthUserDto?> FetchUserProfileAsync()=>GetCurrentUserAsync();
 public Task<bool> IsAuthenticatedAsync()=>Task.FromResult(true);
 public int AccessCalls;
 public Task<string?> GetAccessTokenAsync(){AccessCalls++;return Task.FromResult<string?>("test");}
 public Task<bool> RefreshTokenAsync(string? f=null,CancellationToken t=default)=>Task.FromResult(false);
 public Task LogoutAsync()=>Task.CompletedTask;
 public Task<(bool Success,string Message,AuthUserDto? User)> LoginAsync(string u,string p)=>Task.FromResult<(bool,string,AuthUserDto?)>((false,"",null));
 public Task<(bool Success,string Message,AuthUserDto? User)> RegisterAsync(string u,string e,string p)=>LoginAsync(u,p);
}

class FakeConfig : IConfigService
{
 public string BaseUrl {get;set;} = "https://not-running.invalid";
 public long Generation => 0;
 public CancellationToken ServerChangedToken => CancellationToken.None;
 public string DefaultEmulatorUrl => BaseUrl;
 public string DefaultLanUrl => BaseUrl;
 public event Action? BaseUrlChanged;
}
