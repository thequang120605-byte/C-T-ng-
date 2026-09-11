using ChessCore.Models;
using ChessCore.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessCore.Services
{
    public interface IGameService
    {
        Task<Game> CreateGameAsync();
        Task<Game> GetGameByIdAsync(int gameId);
        Task<bool> UpdateGameTurnAndStatusAsync(int gameId, PieceColor nextTurn, GameStatus status);
    }
}
