import 'dart:async';
import 'dart:isolate';
import '../board/board.dart';
import '../models/engine_move.dart';
import '../models/piece_color.dart';
import '../models/position.dart';
import '../search/minimax_engine.dart';

class AiSearchRequest {
  final String requestId;
  final String fen;
  final PieceColor computerColor;
  final int depth;
  final int timeoutMs;

  AiSearchRequest({
    required this.requestId,
    required this.fen,
    required this.computerColor,
    required this.depth,
    this.timeoutMs = 2000,
  });
}

class AiSearchResult {
  final String requestId;
  final Position? from;
  final Position? to;
  final int completedDepth;
  final double elapsedMs;

  AiSearchResult({
    required this.requestId,
    this.from,
    this.to,
    required this.completedDepth,
    required this.elapsedMs,
  });
}

class EngineWorker {
  Isolate? _isolate;
  SendPort? _sendPort;
  final _resultController = StreamController<AiSearchResult>.broadcast();
  Completer<void>? _readyCompleter;

  Stream<AiSearchResult> get onResult => _resultController.stream;

  Future<void> start() async {
    if (_isolate != null) return;
    _readyCompleter = Completer<void>();
    final receivePort = ReceivePort();

    _isolate = await Isolate.spawn(_workerEntry, receivePort.sendPort);

    receivePort.listen((message) {
      if (message is SendPort) {
        _sendPort = message;
        _readyCompleter?.complete();
      } else if (message is Map<String, dynamic>) {
        final result = AiSearchResult(
          requestId: message['requestId'],
          from: message['fromRow'] != null
              ? Position(message['fromRow'], message['fromCol'])
              : null,
          to: message['toRow'] != null
              ? Position(message['toRow'], message['toCol'])
              : null,
          completedDepth: message['depth'] ?? 0,
          elapsedMs: (message['elapsed'] as num?)?.toDouble() ?? 0.0,
        );
        _resultController.add(result);
      }
    });

    await _readyCompleter?.future;
  }

  void search(AiSearchRequest request) {
    if (_sendPort == null) return;
    _sendPort!.send({
      'requestId': request.requestId,
      'fen': request.fen,
      'color': request.computerColor.name,
      'depth': request.depth,
      'timeoutMs': request.timeoutMs,
    });
  }

  void stop() {
    _isolate?.kill(priority: Isolate.immediate);
    _isolate = null;
    _sendPort = null;
  }

  static void _workerEntry(SendPort sendPort) {
    final port = ReceivePort();
    sendPort.send(port.sendPort);

    port.listen((message) {
      if (message is Map<String, dynamic>) {
        final requestId = message['requestId'] as String;
        final fen = message['fen'] as String;
        final color = message['color'] == 'red' ? PieceColor.red : PieceColor.black;
        final depth = message['depth'] as int;
        final timeoutMs = message['timeoutMs'] as int;

        final board = Board.tryParseFen(fen) ?? Board();
        final engine = MinimaxEngine(depth: depth);
        final bestMove = engine.findBestMove(
          board,
          color,
          timeLimit: Duration(milliseconds: timeoutMs),
        );

        sendPort.send({
          'requestId': requestId,
          'fromRow': bestMove?.from.row,
          'fromCol': bestMove?.from.col,
          'toRow': bestMove?.to.row,
          'toCol': bestMove?.to.col,
          'depth': engine.lastStats.completedDepth,
          'elapsed': engine.lastStats.elapsedMilliseconds,
        });
      }
    });
  }
}
