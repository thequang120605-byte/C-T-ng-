import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';

class AdvantageBarWidget extends StatefulWidget {
  /// Điểm đánh giá thế trận (theo góc nhìn Red):
  /// > 0 là Đỏ ưu, < 0 là Đen ưu, = 0 là Cân bằng.
  final int evaluationScore;
  final VoidCallback? onHelpTapped;

  const AdvantageBarWidget({
    super.key,
    required this.evaluationScore,
    this.onHelpTapped,
  });

  @override
  State<AdvantageBarWidget> createState() => _AdvantageBarWidgetState();
}

class _AdvantageBarWidgetState extends State<AdvantageBarWidget>
    with SingleTickerProviderStateMixin {
  late final AnimationController _flameCtrl;

  @override
  void initState() {
    super.initState();
    _flameCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1400),
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _flameCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    // Chuẩn hóa điểm ưu thế: thang đo -500 đến +500
    // fraction: 0.0 = Đen áp đảo hoàn toàn, 0.5 = Cân bằng, 1.0 = Đỏ áp đảo hoàn toàn
    final clampedScore = widget.evaluationScore.clamp(-500, 500);
    final blackScoreRatio = (500 - clampedScore) / 1000.0; // 0.0 -> 1.0 cho Black

    final isRedLeading = widget.evaluationScore > 0;
    final isBlackLeading = widget.evaluationScore < 0;
    final leadScore = widget.evaluationScore.abs();

    String titleText = 'Cân bằng thế trận';
    if (isRedLeading) {
      titleText = 'Đỏ ưu : $leadScore';
    } else if (isBlackLeading) {
      titleText = 'Đen ưu : $leadScore';
    }

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        // Dòng chữ thư pháp hiển thị ưu thế
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              titleText,
              style: GoogleFonts.maShanZheng(
                fontSize: 18,
                color: isRedLeading
                    ? const Color(0xFFFFB74D)
                    : isBlackLeading
                        ? const Color(0xFFECEFF1)
                        : AppColors.goldLight,
                fontWeight: FontWeight.bold,
                letterSpacing: 1.5,
                shadows: [
                  Shadow(
                    color: isRedLeading
                        ? Colors.redAccent.withAlpha(180)
                        : Colors.black,
                    blurRadius: 8,
                    offset: const Offset(0, 1),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            GestureDetector(
              onTap: widget.onHelpTapped ??
                  () {
                    showDialog(
                      context: context,
                      builder: (ctx) => AlertDialog(
                        backgroundColor: AppColors.darkSurface,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14),
                          side: const BorderSide(color: AppColors.gold, width: 1.5),
                        ),
                        title: Row(
                          children: [
                            const Icon(Icons.help_outline, color: AppColors.gold),
                            const SizedBox(width: 8),
                            Text(
                              'Thanh Ưu Thế Thế Trận',
                              style: GoogleFonts.philosopher(
                                color: AppColors.goldLight,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                        content: Text(
                          'Thanh thế trận đánh giá độ chênh lệch thực lực hiện tại giữa Đỏ và Đen theo thuật toán Minimax và AI Heuristic.\n\n'
                          '• Ngọn lửa dịch chuyển về bên nào chứng tỏ bên đó đang chịu sức ép.\n'
                          '• Điểm số càng lớn, thế cờ càng chênh lệch rõ rệt.',
                          style: GoogleFonts.philosopher(
                            color: AppColors.paperLight,
                            fontSize: 13,
                            height: 1.5,
                          ),
                        ),
                        actions: [
                          TextButton(
                            onPressed: () => Navigator.pop(ctx),
                            child: Text(
                              'Đã hiểu',
                              style: GoogleFonts.philosopher(
                                color: AppColors.gold,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                        ],
                      ),
                    );
                  },
              child: Container(
                padding: const EdgeInsets.all(3),
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: Colors.black45,
                  border: Border.all(color: AppColors.goldDark, width: 1),
                ),
                child: const Icon(
                  Icons.question_mark,
                  size: 11,
                  color: AppColors.goldLight,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 5),

        // Thanh đo vật lý với huy hiệu 2 đầu
        SizedBox(
          height: 28,
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              // Huy hiệu Tướng Đen [將]
              _buildMedallion(
                char: '將',
                isRed: false,
              ),

              // Ống đo năng lượng
              Expanded(
                child: AnimatedBuilder(
                  animation: _flameCtrl,
                  builder: (context, _) {
                    return CustomPaint(
                      size: const Size(double.infinity, 16),
                      painter: _AdvantageTubePainter(
                        blackRatio: blackScoreRatio,
                        flameAnimValue: _flameCtrl.value,
                      ),
                    );
                  },
                ),
              ),

              // Huy hiệu Tướng Đỏ [帥]
              _buildMedallion(
                char: '帥',
                isRed: true,
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildMedallion({required String char, required bool isRed}) {
    return Container(
      width: 28,
      height: 28,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: isRed
              ? const [Color(0xFFE53935), Color(0xFF8E0000), Color(0xFF4A0000)]
              : const [Color(0xFF424242), Color(0xFF212121), Color(0xFF000000)],
        ),
        border: Border.all(
          color: isRed ? const Color(0xFFFFD54F) : const Color(0xFFB0BEC5),
          width: 1.8,
        ),
        boxShadow: [
          BoxShadow(
            color: isRed
                ? Colors.redAccent.withAlpha(120)
                : Colors.white.withAlpha(30),
            blurRadius: 6,
            spreadRadius: 1,
          ),
        ],
      ),
      child: Center(
        child: Text(
          char,
          style: GoogleFonts.maShanZheng(
            fontSize: 15,
            fontWeight: FontWeight.bold,
            color: isRed ? const Color(0xFFFFE082) : const Color(0xFFECEFF1),
          ),
        ),
      ),
    );
  }
}

class _AdvantageTubePainter extends CustomPainter {
  final double blackRatio; // 0.0 -> 1.0 (vị trí phân chia tính từ trái sang)
  final double flameAnimValue;

  _AdvantageTubePainter({
    required this.blackRatio,
    required this.flameAnimValue,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final h = size.height;
    final w = size.width;
    if (w <= 0 || h <= 0) return;

    final barRect = RRect.fromRectAndRadius(
      Rect.fromLTWH(0, (h - 10) / 2, w, 10),
      const Radius.circular(5),
    );

    // 1. Viền ngoài kim loại cổ
    canvas.save();
    canvas.clipRRect(barRect);

    final dividerX = (w * blackRatio).clamp(8.0, w - 8.0);

    // Nửa bên trái: Phe Đen (Kim loại đen/đá xám)
    final blackPaint = Paint()
      ..shader = const LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [Color(0xFF4A4A4A), Color(0xFF1E1E1E), Color(0xFF121212)],
      ).createShader(Rect.fromLTWH(0, 0, dividerX, h));
    canvas.drawRect(Rect.fromLTWH(0, 0, dividerX, h), blackPaint);

    // Nửa bên phải: Phe Đỏ (Lửa nham thạch / đỏ rực)
    final redPaint = Paint()
      ..shader = const LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [Color(0xFFFF5722), Color(0xFFD84315), Color(0xFF870000)],
      ).createShader(Rect.fromLTWH(dividerX, 0, w - dividerX, h));
    canvas.drawRect(Rect.fromLTWH(dividerX, 0, w - dividerX, h), redPaint);

    canvas.restore();

    // 2. Viền mạ vàng thanh đo
    final borderPaint = Paint()
      ..color = const Color(0xFF8D6E63)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.2;
    canvas.drawRRect(barRect, borderPaint);

    // 3. Vệt lửa phát sáng ở vị trí ranh giới (Flame Divider)
    final flamePulse = math.sin(flameAnimValue * math.pi) * 3;
    final flameGlow = Paint()
      ..shader = RadialGradient(
        colors: [
          Colors.white,
          const Color(0xFFFFEB3B),
          const Color(0xFFFF5722).withAlpha(180),
          Colors.transparent,
        ],
        stops: const [0.0, 0.25, 0.65, 1.0],
      ).createShader(
        Rect.fromCircle(
          center: Offset(dividerX, h / 2),
          radius: 12 + flamePulse,
        ),
      );

    canvas.drawCircle(Offset(dividerX, h / 2), 12 + flamePulse, flameGlow);

    // Lõi lửa sáng chói
    final corePaint = Paint()..color = Colors.white;
    canvas.drawOval(
      Rect.fromCenter(
        center: Offset(dividerX, h / 2),
        width: 3.5,
        height: 12 + flamePulse * 0.5,
      ),
      corePaint,
    );
  }

  @override
  bool shouldRepaint(covariant _AdvantageTubePainter oldDelegate) {
    return oldDelegate.blackRatio != blackRatio ||
        oldDelegate.flameAnimValue != flameAnimValue;
  }
}
