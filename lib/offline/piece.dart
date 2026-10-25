enum PieceColor {
  red,
  black;

  String get label => this == PieceColor.red ? 'Red' : 'Black';

  static PieceColor fromString(String value) {
    return value.toLowerCase() == 'black' ? PieceColor.black : PieceColor.red;
  }
}

enum PieceType {
  king, // Tướng
  advisor, // Sĩ
  elephant, // Tượng
  horse, // Mã
  rook, // Xe
  cannon, // Pháo
  pawn; // Tốt

  String get label {
    switch (this) {
      case PieceType.king:
        return 'King';
      case PieceType.advisor:
        return 'Advisor';
      case PieceType.elephant:
        return 'Elephant';
      case PieceType.horse:
        return 'Horse';
      case PieceType.rook:
        return 'Rook';
      case PieceType.cannon:
        return 'Cannon';
      case PieceType.pawn:
        return 'Pawn';
    }
  }
}

class Piece {
  final PieceType type;
  final PieceColor color;

  const Piece(this.type, this.color);
}

class Position {
  final int row; // 0 đến 9
  final int col; // 0 đến 8

  const Position(this.row, this.col);

  @override
  bool operator ==(Object other) =>
      other is Position && other.row == row && other.col == col;

  @override
  int get hashCode => Object.hash(row, col);

  @override
  String toString() => '($row, $col)';
}

enum GameStatus {
  waitingForPlayers,
  inProgress,
  redWins,
  blackWins,
  draw,
  resigned;

  String get label {
    switch (this) {
      case GameStatus.waitingForPlayers:
        return 'WaitingForPlayers';
      case GameStatus.inProgress:
        return 'InProgress';
      case GameStatus.redWins:
        return 'RedWins';
      case GameStatus.blackWins:
        return 'BlackWins';
      case GameStatus.draw:
        return 'Draw';
      case GameStatus.resigned:
        return 'Resigned';
    }
  }
}

class EngineMove {
  final Position from;
  final Position to;
  final Piece? capturedPiece;

  const EngineMove(this.from, this.to, [this.capturedPiece]);
}
