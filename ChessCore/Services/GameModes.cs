using ChessCore.Models;
using ChessCore.Enums;
namespace ChessCore.Services;

public static class GameModes
{
    public const string Online = "Online";
    public const string PvE = "PvE";
    public const string Local = "PvP";
    public const string AiVsAi = "AiVsAi";

    public static readonly string[] StandardModes = { Local, PvE, Online, AiVsAi };

    public static bool IsTerminal(GameStatus status) => status is
        GameStatus.RedWins or GameStatus.BlackWins or GameStatus.Draw;

    public static string Normalize(string? mode, int? redPlayerId = null, int? blackPlayerId = null)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            if (redPlayerId.HasValue && blackPlayerId.HasValue && redPlayerId != blackPlayerId)
                return Online;
            return Local;
        }

        if (mode.StartsWith("Online", StringComparison.OrdinalIgnoreCase))
            return Online;
        if (mode.Equals(PvE, StringComparison.OrdinalIgnoreCase))
            return PvE;
        if (mode.Equals(AiVsAi, StringComparison.OrdinalIgnoreCase))
            return AiVsAi;
        if (mode.Equals("PvP", StringComparison.OrdinalIgnoreCase) || mode.Equals("Local", StringComparison.OrdinalIgnoreCase))
            return Local;

        // Legacy / Unknown fallback
        if (redPlayerId.HasValue && blackPlayerId.HasValue && redPlayerId != blackPlayerId)
            return Online;

        return Local;
    }

    public static string Label(string mode) => Normalize(mode) switch
    {
        Online => "Online",
        PvE => "Chơi với máy",
        Local => "Hai người cùng máy",
        AiVsAi => "Máy với máy",
        _ => "Hai người cùng máy"
    };

    public static bool CanRate(Game g) => g.IsRated && Normalize(g.GameMode) == Online
        && g.RedPlayerId.HasValue && g.BlackPlayerId.HasValue
        && g.RedPlayerId != g.BlackPlayerId && string.IsNullOrWhiteSpace(g.InitialFen);
}

public static class RatingCalculator
{
    public const int K = 32; // Application policy, not an official WXF rating.
    public static int RedDelta(int red, int black, GameStatus status)
    {
        if (!GameModes.IsTerminal(status)) throw new ArgumentException("Game has not ended.");
        double actual = status == GameStatus.RedWins ? 1 : status == GameStatus.Draw ? .5 : 0;
        double expected = 1 / (1 + Math.Pow(10, ((double)black - red) / 400));
        int delta = (int)Math.Round(K * (actual - expected), MidpointRounding.AwayFromZero);
        return Math.Clamp(delta, -Math.Max(0, red), Math.Max(0, black));
    }
}
