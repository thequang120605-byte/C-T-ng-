using System;
using System.Collections.Generic;
using ChessCore.Enums;

namespace ChessCore.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
        public PieceColor CurrentTurn { get; set; } = PieceColor.Red; // Red goes first
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastMoveAt { get; set; }

        /// <summary>
        /// Thế cờ xuất phát (FEN). Null = thế chuẩn. Dùng cho ván nhập từ FEN.
        /// </summary>
        public string? InitialFen { get; set; }

        public int? RedPlayerId { get; set; }
        public int? BlackPlayerId { get; set; }

        public User RedPlayer { get; set; }
        public User BlackPlayer { get; set; }

        // Navigation properties
        public List<Move> Moves { get; set; } = new List<Move>();
    }
}
