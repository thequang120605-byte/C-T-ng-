import 'board.dart';
import 'move_generator.dart';
import 'piece.dart';

class GameRules {
  static PieceColor opponent(PieceColor color) =>
      color == PieceColor.red ? PieceColor.black : PieceColor.red;

  static List<EngineMove> getAllLegalMoves(Board board, PieceColor color) {
    final moves = <EngineMove>[];
    for (int row = 0; row < 10; row++) {
      for (int col = 0; col < 9; col++) {
        final piece = board.getPieceAt(row, col);
        if (piece != null && piece.color == color) {
          moves.addAll(MoveGenerator.getValidMoves(board, Position(row, col)));
        }
      }
    }
    return moves;
  }

  static bool hasLegalMove(Board board, PieceColor color) {
    for (int row = 0; row < 10; row++) {
      for (int col = 0; col < 9; col++) {
        final piece = board.getPieceAt(row, col);
        if (piece != null &&
            piece.color == color &&
            MoveGenerator.getValidMoves(board, Position(row, col)).isNotEmpty) {
          return true;
        }
      }
    }
    return false;
  }

  static GameStatus statusAfterMove(Board board) =>
      hasLegalMove(board, board.currentTurn)
          ? GameStatus.inProgress
          : board.currentTurn == PieceColor.red
              ? GameStatus.blackWins
              : GameStatus.redWins;
}
