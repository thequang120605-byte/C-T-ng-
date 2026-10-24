using System.Text.Json;
namespace ChessCore.Mobile.Models;
public static class JoinResponseParser
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public static GameResponseDto Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Phản hồi phòng không hợp lệ.");
        var payload = root.TryGetProperty("game", out var game) ? game : root;
        var dto = payload.Deserialize<GameResponseDto>(Options);
        return dto is { Id: > 0 } ? dto : throw new InvalidOperationException("Máy chủ chưa trả mã ván hợp lệ.");
    }
}
