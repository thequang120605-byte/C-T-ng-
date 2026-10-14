using System.Diagnostics;
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
    // Extra tactical plies, not a replacement for requested full-width depth.
    public const int QuiescenceCaptureDepth = 4;
    public const int MaxQuiescencePly = 8;
    private readonly EngineMove?[,] _killers = new EngineMove?[32, 2];
    private readonly int[,,] _history = new int[2, 90, 90];
    private readonly int _depth;
    // A table belongs to one search/perspective. No cross-request shared writes.
    private readonly Dictionary<(ulong Hash, int Depth), Entry> _table = new();
    private readonly record struct Entry(int Score, int Flag, EngineMove? BestMove);
    public EngineEvaluationStats LastStats { get; private set; } = new();
    public long NodesSearched => LastStats.NodesEvaluated;
    public long Cutoffs => LastStats.BetaCutoffs;

    public MinimaxEngine(int depth = 2) => _depth = Math.Clamp(depth, 1, 4);

    public EngineMove? FindBestMove(Board board, PieceColor computerColor)
    {
        var watch = Stopwatch.StartNew();
        LastStats = new() { Depth = _depth };
        _table.Clear();
        // The caller's board (including side to move) is never modified.
        var searchBoard = board.Clone();
        searchBoard.SetCurrentTurn(computerColor);
        var moves = GameRules.GetAllLegalMoves(searchBoard, computerColor);
        EngineMove? bestMove = null;
        // Iterative deepening supplies a principal move for ordering. Each iteration
        // completes to its full depth; it does not silently replace depth 4 by depth 2.
        Array.Clear(_killers);
        Array.Clear(_history);
        for (int depth = 1; depth <= _depth && moves.Count > 0; depth++)
        {
            OrderMoves(searchBoard, moves, bestMove, 0);
            int best = -MateScore * 2, alpha = best;
            EngineMove? iterationBest = null;
            foreach (var move in moves)
            {
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
        watch.Stop();
        LastStats.ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds;
        Console.WriteLine($"AI depth={_depth}, ms={LastStats.ElapsedMilliseconds:F0}, nodes={NodesSearched}, cutoffs={Cutoffs}, ttHits={LastStats.TranspositionHits}, qNodes={LastStats.QuiescenceNodes}, completed={LastStats.CompletedDepth}");
        return bestMove;
    }

    public int EvaluatePosition(Board board, PieceColor perspective)
    {
        LastStats = new() { Depth = _depth };
        _table.Clear();
        Array.Clear(_killers);
        Array.Clear(_history);
        var watch = Stopwatch.StartNew();
        int score = Search(board.Clone(), _depth, -MateScore * 2, MateScore * 2, perspective, 0);
        LastStats.CompletedDepth = _depth;
        LastStats.ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds;
        return score;
    }

    private int Search(Board board, int depth, int alpha, int beta, PieceColor perspective, int ply)
    {
        if (depth == 0) return Quiescence(board, alpha, beta, perspective, ply, 0);
        LastStats.NodesEvaluated++;
        var side = board.CurrentTurn;
        // Xiangqi: the side to move loses if it has no legal move, even without check.
        if (!GameRules.HasLegalMove(board, side))
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
        var moves = GameRules.GetAllLegalMoves(board, side);
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

    // At a quiet horizon we allow stand-pat; when checked we search ALL evasions.
    // No delta-pruning of sacrifices: a cheap-looking capture can be a real tactic.
    private int Quiescence(Board board, int alpha, int beta, PieceColor perspective, int ply, int qPly)
    {
        LastStats.NodesEvaluated++;
        LastStats.QuiescenceNodes++;
        bool maximizing = board.CurrentTurn == perspective;
        bool inCheck = MoveGenerator.CheckIfInCheck(board, board.CurrentTurn);
        var moves = GameRules.GetAllLegalMoves(board, board.CurrentTurn);
        if (moves.Count == 0) return maximizing ? -MateScore + ply : MateScore - ply;
        int standPat = BoardEvaluator.Evaluate(board, perspective);
        // A finite tactical horizon is necessary for sequences of repeated checks.
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

    private static int ToTableScore(int score, int ply) => score > MateScore - 1000 ? score + ply : score < -MateScore + 1000 ? score - ply : score;
    private static int FromTableScore(int score, int ply) => score > MateScore - 1000 ? score - ply : score < -MateScore + 1000 ? score + ply : score;
    private static bool SameMove(EngineMove? a, EngineMove? b) => a != null && b != null && a.From == b.From && a.To == b.To;

    private void RecordQuietCutoff(Board board, EngineMove move, int depth, int ply)
    {
        if (ply < 32 && !SameMove(move, _killers[ply, 0]))
        { _killers[ply, 1] = _killers[ply, 0]; _killers[ply, 0] = move; }
        int side = board.CurrentTurn == PieceColor.Red ? 0 : 1;
        int from = move.From.Row * 9 + move.From.Col, to = move.To.Row * 9 + move.To.Col;
        _history[side, from, to] = Math.Min(20000, _history[side, from, to] + depth * depth);
    }

    private void OrderMoves(Board board, List<EngineMove> moves, EngineMove? preferred, int ply)
    {
        // Cache ordering scores once; the comparison delegate must not rescan the board.
        var ranked = moves.Select(m => (Move: m, Score: MoveScore(board, m, preferred, ply))).ToList();
        ranked.Sort((a, b) => b.Score.CompareTo(a.Score));
        for (int i = 0; i < ranked.Count; i++) moves[i] = ranked[i].Move;
    }

    private int MoveScore(Board board, EngineMove move, EngineMove? preferred, int ply)
    {
        if (SameMove(move, preferred)) return 1000000;
        var victim = board.Grid[move.To.Row, move.To.Col];
        var attacker = board.Grid[move.From.Row, move.From.Col];
        // Captures first is only a SEARCH ORDER, never a bonus in the returned value.
        if (victim != null) return 100000 + BoardEvaluator.GetPieceValue(victim.Type, victim.Color, move.To.Row) * 10
            - (attacker == null ? 0 : BoardEvaluator.GetPieceValue(attacker.Type));
        if (ply < 32 && SameMove(move, _killers[ply, 0])) return 80000;
        if (ply < 32 && SameMove(move, _killers[ply, 1])) return 70000;
        int side = board.CurrentTurn == PieceColor.Red ? 0 : 1;
        return _history[side, move.From.Row * 9 + move.From.Col, move.To.Row * 9 + move.To.Col];
    }
}
