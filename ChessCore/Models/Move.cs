using System;
using ChessCore.Enums;

namespace ChessCore.Models
{
    public class Move
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int FromX { get; set; }
        public int FromY { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }
        public PieceType PieceType { get; set; }
        public PieceColor PieceColor { get; set; }
        public int MoveOrder { get; set; } // 0-indexed, first move is 0
        public DateTime MovedAt { get; set; } = DateTime.UtcNow;
    }
}
