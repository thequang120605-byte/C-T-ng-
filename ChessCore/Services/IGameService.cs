using ChessCore.Models;
using ChessCore.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessCore.Services
{
    public interface IGameService
    {
        Task<Game> CreateGameAsync(int? redPlayerId, int? blackPlayerId, GameType gameType);
        Task<Game> GetGameByIdAsync(int gameId);
        Task<bool> UpdateGameTurnAndStatusAsync(int gameId, PieceColor nextTurn, GameStatus status);
        Task<bool> FinishGameAsync(int gameId, PieceColor winner, GameStatus status);
    }
}
