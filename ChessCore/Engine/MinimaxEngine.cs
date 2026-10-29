using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Engine;

public class EngineEvaluationStats
{
    public long NodesEvaluated { get; set; }
    public long TranspositionHits { get; set; }
    public long BetaCutoffs { get; set; }
    public double ElapsedMilliseconds { get; set; }
    public int Depth { get; set; }
    public int CompletedDepth { get; set; }
    public long QuiescenceNodes { get; set; }
}

public class MinimaxEngine
{
    private const int MateScore = 1000000;
    public const int QuiescenceCaptureDepth = 4;
    public const int MaxQuiescencePly = 8;
    private readonly EngineMove?[,] _killers = new EngineMove?[32, 2];
    private readonly int[,,] _history = new int[2, 90, 90];
    private readonly int _depth;
    private readonly Dictionary<(ulong Hash, int Depth), Entry> _table = new();
    private readonly record struct Entry(int Score, int Flag, EngineMove? BestMove);

    private CancellationToken _searchToken;
    private readonly Stopwatch _searchWatch = new();
    private int _timeBudgetMs = 3000;

    public EngineEvaluationStats LastStats { get; private set; } = new();
    public long NodesSearched => LastStats.NodesEvaluated;
    public long Cutoffs => LastStats.BetaCutoffs;

    public MinimaxEngine(int depth = 2) => _depth = Math.Clamp(depth, 1, 4);

    private void CheckSearchBudget()
    {
        _searchToken.ThrowIfCancellationRequested();
        if (_searchWatch.ElapsedMilliseconds >= _timeBudgetMs)
        {
            throw new TimeoutException("Hết ngân sách thời gian tìm kiếm.");
        }
    }

    public EngineMove? FindBestMove(Board board, PieceColor computerColor, CancellationToken cancellationToken = default, int maxTimeMs = 8000)
    {
        _searchToken = cancellationToken;
        _searchWatch.Restart();
        _timeBudgetMs = Math.Max(200, maxTimeMs);

        LastStats = new() { Depth = _depth };
        _table.Clear();
        Array.Clear(_killers);
        Array.Clear(_history);

        var searchBoard = board.Clone();
        searchBoard.SetCurrentTurn(computerColor);
        var moves = GameRules.GetAllLegalMoves(searchBoard, computerColor);
        if (moves.Count == 0) return null;

        EngineMove? bestMove = null;

        for (int depth = 1; depth <= _depth && moves.Count > 0; depth++)
        {
            try
            {
                CheckSearchBudget();
                OrderMoves(searchBoard, moves, bestMove, 0);
                int best = -MateScore * 2, alpha = best;
                EngineMove? iterationBest = null;

                foreach (var move in moves)
                {
                    CheckSearchBudget();
                    var captured = MakeMove(searchBoard, move);
                    int score;
                    try { score = Search(searchBoard, depth - 1, alpha, MateScore * 2, computerColor, 1); }
                    finally { UndoMove(searchBoard, move, captured); }
                    if (score > best) { best = score; iterationBest = move; }
                    alpha = Math.Max(alpha, best);
                }

                bestMove = iterationBest;
                LastStats.CompletedDepth = depth;
            }
            catch (TimeoutException)
            {
                // Hết ngân sách thời gian: Giữ nguyên kết quả của vòng lặp sâu nhất đã hoàn thành trọn vẹn
                break;
            }
        }

        _searchWatch.Stop();
        LastStats.ElapsedMilliseconds = _searchWatch.Elapsed.TotalMilliseconds;
        Console.WriteLine($"AI depth={_depth}, ms={LastStats.ElapsedMilliseconds:F0}, nodes={NodesSearched}, cutoffs={Cutoffs}, ttHits={LastStats.TranspositionHits}, qNodes={LastStats.QuiescenceNodes}, completed={LastStats.CompletedDepth}");
        
        // Luôn có nước đi hợp lệ dự phòng nếu bị ngắt trước khi depth 1 kịp hoàn tất
        return bestMove ?? moves.FirstOrDefault();
    }

    public int EvaluatePosition(Board board, PieceColor perspective)
    {
        LastStats = new() { Depth = _depth };
        _table.Clear();
        Array.Clear(_killers);
        Array.Clear(_history);
        _searchToken = CancellationToken.None;
        _searchWatch.Restart();
        _timeBudgetMs = 5000;

        int score = Search(board.Clone(), _depth, -MateScore * 2, MateScore * 2, perspective, 0);
        _searchWatch.Stop();
        LastStats.CompletedDepth = _depth;
        LastStats.ElapsedMilliseconds = _searchWatch.Elapsed.TotalMilliseconds;
        return score;
    }

    private int Search(Board board, int depth, int alpha, int beta, PieceColor perspective, int ply)
    {
        if ((LastStats.NodesEvaluated & 127) == 0) CheckSearchBudget();

        if (depth == 0) return Quiescence(board, alpha, beta, perspective, ply, 0);
        LastStats.NodesEvaluated++;
        var side = board.CurrentTurn;

        // Sinh nước đi 1 lần duy nhất, tránh gọi HasLegalMove rồi lại GetAllLegalMoves
        var moves = GameRules.GetAllLegalMoves(board, side);
        if (moves.Count == 0)
            return side == perspective ? -MateScore + ply : MateScore - ply;

        var key = (ZobristHashing.ComputeHash(board), depth);
        int originalAlpha = alpha, originalBeta = beta;
        EngineMove? preferred = null;
        if (_table.TryGetValue(key, out var entry))
        {
            preferred = entry.BestMove;
            int cached = FromTableScore(entry.Score, ply);
            LastStats.TranspositionHits++;
            if (entry.Flag == 0) return cached;
            if (entry.Flag == 1) alpha = Math.Max(alpha, cached);
            else beta = Math.Min(beta, cached);
            if (alpha >= beta) return cached;
        }

        bool maximizing = side == perspective;
        int best = maximizing ? -MateScore * 2 : MateScore * 2;
        OrderMoves(board, moves, preferred, ply);
        EngineMove? bestMove = null;

        foreach (var move in moves)
        {
            var captured = MakeMove(board, move);
            int score;
            try { score = Search(board, depth - 1, alpha, beta, perspective, ply + 1); }
            finally { UndoMove(board, move, captured); }
            if ((maximizing && score > best) || (!maximizing && score < best))
            { best = score; bestMove = move; }
            if (maximizing) alpha = Math.Max(alpha, best); else beta = Math.Min(beta, best);
            if (alpha >= beta)
            {
                LastStats.BetaCutoffs++;
                if (captured == null) RecordQuietCutoff(board, move, depth, ply);
                break;
            }
        }
        if (_table.Count < 250000)
            _table[key] = new(ToTableScore(best, ply), best <= originalAlpha ? 2 : best >= originalBeta ? 1 : 0, bestMove);
        return best;
    }

    private static Piece? MakeMove(Board board, EngineMove move)
    {
        var captured = board.Grid[move.To.Row, move.To.Col];
        board.Grid[move.To.Row, move.To.Col] = board.Grid[move.From.Row, move.From.Col];
        board.Grid[move.From.Row, move.From.Col] = null;
        board.SwitchTurn();
        return captured;
    }

    private static void UndoMove(Board board, EngineMove move, Piece? captured)
    {
        board.SwitchTurn();
        board.Grid[move.From.Row, move.From.Col] = board.Grid[move.To.Row, move.To.Col];
        board.Grid[move.To.Row, move.To.Col] = captured;
    }

    private int Quiescence(Board board, int alpha, int beta, PieceColor perspective, int ply, int qPly)
    {
        if ((LastStats.NodesEvaluated & 127) == 0) CheckSearchBudget();

        LastStats.NodesEvaluated++;
        LastStats.QuiescenceNodes++;
        bool maximizing = board.CurrentTurn == perspective;
        bool inCheck = MoveGenerator.CheckIfInCheck(board, board.CurrentTurn);
        var moves = GameRules.GetAllLegalMoves(board, board.CurrentTurn);
        if (moves.Count == 0) return maximizing ? -MateScore + ply : MateScore - ply;
        int standPat = BoardEvaluator.Evaluate(board, perspective);

        if (qPly >= MaxQuiescencePly) return standPat;
        int best = maximizing ? -MateScore * 2 : MateScore * 2;
        if (!inCheck)
        {
            best = standPat;
            if (maximizing) { if (best >= beta) return best; alpha = Math.Max(alpha, best); }
            else { if (best <= alpha) return best; beta = Math.Min(beta, best); }
            if (qPly >= QuiescenceCaptureDepth) return best;
            moves.RemoveAll(m => board.Grid[m.To.Row, m.To.Col] == null);
        }
        OrderMoves(board, moves, null, ply);
        foreach (var move in moves)
        {
            var captured = MakeMove(board, move);
            int score;
            try { score = Quiescence(board, alpha, beta, perspective, ply + 1, qPly + 1); }
            finally { UndoMove(board, move, captured); }
            best = maximizing ? Math.Max(best, score) : Math.Min(best, score);
            if (maximizing) alpha = Math.Max(alpha, best); else beta = Math.Min(beta, best);
            if (alpha >= beta) { LastStats.BetaCutoffs++; break; }
        }
        return best;
    }

    private void OrderMoves(Board board, List<EngineMove> moves, EngineMove? ttMove, int ply)
    {
        int killerIndex = Math.Clamp(ply, 0, 31);
        int sideIndex = board.CurrentTurn == PieceColor.Red ? 0 : 1;
        moves.Sort((a, b) => ScoreMove(b, board, ttMove, killerIndex, sideIndex).CompareTo(ScoreMove(a, board, ttMove, killerIndex, sideIndex)));
    }

    private int ScoreMove(EngineMove move, Board board, EngineMove? ttMove, int killerIndex, int sideIndex)
    {
        if (ttMove != null && SameMove(move, ttMove)) return 1_000_000;
        var captured = board.Grid[move.To.Row, move.To.Col];
        if (captured != null)
        {
            var moving = board.Grid[move.From.Row, move.From.Col];
            return 100_000 + PieceValue(captured.Type) * 10 - (moving != null ? PieceValue(moving.Type) : 0);
        }
        if (_killers[killerIndex, 0] != null && SameMove(move, _killers[killerIndex, 0]!)) return 9_000;
        if (_killers[killerIndex, 1] != null && SameMove(move, _killers[killerIndex, 1]!)) return 8_000;
        return _history[sideIndex, move.From.Row * 9 + move.From.Col, move.To.Row * 9 + move.To.Col];
    }

    private void RecordQuietCutoff(Board board, EngineMove move, int depth, int ply)
    {
        int killerIndex = Math.Clamp(ply, 0, 31);
        if (_killers[killerIndex, 0] == null || !SameMove(_killers[killerIndex, 0]!, move))
        {
            _killers[killerIndex, 1] = _killers[killerIndex, 0];
            _killers[killerIndex, 0] = move;
        }
        int sideIndex = board.CurrentTurn == PieceColor.Red ? 0 : 1;
        int bonus = depth * depth;
        int fromIdx = move.From.Row * 9 + move.From.Col;
        int toIdx = move.To.Row * 9 + move.To.Col;
        _history[sideIndex, fromIdx, toIdx] = Math.Min(_history[sideIndex, fromIdx, toIdx] + bonus, 50_000);
    }

    private static bool SameMove(EngineMove a, EngineMove b) =>
        a.From.Row == b.From.Row && a.From.Col == b.From.Col &&
        a.To.Row == b.To.Row && a.To.Col == b.To.Col;

    private static int PieceValue(PieceType type) => type switch
    {
        PieceType.King => 10000,
        PieceType.Rook => 900,
        PieceType.Cannon => 450,
        PieceType.Horse => 400,
        PieceType.Elephant => 200,
        PieceType.Advisor => 200,
        PieceType.Pawn => 100,
        _ => 0
    };

    private static int ToTableScore(int score, int ply)
    {
        if (score >= MateScore - 100) return score + ply;
        if (score <= -MateScore + 100) return score - ply;
        return score;
    }

    private static int FromTableScore(int score, int ply)
    {
        if (score >= MateScore - 100) return score - ply;
        if (score <= -MateScore + 100) return score + ply;
        return score;
    }
}
