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

            var results = scope.ServiceProvider.GetRequiredService<GameResultService>();
            var unsettled = await dbContext.Games.AsNoTracking()
                .Where(g => g.ResultsProcessedAt == null &&
                    (g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw))
                .Select(g => g.Id).Take(100).ToListAsync();
            foreach (var id in unsettled) await results.ProcessAsync(id);
            var now = DateTime.UtcNow;
            var timeoutLimit = TimeSpan.FromMinutes(3);
            var timeoutThreshold = now - timeoutLimit;

            // Quét các ván online đang InProgress và thời gian nước đi cuối vượt quá 3 phút
            var timedOutGames = await dbContext.Games
                .Include(g => g.Moves)
                .Where(g => g.Status == GameStatus.InProgress &&
                            g.GameMode == GameModes.Online &&
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

                var affected = await dbContext.Games
                    .Where(g => g.Id == game.Id && g.Status == GameStatus.InProgress
                        && g.CurrentTurn == game.CurrentTurn
                        && (g.LastMoveAt ?? g.CreatedAt) <= timeoutThreshold)
                    .ExecuteUpdateAsync(set => set.SetProperty(g => g.Status, game.Status));
                if (affected == 0) continue;
                await results.ProcessAsync(game.Id);

                var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
                var response = BuildTimeoutResponse(game, board);

                await hubContext.Clients.Group(game.Id.ToString()).SendAsync("GameUpdated", response);
            }
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

            var red = BoardEvaluator.GetSideScores(board, PieceColor.Red);
            var black = BoardEvaluator.GetSideScores(board, PieceColor.Black);
            return new GameResponseDto
            {
                GameMode = game.GameMode, IsRated = game.IsRated,
                RedScore = red.Total - black.Total, BlackScore = black.Total - red.Total,
                Evaluation = new HeuristicEvaluationDto { H1 = red.H1-black.H1,
                    H2 = red.H2-black.H2, H3 = red.H3-black.H3, H4 = red.H4-black.H4 },
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
