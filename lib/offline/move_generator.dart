import 'board.dart';
import 'game_rules.dart';
import 'piece.dart';

class MoveGenerator {
  static List<EngineMove> getAllValidMoves(Board board, PieceColor color) {
    final moves = <EngineMove>[];
    for (int row = 0; row < 10; row++) {
      for (int col = 0; col < 9; col++) {
        final piece = board.getPieceAt(row, col);
        if (piece == null || piece.color != color) continue;
        moves.addAll(getValidMoves(board, Position(row, col)));
      }
    }
    return moves;
  }

  static bool hasAnyLegalMoves(Board board, PieceColor color) {
    return GameRules.hasLegalMove(board, color);
  }

  static List<EngineMove> getValidMoves(Board board, Position from) {
    final pseudoMoves = getPseudoLegalMoves(board, from);
    final legalMoves = <EngineMove>[];
    final currentPiece = board.getPieceAt(from.row, from.col);
    if (currentPiece == null) return legalMoves;

    for (final move in pseudoMoves) {
      final captured = board.getPieceAt(move.to.row, move.to.col);
      if (captured != null && captured.type == PieceType.king) continue;

      board.grid[move.to.row][move.to.col] = currentPiece;
      board.grid[move.from.row][move.from.col] = null;

      final isSelfInCheck = checkIfInCheck(board, currentPiece.color);

      board.grid[move.from.row][move.from.col] = currentPiece;
      board.grid[move.to.row][move.to.col] = captured;

      if (!isSelfInCheck) {
        legalMoves.add(move);
      }
    }
    return legalMoves;
  }

  static bool checkIfInCheck(Board board, PieceColor color) {
    final enemy = color == PieceColor.red ? PieceColor.black : PieceColor.red;
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final p = board.getPieceAt(r, c);
        if (p?.type == PieceType.king && p?.color == color) {
          return isSquareAttacked(board, r, c, enemy);
        }
      }
    }
    return true;
  }

  static bool isSquareAttacked(
      Board board, int row, int col, PieceColor color) {
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final p = board.getPieceAt(r, c);
        if (p == null || p.color != color || (r == row && c == col)) continue;
        final dr = row - r;
        final dc = col - c;
        final ar = dr.abs();
        final ac = dc.abs();

        switch (p.type) {
          case PieceType.horse:
            if (ar == 2 &&
                ac == 1 &&
                board.getPieceAt(r + dr.sign, c) == null) {
              return true;
            }
            if (ar == 1 &&
                ac == 2 &&
                board.getPieceAt(r, c + dc.sign) == null) {
              return true;
            }
            break;
          case PieceType.elephant:
            if (ar == 2 &&
                ac == 2 &&
                (color == PieceColor.red ? row >= 5 : row <= 4) &&
                board.getPieceAt(r + dr ~/ 2, c + dc ~/ 2) == null) {
              return true;
            }
            break;
          case PieceType.advisor:
            if (ar == 1 && ac == 1 && _isInPalace(row, col, color)) {
              return true;
            }
            break;
          case PieceType.pawn:
            if (dc == 0 && dr == (color == PieceColor.red ? -1 : 1)) {
              return true;
            }
            if (dr == 0 &&
                ac == 1 &&
                (color == PieceColor.red ? r <= 4 : r >= 5)) {
              return true;
            }
            break;
          case PieceType.king:
            if (ar + ac == 1 && _isInPalace(row, col, color)) {
              return true;
            }
            if (dc != 0 || board.getPieceAt(row, col)?.type != PieceType.king) {
              break;
            }
            int screens = 0;
            final sr = dr.sign;
            final sc = dc.sign;
            for (int rr = r + sr, cc = c + sc;
                rr != row || cc != col;
                rr += sr, cc += sc) {
              if (board.getPieceAt(rr, cc) != null) screens++;
            }
            if (screens == 0) return true;
            break;
          case PieceType.rook:
          case PieceType.cannon:
            if (dr != 0 && dc != 0) break;
            int screens = 0;
            final sr = dr.sign;
            final sc = dc.sign;
            for (int rr = r + sr, cc = c + sc;
                rr != row || cc != col;
                rr += sr, cc += sc) {
              if (board.getPieceAt(rr, cc) != null) screens++;
            }
            if (screens == (p.type == PieceType.cannon ? 1 : 0)) return true;
            break;
        }
      }
    }
    return false;
  }

  static List<EngineMove> getPseudoLegalMoves(Board board, Position from) {
    final moves = <EngineMove>[];
    final piece = board.getPieceAt(from.row, from.col);
    if (piece == null) return moves;

    switch (piece.type) {
      case PieceType.rook:
        moves.addAll(_getRookMoves(board, from, piece.color));
        break;
      case PieceType.horse:
        moves.addAll(_getHorseMoves(board, from, piece.color));
        break;
      case PieceType.elephant:
        moves.addAll(_getElephantMoves(board, from, piece.color));
        break;
      case PieceType.advisor:
        moves.addAll(_getAdvisorMoves(board, from, piece.color));
        break;
      case PieceType.king:
        moves.addAll(_getKingMoves(board, from, piece.color));
        break;
      case PieceType.cannon:
        moves.addAll(_getCannonMoves(board, from, piece.color));
        break;
      case PieceType.pawn:
        moves.addAll(_getPawnMoves(board, from, piece.color));
        break;
    }
    return moves;
  }

  static List<EngineMove> _getRookMoves(
      Board board, Position from, PieceColor color) {
    final moves = <EngineMove>[];
    const dR = [-1, 1, 0, 0];
    const dC = [0, 0, -1, 1];
    for (int i = 0; i < 4; i++) {
      int r = from.row + dR[i];
      int c = from.col + dC[i];
      while (_isInsideBoard(r, c)) {
        final target = board.getPieceAt(r, c);
        if (target == null) {
          moves.add(EngineMove(from, Position(r, c)));
        } else {
          if (target.color != color) {
            moves.add(EngineMove(from, Position(r, c), target));
          }
          break;
        }
        r += dR[i];
        c += dC[i];
      }
    }
    return moves;
  }

  static List<EngineMove> _getHorseMoves(
      Board board, Position from, PieceColor color) {
    final moves = <EngineMove>[];
    const blockRow = [-1, 1, 0, 0];
    const blockCol = [0, 0, -1, 1];
    const targetRow = [
      [-2, -2],
      [2, 2],
      [-1, 1],
      [-1, 1]
    ];
    const targetCol = [
      [-1, 1],
      [-1, 1],
      [-2, -2],
      [2, 2]
    ];

    for (int i = 0; i < 4; i++) {
      final blockR = from.row + blockRow[i];
      final blockC = from.col + blockCol[i];
      if (!_isInsideBoard(blockR, blockC)) continue;
      if (board.getPieceAt(blockR, blockC) != null) continue;
      for (int j = 0; j < 2; j++) {
        final r = from.row + targetRow[i][j];
        final c = from.col + targetCol[i][j];
        _addMoveIfValid(board, moves, from, r, c, color);
      }
    }
    return moves;
  }

  static List<EngineMove> _getElephantMoves(
      Board board, Position from, PieceColor color) {
    final moves = <EngineMove>[];
    const dR = [-2, -2, 2, 2];
    const dC = [-2, 2, -2, 2];
    const eyeR = [-1, -1, 1, 1];
    const eyeC = [-1, 1, -1, 1];

    for (int i = 0; i < 4; i++) {
      final r = from.row + dR[i];
      final c = from.col + dC[i];
      final er = from.row + eyeR[i];
      final ec = from.col + eyeC[i];
      if (!_isInsideBoard(r, c) || !_isInsideBoard(er, ec)) continue;
      final crossRiver = color == PieceColor.red ? r < 5 : r > 4;
      if (crossRiver) continue;
      if (board.getPieceAt(er, ec) != null) continue;
      _addMoveIfValid(board, moves, from, r, c, color);
    }
    return moves;
  }

  static List<EngineMove> _getAdvisorMoves(
      Board board, Position from, PieceColor color) {
    final moves = <EngineMove>[];
    const dR = [-1, -1, 1, 1];
    const dC = [-1, 1, -1, 1];
    for (int i = 0; i < 4; i++) {
      final r = from.row + dR[i];
      final c = from.col + dC[i];
      if (!_isInPalace(r, c, color)) continue;
      _addMoveIfValid(board, moves, from, r, c, color);
    }
    return moves;
  }

  static List<EngineMove> _getKingMoves(
      Board board, Position from, PieceColor color) {
    final moves = <EngineMove>[];
    const dR = [-1, 1, 0, 0];
    const dC = [0, 0, -1, 1];
    for (int i = 0; i < 4; i++) {
      final r = from.row + dR[i];
      final c = from.col + dC[i];
      if (!_isInPalace(r, c, color)) continue;
      _addMoveIfValid(board, moves, from, r, c, color);
    }
    return moves;
  }

  static List<EngineMove> _getCannonMoves(
      Board board, Position from, PieceColor color) {
    final moves = <EngineMove>[];
    const dR = [-1, 1, 0, 0];
    const dC = [0, 0, -1, 1];
    for (int i = 0; i < 4; i++) {
      bool foundMount = false;
      int r = from.row + dR[i];
      int c = from.col + dC[i];
      while (_isInsideBoard(r, c)) {
        final target = board.getPieceAt(r, c);
        if (!foundMount) {
          if (target == null) {
            moves.add(EngineMove(from, Position(r, c)));
          } else {
            foundMount = true;
          }
        } else {
          if (target != null) {
            if (target.color != color) {
              moves.add(EngineMove(from, Position(r, c), target));
            }
            break;
          }
        }
        r += dR[i];
        c += dC[i];
      }
    }
    return moves;
  }

  static List<EngineMove> _getPawnMoves(
      Board board, Position from, PieceColor color) {
    final moves = <EngineMove>[];
    final forward = color == PieceColor.red ? -1 : 1;
    final crossedRiver = color == PieceColor.red ? from.row <= 4 : from.row >= 5;

    _addMoveIfValid(board, moves, from, from.row + forward, from.col, color);
    if (crossedRiver) {
      _addMoveIfValid(board, moves, from, from.row, from.col - 1, color);
      _addMoveIfValid(board, moves, from, from.row, from.col + 1, color);
    }
    return moves;
  }

  static bool _isInsideBoard(int row, int col) =>
      row >= 0 && row <= 9 && col >= 0 && col <= 8;

  static bool _isInPalace(int row, int col, PieceColor color) {
    if (col < 3 || col > 5) return false;
    if (color == PieceColor.red) return row >= 7 && row <= 9;
    return row >= 0 && row <= 2;
  }

  static void _addMoveIfValid(Board board, List<EngineMove> moves,
      Position from, int row, int col, PieceColor color) {
    if (!_isInsideBoard(row, col)) return;
    final target = board.getPieceAt(row, col);
    if (target == null) {
      moves.add(EngineMove(from, Position(row, col)));
    } else if (target.color != color) {
      moves.add(EngineMove(from, Position(row, col), target));
    }
  }
}
