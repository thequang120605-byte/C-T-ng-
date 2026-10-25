import 'package:flutter_test/flutter_test.dart';
import 'package:chesscore_flutter/offline/board.dart';
import 'package:chesscore_flutter/offline/board_evaluator.dart';
import 'package:chesscore_flutter/offline/game_rules.dart';
import 'package:chesscore_flutter/offline/minimax_engine.dart';
import 'package:chesscore_flutter/offline/piece.dart';

void main() {
  test('Board initializes with 32 pieces and Red turn', () {
    final board = Board();
    expect(board.currentTurn, PieceColor.red);

    int count = 0;
    for (int r = 0; r < 10; r++) {
      for (int c = 0; c < 9; c++) {
        if (board.getPieceAt(r, c) != null) count++;
      }
    }
    expect(count, 32);
  });

  test('MoveGenerator finds initial legal moves for Red', () {
    final board = Board();
    final moves = GameRules.getAllLegalMoves(board, PieceColor.red);
    expect(moves.isNotEmpty, true);
    expect(moves.length, 44); // Standard opening legal moves for Red
  });

  test('FEN parser round-trips start fen', () {
    final board = Board();
    final fen = board.toFen();
    expect(fen, Board.startFen);

    final parsed = Board.fromFen(fen);
    expect(parsed.toFen(), Board.startFen);
  });

  test('MinimaxEngine finds a legal best move for Black', () {
    final board = Board();
    // Simulate Red opening: Cannon 2 to center (7, 1) -> (7, 4)
    board.makeMove(const EngineMove(Position(7, 1), Position(7, 4)));
    expect(board.currentTurn, PieceColor.black);

    final engine = MinimaxEngine(1);
    final best = engine.findBestMove(board, PieceColor.black, timeLimitMs: 500);
    expect(best != null, true);
    expect(best!.from.row >= 0 && best.from.row <= 9, true);
  });

  test('BoardEvaluator computes H1-H4 side scores', () {
    final board = Board();
    final redScores = BoardEvaluator.getSideScores(board, PieceColor.red);
    final blackScores = BoardEvaluator.getSideScores(board, PieceColor.black);

    expect(redScores.h1 > 0, true);
    expect(blackScores.h1 > 0, true);
    expect(redScores.h1, blackScores.h1); // Symmetric at start
  });
}
