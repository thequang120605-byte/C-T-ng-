using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChessCore.Mobile.Models;
namespace ChessCore.Mobile.Services;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<string?> GetAccessTokenAsync();
    Task<AuthUserDto?> GetCurrentUserAsync();
    Task<AuthUserDto?> FetchUserProfileAsync();
    Task<(bool Success, string Message, AuthUserDto? User)> LoginAsync(string username, string password);
    Task<(bool Success, string Message, AuthUserDto? User)> RegisterAsync(string username, string email, string password);
    Task<bool> RefreshTokenAsync(string? failedToken = null, CancellationToken cancellationToken = default);
    Task LogoutAsync();
}
public sealed class AuthService : IAuthService
{
    private const string AccessTokenKey = "jwt_access_token", RefreshTokenKey = "jwt_refresh_token";
    private const string UserInfoKey = "cached_user_info", OriginKey = "auth_server_origin";
    private readonly IConfigService _config;
    private readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private AuthUserDto? _currentUser;
    private long _sessionEpoch;
    public AuthService(IConfigService config)
    {
        _config = config;
        _config.BaseUrlChanged += () => { Interlocked.Increment(ref _sessionEpoch); ClearLocal(); };
        // Legacy tokens had no server binding and must not be sent to a new origin.
        if (!OriginMatches()) ClearLocal();
    }
    private bool OriginMatches() => string.Equals(Preferences.Default.Get(OriginKey, ""), _config.BaseUrl, StringComparison.OrdinalIgnoreCase);
    private void ClearLocal()
    {
        _currentUser = null;
        Preferences.Default.Remove(OriginKey);
        Preferences.Default.Remove(UserInfoKey);
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
    }
    public async Task<string?> GetAccessTokenAsync()
    {
        var generation = _config.Generation;
        if (!OriginMatches()) return null;
        try
        {
            var token = await SecureStorage.Default.GetAsync(AccessTokenKey);
            return generation == _config.Generation && OriginMatches() ? token : null;
        }
        catch { return null; }
    }
    public async Task<bool> IsAuthenticatedAsync() => !string.IsNullOrWhiteSpace(await GetAccessTokenAsync());
    public Task<AuthUserDto?> GetCurrentUserAsync()
    {
        if (!OriginMatches()) return Task.FromResult<AuthUserDto?>(null);
        if (_currentUser != null) return Task.FromResult<AuthUserDto?>(_currentUser);
        try { _currentUser = JsonSerializer.Deserialize<AuthUserDto>(Preferences.Default.Get(UserInfoKey, "null"), _json); }
        catch { _currentUser = null; }
        return Task.FromResult(_currentUser);
    }
    public async Task<AuthUserDto?> FetchUserProfileAsync()
    {
        var generation = _config.Generation;
        var epoch = Interlocked.Read(ref _sessionEpoch);
        var origin = _config.BaseUrl;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_config.ServerChangedToken);
        try
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) return null;
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{origin}/api/auth/me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var res = await _http.SendAsync(req, linked.Token);
            if (!res.IsSuccessStatusCode) return await GetCurrentUserAsync();
            var text = await res.Content.ReadAsStringAsync(linked.Token);
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("isAuthenticated", out var authenticated) && authenticated.ValueKind == JsonValueKind.False) return null;
            var user = JsonSerializer.Deserialize<AuthUserDto>(text, _json);
            if (generation != _config.Generation || epoch != Interlocked.Read(ref _sessionEpoch)) return null;
            if (user is not { Id: > 0 }) return await GetCurrentUserAsync();
            _currentUser = user;
            Preferences.Default.Set(UserInfoKey, JsonSerializer.Serialize(user, _json));
            return user;
        }
        catch (OperationCanceledException) { return null; }
        catch { return await GetCurrentUserAsync(); }
    }
    public Task<(bool Success, string Message, AuthUserDto? User)> LoginAsync(string username, string password)
        => AuthenticateAsync("login", new { username = username.Trim(), password });
    public Task<(bool Success, string Message, AuthUserDto? User)> RegisterAsync(string username, string email, string password)
        => AuthenticateAsync("register", new { username = username.Trim(), email = email.Trim(), password });
    private async Task<(bool Success, string Message, AuthUserDto? User)> AuthenticateAsync(string action, object payload)
    {
        var generation = _config.Generation;
        var epoch = Interlocked.Read(ref _sessionEpoch);
        var origin = _config.BaseUrl;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_config.ServerChangedToken);
        await _sessionLock.WaitAsync(linked.Token);
        try
        {
            using var content = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");
            using var res = await _http.PostAsync($"{origin}/api/auth/{action}", content, linked.Token);
            var body = await res.Content.ReadAsStringAsync(linked.Token);
            if (!res.IsSuccessStatusCode) return (false, ApiService.ReadError(res.StatusCode, body), null);
            var dto = JsonSerializer.Deserialize<AuthResponseDto>(body, _json);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Token) || dto.User.Id <= 0)
                return (false, "Máy chủ chưa trả phiên đăng nhập mobile hợp lệ.", null);
            if (generation != _config.Generation || epoch != Interlocked.Read(ref _sessionEpoch)) return (false, "Phiên kết nối đã thay đổi.", null);
            await SecureStorage.Default.SetAsync(AccessTokenKey, dto.Token);
            if (!string.IsNullOrWhiteSpace(dto.RefreshToken)) await SecureStorage.Default.SetAsync(RefreshTokenKey, dto.RefreshToken);
            else SecureStorage.Default.Remove(RefreshTokenKey);
            if (generation != _config.Generation || epoch != Interlocked.Read(ref _sessionEpoch)) { ClearLocal(); return (false, "Phiên kết nối đã thay đổi.", null); }
            _currentUser = dto.User;
            Preferences.Default.Set(UserInfoKey, JsonSerializer.Serialize(dto.User, _json));
            Preferences.Default.Set(OriginKey, origin);
            return (true, "Đăng nhập thành công.", dto.User);
        }
        catch (OperationCanceledException) { return (false, "Kết nối đã hủy hoặc hết thời gian chờ.", null); }
        catch (Exception) { return (false, "Không thể đăng nhập. Kiểm tra địa chỉ máy chủ và API mobile.", null); }
        finally { _sessionLock.Release(); }
    }
    public async Task<bool> RefreshTokenAsync(string? failedToken = null, CancellationToken cancellationToken = default)
    {
        var generation = _config.Generation;
        var epoch = Interlocked.Read(ref _sessionEpoch);
        var origin = _config.BaseUrl;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _config.ServerChangedToken);
        await _sessionLock.WaitAsync(linked.Token);
        try
        {
            if (!OriginMatches()) return false;
            var current = await GetAccessTokenAsync();
            // Another waiting request already refreshed this failed access token.
            if (failedToken != null && current != null && current != failedToken) return true;
            var refresh = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(refresh)) return false;
            using var content = new StringContent(JsonSerializer.Serialize(new { refreshToken = refresh }), Encoding.UTF8, "application/json");
            using var res = await _http.PostAsync($"{origin}/api/auth/refresh-token", content, linked.Token);
            if (!res.IsSuccessStatusCode) return false;
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(linked.Token));
            if (!doc.RootElement.TryGetProperty("token", out var prop) || prop.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(prop.GetString())) return false;
            if (generation != _config.Generation || epoch != Interlocked.Read(ref _sessionEpoch)) return false;
            await SecureStorage.Default.SetAsync(AccessTokenKey, prop.GetString()!);
            if (doc.RootElement.TryGetProperty("refreshToken", out var rt) && rt.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(rt.GetString()))
                await SecureStorage.Default.SetAsync(RefreshTokenKey, rt.GetString()!);
            if (generation != _config.Generation || epoch != Interlocked.Read(ref _sessionEpoch)) { ClearLocal(); return false; }
            return true;
        }
        catch (OperationCanceledException) { return false; }
        catch { return false; }
        finally { _sessionLock.Release(); }
    }
    public async Task LogoutAsync()
    {
        Interlocked.Increment(ref _sessionEpoch);
        await _sessionLock.WaitAsync();
        try
        {
            var origin = _config.BaseUrl;
            var token = await GetAccessTokenAsync();
            var refresh = OriginMatches() ? await SecureStorage.Default.GetAsync(RefreshTokenKey) : null;
            ClearLocal(); // Local logout must not wait for a network timeout.
            if (!string.IsNullOrWhiteSpace(refresh))
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var req = new HttpRequestMessage(HttpMethod.Post, $"{origin}/api/auth/logout")
                { Content = new StringContent(JsonSerializer.Serialize(new { refreshToken = refresh }), Encoding.UTF8, "application/json") };
                if (token != null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var res = await _http.SendAsync(req, cts.Token);
            }
        }
        catch { ClearLocal(); }
        finally { _sessionLock.Release(); }
    }
}
