import 'dart:math' as math;
import 'board.dart';
import 'game_rules.dart';
import 'move_generator.dart';
import 'piece.dart';

class SideScores {
  int h1 = 0;
  int h2 = 0;
  int h3 = 0;
  int h4 = 0;
  int get total => h1 + h2 + h3 + h4;
}

class _Features {
  final List<List<int>> attacks =
      List.generate(2, (_) => List<int>.filled(90, 0));
  final List<List<int>> cheapestAttacker =
      List.generate(2, (_) => List<int>.filled(90, 0));
  final List<int> advisors = [0, 0];
  final List<int> elephants = [0, 0];
  final List<Position?> kings = [null, null];
  int opening = 0;
  bool controlsEnemyPalace = false;
  final List<List<int>> wingCount = [
    [0, 0],
    [0, 0]
  ];
  final List<List<int>> wingPressure = [
    [0, 0],
    [0, 0]
  ];
}

class BoardEvaluator {
  static const int threePieceWingBonus = 60;
  static const int fourPieceWingBonus = 100;
  static const int advisorPairBonus = 70;
  static const int elephantPairBonus = 55;
  static const int missingGuardPenalty = 35;
  static const int guardHomeBonus = 12;
  static const int elephantAdvancePenalty = 35;
  static const int palaceDangerPenalty = 24;
  static const int hangingPiecePercent = 55;
  static const int prematureRaidPenalty = 55;
  static const int rookMobilityWeight = 3;
  static const int horseMobilityWeight = 6;
  static const int cannonMobilityWeight = 2;
  static const int importantFileBonus = 18;
  static const int centralRookBonus = 28;
  static const int riverRookBonus = 22;
  static const int bottomCannonBonus = 32;

  static int getPieceValue(PieceType type, [PieceColor? color, int? row]) {
    if (color != null &&
        row != null &&
        type == PieceType.pawn &&
        _relativeRow(color, row) >= 5) {
      return 200;
    }
    switch (type) {
      case PieceType.rook:
        return 900;
      case PieceType.cannon:
        return 450;
      case PieceType.horse:
        return 400;
      case PieceType.advisor:
      case PieceType.elephant:
        return 200;
      case PieceType.pawn:
        return 100;
      case PieceType.king:
        return 0;
    }
  }

  static int _relativeRow(PieceColor color, int row) =>
      color == PieceColor.red ? 9 - row : row;
  static int _index(int row, int col) => row * 9 + col;
  static int _side(PieceColor color) => color == PieceColor.red ? 0 : 1;
  static bool _major(PieceType type) =>
      type == PieceType.rook ||
      type == PieceType.horse ||
      type == PieceType.cannon;

  static const _directions = [
    (-1, 0),
    (1, 0),
    (0, -1),
    (0, 1),
  ];
  static const _horseSteps = [
    (-2, -1),
    (-2, 1),
    (2, -1),
    (2, 1),
    (-1, -2),
    (1, -2),
    (-1, 2),
    (1, 2),
  ];
  static const _diagonalSigns = [-1, 1];

  static _Features _buildFeatures(Board board) {
    final f = _Features();
    int undeveloped = 0;
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final p = board.getPieceAt(r, c);
        if (p == null) continue;
        final side = _side(p.color);
        final rank = _relativeRow(p.color, r);
        if (p.type == PieceType.advisor) f.advisors[side]++;
        if (p.type == PieceType.elephant) f.elephants[side]++;
        if (p.type == PieceType.king) f.kings[side] = Position(r, c);
        if ((p.type == PieceType.rook && rank == 0 && (c == 0 || c == 8)) ||
            (p.type == PieceType.horse && rank == 0 && (c == 1 || c == 7)) ||
            (p.type == PieceType.cannon && rank == 2 && (c == 1 || c == 7))) {
          undeveloped++;
        }
        _addControls(board, f, p, r, c);
        if (_major(p.type) && rank >= 5 && c != 4) {
          final wing = c < 4 ? 0 : 1;
          f.wingCount[side][wing]++;
          if (f.controlsEnemyPalace) f.wingPressure[side][wing]++;
        }
      }
    }
    f.opening = (undeveloped * 100 ~/ 12).clamp(0, 100);
    return f;
  }

  static void _mark(_Features f, Piece p, int row, int col) {
    if (row < 0 || row > 9 || col < 0 || col > 8) return;
    final side = _side(p.color);
    final square = _index(row, col);
    f.attacks[side][square]++;
    if (_palace(GameRules.opponent(p.color), row, col)) {
      f.controlsEnemyPalace = true;
    }
    final value = p.type == PieceType.king ? 10000 : getPieceValue(p.type);
    final previous = f.cheapestAttacker[side][square];
    if (previous == 0 || value < previous) {
      f.cheapestAttacker[side][square] = value;
    }
  }

  static bool _palace(PieceColor color, int row, int col) =>
      col >= 3 && col <= 5 && _relativeRow(color, row) <= 2;

  static void _addControls(
      Board board, _Features f, Piece p, int row, int col) {
    f.controlsEnemyPalace = false;
    if (p.type == PieceType.rook || p.type == PieceType.cannon) {
      for (final (dr, dc) in _directions) {
        bool screen = false;
        for (int r = row + dr, c = col + dc;
            r >= 0 && r < 10 && c >= 0 && c < 9;
            r += dr, c += dc) {
          final target = board.getPieceAt(r, c);
          if (p.type == PieceType.rook) {
            _mark(f, p, r, c);
            if (target != null) break;
          } else if (!screen) {
            if (target != null) screen = true;
          } else {
            _mark(f, p, r, c);
            if (target != null) break;
          }
        }
      }
    } else if (p.type == PieceType.horse) {
      for (final (dr, dc) in _horseSteps) {
        final legR = row + (dr.abs() == 2 ? dr.sign : 0);
        final legC = col + (dc.abs() == 2 ? dc.sign : 0);
        if (board.getPieceAt(legR, legC) == null) {
          _mark(f, p, row + dr, col + dc);
        }
      }
    } else if (p.type == PieceType.elephant) {
      for (final dr in _diagonalSigns) {
        for (final dc in _diagonalSigns) {
          final r = row + dr * 2;
          final c = col + dc * 2;
          if (r >= 0 &&
              r < 10 &&
              _relativeRow(p.color, r) <= 4 &&
              board.getPieceAt(row + dr, col + dc) == null) {
            _mark(f, p, r, c);
          }
        }
      }
    } else if (p.type == PieceType.advisor) {
      for (final dr in _diagonalSigns) {
        for (final dc in _diagonalSigns) {
          if (_palace(p.color, row + dr, col + dc)) {
            _mark(f, p, row + dr, col + dc);
          }
        }
      }
    } else if (p.type == PieceType.king) {
      for (final (dr, dc) in _directions) {
        if (_palace(p.color, row + dr, col + dc)) {
          _mark(f, p, row + dr, col + dc);
        }
      }
    } else if (p.type == PieceType.pawn) {
      _mark(f, p, row + (p.color == PieceColor.red ? -1 : 1), col);
      if (_relativeRow(p.color, row) >= 5) {
        _mark(f, p, row, col - 1);
        _mark(f, p, row, col + 1);
      }
    }
  }

  static SideScores getSideScores(Board board, PieceColor color) =>
      _getSideScores(board, color, _buildFeatures(board));

  static int evaluate(Board board, PieceColor perspective) {
    final f = _buildFeatures(board);
    final net = _getSideScores(board, PieceColor.red, f).total -
        _getSideScores(board, PieceColor.black, f).total;
    return perspective == PieceColor.red ? net : -net;
  }

  static SideScores _getSideScores(
      Board board, PieceColor color, _Features f) {
    final result = SideScores();
    final side = _side(color);
    final enemy = 1 - side;

    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final p = board.getPieceAt(r, c);
        if (p == null || p.color != color) continue;
        final square = _index(r, c);
        final rank = _relativeRow(color, r);
        final value = getPieceValue(p.type, color, r);
        final attacked = f.attacks[enemy][square] > 0;
        final defended = f.attacks[side][square] > 0;

        result.h1 += value;
        if (_major(p.type)) {
          final moves = MoveGenerator.getPseudoLegalMoves(board, Position(r, c));
          int safeMobility = 0;
          for (final move in moves) {
            if (f.attacks[enemy][_index(move.to.row, move.to.col)] == 0) {
              safeMobility++;
            }
          }
          final weight = p.type == PieceType.horse
              ? horseMobilityWeight
              : p.type == PieceType.rook
                  ? rookMobilityWeight
                  : cannonMobilityWeight;
          result.h2 += moves.length * weight + safeMobility * 2;
          if (c == 3 || c == 5) result.h2 += importantFileBonus;
          result.h2 += math.max(0, 4 - (4 - c).abs()) * 3;
          if (p.type == PieceType.rook) {
            if (c == 4) result.h2 += centralRookBonus;
            if (rank == 4 || rank == 5) result.h2 += riverRookBonus;
            if (rank >= 1 && rank <= 3) result.h2 += 18;
          }
          if (p.type == PieceType.cannon) {
            if (rank >= 8 && (!attacked || defended)) {
              result.h2 += bottomCannonBonus;
            }
            result.h3 += _cannonPressure(board, color, r, c);
          }
          if (rank >= 5 && !defended) {
            result.h3 -= prematureRaidPenalty * f.opening ~/ 100;
          }
          if (moves.length <= 1) result.h2 -= 35;
        } else if (p.type == PieceType.pawn) {
          result.h2 +=
              math.max(0, 4 - (4 - c).abs()) * 4 + (rank >= 7 ? 12 : 0);
        } else if (p.type == PieceType.advisor && rank == 1 && c == 4) {
          result.h3 += 22 * (50 + f.opening) ~/ 100;
        } else if (p.type == PieceType.elephant && rank == 2 && c == 4) {
          result.h3 += 18 * (50 + f.opening) ~/ 100;
        }

        if (p.type == PieceType.advisor || p.type == PieceType.elephant) {
          if (rank <= 2) {
            result.h3 += guardHomeBonus;
          } else if (p.type == PieceType.elephant) {
            result.h3 -= elephantAdvancePenalty * f.opening ~/ 100;
          }
        }
        if (p.type != PieceType.king) {
          if (defended) result.h3 += 8;
          if (attacked && !defended) {
            result.h3 -= value * hangingPiecePercent ~/ 100;
          } else if (attacked) {
            result.h3 -=
                math.max(0, value - f.cheapestAttacker[enemy][square]) ~/ 5;
          }
        }
      }
    }

    result.h4 = _wingScore(f, color);
    result.h3 += _kingSafety(board, color, f);
    return result;
  }

  static int evaluateWingCoordination(Board board, PieceColor color) {
    return _wingScore(_buildFeatures(board), color);
  }

  static int _wingScore(_Features f, PieceColor color) {
    final side = _side(color);
    int best = 0;
    for (int wing = 0; wing < 2; wing++) {
      if (f.wingPressure[side][wing] >= 2) {
        final bonus = f.wingCount[side][wing] >= 4
            ? fourPieceWingBonus
            : f.wingCount[side][wing] == 3
                ? threePieceWingBonus
                : 0;
        best = math.max(best, bonus);
      }
    }
    return best;
  }

  static int _kingSafety(Board board, PieceColor color, _Features f) {
    final side = _side(color);
    final enemy = 1 - side;
    final king = f.kings[side];
    if (king == null) return -20000;

    int score = f.advisors[side] >= 2 ? advisorPairBonus : 0;
    score += f.elephants[side] >= 2 ? elephantPairBonus : 0;
    final missing = math.max(0, 2 - f.advisors[side]) +
        math.max(0, 2 - f.elephants[side]);
    score -= missing * missingGuardPenalty * (50 + f.opening) ~/ 100;
    if (_relativeRow(color, king.row) > 0) score -= 28 * f.opening ~/ 100;
    for (int r = math.max(0, king.row - 1);
        r <= math.min(9, king.row + 1);
        r++) {
      for (int c = math.max(0, king.col - 1);
          c <= math.min(8, king.col + 1);
          c++) {
        if (_palace(color, r, c) && f.attacks[enemy][_index(r, c)] > 0) {
          score -= palaceDangerPenalty *
              math.min(3, f.attacks[enemy][_index(r, c)]);
        }
      }
    }
    if (MoveGenerator.checkIfInCheck(board, color)) score -= 90;
    return score;
  }

  static int _cannonPressure(Board board, PieceColor color, int row, int col) {
    int score = 0;
    for (final (dr, dc) in _directions) {
      bool screen = false;
      for (int r = row + dr, c = col + dc;
          r >= 0 && r < 10 && c >= 0 && c < 9;
          r += dr, c += dc) {
        final p = board.getPieceAt(r, c);
        if (p == null) continue;
        if (!screen) {
          screen = true;
          continue;
        }
        if (p.color != color) {
          score += p.type == PieceType.king ? 35 : 12;
        }
        break;
      }
    }
    return score;
  }

  static bool isCheckmate(Board board, PieceColor color) =>
      MoveGenerator.checkIfInCheck(board, color) &&
      !GameRules.hasLegalMove(board, color);

  static bool hasNoLegalMoves(Board board, PieceColor color) =>
      !GameRules.hasLegalMove(board, color);
}
