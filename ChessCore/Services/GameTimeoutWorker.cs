using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Enums;
using ChessCore.Engine;
using ChessCore.Hubs;
using ChessCore.DTOs;

namespace ChessCore.Services
{
    /// <summary>
    /// Background worker quét các ván cờ online đang diễn ra.
    /// Nếu một bên hết 3 phút mà chưa thực hiện nước đi -> Xử thua ngay lập tức (Rụng kim / Time Out)
    /// và phát thông báo SignalR tức thời về cho cả 2 client.
    /// </summary>
    public class GameTimeoutWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GameTimeoutWorker> _logger;

        public GameTimeoutWorker(IServiceScopeFactory scopeFactory, ILogger<GameTimeoutWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GameTimeoutWorker đã khởi động (Giới hạn: 3 phút/nước).");
            // Đợi 10 giây để bảo đảm Database migration hoàn tất khi ứng dụng vừa khởi động
            await Task.Delay(10000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckTimeoutsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong chu kỳ quét hết giờ ván cờ.");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task CheckTimeoutsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameHub>>();

            var now = DateTime.UtcNow;
            var timeoutLimit = TimeSpan.FromMinutes(3);
            var timeoutThreshold = now - timeoutLimit;

            // Quét các ván online đang InProgress và thời gian nước đi cuối vượt quá 3 phút
            var timedOutGames = await dbContext.Games
                .Include(g => g.Moves)
                .Where(g => g.Status == GameStatus.InProgress &&
                            (g.RedPlayerId.HasValue || g.BlackPlayerId.HasValue) &&
                            (g.LastMoveAt ?? g.CreatedAt) <= timeoutThreshold)
                .ToListAsync();

            if (timedOutGames.Count == 0) return;

            foreach (var game in timedOutGames)
            {
                _logger.LogInformation($"[TIMEOUT] Ván {game.Id} (Phòng: {game.RoomCode}): {game.CurrentTurn} hết 3 phút không đi nước cờ -> Xử thua.");

                // Bên đang đến lượt bị xử thua do hết giờ
                game.Status = game.CurrentTurn == PieceColor.Red
                    ? GameStatus.BlackWins
                    : GameStatus.RedWins;

                dbContext.Games.Update(game);
                await dbContext.SaveChangesAsync();

                var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
                var response = BuildTimeoutResponse(game, board);

                await hubContext.Clients.Group(game.Id.ToString()).SendAsync("GameUpdated", response);
            }
        }


        private static async Task UpdateUserStatsOnTimeoutAsync(AppDbContext dbContext, Models.Game game, GameStatus status)
        {
            if (status != GameStatus.RedWins && status != GameStatus.BlackWins) return;

            bool isOnlineMatch = GameModes.Normalize(game.GameMode) == GameModes.Online;

            if (game.RedPlayerId.HasValue)
            {
                var redUser = await dbContext.Users.FindAsync(game.RedPlayerId.Value);
                if (redUser != null)
                {
                    int oldElo = redUser.Elo;
                    int eloChange = 0;
                    redUser.GamesPlayed++;
                    if (status == GameStatus.RedWins)
                    {
                        redUser.Wins++;
                        if (isOnlineMatch)
                        {
                            eloChange = 15;
                            redUser.Elo += eloChange;
                        }
                    }
                    else if (status == GameStatus.BlackWins)
                    {
                        redUser.Losses++;
                        if (isOnlineMatch)
                        {
                            eloChange = -10;
                            redUser.Elo = Math.Max(400, redUser.Elo + eloChange);
                            eloChange = redUser.Elo - oldElo;
                        }
                    }

                    if (isOnlineMatch && eloChange != 0)
                    {
                        dbContext.UserEloHistories.Add(new Models.UserEloHistory
                        {
                            UserId = redUser.Id,
                            GameId = game.Id,
                            OldElo = oldElo,
                            NewElo = redUser.Elo,
                            EloChange = eloChange,
                            Reason = status == GameStatus.RedWins ? "Thắng do đối thủ hết giờ (Timeout)" : "Thua do hết giờ (Timeout)",
                            RecordedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            if (game.BlackPlayerId.HasValue)
            {
                var blackUser = await dbContext.Users.FindAsync(game.BlackPlayerId.Value);
                if (blackUser != null)
                {
                    int oldElo = blackUser.Elo;
                    int eloChange = 0;
                    blackUser.GamesPlayed++;
                    if (status == GameStatus.BlackWins)
                    {
                        blackUser.Wins++;
                        if (isOnlineMatch)
                        {
                            eloChange = 15;
                            blackUser.Elo += eloChange;
                        }
                    }
                    else if (status == GameStatus.RedWins)
                    {
                        blackUser.Losses++;
                        if (isOnlineMatch)
                        {
                            eloChange = -10;
                            blackUser.Elo = Math.Max(400, blackUser.Elo + eloChange);
                            eloChange = blackUser.Elo - oldElo;
                        }
                    }

                    if (isOnlineMatch && eloChange != 0)
                    {
                        dbContext.UserEloHistories.Add(new Models.UserEloHistory
                        {
                            UserId = blackUser.Id,
                            GameId = game.Id,
                            OldElo = oldElo,
                            NewElo = blackUser.Elo,
                            EloChange = eloChange,
                            Reason = status == GameStatus.BlackWins ? "Thắng do đối thủ hết giờ (Timeout)" : "Thua do hết giờ (Timeout)",
                            RecordedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }
        private static GameResponseDto BuildTimeoutResponse(Models.Game game, Board board)
        {
            var pieces = new System.Collections.Generic.List<PieceDto>();
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    var p = board.GetPieceAt(r, c);
                    if (p != null)
                    {
                        pieces.Add(new PieceDto
                        {
                            Type = p.Type.ToString(),
                            Color = p.Color.ToString(),
                            Row = r,
                            Col = c
                        });
                    }
                }
            }

            return new GameResponseDto
            {
                Id = game.Id,
                RoomCode = game.RoomCode,
                Status = game.Status.ToString(),
                CurrentTurn = game.CurrentTurn.ToString(),
                IsCheck = false,
                CreatedAt = game.CreatedAt,
                LastMoveAt = game.LastMoveAt,
                RemainingSeconds = 0,
                BoardPieces = pieces,
                Moves = game.Moves?
                    .OrderBy(m => m.MoveOrder)
                    .Select(m => new MoveHistoryDto
                    {
                        Id = m.Id,
                        FromX = m.FromX,
                        FromY = m.FromY,
                        ToX = m.ToX,
                        ToY = m.ToY,
                        PieceType = m.PieceType.ToString(),
                        PieceColor = m.PieceColor.ToString(),
                        MoveOrder = m.MoveOrder,
                        MovedAt = m.MovedAt
                    }).ToList() ?? new()
            };
        }
    }
}
