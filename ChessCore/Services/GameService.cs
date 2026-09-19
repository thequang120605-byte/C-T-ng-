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

        public async Task<Game> CreateGameAsync(int? redPlayerId, int? blackPlayerId, GameType gameType)
        {
            var game = new Game
            {
                RedPlayerId = redPlayerId,
                BlackPlayerId = blackPlayerId,
                GameType = gameType,
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
            game.Winner = status switch
            {
                GameStatus.RedWins => PieceColor.Red,
                GameStatus.BlackWins => PieceColor.Black,
                _ => null
            };
            game.LastMoveAt = DateTime.UtcNow;

            _context.Games.Update(game);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FinishGameAsync(int gameId, PieceColor winner, GameStatus status)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return false;

            game.Winner = winner;
            game.Status = status;
            game.CurrentTurn = winner;
            game.LastMoveAt = DateTime.UtcNow;

            _context.Games.Update(game);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
