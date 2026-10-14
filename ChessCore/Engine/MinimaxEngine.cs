using System;
using System.Collections.Generic;
using System.Diagnostics;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine
{
    public enum HashEntryType : byte
    {
        Exact,
        Alpha,
        Beta
    }

    public struct TranspositionEntry
    {
        public ulong Hash;
        public int Score;
        public int Depth;
        public HashEntryType Flag;
    }

    public class EngineEvaluationStats
    {
        public long NodesEvaluated { get; set; }
        public long TranspositionHits { get; set; }
        public long BetaCutoffs { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public int Depth { get; set; }
    }

    public class MinimaxEngine
    {
        private readonly int _depth;
        private const int TableSize = 1048576;
        private static readonly TranspositionEntry[] TranspositionTable = new TranspositionEntry[TableSize];

        public EngineEvaluationStats LastStats { get; private set; } = new();

        public MinimaxEngine(int depth = 2)
        {
            _depth = Math.Max(1, depth);
        }

        public EngineMove? FindBestMove(Board board, PieceColor computerColor)
        {
            var sw = Stopwatch.StartNew();
            LastStats = new EngineEvaluationStats { Depth = _depth };

            List<EngineMove> moves = GetAllLegalMoves(board, computerColor);
            if (moves.Count == 0) return null;

            // Sắp xếp nước đi theo MVV-LVA
            OrderMoves(board, moves);

            EngineMove? bestMove = null;
            int bestScore = int.MinValue;
            int alpha = int.MinValue;
            int beta = int.MaxValue;

            foreach (EngineMove move in moves)
            {
                Board simulatedBoard = CloneBoard(board);
                ApplyMove(simulatedBoard, move);

                int score = AlphaBeta(simulatedBoard, _depth - 1, alpha, beta, false, computerColor);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, bestScore);
                if (alpha >= beta)
                {
                    LastStats.BetaCutoffs++;
                    break;
                }
            }

            sw.Stop();
            LastStats.ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds;
            return bestMove ?? moves[0];
        }

        public int EvaluatePosition(Board board, PieceColor perspective)
        {
            bool maximizing = board.CurrentTurn == perspective;
            return AlphaBeta(board, _depth, int.MinValue, int.MaxValue, maximizing, perspective);
        }

        private int AlphaBeta(
            Board board,
            int depth,
            int alpha,
            int beta,
            bool maximizing,
            PieceColor computerColor)
        {
            LastStats.NodesEvaluated++;

            PieceColor opponentColor = computerColor == PieceColor.Red ? PieceColor.Black : PieceColor.Red;

            if (BoardEvaluator.IsCheckmate(board, opponentColor))
                return 1000000 + depth;

            if (BoardEvaluator.IsCheckmate(board, computerColor))
                return -1000000 - depth;

            if (BoardEvaluator.IsStalemate(board, computerColor) || BoardEvaluator.IsStalemate(board, opponentColor))
                return 0;

            if (depth <= 0)
            {
                return BoardEvaluator.Evaluate(board, computerColor);
            }

            // Tra cứu Bảng chuyển vị
            ulong hash = ZobristHashing.ComputeHash(board);
            int index = (int)(hash % (ulong)TableSize);
            ref TranspositionEntry entry = ref TranspositionTable[index];

            if (entry.Hash == hash && entry.Depth >= depth)
            {
                LastStats.TranspositionHits++;
                if (entry.Flag == HashEntryType.Exact)
                    return entry.Score;
                if (entry.Flag == HashEntryType.Alpha && entry.Score <= alpha)
                    return alpha;
                if (entry.Flag == HashEntryType.Beta && entry.Score >= beta)
                    return beta;
            }

            int originalAlpha = alpha;
            PieceColor sideToMove = maximizing ? computerColor : opponentColor;
            List<EngineMove> moves = GetAllLegalMoves(board, sideToMove);

            if (moves.Count == 0)
            {
                return BoardEvaluator.Evaluate(board, computerColor);
            }

            OrderMoves(board, moves);

            int bestScore = maximizing ? int.MinValue : int.MaxValue;

            if (maximizing)
            {
                foreach (EngineMove move in moves)
                {
                    Board nextBoard = CloneBoard(board);
                    ApplyMove(nextBoard, move);

                    int score = AlphaBeta(nextBoard, depth - 1, alpha, beta, false, computerColor);
                    bestScore = Math.Max(bestScore, score);
                    alpha = Math.Max(alpha, bestScore);

                    if (alpha >= beta)
                    {
                        LastStats.BetaCutoffs++;
                        break;
                    }
                }
            }
            else
            {
                foreach (EngineMove move in moves)
                {
                    Board nextBoard = CloneBoard(board);
                    ApplyMove(nextBoard, move);

                    int score = AlphaBeta(nextBoard, depth - 1, alpha, beta, true, computerColor);
                    bestScore = Math.Min(bestScore, score);
                    beta = Math.Min(beta, bestScore);

                    if (alpha >= beta)
                    {
                        LastStats.BetaCutoffs++;
                        break;
                    }
                }
            }

            entry.Hash = hash;
            entry.Score = bestScore;
            entry.Depth = depth;
            if (bestScore <= originalAlpha)
                entry.Flag = HashEntryType.Alpha;
            else if (bestScore >= beta)
                entry.Flag = HashEntryType.Beta;
            else
                entry.Flag = HashEntryType.Exact;

            return bestScore;
        }

        private void OrderMoves(Board board, List<EngineMove> moves)
        {
            moves.Sort((a, b) =>
            {
                int scoreA = 0;
                int scoreB = 0;

                Piece? targetA = board.GetPieceAt(a.To.Row, a.To.Col);
                if (targetA != null)
                {
                    Piece? moverA = board.GetPieceAt(a.From.Row, a.From.Col);
                    scoreA = GetPieceValue(targetA.Type) * 10 - (moverA != null ? GetPieceValue(moverA.Type) : 0);
                }

                Piece? targetB = board.GetPieceAt(b.To.Row, b.To.Col);
                if (targetB != null)
                {
                    Piece? moverB = board.GetPieceAt(b.From.Row, b.From.Col);
                    scoreB = GetPieceValue(targetB.Type) * 10 - (moverB != null ? GetPieceValue(moverB.Type) : 0);
                }

                return scoreB.CompareTo(scoreA);
            });
        }

        private static int GetPieceValue(PieceType type)
        {
            return type switch
            {
                PieceType.King => 100000,
                PieceType.Rook => 900,
                PieceType.Cannon => 450,
                PieceType.Horse => 400,
                PieceType.Elephant => 200,
                PieceType.Advisor => 200,
                PieceType.Pawn => 100,
                _ => 0
            };
        }

        private List<EngineMove> GetAllLegalMoves(Board board, PieceColor color)
        {
            List<EngineMove> moves = new List<EngineMove>();

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece = board.GetPieceAt(row, col);
                    if (piece == null || piece.Color != color)
                        continue;

                    Position from = new Position(row, col);
                    List<EngineMove> pieceMoves = MoveGenerator.GetValidMoves(board, from);
                    moves.AddRange(pieceMoves);
                }
            }

            return moves;
        }

        private static Board CloneBoard(Board source)
        {
            Board clone = new Board();

            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    Piece? piece = source.GetPieceAt(row, col);
                    if (piece == null)
                    {
                        clone.Grid[row, col] = null;
                    }
                    else
                    {
                        clone.Grid[row, col] = new Piece(piece.Type, piece.Color);
                    }
                }
            }

            clone.SetCurrentTurn(source.CurrentTurn);
            return clone;
        }

        private static void ApplyMove(Board board, EngineMove move)
        {
            Piece? piece = board.GetPieceAt(move.From.Row, move.From.Col);
            if (piece == null) return;

            board.Grid[move.To.Row, move.To.Col] = piece;
            board.Grid[move.From.Row, move.From.Col] = null;
            board.SwitchTurn();
        }
    }
}