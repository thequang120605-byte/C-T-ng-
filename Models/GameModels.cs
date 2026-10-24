using System;
using System.Collections.Generic;

namespace ChessCore.Mobile.Models
{
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

    public class HeuristicEvaluationDto
    {
        public int H1 { get; set; }
        public int H2 { get; set; }
        public int H3 { get; set; }
        public int H4 { get; set; }
        public int Total => H1 + H2 + H3 + H4;
    }

    public class LastMoveEvaluationDto
    {
        public string PieceName { get; set; } = string.Empty;
        public string MoveDescription { get; set; } = string.Empty;
        public int DeltaH1 { get; set; }
        public int DeltaH2 { get; set; }
        public int DeltaH3 { get; set; }
        public int DeltaH4 { get; set; }
        public int TotalDelta => DeltaH1 + DeltaH2 + DeltaH3 + DeltaH4;
    }

    public class GameResponseDto
    {
        public int Id { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CurrentTurn { get; set; } = string.Empty;
        public bool IsCheck { get; set; }
        public bool HasNoLegalMoves { get; set; }
        public bool IsCheckmate { get; set; }
        public string EndReason { get; set; } = string.Empty;
        public string? InitialFen { get; set; }
        public HeuristicEvaluationDto Evaluation { get; set; } = new();
        public string GameMode { get; set; } = "PvE";
        public bool IsRated { get; set; }
        public int RedScore { get; set; }
        public int BlackScore { get; set; }
        public int NeuralScore { get; set; }
        public LastMoveEvaluationDto? LastMoveEval { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMoveAt { get; set; }
        public int RemainingSeconds { get; set; } = 180;
        public List<PieceDto> BoardPieces { get; set; } = new();
        public List<MoveHistoryDto> Moves { get; set; } = new();
        public long? Revision { get; set; }
        public int? RedUserId { get; set; }
        public int? BlackUserId { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(FlexibleIdConverter))]
        public string? RedPlayerId { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(FlexibleIdConverter))]
        public string? BlackPlayerId { get; set; }
        public string? RedPlayerUsername { get; set; }
        public string? BlackPlayerUsername { get; set; }
    }

    public class JoinGameResponseDto
    {
        public GameResponseDto? Game { get; set; }
        public string? OpponentUsername { get; set; }
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

    public class AuthUserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public string Role { get; set; } = "Player";
        public int Elo { get; set; } = 1200;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int PuzzleScore { get; set; }
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public AuthUserDto User { get; set; } = new();
    }

    public class RoomItemDto
    {
        public int Id { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatorUsername { get; set; } = string.Empty;
        public int CreatorElo { get; set; }
    }

    public class LiveMatchDto
    {
        public int Id { get; set; }
        public string RoomCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string RedPlayerName { get; set; } = string.Empty;
        public int RedElo { get; set; }
        public string BlackPlayerName { get; set; } = string.Empty;
        public int BlackElo { get; set; }
        public int MoveCount { get; set; }
    }
}
