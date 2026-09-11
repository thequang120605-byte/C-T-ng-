using System;
using System.Collections.Generic;
using ChessCore.Enums;

namespace ChessCore.Models
{
    public class Game
    {
        public int Id { get; set; }
        public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
        public PieceColor CurrentTurn { get; set; } = PieceColor.Red; // Red goes first
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastMoveAt { get; set; }

        // Navigation properties
        public List<Move> Moves { get; set; } = new List<Move>();
    }
}
