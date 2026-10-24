using Microsoft.Maui.Storage;
namespace ChessCore.Mobile.Services;

public interface IConfigService
{
    string BaseUrl { get; set; }
    long Generation { get; }
    CancellationToken ServerChangedToken { get; }
    string DefaultEmulatorUrl { get; }
    string DefaultLanUrl { get; }
    event Action? BaseUrlChanged;
}
public sealed class ConfigService : IConfigService
{
    private CancellationTokenSource _changed = new();
    private long _generation;
    public long Generation => Interlocked.Read(ref _generation);
    public CancellationToken ServerChangedToken => _changed.Token;
    public string DefaultEmulatorUrl => "http://10.0.2.2:5000";
    public string DefaultLanUrl => "http://192.168.1.10:5000";
    public event Action? BaseUrlChanged;
    public string BaseUrl
    {
        get => Preferences.Default.Get("app_base_url", DeviceInfo.DeviceType == DeviceType.Virtual ? DefaultEmulatorUrl : DefaultLanUrl);
        set
        {
            var normalized = Normalize(value);
            if (string.Equals(normalized, BaseUrl, StringComparison.OrdinalIgnoreCase)) return;
            var previous = _changed;
            _changed = new CancellationTokenSource();
            Preferences.Default.Set("app_base_url", normalized);
            Interlocked.Increment(ref _generation);
            previous.Cancel();
            previous.Dispose();
            BaseUrlChanged?.Invoke();
        }
    }
    public static string Normalize(string? value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/")
            throw new ArgumentException("Nhập địa chỉ máy chủ dạng https://ten-mien hoặc http://IP:cong, không kèm đường dẫn.");
#if !DEBUG && !LOCAL_HTTP
        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Bản phát hành yêu cầu máy chủ HTTPS. Bản thử nghiệm LAN cần được build với AllowLocalHttp=true.");
#endif
        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }
}
