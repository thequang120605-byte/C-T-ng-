using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace ChessCore.Mobile.Services;

public interface IApiService
{
    Task<bool> PingAsync(CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
}
public sealed class ApiService : IApiService
{
    private readonly IConfigService _config;
    private readonly IAuthService _auth;
    private readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(40) };
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    public ApiService(IConfigService config, IAuthService auth) { _config = config; _auth = auth; }
    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _config.ServerChangedToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        var origin = _config.BaseUrl;
        try
        {
            using var res = await _http.GetAsync($"{origin}/api/health", cts.Token);
            if (res.IsSuccessStatusCode) return true;
            if (res.StatusCode != HttpStatusCode.NotFound) return false;
            using var fallback = await _http.GetAsync(origin, cts.Token);
            return fallback.IsSuccessStatusCode;
        }
        catch { return false; }
    }
    public Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
        => SendAsync<T>(HttpMethod.Get, endpoint, null, cancellationToken);
    public Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(HttpMethod.Post, endpoint, JsonSerializer.Serialize(data, _json), cancellationToken);
    public Task<TResponse?> PostAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(HttpMethod.Post, endpoint, null, cancellationToken);
    private async Task<T?> SendAsync<T>(HttpMethod method, string endpoint, string? json, CancellationToken cancellationToken)
    {
        var generation = _config.Generation;
        var origin = _config.BaseUrl;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _config.ServerChangedToken);
        var url = $"{origin}/{endpoint.TrimStart('/')}";
        for (int attempt = 0; attempt < 2; attempt++)
        {
            linked.Token.ThrowIfCancellationRequested();
            var access = await _auth.GetAccessTokenAsync();
            if (generation != _config.Generation) throw new OperationCanceledException("Máy chủ đã thay đổi.");
            HttpStatusCode status;
            string body;
            using (var req = new HttpRequestMessage(method, url))
            {
                if (json != null) req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(access)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
                using var res = await _http.SendAsync(req, linked.Token);
                status = res.StatusCode;
                body = await res.Content.ReadAsStringAsync(linked.Token);
                if (res.IsSuccessStatusCode)
                {
                    if (generation != _config.Generation) throw new OperationCanceledException();
                    if (typeof(T) == typeof(JsonElement)) return JsonSerializer.Deserialize<T>(body, _json);
                    if (string.IsNullOrWhiteSpace(body)) return default;
                    try { return JsonSerializer.Deserialize<T>(body, _json); }
                    catch (JsonException ex) { throw new InvalidOperationException("Dữ liệu máy chủ không khớp ứng dụng. Kiểm tra hợp đồng API mobile.", ex); }
                }
            }
            // A failed HTTP request is disposed before refresh/retry. Never retry a timeout POST.
            if (status == HttpStatusCode.Unauthorized && attempt == 0 && await _auth.RefreshTokenAsync(access, linked.Token)) continue;
            throw new HttpRequestException(ReadError(status, body), null, status);
        }
        throw new HttpRequestException("Phiên đăng nhập đã hết hạn.");
    }
    public static string ReadError(HttpStatusCode status, string body)
    {
        if (status == HttpStatusCode.Unauthorized) return "Phiên đăng nhập đã hết hạn hoặc máy chủ chưa hỗ trợ đăng nhập mobile.";
        if (status == HttpStatusCode.Forbidden) return "Bạn không có quyền thực hiện thao tác này.";
        if (status == HttpStatusCode.NotFound) return "Không tìm thấy dữ liệu hoặc API mobile trên máy chủ.";
        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var name in new[] { "message", "error", "title" })
                if (doc.RootElement.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.GetString()))
                    return p.GetString()!;
        }
        catch (JsonException) { }
        return $"Máy chủ trả lỗi {(int)status}. Vui lòng thử lại.";
    }
}
