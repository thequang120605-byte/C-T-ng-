import 'piece.dart';
import 'position.dart';

class EngineMove {
  final Position from;
  final Position to;
  final Piece? capturedPiece;

  const EngineMove({
    required this.from,
    required this.to,
    this.capturedPiece,
  });

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is EngineMove &&
          runtimeType == other.runtimeType &&
          from == other.from &&
          to == other.to;

  @override
  int get hashCode => Object.hash(from, to);

  @override
  String toString() => '$from -> $to';
}
