using System;
using System.Collections.Generic;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine
{
    public static class MoveGenerator
    {
        public static List<EngineMove> GetValidMoves(
            Board board,
            Position from)
        {
            List<EngineMove> pseudoMoves =
                GetPseudoLegalMoves(
                    board,
                    from);

            List<EngineMove> legalMoves =
                new List<EngineMove>();

            Piece? currentPiece =
                board.GetPieceAt(
                    from.Row,
                    from.Col);

            if (currentPiece == null)
                return legalMoves;

            foreach (EngineMove move in pseudoMoves)
            {
                Piece? captured =
                    board.GetPieceAt(
                        move.To.Row,
                        move.To.Col);

                board.Grid[
                    move.To.Row,
                    move.To.Col] = currentPiece;

                board.Grid[
                    move.From.Row,
                    move.From.Col] = null;

                bool isSelfInCheck =
                    CheckIfInCheck(
                        board,
                        currentPiece.Color);

                board.Grid[
                    move.From.Row,
                    move.From.Col] = currentPiece;

                board.Grid[
                    move.To.Row,
                    move.To.Col] = captured;

                if (!isSelfInCheck)
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }

        public static bool CheckIfInCheck(
            Board board,
            PieceColor color)
        {
            Position? kingPos = null;
            Position? opponentKingPos = null;

            PieceColor opponentColor =
                color == PieceColor.Red
                    ? PieceColor.Black
                    : PieceColor.Red;

            // ==============================
            // TÌM HAI TƯỚNG
            // ==============================

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? piece =
                        board.GetPieceAt(r, c);

                    if (piece == null)
                        continue;

                    if (piece.Type != PieceType.King)
                        continue;

                    if (piece.Color == color)
                    {
                        kingPos =
                            new Position(r, c);
                    }
                    else
                    {
                        opponentKingPos =
                            new Position(r, c);
                    }
                }
            }

            if (!kingPos.HasValue)
                return false;

            // ==============================
            // HAI TƯỚNG ĐỐI MẶT
            // ==============================

            if (opponentKingPos.HasValue &&
                kingPos.Value.Col ==
                opponentKingPos.Value.Col)
            {
                int minRow =
                    Math.Min(
                        kingPos.Value.Row,
                        opponentKingPos.Value.Row);

                int maxRow =
                    Math.Max(
                        kingPos.Value.Row,
                        opponentKingPos.Value.Row);

                bool hasObstacle = false;

                for (
                    int r = minRow + 1;
                    r < maxRow;
                    r++)
                {
                    if (board.GetPieceAt(
                        r,
                        kingPos.Value.Col) != null)
                    {
                        hasObstacle = true;
                        break;
                    }
                }

                if (!hasObstacle)
                    return true;
            }

            // ==============================
            // KIỂM TRA QUÂN ĐỊCH TẤN CÔNG
            // ==============================

            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    Piece? piece =
                        board.GetPieceAt(r, c);

                    if (piece == null)
                        continue;

                    if (piece.Color != opponentColor)
                        continue;

                    Position from =
                        new Position(r, c);

                    List<EngineMove> threats =
                        GetPseudoLegalMoves(
                            board,
                            from);

                    foreach (EngineMove threat in threats)
                    {
                        if (threat.To.Equals(
                            kingPos.Value))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static List<EngineMove> GetPseudoLegalMoves(
            Board board,
            Position from)
        {
            List<EngineMove> moves =
                new List<EngineMove>();

            Piece? piece =
                board.GetPieceAt(
                    from.Row,
                    from.Col);

            if (piece == null)
                return moves;

            switch (piece.Type)
            {
                case PieceType.Rook:
                    moves.AddRange(
                        GetRookMoves(
                            board,
                            from,
                            piece.Color));
                    break;

                case PieceType.Horse:
                    moves.AddRange(
                        GetHorseMoves(
                            board,
                            from,
                            piece.Color));
                    break;

                case PieceType.Elephant:
                    moves.AddRange(
                        GetElephantMoves(
                            board,
                            from,
                            piece.Color));
                    break;

                case PieceType.Advisor:
                    moves.AddRange(
                        GetAdvisorMoves(
                            board,
                            from,
                            piece.Color));
                    break;

                case PieceType.King:
                    moves.AddRange(
                        GetKingMoves(
                            board,
                            from,
                            piece.Color));
                    break;

                case PieceType.Cannon:
                    moves.AddRange(
                        GetCannonMoves(
                            board,
                            from,
                            piece.Color));
                    break;

                case PieceType.Pawn:
                    moves.AddRange(
                        GetPawnMoves(
                            board,
                            from,
                            piece.Color));
                    break;
            }

            return moves;
        }

        private static List<EngineMove> GetRookMoves(
            Board board,
            Position from,
            PieceColor color)
        {
            List<EngineMove> moves =
                new List<EngineMove>();

            int[] dR =
                { -1, 1, 0, 0 };

            int[] dC =
                { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r =
                    from.Row + dR[i];

                int c =
                    from.Col + dC[i];

                while (IsInsideBoard(r, c))
                {
                    Piece? target =
                        board.GetPieceAt(r, c);

                    if (target == null)
                    {
                        moves.Add(
                            new EngineMove(
                                from,
                                new Position(r, c)));
                    }
                    else
                    {
                        if (target.Color != color)
                        {
                            moves.Add(
                                new EngineMove(
                                    from,
                                    new Position(r, c),
                                    target));
                        }

                        break;
                    }

                    r += dR[i];
                    c += dC[i];
                }
            }

            return moves;
        }

        private static List<EngineMove> GetHorseMoves(
            Board board,
            Position from,
            PieceColor color)
        {
            List<EngineMove> moves =
                new List<EngineMove>();

            int[] blockRow =
                { -1, 1, 0, 0 };

            int[] blockCol =
                { 0, 0, -1, 1 };

            int[,] targetRow =
            {
                { -2, -2 },
                { 2, 2 },
                { -1, 1 },
                { -1, 1 }
            };

            int[,] targetCol =
            {
                { -1, 1 },
                { -1, 1 },
                { -2, -2 },
                { 2, 2 }
            };

            for (int i = 0; i < 4; i++)
            {
                int blockR =
                    from.Row + blockRow[i];

                int blockC =
                    from.Col + blockCol[i];

                if (!IsInsideBoard(
                    blockR,
                    blockC))
                {
                    continue;
                }

                if (board.GetPieceAt(
                    blockR,
                    blockC) != null)
                {
                    continue;
                }

                for (int j = 0; j < 2; j++)
                {
                    int r =
                        from.Row +
                        targetRow[i, j];

                    int c =
                        from.Col +
                        targetCol[i, j];

                    AddMoveIfValid(
                        board,
                        moves,
                        from,
                        r,
                        c,
                        color);
                }
            }

            return moves;
        }

        private static List<EngineMove> GetElephantMoves(
            Board board,
            Position from,
            PieceColor color)
        {
            List<EngineMove> moves =
                new List<EngineMove>();

            int[] dR =
                { -2, -2, 2, 2 };

            int[] dC =
                { -2, 2, -2, 2 };

            int[] eyeR =
                { -1, -1, 1, 1 };

            int[] eyeC =
                { -1, 1, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r =
                    from.Row + dR[i];

                int c =
                    from.Col + dC[i];

                int eyeRow =
                    from.Row + eyeR[i];

                int eyeCol =
                    from.Col + eyeC[i];

                if (!IsInsideBoard(r, c))
                    continue;

                if (!IsInsideBoard(
                    eyeRow,
                    eyeCol))
                {
                    continue;
                }

                bool crossRiver =
                    color == PieceColor.Red
                        ? r < 5
                        : r > 4;

                if (crossRiver)
                    continue;

                if (board.GetPieceAt(
                    eyeRow,
                    eyeCol) != null)
                {
                    continue;
                }

                AddMoveIfValid(
                    board,
                    moves,
                    from,
                    r,
                    c,
                    color);
            }

            return moves;
        }

        private static List<EngineMove> GetAdvisorMoves(
            Board board,
            Position from,
            PieceColor color)
        {
            List<EngineMove> moves =
                new List<EngineMove>();

            int[] dR =
                { -1, -1, 1, 1 };

            int[] dC =
                { -1, 1, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r =
                    from.Row + dR[i];

                int c =
                    from.Col + dC[i];

                if (!IsInPalace(
                    r,
                    c,
                    color))
                {
                    continue;
                }

                AddMoveIfValid(
                    board,
                    moves,
                    from,
                    r,
                    c,
                    color);
            }

            return moves;
        }

        private static List<EngineMove> GetKingMoves(
            Board board,
            Position from,
            PieceColor color)
        {
            List<EngineMove> moves =
                new List<EngineMove>();

            int[] dR =
                { -1, 1, 0, 0 };

            int[] dC =
                { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int r =
                    from.Row + dR[i];

                int c =
                    from.Col + dC[i];

                if (!IsInPalace(
                    r,
                    c,
                    color))
                {
                    continue;
                }

                AddMoveIfValid(
                    board,
                    moves,
                    from,
                    r,
                    c,
                    color);
            }

            return moves;
        }

        private static List<EngineMove> GetCannonMoves(
            Board board,
            Position from,
            PieceColor color)
        {
            List<EngineMove> moves =
                new List<EngineMove>();

            int[] dR =
                { -1, 1, 0, 0 };

            int[] dC =
                { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                bool foundMount =
                    false;

                int r =
                    from.Row + dR[i];

                int c =
                    from.Col + dC[i];

                while (IsInsideBoard(r, c))
                {
                    Piece? target =
                        board.GetPieceAt(r, c);

                    if (!foundMount)
                    {
                        if (target == null)
                        {
                            moves.Add(
                                new EngineMove(
                                    from,
                                    new Position(r, c)));
                        }
                        else
                        {
                            foundMount = true;
                        }
                    }
                    else
                    {
                        if (target != null)
                        {
                            if (target.Color != color)
                            {
                                moves.Add(
                                    new EngineMove(
                                        from,
                                        new Position(r, c),
                                        target));
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

        private static List<EngineMove> GetPawnMoves(
            Board board,
            Position from,
            PieceColor color)
        {
            List<EngineMove> moves =
                new List<EngineMove>();

            int forward =
                color == PieceColor.Red
                    ? -1
                    : 1;

            bool crossedRiver =
                color == PieceColor.Red
                    ? from.Row <= 4
                    : from.Row >= 5;

            AddMoveIfValid(
                board,
                moves,
                from,
                from.Row + forward,
                from.Col,
                color);

            if (crossedRiver)
            {
                AddMoveIfValid(
                    board,
                    moves,
                    from,
                    from.Row,
                    from.Col - 1,
                    color);

                AddMoveIfValid(
                    board,
                    moves,
                    from,
                    from.Row,
                    from.Col + 1,
                    color);
            }

            return moves;
        }

        private static bool IsInsideBoard(
            int row,
            int col)
        {
            return row >= 0 &&
                   row <= 9 &&
                   col >= 0 &&
                   col <= 8;
        }

        private static bool IsInPalace(
            int row,
            int col,
            PieceColor color)
        {
            if (col < 3 || col > 5)
                return false;

            if (color == PieceColor.Red)
            {
                return row >= 7 &&
                       row <= 9;
            }

            return row >= 0 &&
                   row <= 2;
        }

        private static void AddMoveIfValid(
            Board board,
            List<EngineMove> moves,
            Position from,
            int row,
            int col,
            PieceColor color)
        {
            if (!IsInsideBoard(row, col))
                return;

            Piece? target =
                board.GetPieceAt(row, col);

            if (target == null)
            {
                moves.Add(
                    new EngineMove(
                        from,
                        new Position(row, col)));
            }
            else if (target.Color != color)
            {
                moves.Add(
                    new EngineMove(
                        from,
                        new Position(row, col),
                        target));
            }
        }
    }
}