using System;
using System.Collections.Generic;

namespace ChessCore.DTOs
{
    public class LastMoveEvaluationDto
    {
        public string PieceName { get; set; } = string.Empty;
        public string MoveDescription { get; set; } = string.Empty;
        public int DeltaH1 { get; set; } // Thay doi chenh lech vat chat theo ben vua di
        public int DeltaH2 { get; set; } // Điểm vị trí / cơ động
        public int DeltaH3 { get; set; } // Điểm chiến thuật
        public int TotalDelta => DeltaH1 + DeltaH2 + DeltaH3;
    }

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


    public class HeuristicEvaluationDto
    {
        // All three components are from Red's perspective, in raw heuristic points.
        public int H1 { get; set; }
        public int H2 { get; set; }
        public int H3 { get; set; }
    }

    public class GameResponseDto
    {
        public int Id { get; set; }

        public string RoomCode { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;

        public string CurrentTurn { get; set; }
            = string.Empty;
        public bool IsCheck { get; set; }
        public bool HasNoLegalMoves { get; set; }
        public bool IsCheckmate { get; set; }
        public string EndReason { get; set; } = string.Empty;
        public string? InitialFen { get; set; }
        public HeuristicEvaluationDto Evaluation { get; set; } = new();
        public int RedScore { get; set; }
        public int BlackScore { get; set; }
        public LastMoveEvaluationDto? LastMoveEval { get; set; }
        public DateTime CreatedAt { get; set; }

        public DateTime? LastMoveAt { get; set; }
        public int RemainingSeconds { get; set; } = 180;

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

    public class RespondDrawRequestDto
    {
        public bool Accepted { get; set; }
        public bool IsAccepted { get; set; }
    }
}
