import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';
import '../theme/app_theme.dart';
import 'login_screen.dart';

class ProfileScreen extends StatefulWidget {
  final VoidCallback onBackToHome;

  const ProfileScreen({super.key, required this.onBackToHome});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> with SingleTickerProviderStateMixin {
  AuthUserDto? _user;
  List<MatchHistoryItemDto> _matches = [];
  bool _isLoading = true;
  int _selectedTab = 0; // 0: Chiến Tích, 1: Lịch Sử Đấu

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  Future<void> _loadProfile() async {
    setState(() => _isLoading = true);
    try {
      final user = await AppServices.instance.auth.fetchUserProfileAsync();
      final matches = await AppServices.instance.api.getMatchHistoryAsync(
        userId: user?.id != null && user!.id > 0 ? user.id : null,
      );
      if (mounted) {
        setState(() {
          _user = user;
          _matches = matches;
        });
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  String _getRankTitle(int elo) {
    if (elo >= 2600) return 'Kỳ Thánh';
    if (elo >= 2400) return 'Đại Sư';
    if (elo >= 2200) return 'Kiện Tướng';
    if (elo >= 1800) return 'Kỳ Thủ Cấp 1';
    if (elo >= 1500) return 'Kỳ Thủ';
    return 'Tập Sự';
  }

  Color _getRankColor(int elo) {
    if (elo >= 2600) return const Color(0xFFFFD54F);
    if (elo >= 2400) return const Color(0xFFFF8A65);
    if (elo >= 2200) return const Color(0xFFBA68C8);
    if (elo >= 1800) return const Color(0xFF4FC3F7);
    if (elo >= 1500) return const Color(0xFF81C784);
    return const Color(0xFFB0BEC5);
  }

  Future<void> _confirmLogout() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF1E1714),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFFFD54F), width: 1.5),
        ),
        title: Row(
          children: [
            const Icon(Icons.logout, color: Color(0xFFE53935)),
            const SizedBox(width: 8),
            Text(
              'ĐĂNG XUẤT',
              style: GoogleFonts.cinzel(
                color: const Color(0xFFFFE082),
                fontWeight: FontWeight.bold,
                fontSize: 16,
              ),
            ),
          ],
        ),
        content: Text(
          'Các hạ có chắc chắn muốn đăng xuất khỏi tài khoản hiện tại không?',
          style: GoogleFonts.philosopher(color: AppColors.paperLight, fontSize: 13),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text('Hủy', style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4))),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              side: const BorderSide(color: Color(0xFFFFD54F)),
            ),
            onPressed: () => Navigator.pop(ctx, true),
            child: Text('Đăng Xuất', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );

    if (confirm == true) {
      await AppServices.instance.auth.logoutAsync();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: const Color(0xFF2E7D32),
          content: Text('Đã đăng xuất thành công!', style: GoogleFonts.philosopher()),
        ),
      );
      _loadProfile();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isLoggedIn = _user != null && _user!.id > 0;
    final username = isLoggedIn ? _user!.username : 'Kỳ thủ Khách';
    final email = isLoggedIn ? _user!.email : 'Chưa đăng nhập tài khoản';
    final elo = _user?.elo ?? 1200;
    final rankTitle = _getRankTitle(elo);
    final rankColor = _getRankColor(elo);
    final wins = _user?.wins ?? 0;
    final losses = _user?.losses ?? 0;
    final draws = _user?.draws ?? 0;
    final totalGames = wins + losses + draws;
    final winRate = totalGames > 0
        ? ((wins * 1000 ~/ totalGames) / 10.0).toStringAsFixed(1)
        : '0.0';

    return Scaffold(
      backgroundColor: const Color(0xFF140F0D),
      body: SafeArea(
        child: _isLoading
            ? const Center(child: CircularProgressIndicator(color: Color(0xFFFFD54F)))
            : SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 600),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        // 1. TOP HEADER HOÀNG GIA
                        _buildTopHeader(),
                        const SizedBox(height: 14),

                        // 2. HERO CARD HỒ SƠ KỲ THỦ
                        _buildProfileHeroCard(
                          username: username,
                          email: email,
                          elo: elo,
                          rankTitle: rankTitle,
                          rankColor: rankColor,
                          isLoggedIn: isLoggedIn,
                        ),
                        const SizedBox(height: 16),

                        // 3. THANH TAB CHUYỂN ĐỔI (Chiến Tích / Lịch Sử Đấu)
                        _buildTabBar(),
                        const SizedBox(height: 14),

                        // 4. NỘI DUNG THEO TAB
                        if (_selectedTab == 0)
                          _buildStatsTab(wins, draws, losses, totalGames, winRate, elo)
                        else
                          _buildMatchHistoryTab(),

                        const SizedBox(height: 20),

                        // 5. NÚT ĐIỀU HƯỚNG / ĐĂNG XUẤT / ĐĂNG NHẬP
                        _buildFooterActions(isLoggedIn),
                      ],
                    ),
                  ),
                ),
              ),
      ),
    );
  }

  // Header trên cùng
  Widget _buildTopHeader() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        GestureDetector(
          onTap: widget.onBackToHome,
          child: Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: const LinearGradient(
                colors: [Color(0xFF3E2723), Color(0xFF271A15)],
              ),
              border: Border.all(color: const Color(0xFF8D6E63), width: 1.2),
            ),
            child: const Icon(Icons.arrow_back_ios_new, size: 16, color: Color(0xFFFFD54F)),
          ),
        ),
        Text(
          'HỒ SƠ KỲ THỦ',
          style: GoogleFonts.cinzel(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: const Color(0xFFFFE082),
            letterSpacing: 1.5,
          ),
        ),
        GestureDetector(
          onTap: _loadProfile,
          child: Container(
            width: 38,
            height: 38,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: const LinearGradient(
                colors: [Color(0xFF3E2723), Color(0xFF271A15)],
              ),
              border: Border.all(color: const Color(0xFF8D6E63), width: 1.2),
            ),
            child: const Icon(Icons.refresh, size: 18, color: Color(0xFFFFD54F)),
          ),
        ),
      ],
    );
  }

  // Hero Card thông tin kỳ thủ
  Widget _buildProfileHeroCard({
    required String username,
    required String email,
    required int elo,
    required String rankTitle,
    required Color rankColor,
    required bool isLoggedIn,
  }) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF2C1E18), Color(0xFF1E1511), Color(0xFF140F0D)],
        ),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFFFD54F), width: 1.5),
        boxShadow: const [
          BoxShadow(color: Colors.black54, blurRadius: 10, offset: Offset(0, 4)),
        ],
      ),
      child: Column(
        children: [
          Stack(
            alignment: Alignment.bottomCenter,
            children: [
              Container(
                width: 72,
                height: 72,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: const LinearGradient(
                    colors: [Color(0xFFBA2213), Color(0xFF6B1107)],
                  ),
                  border: Border.all(color: const Color(0xFFFFD54F), width: 2.5),
                  boxShadow: const [
                    BoxShadow(color: Colors.black54, blurRadius: 6, offset: Offset(0, 3)),
                  ],
                ),
                child: Center(
                  child: Text(
                    username.isNotEmpty ? username[0].toUpperCase() : '帥',
                    style: GoogleFonts.maShanZheng(
                      fontSize: 34,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFFFFE082),
                    ),
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    colors: [rankColor.withValues(alpha: 0.9), const Color(0xFF212121)],
                  ),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: const Color(0xFFFFD54F), width: 1),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.military_tech, size: 10, color: rankColor),
                    const SizedBox(width: 2),
                    Text(
                      rankTitle,
                      style: GoogleFonts.philosopher(
                        fontSize: 9,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            username,
            style: GoogleFonts.philosopher(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: const Color(0xFFFFE082),
            ),
          ),
          const SizedBox(height: 3),
          Text(
            email,
            style: GoogleFonts.philosopher(
              fontSize: 12,
              color: const Color(0xFFBCAAA4),
            ),
          ),
          const SizedBox(height: 14),

          // Huy hiệu Elo & Cờ thế
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              _buildBadgePill(Icons.shield, 'Hệ số Elo', '$elo', const Color(0xFFFFD54F)),
              const SizedBox(width: 12),
              _buildBadgePill(Icons.auto_stories, 'Cờ Thế', '${_user?.puzzleScore ?? 0}đ', const Color(0xFF80CBC4)),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildBadgePill(IconData icon, String label, String value, Color accentColor) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
      decoration: BoxDecoration(
        color: const Color(0xFF18120F),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: accentColor.withValues(alpha: 0.5), width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: accentColor),
          const SizedBox(width: 6),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: GoogleFonts.philosopher(fontSize: 9, color: const Color(0xFF8D6E63)),
              ),
              Text(
                value,
                style: GoogleFonts.cinzel(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: accentColor,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  // Thanh tab chuyển đổi
  Widget _buildTabBar() {
    return Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: const Color(0xFF1E1714),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFF3E2723), width: 1),
      ),
      child: Row(
        children: [
          Expanded(
            child: _buildTabButton(0, 'CHIẾN TÍCH', Icons.emoji_events),
          ),
          Expanded(
            child: _buildTabButton(1, 'LỊCH SỬ ĐẤU', Icons.history_edu),
          ),
        ],
      ),
    );
  }

  Widget _buildTabButton(int index, String title, IconData icon) {
    final isSelected = _selectedTab == index;
    return GestureDetector(
      onTap: () => setState(() => _selectedTab = index),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 8),
        decoration: BoxDecoration(
          gradient: isSelected
              ? const LinearGradient(colors: [Color(0xFFBA2213), Color(0xFF8E0000)])
              : null,
          borderRadius: BorderRadius.circular(8),
          border: isSelected ? Border.all(color: const Color(0xFFFFD54F), width: 1) : null,
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 15, color: isSelected ? const Color(0xFFFFD54F) : const Color(0xFF8D6E63)),
            const SizedBox(width: 6),
            Text(
              title,
              style: GoogleFonts.philosopher(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: isSelected ? Colors.white : const Color(0xFF8D6E63),
              ),
            ),
          ],
        ),
      ),
    );
  }

  // Tab 1: Thống Kê Chiến Tích
  Widget _buildStatsTab(int wins, int draws, int losses, int totalGames, String winRate, int elo) {
    return Column(
      children: [
        // Grid 4 chỉ số
        GridView.count(
          crossAxisCount: 4,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          crossAxisSpacing: 8,
          mainAxisSpacing: 8,
          childAspectRatio: 1.0,
          children: [
            _buildStatCard('Tổng ván', '$totalGames', const Color(0xFFFFD54F)),
            _buildStatCard('Thắng', '$wins', const Color(0xFF66BB6A)),
            _buildStatCard('Hòa', '$draws', const Color(0xFFFFA726)),
            _buildStatCard('Thua', '$losses', const Color(0xFFEF5350)),
          ],
        ),
        const SizedBox(height: 12),

        // Thanh tỷ lệ thắng trận
        Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: const Color(0xFF1E1714),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: const Color(0xFF4E342E), width: 1),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Tỷ Lệ Thắng Trận',
                    style: GoogleFonts.philosopher(fontSize: 13, color: const Color(0xFFD7CCC8)),
                  ),
                  Text(
                    '$winRate%',
                    style: GoogleFonts.cinzel(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFF66BB6A),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              ClipRRect(
                borderRadius: BorderRadius.circular(6),
                child: LinearProgressIndicator(
                  value: totalGames > 0 ? (wins / totalGames) : 0.0,
                  minHeight: 8,
                  backgroundColor: const Color(0xFF2C1E18),
                  valueColor: const AlwaysStoppedAnimation<Color>(Color(0xFF66BB6A)),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildStatCard(String label, String value, Color color) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF1E1714),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: const Color(0xFF3E2723), width: 1),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Text(
            label,
            style: GoogleFonts.philosopher(fontSize: 10, color: const Color(0xFF8D6E63)),
          ),
          const SizedBox(height: 4),
          Text(
            value,
            style: GoogleFonts.cinzel(
              fontSize: 17,
              fontWeight: FontWeight.bold,
              color: color,
            ),
          ),
        ],
      ),
    );
  }

  // Tab 2: Lịch Sử Đấu Thật từ Backend
  Widget _buildMatchHistoryTab() {
    if (_matches.isEmpty) {
      return Container(
        padding: const EdgeInsets.symmetric(vertical: 36, horizontal: 16),
        decoration: BoxDecoration(
          color: const Color(0xFF1E1714),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: const Color(0xFF3E2723), width: 1),
        ),
        child: Column(
          children: [
            const Icon(Icons.history, size: 42, color: Color(0xFF5D4037)),
            const SizedBox(height: 10),
            Text(
              'Chưa có lịch sử ván cờ nào',
              style: GoogleFonts.philosopher(fontSize: 14, color: const Color(0xFF8D6E63)),
            ),
            const SizedBox(height: 4),
            Text(
              'Hãy bắt đầu ván đấu đầu tiên để ghi danh vào kỳ phổ!',
              textAlign: TextAlign.center,
              style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFF5D4037)),
            ),
          ],
        ),
      );
    }

    return ListView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: _matches.length,
      itemBuilder: (context, idx) {
        final m = _matches[idx];
        final isWin = m.outcome.toLowerCase() == 'win';
        final isLoss = m.outcome.toLowerCase() == 'loss';

        final outcomeColor = isWin
            ? const Color(0xFF66BB6A)
            : (isLoss ? const Color(0xFFEF5350) : const Color(0xFFFFA726));
        final outcomeIcon = isWin ? Icons.check_circle : (isLoss ? Icons.cancel : Icons.remove_circle);

        return Container(
          margin: const EdgeInsets.only(bottom: 8),
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          decoration: BoxDecoration(
            color: const Color(0xFF1E1714),
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: const Color(0xFF3E2723), width: 1),
          ),
          child: Row(
            children: [
              // Badge Thắng/Thua/Hòa
              Container(
                width: 38,
                height: 38,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: outcomeColor.withValues(alpha: 0.15),
                  border: Border.all(color: outcomeColor, width: 1.2),
                ),
                child: Center(
                  child: Icon(outcomeIcon, color: outcomeColor, size: 20),
                ),
              ),
              const SizedBox(width: 12),

              // Thông tin ván đấu
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Text(
                          'vs ${m.opponentName}',
                          style: GoogleFonts.philosopher(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                        const SizedBox(width: 6),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                          decoration: BoxDecoration(
                            color: const Color(0xFF2C1E18),
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: Text(
                            m.gameMode,
                            style: GoogleFonts.philosopher(fontSize: 9, color: const Color(0xFFFFD54F)),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${m.playedAt} • ${m.totalMoves} nước • Quân ${m.myColor == "Red" ? "Đỏ" : "Đen"}',
                      style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFF8D6E63)),
                    ),
                  ],
                ),
              ),

              // Kết quả Text
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: outcomeColor.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  m.outcomeText,
                  style: GoogleFonts.philosopher(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: outcomeColor,
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  // Footer Actions
  Widget _buildFooterActions(bool isLoggedIn) {
    return Column(
      children: [
        if (isLoggedIn)
          ElevatedButton.icon(
            icon: const Icon(Icons.logout, size: 18, color: Colors.white),
            label: Text(
              'Đăng Xuất Tài Khoản',
              style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.white),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFF7B1FA2).withValues(alpha: 0.8),
              side: const BorderSide(color: Color(0xFFCE93D8), width: 1),
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
            onPressed: _confirmLogout,
          )
        else
          ElevatedButton.icon(
            icon: const Icon(Icons.login, size: 18, color: Colors.black87),
            label: Text(
              'Đăng Nhập / Đăng Ký',
              style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.black87),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFFFD54F),
              side: const BorderSide(color: Color(0xFFFFE082), width: 1),
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
            onPressed: () async {
              final ok = await Navigator.push<bool>(
                context,
                MaterialPageRoute(builder: (_) => const LoginScreen()),
              );
              if (ok == true) _loadProfile();
            },
          ),
        const SizedBox(height: 10),
        TextButton(
          onPressed: widget.onBackToHome,
          child: Text(
            'Quay lại Trang Chủ',
            style: GoogleFonts.philosopher(color: const Color(0xFF8D6E63), fontSize: 13),
          ),
        ),
      ],
    );
  }
}
