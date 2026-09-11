using ChessCore.Data;
using ChessCore.Enums;
using ChessCore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessCore.Services
{
    public class MoveService : IMoveService
    {
        private readonly AppDbContext _context;

        public MoveService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Move> RecordMoveAsync(int gameId, int fromX, int fromY, int toX, int toY, PieceType pieceType, PieceColor pieceColor)
        {
            int currentMoveCount = await _context.Moves.CountAsync(m => m.GameId == gameId);

            var move = new Move
            {
                GameId = gameId,
                FromX = fromX,
                FromY = fromY,
                ToX = toX,
                ToY = toY,
                PieceType = pieceType,
                PieceColor = pieceColor,
                MoveOrder = currentMoveCount,
                MovedAt = DateTime.UtcNow
            };

            _context.Moves.Add(move);
            await _context.SaveChangesAsync();
            return move;
        }

        public async Task<List<Move>> GetMovesByGameIdAsync(int gameId)
        {
            return await _context.Moves
                .Where(m => m.GameId == gameId)
                .OrderBy(m => m.MoveOrder)
                .ToListAsync();
        }
    }
}
