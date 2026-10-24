using System.Windows.Input;
using ChessCore.Mobile.Models;
using ChessCore.Mobile.Offline;

namespace ChessCore.Mobile.ViewModels;

public sealed class PuzzleViewModel : BaseViewModel
{
    private readonly List<PuzzleItem> _puzzles = new()
    {
        new() { Id = 1, Title = "Mã Hậu Pháo", Goal = "Đỏ đi trước: Dùng Pháo mượn ngòi Mã chiếu bí.", Fen = "3k5/4a4/4N4/4C4/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Dùng Pháo tiến lên chiếu thẳng mặt Tướng!" },
        new() { Id = 2, Title = "Thiết Môn Thuyên", Goal = "Đỏ đi trước: Pháo khống chế trung lộ, Xe khóa tử môn.", Fen = "3k5/4a4/9/9/4C4/9/4P4/4R4/9/4K4 w - - 0 1", Hint = "Xe đâm thẳng chiếu đáy hoặc Pháo yểm trợ!" },
        new() { Id = 3, Title = "Đơn Xa Sát Cục", Goal = "Đỏ đi trước: Xe thọc sườn đâm chiếu tướng.", Fen = "3k1a3/4a4/9/R8/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Đưa Xe sang lộ 4 hoặc lộ 6 chiếu sườn." },
        new() { Id = 4, Title = "Trọng Pháo Tuyệt Sát", Goal = "Đỏ đi trước: Hai Pháo đồng trục hợp lực sát cục.", Fen = "3k5/9/4C4/4C4/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Pháo trước chiếu, Pháo sau làm ngòi yểm trợ." },
        new() { Id = 5, Title = "Bạch Mã Hiện Hình", Goal = "Đỏ đi trước: Mã ngọa tào khóa chặt cửu cung.", Fen = "4k4/3Na4/9/9/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Mã nhảy chiếu điểm yếu của cung Tướng." },
        new() { Id = 6, Title = "Song Xa Phá Thành", Goal = "Đỏ đi trước: Hai Xe liên công phá tan phòng tuyến.", Fen = "3k5/4a4/9/R8/1R7/9/4P4/9/9/4K4 w - - 0 1", Hint = "Xe sau yểm trợ Xe trước đâm chiếu dồn dập." },
        new() { Id = 7, Title = "Mã Đội Pháo", Goal = "Đỏ đi trước: Kết hợp Mã và Pháo trung lộ.", Fen = "3k5/4a4/4N4/9/4C4/9/4P4/9/9/4K4 w - - 0 1", Hint = "Tận dụng góc chiếu hiểm hóc của Mã." },
        new() { Id = 8, Title = "Điểm Huyệt Tuyệt Chiêu", Goal = "Đỏ đi trước: Tốt áp sát khống chế Tướng đen.", Fen = "3k5/4P4/9/9/9/9/9/9/9/4K4 w - - 0 1", Hint = "Tốt đã qua sông có sức mạnh áp sát như Xe!" },
        new() { Id = 9, Title = "Hải Để Lao Nguyệt", Goal = "Đỏ đi trước: Mò trăng đáy biển với Xe Pháo.", Fen = "3k5/4a4/9/9/9/9/4P4/9/2R6/3CK4 w - - 0 1", Hint = "Xe điều động sang lộ xung yếu để khóa chân Tướng." },
        new() { Id = 10, Title = "Kim Câu Điếu Ngư", Goal = "Đỏ đi trước: Thế câu cá vàng, Mã cản chân Tướng.", Fen = "3k5/4a4/3N5/9/9/9/4P4/9/4R4/4K4 w - - 0 1", Hint = "Mã kiểm soát ô thoát của Tướng, Xe kết liễu." },
        new() { Id = 11, Title = "Đại Đao Trảm Quan", Goal = "Đỏ đi trước: Xe dũng mãnh đột phá trung tâm.", Fen = "4k4/4a4/4R4/9/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Đâm Xe chiếu trực diện buộc đối phương quy hàng." },
        new() { Id = 12, Title = "Song Mã Khâm Đao", Goal = "Đỏ đi trước: Hai Mã tung hoành ngang dọc.", Fen = "3k5/4a4/3NN4/9/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Nhảy Mã vào vị trí chiếu hiểm hóc." },
        new() { Id = 13, Title = "Tiến Tốt Chiếu Bí", Goal = "Đỏ đi trước: Song Tốt tiến sát bức tử Tướng.", Fen = "3k5/9/3PP4/9/9/9/9/9/9/4K4 w - - 0 1", Hint = "Tiến Tốt vào cung khóa chặt mọi đường thoái." },
        new() { Id = 14, Title = "Pháo Lồng Cửu Cung", Goal = "Đỏ đi trước: Hai Pháo khép góc tiêu diệt.", Fen = "3k5/4a4/9/3C1C3/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Điều chuyển Pháo vào trục chính." },
        new() { Id = 15, Title = "Khởi Mã Hoành Xa", Goal = "Đỏ đi trước: Mã mở đường thần tốc cho Xe.", Fen = "3k5/4a4/5N3/4R4/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Kết hợp Mã chiếu và Xe bọc lót." },
        new() { Id = 16, Title = "Ngọa Long Xuất Trận", Goal = "Đỏ đi trước: Pháo đầu yểm trợ Mã biên.", Fen = "3k5/4a4/9/3N5/4C4/9/4P4/9/9/4K4 w - - 0 1", Hint = "Pháo giữ trung lộ làm điểm tựa vững chắc." },
        new() { Id = 17, Title = "Lưỡng Long Triều Nhật", Goal = "Đỏ đi trước: Song Xe giáp công hai cánh.", Fen = "3k5/9/9/R3R4/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Một Xe ép góc, Xe còn lại đâm đáy." },
        new() { Id = 18, Title = "Bạt Sơn Khí Cái", Goal = "Đỏ đi trước: Sức mạnh công phá của Pháo.", Fen = "3k5/4a4/2C6/3C5/9/9/4P4/9/9/4K4 w - - 0 1", Hint = "Bình Pháo chiếu tướng ép Tướng lệch cung." },
        new() { Id = 19, Title = "Tam Xa Tề Hội", Goal = "Đỏ đi trước: Hai Xe một Tốt uy hiếp thành trì.", Fen = "3k5/4a4/3P5/R3R4/9/9/9/9/9/4K4 w - - 0 1", Hint = "Tận dụng Tốt làm ngòi hoặc cản Tướng." },
        new() { Id = 20, Title = "Cửu Cung Tuyệt Sát", Goal = "Đỏ đi trước: Xe Pháo Mã đồng loạt ra tay.", Fen = "3k5/4a4/3N5/4R4/4C4/9/4P4/9/9/4K4 w - - 0 1", Hint = "Tìm nước chiếu khiến đối phương không thể cản phá!" }
    };

    private int _currentIndex;
    private Board _board = new();
    private List<PieceDto> _boardPieces = new();
    private int? _selectedRow, _selectedCol;
    private List<ValidMoveDto> _validMoves = new();
    private (int Row, int Col)? _lastMoveFrom, _lastMoveTo;
    private bool _isSolved;
    private string _statusMessage = "Đến lượt Đỏ đi";
    private readonly HashSet<int> _solvedSet = new();

    public PuzzleViewModel()
    {
        LoadProgress();
        CellTappedCommand = new Command<(int Row, int Col)>(OnCellTapped);
        ResetCommand = new Command(LoadCurrentPuzzle);
        HintCommand = new Command(ShowHint);
        NextCommand = new Command(GoNext);
        PrevCommand = new Command(GoPrev);
        BackCommand = new AsyncCommand(async () =>
        {
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//home");
        }, _ => { });

        LoadCurrentPuzzle();
    }

    public event Action? BoardStateChanged;

    public ICommand CellTappedCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand HintCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand PrevCommand { get; }
    public ICommand BackCommand { get; }

    public int CurrentIndex => _currentIndex;
    public int PuzzleNumber => _currentIndex + 1;
    public int TotalPuzzles => _puzzles.Count;
    public PuzzleItem CurrentPuzzle => _puzzles[_currentIndex];
    public string ProgressText => $"Đã giải: {_solvedSet.Count} / {_puzzles.Count} bài thế";
    public string PuzzleTitle => $"Bài {PuzzleNumber}: {CurrentPuzzle.Title}";
    public string PuzzleGoal => CurrentPuzzle.Goal;
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public bool IsSolved { get => _isSolved; private set => SetProperty(ref _isSolved, value); }
    public bool CanGoNext => _currentIndex < _puzzles.Count - 1;
    public bool CanGoPrev => _currentIndex > 0;

    public IReadOnlyList<PieceDto> DisplayPieces => _boardPieces;
    public int? SelectedRow => _selectedRow;
    public int? SelectedCol => _selectedCol;
    public List<ValidMoveDto> ValidMoves => _validMoves;
    public (int Row, int Col)? LastMoveFrom => _lastMoveFrom;
    public (int Row, int Col)? LastMoveTo => _lastMoveTo;

    public void LoadCurrentPuzzle()
    {
        _selectedRow = _selectedCol = null;
        _validMoves = new();
        _lastMoveFrom = _lastMoveTo = null;
        _isSolved = false;
        StatusMessage = "Đến lượt Đỏ đi";

        if (Board.TryParseFen(CurrentPuzzle.Fen, out var b, out _))
        {
            _board = b!;
            UpdateBoardPieces();
        }

        NotifyAll();
        BoardStateChanged?.Invoke();
    }

    private void ShowHint()
    {
        StatusMessage = $"💡 Gợi ý: {CurrentPuzzle.Hint}";
    }

    private void GoNext()
    {
        if (CanGoNext)
        {
            _currentIndex++;
            LoadCurrentPuzzle();
        }
    }

    private void GoPrev()
    {
        if (CanGoPrev)
        {
            _currentIndex--;
            LoadCurrentPuzzle();
        }
    }

    private void OnCellTapped((int Row, int Col) pos)
    {
        int row = pos.Row;
        int col = pos.Col;

        if (_isSolved) return;

        // Deselect
        if (_selectedRow == row && _selectedCol == col)
        {
            ClearSelection();
            return;
        }

        // Move to valid square
        if (_selectedRow.HasValue && _selectedCol.HasValue && _validMoves.Any(m => m.Row == row && m.Col == col))
        {
            MakeMove(_selectedRow.Value, _selectedCol.Value, row, col);
            return;
        }

        // Select own piece (Red)
        var piece = _board.GetPieceAt(row, col);
        if (piece != null && piece.Color == PieceColor.Red)
        {
            _selectedRow = row;
            _selectedCol = col;
            var engineMoves = MoveGenerator.GetValidMoves(_board, new Position(row, col));
            _validMoves = engineMoves.Select(m => new ValidMoveDto { Row = m.To.Row, Col = m.To.Col }).ToList();
            NotifySelection();
            BoardStateChanged?.Invoke();
        }
        else
        {
            ClearSelection();
        }
    }

    private void MakeMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        var piece = _board.GetPieceAt(fromRow, fromCol);
        if (piece == null) return;

        _board.ApplyMove(new Position(fromRow, fromCol), new Position(toRow, toCol));
        _board.SwitchTurn();

        _lastMoveFrom = (fromRow, fromCol);
        _lastMoveTo = (toRow, toCol);
        ClearSelection();
        UpdateBoardPieces();

        // Check if Black has any legal moves left and is under check (Checkmate)
        bool blackCheck = MoveGenerator.CheckIfInCheck(_board, PieceColor.Black);
        bool blackHasMoves = MoveGenerator.HasAnyLegalMoves(_board, PieceColor.Black);

        if (blackCheck && !blackHasMoves)
        {
            IsSolved = true;
            _solvedSet.Add(CurrentPuzzle.Id);
            SaveProgress();
            StatusMessage = "🎉 XUẤT SẮC! CHIẾU BÍ THÀNH CÔNG!";
        }
        else if (blackCheck)
        {
            StatusMessage = "Chiếu tướng! Nước đi rất tốt!";
        }
        else
        {
            StatusMessage = "Đã đi nước cờ. Hãy tìm nước chiếu dứt điểm!";
        }

        NotifyAll();
        BoardStateChanged?.Invoke();
    }

    private void ClearSelection()
    {
        _selectedRow = _selectedCol = null;
        _validMoves = new();
        NotifySelection();
        BoardStateChanged?.Invoke();
    }

    private void UpdateBoardPieces()
    {
        var list = new List<PieceDto>();
        for (int r = 0; r < 10; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                var p = _board.GetPieceAt(r, c);
                if (p != null)
                {
                    list.Add(new PieceDto
                    {
                        Row = r,
                        Col = c,
                        Type = p.Type.ToString(),
                        Color = p.Color.ToString()
                    });
                }
            }
        }
        _boardPieces = list;
    }

    private void NotifySelection()
    {
        OnPropertyChanged(nameof(SelectedRow));
        OnPropertyChanged(nameof(SelectedCol));
        OnPropertyChanged(nameof(ValidMoves));
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(CurrentIndex));
        OnPropertyChanged(nameof(PuzzleNumber));
        OnPropertyChanged(nameof(PuzzleTitle));
        OnPropertyChanged(nameof(PuzzleGoal));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(DisplayPieces));
        OnPropertyChanged(nameof(LastMoveFrom));
        OnPropertyChanged(nameof(LastMoveTo));
        OnPropertyChanged(nameof(IsSolved));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrev));
        NotifySelection();
    }

    private void LoadProgress()
    {
        try
        {
            var saved = Preferences.Default.Get("solved_puzzles", "");
            if (!string.IsNullOrEmpty(saved))
            {
                foreach (var idStr in saved.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(idStr, out int id))
                        _solvedSet.Add(id);
                }
            }
        }
        catch { }
    }

    private void SaveProgress()
    {
        try
        {
            Preferences.Default.Set("solved_puzzles", string.Join(",", _solvedSet));
        }
        catch { }
    }
}
