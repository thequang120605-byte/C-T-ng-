using ChessCore.Models;
using ChessCore.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChessCore.Services
{
    public interface IMoveService
    {
        Task<Move> RecordMoveAsync(int gameId, int fromX, int fromY, int toX, int toY, PieceType pieceType, PieceColor pieceColor);
        Task<List<Move>> GetMovesByGameIdAsync(int gameId);
    }
}
