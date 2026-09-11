using System;
using System.Collections.Generic;

namespace ChessCore.DTOs
{
    public class PieceDto
    {
        public int Row { get; set; }

        public int Col { get; set; }

        public string Type { get; set; }
            = string.Empty;

        public string Color { get; set; }
            = string.Empty;
    }


    public class MoveHistoryDto
    {
        public int Id { get; set; }

        public int FromX { get; set; }

        public int FromY { get; set; }

        public int ToX { get; set; }

        public int ToY { get; set; }

        public string PieceType { get; set; }
            = string.Empty;

        public string PieceColor { get; set; }
            = string.Empty;

        public int MoveOrder { get; set; }

        public DateTime MovedAt { get; set; }
    }


    public class GameResponseDto
    {
        public int Id { get; set; }

        public string Status { get; set; }
            = string.Empty;

        public string CurrentTurn { get; set; }
            = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? LastMoveAt { get; set; }

        public List<PieceDto> BoardPieces { get; set; }
            = new();

        public List<MoveHistoryDto> Moves { get; set; }
            = new();
    }


    public class MakeMoveRequestDto
    {
        public int FromX { get; set; }

        public int FromY { get; set; }

        public int ToX { get; set; }

        public int ToY { get; set; }
    }


    // =========================================
    // NƯỚC ĐI HỢP LỆ
    // =========================================

    public class ValidMoveDto
    {
        public int Row { get; set; }

        public int Col { get; set; }

        public bool IsCapture { get; set; }
    }
}