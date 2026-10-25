import 'dart:math' as math;
import 'dart:ui' as ui;
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
  final ScrollController _scrollController = ScrollController();

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

  static const double _mapHeight = 1960.0;

  @override
  void initState() {
    super.initState();
    _pulseCtrl = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1400),
    )..repeat(reverse: true);

    WidgetsBinding.instance.addPostFrameCallback((_) {
      _scrollToActiveStage();
    });
  }

  void _scrollToActiveStage() {
    if (!_scrollController.hasClients) return;
    final screenH = MediaQuery.of(context).size.height;
    // Ải 5 ở y = 1030.0 -> cuộn để Ải 5 nằm giữa màn hình
    const targetY = 1030.0;
    final scrollOffset = (targetY - screenH * 0.45).clamp(
      0.0,
      _scrollController.position.maxScrollExtent,
    );
    _scrollController.animateTo(
      scrollOffset,
      duration: const Duration(milliseconds: 950),
      curve: Curves.easeInOutCubic,
    );
  }

  @override
  void dispose() {
    _pulseCtrl.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _onStageSelected(_PuzzleStage stage) {
    if (!stage.isUnlocked) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: const Color(0xFFC0392B),
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          content: Row(
            children: [
              const Icon(Icons.lock, color: Colors.white, size: 20),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  'Ải ${stage.id} chưa mở khóa! Hãy vượt qua Ải ${stage.id - 1} trước.',
                  style: GoogleFonts.philosopher(fontWeight: FontWeight.bold),
                ),
              ),
            ],
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
              width: 36,
              height: 36,
              decoration: const BoxDecoration(
                shape: BoxShape.circle,
                gradient: LinearGradient(
                  colors: [Color(0xFFE5A855), Color(0xFFB57A2A)],
                ),
              ),
              child: Center(
                child: Text(
                  '${stage.id}',
                  style: GoogleFonts.cinzel(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: Colors.black87,
                  ),
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

  void _showChestDialog(String name, String reward, bool isClaimed) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF221711),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFFFD54F), width: 1.5),
        ),
        title: Row(
          children: [
            const Icon(Icons.card_giftcard, color: Color(0xFFFFD54F), size: 26),
            const SizedBox(width: 10),
            Text(
              name,
              style: GoogleFonts.cinzel(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: AppColors.goldLight,
              ),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              isClaimed
                  ? 'Bạn đã mở khóa rương kho báu này:'
                  : 'Vượt qua các ải liên quan để mở rương:',
              style: GoogleFonts.philosopher(color: const Color(0xFFD7CCC8)),
            ),
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: Colors.black45,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: const Color(0xFF5D4037)),
              ),
              child: Text(
                reward,
                style: GoogleFonts.philosopher(
                  color: const Color(0xFFFFD54F),
                  fontWeight: FontWeight.bold,
                  fontSize: 14,
                ),
              ),
            ),
          ],
        ),
        actions: [
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFB57A2A),
            ),
            onPressed: () => Navigator.pop(ctx),
            child: Text(
              'Xác Nhận',
              style: GoogleFonts.philosopher(color: Colors.white),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF140F0C),
      body: SafeArea(
        child: Column(
          children: [
            // 1. THANH TÀI NGUYÊN TRÊN CÙNG
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
                  _buildResourceBadge(Icons.favorite, '$_hearts', const Color(0xFFE53935)),
                  const SizedBox(width: 8),
                  _buildResourceBadge(Icons.lightbulb, '$_hints', const Color(0xFFFFD54F)),
                  const SizedBox(width: 8),
                  _buildResourceBadge(Icons.replay, '$_retries', const Color(0xFF42A5F5)),
                ],
              ),
            ),

            // 2. BẢN ĐỒ UỐN LƯỢN HOÀNG GIA (Curved Serpentine Saga Map)
            Expanded(
              child: LayoutBuilder(
                builder: (context, constraints) {
                  final mapWidth = constraints.maxWidth;
                  final effectiveW = math.min(mapWidth, 460.0);
                  final horizontalOffset = (mapWidth - effectiveW) / 2.0;

                  // Tính toán tọa độ của tất cả các ải và điểm mốc trên bản đồ
                  final waypoints = _computeWaypoints(effectiveW);

                  return SingleChildScrollView(
                    controller: _scrollController,
                    physics: const BouncingScrollPhysics(),
                    child: Center(
                      child: Container(
                        width: mapWidth,
                        height: _mapHeight,
                        color: const Color(0xFF130E0A),
                        child: Stack(
                          clipBehavior: Clip.none,
                          children: [
                            // Lớp 1: Vẽ cảnh nền thủy mặc núi non & con đường đá uốn lượn
                            Positioned(
                              left: horizontalOffset,
                              top: 0,
                              width: effectiveW,
                              height: _mapHeight,
                              child: AnimatedBuilder(
                                animation: _pulseCtrl,
                                builder: (context, _) {
                                  return CustomPaint(
                                    size: Size(effectiveW, _mapHeight),
                                    painter: _CurvedSagaPathPainter(
                                      waypoints: waypoints,
                                      pulseValue: _pulseCtrl.value,
                                    ),
                                  );
                                },
                              ),
                            ),

                            // Lớp 2: Khởi trại dưới chân núi
                            Positioned(
                              left: horizontalOffset + waypoints.startCamp.dx - 80,
                              top: waypoints.startCamp.dy - 20,
                              child: _buildStartCamp(),
                            ),

                            // Lớp 3: Rương bảo vật chặng 1 & chặng 2
                            Positioned(
                              left: horizontalOffset + waypoints.chest1.dx - 30,
                              top: waypoints.chest1.dy - 30,
                              child: _buildTreasureChest(
                                name: 'Rương Bạc Vượt Ải',
                                reward: '+3 Tim ❤️, +2 Gợi ý 💡',
                                isClaimed: true,
                              ),
                            ),
                            Positioned(
                              left: horizontalOffset + waypoints.chest2.dx - 30,
                              top: waypoints.chest2.dy - 30,
                              child: _buildTreasureChest(
                                name: 'Rương Vàng Hoàng Kim',
                                reward: '+500 Elo, +5 Đi lại ↩️',
                                isClaimed: false,
                              ),
                            ),

                            // Lớp 4: Đỉnh Cung Điện Vô Song ở trên cùng (Ải 10)
                            Positioned(
                              left: horizontalOffset + waypoints.palacePeak.dx - 90,
                              top: waypoints.palacePeak.dy - 50,
                              child: _buildPalacePeak(),
                            ),

                            // Lớp 5: Các trạm ải 1 đến 10 (Đặt chính xác tại tọa độ đường cong)
                            ..._stages.map((stage) {
                              final pt = waypoints.stagePoints[stage.id - 1];
                              return Positioned(
                                left: horizontalOffset + pt.dx - 60,
                                top: pt.dy - 55,
                                child: AnimatedBuilder(
                                  animation: _pulseCtrl,
                                  builder: (context, _) {
                                    return _buildCurvedStageNode(stage);
                                  },
                                ),
                              );
                            }),
                          ],
                        ),
                      ),
                    ),
                  );
                },
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
        color: Colors.black54,
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

  // Tọa độ tính toán đường cong S-Curve cho toàn bộ 10 Ải
  _MapWaypoints _computeWaypoints(double width) {
    // Ải 1 -> Ải 10 từ đáy lên đỉnh
    final stagePoints = [
      Offset(width * 0.28, 1720.0), // Ải 1 (Trái)
      Offset(width * 0.74, 1550.0), // Ải 2 (Phải)
      Offset(width * 0.28, 1380.0), // Ải 3 (Trái)
      Offset(width * 0.74, 1210.0), // Ải 4 (Phải)
      Offset(width * 0.32, 1030.0), // Ải 5 (Trái - Đang chơi)
      Offset(width * 0.72, 850.0),  // Ải 6 (Phải)
      Offset(width * 0.28, 680.0),  // Ải 7 (Trái)
      Offset(width * 0.72, 510.0),  // Ải 8 (Phải)
      Offset(width * 0.30, 340.0),  // Ải 9 (Trái)
      Offset(width * 0.50, 180.0),  // Ải 10 (Đỉnh Trung Tâm)
    ];

    return _MapWaypoints(
      startCamp: Offset(width * 0.50, 1860.0),
      stagePoints: stagePoints,
      chest1: Offset(width * 0.52, 1120.0), // Giữa ải 4 và ải 5
      chest2: Offset(width * 0.50, 595.0),  // Giữa ải 7 và ải 8
      palacePeak: Offset(width * 0.50, 95.0),
    );
  }

  // Nút Trạm Ải theo phong cách Ngọc Ấn / Bát Quái
  Widget _buildCurvedStageNode(_PuzzleStage stage) {
    final isCurrent = stage.isUnlocked && stage.stars == 0;
    final isCleared = stage.isUnlocked && stage.stars > 0;
    final isBoss = stage.id == 10;

    return GestureDetector(
      onTap: () => _onStageSelected(stage),
      child: SizedBox(
        width: 120,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            // Cờ hiệu "ĐANG ĐẤU" bồng bềnh nếu là ải hiện tại
            if (isCurrent) ...[
              Transform.translate(
                offset: Offset(0, math.sin(_pulseCtrl.value * 2 * math.pi) * 3),
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    gradient: const LinearGradient(
                      colors: [Color(0xFFE53935), Color(0xFFB71C1C)],
                    ),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: const Color(0xFFFFD54F), width: 1.2),
                    boxShadow: [
                      BoxShadow(
                        color: const Color(0xFFE53935).withAlpha(120),
                        blurRadius: 6,
                        spreadRadius: 1,
                      ),
                    ],
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.flash_on, color: Color(0xFFFFD54F), size: 12),
                      const SizedBox(width: 2),
                      Text(
                        'ĐANG ĐẤU',
                        style: GoogleFonts.cinzel(
                          fontSize: 9,
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                          letterSpacing: 0.5,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 3),
            ],

            // Khối Ngọc Ấn tròn
            Stack(
              alignment: Alignment.center,
              clipBehavior: Clip.none,
              children: [
                // Vòng hào quang tỏa sáng nếu là ải hiện tại
                if (isCurrent)
                  Container(
                    width: 72 + _pulseCtrl.value * 8,
                    height: 72 + _pulseCtrl.value * 8,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      boxShadow: [
                        BoxShadow(
                          color: const Color(0xFFFFD54F).withAlpha(80),
                          blurRadius: 12,
                          spreadRadius: 2,
                        ),
                      ],
                    ),
                  ),

                // Thân nút chính
                Container(
                  width: isBoss ? 68 : 58,
                  height: isBoss ? 68 : 58,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: isCleared
                          ? const [Color(0xFFFFE082), Color(0xFFD49A3D), Color(0xFF8D5B1B)]
                          : (isCurrent
                              ? const [Color(0xFFE53935), Color(0xFFB71C1C), Color(0xFF6B0E0E)]
                              : const [Color(0xFF3E352E), Color(0xFF261F1A)]),
                    ),
                    border: Border.all(
                      color: isCleared
                          ? const Color(0xFFFFF9C4)
                          : (isCurrent
                              ? const Color(0xFFFFD54F)
                              : const Color(0xFF615349)),
                      width: isBoss ? 3.0 : 2.2,
                    ),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withAlpha(180),
                        blurRadius: 8,
                        offset: const Offset(0, 4),
                      ),
                      if (isCleared)
                        BoxShadow(
                          color: const Color(0xFFFFB300).withAlpha(70),
                          blurRadius: 8,
                          spreadRadius: 1,
                        ),
                    ],
                  ),
                  child: Center(
                    child: stage.isUnlocked
                        ? (isBoss
                            ? const Icon(Icons.workspace_premium, color: Color(0xFFFFD54F), size: 30)
                            : Text(
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
                              ))
                        : const Icon(
                            Icons.lock,
                            color: Color(0xFF9E8E81),
                            size: 22,
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

            // Tên Ải Cổ Phong
            Container(
              margin: const EdgeInsets.only(top: 2),
              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
              decoration: BoxDecoration(
                color: const Color(0xFF1E1712).withAlpha(220),
                borderRadius: BorderRadius.circular(6),
                border: Border.all(
                  color: isCurrent
                      ? const Color(0xFFFFD54F)
                      : (isCleared ? const Color(0xFFB57A2A) : const Color(0xFF4E3D31)),
                  width: isCurrent ? 1.2 : 0.8,
                ),
                boxShadow: const [
                  BoxShadow(
                    color: Colors.black45,
                    blurRadius: 3,
                    offset: Offset(0, 1),
                  ),
                ],
              ),
              child: Text(
                stage.title,
                textAlign: TextAlign.center,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: GoogleFonts.philosopher(
                  fontSize: 10.5,
                  fontWeight: FontWeight.bold,
                  color: isCleared
                      ? const Color(0xFFFFE082)
                      : (isCurrent ? Colors.white : const Color(0xFF9E8E81)),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  // Khởi trại dưới chân núi
  Widget _buildStartCamp() {
    return Container(
      width: 160,
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: const Color(0xFF221711).withAlpha(220),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFF8D6E63), width: 1.2),
        boxShadow: const [
          BoxShadow(color: Colors.black87, blurRadius: 6, offset: Offset(0, 2)),
        ],
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.temple_buddhist, color: Color(0xFFFFD54F), size: 18),
          const SizedBox(width: 6),
          Text(
            'Khởi Hành Kỳ Đạo',
            style: GoogleFonts.philosopher(
              fontSize: 11,
              fontWeight: FontWeight.bold,
              color: const Color(0xFFFFE082),
            ),
          ),
        ],
      ),
    );
  }

  // Rương báu trên đường vượt ải
  Widget _buildTreasureChest({
    required String name,
    required String reward,
    required bool isClaimed,
  }) {
    return GestureDetector(
      onTap: () => _showChestDialog(name, reward, isClaimed),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: isClaimed ? const Color(0xFF3E2723) : const Color(0xFF5D4037),
              border: Border.all(
                color: isClaimed ? const Color(0xFFFFD54F) : const Color(0xFFBCAAA4),
                width: 1.8,
              ),
              boxShadow: [
                BoxShadow(
                  color: isClaimed
                      ? const Color(0xFFFFB300).withAlpha(100)
                      : Colors.black54,
                  blurRadius: 8,
                ),
              ],
            ),
            child: Icon(
              isClaimed ? Icons.lock_open : Icons.card_giftcard,
              color: isClaimed ? const Color(0xFFFFD54F) : const Color(0xFFFFCA28),
              size: 24,
            ),
          ),
          const SizedBox(height: 2),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1.5),
            decoration: BoxDecoration(
              color: Colors.black54,
              borderRadius: BorderRadius.circular(4),
            ),
            child: Text(
              name,
              style: GoogleFonts.philosopher(
                fontSize: 9,
                color: isClaimed ? const Color(0xFFFFD54F) : const Color(0xFFBCAAA4),
              ),
            ),
          ),
        ],
      ),
    );
  }

  // Cung điện đỉnh phong ở Ải 10
  Widget _buildPalacePeak() {
    return Container(
      width: 180,
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF3E1F11), Color(0xFF1E100A)],
        ),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFFFD54F), width: 1.5),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFFFFB300).withAlpha(80),
            blurRadius: 12,
            spreadRadius: 1,
          ),
        ],
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.castle, color: Color(0xFFFFD54F), size: 20),
              const SizedBox(width: 6),
              Text(
                'THIÊN NGOẠI CUNG',
                style: GoogleFonts.cinzel(
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                  color: const Color(0xFFFFD54F),
                  letterSpacing: 1.0,
                ),
              ),
            ],
          ),
          const SizedBox(height: 2),
          Text(
            'Đỉnh Phong Kỳ Đạo',
            style: GoogleFonts.philosopher(
              fontSize: 10,
              color: const Color(0xFFD7CCC8),
            ),
          ),
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

class _MapWaypoints {
  final Offset startCamp;
  final List<Offset> stagePoints;
  final Offset chest1;
  final Offset chest2;
  final Offset palacePeak;

  _MapWaypoints({
    required this.startCamp,
    required this.stagePoints,
    required this.chest1,
    required this.chest2,
    required this.palacePeak,
  });
}

// BỘ VẼ ĐƯỜNG CONG SAGA NỐI CÁC ẢI VÀ NỀN THỦY MẶC
class _CurvedSagaPathPainter extends CustomPainter {
  final _MapWaypoints waypoints;
  final double pulseValue;

  _CurvedSagaPathPainter({
    required this.waypoints,
    required this.pulseValue,
  });

  @override
  void paint(Canvas canvas, Size size) {
    if (size.width <= 0 || size.height <= 0) return;

    // 1. NỀN THỦY MẶC NÚI NON HUYỀN ẢO
    _paintLandscapeBackground(canvas, size);

    // 2. DỰNG ĐƯỜNG CONG LIÊN TỤC NỐI TẤT CẢ CÁC ĐIỂM
    final allNodes = <Offset>[
      waypoints.startCamp,
      waypoints.stagePoints[0], // Ải 1
      waypoints.stagePoints[1], // Ải 2
      waypoints.stagePoints[2], // Ải 3
      waypoints.stagePoints[3], // Ải 4
      waypoints.chest1,         // Rương 1
      waypoints.stagePoints[4], // Ải 5
      waypoints.stagePoints[5], // Ải 6
      waypoints.stagePoints[6], // Ải 7
      waypoints.chest2,         // Rương 2
      waypoints.stagePoints[7], // Ải 8
      waypoints.stagePoints[8], // Ải 9
      waypoints.stagePoints[9], // Ải 10
      waypoints.palacePeak,
    ];

    final fullPath = Path();
    fullPath.moveTo(allNodes[0].dx, allNodes[0].dy);

    for (int i = 0; i < allNodes.length - 1; i++) {
      final p1 = allNodes[i];
      final p2 = allNodes[i + 1];
      final dy = p2.dy - p1.dy; // Âm vì hướng lên trên

      // Tiếp tuyến thẳng đứng ở hai đầu tạo đường cong mềm mại chữ S tự nhiên
      final cp1 = Offset(p1.dx, p1.dy + dy * 0.45);
      final cp2 = Offset(p2.dx, p2.dy - dy * 0.45);

      fullPath.cubicTo(cp1.dx, cp1.dy, cp2.dx, cp2.dy, p2.dx, p2.dy);
    }

    // 3. VẼ ĐƯỜNG ĐÁ CỔ PHONG (ROAD BED)
    // Bóng đổ mờ của con đường
    final shadowPaint = Paint()
      ..color = Colors.black.withAlpha(160)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 32.0
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 8);
    canvas.drawPath(fullPath, shadowPaint);

    // Viền bờ đá cổ
    final stoneBorderPaint = Paint()
      ..color = const Color(0xFF382517)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 24.0
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;
    canvas.drawPath(fullPath, stoneBorderPaint);

    // Lòng đường đá cuội
    final stonePavementPaint = Paint()
      ..color = const Color(0xFF5A3E2A)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 16.0
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;
    canvas.drawPath(fullPath, stonePavementPaint);

    // 4. VẼ CÁC BẬC ĐÁ LÁT / VẠCH ĐÁ HOA CƯƠNG THEO ĐƯỜNG CONG
    _paintSteppingStones(canvas, fullPath);

    // 5. ĐƯỜNG HOÀNG KIM NỐI CÁC ẢI ĐÃ VƯỢT (Conquered Golden Trail)
    // Từ Ải 1 đến Ải 5 (Ải hiện tại)
    _paintConqueredTrail(canvas, allNodes);

    // 6. SƯƠNG MÙ CHE PHỦ ĐOẠN ẢI CHƯA MỞ KHÓA (Ải 6 - Ải 10)
    _paintFogOfWar(canvas, size);
  }

  void _paintLandscapeBackground(Canvas canvas, Size size) {
    // Gradient nền đêm trăng núi non
    final bgPaint = Paint()
      ..shader = ui.Gradient.linear(
        const Offset(0, 0),
        Offset(0, size.height),
        const [
          Color(0xFF0C0907),
          Color(0xFF1E1712),
          Color(0xFF16110D),
          Color(0xFF110C09),
        ],
        [0.0, 0.35, 0.75, 1.0],
      );
    canvas.drawRect(Rect.fromLTWH(0, 0, size.width, size.height), bgPaint);

    // Dãy núi thủy mặc mờ ảo (Ink mountains silhouettes)
    final mountainPaint1 = Paint()
      ..color = const Color(0xFF261D17).withAlpha(90)
      ..style = PaintingStyle.fill;

    final m1 = Path();
    m1.moveTo(0, size.height * 0.25);
    m1.quadraticBezierTo(size.width * 0.3, size.height * 0.18, size.width * 0.6, size.height * 0.26);
    m1.quadraticBezierTo(size.width * 0.85, size.height * 0.22, size.width, size.height * 0.28);
    m1.lineTo(size.width, size.height);
    m1.lineTo(0, size.height);
    m1.close();
    canvas.drawPath(m1, mountainPaint1);

    final mountainPaint2 = Paint()
      ..color = const Color(0xFF1E1611).withAlpha(120)
      ..style = PaintingStyle.fill;

    final m2 = Path();
    m2.moveTo(0, size.height * 0.55);
    m2.quadraticBezierTo(size.width * 0.35, size.height * 0.48, size.width * 0.7, size.height * 0.56);
    m2.quadraticBezierTo(size.width * 0.9, size.height * 0.52, size.width, size.height * 0.58);
    m2.lineTo(size.width, size.height);
    m2.lineTo(0, size.height);
    m2.close();
    canvas.drawPath(m2, mountainPaint2);

    // Vầng trăng vàng mờ trên đỉnh núi
    final moonPaint = Paint()
      ..color = const Color(0xFFFFE082).withAlpha(35)
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 24);
    canvas.drawCircle(Offset(size.width * 0.5, 140), 70, moonPaint);
  }

  void _paintSteppingStones(Canvas canvas, Path path) {
    final metrics = path.computeMetrics();
    final stonePaint = Paint()
      ..color = const Color(0xFF7A593D).withAlpha(160)
      ..style = PaintingStyle.fill;

    for (final metric in metrics) {
      const step = 28.0;
      for (double d = 10.0; d < metric.length - 10.0; d += step) {
        final tangent = metric.getTangentForOffset(d);
        if (tangent != null) {
          canvas.drawCircle(tangent.position, 2.5, stonePaint);
        }
      }
    }
  }

  void _paintConqueredTrail(Canvas canvas, List<Offset> nodes) {
    // Ải 1 -> Ải 5 (nodes 1 đến 6 trong allNodes)
    final conqueredPath = Path();
    conqueredPath.moveTo(nodes[0].dx, nodes[0].dy);

    // Điểm đến là Ải 5 (index 6 trong allNodes)
    const targetIdx = 6;
    for (int i = 0; i < targetIdx && i < nodes.length - 1; i++) {
      final p1 = nodes[i];
      final p2 = nodes[i + 1];
      final dy = p2.dy - p1.dy;
      final cp1 = Offset(p1.dx, p1.dy + dy * 0.45);
      final cp2 = Offset(p2.dx, p2.dy - dy * 0.45);
      conqueredPath.cubicTo(cp1.dx, cp1.dy, cp2.dx, cp2.dy, p2.dx, p2.dy);
    }

    // Luồng sáng hoàng kim mờ
    final glowPaint = Paint()
      ..color = const Color(0xFFFFB300).withAlpha(70)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 10.0
      ..strokeCap = StrokeCap.round
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 5);
    canvas.drawPath(conqueredPath, glowPaint);

    // Vạch năng lượng hoàng kim tâm lõi
    final corePaint = Paint()
      ..shader = const LinearGradient(
        colors: [Color(0xFFFFD54F), Color(0xFFFFA000), Color(0xFFFFD54F)],
      ).createShader(const Rect.fromLTWH(0, 900, 400, 1000))
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3.5
      ..strokeCap = StrokeCap.round;
    canvas.drawPath(conqueredPath, corePaint);
  }

  void _paintFogOfWar(Canvas canvas, Size size) {
    // Mây sương mù che phủ nhẹ từ ải 6 trở lên (y < 950)
    final fogPaint = Paint()
      ..color = const Color(0xFF795548).withAlpha(20)
      ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 35);

    canvas.drawCircle(Offset(size.width * 0.3, 750), 120, fogPaint);
    canvas.drawCircle(Offset(size.width * 0.8, 600), 140, fogPaint);
    canvas.drawCircle(Offset(size.width * 0.4, 400), 150, fogPaint);
    canvas.drawCircle(Offset(size.width * 0.7, 280), 130, fogPaint);
  }

  @override
  bool shouldRepaint(covariant _CurvedSagaPathPainter oldDelegate) {
    return oldDelegate.pulseValue != pulseValue;
  }
}
