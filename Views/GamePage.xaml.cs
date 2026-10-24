using ChessCore.Mobile.Controls;
using ChessCore.Mobile.ViewModels;

namespace ChessCore.Mobile.Views;

public partial class GamePage : ContentPage
{
    private readonly GameViewModel _vm;
    private readonly ChessBoardDrawable _drawable = new();
    private Task _cleanup = Task.CompletedTask;
    private bool _visible, _wasFlipped;
    private string _lastMoveKey = "";
    private (float X, float Y)? _touchStart;
    private (int Row, int Col)? _touchStartCell;
    private bool _dragged;

    public GamePage(GameViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        ChessBoardView.Drawable = _drawable;
        SizeChanged += (_, _) => UpdateLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _visible = true;
        _vm.BoardStateChanged -= OnBoardStateChanged;
        _vm.BoardStateChanged += OnBoardStateChanged;
        if (Window != null) { Window.Stopped += OnWindowStopped; Window.Resumed += OnWindowResumed; }
        try { await _cleanup; if (_visible) await _vm.ActivateAsync(); }
        catch (Exception ex) { _vm.ErrorMessage = ex.Message; }
        UpdateLayout();
        OnBoardStateChanged();
    }

    protected override void OnDisappearing()
    {
        _visible = false;
        this.AbortAnimation("MovePiece");
        _vm.BoardStateChanged -= OnBoardStateChanged;
        if (Window != null) { Window.Stopped -= OnWindowStopped; Window.Resumed -= OnWindowResumed; }
        _cleanup = _vm.DeactivateAsync();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _vm.ConfirmExitCommand.Execute(null);
        return true;
    }

    private void OnWindowStopped(object? sender, EventArgs e) => _cleanup = _vm.DeactivateAsync();

    private async void OnWindowResumed(object? sender, EventArgs e)
    {
        try { await _cleanup; if (_visible) await _vm.ActivateAsync(); }
        catch (Exception ex) { _vm.ErrorMessage = ex.Message; }
    }

    private void UpdateLayout()
    {
        bool wide = Width >= 780 || (Width > Height && Width >= 640);
        GameLayout.ColumnDefinitions = wide
            ? new ColumnDefinitionCollection { new() { Width = GridLength.Star }, new() { Width = new GridLength(280) } }
            : new ColumnDefinitionCollection { new() { Width = GridLength.Star } };
        GameLayout.RowDefinitions = wide
            ? new RowDefinitionCollection { new() { Height = GridLength.Star } }
            : new RowDefinitionCollection { new() { Height = GridLength.Star }, new() { Height = GridLength.Auto } };
        Grid.SetRow(DetailPanel, wide ? 0 : 1);
        Grid.SetColumn(DetailPanel, wide ? 1 : 0);
        WideDetails.IsVisible = wide;
        OpponentCard.IsVisible = PlayerCard.IsVisible = !wide;
    }

    private void OnBoardStateChanged() => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (!_visible) return;
        var key = $"{_vm.GameId}:{_vm.Game?.Moves?.Count}:{_vm.ReplayIndex}:{_vm.LastMoveFrom}:{_vm.LastMoveTo}";
        bool animate = !_vm.IsReplayMode && _lastMoveKey.Length > 0 && key != _lastMoveKey && _vm.LastMoveFrom.HasValue && _vm.LastMoveTo.HasValue && _wasFlipped == _vm.IsFlipped && !Preferences.Default.Get("reduce_motion", false);
        // Don't animate moves across unrelated game IDs.
        animate &= _lastMoveKey.StartsWith($"{_vm.GameId}:", StringComparison.Ordinal);
        _lastMoveKey = key;
        _wasFlipped = _vm.IsFlipped;
        this.AbortAnimation("MovePiece");
        _drawable.Pieces = _vm.DisplayPieces;
        _drawable.SelectedRow = _vm.SelectedRow;
        _drawable.SelectedCol = _vm.SelectedCol;
        _drawable.ValidMoves = _vm.ValidMoves;
        _drawable.LastMoveFrom = _vm.LastMoveFrom;
        _drawable.LastMoveTo = _vm.LastMoveTo;
        _drawable.IsFlipped = _vm.IsFlipped;
        _drawable.MoveProgress = animate ? 0 : 1;
        ChessBoardView.Invalidate();
        if (animate)
        {
            TryHapticFeedback();
            new Animation(v => { _drawable.MoveProgress = (float)v; ChessBoardView.Invalidate(); }, 0, 1)
                .Commit(this, "MovePiece", 16, 160, Easing.CubicOut);
        }
    });

    private void OnBoardStartInteraction(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        _touchStart = (e.Touches[0].X, e.Touches[0].Y);
        _dragged = false;
        _touchStartCell = _drawable.TouchToBoardPos(new Point(e.Touches[0].X, e.Touches[0].Y));
    }

    private void OnBoardDragInteraction(object? sender, TouchEventArgs e)
    {
        if (_touchStart is not { } start || e.Touches.Length == 0) return;
        var dx = e.Touches[0].X - start.X;
        var dy = e.Touches[0].Y - start.Y;
        if (dx * dx + dy * dy > 196) _dragged = true;
    }

    private void OnBoardEndInteraction(object? sender, TouchEventArgs e)
    {
        if (_touchStart is not { } start) return;
        var point = e.Touches.Length > 0 ? new Point(e.Touches[0].X, e.Touches[0].Y) : new Point(start.X, start.Y);
        var endCell = _drawable.TouchToBoardPos(point);
        var startCell = _touchStartCell;

        _touchStart = null;
        _touchStartCell = null;

        if (_dragged)
        {
            // Hỗ trợ Drag-and-Drop: Chạm quân và kéo thả sang ô hợp lệ
            if (startCell.HasValue && endCell.HasValue && startCell.Value != endCell.Value)
            {
                _vm.CellTappedCommand.Execute((startCell.Value.Row, startCell.Value.Col));
                _vm.CellTappedCommand.Execute((endCell.Value.Row, endCell.Value.Col));
                TryHapticFeedback();
                return;
            }
        }

        // Tap bình thường
        if (endCell.HasValue)
        {
            _vm.CellTappedCommand.Execute((endCell.Value.Row, endCell.Value.Col));
            TryHapticFeedback();
        }
    }

    private void OnBoardCancelInteraction(object? sender, EventArgs e)
    {
        _touchStart = null;
        _touchStartCell = null;
    }

    private static void TryHapticFeedback()
    {
        try
        {
            if (!Preferences.Default.Get("reduce_motion", false))
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Bỏ qua trên các nền tảng hoặc simulator không có motor rung
        }
    }
}
