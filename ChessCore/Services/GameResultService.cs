using System.Data;
using ChessCore.Data;
using ChessCore.Enums;
using ChessCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ChessCore.Services;

/// <summary>
/// Settles a persisted terminal game once. Ratings, history and the receipt
/// commit together. A fresh DbContext is used for each execution-strategy retry.
/// Does not claim to make the earlier move writes atomic.
/// </summary>
public sealed class GameResultService(IServiceScopeFactory scopeFactory)
{
    public async Task ProcessAsync(int gameId, CancellationToken ct = default)
    {
        using var strategyScope = scopeFactory.CreateScope();
        var strategyDb = strategyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var strategy = strategyDb.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var game = await db.Games.SingleOrDefaultAsync(g => g.Id == gameId, ct);
            if (game == null || !GameModes.IsTerminal(game.Status) || game.ResultsProcessedAt.HasValue)
                return;

            if (game.GameMode == GameModes.Online || game.GameMode == GameModes.PvE)
            {
                // Deterministic order reduces deadlocks between simultaneous games.
                var ids = new[] { game.RedPlayerId, game.BlackPlayerId }
                    .Where(id => id.HasValue).Select(id => id!.Value).Distinct().Order().ToArray();
                var users = new Dictionary<int, User>();
                foreach (var id in ids)
                {
                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
                    if (user != null) users.Add(id, user);
                }
                foreach (var user in users.Values)
                {
                    user.GamesPlayed++;
                    bool red = user.Id == game.RedPlayerId;
                    if (game.Status == GameStatus.Draw) user.Draws++;
                    else if ((game.Status == GameStatus.RedWins) == red) user.Wins++;
                    else user.Losses++;
                }
                if (GameModes.CanRate(game)
                    && users.TryGetValue(game.RedPlayerId!.Value, out var redUser)
                    && users.TryGetValue(game.BlackPlayerId!.Value, out var blackUser))
                {
                    int delta = RatingCalculator.RedDelta(redUser.Elo, blackUser.Elo, game.Status);
                    RecordRating(db, game, redUser, delta);
                    RecordRating(db, game, blackUser, -delta);
                }
            }
            game.ResultsProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    private static void RecordRating(AppDbContext db, Game game, User user, int delta)
    {
        var old = user.Elo;
        user.Elo += delta;
        db.UserEloHistories.Add(new UserEloHistory {
            UserId = user.Id, GameId = game.Id, OldElo = old, NewElo = user.Elo,
            EloChange = delta, Reason = $"Online · {game.Status} · Elo nội bộ K=32",
            RecordedAt = DateTime.UtcNow
        });
    }
}
