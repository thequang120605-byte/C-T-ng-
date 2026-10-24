import '../models/engine_move.dart';
import '../models/piece.dart';
import '../models/piece_color.dart';
import '../models/piece_type.dart';
import '../models/position.dart';

class Board {
  static const String startFen =
      'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1';

  final List<List<Piece?>> grid;
  PieceColor currentTurn;

  Board({bool empty = false})
      : grid = List.generate(10, (_) => List<Piece?>.filled(9, null)),
        currentTurn = PieceColor.red {
    if (!empty) {
      initializeBoard();
    }
  }

  void initializeBoard() {
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        grid[r][c] = null;
      }
    }
    currentTurn = PieceColor.red;

    // Quân Đen
    grid[0][0] = const Piece(PieceType.rook, PieceColor.black);
    grid[0][1] = const Piece(PieceType.horse, PieceColor.black);
    grid[0][2] = const Piece(PieceType.elephant, PieceColor.black);
    grid[0][3] = const Piece(PieceType.advisor, PieceColor.black);
    grid[0][4] = const Piece(PieceType.king, PieceColor.black);
    grid[0][5] = const Piece(PieceType.advisor, PieceColor.black);
    grid[0][6] = const Piece(PieceType.elephant, PieceColor.black);
    grid[0][7] = const Piece(PieceType.horse, PieceColor.black);
    grid[0][8] = const Piece(PieceType.rook, PieceColor.black);
    grid[2][1] = const Piece(PieceType.cannon, PieceColor.black);
    grid[2][7] = const Piece(PieceType.cannon, PieceColor.black);
    for (int c = 0; c <= 8; c += 2) {
      grid[3][c] = const Piece(PieceType.pawn, PieceColor.black);
    }

    // Quân Đỏ
    grid[9][0] = const Piece(PieceType.rook, PieceColor.red);
    grid[9][1] = const Piece(PieceType.horse, PieceColor.red);
    grid[9][2] = const Piece(PieceType.elephant, PieceColor.red);
    grid[9][3] = const Piece(PieceType.advisor, PieceColor.red);
    grid[9][4] = const Piece(PieceType.king, PieceColor.red);
    grid[9][5] = const Piece(PieceType.advisor, PieceColor.red);
    grid[9][6] = const Piece(PieceType.elephant, PieceColor.red);
    grid[9][7] = const Piece(PieceType.horse, PieceColor.red);
    grid[9][8] = const Piece(PieceType.rook, PieceColor.red);
    grid[7][1] = const Piece(PieceType.cannon, PieceColor.red);
    grid[7][7] = const Piece(PieceType.cannon, PieceColor.red);
    for (int c = 0; c <= 8; c += 2) {
      grid[6][c] = const Piece(PieceType.pawn, PieceColor.red);
    }
  }

  Piece? getPieceAt(int row, int col) {
    if (row < 0 || row > 9 || col < 0 || col > 8) return null;
    return grid[row][col];
  }

  void setPieceAt(int row, int col, Piece? piece) {
    if (row >= 0 && row < 10 && col >= 0 && col < 9) {
      grid[row][col] = piece;
    }
  }

  void setCurrentTurn(PieceColor color) => currentTurn = color;

  void switchTurn() {
    currentTurn = currentTurn == PieceColor.red ? PieceColor.black : PieceColor.red;
  }

  void applyMove(Position from, Position to) {
    grid[to.row][to.col] = grid[from.row][from.col];
    grid[from.row][from.col] = null;
    switchTurn();
  }

  Board clone() {
    final copy = Board(empty: true);
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final p = grid[r][c];
        copy.grid[r][c] = p == null ? null : Piece(p.type, p.color);
      }
    }
    copy.currentTurn = currentTurn;
    return copy;
  }

  String toFen() {
    final sb = StringBuffer();
    for (int r = 0; r < 10; r++) {
      int empty = 0;
      for (int c = 0; c < 9; c++) {
        final p = grid[r][c];
        if (p == null) {
          empty++;
          continue;
        }
        if (empty > 0) {
          sb.write(empty);
          empty = 0;
        }
        sb.write(_pieceToChar(p));
      }
      if (empty > 0) sb.write(empty);
      if (r < 9) sb.write('/');
    }
    sb.write(currentTurn == PieceColor.red ? ' w' : ' b');
    sb.write(' - - 0 1');
    return sb.toString();
  }

  static String _pieceToChar(Piece p) {
    String ch = switch (p.type) {
      PieceType.king => 'k',
      PieceType.advisor => 'a',
      PieceType.elephant => 'b',
      PieceType.horse => 'n',
      PieceType.rook => 'r',
      PieceType.cannon => 'c',
      PieceType.pawn => 'p',
    };
    return p.color == PieceColor.red ? ch.toUpperCase() : ch;
  }

  static Board? tryParseFen(String? fen) {
    if (fen == null || fen.trim().isEmpty) return null;
    final fields = fen.trim().split(RegExp(r'\s+'));
    final rows = fields[0].split('/');
    if (rows.length != 10) return null;

    final board = Board(empty: true);
    for (int r = 0; r < 10; r++) {
      int col = 0;
      for (int i = 0; i < rows[r].length; i++) {
        final ch = rows[r][i];
        if (ch.codeUnitAt(0) >= 49 && ch.codeUnitAt(0) <= 57) {
          col += int.parse(ch);
        } else {
          final piece = _charToPiece(ch);
          if (piece == null || col >= 9) return null;
          board.grid[r][col] = piece;
          col++;
        }
      }
      if (col != 9) return null;
    }

    if (fields.length > 1) {
      board.currentTurn = fields[1].toLowerCase() == 'b' ? PieceColor.black : PieceColor.red;
    }
    return board;
  }

  static Piece? _charToPiece(String ch) {
    final isUpper = ch == ch.toUpperCase();
    final color = isUpper ? PieceColor.red : PieceColor.black;
    final lower = ch.toLowerCase();
    final type = switch (lower) {
      'k' => PieceType.king,
      'a' => PieceType.advisor,
      'b' || 'e' => PieceType.elephant,
      'n' || 'h' => PieceType.horse,
      'r' => PieceType.rook,
      'c' => PieceType.cannon,
      'p' || 's' => PieceType.pawn,
      _ => null,
    };
    return type != null ? Piece(type, color) : null;
  }
}
