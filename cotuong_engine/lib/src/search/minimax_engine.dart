import 'dart:math' as math;
import '../board/board.dart';
import '../evaluation/board_evaluator.dart';
import '../models/engine_move.dart';
import '../models/piece.dart';
import '../models/piece_color.dart';
import '../models/piece_type.dart';
import '../models/position.dart';
import '../rules/game_rules.dart';
import '../rules/move_generator.dart';
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

class _TTEntry {
  final int score;
  final int flag; // 0: exact, 1: lowerbound, 2: upperbound
  final EngineMove? bestMove;

  _TTEntry(this.score, this.flag, this.bestMove);
}

class SearchBudgetExceededException implements Exception {}

class MinimaxEngine {
  static const int mateScore = 1000000;
  static const int quiescenceCaptureDepth = 4;
  static const int maxQuiescencePly = 8;

  final int depth;
  int _deadline = 0x7FFFFFFFFFFFFFFF;
  bool _isCanceled = false;

  final List<List<EngineMove?>> _killers =
      List.generate(32, (_) => List.filled(2, null));
  final List<List<List<int>>> _history =
      List.generate(2, (_) => List.generate(90, (_) => List.filled(90, 0)));
  final Map<String, _TTEntry> _table = {};

  EngineEvaluationStats lastStats = EngineEvaluationStats();

  MinimaxEngine({int depth = 2}) : depth = depth.clamp(1, 4);

  void cancel() {
    _isCanceled = true;
  }

  void _checkBudget() {
    if (_isCanceled) throw Exception('Search canceled');
    if (DateTime.now().millisecondsSinceEpoch >= _deadline) {
      throw SearchBudgetExceededException();
    }
  }

  EngineMove? findBestMove(
    Board board,
    PieceColor computerColor, {
    Duration timeLimit = const Duration(seconds: 2),
  }) {
    _isCanceled = false;
    _deadline = DateTime.now().millisecondsSinceEpoch + timeLimit.inMilliseconds;
    final stopwatch = Stopwatch()..start();
    lastStats = EngineEvaluationStats()..depth = depth;
    _table.clear();

    final searchBoard = board.clone();
    searchBoard.setCurrentTurn(computerColor);

    final moves = GameRules.getAllLegalMoves(searchBoard, computerColor);
    if (moves.isEmpty) return null;
    EngineMove? bestMove = moves.first;

    for (int r = 0; r < 32; r++) {
      _killers[r][0] = null;
      _killers[r][1] = null;
    }
    for (int s = 0; s < 2; s++) {
      for (int i = 0; i < 90; i++) {
        _history[s][i].fillRange(0, 90, 0);
      }
    }

    for (int d = 1; d <= depth && moves.isNotEmpty; d++) {
      try {
        _checkBudget();
        _orderMoves(searchBoard, moves, bestMove, 0);
        int best = -mateScore * 2;
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
              mateScore * 2,
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
      } on SearchBudgetExceededException {
        break;
      }
    }

    stopwatch.stop();
    lastStats.elapsedMilliseconds = stopwatch.elapsedMilliseconds.toDouble();
    return bestMove;
  }

  int _search(
    Board board,
    int currentDepth,
    int alpha,
    int beta,
    PieceColor perspective,
    int ply,
  ) {
    lastStats.nodesEvaluated++;
    if (currentDepth <= 0 || ply >= 30) {
      return _quiescence(board, alpha, beta, perspective, 0);
    }
    _checkBudget();

    final hash = ZobristHashing.computeHash(board);
    final key = '$hash:$currentDepth';
    final entry = _table[key];
    if (entry != null) {
      lastStats.transpositionHits++;
      if (entry.flag == 0) return entry.score;
      if (entry.flag == 1 && entry.score > alpha) alpha = entry.score;
      if (entry.flag == 2 && entry.score < beta) beta = entry.score;
      if (alpha >= beta) return entry.score;
    }

    final sideToMove = board.currentTurn;
    final moves = GameRules.getAllLegalMoves(board, sideToMove);

    if (moves.isEmpty) {
      if (MoveGenerator.checkIfInCheck(board, sideToMove)) {
        return -mateScore + ply;
      }
      return 0; // Hòa hết nước đi
    }

    _orderMoves(board, moves, entry?.bestMove, ply);

    int bestScore = -mateScore * 2;
    EngineMove? bestMove;

    for (final move in moves) {
      final captured = _makeMove(board, move);
      int score;
      try {
        score = -_search(
          board,
          currentDepth - 1,
          -beta,
          -alpha,
          GameRules.opponent(perspective),
          ply + 1,
        );
      } finally {
        _undoMove(board, move, captured);
      }

      if (score > bestScore) {
        bestScore = score;
        bestMove = move;
      }
      if (score > alpha) {
        alpha = score;
        if (move.capturedPiece == null && ply < 32) {
          if (_killers[ply][0] != move) {
            _killers[ply][1] = _killers[ply][0];
            _killers[ply][0] = move;
          }
          final sIdx = BoardEvaluator.side(sideToMove);
          final fromIdx = move.from.row * 9 + move.from.col;
          final toIdx = move.to.row * 9 + move.to.col;
          _history[sIdx][fromIdx][toIdx] += currentDepth * currentDepth;
        }
      }
      if (alpha >= beta) {
        lastStats.betaCutoffs++;
        break;
      }
    }

    int flag = 0;
    if (bestScore <= alpha) flag = 2;
    if (bestScore >= beta) flag = 1;
    _table[key] = _TTEntry(bestScore, flag, bestMove);

    return bestScore;
  }

  int _quiescence(
    Board board,
    int alpha,
    int beta,
    PieceColor perspective,
    int qPly,
  ) {
    lastStats.quiescenceNodes++;
    _checkBudget();

    final standPat = BoardEvaluator.evaluate(board, board.currentTurn);
    if (standPat >= beta) return beta;
    if (standPat > alpha) alpha = standPat;
    if (qPly >= maxQuiescencePly) return standPat;

    final sideToMove = board.currentTurn;
    final moves = GameRules.getAllLegalMoves(board, sideToMove)
        .where((m) => m.capturedPiece != null)
        .toList();

    _orderCaptures(board, moves);

    for (final move in moves) {
      final captured = _makeMove(board, move);
      int score;
      try {
        score = -_quiescence(
          board,
          -beta,
          -alpha,
          GameRules.opponent(perspective),
          qPly + 1,
        );
      } finally {
        _undoMove(board, move, captured);
      }

      if (score >= beta) return beta;
      if (score > alpha) alpha = score;
    }
    return alpha;
  }

  void _orderMoves(
    Board board,
    List<EngineMove> moves,
    EngineMove? hashMove,
    int ply,
  ) {
    moves.sort((a, b) {
      int scoreA = 0;
      int scoreB = 0;

      if (hashMove != null) {
        if (a == hashMove) scoreA += 1000000;
        if (b == hashMove) scoreB += 1000000;
      }

      if (a.capturedPiece != null) {
        final attacker = board.getPieceAt(a.from.row, a.from.col);
        scoreA += 10000 +
            BoardEvaluator.getPieceValue(a.capturedPiece!.type) * 10 -
            (attacker != null ? BoardEvaluator.getPieceValue(attacker.type) : 0);
      }
      if (b.capturedPiece != null) {
        final attacker = board.getPieceAt(b.from.row, b.from.col);
        scoreB += 10000 +
            BoardEvaluator.getPieceValue(b.capturedPiece!.type) * 10 -
            (attacker != null ? BoardEvaluator.getPieceValue(attacker.type) : 0);
      }

      if (ply < 32) {
        if (a == _killers[ply][0]) scoreA += 900;
        if (a == _killers[ply][1]) scoreA += 800;
        if (b == _killers[ply][0]) scoreB += 900;
        if (b == _killers[ply][1]) scoreB += 800;
      }

      final s = BoardEvaluator.side(board.currentTurn);
      scoreA += _history[s][a.from.row * 9 + a.from.col][a.to.row * 9 + a.to.col];
      scoreB += _history[s][b.from.row * 9 + b.from.col][b.to.row * 9 + b.to.col];

      return scoreB.compareTo(scoreA);
    });
  }

  void _orderCaptures(Board board, List<EngineMove> moves) {
    moves.sort((a, b) {
      final valA = a.capturedPiece != null
          ? BoardEvaluator.getPieceValue(a.capturedPiece!.type)
          : 0;
      final valB = b.capturedPiece != null
          ? BoardEvaluator.getPieceValue(b.capturedPiece!.type)
          : 0;
      return valB.compareTo(valA);
    });
  }

  Piece? _makeMove(Board board, EngineMove move) {
    final captured = board.getPieceAt(move.to.row, move.to.col);
    board.grid[move.to.row][move.to.col] =
        board.getPieceAt(move.from.row, move.from.col);
    board.grid[move.from.row][move.from.col] = null;
    board.switchTurn();
    return captured;
  }

  void _undoMove(Board board, EngineMove move, Piece? captured) {
    board.switchTurn();
    board.grid[move.from.row][move.from.col] =
        board.getPieceAt(move.to.row, move.to.col);
    board.grid[move.to.row][move.to.col] = captured;
  }
}
