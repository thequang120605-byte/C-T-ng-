import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';
import 'game_screen.dart';

class PuzzleMapScreen extends StatefulWidget {
  final VoidCallback onBackToHome;

  const PuzzleMapScreen({super.key, required this.onBackToHome});

  @override
  State<PuzzleMapScreen> createState() => _PuzzleMapScreenState();
}

class _PuzzleMapScreenState extends State<PuzzleMapScreen>
    with SingleTickerProviderStateMixin {
  late final AnimationController _pulseCtrl;

  // 10 Ải Cờ Thế Kinh Điển
  final List<_PuzzleStage> _stages = [
    _PuzzleStage(
      id: 1,
      title: 'Khởi Mã Phá Trận',
      difficulty: 'Dễ',
      stars: 3,
      isUnlocked: true,
      description: 'Mã nhập cung phá vỡ hàng rào sĩ tượng đối phương.',
    ),
    _PuzzleStage(
      id: 2,
      title: 'Đơn Xa Bạt Trại',
      difficulty: 'Dễ',
      stars: 3,
      isUnlocked: true,
      description: 'Một Xe tung hoành giữa trùng điệp phòng tuyến.',
    ),
    _PuzzleStage(
      id: 3,
      title: 'Pháo Giác Chiếu Bí',
      difficulty: 'Trung bình',
      stars: 3,
      isUnlocked: true,
      description: 'Uy lực Pháo góc kết hợp Mã ngoạ tào.',
    ),
    _PuzzleStage(
      id: 4,
      title: 'Thất Tinh Tụ Hội',
      difficulty: 'Cao thủ',
      stars: 2,
      isUnlocked: true,
      description: 'Thế cờ giang hồ danh bất hư truyền ngàn năm.',
    ),
    _PuzzleStage(
      id: 5,
      title: 'Dã Mã Thao Điền',
      difficulty: 'Cao thủ',
      stars: 0,
      isUnlocked: true, // Ải đang chơi
      description: 'Ngựa hoang giày xéo ruộng đồng, biến ảo khôn lường.',
    ),
    _PuzzleStage(
      id: 6,
      title: 'Khổng Minh Khóc Bạn',
      difficulty: 'Đại sư',
      stars: 0,
      isUnlocked: false,
      description: 'Thế trận bi tráng đầy mưu lược sâu xa.',
    ),
    _PuzzleStage(
      id: 7,
      title: 'Thiềm Thừ Hí Nguyệt',
      difficulty: 'Đại sư',
      stars: 0,
      isUnlocked: false,
      description: 'Cóc vàng giỡn trăng, phản kích ngoạn mục.',
    ),
    _PuzzleStage(
      id: 8,
      title: 'Cửu Tử Nhất Sinh',
      difficulty: 'Kỳ thánh',
      stars: 0,
      isUnlocked: false,
      description: 'Ngàn cân treo sợi tóc, tìm đường sống trong cửa tử.',
    ),
    _PuzzleStage(
      id: 9,
      title: 'Hỏa Đốt Ô Sào',
      difficulty: 'Kỳ thánh',
      stars: 0,
      isUnlocked: false,
      description: 'Tập kích kho lương, phá vỡ thế giằng co.',
    ),
    _PuzzleStage(
      id: 10,
      title: 'Thiên Ngoại Hữu Thiên',
      difficulty: 'Vô song',
      stars: 0,
      isUnlocked: false,
      description: 'Đỉnh cao trí tuệ cờ tướng cổ điển Trung Hoa.',
    ),
  ];

  final int _hearts = 3;
  final int _hints = 5;
  final int _retries = 3;

  @override
  void initState() {
    super.initState();
    _pulseCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1200),
    )..repeat(reverse: true);
  }

  @override
  void dispose() {
    _pulseCtrl.dispose();
    super.dispose();
  }

  void _onStageSelected(_PuzzleStage stage) {
    if (!stage.isUnlocked) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: const Color(0xFFC0392B),
          content: Text(
            'Ải này chưa mở khóa! Hãy vượt qua ải trước đó.',
            style: GoogleFonts.philosopher(),
          ),
        ),
      );
      return;
    }

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.darkSurface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: AppColors.gold, width: 1.5),
        ),
        title: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(6),
              decoration: const BoxDecoration(
                shape: BoxShape.circle,
                gradient: LinearGradient(
                  colors: [Color(0xFFE5A855), Color(0xFFB57A2A)],
                ),
              ),
              child: Text(
                '${stage.id}',
                style: GoogleFonts.cinzel(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: Colors.black87,
                ),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    stage.title,
                    style: GoogleFonts.philosopher(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: AppColors.goldLight,
                    ),
                  ),
                  Text(
                    'Độ khó: ${stage.difficulty}',
                    style: GoogleFonts.philosopher(
                      fontSize: 12,
                      color: const Color(0xFFBCAAA4),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              stage.description,
              style: GoogleFonts.philosopher(
                color: AppColors.paperLight,
                fontSize: 14,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                const Icon(Icons.star, color: Color(0xFFFFD54F), size: 18),
                const SizedBox(width: 4),
                Text(
                  'Thành tích: ${stage.stars}/3 sao',
                  style: GoogleFonts.philosopher(
                    color: const Color(0xFFFFD54F),
                    fontWeight: FontWeight.bold,
                    fontSize: 13,
                  ),
                ),
              ],
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(
              'Đóng',
              style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4)),
            ),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              side: const BorderSide(color: Color(0xFFFFD54F), width: 1.2),
            ),
            onPressed: () {
              Navigator.pop(ctx);
              Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (_) => GameScreen(
                    gameId: 1000 + stage.id,
                    difficulty: 3,
                    mode: 'PvE',
                    seatHint: 'Red',
                  ),
                ),
              );
            },
            child: Text(
              'Vượt Ải Ngay',
              style: GoogleFonts.philosopher(
                fontWeight: FontWeight.bold,
                color: Colors.white,
              ),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF16120F),
      body: SafeArea(
        child: Column(
          children: [
            // 1. THANH TÀI NGUYÊN TRÊN CÙNG (Hearts, Hints, Retries)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              decoration: const BoxDecoration(
                color: Color(0xFF221A15),
                border: Border(
                  bottom: BorderSide(color: Color(0xFF4A3728), width: 1.2),
                ),
              ),
              child: Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_ios, color: AppColors.goldLight, size: 20),
                    onPressed: widget.onBackToHome,
                  ),
                  Text(
                    'BẢN ĐỒ VƯỢT ẢI',
                    style: GoogleFonts.cinzel(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: AppColors.goldLight,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const Spacer(),
                  // Tim ❤️
                  _buildResourceBadge(Icons.favorite, '$_hearts', const Color(0xFFE53935)),
                  const SizedBox(width: 10),
                  // Gợi ý 💡
                  _buildResourceBadge(Icons.lightbulb, '$_hints', const Color(0xFFFFD54F)),
                  const SizedBox(width: 10),
                  // Đi lại ↩️
                  _buildResourceBadge(Icons.replay, '$_retries', const Color(0xFF42A5F5)),
                ],
              ),
            ),

            // 2. BẢN ĐỒ THỦY MẶC UỐN LƯỢN (Mountain Ink Path)
            Expanded(
              child: Stack(
                children: [
                  // Nền núi non mây ngàn
                  Positioned.fill(
                    child: CustomPaint(
                      painter: _MountainInkPainter(),
                    ),
                  ),

                  // Con đường và các trạm ải (Scrollable Path)
                  SingleChildScrollView(
                    reverse: true, // Cuộn từ ải 1 ở dưới lên ải 10 ở trên
                    padding: const EdgeInsets.symmetric(vertical: 40, horizontal: 20),
                    child: Center(
                      child: SizedBox(
                        width: 320,
                        child: AnimatedBuilder(
                          animation: _pulseCtrl,
                          builder: (context, _) {
                            return Column(
                              children: List.generate(_stages.length, (idx) {
                                // Đảo thứ tự để Ải 10 ở trên cùng, Ải 1 ở dưới cùng
                                final stage = _stages[_stages.length - 1 - idx];
                                // Tạo độ dốc lệch trái/phải uốn lượn hình chữ S
                                final isLeft = (stage.id % 2 == 1);

                                return _buildStageNode(stage, isLeft);
                              }),
                            );
                          },
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildResourceBadge(IconData icon, String count, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: Colors.black45,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withAlpha(160), width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: color),
          const SizedBox(width: 4),
          Text(
            count,
            style: GoogleFonts.cinzel(
              fontSize: 12,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStageNode(_PuzzleStage stage, bool isLeft) {
    final isCurrent = stage.isUnlocked && stage.stars == 0;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 14),
      child: Row(
        mainAxisAlignment:
            isLeft ? MainAxisAlignment.start : MainAxisAlignment.end,
        children: [
          if (!isLeft) const SizedBox(width: 80),
          GestureDetector(
            onTap: () => _onStageSelected(stage),
            child: Column(
              children: [
                // Bệ đá Bát quái / Ngọc ấn
                Stack(
                  alignment: Alignment.center,
                  children: [
                    // Hào quang tỏa sáng nếu là ải hiện tại
                    if (isCurrent)
                      Container(
                        width: 68 + _pulseCtrl.value * 8,
                        height: 68 + _pulseCtrl.value * 8,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: const Color(0xFFFFD54F).withAlpha(60),
                        ),
                      ),

                    // Khối bệ đá tròn
                    Container(
                      width: 58,
                      height: 58,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        gradient: LinearGradient(
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                          colors: stage.isUnlocked
                              ? (stage.stars > 0
                                  ? const [Color(0xFFE5A855), Color(0xFFB57A2A)]
                                  : const [Color(0xFFBA2213), Color(0xFF6B1107)])
                              : const [Color(0xFF424242), Color(0xFF212121)],
                        ),
                        border: Border.all(
                          color: stage.isUnlocked
                              ? const Color(0xFFFFE082)
                              : const Color(0xFF757575),
                          width: 2.2,
                        ),
                        boxShadow: const [
                          BoxShadow(
                            color: Colors.black54,
                            blurRadius: 6,
                            offset: Offset(0, 3),
                          ),
                        ],
                      ),
                      child: Center(
                        child: stage.isUnlocked
                            ? Text(
                                '${stage.id}',
                                style: GoogleFonts.cinzel(
                                  fontSize: 22,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.white,
                                  shadows: const [
                                    Shadow(
                                      color: Colors.black87,
                                      blurRadius: 4,
                                      offset: Offset(1, 1),
                                    ),
                                  ],
                                ),
                              )
                            : const Icon(
                                Icons.lock,
                                color: Color(0xFF9E9E9E),
                                size: 24,
                              ),
                      ),
                    ),
                  ],
                ),

                const SizedBox(height: 4),

                // Sao thành tích ⭐⭐⭐
                if (stage.isUnlocked && stage.stars > 0)
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: List.generate(
                      3,
                      (i) => Icon(
                        Icons.star,
                        size: 13,
                        color: i < stage.stars
                            ? const Color(0xFFFFD54F)
                            : Colors.white24,
                      ),
                    ),
                  ),

                // Tên ải
                Container(
                  margin: const EdgeInsets.only(top: 2),
                  padding:
                      const EdgeInsets.symmetric(horizontal: 6, vertical: 1.5),
                  decoration: BoxDecoration(
                    color: Colors.black54,
                    borderRadius: BorderRadius.circular(6),
                    border: Border.all(color: const Color(0xFF5D4037), width: 0.8),
                  ),
                  child: Text(
                    stage.title,
                    style: GoogleFonts.philosopher(
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                      color: stage.isUnlocked
                          ? const Color(0xFFFFE082)
                          : const Color(0xFF9E9E9E),
                    ),
                  ),
                ),
              ],
            ),
          ),
          if (isLeft) const SizedBox(width: 80),
        ],
      ),
    );
  }
}

class _PuzzleStage {
  final int id;
  final String title;
  final String difficulty;
  final int stars;
  final bool isUnlocked;
  final String description;

  _PuzzleStage({
    required this.id,
    required this.title,
    required this.difficulty,
    required this.stars,
    required this.isUnlocked,
    required this.description,
  });
}

class _MountainInkPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    if (size.width <= 0 || size.height <= 0) return;

    // Nền chuyển sắc phong cảnh đêm trăng mây mù
    final bgPaint = Paint()
      ..shader = const LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [Color(0xFF0F0C0A), Color(0xFF1E1712), Color(0xFF140F0C)],
      ).createShader(Rect.fromLTWH(0, 0, size.width, size.height));
    canvas.drawRect(Rect.fromLTWH(0, 0, size.width, size.height), bgPaint);

    // Mây ngàn lượn lờ (Ink wash mist)
    final mistPaint = Paint()
      ..color = const Color(0xFF8D6E63).withAlpha(25)
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 20);

    canvas.drawCircle(Offset(size.width * 0.2, size.height * 0.3), 120, mistPaint);
    canvas.drawCircle(Offset(size.width * 0.8, size.height * 0.6), 140, mistPaint);
    canvas.drawCircle(Offset(size.width * 0.3, size.height * 0.8), 160, mistPaint);

    // Đường đá uốn lượn nối các ải
    final path = Path();
    path.moveTo(size.width * 0.35, size.height * 0.9);
    path.cubicTo(
      size.width * 0.75,
      size.height * 0.7,
      size.width * 0.25,
      size.height * 0.45,
      size.width * 0.65,
      size.height * 0.15,
    );

    final pathPaint = Paint()
      ..color = const Color(0xFF8D6E63).withAlpha(90)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 5.0
      ..strokeCap = StrokeCap.round;
    canvas.drawPath(path, pathPaint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
