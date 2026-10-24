#nullable enable annotations
#nullable disable warnings
using System;


namespace ChessCore.Mobile.Offline
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
