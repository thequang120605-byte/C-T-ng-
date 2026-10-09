using ChessCore.Models;
using ChessCore.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessCore.Services
{
    public interface IGameService
    {
        Task<Game> CreateGameAsync(string? initialFen = null, GameStatus status = GameStatus.InProgress);
        Task<Game> GetGameByIdAsync(int gameId);
        Task<bool> UpdateGameTurnAndStatusAsync(int gameId, PieceColor nextTurn, GameStatus status);
        Task<bool> UndoMoveAsync(int gameId, bool isPvE = true);
    }
}
