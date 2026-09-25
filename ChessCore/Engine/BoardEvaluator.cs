using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine
{
    public static class BoardEvaluator
    {
        private const int KingValue = 100000;
        private const int RookValue = 900;
        private const int CannonValue = 450;
        private const int HorseValue = 400;
        private const int ElephantValue = 200;
        private const int AdvisorValue = 200;
        private const int PawnValue = 100;

        public static int Evaluate(Board board, PieceColor perspective)
        {
            int score = 0;

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece = board.GetPieceAt(row, col);

                    if (piece == null)
                        continue;

                    int value = GetPieceValue(piece.Type);

                    value += GetPositionBonus(board, piece, row, col);

                    if (piece.Color == perspective)
                        score += value;
                    else
                        score -= value;
                }
            }

            return score;
        }

        private static int GetPieceValue(PieceType type)
        {
            return type switch
            {
                PieceType.King => KingValue,
                PieceType.Rook => RookValue,
                PieceType.Cannon => CannonValue,
                PieceType.Horse => HorseValue,
                PieceType.Elephant => ElephantValue,
                PieceType.Advisor => AdvisorValue,
                PieceType.Pawn => PawnValue,
                _ => 0
            };
        }

        private static int GetPositionBonus(
            Board board,
            Piece piece,
            int row,
            int col)
        {
            int bonus = 0;

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    bonus += GetPawnPositionBonus(piece.Color, row);
                    break;

                case PieceType.Horse:
                    bonus += GetCenterBonus(row, col);
                    break;

                case PieceType.Cannon:
                    bonus += GetCenterBonus(row, col);
                    break;

                case PieceType.Rook:
                    bonus += GetCenterBonus(row, col);
                    break;
            }

            return bonus;
        }

        private static int GetPawnPositionBonus(PieceColor color, int row)
        {
            if (color == PieceColor.Red)
            {
                if (row <= 4)
                    return 80;

                return 20;
            }

            if (row >= 5)
                return 80;

            return 20;
        }

        private static int GetCenterBonus(int row, int col)
        {
            int rowDistance = System.Math.Abs(4 - row);
            int colDistance = System.Math.Abs(4 - col);

            int bonus = 20 - (rowDistance + colDistance);

            return System.Math.Max(0, bonus);
        }

        public static bool IsCheckmate(Board board, PieceColor color)
        {
            if (!MoveGenerator.CheckIfInCheck(board, color))
                return false;

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece = board.GetPieceAt(row, col);

                    if (piece == null || piece.Color != color)
                        continue;

                    Position position = new Position(row, col);

                    if (MoveGenerator.GetValidMoves(board, position).Count > 0)
                        return false;
                }
            }

            return true;
        }

        public static bool IsStalemate(Board board, PieceColor color)
        {
            if (MoveGenerator.CheckIfInCheck(board, color))
                return false;

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece = board.GetPieceAt(row, col);

                    if (piece == null || piece.Color != color)
                        continue;

                    Position position = new Position(row, col);

                    if (MoveGenerator.GetValidMoves(board, position).Count > 0)
                        return false;
                }
            }

            return true;
        }
    }
}