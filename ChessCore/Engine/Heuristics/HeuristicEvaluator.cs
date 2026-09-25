using System;
using ChessCore.Enums;

namespace ChessCore.Engine.Heuristics
{
    public static class HeuristicEvaluator
    {
        private const int KingValue = 10000;
        private const int RookValue = 900;
        private const int CannonValue = 475;
        private const int HorseValue = 400;
        private const int ElephantValue = 200;
        private const int AdvisorValue = 200;
        private const int PawnValue = 100;

        public static int EvaluateH1(Board board, PieceColor perspective)
        {
            ArgumentNullException.ThrowIfNull(board);

            int score = 0;

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    var piece = board.GetPieceAt(row, col);
                    if (piece == null)
                    {
                        continue;
                    }

                    int materialValue = GetMaterialValue(piece.Type);
                    score += piece.Color == perspective ? materialValue : -materialValue;
                }
            }

            return score;
        }

        private static int GetMaterialValue(PieceType pieceType)
        {
            return pieceType switch
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
    }
}
