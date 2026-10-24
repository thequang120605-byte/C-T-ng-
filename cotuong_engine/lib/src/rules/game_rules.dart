import '../board/board.dart';
import '../models/engine_move.dart';
import '../models/game_status.dart';
import '../models/piece_color.dart';
import '../models/position.dart';
import 'move_generator.dart';

class GameRules {
  static PieceColor opponent(PieceColor color) =>
      color == PieceColor.red ? PieceColor.black : PieceColor.red;

  static List<EngineMove> getAllLegalMoves(Board board, PieceColor color) {
    final moves = <EngineMove>[];
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final piece = board.getPieceAt(r, c);
        if (piece != null && piece.color == color) {
          moves.addAll(MoveGenerator.getValidMoves(board, Position(r, c)));
        }
      }
    }
    return moves;
  }

  static bool hasLegalMove(Board board, PieceColor color) {
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final piece = board.getPieceAt(r, c);
        if (piece != null && piece.color == color) {
          if (MoveGenerator.getValidMoves(board, Position(r, c)).isNotEmpty) {
            return true;
          }
        }
      }
    }
    return false;
  }

  static GameStatus statusAfterMove(Board board) {
    if (hasLegalMove(board, board.currentTurn)) {
      return GameStatus.inProgress;
    }
    return board.currentTurn == PieceColor.red
        ? GameStatus.blackWins
        : GameStatus.redWins;
  }
}
