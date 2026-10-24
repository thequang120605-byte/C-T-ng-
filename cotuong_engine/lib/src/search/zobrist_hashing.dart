import '../board/board.dart';
import '../models/piece_color.dart';
import '../models/piece_type.dart';
import 'zobrist_keys.dart';

class ZobristHashing {
  static int _getPieceIndex(PieceType type, PieceColor color) {
    int baseIdx = switch (type) {
      PieceType.king => 0,
      PieceType.advisor => 1,
      PieceType.elephant => 2,
      PieceType.horse => 3,
      PieceType.rook => 4,
      PieceType.cannon => 5,
      PieceType.pawn => 6,
    };
    return color == PieceColor.red ? baseIdx : baseIdx + 7;
  }

  static BigInt computeHash(Board board) {
    var hash = BigInt.zero;
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final piece = board.getPieceAt(r, c);
        if (piece != null) {
          final pIdx = _getPieceIndex(piece.type, piece.color);
          final keyIdx = (r * 9 + c) * 14 + pIdx;
          hash ^= zobristPieceKeys[keyIdx];
        }
      }
    }
    if (board.currentTurn == PieceColor.black) {
      hash ^= zobristBlackTurnKey;
    }
    return hash;
  }
}
