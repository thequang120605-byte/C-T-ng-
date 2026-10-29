using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using ChessCore.Data;
using ChessCore.Enums;
using ChessCore.Hubs;
using ChessCore.Models;

namespace ChessCore.Services
{
    public interface IGameSettlementService
    {
        Task<GameSettlementResult> SettleGameAsync(int gameId, GameStatus status, string winReason, CancellationToken cancellationToken = default);
    }

    public class GameSettlementResult
    {
        public bool Success { get; set; }
        public bool AlreadyProcessed { get; set; }
        public Game Game { get; set; } = null!;
        public int RedEloChange { get; set; }
        public int BlackEloChange { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class GameSettlementService : IGameSettlementService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GameSettlementService> _logger;

        public GameSettlementService(
            IServiceScopeFactory scopeFactory,
            ILogger<GameSettlementService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task<GameSettlementResult> SettleGameAsync(int gameId, GameStatus status, string winReason, CancellationToken cancellationToken = default)
        {
            if (status == GameStatus.InProgress || status == GameStatus.WaitingForPlayers)
            {
                return new GameSettlementResult { Success = false, Message = "Trạng thái không phải kết thúc ván." };
            }

            // Thực thi trong ExecutionStrategy để tương thích hoàn toàn với EnableRetryOnFailure của Pomelo/MySQL
            await using var probeScope = _scopeFactory.CreateAsyncScope();
            var probeDb = probeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var strategy = probeDb.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var game = await db.Games
                        .Include(g => g.Moves)
                        .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);

                    if (game == null)
                    {
                        return new GameSettlementResult { Success = false, Message = $"Không tìm thấy ván cờ #{gameId}" };
                    }

                    // Idempotent: Nếu đã xử lý kết quả trước đó, tuyệt đối không tính điểm lần hai
                    if (game.ResultsProcessedAt.HasValue)
                    {
                        _logger.LogInformation($"Ván #{gameId} đã được quyết toán tại {game.ResultsProcessedAt.Value:o}. Bỏ qua quyết toán trùng.");
                        return new GameSettlementResult
                        {
                            Success = true,
                            AlreadyProcessed = true,
                            Game = game,
                            Message = "Ván cờ đã được xử lý trước đó."
                        };
                    }

                    game.Status = status;
                    game.ResultsProcessedAt = DateTime.UtcNow;
                    game.Version++;

                    int actualRedDelta = 0;
                    int actualBlackDelta = 0;

                    bool canRate = GameModes.CanRate(game);
                    User? redUser = game.RedPlayerId.HasValue ? await db.Users.FindAsync(new object[] { game.RedPlayerId.Value }, cancellationToken) : null;
                    User? blackUser = game.BlackPlayerId.HasValue ? await db.Users.FindAsync(new object[] { game.BlackPlayerId.Value }, cancellationToken) : null;

                    if (canRate && redUser != null && blackUser != null)
                    {
                        int rawRedDelta = RatingCalculator.RedDelta(redUser.Elo, blackUser.Elo, status);
                        int rawBlackDelta = -rawRedDelta;

                        int oldRedElo = redUser.Elo;
                        int oldBlackElo = blackUser.Elo;

                        redUser.Elo = Math.Max(400, oldRedElo + rawRedDelta);
                        actualRedDelta = redUser.Elo - oldRedElo;

                        blackUser.Elo = Math.Max(400, oldBlackElo + rawBlackDelta);
                        actualBlackDelta = blackUser.Elo - oldBlackElo;

                        // Ghi lịch sử Elo thực tế sau khi đã áp dụng sàn 400
                        db.UserEloHistories.Add(new UserEloHistory
                        {
                            UserId = redUser.Id,
                            GameId = game.Id,
                            OldElo = oldRedElo,
                            NewElo = redUser.Elo,
                            EloChange = actualRedDelta,
                            Reason = $"Ván đấu #{game.Id} ({winReason})",
                            RecordedAt = DateTime.UtcNow
                        });

                        db.UserEloHistories.Add(new UserEloHistory
                        {
                            UserId = blackUser.Id,
                            GameId = game.Id,
                            OldElo = oldBlackElo,
                            NewElo = blackUser.Elo,
                            EloChange = actualBlackDelta,
                            Reason = $"Ván đấu #{game.Id} ({winReason})",
                            RecordedAt = DateTime.UtcNow
                        });
                    }

                    // Cập nhật thống kê số trận thắng/thua/hòa
                    if (redUser != null)
                    {
                        redUser.GamesPlayed++;
                        if (status == GameStatus.RedWins) redUser.Wins++;
                        else if (status == GameStatus.BlackWins) redUser.Losses++;
                        else if (status == GameStatus.Draw) redUser.Draws++;
                    }

                    if (blackUser != null)
                    {
                        blackUser.GamesPlayed++;
                        if (status == GameStatus.BlackWins) blackUser.Wins++;
                        else if (status == GameStatus.RedWins) blackUser.Losses++;
                        else if (status == GameStatus.Draw) blackUser.Draws++;
                    }

                    await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation($"[SETTLEMENT] Quyết toán ván #{gameId} thành công: Status={status}, Reason={winReason}, RedEloDelta={actualRedDelta}, BlackEloDelta={actualBlackDelta}");

                    return new GameSettlementResult
                    {
                        Success = true,
                        AlreadyProcessed = false,
                        Game = game,
                        RedEloChange = actualRedDelta,
                        BlackEloChange = actualBlackDelta,
                        Message = $"Quyết toán thành công: {winReason}"
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogWarning(ex, $"Xung đột Concurrency khi quyết toán ván cờ #{gameId}.");
                    return new GameSettlementResult
                    {
                        Success = false,
                        Message = "Ván cờ đã bị thay đổi bởi thao tác khác. Vui lòng tải lại trang."
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, $"Lỗi không mong muốn khi quyết toán ván cờ #{gameId}.");
                    return new GameSettlementResult
                    {
                        Success = false,
                        Message = "Lỗi hệ thống khi quyết toán ván cờ. Hãy thử lại sau."
                    };
                }
            });
        }
    }
}
