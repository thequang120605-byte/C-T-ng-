import 'package:flutter/material.dart';

class PavilionBanner extends StatelessWidget {
  final Widget child;
  const PavilionBanner({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(21),
      child: CustomPaint(
        painter: _PavilionPainter(),
        child: child,
      ),
    );
  }
}

class _PavilionPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final sx = size.width / 1000.0;
    final sy = size.height / 500.0;

    // Sky gradient
    final rect = Offset.zero & size;
    final skyPaint = Paint()
      ..shader = const LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [Color(0xFFE9D9B7), Color(0xFFF5EDDE)],
      ).createShader(rect);
    canvas.drawRect(rect, skyPaint);

    canvas.save();
    canvas.scale(sx, sy);

    // Sun
    final sunPaint = Paint()
      ..color = const Color(0xFFD3AD69).withValues(alpha: 0.24);
    canvas.drawCircle(const Offset(762, 102), 62, sunPaint);

    // Distant hills 1
    final hill1 = Path()
      ..moveTo(0, 270)
      ..quadraticBezierTo(100, 160, 230, 260)
      ..quadraticBezierTo(360, 360, 470, 235)
      ..quadraticBezierTo(580, 110, 740, 230)
      ..quadraticBezierTo(900, 350, 1000, 155)
      ..lineTo(1000, 500)
      ..lineTo(0, 500)
      ..close();
    canvas.drawPath(
      hill1,
      Paint()..color = const Color(0xFFB9B69D).withValues(alpha: 0.25),
    );

    // Distant hills 2
    final hill2 = Path()
      ..moveTo(0, 355)
      ..quadraticBezierTo(170, 235, 310, 325)
      ..quadraticBezierTo(450, 415, 610, 300)
      ..quadraticBezierTo(770, 185, 1000, 260)
      ..lineTo(1000, 500)
      ..lineTo(0, 500)
      ..close();
    canvas.drawPath(
      hill2,
      Paint()..color = const Color(0xFF97A18D).withValues(alpha: 0.19),
    );

    // Pavilion silhouette
    final pavPaint = Paint()
      ..color = const Color(0xFF8E7957).withValues(alpha: 0.27);
    final p1 = Path()
      ..moveTo(545, 285)
      ..lineTo(890, 285)
      ..lineTo(862, 270)
      ..lineTo(574, 270)
      ..close();
    canvas.drawPath(p1, pavPaint);

    final p2 = Path()
      ..moveTo(589, 262)
      ..lineTo(852, 262)
      ..quadraticBezierTo(815, 252, 800, 227)
      ..lineTo(637, 227)
      ..quadraticBezierTo(620, 251, 589, 262)
      ..close();
    canvas.drawPath(p2, pavPaint);

    final p3 = Path()
      ..moveTo(630, 221)
      ..lineTo(807, 221)
      ..quadraticBezierTo(780, 211, 771, 188)
      ..lineTo(666, 188)
      ..quadraticBezierTo(657, 210, 630, 221)
      ..close();
    canvas.drawPath(p3, pavPaint);

    final p4 = Path()
      ..moveTo(661, 183)
      ..lineTo(777, 183)
      ..quadraticBezierTo(741, 167, 719, 144)
      ..quadraticBezierTo(696, 170, 661, 183)
      ..close();
    canvas.drawPath(p4, pavPaint);

    canvas.drawRect(const Rect.fromLTRB(653, 265, 785, 341), pavPaint);
    canvas.drawRect(const Rect.fromLTRB(618, 288, 632, 350), pavPaint);
    canvas.drawRect(const Rect.fromLTRB(805, 288, 819, 350), pavPaint);
    canvas.drawRect(const Rect.fromLTRB(556, 347, 882, 356), pavPaint);
    canvas.drawRect(const Rect.fromLTRB(539, 362, 900, 373), pavPaint);
    canvas.drawRect(const Rect.fromLTRB(520, 380, 919, 392), pavPaint);

    // Bamboo stems
    final bambooStroke = Paint()
      ..color = const Color(0xFF54745C).withValues(alpha: 0.28)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 7;
    final b1 = Path()
      ..moveTo(965, 500)
      ..quadraticBezierTo(942, 300, 966, 110);
    final b2 = Path()
      ..moveTo(987, 500)
      ..quadraticBezierTo(963, 342, 999, 240);
    canvas.drawPath(b1, bambooStroke);
    canvas.drawPath(b2, bambooStroke);

    canvas.restore();
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class ChessEmblemIcon extends StatelessWidget {
  final double size;
  const ChessEmblemIcon({super.key, this.size = 68});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: size,
      height: size,
      child: CustomPaint(painter: _ChessEmblemPainter()),
    );
  }
}

class _ChessEmblemPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final s = size.width / 180.0;
    canvas.save();
    canvas.scale(s, s);

    // Shadow
    canvas.drawCircle(
      const Offset(90, 94),
      78,
      Paint()..color = const Color(0xFF5F3820).withValues(alpha: 0.22),
    );

    // Body
    final bodyPaint = Paint()
      ..shader = const RadialGradient(
        colors: [Color(0xFFFFF1CA), Color(0xFFCA9952)],
      ).createShader(Rect.fromCircle(center: const Offset(90, 87), radius: 75));
    canvas.drawCircle(const Offset(90, 87), 75, bodyPaint);

    // Outer border
    canvas.drawCircle(
      const Offset(90, 87),
      75,
      Paint()
        ..color = const Color(0xFF805327)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 4,
    );

    // Inner border
    canvas.drawCircle(
      const Offset(90, 87),
      64,
      Paint()
        ..color = const Color(0xFF9D6C32)
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2,
    );

    // Cross lines
    final linePaint = Paint()
      ..color = const Color(0xFFA35031).withValues(alpha: 0.7)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 7;
    canvas.drawLine(const Offset(49, 87), const Offset(131, 87), linePaint);
    canvas.drawLine(const Offset(90, 46), const Offset(90, 128), linePaint);
    canvas.drawLine(const Offset(61, 58), const Offset(119, 116), linePaint);
    canvas.drawLine(const Offset(61, 116), const Offset(119, 58), linePaint);

    canvas.restore();
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
