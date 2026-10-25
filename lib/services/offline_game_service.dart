import 'dart:math' as math;
import 'package:flutter/foundation.dart';
import '../models/game_models.dart';
import '../offline/board.dart';
import '../offline/board_evaluator.dart';
import '../offline/game_rules.dart';
import '../offline/minimax_engine.dart';
import '../offline/move_generator.dart';
import '../offline/piece.dart';

class _AiComputeRequest {
  final String fen;
  final int difficulty;
  const _AiComputeRequest(this.fen, this.difficulty);
}

(int, int, int, int)? _runAiCompute(_AiComputeRequest req) {
  final board = Board.fromFen(req.fen);
  final depth = req.difficulty.clamp(1, 4);
  final budgetMs = const [250, 650, 1200, 2200][depth - 1];
  final engine = MinimaxEngine(depth);
  final move = engine.findBestMove(
    board,
    PieceColor.black,
    timeLimitMs: budgetMs,
  );
  if (move == null) return null;
  return (move.from.row, move.from.col, move.to.row, move.to.col);
}

class _Session {
  final int id;
  final Board board = Board();
  GameStatus status = GameStatus.inProgress;
  final List<MoveHistoryDto> moves = [];
  final Map<String, int> positions = {};
  String endReason = '';
  int revision = 1;
  final DateTime created = DateTime.now().toUtc();

  _Session(this.id) {
    positions[board.toFen()] = 1;
  }
}

class OfflineGameService {
  final Map<int, _Session> _sessions = {};
  int _nextId = 0;

  Future<GameResponseDto> createAsync() async {
    _nextId--;
    final session = _Session(_nextId);
    while (_sessions.length >= 8) {
      final maxKey = _sessions.keys.reduce(math.max);
      _sessions.remove(maxKey);
    }
    _sessions[session.id] = session;
    return _snapshot(session);
  }

  Future<GameResponseDto> createFromFenAsync(String fen) async {
    _nextId--;
    final session = _Session(_nextId);
    final (parsedBoard, _) = Board.tryParseFen(fen);
    if (parsedBoard != null) {
      session.board.grid = parsedBoard.grid;
      session.board.currentTurn = parsedBoard.currentTurn;
      session.positions.clear();
      session.positions[fen] = 1;
    }
    while (_sessions.length >= 8) {
      final maxKey = _sessions.keys.reduce(math.max);
      _sessions.remove(maxKey);
    }
    _sessions[session.id] = session;
    return _snapshot(session);
  }

  _Session _getSession(int id) {
    final s = _sessions[id];
    if (s == null) {
      throw Exception(
          'Ván luyện tập không còn trong bộ nhớ. Hãy tạo ván mới.');
    }
    return s;
  }

  Future<GameResponseDto> getAsync(int id) async {
    return _snapshot(_getSession(id));
  }

  Future<List<ValidMoveDto>> validMovesAsync(int id, int row, int col) async {
    final s = _getSession(id);
    if (row < 0 ||
        row > 9 ||
        col < 0 ||
        col > 8 ||
        s.status != GameStatus.inProgress ||
        s.board.currentTurn != PieceColor.red) {
      return [];
    }
    final piece = s.board.getPieceAt(row, col);
    if (piece == null || piece.color != PieceColor.red) return [];
    return MoveGenerator.getValidMoves(s.board, Position(row, col))
        .map(
          (m) => ValidMoveDto(
            row: m.to.row,
            col: m.to.col,
            isCapture: s.board.getPieceAt(m.to.row, m.to.col) != null,
          ),
        )
        .toList();
  }

  Future<GameResponseDto> moveAsync(int id, MakeMoveRequestDto move) async {
    final s = _getSession(id);
    if (s.status != GameStatus.inProgress ||
        s.board.currentTurn != PieceColor.red) {
      throw Exception('Chưa đến lượt quân Đỏ.');
    }
    if (move.fromX < 0 ||
        move.fromX > 9 ||
        move.toX < 0 ||
        move.toX > 9 ||
        move.fromY < 0 ||
        move.fromY > 8 ||
        move.toY < 0 ||
        move.toY > 8) {
      throw Exception('Tọa độ nằm ngoài bàn cờ.');
    }
    final piece = s.board.getPieceAt(move.fromX, move.fromY);
    if (piece == null || piece.color != PieceColor.red) {
      throw Exception('Hãy chọn quân Đỏ của bạn.');
    }
    final legalMoves = MoveGenerator.getValidMoves(
      s.board,
      Position(move.fromX, move.fromY),
    );
    EngineMove? legal;
    for (final m in legalMoves) {
      if (m.to.row == move.toX && m.to.col == move.toY) {
        legal = m;
        break;
      }
    }
    if (legal == null) {
      throw Exception('Nước đi này không hợp lệ.');
    }
    _apply(s, legal);
    return _snapshot(s);
  }

  Future<GameResponseDto> computerAsync(int id, int difficulty) async {
    final s = _getSession(id);
    if (s.status != GameStatus.inProgress ||
        s.board.currentTurn != PieceColor.black) {
      return _snapshot(s);
    }
    final fen = s.board.toFen();
    final revBefore = s.revision;
    final coords = await compute(
      _runAiCompute,
      _AiComputeRequest(fen, difficulty),
    );
    if (s.revision != revBefore ||
        s.status != GameStatus.inProgress ||
        s.board.currentTurn != PieceColor.black) {
      return _snapshot(s);
    }
    if (coords != null) {
      final (fromRow, fromCol, toRow, toCol) = coords;
      final captured = s.board.getPieceAt(toRow, toCol);
      _apply(
        s,
        EngineMove(Position(fromRow, fromCol), Position(toRow, toCol), captured),
      );
    } else {
      s.status = GameRules.statusAfterMove(s.board);
      s.revision++;
    }
    return _snapshot(s);
  }

  Future<GameResponseDto> resignAsync(int id) async {
    final s = _getSession(id);
    if (s.status == GameStatus.inProgress) {
      s.status = GameStatus.blackWins;
      s.endReason = 'Resigned';
      s.revision++;
    }
    return _snapshot(s);
  }

  Future<GameResponseDto> offerDrawAsync(int gameId) async {
    final s = _sessions[gameId];
    if (s == null) throw Exception('Ván không tồn tại.');
    if (s.status == GameStatus.inProgress) {
      s.status = GameStatus.draw;
      s.endReason = 'DrawAgreed';
      s.revision++;
    }
    return _snapshot(s);
  }

  Future<GameResponseDto> undoAsync(int gameId) async {
    final s = _sessions[gameId];
    if (s == null) throw Exception('Ván không tồn tại.');
    if (s.moves.isEmpty) return _snapshot(s);

    final movesToKeep = s.moves.length >= 2 ? s.moves.length - 2 : 0;
    final keptMoves = s.moves.take(movesToKeep).toList();

    s.board.initializeBoard();
    s.moves.clear();
    s.positions.clear();
    s.positions[s.board.toFen()] = 1;
    s.status = GameStatus.inProgress;
    s.endReason = '';

    for (final m in keptMoves) {
      _apply(s, EngineMove(Position(m.fromX, m.fromY), Position(m.toX, m.toY)));
    }
    s.revision++;
    return _snapshot(s);
  }

  static void _apply(_Session s, EngineMove move) {
    final piece = s.board.getPieceAt(move.from.row, move.from.col)!;
    s.board.grid[move.to.row][move.to.col] = piece;
    s.board.grid[move.from.row][move.from.col] = null;
    s.board.switchTurn();

    s.moves.add(
      MoveHistoryDto(
        id: s.moves.length + 1,
        moveOrder: s.moves.length,
        fromX: move.from.row,
        fromY: move.from.col,
        toX: move.to.row,
        toY: move.to.col,
        pieceType: piece.type.label,
        pieceColor: piece.color.label,
        movedAt: DateTime.now().toUtc(),
      ),
    );

    s.status = GameRules.statusAfterMove(s.board);
    if (s.status != GameStatus.inProgress) {
      s.endReason = MoveGenerator.checkIfInCheck(s.board, s.board.currentTurn)
          ? 'Checkmate'
          : 'NoLegalMoves';
    }
    final key = s.board.toFen();
    final count = (s.positions[key] ?? 0) + 1;
    s.positions[key] = count;
    if (s.status == GameStatus.inProgress && count >= 3) {
      s.status = GameStatus.draw;
      s.endReason = 'Repetition';
    }
    s.revision++;
  }

  static GameResponseDto _snapshot(_Session s) {
    final red = BoardEvaluator.getSideScores(s.board, PieceColor.red);
    final black = BoardEvaluator.getSideScores(s.board, PieceColor.black);
    final check = MoveGenerator.checkIfInCheck(s.board, s.board.currentTurn);
    final noMoves = !GameRules.hasLegalMove(s.board, s.board.currentTurn);

    final pieces = <PieceDto>[];
    for (int row = 0; row < 10; row++) {
      for (int col = 0; col < 9; col++) {
        final p = s.board.getPieceAt(row, col);
        if (p != null) {
          pieces.add(
            PieceDto(
              row: row,
              col: col,
              type: p.type.label,
              color: p.color.label,
            ),
          );
        }
      }
    }

    return GameResponseDto(
      id: s.id,
      status: s.status.label,
      currentTurn: s.board.currentTurn.label,
      revision: s.revision,
      isCheck: check,
      hasNoLegalMoves: noMoves,
      isCheckmate: check && noMoves,
      endReason: s.endReason,
      gameMode: 'Offline',
      isRated: false,
      remainingSeconds: 0,
      createdAt: s.created,
      lastMoveAt: s.moves.isNotEmpty ? s.moves.last.movedAt : null,
      evaluation: HeuristicEvaluationDto(
        h1: red.h1 - black.h1,
        h2: red.h2 - black.h2,
        h3: red.h3 - black.h3,
        h4: red.h4 - black.h4,
      ),
      redScore: red.total - black.total,
      blackScore: black.total - red.total,
      boardPieces: pieces,
      moves: List<MoveHistoryDto>.from(s.moves),
    );
  }
}
