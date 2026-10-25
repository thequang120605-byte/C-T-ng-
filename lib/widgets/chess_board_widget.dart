import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';

class ChessBoardWidget extends StatefulWidget {
  final List<PieceDto> pieces;
  final int? selectedRow;
  final int? selectedCol;
  final List<ValidMoveDto> validMoves;
  final (int, int)? lastMoveFrom;
  final (int, int)? lastMoveTo;
  final bool isFlipped;
  final bool reduceMotion;
  final int gameId;
  final int moveCount;
  final String riverWatermarkText;
  final (int, int)? arrowFrom;
  final (int, int)? arrowTo;
  final Color? arrowColor;
  final Map<String, String>? pieceBadges; // key: "$row,$col", value: "??" or "?" or "⭐"
  final bool rotateBlackPieces;
  final void Function(int row, int col) onCellTapped;

  const ChessBoardWidget({
    super.key,
    required this.pieces,
    required this.selectedRow,
    required this.selectedCol,
    required this.validMoves,
    required this.lastMoveFrom,
    required this.lastMoveTo,
    required this.isFlipped,
    required this.reduceMotion,
    required this.gameId,
    required this.moveCount,
    this.riverWatermarkText = 'Cờ Chậm',
    this.arrowFrom,
    this.arrowTo,
    this.arrowColor,
    this.pieceBadges,
    this.rotateBlackPieces = false,
    required this.onCellTapped,
  });

  @override
  State<ChessBoardWidget> createState() => _ChessBoardWidgetState();
}

class _ChessBoardWidgetState extends State<ChessBoardWidget>
    with SingleTickerProviderStateMixin {
  late final AnimationController _animCtrl;
  late final Animation<double> _moveAnim;
  String _lastMoveKey = '';
  bool _wasFlipped = false;

  Offset? _touchStart;
  (int, int)? _touchStartCell;
  bool _dragged = false;
  Rect _lastBoardRect = Rect.zero;
  double _cellSize = 0;

  @override
  void initState() {
    super.initState();
    _animCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 160),
      value: 1.0,
    );
    _moveAnim = CurvedAnimation(parent: _animCtrl, curve: Curves.easeOutCubic);
  }

  @override
  void dispose() {
    _animCtrl.dispose();
    super.dispose();
  }

  @override
  void didUpdateWidget(covariant ChessBoardWidget oldWidget) {
    super.didUpdateWidget(oldWidget);
    final key =
        '${widget.gameId}:${widget.moveCount}:${widget.lastMoveFrom}:${widget.lastMoveTo}';
    bool animate = _lastMoveKey.isNotEmpty &&
        key != _lastMoveKey &&
        widget.lastMoveFrom != null &&
        widget.lastMoveTo != null &&
        _wasFlipped == widget.isFlipped &&
        !widget.reduceMotion &&
        _lastMoveKey.startsWith('${widget.gameId}:');

    _lastMoveKey = key;
    _wasFlipped = widget.isFlipped;

    if (animate) {
      _tryHaptic();
      _animCtrl.forward(from: 0.0);
    } else if (key != oldWidget.gameId.toString()) {
      _animCtrl.value = 1.0;
    }
  }

  void _tryHaptic() {
    if (!widget.reduceMotion) {
      HapticFeedback.lightImpact();
    }
  }

  (int, int)? _touchToBoardPos(Offset localPos) {
    if (_cellSize <= 0) return null;
    final relX = localPos.dx - _lastBoardRect.left;
    final relY = localPos.dy - _lastBoardRect.top;

    final visualCol = (relX / _cellSize).round();
    final visualRow = (relY / _cellSize).round();

    if (visualCol < 0 || visualCol > 8 || visualRow < 0 || visualRow > 9) {
      return null;
    }

    final row = widget.isFlipped ? 9 - visualRow : visualRow;
    final col = widget.isFlipped ? 8 - visualCol : visualCol;
    return (row, col);
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final w = constraints.maxWidth;
        final h = constraints.maxHeight;

        const marginUnits = 0.65;
        const totalHorizUnits = 8.0 + (2.0 * marginUnits);
        const totalVertUnits = 9.0 + (2.0 * marginUnits);

        _cellSize = math.min(w / totalHorizUnits, h / totalVertUnits);
        final boardW = _cellSize * 8.0;
        final boardH = _cellSize * 9.0;
        final boardMargin = _cellSize * marginUnits;

        final startX = (w - boardW) / 2.0;
        final startY = (h - boardH) / 2.0;

        _lastBoardRect = Rect.fromLTWH(
          startX - boardMargin,
          startY - boardMargin,
          boardW + 2 * boardMargin,
          boardH + 2 * boardMargin,
        );

        return GestureDetector(
          behavior: HitTestBehavior.opaque,
          onPanDown: (details) {
            _touchStart = details.localPosition;
            _touchStartCell = _touchToBoardPos(details.localPosition);
            _dragged = false;
          },
          onPanUpdate: (details) {
            if (_touchStart != null) {
              final dist = (details.localPosition - _touchStart!).distance;
              if (dist > 12) _dragged = true;
            }
          },
          onPanEnd: (details) {
            if (_touchStart == null) return;
            final endCell = _touchToBoardPos(details.localPosition);
            final startCell = _touchStartCell;
            _touchStart = null;
            _touchStartCell = null;

            if (_dragged &&
                startCell != null &&
                endCell != null &&
                startCell != endCell) {
              widget.onCellTapped(startCell.$1, startCell.$2);
              widget.onCellTapped(endCell.$1, endCell.$2);
              _tryHaptic();
              return;
            }

            final target = endCell ?? startCell;
            if (target != null) {
              widget.onCellTapped(target.$1, target.$2);
              _tryHaptic();
            }
          },
          onPanCancel: () {
            _touchStart = null;
            _touchStartCell = null;
          },
          child: AnimatedBuilder(
            animation: _moveAnim,
            builder: (context, _) {
              return CustomPaint(
                size: Size(w, h),
                painter: _ChessBoardPainter(
                  pieces: widget.pieces,
                  selectedRow: widget.selectedRow,
                  selectedCol: widget.selectedCol,
                  validMoves: widget.validMoves,
                  lastMoveFrom: widget.lastMoveFrom,
                  lastMoveTo: widget.lastMoveTo,
                  isFlipped: widget.isFlipped,
                  moveProgress: _moveAnim.value,
                  riverWatermarkText: widget.riverWatermarkText,
                  arrowFrom: widget.arrowFrom,
                  arrowTo: widget.arrowTo,
                  arrowColor: widget.arrowColor,
                  pieceBadges: widget.pieceBadges,
                  rotateBlackPieces: widget.rotateBlackPieces,
                ),
              );
            },
          ),
        );
      },
    );
  }
}

class _ChessBoardPainter extends CustomPainter {
  // Bảng màu gỗ 3D cao cấp (Master Wood Aesthetic)
  static const Color _woodLight = Color(0xFFF1D8B0);
  static const Color _woodBase = Color(0xFFE4C393);
  static const Color _woodDark = Color(0xFFC79E67);
  static const Color _woodBorderHighlight = Color(0xFFFFF0D4);
  static const Color _woodBorderShadow = Color(0xFF5A361A);
  static const Color _gridLineColor = Color(0xFF533319);
  static const Color _coordTextColor = Color(0xFF7A4B1A);
  static const Color _riverTextColor = Color(0xFF9E6E3D);

  // Quân cờ
  static const Color _pieceLight = Color(0xFFFFF7E6);
  static const Color _pieceBase = Color(0xFFF2E3C6);
  static const Color _pieceDark = Color(0xFFDAC098);
  static const Color _pieceBevelDark = Color(0xFFB59368);
  static const Color _redInk = Color(0xFFBA2213);
  static const Color _blackInk = Color(0xFF221F1F);

  // Hiệu ứng nước đi
  static const Color _goldGlow = Color(0xFFFFD54F);
  static const Color _validDotCol = Color(0xFF10B981);
  static const Color _captureRingCol = Color(0xFFE53935);

  final List<PieceDto> pieces;
  final int? selectedRow;
  final int? selectedCol;
  final List<ValidMoveDto> validMoves;
  final (int, int)? lastMoveFrom;
  final (int, int)? lastMoveTo;
  final bool isFlipped;
  final double moveProgress;
  final String riverWatermarkText;
  final (int, int)? arrowFrom;
  final (int, int)? arrowTo;
  final Color? arrowColor;
  final Map<String, String>? pieceBadges;
  final bool rotateBlackPieces;

  _ChessBoardPainter({
    required this.pieces,
    required this.selectedRow,
    required this.selectedCol,
    required this.validMoves,
    required this.lastMoveFrom,
    required this.lastMoveTo,
    required this.isFlipped,
    required this.moveProgress,
    required this.riverWatermarkText,
    this.arrowFrom,
    this.arrowTo,
    this.arrowColor,
    this.pieceBadges,
    this.rotateBlackPieces = false,
  });

  @override
  void paint(Canvas canvas, Size size) {
    if (size.width <= 0 || size.height <= 0) return;

    const marginUnits = 0.65;
    const totalHorizUnits = 8.0 + (2.0 * marginUnits);
    const totalVertUnits = 9.0 + (2.0 * marginUnits);

    final cellSize =
        math.min(size.width / totalHorizUnits, size.height / totalVertUnits);
    final boardWidth = cellSize * 8.0;
    final boardHeight = cellSize * 9.0;
    final boardMargin = cellSize * marginUnits;

    final startX = (size.width - boardWidth) / 2.0;
    final startY = (size.height - boardHeight) / 2.0;

    // 1. VẼ KHỐI GỖ 3D CỦA BÀN CỜ (Wood Block with Beveled Edge & Shadow)
    final boardFullRect = Rect.fromLTWH(
      startX - boardMargin,
      startY - boardMargin,
      boardWidth + 2 * boardMargin,
      boardHeight + 2 * boardMargin,
    );

    // Drop shadow đáy bàn cờ
    canvas.drawRRect(
      RRect.fromRectAndRadius(
        boardFullRect.translate(0, 7),
        const Radius.circular(14),
      ),
      Paint()
        ..color = Colors.black.withAlpha(160)
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 10),
    );

    // Khối gỗ chính với gradient vân gỗ
    final woodPaint = Paint()
      ..shader = const LinearGradient(
        begin: Alignment.topLeft,
        end: Alignment.bottomRight,
        colors: [_woodLight, _woodBase, _woodDark, _woodBase],
        stops: [0.0, 0.35, 0.75, 1.0],
      ).createShader(boardFullRect);
    canvas.drawRRect(
      RRect.fromRectAndRadius(boardFullRect, const Radius.circular(12)),
      woodPaint,
    );

    // Viền vát cạnh 3D (Bevel stroke)
    final bevelLight = Paint()
      ..color = _woodBorderHighlight.withAlpha(180)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2.0;
    canvas.drawRRect(
      RRect.fromRectAndRadius(boardFullRect, const Radius.circular(12)),
      bevelLight,
    );

    final bevelDark = Paint()
      ..color = _woodBorderShadow
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3.2;
    canvas.drawRRect(
      RRect.fromRectAndRadius(boardFullRect.deflate(1.5), const Radius.circular(11)),
      bevelDark,
    );

    // Rãnh chỉ bo viền trong (Double inner groove)
    final innerGrooveRect = RRect.fromRectAndRadius(
      Rect.fromLTWH(
        startX - cellSize * 0.22,
        startY - cellSize * 0.22,
        boardWidth + cellSize * 0.44,
        boardHeight + cellSize * 0.44,
      ),
      const Radius.circular(6),
    );
    canvas.drawRRect(
      innerGrooveRect,
      Paint()
        ..color = _woodBorderShadow.withAlpha(140)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 1.5,
    );

    // 2. VẼ TỌA ĐỘ SỐ 1 - 9 Ở CẢ HAI ĐẦU BÀN CỜ (Header & Footer Numbers)
    _drawCoordinateNumbers(canvas, startX, startY, cellSize, boardHeight);

    // 3. VẼ CÁC ĐƯỜNG LƯỚI BÀN CỜ (Grid lines)
    final gridPaint = Paint()
      ..color = _gridLineColor
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.8;

    // Đường ngang
    for (int r = 0; r < 10; r++) {
      final y = startY + r * cellSize;
      canvas.drawLine(
        Offset(startX, y),
        Offset(startX + boardWidth, y),
        gridPaint,
      );
    }

    // Hai biên dọc ngoài cùng (liên tục từ r=0 đến r=9)
    canvas.drawLine(
      Offset(startX, startY),
      Offset(startX, startY + boardHeight),
      gridPaint,
    );
    canvas.drawLine(
      Offset(startX + boardWidth, startY),
      Offset(startX + boardWidth, startY + boardHeight),
      gridPaint,
    );

    // 7 đường dọc bên trong (bị ngắt quãng tại Sông)
    for (int c = 1; c < 8; c++) {
      final x = startX + c * cellSize;
      // Nửa trên
      canvas.drawLine(
        Offset(x, startY),
        Offset(x, startY + 4 * cellSize),
        gridPaint,
      );
      // Nửa dưới
      canvas.drawLine(
        Offset(x, startY + 5 * cellSize),
        Offset(x, startY + boardHeight),
        gridPaint,
      );
    }

    // 4. VẼ CỬU CUNG (Palace diagonals)
    // Cung trên
    canvas.drawLine(
      Offset(startX + 3 * cellSize, startY),
      Offset(startX + 5 * cellSize, startY + 2 * cellSize),
      gridPaint,
    );
    canvas.drawLine(
      Offset(startX + 5 * cellSize, startY),
      Offset(startX + 3 * cellSize, startY + 2 * cellSize),
      gridPaint,
    );
    // Cung dưới
    canvas.drawLine(
      Offset(startX + 3 * cellSize, startY + 7 * cellSize),
      Offset(startX + 5 * cellSize, startY + 9 * cellSize),
      gridPaint,
    );
    canvas.drawLine(
      Offset(startX + 5 * cellSize, startY + 7 * cellSize),
      Offset(startX + 3 * cellSize, startY + 9 * cellSize),
      gridPaint,
    );

    // 5. VẼ CÁC ĐIỂM THẬP TỰ ĐỊNH VỊ (Crosshairs cho Pháo và Tốt)
    _drawAllCrosshairs(canvas, startX, startY, cellSize);

    // 6. VẼ CHỮ THƯ PHÁP MỜ GIỮA SÔNG (River Watermark Text)
    final riverCenterY = startY + 4.5 * cellSize;
    _drawRiverWatermark(canvas, startX, riverCenterY, cellSize, boardWidth);

    // 7. VẼ ĐÁNH DẤU NƯỚC VỪA ĐI (Last Move Indicators)
    if (lastMoveFrom != null) {
      final fromPt = _getScreenCoord(
        lastMoveFrom!.$1,
        lastMoveFrom!.$2,
        startX,
        startY,
        cellSize,
      );
      // Chấm định vị tại ô xuất phát
      canvas.drawCircle(
        fromPt,
        cellSize * 0.16,
        Paint()
          ..color = const Color(0xFFBA2213).withAlpha(180)
          ..style = PaintingStyle.stroke
          ..strokeWidth = 2.5,
      );
      canvas.drawCircle(
        fromPt,
        cellSize * 0.06,
        Paint()..color = const Color(0xFFBA2213),
      );
    }

    // 8. VẼ CÁC QUÂN CỜ 3D NỔI BẬT (Master 3D Wood Pieces)
    for (final piece in pieces) {
      var pt = _getScreenCoord(piece.row, piece.col, startX, startY, cellSize);

      // Nếu đang chạy hoạt ảnh di chuyển
      if (moveProgress < 1.0 &&
          lastMoveFrom != null &&
          lastMoveTo == (piece.row, piece.col)) {
        final from = _getScreenCoord(
          lastMoveFrom!.$1,
          lastMoveFrom!.$2,
          startX,
          startY,
          cellSize,
        );
        pt = Offset(
          from.dx + (pt.dx - from.dx) * moveProgress,
          from.dy + (pt.dy - from.dy) * moveProgress,
        );
      }

      final isLastMoved = lastMoveTo == (piece.row, piece.col);
      final isSelected = selectedRow == piece.row && selectedCol == piece.col;

      _draw3DPiece(
        canvas: canvas,
        center: pt,
        piece: piece,
        radius: cellSize * 0.44,
        isLastMoved: isLastMoved,
        isSelected: isSelected,
      );

      // Vẽ Badge cảnh báo (ví dụ "??" hoặc "⭐") nếu có
      final badgeKey = '${piece.row},${piece.col}';
      if (pieceBadges != null && pieceBadges!.containsKey(badgeKey)) {
        _drawPieceBadge(
          canvas,
          pt,
          cellSize * 0.44,
          pieceBadges![badgeKey]!,
        );
      }
    }

    // 9. VẼ MŨI TÊN CHIẾN THUẬT (Tactical Arrows cho Phân tích / Gợi ý)
    if (arrowFrom != null && arrowTo != null) {
      final p1 = _getScreenCoord(
        arrowFrom!.$1,
        arrowFrom!.$2,
        startX,
        startY,
        cellSize,
      );
      final p2 = _getScreenCoord(
        arrowTo!.$1,
        arrowTo!.$2,
        startX,
        startY,
        cellSize,
      );
      _drawTacticalArrow(
        canvas,
        p1,
        p2,
        arrowColor ?? const Color(0xFF00E676),
        cellSize * 0.44,
      );
    }

    // 10. VẼ CÁC NƯỚC ĐI HỢP LỆ (Valid Move Dots / Capture Rings)
    for (final vm in validMoves) {
      final pt = _getScreenCoord(vm.row, vm.col, startX, startY, cellSize);
      if (vm.isCapture) {
        // Vòng cung bắt quân đỏ
        canvas.drawCircle(
          pt,
          cellSize * 0.45,
          Paint()
            ..color = _captureRingCol
            ..style = PaintingStyle.stroke
            ..strokeWidth = 3.2,
        );
      } else {
        // Chấm ngọc phát sáng mờ
        canvas.drawCircle(
          pt,
          cellSize * 0.16,
          Paint()..color = _validDotCol.withAlpha(220),
        );
        canvas.drawCircle(
          pt,
          cellSize * 0.08,
          Paint()..color = Colors.white,
        );
      }
    }
  }

  // Vẽ các chữ số tọa độ 1 - 9
  void _drawCoordinateNumbers(
    Canvas canvas,
    double startX,
    double startY,
    double cellSize,
    double boardHeight,
  ) {
    for (int c = 0; c < 9; c++) {
      final x = startX + c * cellSize;

      // Hàng trên cùng: 1 -> 9 (hoặc 9 -> 1 nếu flipped)
      final topNumber = isFlipped ? (9 - c).toString() : (c + 1).toString();
      _drawCenteredText(
        canvas,
        topNumber,
        Offset(x, startY - cellSize * 0.38),
        GoogleFonts.cinzel(
          fontSize: cellSize * 0.30,
          fontWeight: FontWeight.bold,
          color: _coordTextColor,
        ),
      );

      // Hàng dưới cùng: 9 -> 1 (hoặc 1 -> 9 nếu flipped)
      final bottomNumber = isFlipped ? (c + 1).toString() : (9 - c).toString();
      _drawCenteredText(
        canvas,
        bottomNumber,
        Offset(x, startY + boardHeight + cellSize * 0.38),
        GoogleFonts.cinzel(
          fontSize: cellSize * 0.30,
          fontWeight: FontWeight.bold,
          color: _coordTextColor,
        ),
      );
    }
  }

  // Vẽ chữ mờ ở giữa sông (River Watermark)
  void _drawRiverWatermark(
    Canvas canvas,
    double startX,
    double riverCenterY,
    double cellSize,
    double boardWidth,
  ) {
    final riverText = riverWatermarkText.isNotEmpty
        ? riverWatermarkText
        : (isFlipped ? 'HÁN GIỚI • SỞ HÀ' : 'SỞ HÀ • HÁN GIỚI');

    _drawCenteredText(
      canvas,
      riverText,
      Offset(startX + boardWidth / 2, riverCenterY),
      GoogleFonts.maShanZheng(
        fontSize: cellSize * 0.58,
        color: _riverTextColor.withAlpha(160),
        fontWeight: FontWeight.bold,
        letterSpacing: 4.0,
      ),
    );
  }

  // Vẽ Quân Cờ 3D Tiện Gỗ Khắc Chữ (Master 3D Piece)
  void _draw3DPiece({
    required Canvas canvas,
    required Offset center,
    required PieceDto piece,
    required double radius,
    required bool isLastMoved,
    required bool isSelected,
  }) {
    final isRed = piece.color.toLowerCase() == 'red';
    final ink = isRed ? _redInk : _blackInk;

    // 1. Hiệu ứng Hào quang Vàng Kim khi vừa đi hoặc đang chọn
    if (isLastMoved || isSelected) {
      canvas.drawCircle(
        center,
        radius + 4,
        Paint()
          ..color = _goldGlow.withAlpha(180)
          ..style = PaintingStyle.stroke
          ..strokeWidth = 3.5
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 3),
      );
    }

    // 2. Drop shadow khối quân cờ
    canvas.drawCircle(
      center.translate(1.5, 3.2),
      radius,
      Paint()
        ..color = Colors.black.withAlpha(90)
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 2.5),
    );

    // 3. Khối gỗ thân quân cờ (Radial lighting gradient)
    final piecePaint = Paint()
      ..shader = RadialGradient(
        center: const Alignment(-0.3, -0.4),
        radius: 0.9,
        colors: [_pieceLight, _pieceBase, _pieceDark],
        stops: const [0.0, 0.65, 1.0],
      ).createShader(Rect.fromCircle(center: center, radius: radius));
    canvas.drawCircle(center, radius, piecePaint);

    // 4. Viền Bevel ngoài cùng (Outer rim)
    final rimPaint = Paint()
      ..color = _pieceBevelDark
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.6;
    canvas.drawCircle(center, radius, rimPaint);

    // 5. Rãnh chạm khắc trong (Inner engraved ring)
    final innerRingPaint = Paint()
      ..color = ink.withAlpha(120)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.3;
    canvas.drawCircle(center, radius - 3.8, innerRingPaint);

    // 6. Ký hiệu chữ Hán Thư Pháp Khắc Chìm hoặc Quân Úp Bí Ẩn
    if (piece.isFaceDown) {
      final bronzeBorder = Paint()
        ..color = const Color(0xFFC5A059)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2.0;
      canvas.drawCircle(center, radius - 6, bronzeBorder);

      final bronzeInner = Paint()
        ..color = const Color(0xFF8D6E63).withAlpha(140)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 1.0;
      canvas.drawCircle(center, radius - 9, bronzeInner);

      _drawCenteredText(
        canvas,
        '?',
        center.translate(0, -0.5),
        GoogleFonts.cinzel(
          color: const Color(0xFFFFD54F),
          fontSize: radius * 1.1,
          fontWeight: FontWeight.bold,
          height: 1.0,
          shadows: [
            Shadow(
              color: Colors.black.withAlpha(200),
              offset: const Offset(1, 1),
              blurRadius: 2,
            ),
          ],
        ),
      );
      return;
    }

    final shouldRotate = rotateBlackPieces && !isRed;
    if (shouldRotate) {
      canvas.save();
      canvas.translate(center.dx, center.dy);
      canvas.rotate(math.pi);
      canvas.translate(-center.dx, -center.dy);
    }

    final label = _getPieceLabel(piece.type, isRed);
    _drawCenteredText(
      canvas,
      label,
      center.translate(0, -0.5),
      GoogleFonts.maShanZheng(
        color: ink,
        fontSize: radius * 1.05,
        fontWeight: FontWeight.bold,
        height: 1.0,
        shadows: [
          Shadow(
            color: Colors.white.withAlpha(120),
            offset: const Offset(0.6, 0.6),
            blurRadius: 1,
          ),
          Shadow(
            color: ink.withAlpha(60),
            offset: const Offset(-0.5, -0.5),
            blurRadius: 1,
          ),
        ],
      ),
    );

    if (shouldRotate) {
      canvas.restore();
    }
  }

  // Vẽ Badge trên quân cờ (??, ?, ⭐)
  void _drawPieceBadge(
    Canvas canvas,
    Offset center,
    double pieceRadius,
    String badgeText,
  ) {
    final badgeCenter = Offset(
      center.dx + pieceRadius * 0.72,
      center.dy - pieceRadius * 0.72,
    );
    const badgeR = 10.5;

    final isGreat = badgeText == '⭐';
    final isBlunder = badgeText.contains('?');

    final bgPaint = Paint()
      ..color = isGreat
          ? const Color(0xFF2E7D32)
          : isBlunder
              ? const Color(0xFFD32F2F)
              : const Color(0xFFF57C00);

    // Shadow
    canvas.drawCircle(
      badgeCenter.translate(0, 1.5),
      badgeR,
      Paint()..color = Colors.black45,
    );
    // Circle
    canvas.drawCircle(badgeCenter, badgeR, bgPaint);
    canvas.drawCircle(
      badgeCenter,
      badgeR,
      Paint()
        ..color = Colors.white
        ..style = PaintingStyle.stroke
        ..strokeWidth = 1.2,
    );

    // Text
    _drawCenteredText(
      canvas,
      badgeText,
      badgeCenter,
      GoogleFonts.philosopher(
        fontSize: isGreat ? 11 : 10,
        fontWeight: FontWeight.bold,
        color: Colors.white,
      ),
    );
  }

  // Vẽ Mũi tên chiến thuật (Tactical Arrow)
  void _drawTacticalArrow(
    Canvas canvas,
    Offset from,
    Offset to,
    Color color,
    double pieceRadius,
  ) {
    final angle = math.atan2(to.dy - from.dy, to.dx - from.dx);
    final arrowPaint = Paint()
      ..color = color
      ..strokeWidth = 4.0
      ..strokeCap = StrokeCap.round
      ..style = PaintingStyle.stroke;

    final glowPaint = Paint()
      ..color = color.withAlpha(120)
      ..strokeWidth = 8.0
      ..strokeCap = StrokeCap.round
      ..style = PaintingStyle.stroke
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 3);

    // Rút ngắn 2 đầu để không đè xuyên tâm quân cờ
    final startPt = Offset(
      from.dx + pieceRadius * 0.5 * math.cos(angle),
      from.dy + pieceRadius * 0.5 * math.sin(angle),
    );
    final endPt = Offset(
      to.dx - pieceRadius * 0.5 * math.cos(angle),
      to.dy - pieceRadius * 0.5 * math.sin(angle),
    );

    // Vẽ thân mũi tên
    canvas.drawLine(startPt, endPt, glowPaint);
    canvas.drawLine(startPt, endPt, arrowPaint);

    // Vẽ đầu mũi tên hình tam giác
    const arrowHeadLen = 14.0;
    const arrowHeadAngle = 0.5;

    final path = Path()
      ..moveTo(endPt.dx, endPt.dy)
      ..lineTo(
        endPt.dx - arrowHeadLen * math.cos(angle - arrowHeadAngle),
        endPt.dy - arrowHeadLen * math.sin(angle - arrowHeadAngle),
      )
      ..lineTo(
        endPt.dx - arrowHeadLen * math.cos(angle + arrowHeadAngle),
        endPt.dy - arrowHeadLen * math.sin(angle + arrowHeadAngle),
      )
      ..close();

    final fillPaint = Paint()
      ..color = color
      ..style = PaintingStyle.fill;
    canvas.drawPath(path, fillPaint);
  }

  void _drawAllCrosshairs(
      Canvas canvas, double startX, double startY, double cellSize) {
    final crossPaint = Paint()
      ..color = _gridLineColor.withAlpha(180)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.3;

    _drawSingleCross(
        canvas, crossPaint, 2, 1, startX, startY, cellSize, true, true);
    _drawSingleCross(
        canvas, crossPaint, 2, 7, startX, startY, cellSize, true, true);
    _drawSingleCross(
        canvas, crossPaint, 7, 1, startX, startY, cellSize, true, true);
    _drawSingleCross(
        canvas, crossPaint, 7, 7, startX, startY, cellSize, true, true);

    for (int col = 0; col <= 8; col += 2) {
      _drawSingleCross(canvas, crossPaint, 3, col, startX, startY, cellSize,
          col > 0, col < 8);
      _drawSingleCross(canvas, crossPaint, 6, col, startX, startY, cellSize,
          col > 0, col < 8);
    }
  }

  void _drawSingleCross(
    Canvas canvas,
    Paint paint,
    int row,
    int col,
    double startX,
    double startY,
    double cellSize,
    bool hasLeft,
    bool hasRight,
  ) {
    final pt = _getScreenCoord(row, col, startX, startY, cellSize);
    final gap = cellSize * 0.08;
    final len = cellSize * 0.16;

    if (hasLeft) {
      canvas.drawLine(
        Offset(pt.dx - gap - len, pt.dy - gap),
        Offset(pt.dx - gap, pt.dy - gap),
        paint,
      );
      canvas.drawLine(
        Offset(pt.dx - gap, pt.dy - gap - len),
        Offset(pt.dx - gap, pt.dy - gap),
        paint,
      );
      canvas.drawLine(
        Offset(pt.dx - gap - len, pt.dy + gap),
        Offset(pt.dx - gap, pt.dy + gap),
        paint,
      );
      canvas.drawLine(
        Offset(pt.dx - gap, pt.dy + gap + len),
        Offset(pt.dx - gap, pt.dy + gap),
        paint,
      );
    }
    if (hasRight) {
      canvas.drawLine(
        Offset(pt.dx + gap, pt.dy - gap),
        Offset(pt.dx + gap + len, pt.dy - gap),
        paint,
      );
      canvas.drawLine(
        Offset(pt.dx + gap, pt.dy - gap - len),
        Offset(pt.dx + gap, pt.dy - gap),
        paint,
      );
      canvas.drawLine(
        Offset(pt.dx + gap, pt.dy + gap),
        Offset(pt.dx + gap + len, pt.dy + gap),
        paint,
      );
      canvas.drawLine(
        Offset(pt.dx + gap, pt.dy + gap + len),
        Offset(pt.dx + gap, pt.dy + gap),
        paint,
      );
    }
  }

  static String _getPieceLabel(String type, bool isRed) {
    switch (type.toLowerCase()) {
      case 'king':
        return isRed ? '帥' : '將';
      case 'advisor':
        return isRed ? '仕' : '士';
      case 'elephant':
        return isRed ? '相' : '象';
      case 'horse':
        return isRed ? '傌' : '馬';
      case 'rook':
        return isRed ? '俥' : '車';
      case 'cannon':
        return isRed ? '炮' : '砲';
      case 'pawn':
        return isRed ? '兵' : '卒';
      default:
        return '?';
    }
  }

  Offset _getScreenCoord(
      int row, int col, double startX, double startY, double cellSize) {
    final visualRow = isFlipped ? 9 - row : row;
    final visualCol = isFlipped ? 8 - col : col;
    return Offset(
      startX + visualCol * cellSize,
      startY + visualRow * cellSize,
    );
  }

  static void _drawCenteredText(
    Canvas canvas,
    String text,
    Offset center,
    TextStyle style,
  ) {
    final tp = TextPainter(
      text: TextSpan(text: text, style: style),
      textDirection: TextDirection.ltr,
      textAlign: TextAlign.center,
    )..layout();
    tp.paint(
      canvas,
      Offset(center.dx - tp.width / 2.0, center.dy - tp.height / 2.0),
    );
  }

  @override
  bool shouldRepaint(covariant _ChessBoardPainter oldDelegate) {
    return oldDelegate.pieces != pieces ||
        oldDelegate.selectedRow != selectedRow ||
        oldDelegate.selectedCol != selectedCol ||
        oldDelegate.validMoves != validMoves ||
        oldDelegate.lastMoveFrom != lastMoveFrom ||
        oldDelegate.lastMoveTo != lastMoveTo ||
        oldDelegate.isFlipped != isFlipped ||
        oldDelegate.moveProgress != moveProgress ||
        oldDelegate.riverWatermarkText != riverWatermarkText ||
        oldDelegate.arrowFrom != arrowFrom ||
        oldDelegate.arrowTo != arrowTo ||
        oldDelegate.arrowColor != arrowColor ||
        oldDelegate.pieceBadges != pieceBadges ||
        oldDelegate.rotateBlackPieces != rotateBlackPieces;
  }
}
