import 'dart:convert';
import 'dart:io';
import 'package:cotuong_engine/cotuong_engine.dart';
import 'package:test/test.dart';

void main() {
  group('Engine Parity C# vs Dart Tests', () {
    late Map<String, dynamic> suite;

    setUpAll(() {
      final file = File('../Checks/ground_truth_suite.json');
      if (!file.existsSync()) {
        fail('Cannot find Checks/ground_truth_suite.json');
      }
      suite = jsonDecode(file.readAsStringSync());
    });

    test('Verify all 5 Ground Truth positions match C# outputs exactly', () {
      final positions = suite['Positions'] as List;

      for (final pos in positions) {
        final name = pos['Name'] as String;
        final fen = pos['Fen'] as String;
        final expectedRedMoves = pos['RedLegalMovesCount'] as int;
        final expectedBlackMoves = pos['BlackLegalMovesCount'] as int;
        final expectedRedH1 = pos['RedH1'] as int;
        final expectedRedH2 = pos['RedH2'] as int;
        final expectedRedH3 = pos['RedH3'] as int;
        final expectedRedH4 = pos['RedH4'] as int;
        final expectedRedTotal = pos['RedEvalTotal'] as int;
        final expectedRedCheck = pos['RedInCheck'] as bool;
        final expectedBlackCheck = pos['BlackInCheck'] as bool;
        final expectedZobrist = (pos['ZobristHex'] as String).toUpperCase();

        final board = Board.tryParseFen(fen);
        expect(board, isNotNull, reason: 'Failed to parse FEN for $name');

        // Check legal moves
        final redMoves = GameRules.getAllLegalMoves(board!, PieceColor.red);
        final blackMoves = GameRules.getAllLegalMoves(board, PieceColor.black);
        expect(redMoves.length, equals(expectedRedMoves),
            reason: '$name: Red legal moves mismatch');
        expect(blackMoves.length, equals(expectedBlackMoves),
            reason: '$name: Black legal moves mismatch');

        // Check Evaluation H1, H2, H3, H4
        final redScores = BoardEvaluator.getSideScores(board, PieceColor.red);
        expect(redScores.h1, equals(expectedRedH1),
            reason: '$name: Red H1 mismatch');
        expect(redScores.h2, equals(expectedRedH2),
            reason: '$name: Red H2 mismatch');
        expect(redScores.h3, equals(expectedRedH3),
            reason: '$name: Red H3 mismatch');
        expect(redScores.h4, equals(expectedRedH4),
            reason: '$name: Red H4 mismatch');
        expect(redScores.total, equals(expectedRedTotal),
            reason: '$name: Red Total mismatch');

        // Check Check-state
        expect(MoveGenerator.checkIfInCheck(board, PieceColor.red),
            equals(expectedRedCheck),
            reason: '$name: Red in-check mismatch');
        expect(MoveGenerator.checkIfInCheck(board, PieceColor.black),
            equals(expectedBlackCheck),
            reason: '$name: Black in-check mismatch');

        // Check Zobrist hash
        final hashBigInt = ZobristHashing.computeHash(board);
        final hashHex = hashBigInt.toRadixString(16).toUpperCase().padLeft(16, '0');
        expect(hashHex, equals(expectedZobrist),
            reason: '$name: Zobrist hash mismatch');

        print('PASS Parity: $name (FEN: $fen) -> $expectedRedMoves Red moves, $expectedBlackMoves Black moves, Hash: $hashHex');
      }
    });

    test('Minimax search generates valid move within budget', () {
      final board = Board();
      final engine = MinimaxEngine(depth: 2);
      final move = engine.findBestMove(
        board,
        PieceColor.red,
        timeLimit: const Duration(milliseconds: 500),
      );
      expect(move, isNotNull);
      final legalMoves = GameRules.getAllLegalMoves(board, PieceColor.red);
      expect(legalMoves.any((m) => m.from == move!.from && m.to == move.to), isTrue);
      print('PASS Minimax best move: ${move!.from} -> ${move.to}');
    });
  });
}
