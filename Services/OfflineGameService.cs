using ChessCore.Mobile.Models;
using ChessCore.Mobile.Offline;
namespace ChessCore.Mobile.Services;

// Isolated practice games: no HTTP, account, Elo update, or database write.
public sealed class OfflineGameService
{
    private sealed class Session
    {
        public int Id;
        public Board Board = new();
        public GameStatus Status = GameStatus.InProgress;
        public readonly SemaphoreSlim Gate = new(1, 1);
        public readonly List<MoveHistoryDto> Moves = new();
        public readonly Dictionary<string, int> Positions = new();
        public string EndReason = "";
        public long Revision = 1;
        public DateTime Created = DateTime.UtcNow;
    }
    private readonly Dictionary<int, Session> _sessions = new();
    private readonly object _sync = new();
    private int _nextId;
    public Task<GameResponseDto> CreateAsync(CancellationToken token = default) => Task.Run(() =>
    {
        token.ThrowIfCancellationRequested();
        var session = new Session { Id = Interlocked.Decrement(ref _nextId) };
        session.Positions[session.Board.ToFen()] = 1;
        var dto = Snapshot(session);
        token.ThrowIfCancellationRequested();
        lock (_sync)
        {
            while (_sessions.Count >= 8) _sessions.Remove(_sessions.Keys.Max());
            _sessions.Add(session.Id, session);
        }
        return dto;
    }, token);
    private async Task<T> RunAsync<T>(int id, Func<Session, CancellationToken, T> action, CancellationToken token)
    {
        Session session;
        lock (_sync)
            if (!_sessions.TryGetValue(id, out session!)) throw new InvalidOperationException("Ván luyện tập không còn trong bộ nhớ. Hãy tạo ván mới.");
        await session.Gate.WaitAsync(token);
        try { return await Task.Run(() => { token.ThrowIfCancellationRequested(); return action(session, token); }, token); }
        finally { session.Gate.Release(); }
    }
    public Task<GameResponseDto> GetAsync(int id, CancellationToken token = default) => RunAsync(id, (s, _) => Snapshot(s), token);
    public Task<List<ValidMoveDto>> ValidMovesAsync(int id, int row, int col, CancellationToken token = default) => RunAsync(id, (s, _) =>
    {
        if (row is < 0 or > 9 || col is < 0 or > 8 || s.Status != GameStatus.InProgress || s.Board.CurrentTurn != PieceColor.Red) return new();
        var piece = s.Board.GetPieceAt(row, col);
        if (piece == null || piece.Color != PieceColor.Red) return new();
        return MoveGenerator.GetValidMoves(s.Board, new Position(row, col)).Select(m => new ValidMoveDto
        { Row = m.To.Row, Col = m.To.Col, IsCapture = s.Board.GetPieceAt(m.To.Row, m.To.Col) != null }).ToList();
    }, token);
    public Task<GameResponseDto> MoveAsync(int id, MakeMoveRequestDto move, CancellationToken token = default) => RunAsync(id, (s, ct) =>
    {
        if (s.Status != GameStatus.InProgress || s.Board.CurrentTurn != PieceColor.Red) throw new InvalidOperationException("Chưa đến lượt quân Đỏ.");
        if (move.FromX is < 0 or > 9 || move.ToX is < 0 or > 9 || move.FromY is < 0 or > 8 || move.ToY is < 0 or > 8) throw new InvalidOperationException("Tọa độ nằm ngoài bàn cờ.");
        var piece = s.Board.GetPieceAt(move.FromX, move.FromY);
        if (piece == null || piece.Color != PieceColor.Red) throw new InvalidOperationException("Hãy chọn quân Đỏ của bạn.");
        var legal = MoveGenerator.GetValidMoves(s.Board, new Position(move.FromX, move.FromY))
            .FirstOrDefault(m => m.To.Row == move.ToX && m.To.Col == move.ToY);
        if (legal == null) throw new InvalidOperationException("Nước đi này không hợp lệ.");
        ct.ThrowIfCancellationRequested();
        Apply(s, legal);
        return Snapshot(s);
    }, token);
    public Task<GameResponseDto> ComputerAsync(int id, int difficulty, CancellationToken token = default) => RunAsync(id, (s, ct) =>
    {
        // Resuming after cancellation never submits an extra AI move if Black already moved.
        if (s.Status != GameStatus.InProgress || s.Board.CurrentTurn != PieceColor.Black) return Snapshot(s);
        int depth = Math.Clamp(difficulty, 1, 4);
        int budgetMs = new[] { 250, 650, 1200, 2200 }[depth - 1];
        var engine = new MinimaxEngine(depth);
        var move = engine.FindBestMove(s.Board, PieceColor.Black, ct, TimeSpan.FromMilliseconds(budgetMs));
        ct.ThrowIfCancellationRequested();
        if (move != null) Apply(s, move);
        else { s.Status = GameRules.StatusAfterMove(s.Board); s.Revision++; }
        return Snapshot(s);
    }, token);
    public Task<GameResponseDto> ResignAsync(int id, CancellationToken token = default) => RunAsync(id, (s, _) =>
    {
        if (s.Status == GameStatus.InProgress) { s.Status = GameStatus.BlackWins; s.EndReason = "Resigned"; s.Revision++; }
        return Snapshot(s);
    }, token);
    private static void Apply(Session s, EngineMove move)
    {
        var piece = s.Board.GetPieceAt(move.From.Row, move.From.Col)!;
        s.Board.Grid[move.To.Row, move.To.Col] = piece;
        s.Board.Grid[move.From.Row, move.From.Col] = null;
        s.Board.SwitchTurn();
        s.Moves.Add(new MoveHistoryDto { Id = s.Moves.Count + 1, MoveOrder = s.Moves.Count,
            FromX = move.From.Row, FromY = move.From.Col, ToX = move.To.Row, ToY = move.To.Col,
            PieceType = piece.Type.ToString(), PieceColor = piece.Color.ToString(), MovedAt = DateTime.UtcNow });
        s.Status = GameRules.StatusAfterMove(s.Board);
        if (s.Status != GameStatus.InProgress)
            s.EndReason = MoveGenerator.CheckIfInCheck(s.Board, s.Board.CurrentTurn) ? "Checkmate" : "NoLegalMoves";
        var key = s.Board.ToFen();
        s.Positions.TryGetValue(key, out var count); s.Positions[key] = count + 1;
        if (s.Status == GameStatus.InProgress && count + 1 >= 3) { s.Status = GameStatus.Draw; s.EndReason = "Repetition"; }
        s.Revision++;
    }
    private static GameResponseDto Snapshot(Session s)
    {
        var red = BoardEvaluator.GetSideScores(s.Board, PieceColor.Red);
        var black = BoardEvaluator.GetSideScores(s.Board, PieceColor.Black);
        bool check = MoveGenerator.CheckIfInCheck(s.Board, s.Board.CurrentTurn);
        bool noMoves = !GameRules.HasLegalMove(s.Board, s.Board.CurrentTurn);
        var pieces = new List<PieceDto>();
        for (int row = 0; row < 10; row++) for (int col = 0; col < 9; col++)
            if (s.Board.GetPieceAt(row, col) is { } p) pieces.Add(new() { Row = row, Col = col, Type = p.Type.ToString(), Color = p.Color.ToString() });
        return new GameResponseDto
        {
            Id = s.Id, Status = s.Status.ToString(), CurrentTurn = s.Board.CurrentTurn.ToString(), Revision = s.Revision,
            IsCheck = check, HasNoLegalMoves = noMoves, IsCheckmate = check && noMoves,
            EndReason = s.EndReason, GameMode = "Offline", IsRated = false, RemainingSeconds = 0,
            CreatedAt = s.Created, LastMoveAt = s.Moves.LastOrDefault()?.MovedAt,
            Evaluation = new() { H1 = red.H1 - black.H1, H2 = red.H2 - black.H2, H3 = red.H3 - black.H3, H4 = red.H4 - black.H4 },
            RedScore = red.Total - black.Total, BlackScore = black.Total - red.Total,
            BoardPieces = pieces, Moves = s.Moves.Select(m => new MoveHistoryDto { Id=m.Id, MoveOrder=m.MoveOrder, FromX=m.FromX, FromY=m.FromY,
                ToX=m.ToX, ToY=m.ToY, PieceType=m.PieceType, PieceColor=m.PieceColor, MovedAt=m.MovedAt }).ToList()
        };
    }
}
