using System;
using System.Collections.Generic;

namespace ChessCore.DTOs
{
    // =========================================
    // ĐÁNH GIÁ NƯỚC VỪA ĐI (DELTA H1, H2, H3)
    // =========================================
    public class LastMoveEvaluationDto
    {
        public string PieceName { get; set; } = string.Empty;
        public string MoveDescription { get; set; } = string.Empty;
        public int DeltaH1 { get; set; } // Điểm ăn quân
        public int DeltaH2 { get; set; } // Điểm vị trí / cơ động
        public int DeltaH3 { get; set; } // Điểm chiến thuật
        public int TotalDelta => DeltaH1 + DeltaH2 + DeltaH3;
    }

    public class PieceDto
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class MoveHistoryDto
    {
        public int Id { get; set; }
        public int FromX { get; set; }
        public int FromY { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }
        public string PieceType { get; set; } = string.Empty;
        public string PieceColor { get; set; } = string.Empty;
        public int MoveOrder { get; set; }
        public DateTime MovedAt { get; set; }
    }

    public class GameResponseDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CurrentTurn { get; set; } = string.Empty;
        public bool IsCheck { get; set; }

        // Điểm thế trận tổng hợp (H1 + H2 + H3)
        public int RedScore { get; set; }
        public int BlackScore { get; set; }

        // Đánh giá nước vừa đi
        public LastMoveEvaluationDto? LastMoveEval { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastMoveAt { get; set; }
        public List<PieceDto> BoardPieces { get; set; } = new();
        public List<MoveHistoryDto> Moves { get; set; } = new();
    }

    public class MakeMoveRequestDto
    {
        public int FromX { get; set; }
        public int FromY { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }
    }

    public class ValidMoveDto
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsCapture { get; set; }
    }
}