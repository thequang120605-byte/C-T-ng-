using System;
using ChessCore.Enums;

namespace ChessCore.Models
{
    public class EngineMove
    {
        public Position From { get; set; }
        public Position To { get; set; }
        public Piece CapturedPiece { get; set; }

        public EngineMove(Position from, Position to, Piece capturedPiece = null)
        {
            From = from;
            To = to;
            CapturedPiece = capturedPiece;
        }
    }
}
