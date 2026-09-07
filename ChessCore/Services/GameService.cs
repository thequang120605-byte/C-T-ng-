using ChessCore.Data;
using ChessCore.Enums;
using ChessCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task<Game> CreateGameAsync()
        {
            var game = new Game
            {
                Status = GameStatus.InProgress,
                CurrentTurn = PieceColor.Red,
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
    }
}
