namespace ChessCore.Mobile.Offline;
public static class GameRules
{
    public static PieceColor Opponent(PieceColor color) => color == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
    public static List<EngineMove> GetAllLegalMoves(Board board, PieceColor color)
    {
        var moves = new List<EngineMove>();
        for (int row = 0; row < 10; row++) for (int col = 0; col < 9; col++)
            if (board.GetPieceAt(row, col) is { } piece && piece.Color == color)
                moves.AddRange(MoveGenerator.GetValidMoves(board, new Position(row, col)));
        return moves;
    }
    public static bool HasLegalMove(Board board, PieceColor color)
    {
        for (int row = 0; row < 10; row++) for (int col = 0; col < 9; col++)
            if (board.GetPieceAt(row, col) is { } piece && piece.Color == color && MoveGenerator.GetValidMoves(board, new Position(row, col)).Count > 0) return true;
        return false;
    }
    public static GameStatus StatusAfterMove(Board board) => HasLegalMove(board, board.CurrentTurn) ? GameStatus.InProgress :
        board.CurrentTurn == PieceColor.Red ? GameStatus.BlackWins : GameStatus.RedWins;
}
