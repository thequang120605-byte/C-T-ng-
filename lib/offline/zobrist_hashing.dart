import 'dart:math' as math;
import 'board.dart';
import 'piece.dart';

class ZobristHashing {
  static final List<List<List<int>>> _pieceKeys = _initPieceKeys();
  static final int _blackTurnKey = _initBlackTurnKey();

  static int _nextSafeInt(math.Random rnd) {
    final high = rnd.nextInt(0x1FFFFF);
    final low = rnd.nextInt(0x7FFFFFFF);
    return (high * 0x80000000) ^ low;
  }

  static List<List<List<int>>> _initPieceKeys() {
    final rnd = math.Random(20261014);
    return List.generate(
      10,
      (_) => List.generate(
        9,
        (_) => List.generate(14, (_) => _nextSafeInt(rnd)),
      ),
    );
  }

  static int _initBlackTurnKey() {
    final rnd = math.Random(20261015);
    return _nextSafeInt(rnd);
  }

  static int _getPieceIndex(PieceType type, PieceColor color) {
    final baseIdx = type.index;
    return color == PieceColor.red ? baseIdx : baseIdx + 7;
  }

  static int computeHash(Board board) {
    int hash = 0;
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final piece = board.getPieceAt(r, c);
        if (piece != null) {
          final pIdx = _getPieceIndex(piece.type, piece.color);
          hash ^= _pieceKeys[r][c][pIdx];
        }
      }
    }
    if (board.currentTurn == PieceColor.black) {
      hash ^= _blackTurnKey;
    }
    return hash;
  }
}
