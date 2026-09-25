using System;
using System.Collections.Generic;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine
{
    public class MinimaxEngine
    {
        private readonly int _depth;

        public MinimaxEngine(int depth = 2)
        {
            _depth = Math.Max(1, depth);
        }

        public EngineMove? FindBestMove(
            Board board,
            PieceColor computerColor)
        {
            List<EngineMove> moves =
                GetAllLegalMoves(
                    board,
                    computerColor);

            if (moves.Count == 0)
                return null;

            EngineMove? bestMove = null;

            int bestScore =
                int.MinValue;

            foreach (EngineMove move in moves)
            {
                Board simulatedBoard =
                    CloneBoard(board);

                ApplyMove(
                    simulatedBoard,
                    move);

                int score =
                    Minimax(
                        simulatedBoard,
                        _depth - 1,
                        false,
                        computerColor);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private int Minimax(
            Board board,
            int depth,
            bool maximizing,
            PieceColor computerColor)
        {
            PieceColor opponentColor =
                computerColor == PieceColor.Red
                    ? PieceColor.Black
                    : PieceColor.Red;

            if (BoardEvaluator.IsCheckmate(
                board,
                opponentColor))
            {
                return 1000000 + depth;
            }

            if (BoardEvaluator.IsCheckmate(
                board,
                computerColor))
            {
                return -1000000 - depth;
            }

            if (BoardEvaluator.IsStalemate(
                    board,
                    computerColor) ||
                BoardEvaluator.IsStalemate(
                    board,
                    opponentColor))
            {
                return 0;
            }

            if (depth <= 0)
            {
                return BoardEvaluator.Evaluate(
                    board,
                    computerColor);
            }

            PieceColor sideToMove =
                maximizing
                    ? computerColor
                    : opponentColor;

            List<EngineMove> moves =
                GetAllLegalMoves(
                    board,
                    sideToMove);

            if (moves.Count == 0)
            {
                return BoardEvaluator.Evaluate(
                    board,
                    computerColor);
            }

            if (maximizing)
            {
                int bestScore =
                    int.MinValue;

                foreach (EngineMove move in moves)
                {
                    Board nextBoard =
                        CloneBoard(board);

                    ApplyMove(
                        nextBoard,
                        move);

                    int score =
                        Minimax(
                            nextBoard,
                            depth - 1,
                            false,
                            computerColor);

                    bestScore =
                        Math.Max(
                            bestScore,
                            score);
                }

                return bestScore;
            }

            int worstScore =
                int.MaxValue;

            foreach (EngineMove move in moves)
            {
                Board nextBoard =
                    CloneBoard(board);

                ApplyMove(
                    nextBoard,
                    move);

                int score =
                    Minimax(
                        nextBoard,
                        depth - 1,
                        true,
                        computerColor);

                worstScore =
                    Math.Min(
                        worstScore,
                        score);
            }

            return worstScore;
        }

        private List<EngineMove> GetAllLegalMoves(
            Board board,
            PieceColor color)
        {
            List<EngineMove> moves =
                new List<EngineMove>();

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece =
                        board.GetPieceAt(
                            row,
                            col);

                    if (piece == null)
                        continue;

                    if (piece.Color != color)
                        continue;

                    Position from =
                        new Position(
                            row,
                            col);

                    List<EngineMove> pieceMoves =
                        MoveGenerator.GetValidMoves(
                            board,
                            from);

                    moves.AddRange(pieceMoves);
                }
            }

            return moves;
        }

        private static Board CloneBoard(
            Board source)
        {
            Board clone =
                new Board();

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece =
                        source.GetPieceAt(
                            row,
                            col);

                    if (piece == null)
                    {
                        clone.Grid[row, col] =
                            null;
                    }
                    else
                    {
                        clone.Grid[row, col] =
                            new Piece(
                                piece.Type,
                                piece.Color);
                    }
                }
            }

            // Quan trọng:
            // Giữ nguyên lượt hiện tại của bàn cờ
            clone.SetCurrentTurn(
                source.CurrentTurn);

            return clone;
        }

        private static void ApplyMove(
            Board board,
            EngineMove move)
        {
            Piece? piece =
                board.GetPieceAt(
                    move.From.Row,
                    move.From.Col);

            if (piece == null)
                return;

            board.Grid[
                move.To.Row,
                move.To.Col] = piece;

            board.Grid[
                move.From.Row,
                move.From.Col] = null;

            board.SwitchTurn();
        }
    }
}