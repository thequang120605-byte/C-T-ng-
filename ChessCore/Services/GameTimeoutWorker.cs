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
    /// Nếu một bên hết thời gian quy định (dựa trên TimeControlMinutes) mà chưa thực hiện nước đi -> 
    /// Tự động gọi GameSettlementService để quyết toán ván cờ (xử thua do rụng kim, tính Elo chuẩn, cập nhật lịch sử)
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
            _logger.LogInformation("GameTimeoutWorker đã khởi động (Quét hết giờ dựa theo cấu hình ván).");
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

                await Task.Delay(3000, stoppingToken);
            }
        }

        private async Task CheckTimeoutsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameHub>>();
            var settlementService = scope.ServiceProvider.GetRequiredService<IGameSettlementService>();

            var now = DateTime.UtcNow;

            // Quét các ván online đang InProgress và chưa quyết toán
            var activeGames = await dbContext.Games
                .Include(g => g.Moves)
                .Where(g => g.Status == GameStatus.InProgress &&
                            g.ResultsProcessedAt == null &&
                            (g.RedPlayerId.HasValue || g.BlackPlayerId.HasValue))
                .ToListAsync();

            if (activeGames.Count == 0) return;

            foreach (var game in activeGames)
            {
                int timeLimitMinutes = game.TimeControlMinutes > 0 ? game.TimeControlMinutes : 3;
                // Sử dụng hàm tính deadline thống nhất cho toàn bộ hệ thống
                if (now > GameModes.GetMoveDeadlineUtc(game))
                {
                    _logger.LogInformation($"[TIMEOUT] Ván #{game.Id} (Phòng: {game.RoomCode}): {game.CurrentTurn} hết {timeLimitMinutes} phút không đi cờ -> Xử thua.");

                    var losingColor = game.CurrentTurn;
                    var winningStatus = losingColor == PieceColor.Red
                        ? GameStatus.BlackWins
                        : GameStatus.RedWins;

                    var settlement = await settlementService.SettleGameAsync(game.Id, winningStatus, "Hết giờ (Rụng kim)");

                    var board = GameRules.RebuildBoard(game.InitialFen, game.Moves, game.CurrentTurn);
                    var response = BuildTimeoutResponse(game, board);

                    await hubContext.Clients.Group(game.Id.ToString()).SendAsync("GameUpdated", response);
                    await hubContext.Clients.Group(game.Id.ToString()).SendAsync("MatchEnded_Timeout", new
                    {
                        gameId = game.Id,
                        loserColor = losingColor.ToString(),
                        message = $"Hết thời gian {timeLimitMinutes} phút! Bên {losingColor} đã bị xử thua vì rụng kim.",
                        redEloChange = settlement.RedEloChange,
                        blackEloChange = settlement.BlackEloChange
                    });
                }
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

            return new GameResponseDto
            {
                Id = game.Id,
                RoomCode = game.RoomCode,
                Status = game.Status.ToString(),
                CurrentTurn = game.CurrentTurn.ToString(),
                IsCheck = MoveGenerator.CheckIfInCheck(board, game.CurrentTurn),
                HasNoLegalMoves = !GameRules.HasLegalMove(board, game.CurrentTurn),
                IsCheckmate = false,
                EndReason = "Timeout",
                InitialFen = game.InitialFen,
                BoardPieces = pieces,
                CreatedAt = game.CreatedAt,
                LastMoveAt = game.LastMoveAt,
                RemainingSeconds = 0,
                GameMode = game.GameMode ?? "",
                RedPlayerId = game.RedPlayerId,
                BlackPlayerId = game.BlackPlayerId,
                Moves = game.Moves?.OrderBy(m => m.MoveOrder).Select(m => new MoveHistoryDto
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
