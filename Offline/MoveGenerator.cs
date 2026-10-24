#nullable enable annotations
#nullable disable warnings
using System;
using System.Collections.Generic;



namespace ChessCore.Mobile.Offline
{
    public static class MoveGenerator
    {
        // Lấy toàn bộ nước đi hợp lệ của một bên.
        // Dùng cho checkmate / hết nước đi / AI.
        public static List<EngineMove> GetAllValidMoves(
            Board board,
            PieceColor color)
        {
            List<EngineMove> moves = new List<EngineMove>();

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece = board.GetPieceAt(row, col);

                    if (piece == null || piece.Color != color)
                        continue;

                    moves.AddRange(
                        GetValidMoves(
                            board,
                            new Position(row, col)));
                }
            }

            return moves;
        }

        public static bool HasAnyLegalMoves(
            Board board,
            PieceColor color)
        {
            return GameRules.HasLegalMove(board, color);
        }

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

                // Theo luật, không có nước "ăn Tướng".
                // Ván cờ kết thúc bằng chiếu bí / hết nước đi.
                if (captured != null && captured.Type == PieceType.King)
                    continue;

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

        public static bool CheckIfInCheck(Board board, PieceColor color)
        {
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 9; c++)
                {
                    var p = board.GetPieceAt(r, c);
                    if (p?.Type == PieceType.King && p.Color == color)
                        return IsSquareAttacked(board, r, c, color == PieceColor.Red ? PieceColor.Black : PieceColor.Red);
                }
            return true; // A missing king is never a safe position.
        }

        // Geometric attacks include friendly occupied targets, so H3 can count defenders.
        public static bool IsSquareAttacked(Board board, int row, int col, PieceColor color)
        {
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 9; c++)
                {
                    var p = board.GetPieceAt(r, c);
                    if (p == null || p.Color != color || (r == row && c == col)) continue;
                    int dr = row - r, dc = col - c;
                    int ar = Math.Abs(dr), ac = Math.Abs(dc);
                    switch (p.Type)
                    {
                        case PieceType.Horse:
                            if (ar == 2 && ac == 1 && board.GetPieceAt(r + Math.Sign(dr), c) == null) return true;
                            if (ar == 1 && ac == 2 && board.GetPieceAt(r, c + Math.Sign(dc)) == null) return true;
                            break;
                        case PieceType.Elephant:
                            if (ar == 2 && ac == 2 && (color == PieceColor.Red ? row >= 5 : row <= 4)
                                && board.GetPieceAt(r + dr / 2, c + dc / 2) == null) return true;
                            break;
                        case PieceType.Advisor:
                            if (ar == 1 && ac == 1 && IsInPalace(row, col, color)) return true;
                            break;
                        case PieceType.Pawn:
                            if (dc == 0 && dr == (color == PieceColor.Red ? -1 : 1)) return true;
                            if (dr == 0 && ac == 1 && (color == PieceColor.Red ? r <= 4 : r >= 5)) return true;
                            break;
                        case PieceType.King:
                            if (ar + ac == 1 && IsInPalace(row, col, color)) return true;
                            if (dc != 0 || board.GetPieceAt(row, col)?.Type != PieceType.King) break;
                            goto case PieceType.Rook;
                        case PieceType.Rook:
                        case PieceType.Cannon:
                            if (dr != 0 && dc != 0) break;
                            int screens = 0;
                            int sr = Math.Sign(dr), sc = Math.Sign(dc);
                            for (int rr = r + sr, cc = c + sc; rr != row || cc != col; rr += sr, cc += sc)
                                if (board.GetPieceAt(rr, cc) != null) screens++;
                            if (screens == (p.Type == PieceType.Cannon ? 1 : 0)) return true;
                            break;
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