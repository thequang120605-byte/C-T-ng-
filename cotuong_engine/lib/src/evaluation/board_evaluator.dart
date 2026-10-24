import 'dart:math' as math;
import '../board/board.dart';
import '../models/piece.dart';
import '../models/piece_color.dart';
import '../models/piece_type.dart';
import '../models/position.dart';
import '../models/side_scores.dart';
import '../rules/game_rules.dart';
import '../rules/move_generator.dart';

class _Features {
  final List<List<int>> attacks = List.generate(2, (_) => List.filled(90, 0));
  final List<List<int>> cheapestAttacker = List.generate(2, (_) => List.filled(90, 0));
  final List<int> advisors = [0, 0];
  final List<int> elephants = [0, 0];
  final List<Position?> kings = [null, null];
  int opening = 0;
  bool controlsEnemyPalace = false;
  final List<List<int>> wingCount = List.generate(2, (_) => List.filled(2, 0));
  final List<List<int>> wingPressure = List.generate(2, (_) => List.filled(2, 0));
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

  static const List<(int, int)> directions = [(-1, 0), (1, 0), (0, -1), (0, 1)];
  static const List<(int, int)> horseSteps = [
    (-2, -1), (-2, 1), (2, -1), (2, 1),
    (-1, -2), (1, -2), (-1, 2), (1, 2)
  ];
  static const List<int> diagonalSigns = [-1, 1];

  static int getPieceValue(PieceType type) => switch (type) {
        PieceType.rook => 900,
        PieceType.cannon => 450,
        PieceType.horse => 400,
        PieceType.advisor || PieceType.elephant => 200,
        PieceType.pawn => 100,
        PieceType.king => 0,
      };

  static int getPieceValueWithRow(PieceType type, PieceColor color, int row) =>
      type == PieceType.pawn && relativeRow(color, row) >= 5
          ? 200
          : getPieceValue(type);

  static int relativeRow(PieceColor color, int row) =>
      color == PieceColor.red ? 9 - row : row;

  static int index(int row, int col) => row * 9 + col;
  static int side(PieceColor color) => color == PieceColor.red ? 0 : 1;
  static bool major(PieceType type) =>
      type == PieceType.rook || type == PieceType.horse || type == PieceType.cannon;

  static bool palace(PieceColor color, int row, int col) =>
      col >= 3 && col <= 5 && relativeRow(color, row) <= 2;

  static _Features _buildFeatures(Board board) {
    final f = _Features();
    int undeveloped = 0;

    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final p = board.getPieceAt(r, c);
        if (p == null) continue;
        final s = side(p.color);
        final rank = relativeRow(p.color, r);

        if (p.type == PieceType.advisor) f.advisors[s]++;
        if (p.type == PieceType.elephant) f.elephants[s]++;
        if (p.type == PieceType.king) f.kings[s] = Position(r, c);

        if ((p.type == PieceType.rook && rank == 0 && (c == 0 || c == 8)) ||
            (p.type == PieceType.horse && rank == 0 && (c == 1 || c == 7)) ||
            (p.type == PieceType.cannon && rank == 2 && (c == 1 || c == 7))) {
          undeveloped++;
        }

        _addControls(board, f, p, r, c);

        if (major(p.type) && rank >= 5 && c != 4) {
          final wing = c < 4 ? 0 : 1;
          f.wingCount[s][wing]++;
          if (f.controlsEnemyPalace) f.wingPressure[s][wing]++;
        }
      }
    }
    f.opening = ((undeveloped * 100) ~/ 12).clamp(0, 100);
    return f;
  }

  static void _mark(_Features f, Piece p, int row, int col) {
    if (row < 0 || row > 9 || col < 0 || col > 8) return;
    final s = side(p.color);
    final square = index(row, col);
    f.attacks[s][square]++;
    if (palace(GameRules.opponent(p.color), row, col)) {
      f.controlsEnemyPalace = true;
    }
    final value = p.type == PieceType.king ? 10000 : getPieceValue(p.type);
    final previous = f.cheapestAttacker[s][square];
    if (previous == 0 || value < previous) {
      f.cheapestAttacker[s][square] = value;
    }
  }

  static void _addControls(Board board, _Features f, Piece p, int row, int col) {
    f.controlsEnemyPalace = false;
    if (p.type == PieceType.rook || p.type == PieceType.cannon) {
      for (final (dr, dc) in directions) {
        bool screen = false;
        for (int r = row + dr, c = col + dc; r >= 0 && r < 10 && c >= 0 && c < 9; r += dr, c += dc) {
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
      for (final (dr, dc) in horseSteps) {
        final legR = row + (dr.abs() == 2 ? dr.sign : 0);
        final legC = col + (dc.abs() == 2 ? dc.sign : 0);
        if (board.getPieceAt(legR, legC) == null) {
          _mark(f, p, row + dr, col + dc);
        }
      }
    } else if (p.type == PieceType.elephant) {
      for (final dr in diagonalSigns) {
        for (final dc in diagonalSigns) {
          final r = row + dr * 2;
          final c = col + dc * 2;
          if (r >= 0 && r < 10 && relativeRow(p.color, r) <= 4 && board.getPieceAt(row + dr, col + dc) == null) {
            _mark(f, p, r, c);
          }
        }
      }
    } else if (p.type == PieceType.advisor) {
      for (final dr in diagonalSigns) {
        for (final dc in diagonalSigns) {
          if (palace(p.color, row + dr, col + dc)) {
            _mark(f, p, row + dr, col + dc);
          }
        }
      }
    } else if (p.type == PieceType.king) {
      for (final (dr, dc) in directions) {
        if (palace(p.color, row + dr, col + dc)) {
          _mark(f, p, row + dr, col + dc);
        }
      }
    } else if (p.type == PieceType.pawn) {
      _mark(f, p, row + (p.color == PieceColor.red ? -1 : 1), col);
      if (relativeRow(p.color, row) >= 5) {
        _mark(f, p, row, col - 1);
        _mark(f, p, row, col + 1);
      }
    }
  }

  static SideScores getSideScores(Board board, PieceColor color) =>
      _getSideScoresInternal(board, color, _buildFeatures(board));

  static int evaluate(Board board, PieceColor perspective) {
    final f = _buildFeatures(board);
    final net = _getSideScoresInternal(board, PieceColor.red, f).total -
        _getSideScoresInternal(board, PieceColor.black, f).total;
    return perspective == PieceColor.red ? net : -net;
  }

  static SideScores _getSideScoresInternal(Board board, PieceColor color, _Features f) {
    final result = SideScores();
    final s = side(color);
    final enemy = 1 - s;

    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        final p = board.getPieceAt(r, c);
        if (p == null || p.color != color) continue;
        final square = index(r, c);
        final rank = relativeRow(color, r);
        final value = getPieceValueWithRow(p.type, color, r);
        final attacked = f.attacks[enemy][square] > 0;
        final defended = f.attacks[s][square] > 0;

        result.h1 += value;

        if (major(p.type)) {
          final moves = MoveGenerator.getPseudoLegalMoves(board, Position(r, c));
          int safeMobility = 0;
          for (final move in moves) {
            if (f.attacks[enemy][index(move.to.row, move.to.col)] == 0) {
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
            if (rank >= 8 && (!attacked || defended)) result.h2 += bottomCannonBonus;
            result.h3 += _cannonPressure(board, color, r, c);
          }
          if (rank >= 5 && !defended) {
            result.h3 -= (prematureRaidPenalty * f.opening) ~/ 100;
          }
          if (moves.length <= 1) result.h2 -= 35;
        } else if (p.type == PieceType.pawn) {
          result.h2 += math.max(0, 4 - (4 - c).abs()) * 4 + (rank >= 7 ? 12 : 0);
        } else if (p.type == PieceType.advisor && rank == 1 && c == 4) {
          result.h3 += (22 * (50 + f.opening)) ~/ 100;
        } else if (p.type == PieceType.elephant && rank == 2 && c == 4) {
          result.h3 += (18 * (50 + f.opening)) ~/ 100;
        }

        if (p.type == PieceType.advisor || p.type == PieceType.elephant) {
          if (rank <= 2) {
            result.h3 += guardHomeBonus;
          } else if (p.type == PieceType.elephant) {
            result.h3 -= (elephantAdvancePenalty * f.opening) ~/ 100;
          }
        }

        if (p.type != PieceType.king) {
          if (defended) result.h3 += 8;
          if (attacked && !defended) {
            result.h3 -= (value * hangingPiecePercent) ~/ 100;
          } else if (attacked) {
            result.h3 -= math.max(0, value - f.cheapestAttacker[enemy][square]) ~/ 5;
          }
        }
      }
    }

    result.h4 = _wingScore(f, color);
    result.h3 += _kingSafety(board, color, f);
    return result;
  }

  static int _wingScore(_Features f, PieceColor color) {
    final s = side(color);
    int best = 0;
    for (int wing = 0; wing < 2; wing++) {
      if (f.wingPressure[s][wing] >= 2) {
        best = math.max(
          best,
          f.wingCount[s][wing] >= 4
              ? fourPieceWingBonus
              : f.wingCount[s][wing] == 3
                  ? threePieceWingBonus
                  : 0,
        );
      }
    }
    return best;
  }

  static int _kingSafety(Board board, PieceColor color, _Features f) {
    final s = side(color);
    final enemy = 1 - s;
    final king = f.kings[s];
    if (king == null) return -20000;

    int score = f.advisors[s] >= 2 ? advisorPairBonus : 0;
    score += f.elephants[s] >= 2 ? elephantPairBonus : 0;
    final missing = math.max(0, 2 - f.advisors[s]) + math.max(0, 2 - f.elephants[s]);
    score -= (missing * missingGuardPenalty * (50 + f.opening)) ~/ 100;
    if (relativeRow(color, king.row) > 0) score -= (28 * f.opening) ~/ 100;

    for (int r = math.max(0, king.row - 1); r <= math.min(9, king.row + 1); r++) {
      for (int c = math.max(0, king.col - 1); c <= math.min(8, king.col + 1); c++) {
        if (palace(color, r, c) && f.attacks[enemy][index(r, c)] > 0) {
          score -= palaceDangerPenalty * math.min(3, f.attacks[enemy][index(r, c)]);
        }
      }
    }
    if (MoveGenerator.checkIfInCheck(board, color)) score -= 90;
    return score;
  }

  static int _cannonPressure(Board board, PieceColor color, int row, int col) {
    int score = 0;
    for (final (dr, dc) in directions) {
      bool screen = false;
      for (int r = row + dr, c = col + dc; r >= 0 && r < 10 && c >= 0 && c < 9; r += dr, c += dc) {
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
}
