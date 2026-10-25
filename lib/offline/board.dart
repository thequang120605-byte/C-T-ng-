import 'piece.dart';
import 'move_generator.dart';

class Board {
  late List<List<Piece?>> grid;
  PieceColor currentTurn = PieceColor.red;

  static const String startFen =
      'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1';

  Board() {
    grid = List.generate(10, (_) => List<Piece?>.filled(9, null));
    currentTurn = PieceColor.red;
    initializeBoard();
  }

  Board.empty() {
    grid = List.generate(10, (_) => List<Piece?>.filled(9, null));
    currentTurn = PieceColor.red;
  }

  void initializeBoard() {
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        grid[r][c] = null;
      }
    }
    currentTurn = PieceColor.red;

    // QUÂN ĐEN
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

    // QUÂN ĐỎ
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

  void setCurrentTurn(PieceColor color) {
    currentTurn = color;
  }

  void switchTurn() {
    currentTurn =
        currentTurn == PieceColor.red ? PieceColor.black : PieceColor.red;
  }

  bool makeMove(EngineMove move) {
    final p = getPieceAt(move.from.row, move.from.col);
    if (p == null || p.color != currentTurn) return false;

    final validMoves = MoveGenerator.getValidMoves(this, move.from);
    final isValid = validMoves.any((m) => m.to == move.to);
    if (!isValid) return false;

    grid[move.to.row][move.to.col] = p;
    grid[move.from.row][move.from.col] = null;
    switchTurn();
    return true;
  }

  void applyMove(Position from, Position to) {
    grid[to.row][to.col] = grid[from.row][from.col];
    grid[from.row][from.col] = null;
    switchTurn();
  }

  Board clone() {
    final copy = Board.empty();
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
    final String ch;
    switch (p.type) {
      case PieceType.king:
        ch = 'k';
        break;
      case PieceType.advisor:
        ch = 'a';
        break;
      case PieceType.elephant:
        ch = 'b';
        break;
      case PieceType.horse:
        ch = 'n';
        break;
      case PieceType.rook:
        ch = 'r';
        break;
      case PieceType.cannon:
        ch = 'c';
        break;
      case PieceType.pawn:
        ch = 'p';
        break;
    }
    return p.color == PieceColor.red ? ch.toUpperCase() : ch;
  }

  static Piece? _tryCharToPiece(String ch) {
    final isUpper = ch == ch.toUpperCase() && ch != ch.toLowerCase();
    final color = isUpper ? PieceColor.red : PieceColor.black;
    final PieceType type;
    switch (ch.toLowerCase()) {
      case 'k':
        type = PieceType.king;
        break;
      case 'a':
        type = PieceType.advisor;
        break;
      case 'b':
      case 'e':
        type = PieceType.elephant;
        break;
      case 'n':
      case 'h':
        type = PieceType.horse;
        break;
      case 'r':
        type = PieceType.rook;
        break;
      case 'c':
        type = PieceType.cannon;
        break;
      case 'p':
      case 's':
        type = PieceType.pawn;
        break;
      default:
        return null;
    }
    return Piece(type, color);
  }

  static (Board?, String) tryParseFen(String? fen) {
    if (fen == null || fen.trim().isEmpty) {
      return (null, 'Chuỗi FEN đang trống.');
    }
    if (fen.length > 200) {
      return (null, 'Chuỗi FEN quá dài.');
    }
    final fields =
        fen.trim().split(RegExp(r'\s+')).where((s) => s.isNotEmpty).toList();
    final rows = fields[0].split('/');
    if (rows.length != 10) {
      return (null, "FEN phải có đúng 10 hàng, ngăn cách bằng dấu '/'.");
    }

    final result = Board.empty();
    for (int r = 0; r < 10; r++) {
      int col = 0;
      for (int i = 0; i < rows[r].length; i++) {
        final ch = rows[r][i];
        final code = ch.codeUnitAt(0);
        if (code >= 49 && code <= 57) {
          col += code - 48;
        } else {
          final piece = _tryCharToPiece(ch);
          if (piece != null) {
            if (col > 8) {
              return (null, 'Hàng ${r + 1} có quá 9 cột.');
            }
            result.grid[r][col] = piece;
            col++;
          } else {
            return (null, "Ký tự '$ch' không hợp lệ trong FEN.");
          }
        }
        if (col > 9) {
          return (null, 'Hàng ${r + 1} có quá 9 cột.');
        }
      }
      if (col != 9) {
        return (null, 'Hàng ${r + 1} phải đủ đúng 9 cột.');
      }
    }

    final turn = fields.length > 1 ? fields[1].toLowerCase() : 'w';
    if (turn == 'w' || turn == 'r') {
      result.currentTurn = PieceColor.red;
    } else if (turn == 'b') {
      result.currentTurn = PieceColor.black;
    } else {
      return (null, "Lượt đi phải là 'w' (Đỏ) hoặc 'b' (Đen).");
    }

    final validationError = result._validatePosition();
    if (validationError != null) {
      return (null, validationError);
    }
    return (result, '');
  }

  String? _validatePosition() {
    final redCount = List<int>.filled(7, 0);
    final blackCount = List<int>.filled(7, 0);

    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final p = grid[r][c];
        if (p == null) continue;
        if (!_isLegalSquare(p, r, c)) {
          return 'Quân ${p.type.label} (${p.color.label}) không thể đứng ở hàng ${r + 1}, cột ${c + 1}.';
        }
        final counter = p.color == PieceColor.red ? redCount : blackCount;
        counter[p.type.index]++;
      }
    }

    const maxPerType = [1, 2, 2, 2, 2, 2, 5];
    for (int t = 0; t < 7; t++) {
      if (redCount[t] > maxPerType[t] || blackCount[t] > maxPerType[t]) {
        return 'Số lượng quân ${PieceType.values[t].label} vượt quá quy định.';
      }
    }
    if (redCount[PieceType.king.index] != 1 ||
        blackCount[PieceType.king.index] != 1) {
      return 'Mỗi bên phải có đúng 1 Tướng.';
    }

    final opponent =
        currentTurn == PieceColor.red ? PieceColor.black : PieceColor.red;
    if (MoveGenerator.checkIfInCheck(this, opponent)) {
      return 'Thế cờ không hợp lệ: bên không có lượt đi đang bị chiếu (hoặc hai Tướng đối mặt nhau).';
    }
    return null;
  }

  static bool _isLegalSquare(Piece p, int r, int c) {
    final red = p.color == PieceColor.red;
    final inPalace = c >= 3 && c <= 5 && (red ? r >= 7 : r <= 2);

    switch (p.type) {
      case PieceType.king:
        return inPalace;
      case PieceType.advisor:
        return inPalace && ((r + c) % 2 == (red ? 0 : 1));
      case PieceType.elephant:
        return _isElephantPoint(r, c, red);
      case PieceType.pawn:
        if (red) {
          if (r > 6) return false;
          if (r >= 5 && c % 2 != 0) return false;
          return true;
        } else {
          if (r < 3) return false;
          if (r <= 4 && c % 2 != 0) return false;
          return true;
        }
      default:
        return true;
    }
  }

  static bool _isElephantPoint(int r, int c, bool red) {
    const redPoints = [
      [9, 2],
      [9, 6],
      [7, 0],
      [7, 4],
      [7, 8],
      [5, 2],
      [5, 6],
    ];
    for (final pt in redPoints) {
      final pr = red ? pt[0] : 9 - pt[0];
      if (pr == r && pt[1] == c) return true;
    }
    return false;
  }

  static Board fromFen(String fen) {
    final (board, error) = tryParseFen(fen);
    if (board == null) throw FormatException(error);
    return board;
  }
}
