using System;

namespace ChessCore.Models
{
    public enum PieceColor
    {
        Red,
        Black
    }

    public enum PieceType
    {
        King,      // Tướng
        Advisor,   // Sĩ
        Elephant,  // Tượng
        Horse,     // Mã
        Rook,      // Xe
        Cannon,    // Pháo
        Pawn       // Tốt
    }

    public struct Position
    {
        public int Row { get; set; } // 0 đến 9
        public int Col { get; set; } // 0 đến 8

        public Position(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override bool Equals(object obj) => obj is Position p && Row == p.Row && Col == p.Col;
        public override int GetHashCode() => HashCode.Combine(Row, Col);

        public static bool operator ==(Position left, Position right) => left.Equals(right);
        public static bool operator !=(Position left, Position right) => !left.Equals(right);
    }

    public class Piece
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }
    }

    public class Move
    {
        public Position From { get; set; }
        public Position To { get; set; }
        public Piece CapturedPiece { get; set; }

        public Move(Position from, Position to, Piece capturedPiece = null)
        {
            From = from;
            To = to;
            CapturedPiece = capturedPiece;
        }
    }
}