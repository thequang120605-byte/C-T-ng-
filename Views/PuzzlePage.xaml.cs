using ChessCore.Mobile.Controls;
using ChessCore.Mobile.ViewModels;

namespace ChessCore.Mobile.Views;

public partial class PuzzlePage : ContentPage
{
    private readonly PuzzleViewModel _vm;
    private readonly ChessBoardDrawable _drawable = new();

    public PuzzlePage(PuzzleViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        ChessBoardView.Drawable = _drawable;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.BoardStateChanged -= OnBoardStateChanged;
        _vm.BoardStateChanged += OnBoardStateChanged;
        OnBoardStateChanged();
    }

    protected override void OnDisappearing()
    {
        _vm.BoardStateChanged -= OnBoardStateChanged;
        base.OnDisappearing();
    }

    private void OnBoardStateChanged() => MainThread.BeginInvokeOnMainThread(() =>
    {
        _drawable.Pieces = _vm.DisplayPieces;
        _drawable.SelectedRow = _vm.SelectedRow;
        _drawable.SelectedCol = _vm.SelectedCol;
        _drawable.ValidMoves = _vm.ValidMoves;
        _drawable.LastMoveFrom = _vm.LastMoveFrom;
        _drawable.LastMoveTo = _vm.LastMoveTo;
        _drawable.IsFlipped = false;
        _drawable.MoveProgress = 1;
        ChessBoardView.Invalidate();
    });

    private void OnBoardStartInteraction(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        var point = new Point(e.Touches[0].X, e.Touches[0].Y);
        var cell = _drawable.TouchToBoardPos(point);
        if (cell.HasValue)
        {
            _vm.CellTappedCommand.Execute((cell.Value.Row, cell.Value.Col));
        }
    }
}
