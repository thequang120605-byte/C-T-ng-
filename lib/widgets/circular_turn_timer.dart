import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class CircularTurnTimer extends StatefulWidget {
  final bool isMyTurn;
  final int totalSeconds;
  final int remainingSeconds;
  final String playerName;
  final String rankTitle;
  final String? avatarLetter;
  final bool isOpponent;
  final VoidCallback? onTap;

  const CircularTurnTimer({
    super.key,
    required this.isMyTurn,
    this.totalSeconds = 600,
    required this.remainingSeconds,
    required this.playerName,
    this.rankTitle = 'Tập sự',
    this.avatarLetter,
    this.isOpponent = false,
    this.onTap,
  });

  @override
  State<CircularTurnTimer> createState() => _CircularTurnTimerState();
}

class _CircularTurnTimerState extends State<CircularTurnTimer>
    with SingleTickerProviderStateMixin {
  late final AnimationController _pulseCtrl;

  @override
  void initState() {
    super.initState();
    _pulseCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1000),
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _pulseCtrl.dispose();
    super.dispose();
  }

  String _formatTime(int sec) {
    final m = (sec ~/ 60).toString().padLeft(2, '0');
    final s = (sec % 60).toString().padLeft(2, '0');
    return '$m:$s';
  }

  @override
  Widget build(BuildContext context) {
    final progress = widget.totalSeconds > 0
        ? (widget.remainingSeconds / widget.totalSeconds).clamp(0.0, 1.0)
        : 1.0;

    return GestureDetector(
      onTap: widget.onTap,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Vòng tròn Avatar kèm Timer
          Stack(
            alignment: Alignment.center,
            children: [
              // Custom paint vòng tròn đếm ngược
              SizedBox(
                width: 68,
                height: 68,
                child: AnimatedBuilder(
                  animation: _pulseCtrl,
                  builder: (context, _) {
                    return CustomPaint(
                      painter: _CircularTimerPainter(
                        progress: progress,
                        isActive: widget.isMyTurn,
                        isOpponent: widget.isOpponent,
                        pulseValue: _pulseCtrl.value,
                      ),
                    );
                  },
                ),
              ),

              // Avatar bên trong
              Container(
                width: 54,
                height: 54,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: widget.isOpponent
                        ? const [Color(0xFF2C3E50), Color(0xFF1A252F)]
                        : const [Color(0xFF135E4E), Color(0xFF0B3C32)],
                  ),
                  boxShadow: const [
                    BoxShadow(
                      color: Colors.black45,
                      blurRadius: 4,
                      offset: Offset(0, 2),
                    ),
                  ],
                ),
                child: Center(
                  child: widget.isOpponent
                      ? const Icon(
                          Icons.pets,
                          color: Color(0xFF81D4FA),
                          size: 26,
                        )
                      : Text(
                          widget.avatarLetter ?? 'Q',
                          style: GoogleFonts.philosopher(
                            fontSize: 26,
                            fontWeight: FontWeight.bold,
                            color: const Color(0xFFE0F2F1),
                          ),
                        ),
                ),
              ),

              // Badge Cấp bậc "Tập sự ⭐" dán phía dưới Avatar
              Positioned(
                bottom: 0,
                child: Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 6, vertical: 1.5),
                  decoration: BoxDecoration(
                    gradient: const LinearGradient(
                      colors: [Color(0xFF8D6E63), Color(0xFF5D4037)],
                    ),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(
                      color: const Color(0xFFFFD54F),
                      width: 1,
                    ),
                    boxShadow: const [
                      BoxShadow(
                        color: Colors.black45,
                        blurRadius: 3,
                        offset: Offset(0, 1),
                      ),
                    ],
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        widget.rankTitle,
                        style: GoogleFonts.philosopher(
                          fontSize: 9,
                          fontWeight: FontWeight.bold,
                          color: const Color(0xFFFFECB3),
                        ),
                      ),
                      const SizedBox(width: 2),
                      const Icon(
                        Icons.star,
                        size: 9,
                        color: Color(0xFFFFD54F),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),

          const SizedBox(height: 3),

          // Tên người chơi
          SizedBox(
            width: 90,
            child: Text(
              widget.playerName,
              textAlign: TextAlign.center,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: GoogleFonts.philosopher(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: widget.isMyTurn
                    ? const Color(0xFFFFD54F)
                    : const Color(0xFFCFD8DC),
              ),
            ),
          ),

          // Thời gian
          Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                Icons.history_toggle_off,
                size: 13,
                color: widget.isMyTurn
                    ? const Color(0xFFFFD54F)
                    : const Color(0xFF90A4AE),
              ),
              const SizedBox(width: 3),
              Text(
                _formatTime(widget.remainingSeconds),
                style: GoogleFonts.cinzel(
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                  color: widget.isMyTurn
                      ? const Color(0xFFFFD54F)
                      : const Color(0xFFCFD8DC),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _CircularTimerPainter extends CustomPainter {
  final double progress; // 0.0 -> 1.0
  final bool isActive;
  final bool isOpponent;
  final double pulseValue;

  _CircularTimerPainter({
    required this.progress,
    required this.isActive,
    required this.isOpponent,
    required this.pulseValue,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = (size.width - 6) / 2;

    // 1. Vòng nền xám/tối
    final bgPaint = Paint()
      ..color = Colors.black45
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3.5;
    canvas.drawCircle(center, radius, bgPaint);

    if (!isActive) {
      // Vòng tĩnh mờ nhạt khi chưa đến lượt
      final idlePaint = Paint()
        ..color = const Color(0xFF455A64)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2.5;
      canvas.drawCircle(center, radius, idlePaint);
      return;
    }

    // 2. Vòng phát sáng đếm ngược khi ĐANG ĐẾN LƯỢT
    final sweepAngle = 2 * math.pi * progress;
    final startAngle = -math.pi / 2;

    final baseColor = isOpponent
        ? const Color(0xFF03A9F4)
        : const Color(0xFF00E676);

    // Glow effect
    final glowPaint = Paint()
      ..color = baseColor.withAlpha((100 + pulseValue * 100).toInt())
      ..style = PaintingStyle.stroke
      ..strokeWidth = 6.0
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 4);
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      startAngle,
      sweepAngle,
      false,
      glowPaint,
    );

    // Arc chính
    final activePaint = Paint()
      ..color = baseColor
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round
      ..strokeWidth = 4.0;
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      startAngle,
      sweepAngle,
      false,
      activePaint,
    );

    // 3. Đốm sáng rực rỡ tại đầu mút vòng xoay (Comet Head)
    final headAngle = startAngle + sweepAngle;
    final headX = center.dx + radius * math.cos(headAngle);
    final headY = center.dy + radius * math.sin(headAngle);

    final cometGlow = Paint()
      ..color = Colors.white
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 3);
    canvas.drawCircle(Offset(headX, headY), 3.5 + pulseValue * 1.5, cometGlow);

    final cometCore = Paint()..color = const Color(0xFFFFEB3B);
    canvas.drawCircle(Offset(headX, headY), 2.5, cometCore);
  }

  @override
  bool shouldRepaint(covariant _CircularTimerPainter oldDelegate) {
    return oldDelegate.progress != progress ||
        oldDelegate.isActive != isActive ||
        oldDelegate.pulseValue != pulseValue;
  }
}
