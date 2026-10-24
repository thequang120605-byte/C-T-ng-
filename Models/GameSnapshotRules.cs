using System.Globalization;
namespace ChessCore.Mobile.Models;
public static class GameSnapshotRules
{
    private static bool Matches(string? a, string? b) => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    public static string ResolveColor(GameResponseDto game, AuthUserDto? user, string mode, string confirmedSeat)
    {
        if (mode == "PvE") return "Red";
        var hasIdentity = game.RedUserId.HasValue || game.BlackUserId.HasValue ||
            !string.IsNullOrEmpty(game.RedPlayerId) || !string.IsNullOrEmpty(game.BlackPlayerId) ||
            !string.IsNullOrEmpty(game.RedPlayerUsername) || !string.IsNullOrEmpty(game.BlackPlayerUsername);
        if (user is { Id: > 0 })
        {
            var id = user.Id.ToString(CultureInfo.InvariantCulture);
            if (game.RedUserId == user.Id || Matches(game.RedPlayerId, user.PlayerId) || Matches(game.RedPlayerId, id) || Matches(game.RedPlayerUsername, user.Username)) return "Red";
            if (game.BlackUserId == user.Id || Matches(game.BlackPlayerId, user.PlayerId) || Matches(game.BlackPlayerId, id) || Matches(game.BlackPlayerUsername, user.Username)) return "Black";
        }
        // Compatibility hint is only passed by a successful create/join action.
        // It is UI information, never a substitute for backend authorization.
        if (!hasIdentity && confirmedSeat is "Red" or "Black") return confirmedSeat;
        return "";
    }
    public static bool IsMyTurn(GameResponseDto? game, string color) =>
        game is { Status: "InProgress" } && !string.IsNullOrWhiteSpace(color) && Matches(game.CurrentTurn, color);
}
