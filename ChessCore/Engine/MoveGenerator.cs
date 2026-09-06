using System.Collections.Generic;
using ChessCore.Models;

namespace ChessCore.Engine
{
    public static class MoveGenerator
    {
        public static List<Move> GetValidMoves(Board board, Position from)
        {
            List<Move> moves = new List<Move>();
            Piece p = board.GetPieceAt(from.Row, from.Col);
            if (p == null) return moves;

            switch (p.Type)
            {
                case PieceType.Rook:
                    moves.AddRange(GetRookMoves(board, from, p.Color));
                    break;
                case PieceType.Horse:
                    moves.AddRange(GetHorseMoves(board, from, p.Color));
                    break;
                case PieceType.Elephant:
                    moves.AddRange(GetElephantMoves(board, from, p.Color));
                    break;
                case PieceType.Advisor:
                    moves.AddRange(GetAdvisorMoves(board, from, p.Color));
                    break;
                case PieceType.King:
                    moves.AddRange(GetKingMoves(board, from, p.Color));
                    break;
                case PieceType.Cannon:
                    moves.AddRange(GetCannonMoves(board, from, p.Color));
                    break;
                case PieceType.Pawn:
                    moves.AddRange(GetPawnMoves(board, from, p.Color));
                    break;
            }

            return moves;
        }

        private static List<Move> GetRookMoves(Board board, Position from, PieceColor color)
        {
            List<Move> moves = new List<Move>();
            int[] dR = { -1, 1, 0, 0 };
            int[] dC = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r = from.Row + dR[i];
                int c = from.Col + dC[i];
                while (IsInsideBoard(r, c))
                {
                    Piece target = board.GetPieceAt(r, c);
                    if (target == null)
                    {
                        moves.Add(new Move(from, new Position(r, c)));
                    }
                    else
                    {
                        if (target.Color != color) moves.Add(new Move(from, new Position(r, c), target));
                        break;
                    }
                    r += dR[i];
                    c += dC[i];
                }
            }
            return moves;
        }

        private static List<Move> GetHorseMoves(Board board, Position from, PieceColor color)
        {
            List<Move> moves = new List<Move>();
            int[] dRowBlock = { -1, 1, 0, 0 };
            int[] dColBlock = { 0, 0, -1, 1 };

            int[,] dRowTarget = { { -2, -2 }, { 2, 2 }, { -1, 1 }, { -1, 1 } };
            int[,] dColTarget = { { -1, 1 }, { -1, 1 }, { -2, -2 }, { 2, 2 } };

            for (int i = 0; i < 4; i++)
            {
                int blockRow = from.Row + dRowBlock[i];
                int blockCol = from.Col + dColBlock[i];

                if (IsInsideBoard(blockRow, blockCol) && board.GetPieceAt(blockRow, blockCol) == null)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int targetRow = from.Row + dRowTarget[i, j];
                        int targetCol = from.Col + dColTarget[i, j];
                        AddMoveIfValid(board, moves, from, targetRow, targetCol, color);
                    }
                }
            }
            return moves;
        }

        private static List<Move> GetElephantMoves(Board board, Position from, PieceColor color)
        {
            List<Move> moves = new List<Move>();
            int[] dR = { -2, -2, 2, 2 };
            int[] dC = { -2, 2, -2, 2 };
            int[] eyeR = { -1, -1, 1, 1 };
            int[] eyeC = { -1, 1, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r = from.Row + dR[i];
                int c = from.Col + dC[i];
                int eR = from.Row + eyeR[i];
                int eC = from.Col + eyeC[i];

                bool isCrossRiver = color == PieceColor.Red ? r < 5 : r > 4;

                if (!isCrossRiver && IsInsideBoard(r, c) && board.GetPieceAt(eR, eC) == null)
                {
                    AddMoveIfValid(board, moves, from, r, c, color);
                }
            }
            return moves;
        }

        private static List<Move> GetAdvisorMoves(Board board, Position from, PieceColor color)
        {
            List<Move> moves = new List<Move>();
            int[] dR = { -1, -1, 1, 1 };
            int[] dC = { -1, 1, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r = from.Row + dR[i];
                int c = from.Col + dC[i];
                if (IsInPalace(r, c, color)) AddMoveIfValid(board, moves, from, r, c, color);
            }
            return moves;
        }

        private static List<Move> GetKingMoves(Board board, Position from, PieceColor color)
        {
            List<Move> moves = new List<Move>();
            int[] dR = { -1, 1, 0, 0 };
            int[] dC = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r = from.Row + dR[i];
                int c = from.Col + dC[i];
                if (IsInPalace(r, c, color)) AddMoveIfValid(board, moves, from, r, c, color);
            }
            return moves;
        }

        private static List<Move> GetCannonMoves(Board board, Position from, PieceColor color)
        {
            List<Move> moves = new List<Move>();
            int[] dR = { -1, 1, 0, 0 };
            int[] dC = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                bool foundMount = false;
                int r = from.Row + dR[i];
                int c = from.Col + dC[i];

                while (IsInsideBoard(r, c))
                {
                    Piece targetPiece = board.GetPieceAt(r, c);
                    if (!foundMount)
                    {
                        if (targetPiece == null)
                        {
                            moves.Add(new Move(from, new Position(r, c)));
                        }
                        else
                        {
                            foundMount = true;
                        }
                    }
                    else
                    {
                        if (targetPiece != null)
                        {
                            if (targetPiece.Color != color)
                            {
                                moves.Add(new Move(from, new Position(r, c), targetPiece));
                            }
                            break;
                        }
                    }
                    r += dR[i];
                    c += dC[i];
                }
            }
            return moves;
        }

        private static List<Move> GetPawnMoves(Board board, Position from, PieceColor color)
        {
            List<Move> moves = new List<Move>();
            int forward = color == PieceColor.Red ? -1 : 1;
            bool isCrossedRiver = color == PieceColor.Red ? from.Row <= 4 : from.Row >= 5;

            AddMoveIfValid(board, moves, from, from.Row + forward, from.Col, color);

            if (isCrossedRiver)
            {
                AddMoveIfValid(board, moves, from, from.Row, from.Col - 1, color);
                AddMoveIfValid(board, moves, from, from.Row, from.Col + 1, color);
            }
            return moves;
        }

        private static bool IsInsideBoard(int r, int c) => r >= 0 && r <= 9 && c >= 0 && c <= 8;

        private static bool IsInPalace(int r, int c, PieceColor color)
        {
            if (c < 3 || c > 5) return false;
            return color == PieceColor.Red ? r >= 7 && r <= 9 : r >= 0 && r <= 2;
        }

        private static void AddMoveIfValid(Board board, List<Move> moves, Position from, int r, int c, PieceColor color)
        {
            if (!IsInsideBoard(r, c)) return;
            Piece target = board.GetPieceAt(r, c);

            if (target == null)
                moves.Add(new Move(from, new Position(r, c)));
            else if (target.Color != color)
                moves.Add(new Move(from, new Position(r, c), target));
        }
    }
}