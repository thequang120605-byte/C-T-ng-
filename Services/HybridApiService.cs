using ChessCore.Mobile.Models;
namespace ChessCore.Mobile.Services;

// Only negative IDs belong to local games; all normal/online API calls still use the real server.
public sealed class HybridApiService : IApiService
{
    private readonly ApiService _remote;
    private readonly OfflineGameService _local;
    public HybridApiService(ApiService remote, OfflineGameService local) { _remote = remote; _local = local; }
    public Task<bool> PingAsync(CancellationToken cancellationToken = default) => _remote.PingAsync(cancellationToken);
    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var (parts, query) = Parse(endpoint);
        if (parts.SequenceEqual(new[] { "api", "offline", "history" }))
        {
            return (T)(object)_local.GetSavedHistory();
        }
        if (LocalId(parts) is int id)
        {
            object value = parts.Length == 3 ? await _local.GetAsync(id, cancellationToken) :
                parts.Length == 4 && parts[3] == "valid-moves" ? await _local.ValidMovesAsync(id, Number(query,"row",-1), Number(query,"col",-1), cancellationToken) :
                throw new InvalidOperationException("Thao tác luyện cờ chưa được hỗ trợ.");
            return (T)value;
        }
        return await _remote.GetAsync<T>(endpoint, cancellationToken);
    }
    public async Task<T?> PostAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var (parts, query) = Parse(endpoint);
        if (parts.SequenceEqual(new[] { "api", "offline", "games" }))
        {
            var mode = query.TryGetValue("mode", out var m) ? m : "PvE";
            var fen = query.TryGetValue("fen", out var f) ? f : null;
            var title = query.TryGetValue("title", out var t) ? t : "Luyện cờ";
            return (T)(object)await _local.CreateSessionAsync(mode, fen, title, cancellationToken);
        }
        if (LocalId(parts) is int id && parts.Length == 4)
        {
            object value = parts[3] switch
            {
                "computer-move" => await _local.ComputerAsync(id, Number(query,"difficulty",2), cancellationToken),
                "resign" => await _local.ResignAsync(id, cancellationToken),
                _ => throw new InvalidOperationException("Thao tác luyện cờ chưa được hỗ trợ.")
            };
            return (T)value;
        }
        return await _remote.PostAsync<T>(endpoint, cancellationToken);
    }
    public async Task<TResponse?> PostAsync<TRequest,TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
    {
        var (parts, _) = Parse(endpoint);
        if (LocalId(parts) is int id && parts.Length == 4 && parts[3] == "moves" && data is MakeMoveRequestDto move)
            return (TResponse)(object)await _local.MoveAsync(id, move, cancellationToken);
        return await _remote.PostAsync<TRequest,TResponse>(endpoint, data, cancellationToken);
    }
    private static int? LocalId(string[] p) => p.Length >= 3 && p[0] == "api" && p[1] == "games" && int.TryParse(p[2], out int id) && id < 0 ? id : null;
    private static (string[] Parts, Dictionary<string,string> Query) Parse(string endpoint)
    {
        var uri = new Uri("https://local.invalid/" + endpoint.TrimStart('/'));
        var query = uri.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Split('=',2))
            .ToDictionary(x=>Uri.UnescapeDataString(x[0]),x=>x.Length>1?Uri.UnescapeDataString(x[1]):"",StringComparer.OrdinalIgnoreCase);
        return (uri.AbsolutePath.Trim('/').Split('/'),query);
    }
    private static int Number(Dictionary<string,string> query,string key,int fallback) => query.TryGetValue(key,out var s)&&int.TryParse(s,out int v)?v:fallback;
}
