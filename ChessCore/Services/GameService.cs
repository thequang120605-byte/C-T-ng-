using ChessCore.Data;
using ChessCore.Engine;
using ChessCore.Enums;
using ChessCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ChessCore.Services
{
    public class GameService : IGameService
    {
        private readonly AppDbContext _context;

        public GameService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Game> CreateGameAsync(
            string? initialFen = null,
            GameStatus status = GameStatus.InProgress)
        {
            PieceColor firstTurn = PieceColor.Red;
            string? storedFen = null;

            if (!string.IsNullOrWhiteSpace(initialFen))
            {
                if (!Board.TryParseFen(initialFen, out Board? board, out string error))
                {
                    throw new ArgumentException(error);
                }

                firstTurn = board!.CurrentTurn;
                storedFen = board.ToFen();
            }

            var game = new Game
            {
                Status = status,
                CurrentTurn = firstTurn,
                InitialFen = storedFen,
                CreatedAt = DateTime.UtcNow
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();
            return game;
        }

        public async Task<Game> GetGameByIdAsync(int gameId)
        {
            return await _context.Games
                .Include(g => g.Moves)
                .FirstOrDefaultAsync(g => g.Id == gameId);
        }

        public async Task<bool> UpdateGameTurnAndStatusAsync(int gameId, PieceColor nextTurn, GameStatus status)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return false;

            game.CurrentTurn = nextTurn;
            game.Status = status;
            game.LastMoveAt = DateTime.UtcNow;

            _context.Games.Update(game);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Đi lại. PvP: lùi 1 nước. PvE: lùi về lượt của người chơi (quân Đỏ),
        /// tức bỏ nước của máy và nước trước đó của người; nếu ván vừa kết thúc
        /// bằng nước của người thì chỉ bỏ đúng nước đó.
        /// </summary>
        public async Task<bool> UndoMoveAsync(int gameId, bool isPvE = true)
        {
            var game = await _context.Games
                .Include(g => g.Moves)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null || game.Moves == null || game.Moves.Count == 0)
                return false;

            var ordered = game.Moves.OrderByDescending(m => m.MoveOrder).ToList();

            int toRemove = 1;

            if (isPvE && ordered[0].PieceColor == PieceColor.Black)
            {
                // Máy vừa đi: bỏ luôn nước của người trước đó.
                toRemove = 2;
            }

            if (ordered.Count < toRemove)
                return false;

            var removed = ordered.Take(toRemove).ToList();

            // Bên có lượt sau khi lùi = bên đã đi nước cũ nhất bị bỏ.
            PieceColor turnAfterUndo = removed.Last().PieceColor;

            _context.Moves.RemoveRange(removed);

            game.CurrentTurn = turnAfterUndo;
            game.Status = GameStatus.InProgress;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
