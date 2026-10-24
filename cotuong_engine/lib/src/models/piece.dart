import 'piece_color.dart';
import 'piece_type.dart';

class Piece {
  final PieceType type;
  final PieceColor color;

  const Piece(this.type, this.color);

  Piece clone() => Piece(type, color);

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is Piece &&
          runtimeType == other.runtimeType &&
          type == other.type &&
          color == other.color;

  @override
  int get hashCode => Object.hash(type, color);

  @override
  String toString() => '${color.name}_${type.name}';
}
