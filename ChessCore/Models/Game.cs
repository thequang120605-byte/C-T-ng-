using System;
using System.Collections.Generic;
using ChessCore.Enums;

namespace ChessCore.Models
{
    public class Game
    {
        public int Id { get; set; }

        public int? RedPlayerId { get; set; }
        public int? BlackPlayerId { get; set; }

        public GameType GameType { get; set; } = GameType.Standard;
        public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
        public PieceColor CurrentTurn { get; set; } = PieceColor.Red;
        public PieceColor? Winner { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastMoveAt { get; set; }

        public List<Move> Moves { get; set; } = new();
    }
}
