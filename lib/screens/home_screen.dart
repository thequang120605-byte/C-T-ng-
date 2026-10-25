import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';
import '../theme/app_theme.dart';
import 'course_academy_screen.dart';
import 'game_screen.dart';
import 'master_games_screen.dart';
import 'profile_screen.dart';
import 'puzzle_rush_screen.dart';
import 'settings_screen.dart';

class HomeScreen extends StatefulWidget {
  final VoidCallback onGoToLobby;
  final VoidCallback onGoToLeaderboard;
  final VoidCallback onGoToPuzzle;

  const HomeScreen({
    super.key,
    required this.onGoToLobby,
    required this.onGoToLeaderboard,
    required this.onGoToPuzzle,
  });

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> with SingleTickerProviderStateMixin {
  late final AnimationController _pulseAnim;
  AuthUserDto? _user;
  LiveMatchDto? _liveMatch;
  final int _selectedDifficulty = 2;
  bool _isBusy = false;

  @override
  void initState() {
    super.initState();
    _pulseAnim = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1400),
    )..repeat(reverse: true);
    _loadUser();
    _loadLiveMatch();
  }

  @override
  void dispose() {
    _pulseAnim.dispose();
    super.dispose();
  }

  Future<void> _loadUser() async {
    try {
      final u = await AppServices.instance.auth.getCurrentUserAsync();
      if (mounted) setState(() => _user = u);
    } catch (_) {}
  }

  Future<void> _loadLiveMatch() async {
    try {
      final list = await AppServices.instance.api.getLiveMatchesAsync();
      if (mounted && list.isNotEmpty) {
        setState(() => _liveMatch = list.first);
      }
    } catch (_) {}
  }

  Future<void> _playWithAi(String timeControl) async {
    if (_isBusy) return;
    setState(() {
      _isBusy = true;
    });
    try {
      final game = await AppServices.instance.api.createOfflineGameAsync();
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GameScreen(
            gameId: game.id,
            difficulty: _selectedDifficulty,
            mode: 'PvE',
            seatHint: 'Red',
            timeControl: timeControl,
          ),
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFFC0392B),
            content: Text(e.toString(), style: GoogleFonts.philosopher()),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  void _showModeSelectionDialog({required String title, required Function(String timeControl) onSelect}) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF1E1714),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFFFD54F), width: 1.5),
        ),
        titlePadding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
        title: Center(
          child: Column(
            children: [
              Text(
                'CHỌN CHẾ ĐỘ CHƠI',
                style: GoogleFonts.cinzel(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: const Color(0xFFFFE082),
                  letterSpacing: 1.2,
                ),
              ),
              const SizedBox(height: 4),
              Container(
                width: 80,
                height: 2,
                color: const Color(0xFFFFD54F),
              ),
            ],
          ),
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            // Thẻ Cờ Chớp 5 Phút (Rồng vàng + Sét)
            GestureDetector(
              onTap: () {
                Navigator.pop(ctx);
                onSelect('Cờ Chớp');
              },
              child: Container(
                margin: const EdgeInsets.only(bottom: 12),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    colors: [Color(0xFF8D6E63), Color(0xFF4E342E)],
                  ),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFFFFD54F), width: 1.5),
                  boxShadow: const [
                    BoxShadow(color: Colors.black45, blurRadius: 4, offset: Offset(0, 2)),
                  ],
                ),
                child: Row(
                  children: [
                    Container(
                      width: 48,
                      height: 48,
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        gradient: LinearGradient(
                          colors: [Color(0xFFFFD54F), Color(0xFFF57C00)],
                        ),
                      ),
                      child: const Center(
                        child: Icon(Icons.flash_on, color: Colors.white, size: 28),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Text(
                                'CỜ CHỚP (5 Phút)',
                                style: GoogleFonts.philosopher(
                                  fontSize: 15,
                                  fontWeight: FontWeight.bold,
                                  color: const Color(0xFFFFE082),
                                ),
                              ),
                              const SizedBox(width: 6),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFD32F2F),
                                  borderRadius: BorderRadius.circular(4),
                                ),
                                child: Text(
                                  'HOT',
                                  style: GoogleFonts.philosopher(
                                    fontSize: 8,
                                    color: Colors.white,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 2),
                          Text(
                            '5 Phút + 3s • Nhanh - Kịch tính - Căng não',
                            style: GoogleFonts.philosopher(
                              fontSize: 11,
                              color: const Color(0xFFD7CCC8),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const Icon(Icons.arrow_forward_ios, size: 16, color: Color(0xFFFFD54F)),
                  ],
                ),
              ),
            ),

            // Thẻ Cờ Chậm 10 Phút (Ngọc bích + Cá Koi)
            GestureDetector(
              onTap: () {
                Navigator.pop(ctx);
                onSelect('Cờ Chậm');
              },
              child: Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    colors: [Color(0xFF00695C), Color(0xFF004D40)],
                  ),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFF80CBC4), width: 1.5),
                  boxShadow: const [
                    BoxShadow(color: Colors.black45, blurRadius: 4, offset: Offset(0, 2)),
                  ],
                ),
                child: Row(
                  children: [
                    Container(
                      width: 48,
                      height: 48,
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        gradient: LinearGradient(
                          colors: [Color(0xFF80CBC4), Color(0xFF00796B)],
                        ),
                      ),
                      child: const Center(
                        child: Icon(Icons.waves, color: Colors.white, size: 28),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'CỜ CHẬM (10 Phút)',
                            style: GoogleFonts.philosopher(
                              fontSize: 15,
                              fontWeight: FontWeight.bold,
                              color: const Color(0xFFE0F2F1),
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            '10 Phút + 5s • Thư thái - Sâu sắc - Tính toán',
                            style: GoogleFonts.philosopher(
                              fontSize: 11,
                              color: const Color(0xFFB2DFDB),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const Icon(Icons.arrow_forward_ios, size: 16, color: Color(0xFF80CBC4)),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showDailyRewardDialog() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF1E1714),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFFFD54F), width: 1.5),
        ),
        title: Center(
          child: Text(
            'QUÀ ĐĂNG NHẬP MỖI NGÀY',
            style: GoogleFonts.cinzel(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: const Color(0xFFFFE082),
            ),
          ),
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.card_giftcard, size: 48, color: Color(0xFFFFD54F)),
            const SizedBox(height: 10),
            Text(
              'Chúc mừng bạn đã điểm danh ngày thứ 7!\nPhần thưởng: 🪙 +5,000 Vàng & 💎 +10 Kim cương',
              textAlign: TextAlign.center,
              style: GoogleFonts.philosopher(
                color: AppColors.paperLight,
                fontSize: 13,
                height: 1.4,
              ),
            ),
          ],
        ),
        actions: [
          ElevatedButton(
            onPressed: () {
              Navigator.pop(ctx);
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  backgroundColor: const Color(0xFF2E7D32),
                  content: Text('Đã nhận thành công 5,000 Vàng!', style: GoogleFonts.philosopher()),
                ),
              );
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              side: const BorderSide(color: Color(0xFFFFD54F)),
            ),
            child: Text('Nhận Thưởng', style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.white)),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF140F0D),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // 1. TOP BAR TÀI NGUYÊN HOÀNG GIA (Avatar, Vàng, Ngọc, Settings)
              _buildTopResourceBar(),
              const SizedBox(height: 14),

              // 2. THẺ XEM TRẬN ĐẤU TRỰC TIẾP (Live Match Card)
              _buildLiveMatchCard(),
              const SizedBox(height: 14),

              // 2.5 THANH TIỆN ÍCH HOÀNG GIA (Thần Tốc, Kỳ Phổ Danh Thủ, Học Cờ)
              _buildRoyalFeaturesRow(),
              const SizedBox(height: 16),

              // Tiêu đề khối chế độ
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'CHẾ ĐỘ TRANH TÀI',
                    style: GoogleFonts.cinzel(
                      fontSize: 15,
                      fontWeight: FontWeight.bold,
                      color: AppColors.goldLight,
                      letterSpacing: 1.1,
                    ),
                  ),
                  Text(
                    'Chọn kỳ đài nhập cuộc',
                    style: GoogleFonts.philosopher(
                      fontSize: 11,
                      color: const Color(0xFF8D6E63),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),

              // 3. 4 KHỐI GAME CHẾ ĐỘ 3D (Cờ Tướng, Cờ Úp, Cờ Thế, Luyện Tập)
              GridView.count(
                crossAxisCount: 2,
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                mainAxisSpacing: 12,
                crossAxisSpacing: 12,
                childAspectRatio: 1.15,
                children: [
                  // Khối 1: CỜ TƯỚNG (Đỏ son hoàng gia)
                  _build3DModeCard(
                    title: 'CỜ TƯỚNG',
                    subtitle: 'Đấu trí kinh điển 1v1',
                    badge: 'PHỔ BIẾN',
                    badgeColor: const Color(0xFFFF9800),
                    char: '帥',
                    gradient: const [Color(0xFFBA2213), Color(0xFF6B1107)],
                    borderColor: const Color(0xFFFFD54F),
                    onTap: () {
                      _showModeSelectionDialog(
                        title: 'Cờ Tướng',
                        onSelect: (tc) => widget.onGoToLobby(),
                      );
                    },
                  ),

                  // Khối 2: CỜ ÚP (Lam ngọc bí ẩn)
                  _build3DModeCard(
                    title: 'CỜ ÚP',
                    subtitle: 'Biến ảo may rủi',
                    badge: 'HẤP DẪN',
                    badgeColor: const Color(0xFF00E676),
                    char: '?',
                    gradient: const [Color(0xFF1565C0), Color(0xFF0D47A1)],
                    borderColor: const Color(0xFF90CAF9),
                    onTap: () {
                      _showModeSelectionDialog(
                        title: 'Cờ Úp',
                        onSelect: (tc) => widget.onGoToLobby(),
                      );
                    },
                  ),

                  // Khối 3: CỜ THẾ (Vượt Ải Thủy Mặc - 10 Ải)
                  _build3DModeCard(
                    title: 'CỜ THẾ',
                    subtitle: '10 Ải giang hồ hiểm hóc',
                    badge: '10 ẢI',
                    badgeColor: const Color(0xFFE91E63),
                    char: '車',
                    gradient: const [Color(0xFF00695C), Color(0xFF004D40)],
                    borderColor: const Color(0xFF80CBC4),
                    onTap: widget.onGoToPuzzle,
                  ),

                  // Khối 4: LUYỆN TẬP (AI Thông Thái)
                  _build3DModeCard(
                    title: 'LUYỆN TẬP',
                    subtitle: 'Đấu máy 4 cấp độ',
                    badge: 'OFFLINE',
                    badgeColor: const Color(0xFF9C27B0),
                    char: '將',
                    gradient: const [Color(0xFF4A148C), Color(0xFF311B92)],
                    borderColor: const Color(0xFFCE93D8),
                    onTap: () {
                      _showModeSelectionDialog(
                        title: 'Luyện Tập Với Máy',
                        onSelect: (tc) => _playWithAi(tc),
                      );
                    },
                  ),
                ],
              ),

              const SizedBox(height: 16),

              // 4. BANNER SỰ KIỆN & PHẦN THƯỞNG
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    colors: [Color(0xFF3E2723), Color(0xFF271A15)],
                  ),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: const Color(0xFF8D6E63), width: 1),
                ),
                child: Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        color: Color(0xFFC0392B),
                      ),
                      child: const Icon(Icons.card_giftcard, color: Colors.white, size: 22),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Điểm Danh Nhận Vàng Hàng Ngày',
                            style: GoogleFonts.philosopher(
                              fontWeight: FontWeight.bold,
                              fontSize: 13,
                              color: const Color(0xFFFFE082),
                            ),
                          ),
                          Text(
                            'Nhận ngay 5,000 Vàng và 10 Kim cương miễn phí',
                            style: GoogleFonts.philosopher(
                              fontSize: 10,
                              color: const Color(0xFFBCAAA4),
                            ),
                          ),
                        ],
                      ),
                    ),
                    ElevatedButton(
                      onPressed: _showDailyRewardDialog,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFFFFD54F),
                        foregroundColor: Colors.black87,
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                        minimumSize: const Size(0, 32),
                      ),
                      child: Text('Nhận', style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, fontSize: 11)),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 20),
            ],
          ),
        ),
      ),
    );
  }

  // Thanh Tiện Ích Hoàng Gia (Thần Tốc, Kỳ Phổ Danh Thủ, Viện Kỳ Đạo)
  Widget _buildRoyalFeaturesRow() {
    return Row(
      children: [
        _buildFeaturePill(
          icon: Icons.bolt,
          label: 'Thần Tốc',
          color: const Color(0xFFFF9800),
          onTap: () {
            Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => PuzzleRushScreen(onBack: () => Navigator.pop(context))),
            );
          },
        ),
        const SizedBox(width: 8),
        _buildFeaturePill(
          icon: Icons.menu_book,
          label: 'Kỳ Phổ',
          color: const Color(0xFF00E676),
          onTap: () {
            Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => MasterGamesScreen(onBack: () => Navigator.pop(context))),
            );
          },
        ),
        const SizedBox(width: 8),
        _buildFeaturePill(
          icon: Icons.school,
          label: 'Viện Kỳ Đạo',
          color: const Color(0xFF40C4FF),
          onTap: () {
            Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => CourseAcademyScreen(onBack: () => Navigator.pop(context))),
            );
          },
        ),
      ],
    );
  }

  Widget _buildFeaturePill({
    required IconData icon,
    required String label,
    required Color color,
    required VoidCallback onTap,
  }) {
    return Expanded(
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          borderRadius: BorderRadius.circular(10),
          onTap: onTap,
          child: Container(
            padding: const EdgeInsets.symmetric(vertical: 8),
            decoration: BoxDecoration(
              color: const Color(0xFF1E1714),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: color.withValues(alpha: 0.6), width: 1.2),
              boxShadow: const [
                BoxShadow(color: Colors.black38, blurRadius: 4, offset: Offset(0, 2)),
              ],
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(icon, color: color, size: 16),
                const SizedBox(width: 5),
                Text(
                  label,
                  style: GoogleFonts.philosopher(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  // Top Bar Tài Nguyên Hoàng Gia
  Widget _buildTopResourceBar() {
    final playerName = _user?.username.isNotEmpty == true ? _user!.username : 'Quang Nguyễn Thế';

    void openProfile() async {
      await Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => ProfileScreen(
            onBackToHome: () => Navigator.pop(context),
          ),
        ),
      );
      _loadUser();
    }

    return Row(
      children: [
        // Avatar + Cấp bậc "Tập sự ⭐"
        GestureDetector(
          onTap: openProfile,
          child: Stack(
            alignment: Alignment.bottomCenter,
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: const LinearGradient(
                    colors: [Color(0xFF00796B), Color(0xFF004D40)],
                  ),
                  border: Border.all(color: const Color(0xFFFFD54F), width: 1.8),
                  boxShadow: const [
                    BoxShadow(color: Colors.black45, blurRadius: 4, offset: Offset(0, 2)),
                  ],
                ),
                child: Center(
                  child: Text(
                    playerName.isNotEmpty ? playerName[0].toUpperCase() : 'Q',
                    style: GoogleFonts.philosopher(
                      fontSize: 22,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                decoration: BoxDecoration(
                  color: const Color(0xFF6D4C41),
                  borderRadius: BorderRadius.circular(6),
                  border: Border.all(color: const Color(0xFFFFD54F), width: 0.8),
                ),
                child: Text(
                  'Tập sự ⭐',
                  style: GoogleFonts.philosopher(
                    fontSize: 8,
                    fontWeight: FontWeight.bold,
                    color: const Color(0xFFFFECB3),
                  ),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(width: 10),

        // Tên kỳ thủ & ELO
        Expanded(
          child: GestureDetector(
            onTap: openProfile,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  playerName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.philosopher(
                    fontWeight: FontWeight.bold,
                    fontSize: 13,
                    color: AppColors.paperLight,
                  ),
                ),
                Text(
                  '${_user?.elo ?? 1200} ELO',
                  style: GoogleFonts.cinzel(
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                    color: const Color(0xFFFFD54F),
                  ),
                ),
              ],
            ),
          ),
        ),

        // Vàng 🪙
        _buildCurrencyPill(
          icon: Icons.monetization_on,
          value: '120.000',
          color: const Color(0xFFFFD54F),
        ),
        const SizedBox(width: 6),

        // Kim Cương 💎
        _buildCurrencyPill(
          icon: Icons.diamond,
          value: '250',
          color: const Color(0xFF40C4FF),
        ),
        const SizedBox(width: 6),

        // Nút Cài đặt ⚙️
        IconButton(
          icon: const Icon(Icons.settings, color: Color(0xFFBCAAA4), size: 22),
          onPressed: () {
            Navigator.push(context, MaterialPageRoute(builder: (_) => const SettingsScreen()));
          },
        ),
      ],
    );
  }

  Widget _buildCurrencyPill({required IconData icon, required String value, required Color color}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 3),
      decoration: BoxDecoration(
        color: const Color(0xFF221A15),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFF5D4037), width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, color: color, size: 14),
          const SizedBox(width: 3),
          Text(
            value,
            style: GoogleFonts.cinzel(
              fontSize: 11,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
          const SizedBox(width: 3),
          Container(
            padding: const EdgeInsets.all(1),
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: color.withAlpha(50),
            ),
            child: Icon(Icons.add, size: 10, color: color),
          ),
        ],
      ),
    );
  }

  // Thẻ Trực Tiếp Trận Đấu
  Widget _buildLiveMatchCard() {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF2C1E18), Color(0xFF1E1410)],
        ),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFF8D6E63), width: 1.2),
        boxShadow: const [
          BoxShadow(color: Colors.black54, blurRadius: 6, offset: Offset(0, 3)),
        ],
      ),
      child: Row(
        children: [
          // Badge nhấp nháy TRỰC TIẾP
          AnimatedBuilder(
            animation: _pulseAnim,
            builder: (context, _) {
              return Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 3),
                decoration: BoxDecoration(
                  color: const Color(0xFFD32F2F)
                      .withAlpha((180 + _pulseAnim.value * 75).toInt()),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Container(
                      width: 6,
                      height: 6,
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        color: Colors.white,
                      ),
                    ),
                    const SizedBox(width: 4),
                    Text(
                      'TRỰC TIẾP',
                      style: GoogleFonts.philosopher(
                        fontSize: 9,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
          const SizedBox(width: 10),

          // Tên hai kỳ thủ đại chiến
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  _liveMatch != null ? 'Bàn ${_liveMatch!.roomCode}' : 'Kỳ Đài Đại Chiến #108',
                  style: GoogleFonts.cinzel(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: const Color(0xFFFFD54F),
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  _liveMatch != null
                      ? '${_liveMatch!.redPlayer} vs ${_liveMatch!.blackPlayer} (${_liveMatch!.moveCount} nước)'
                      : 'Độc Cô Cầu Bại (2850) vs Bắc Cái (2720)',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.philosopher(
                    fontSize: 11,
                    color: const Color(0xFFCFD8DC),
                  ),
                ),
              ],
            ),
          ),

          // Nút Vào Xem
          ElevatedButton(
            onPressed: () {
              if (_liveMatch != null) {
                Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => GameScreen(
                      gameId: _liveMatch!.id,
                      difficulty: 2,
                      mode: 'Online',
                      seatHint: '',
                    ),
                  ),
                );
              } else {
                widget.onGoToLobby();
              }
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              side: const BorderSide(color: Color(0xFFFFD54F), width: 1),
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              minimumSize: const Size(0, 32),
            ),
            child: Text(
              'Xem Ngay',
              style: GoogleFonts.philosopher(
                fontWeight: FontWeight.bold,
                fontSize: 11,
                color: Colors.white,
              ),
            ),
          ),
        ],
      ),
    );
  }

  // Khối Card 3D Chế Độ Chơi Cổ Phong
  Widget _build3DModeCard({
    required String title,
    required String subtitle,
    required String badge,
    required Color badgeColor,
    required String char,
    required List<Color> gradient,
    required Color borderColor,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: gradient,
          ),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: borderColor, width: 1.5),
          boxShadow: [
            BoxShadow(
              color: borderColor.withAlpha(40),
              blurRadius: 8,
              offset: const Offset(0, 3),
            ),
            const BoxShadow(
              color: Colors.black54,
              blurRadius: 6,
              offset: Offset(0, 3),
            ),
          ],
        ),
        padding: const EdgeInsets.all(12),
        child: Stack(
          children: [
            // Ký hiệu chữ Hán chìm mờ kích thước lớn phía sau
            Positioned(
              right: -6,
              bottom: -10,
              child: Text(
                char,
                style: GoogleFonts.maShanZheng(
                  fontSize: 72,
                  fontWeight: FontWeight.bold,
                  color: Colors.white.withAlpha(35),
                ),
              ),
            ),

            // Nội dung trên
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: badgeColor,
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        badge,
                        style: GoogleFonts.philosopher(
                          fontSize: 8,
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                        ),
                      ),
                    ),
                    Icon(Icons.arrow_outward, size: 14, color: borderColor),
                  ],
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: GoogleFonts.cinzel(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                        letterSpacing: 1.1,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      subtitle,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.philosopher(
                        fontSize: 10,
                        color: Colors.white70,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
