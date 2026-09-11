using System;

namespace ChessCore.Models
{
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
}
