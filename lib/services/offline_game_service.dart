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
  bool isPassAndPlay = false;
  bool isCoUp = false;
  final Map<String, PieceDto> coUpPieces = {};

  _Session(this.id) {
    positions[board.toFen()] = 1;
  }
}

class OfflineGameService {
  final Map<int, _Session> _sessions = {};
  int _nextId = 0;

  static List<PieceDto> defaultStartingPieces() {
    final b = Board();
    final pieces = <PieceDto>[];
    for (int row = 0; row < 10; row++) {
      for (int col = 0; col < 9; col++) {
        final p = b.getPieceAt(row, col);
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
    return pieces;
  }

  Future<GameResponseDto> createAsync() async {
    _nextId--;
    final session = _Session(_nextId);
    _cleanupOldSessions();
    _sessions[session.id] = session;
    return _snapshot(session);
  }

  Future<GameResponseDto> createPassAndPlayAsync() async {
    _nextId--;
    final session = _Session(_nextId);
    session.isPassAndPlay = true;
    _cleanupOldSessions();
    _sessions[session.id] = session;
    return _snapshot(session);
  }

  Future<GameResponseDto> createCoUpAsync({bool isPassAndPlay = false}) async {
    _nextId--;
    final session = _Session(_nextId);
    session.isCoUp = true;
    session.isPassAndPlay = isPassAndPlay;

    // Tướng luôn ngửa ở vị trí ban đầu
    session.coUpPieces['9,4'] = const PieceDto(
      row: 9,
      col: 4,
      type: 'General',
      color: 'Red',
      isFaceDown: false,
      realType: 'General',
    );
    session.coUpPieces['0,4'] = const PieceDto(
      row: 0,
      col: 4,
      type: 'General',
      color: 'Black',
      isFaceDown: false,
      realType: 'General',
    );

    // 15 quân còn lại mỗi bên
    final standardPieces = [
      'Chariot', 'Chariot',
      'Horse', 'Horse',
      'Elephant', 'Elephant',
      'Advisor', 'Advisor',
      'Cannon', 'Cannon',
      'Soldier', 'Soldier', 'Soldier', 'Soldier', 'Soldier',
    ];

    final redPool = List<String>.from(standardPieces)..shuffle();
    final blackPool = List<String>.from(standardPieces)..shuffle();

    final redSpots = [
      (9, 0, 'Chariot'),
      (9, 1, 'Horse'),
      (9, 2, 'Elephant'),
      (9, 3, 'Advisor'),
      (9, 5, 'Advisor'),
      (9, 6, 'Elephant'),
      (9, 7, 'Horse'),
      (9, 8, 'Chariot'),
      (7, 1, 'Cannon'),
      (7, 7, 'Cannon'),
      (6, 0, 'Soldier'),
      (6, 2, 'Soldier'),
      (6, 4, 'Soldier'),
      (6, 6, 'Soldier'),
      (6, 8, 'Soldier'),
    ];

    for (int i = 0; i < redSpots.length; i++) {
      final (r, c, spotRole) = redSpots[i];
      session.coUpPieces['$r,$c'] = PieceDto(
        row: r,
        col: c,
        type: spotRole,
        color: 'Red',
        isFaceDown: true,
        realType: redPool[i],
      );
    }

    final blackSpots = [
      (0, 0, 'Chariot'),
      (0, 1, 'Horse'),
      (0, 2, 'Elephant'),
      (0, 3, 'Advisor'),
      (0, 5, 'Advisor'),
      (0, 6, 'Elephant'),
      (0, 7, 'Horse'),
      (0, 8, 'Chariot'),
      (2, 1, 'Cannon'),
      (2, 7, 'Cannon'),
      (3, 0, 'Soldier'),
      (3, 2, 'Soldier'),
      (3, 4, 'Soldier'),
      (3, 6, 'Soldier'),
      (3, 8, 'Soldier'),
    ];

    for (int i = 0; i < blackSpots.length; i++) {
      final (r, c, spotRole) = blackSpots[i];
      session.coUpPieces['$r,$c'] = PieceDto(
        row: r,
        col: c,
        type: spotRole,
        color: 'Black',
        isFaceDown: true,
        realType: blackPool[i],
      );
    }

    _cleanupOldSessions();
    _sessions[session.id] = session;
    return _snapshot(session);
  }

  void _cleanupOldSessions() {
    while (_sessions.length >= 8) {
      final maxKey = _sessions.keys.reduce(math.max);
      _sessions.remove(maxKey);
    }
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
    _cleanupOldSessions();
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
    if (row < 0 || row > 9 || col < 0 || col > 8 || s.status != GameStatus.inProgress) {
      return [];
    }

    if (s.isCoUp) {
      final p = s.coUpPieces['$row,$col'];
      if (p == null) return [];
      final isRedTurn = s.board.currentTurn == PieceColor.red;
      if (isRedTurn && p.color.toLowerCase() != 'red') return [];
      if (!isRedTurn && p.color.toLowerCase() != 'black') return [];

      return _getCoUpValidMoves(s, p);
    }

    final isExpectedTurn = s.isPassAndPlay
        ? true
        : s.board.currentTurn == PieceColor.red;

    if (!isExpectedTurn) return [];

    final piece = s.board.getPieceAt(row, col);
    if (piece == null || piece.color != s.board.currentTurn) return [];

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

  List<ValidMoveDto> _getCoUpValidMoves(_Session s, PieceDto p) {
    final moves = <ValidMoveDto>[];
    final isRed = p.color.toLowerCase() == 'red';
    final role = p.isFaceDown ? p.type : (p.realType ?? p.type);

    bool isBlocked(int r, int c) => s.coUpPieces.containsKey('$r,$c');
    bool isEnemy(int r, int c) {
      final target = s.coUpPieces['$r,$c'];
      if (target == null) return false;
      return target.color.toLowerCase() != (isRed ? 'red' : 'black');
    }

    switch (role.toLowerCase()) {
      case 'chariot':
      case 'xe':
        for (final dir in const [(-1, 0), (1, 0), (0, -1), (0, 1)]) {
          int r = p.row + dir.$1;
          int c = p.col + dir.$2;
          while (r >= 0 && r <= 9 && c >= 0 && c <= 8) {
            if (!isBlocked(r, c)) {
              moves.add(ValidMoveDto(row: r, col: c, isCapture: false));
            } else {
              if (isEnemy(r, c)) {
                moves.add(ValidMoveDto(row: r, col: c, isCapture: true));
              }
              break;
            }
            r += dir.$1;
            c += dir.$2;
          }
        }
        break;

      case 'horse':
      case 'mã':
        const steps = [
          (-2, -1, -1, 0), (-2, 1, -1, 0),
          (2, -1, 1, 0), (2, 1, 1, 0),
          (-1, -2, 0, -1), (1, -2, 0, -1),
          (-1, 2, 0, 1), (1, 2, 0, 1),
        ];
        for (final st in steps) {
          final legR = p.row + st.$3;
          final legC = p.col + st.$4;
          final r = p.row + st.$1;
          final c = p.col + st.$2;
          if (r >= 0 && r <= 9 && c >= 0 && c <= 8) {
            if (!isBlocked(legR, legC)) {
              if (!isBlocked(r, c)) {
                moves.add(ValidMoveDto(row: r, col: c, isCapture: false));
              } else if (isEnemy(r, c)) {
                moves.add(ValidMoveDto(row: r, col: c, isCapture: true));
              }
            }
          }
        }
        break;

      case 'elephant':
      case 'tượng':
        const diag = [(-2, -2, -1, -1), (-2, 2, -1, 1), (2, -2, 1, -1), (2, 2, 1, 1)];
        for (final d in diag) {
          final eyeR = p.row + d.$3;
          final eyeC = p.col + d.$4;
          final r = p.row + d.$1;
          final c = p.col + d.$2;
          if (r >= 0 && r <= 9 && c >= 0 && c <= 8) {
            if (!isBlocked(eyeR, eyeC)) {
              // Quân úp hoặc cờ úp cho phép Tượng qua sông
              if (!isBlocked(r, c)) {
                moves.add(ValidMoveDto(row: r, col: c, isCapture: false));
              } else if (isEnemy(r, c)) {
                moves.add(ValidMoveDto(row: r, col: c, isCapture: true));
              }
            }
          }
        }
        break;

      case 'advisor':
      case 'sĩ':
        const diag = [(-1, -1), (-1, 1), (1, -1), (1, 1)];
        for (final d in diag) {
          final r = p.row + d.$1;
          final c = p.col + d.$2;
          if (r >= 0 && r <= 9 && c >= 0 && c <= 8) {
            if (!isBlocked(r, c)) {
              moves.add(ValidMoveDto(row: r, col: c, isCapture: false));
            } else if (isEnemy(r, c)) {
              moves.add(ValidMoveDto(row: r, col: c, isCapture: true));
            }
          }
        }
        break;

      case 'cannon':
      case 'pháo':
        for (final dir in const [(-1, 0), (1, 0), (0, -1), (0, 1)]) {
          int r = p.row + dir.$1;
          int c = p.col + dir.$2;
          bool jumped = false;
          while (r >= 0 && r <= 9 && c >= 0 && c <= 8) {
            if (!jumped) {
              if (!isBlocked(r, c)) {
                moves.add(ValidMoveDto(row: r, col: c, isCapture: false));
              } else {
                jumped = true;
              }
            } else {
              if (isBlocked(r, c)) {
                if (isEnemy(r, c)) {
                  moves.add(ValidMoveDto(row: r, col: c, isCapture: true));
                }
                break;
              }
            }
            r += dir.$1;
            c += dir.$2;
          }
        }
        break;

      case 'soldier':
      case 'tốt':
      default:
        final forwardRow = isRed ? p.row - 1 : p.row + 1;
        if (forwardRow >= 0 && forwardRow <= 9) {
          if (!isBlocked(forwardRow, p.col)) {
            moves.add(ValidMoveDto(row: forwardRow, col: p.col, isCapture: false));
          } else if (isEnemy(forwardRow, p.col)) {
            moves.add(ValidMoveDto(row: forwardRow, col: p.col, isCapture: true));
          }
        }
        final crossedRiver = isRed ? p.row <= 4 : p.row >= 5;
        if (crossedRiver) {
          for (final c in [p.col - 1, p.col + 1]) {
            if (c >= 0 && c <= 8) {
              if (!isBlocked(p.row, c)) {
                moves.add(ValidMoveDto(row: p.row, col: c, isCapture: false));
              } else if (isEnemy(p.row, c)) {
                moves.add(ValidMoveDto(row: p.row, col: c, isCapture: true));
              }
            }
          }
        }
        break;
    }
    return moves;
  }

  Future<GameResponseDto> moveAsync(int id, MakeMoveRequestDto move) async {
    final s = _getSession(id);
    if (s.status != GameStatus.inProgress) {
      throw Exception('Trận đấu đã kết thúc.');
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

    if (s.isCoUp) {
      final p = s.coUpPieces['${move.fromX},${move.fromY}'];
      if (p == null) throw Exception('Không tìm thấy quân cờ.');
      final isRedTurn = s.board.currentTurn == PieceColor.red;
      if (isRedTurn && p.color.toLowerCase() != 'red') {
        throw Exception('Chưa đến lượt quân Đỏ.');
      }
      if (!isRedTurn && p.color.toLowerCase() != 'black') {
        throw Exception('Chưa đến lượt quân Đen.');
      }

      final validMoves = _getCoUpValidMoves(s, p);
      final isLegal = validMoves.any((m) => m.row == move.toX && m.col == move.toY);
      if (!isLegal) {
        throw Exception('Nước đi không hợp lệ trong Cờ Úp.');
      }

      s.coUpPieces.remove('${move.fromX},${move.fromY}');
      final captured = s.coUpPieces.remove('${move.toX},${move.toY}');

      // Tiết lộ danh tính quân cờ thật (Lật quân úp)
      final revealedPiece = PieceDto(
        row: move.toX,
        col: move.toY,
        type: p.realType ?? p.type,
        color: p.color,
        isFaceDown: false,
        realType: p.realType ?? p.type,
      );
      s.coUpPieces['${move.toX},${move.toY}'] = revealedPiece;

      s.moves.add(
        MoveHistoryDto(
          id: s.moves.length + 1,
          moveOrder: s.moves.length,
          fromX: move.fromX,
          fromY: move.fromY,
          toX: move.toX,
          toY: move.toY,
          pieceType: revealedPiece.type,
          pieceColor: revealedPiece.color,
          movedAt: DateTime.now().toUtc(),
        ),
      );

      // Nếu bắt Tướng -> Ván cờ ngã ngũ
      if (captured != null && (captured.type.toLowerCase() == 'general' || captured.realType?.toLowerCase() == 'general')) {
        s.status = isRedTurn ? GameStatus.redWins : GameStatus.blackWins;
        s.endReason = 'GeneralCaptured';
      } else {
        s.board.switchTurn();
      }

      s.revision++;
      return _snapshot(s);
    }

    if (!s.isPassAndPlay && s.board.currentTurn != PieceColor.red) {
      throw Exception('Chưa đến lượt quân Đỏ.');
    }

    final piece = s.board.getPieceAt(move.fromX, move.fromY);
    if (piece == null || piece.color != s.board.currentTurn) {
      throw Exception('Hãy chọn quân đúng lượt của bạn.');
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
    if (s.isPassAndPlay ||
        s.status != GameStatus.inProgress ||
        s.board.currentTurn != PieceColor.black) {
      return _snapshot(s);
    }

    // AI cho ván Cờ Úp
    if (s.isCoUp) {
      final blackPieces = s.coUpPieces.values.where((p) => p.color.toLowerCase() == 'black').toList();
      for (final p in blackPieces) {
        final valids = _getCoUpValidMoves(s, p);
        if (valids.isNotEmpty) {
          final chosen = valids.firstWhere((m) => m.isCapture, orElse: () => valids.first);
          return moveAsync(
            id,
            MakeMoveRequestDto(
              fromX: p.row,
              fromY: p.col,
              toX: chosen.row,
              toY: chosen.col,
            ),
          );
        }
      }
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

  Future<({int fromRow, int fromCol, int toRow, int toCol, String explanation})?>
      getAiCoachHintAsync(int gameId) async {
    final s = _sessions[gameId];
    if (s == null) return null;

    final engine = MinimaxEngine(3);
    final move = engine.findBestMove(s.board, s.board.currentTurn, timeLimitMs: 900);
    if (move == null) return null;

    final piece = s.board.getPieceAt(move.from.row, move.from.col);
    final pieceType = piece?.type.label ?? 'Quân';
    final isCapture = s.board.getPieceAt(move.to.row, move.to.col) != null;

    String exp;
    if (isCapture) {
      exp = 'Đòn ăn quân chiến thuật sắc bén! Tiêu diệt hỏa lực của đối phương và mở rộng ưu thế.';
    } else if (piece?.type == PieceType.cannon) {
      exp = 'Điều $pieceType chiếm trung lộ hoặc trục sườn, tạo thế giáp công pháo đầu nguy hiểm.';
    } else if (piece?.type == PieceType.horse) {
      exp = 'Xuất $pieceType bàn hà, vừa che chắn trung lộ vừa tạo thế phản công biến hóa.';
    } else if (piece?.type == PieceType.rook) {
      exp = 'Xuất $pieceType kiểm soát lộ thông suốt, tạo áp lực khống chế toàn bộ cánh.';
    } else if (move.to.row == 4 || move.to.row == 5) {
      exp = 'Điều quân vượt sông áp sát phòng tuyến, chiếm điểm cao đe dọa trực tiếp đối thủ.';
    } else {
      exp = 'Nước đi củng cố trận hình vững chắc, giữ vững thế công thủ toàn diện.';
    }

    return (
      fromRow: move.from.row,
      fromCol: move.from.col,
      toRow: move.to.row,
      toCol: move.to.col,
      explanation: exp,
    );
  }

  Future<GameResponseDto> resignAsync(int id) async {
    final s = _getSession(id);
    if (s.status == GameStatus.inProgress) {
      s.status = s.board.currentTurn == PieceColor.red ? GameStatus.blackWins : GameStatus.redWins;
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
    if (s.isCoUp && s.coUpPieces.isNotEmpty) {
      pieces.addAll(s.coUpPieces.values);
    } else {
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
      gameMode: s.isCoUp ? 'CoUp' : (s.isPassAndPlay ? 'PassAndPlay' : 'Offline'),
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
