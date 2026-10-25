import 'dart:math' as math;
import 'board.dart';
import 'board_evaluator.dart';
import 'game_rules.dart';
import 'move_generator.dart';
import 'piece.dart';
import 'zobrist_hashing.dart';

class EngineEvaluationStats {
  int nodesEvaluated = 0;
  int transpositionHits = 0;
  int betaCutoffs = 0;
  double elapsedMilliseconds = 0;
  int depth = 0;
  int completedDepth = 0;
  int quiescenceNodes = 0;
}

class _SearchBudgetExceededException implements Exception {}

class _Entry {
  final int score;
  final int flag;
  final EngineMove? bestMove;
  const _Entry(this.score, this.flag, this.bestMove);
}

class MinimaxEngine {
  static const int _mateScore = 1000000;
  static const int quiescenceCaptureDepth = 4;
  static const int maxQuiescencePly = 8;

  final int _depth;
  late Stopwatch _watch;
  int _budgetMs = 2000;

  final List<List<EngineMove?>> _killers =
      List.generate(32, (_) => List<EngineMove?>.filled(2, null));
  final List<List<List<int>>> _history = List.generate(
    2,
    (_) => List.generate(90, (_) => List<int>.filled(90, 0)),
  );
  final Map<(int, int), _Entry> _table = {};

  EngineEvaluationStats lastStats = EngineEvaluationStats();

  MinimaxEngine([int depth = 2]) : _depth = depth.clamp(1, 4);

  void _checkBudget() {
    if (_watch.elapsedMilliseconds >= _budgetMs) {
      throw _SearchBudgetExceededException();
    }
  }

  void _clearHeuristics() {
    for (int i = 0; i < 32; i++) {
      _killers[i][0] = null;
      _killers[i][1] = null;
    }
    for (int s = 0; s < 2; s++) {
      for (int f = 0; f < 90; f++) {
        _history[s][f].fillRange(0, 90, 0);
      }
    }
  }

  EngineMove? findBestMove(
    Board board,
    PieceColor computerColor, {
    int timeLimitMs = 2000,
  }) {
    _budgetMs = timeLimitMs;
    _watch = Stopwatch()..start();
    lastStats = EngineEvaluationStats()..depth = _depth;
    _table.clear();
    _clearHeuristics();

    final searchBoard = board.clone();
    searchBoard.setCurrentTurn(computerColor);
    final moves = GameRules.getAllLegalMoves(searchBoard, computerColor);
    EngineMove? bestMove = moves.isNotEmpty ? moves.first : null;

    for (int d = 1; d <= _depth && moves.isNotEmpty; d++) {
      try {
        _checkBudget();
        _orderMoves(searchBoard, moves, bestMove, 0);
        int best = -_mateScore * 2;
        int alpha = best;
        EngineMove? iterationBest;

        for (final move in moves) {
          _checkBudget();
          final captured = _makeMove(searchBoard, move);
          int score;
          try {
            score = _search(
              searchBoard,
              d - 1,
              alpha,
              _mateScore * 2,
              computerColor,
              1,
            );
          } finally {
            _undoMove(searchBoard, move, captured);
          }
          if (score > best) {
            best = score;
            iterationBest = move;
          }
          alpha = math.max(alpha, best);
        }
        bestMove = iterationBest ?? bestMove;
        lastStats.completedDepth = d;
      } on _SearchBudgetExceededException {
        break;
      }
    }
    _watch.stop();
    lastStats.elapsedMilliseconds = _watch.elapsedMilliseconds.toDouble();
    return bestMove;
  }

  int _search(
    Board board,
    int depth,
    int alpha,
    int beta,
    PieceColor perspective,
    int ply,
  ) {
    _checkBudget();
    if (depth == 0) {
      return _quiescence(board, alpha, beta, perspective, ply, 0);
    }
    lastStats.nodesEvaluated++;
    final side = board.currentTurn;
    if (!GameRules.hasLegalMove(board, side)) {
      return side == perspective ? -_mateScore + ply : _mateScore - ply;
    }

    final key = (ZobristHashing.computeHash(board), depth);
    final originalAlpha = alpha;
    final originalBeta = beta;
    EngineMove? preferred;
    final entry = _table[key];
    if (entry != null) {
      preferred = entry.bestMove;
      final cached = _fromTableScore(entry.score, ply);
      lastStats.transpositionHits++;
      if (entry.flag == 0) return cached;
      if (entry.flag == 1) {
        alpha = math.max(alpha, cached);
      } else {
        beta = math.min(beta, cached);
      }
      if (alpha >= beta) return cached;
    }

    final maximizing = side == perspective;
    int best = maximizing ? -_mateScore * 2 : _mateScore * 2;
    final moves = GameRules.getAllLegalMoves(board, side);
    _orderMoves(board, moves, preferred, ply);
    EngineMove? bestMove;

    for (final move in moves) {
      final captured = _makeMove(board, move);
      int score;
      try {
        score = _search(board, depth - 1, alpha, beta, perspective, ply + 1);
      } finally {
        _undoMove(board, move, captured);
      }
      if ((maximizing && score > best) || (!maximizing && score < best)) {
        best = score;
        bestMove = move;
      }
      if (maximizing) {
        alpha = math.max(alpha, best);
      } else {
        beta = math.min(beta, best);
      }
      if (alpha >= beta) {
        lastStats.betaCutoffs++;
        if (captured == null) _recordQuietCutoff(board, move, depth, ply);
        break;
      }
    }

    if (_table.length < 60000) {
      _table[key] = _Entry(
        _toTableScore(best, ply),
        best <= originalAlpha
            ? 2
            : best >= originalBeta
                ? 1
                : 0,
        bestMove,
      );
    }
    return best;
  }

  int _quiescence(
    Board board,
    int alpha,
    int beta,
    PieceColor perspective,
    int ply,
    int qPly,
  ) {
    _checkBudget();
    lastStats.nodesEvaluated++;
    lastStats.quiescenceNodes++;
    final maximizing = board.currentTurn == perspective;
    final inCheck = MoveGenerator.checkIfInCheck(board, board.currentTurn);
    final moves = GameRules.getAllLegalMoves(board, board.currentTurn);
    if (moves.isEmpty) {
      return maximizing ? -_mateScore + ply : _mateScore - ply;
    }
    final standPat = BoardEvaluator.evaluate(board, perspective);
    if (qPly >= maxQuiescencePly) return standPat;

    int best = maximizing ? -_mateScore * 2 : _mateScore * 2;
    if (!inCheck) {
      best = standPat;
      if (maximizing) {
        if (best >= beta) return best;
        alpha = math.max(alpha, best);
      } else {
        if (best <= alpha) return best;
        beta = math.min(beta, best);
      }
      if (qPly >= quiescenceCaptureDepth) return best;
      moves.removeWhere((m) => board.grid[m.to.row][m.to.col] == null);
    }

    _orderMoves(board, moves, null, ply);
    for (final move in moves) {
      final captured = _makeMove(board, move);
      int score;
      try {
        score = _quiescence(
          board,
          alpha,
          beta,
          perspective,
          ply + 1,
          qPly + 1,
        );
      } finally {
        _undoMove(board, move, captured);
      }
      best = maximizing ? math.max(best, score) : math.min(best, score);
      if (maximizing) {
        alpha = math.max(alpha, best);
      } else {
        beta = math.min(beta, best);
      }
      if (alpha >= beta) {
        lastStats.betaCutoffs++;
        break;
      }
    }
    return best;
  }

  static Piece? _makeMove(Board board, EngineMove move) {
    final captured = board.grid[move.to.row][move.to.col];
    board.grid[move.to.row][move.to.col] =
        board.grid[move.from.row][move.from.col];
    board.grid[move.from.row][move.from.col] = null;
    board.switchTurn();
    return captured;
  }

  static void _undoMove(Board board, EngineMove move, Piece? captured) {
    board.switchTurn();
    board.grid[move.from.row][move.from.col] =
        board.grid[move.to.row][move.to.col];
    board.grid[move.to.row][move.to.col] = captured;
  }

  static int _toTableScore(int score, int ply) => score > _mateScore - 1000
      ? score + ply
      : score < -_mateScore + 1000
          ? score - ply
          : score;

  static int _fromTableScore(int score, int ply) => score > _mateScore - 1000
      ? score - ply
      : score < -_mateScore + 1000
          ? score + ply
          : score;

  static bool _sameMove(EngineMove? a, EngineMove? b) =>
      a != null && b != null && a.from == b.from && a.to == b.to;

  void _recordQuietCutoff(Board board, EngineMove move, int depth, int ply) {
    if (ply < 32 && !_sameMove(move, _killers[ply][0])) {
      _killers[ply][1] = _killers[ply][0];
      _killers[ply][0] = move;
    }
    final side = board.currentTurn == PieceColor.red ? 0 : 1;
    final from = move.from.row * 9 + move.from.col;
    final to = move.to.row * 9 + move.to.col;
    _history[side][from][to] =
        math.min(20000, _history[side][from][to] + depth * depth);
  }

  void _orderMoves(
    Board board,
    List<EngineMove> moves,
    EngineMove? preferred,
    int ply,
  ) {
    final ranked = moves
        .map((m) => (m, _moveScore(board, m, preferred, ply)))
        .toList();
    ranked.sort((a, b) => b.$2.compareTo(a.$2));
    for (int i = 0; i < ranked.length; i++) {
      moves[i] = ranked[i].$1;
    }
  }

  int _moveScore(
    Board board,
    EngineMove move,
    EngineMove? preferred,
    int ply,
  ) {
    if (_sameMove(move, preferred)) return 1000000;
    final victim = board.grid[move.to.row][move.to.col];
    final attacker = board.grid[move.from.row][move.from.col];
    if (victim != null) {
      return 100000 +
          BoardEvaluator.getPieceValue(
                  victim.type, victim.color, move.to.row) *
              10 -
          (attacker == null ? 0 : BoardEvaluator.getPieceValue(attacker.type));
    }
    if (ply < 32 && _sameMove(move, _killers[ply][0])) return 80000;
    if (ply < 32 && _sameMove(move, _killers[ply][1])) return 70000;
    final side = board.currentTurn == PieceColor.red ? 0 : 1;
    return _history[side][move.from.row * 9 + move.from.col]
        [move.to.row * 9 + move.to.col];
  }
}
