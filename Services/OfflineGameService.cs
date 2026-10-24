using System.Text.Json;
using ChessCore.Mobile.Models;
using ChessCore.Mobile.Offline;

namespace ChessCore.Mobile.Services;

// Isolated practice games: no HTTP, account, Elo update, or database write.
public sealed class OfflineGameService
{
    public sealed class Session
    {
        public int Id { get; set; }
        public Board Board { get; set; } = new();
        public GameStatus Status { get; set; } = GameStatus.InProgress;
        public readonly SemaphoreSlim Gate = new(1, 1);
        public List<MoveHistoryDto> Moves { get; set; } = new();
        public Dictionary<string, int> Positions { get; set; } = new();
        public string EndReason { get; set; } = "";
        public long Revision { get; set; } = 1;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public string GameMode { get; set; } = "PvE"; // "PvE" or "PassAndPlay"
        public string Title { get; set; } = "Luyện cờ với máy";
        public string? InitialFen { get; set; }
    }

    private readonly Dictionary<int, Session> _sessions = new();
    private readonly object _sync = new();
    private int _nextId;
    private bool _loadedFromDisk;

    public OfflineGameService()
    {
        TryLoadFromDisk();
    }

    public Task<GameResponseDto> CreateAsync(CancellationToken token = default) =>
        CreateSessionAsync("PvE", null, "Luyện cờ với máy", token);

    public Task<GameResponseDto> CreateSessionAsync(string mode = "PvE", string? fen = null, string title = "Luyện cờ", CancellationToken token = default) => Task.Run(() =>
    {
        token.ThrowIfCancellationRequested();
        var session = new Session
        {
            Id = Interlocked.Decrement(ref _nextId),
            GameMode = mode,
            Title = title,
            InitialFen = fen
        };

        if (!string.IsNullOrWhiteSpace(fen))
        {
            if (Board.TryParseFen(fen, out var customBoard, out _))
            {
                session.Board = customBoard!;
            }
        }

        session.Positions[session.Board.ToFen()] = 1;
        var dto = Snapshot(session);
        token.ThrowIfCancellationRequested();

        lock (_sync)
        {
            while (_sessions.Count >= 8) _sessions.Remove(_sessions.Keys.Max());
            _sessions.Add(session.Id, session);
        }

        SaveToDisk();
        return dto;
    }, token);

    private async Task<T> RunAsync<T>(int id, Func<Session, CancellationToken, T> action, CancellationToken token)
    {
        Session session;
        lock (_sync)
        {
            if (!_sessions.TryGetValue(id, out session!))
                throw new InvalidOperationException("Ván luyện tập không còn trong bộ nhớ. Hãy tạo ván mới.");
        }
        await session.Gate.WaitAsync(token);
        try
        {
            return await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var result = action(session, token);
                SaveToDisk();
                return result;
            }, token);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public Task<GameResponseDto> GetAsync(int id, CancellationToken token = default) => RunAsync(id, (s, _) => Snapshot(s), token);

    public Task<List<ValidMoveDto>> ValidMovesAsync(int id, int row, int col, CancellationToken token = default) => RunAsync(id, (s, _) =>
    {
        if (row is < 0 or > 9 || col is < 0 or > 8 || s.Status != GameStatus.InProgress) return new();
        var piece = s.Board.GetPieceAt(row, col);
        if (piece == null) return new();

        bool isTurn = s.GameMode == "PassAndPlay"
            ? s.Board.CurrentTurn == piece.Color
            : (s.Board.CurrentTurn == PieceColor.Red && piece.Color == PieceColor.Red);

        if (!isTurn) return new();

        return MoveGenerator.GetValidMoves(s.Board, new Position(row, col)).Select(m => new ValidMoveDto
        {
            Row = m.To.Row,
            Col = m.To.Col,
            IsCapture = s.Board.GetPieceAt(m.To.Row, m.To.Col) != null
        }).ToList();
    }, token);

    public Task<GameResponseDto> MoveAsync(int id, MakeMoveRequestDto move, CancellationToken token = default) => RunAsync(id, (s, ct) =>
    {
        if (s.Status != GameStatus.InProgress) throw new InvalidOperationException("Ván cờ đã kết thúc.");
        if (move.FromX is < 0 or > 9 || move.ToX is < 0 or > 9 || move.FromY is < 0 or > 8 || move.ToY is < 0 or > 8)
            throw new InvalidOperationException("Tọa độ nằm ngoài bàn cờ.");

        var piece = s.Board.GetPieceAt(move.FromX, move.FromY);
        if (piece == null) throw new InvalidOperationException("Hãy chọn một quân cờ.");

        bool isTurn = s.GameMode == "PassAndPlay"
            ? s.Board.CurrentTurn == piece.Color
            : (s.Board.CurrentTurn == PieceColor.Red && piece.Color == PieceColor.Red);

        if (!isTurn)
            throw new InvalidOperationException($"Chưa đến lượt quân {(s.Board.CurrentTurn == PieceColor.Red ? "Đỏ" : "Đen")}.");

        var legal = MoveGenerator.GetValidMoves(s.Board, new Position(move.FromX, move.FromY))
            .FirstOrDefault(m => m.To.Row == move.ToX && m.To.Col == move.ToY);
        if (legal == null) throw new InvalidOperationException("Nước đi này không hợp lệ.");

        ct.ThrowIfCancellationRequested();
        Apply(s, legal);
        return Snapshot(s);
    }, token);

    public Task<GameResponseDto> ComputerAsync(int id, int difficulty, CancellationToken token = default) => RunAsync(id, (s, ct) =>
    {
        if (s.Status != GameStatus.InProgress || s.Board.CurrentTurn != PieceColor.Black || s.GameMode == "PassAndPlay")
            return Snapshot(s);

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
        if (s.Status == GameStatus.InProgress)
        {
            s.Status = s.Board.CurrentTurn == PieceColor.Red ? GameStatus.BlackWins : GameStatus.RedWins;
            s.EndReason = "Resigned";
            s.Revision++;
        }
        return Snapshot(s);
    }, token);

    public List<GameResponseDto> GetSavedHistory()
    {
        lock (_sync)
        {
            return _sessions.Values
                .OrderByDescending(s => s.Created)
                .Select(Snapshot)
                .ToList();
        }
    }

    private static void Apply(Session s, EngineMove move)
    {
        var piece = s.Board.GetPieceAt(move.From.Row, move.From.Col)!;
        s.Board.Grid[move.To.Row, move.To.Col] = piece;
        s.Board.Grid[move.From.Row, move.From.Col] = null;
        s.Board.SwitchTurn();
        s.Moves.Add(new MoveHistoryDto
        {
            Id = s.Moves.Count + 1,
            MoveOrder = s.Moves.Count,
            FromX = move.From.Row,
            FromY = move.From.Col,
            ToX = move.To.Row,
            ToY = move.To.Col,
            PieceType = piece.Type.ToString(),
            PieceColor = piece.Color.ToString(),
            MovedAt = DateTime.UtcNow
        });
        s.Status = GameRules.StatusAfterMove(s.Board);
        if (s.Status != GameStatus.InProgress)
            s.EndReason = MoveGenerator.CheckIfInCheck(s.Board, s.Board.CurrentTurn) ? "Checkmate" : "NoLegalMoves";
        var key = s.Board.ToFen();
        s.Positions.TryGetValue(key, out var count);
        s.Positions[key] = count + 1;
        if (s.Status == GameStatus.InProgress && count + 1 >= 3)
        {
            s.Status = GameStatus.Draw;
            s.EndReason = "Repetition";
        }
        s.Revision++;
    }

    public static GameResponseDto Snapshot(Session s)
    {
        var red = BoardEvaluator.GetSideScores(s.Board, PieceColor.Red);
        var black = BoardEvaluator.GetSideScores(s.Board, PieceColor.Black);
        bool check = MoveGenerator.CheckIfInCheck(s.Board, s.Board.CurrentTurn);
        bool noMoves = !GameRules.HasLegalMove(s.Board, s.Board.CurrentTurn);
        var pieces = new List<PieceDto>();
        for (int row = 0; row < 10; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (s.Board.GetPieceAt(row, col) is { } p)
                {
                    pieces.Add(new() { Row = row, Col = col, Type = p.Type.ToString(), Color = p.Color.ToString() });
                }
            }
        }

        return new GameResponseDto
        {
            Id = s.Id,
            Status = s.Status.ToString(),
            CurrentTurn = s.Board.CurrentTurn.ToString(),
            Revision = s.Revision,
            IsCheck = check,
            HasNoLegalMoves = noMoves,
            IsCheckmate = check && noMoves,
            EndReason = s.EndReason,
            GameMode = s.GameMode,
            IsRated = false,
            RemainingSeconds = 0,
            InitialFen = s.InitialFen,
            CreatedAt = s.Created,
            LastMoveAt = s.Moves.LastOrDefault()?.MovedAt,
            Evaluation = new()
            {
                H1 = red.H1 - black.H1,
                H2 = red.H2 - black.H2,
                H3 = red.H3 - black.H3,
                H4 = red.H4 - black.H4
            },
            RedScore = red.Total - black.Total,
            BlackScore = black.Total - red.Total,
            BoardPieces = pieces,
            Moves = s.Moves.Select(m => new MoveHistoryDto
            {
                Id = m.Id,
                MoveOrder = m.MoveOrder,
                FromX = m.FromX,
                FromY = m.FromY,
                ToX = m.ToX,
                ToY = m.ToY,
                PieceType = m.PieceType,
                PieceColor = m.PieceColor,
                MovedAt = m.MovedAt
            }).ToList()
        };
    }

    private void TryLoadFromDisk()
    {
        if (_loadedFromDisk) return;
        _loadedFromDisk = true;
        try
        {
            string path = GetStoragePath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var dtos = JsonSerializer.Deserialize<List<SavedSessionDiskDto>>(json);
                if (dtos != null)
                {
                    lock (_sync)
                    {
                        foreach (var d in dtos)
                        {
                            var s = new Session
                            {
                                Id = d.Id,
                                Status = Enum.TryParse<GameStatus>(d.Status, out var st) ? st : GameStatus.InProgress,
                                EndReason = d.EndReason ?? "",
                                Revision = d.Revision,
                                Created = d.Created,
                                GameMode = d.GameMode ?? "PvE",
                                Title = d.Title ?? "Ván cờ",
                                InitialFen = d.InitialFen
                            };
                            if (!string.IsNullOrWhiteSpace(d.Fen) && Board.TryParseFen(d.Fen, out var b, out _))
                            {
                                s.Board = b!;
                            }
                            if (d.Moves != null) s.Moves.AddRange(d.Moves);
                            _sessions[s.Id] = s;
                            if (s.Id <= _nextId) _nextId = s.Id - 1;
                        }
                    }
                }
            }
        }
        catch { }
    }

    private void SaveToDisk()
    {
        try
        {
            string path = GetStoragePath();
            List<SavedSessionDiskDto> toSave;
            lock (_sync)
            {
                toSave = _sessions.Values.Select(s => new SavedSessionDiskDto
                {
                    Id = s.Id,
                    Fen = s.Board.ToFen(),
                    Status = s.Status.ToString(),
                    EndReason = s.EndReason,
                    Revision = s.Revision,
                    Created = s.Created,
                    GameMode = s.GameMode,
                    Title = s.Title,
                    InitialFen = s.InitialFen,
                    Moves = s.Moves
                }).ToList();
            }
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonSerializer.Serialize(toSave));
        }
        catch { }
    }

    private static string GetStoragePath()
    {
        try
        {
#if ANDROID || IOS || MACCATALYST || WINDOWS
            return Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "offline_saved_sessions.json");
#else
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(string.IsNullOrEmpty(appData) ? AppContext.BaseDirectory : appData, "offline_saved_sessions.json");
#endif
        }
        catch
        {
            return Path.Combine(AppContext.BaseDirectory, "offline_saved_sessions.json");
        }
    }

    private sealed class SavedSessionDiskDto
    {
        public int Id { get; set; }
        public string? Fen { get; set; }
        public string? Status { get; set; }
        public string? EndReason { get; set; }
        public long Revision { get; set; }
        public DateTime Created { get; set; }
        public string? GameMode { get; set; }
        public string? Title { get; set; }
        public string? InitialFen { get; set; }
        public List<MoveHistoryDto>? Moves { get; set; }
    }
}
