import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../theme/app_theme.dart';

class LeaderboardScreen extends StatefulWidget {
  final VoidCallback onBackToHome;

  const LeaderboardScreen({super.key, required this.onBackToHome});

  @override
  State<LeaderboardScreen> createState() => _LeaderboardScreenState();
}

class _LeaderboardScreenState extends State<LeaderboardScreen> {
  int _selectedTab = 0; // 0: Cờ Tướng, 1: Cờ Úp

  final List<_RankUser> _classicRanks = [
    _RankUser(
      rank: 1,
      name: 'Độc Cô Cầu Bại',
      title: 'Kỳ Thánh',
      elo: 2850,
      winRate: '88.5%',
      isCrown: true,
      crownColor: Color(0xFFFFD54F),
      avatarColor: Color(0xFFC0392B),
    ),
    _RankUser(
      rank: 2,
      name: 'Bắc Cái Hồng Thất',
      title: 'Đại Sư',
      elo: 2720,
      winRate: '82.1%',
      isCrown: true,
      crownColor: Color(0xFFE0E0E0),
      avatarColor: Color(0xFF2980B9),
    ),
    _RankUser(
      rank: 3,
      name: 'Đông Tà Hoàng Dược',
      title: 'Đại Sư',
      elo: 2650,
      winRate: '79.4%',
      isCrown: true,
      crownColor: Color(0xFFCD7F32),
      avatarColor: Color(0xFF27AE60),
    ),
    _RankUser(
      rank: 4,
      name: 'Tây Độc Âu Dương',
      title: 'Kiện Tướng',
      elo: 2510,
      winRate: '75.2%',
      avatarColor: Color(0xFF8E44AD),
    ),
    _RankUser(
      rank: 5,
      name: 'Nam Đế Đoàn Trí',
      title: 'Kiện Tướng',
      elo: 2480,
      winRate: '73.8%',
      avatarColor: Color(0xFFD35400),
    ),
    _RankUser(
      rank: 6,
      name: 'Lệnh Hồ Xung',
      title: 'Kỳ Thủ',
      elo: 2390,
      winRate: '70.5%',
      avatarColor: Color(0xFF16A085),
    ),
    _RankUser(
      rank: 7,
      name: 'Trương Vô Kỵ',
      title: 'Kỳ Thủ',
      elo: 2320,
      winRate: '68.0%',
      avatarColor: Color(0xFF2C3E50),
    ),
  ];

  final List<_RankUser> _upRanks = [
    _RankUser(
      rank: 1,
      name: 'Thiên Ngoại Phi Tiên',
      title: 'Úp Vương',
      elo: 2790,
      winRate: '84.2%',
      isCrown: true,
      crownColor: Color(0xFFFFD54F),
      avatarColor: Color(0xFF8E44AD),
    ),
    _RankUser(
      rank: 2,
      name: 'Ẩn Danh Cao Thủ',
      title: 'Đại Sư',
      elo: 2680,
      winRate: '80.0%',
      isCrown: true,
      crownColor: Color(0xFFE0E0E0),
      avatarColor: Color(0xFF16A085),
    ),
    _RankUser(
      rank: 3,
      name: 'Biến Huyễn Vô Song',
      title: 'Kiện Tướng',
      elo: 2590,
      winRate: '76.3%',
      isCrown: true,
      crownColor: Color(0xFFCD7F32),
      avatarColor: Color(0xFFC0392B),
    ),
  ];

  @override
  Widget build(BuildContext context) {
    final list = _selectedTab == 0 ? _classicRanks : _upRanks;

    return Scaffold(
      backgroundColor: const Color(0xFF16120F),
      body: SafeArea(
        child: Column(
          children: [
            // Top Bar
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
                    'BẢNG XẾP HẠNG',
                    style: GoogleFonts.cinzel(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: AppColors.goldLight,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const Spacer(),
                  const Icon(Icons.military_tech, color: Color(0xFFFFD54F), size: 26),
                ],
              ),
            ),

            // Tab chọn Cờ Tướng / Cờ Úp dạng cuộn thư
            Container(
              height: 42,
              margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              decoration: BoxDecoration(
                color: const Color(0xFF281E19),
                borderRadius: BorderRadius.circular(22),
                border: Border.all(color: const Color(0xFF5D4037), width: 1.2),
              ),
              child: Row(
                children: [
                  _buildTopTab('CỜ TƯỚNG', 0),
                  _buildTopTab('CỜ ÚP', 1),
                ],
              ),
            ),

            // Cuộn thư Hoàng Gia (Imperial Scroll Container)
            Expanded(
              child: Container(
                margin: const EdgeInsets.symmetric(horizontal: 14),
                decoration: BoxDecoration(
                  color: AppColors.paperBg,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: const Color(0xFFBCA67F), width: 2),
                  boxShadow: const [
                    BoxShadow(
                      color: Colors.black54,
                      blurRadius: 10,
                      offset: Offset(0, 4),
                    ),
                  ],
                ),
                child: Column(
                  children: [
                    // Tiêu đề bảng thư tịch
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      decoration: const BoxDecoration(
                        color: Color(0xFFE4D3B4),
                        borderRadius: BorderRadius.vertical(top: Radius.circular(12)),
                        border: Border(
                          bottom: BorderSide(color: Color(0xFFBCA67F), width: 1.2),
                        ),
                      ),
                      child: Row(
                        children: [
                          SizedBox(
                            width: 44,
                            child: Text(
                              'HẠNG',
                              style: GoogleFonts.philosopher(
                                fontWeight: FontWeight.bold,
                                fontSize: 11,
                                color: AppColors.ink,
                              ),
                            ),
                          ),
                          Expanded(
                            child: Text(
                              'KỲ THỦ',
                              style: GoogleFonts.philosopher(
                                fontWeight: FontWeight.bold,
                                fontSize: 11,
                                color: AppColors.ink,
                              ),
                            ),
                          ),
                          SizedBox(
                            width: 60,
                            child: Text(
                              'ELO',
                              textAlign: TextAlign.center,
                              style: GoogleFonts.cinzel(
                                fontWeight: FontWeight.bold,
                                fontSize: 11,
                                color: AppColors.ink,
                              ),
                            ),
                          ),
                          SizedBox(
                            width: 60,
                            child: Text(
                              'THẮNG',
                              textAlign: TextAlign.right,
                              style: GoogleFonts.philosopher(
                                fontWeight: FontWeight.bold,
                                fontSize: 11,
                                color: AppColors.ink,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),

                    // Danh sách thứ hạng
                    Expanded(
                      child: ListView.separated(
                        padding: const EdgeInsets.symmetric(vertical: 4),
                        itemCount: list.length,
                        separatorBuilder: (_, _) => const Divider(
                          height: 1,
                          color: Color(0xFFDDCDB3),
                          indent: 14,
                          endIndent: 14,
                        ),
                        itemBuilder: (context, idx) {
                          final user = list[idx];
                          return _buildRankRow(user);
                        },
                      ),
                    ),
                  ],
                ),
              ),
            ),

            // Ghim Hàng Kỳ Thủ Bản Thân Dưới Đáy
            Container(
              margin: const EdgeInsets.all(12),
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [Color(0xFF3E2723), Color(0xFF1E1714)],
                ),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFFFD54F), width: 1.2),
                boxShadow: const [
                  BoxShadow(
                    color: Colors.black45,
                    blurRadius: 6,
                    offset: Offset(0, 2),
                  ),
                ],
              ),
              child: Row(
                children: [
                  Container(
                    width: 32,
                    height: 32,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(color: const Color(0xFFFFD54F), width: 1),
                      color: const Color(0xFF00796B),
                    ),
                    child: Center(
                      child: Text(
                        '128',
                        style: GoogleFonts.cinzel(
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                          fontSize: 11,
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
                          'Quang Nguyễn Thế (Bạn)',
                          style: GoogleFonts.philosopher(
                            fontWeight: FontWeight.bold,
                            fontSize: 13,
                            color: const Color(0xFFFFE082),
                          ),
                        ),
                        Text(
                          'Cấp bậc: Tập sự ⭐',
                          style: GoogleFonts.philosopher(
                            fontSize: 10,
                            color: const Color(0xFFBCAAA4),
                          ),
                        ),
                      ],
                    ),
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        '1200 ELO',
                        style: GoogleFonts.cinzel(
                          fontWeight: FontWeight.bold,
                          fontSize: 13,
                          color: const Color(0xFFFFD54F),
                        ),
                      ),
                      Text(
                        'Thắng: 65%',
                        style: GoogleFonts.philosopher(
                          fontSize: 10,
                          color: const Color(0xFF81C784),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTopTab(String title, int index) {
    final isSelected = _selectedTab == index;
    return Expanded(
      child: GestureDetector(
        onTap: () => setState(() => _selectedTab = index),
        child: Container(
          decoration: BoxDecoration(
            color: isSelected ? const Color(0xFFD49B4B) : Colors.transparent,
            borderRadius: BorderRadius.circular(20),
            gradient: isSelected
                ? const LinearGradient(
                    colors: [Color(0xFFE5A855), Color(0xFFB57A2A)],
                  )
                : null,
          ),
          child: Center(
            child: Text(
              title,
              style: GoogleFonts.cinzel(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: isSelected ? Colors.black87 : const Color(0xFFBCAAA4),
                letterSpacing: 1.1,
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildRankRow(_RankUser user) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
      child: Row(
        children: [
          // Cột Thứ hạng (Có vương miện cho Top 1-3)
          SizedBox(
            width: 44,
            child: user.isCrown
                ? Row(
                    children: [
                      Icon(Icons.workspace_premium, size: 20, color: user.crownColor),
                      Text(
                        '${user.rank}',
                        style: GoogleFonts.cinzel(
                          fontWeight: FontWeight.bold,
                          fontSize: 12,
                          color: user.crownColor,
                        ),
                      ),
                    ],
                  )
                : Text(
                    '${user.rank}',
                    style: GoogleFonts.cinzel(
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
                      color: AppColors.inkMuted,
                    ),
                  ),
          ),

          // Avatar + Tên + Danh hiệu
          Expanded(
            child: Row(
              children: [
                CircleAvatar(
                  radius: 14,
                  backgroundColor: user.avatarColor,
                  child: Text(
                    user.name[0],
                    style: GoogleFonts.philosopher(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        user.name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.philosopher(
                          fontWeight: FontWeight.bold,
                          fontSize: 13,
                          color: AppColors.ink,
                        ),
                      ),
                      Text(
                        user.title,
                        style: GoogleFonts.philosopher(
                          fontSize: 9,
                          color: const Color(0xFF8D6E63),
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),

          // ELO
          SizedBox(
            width: 60,
            child: Text(
              '${user.elo}',
              textAlign: TextAlign.center,
              style: GoogleFonts.cinzel(
                fontWeight: FontWeight.bold,
                fontSize: 12,
                color: user.rank <= 3 ? const Color(0xFFB71C1C) : AppColors.ink,
              ),
            ),
          ),

          // Tỷ lệ thắng
          SizedBox(
            width: 60,
            child: Text(
              user.winRate,
              textAlign: TextAlign.right,
              style: GoogleFonts.philosopher(
                fontWeight: FontWeight.bold,
                fontSize: 11,
                color: const Color(0xFF2E7D32),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _RankUser {
  final int rank;
  final String name;
  final String title;
  final int elo;
  final String winRate;
  final bool isCrown;
  final Color crownColor;
  final Color avatarColor;

  _RankUser({
    required this.rank,
    required this.name,
    required this.title,
    required this.elo,
    required this.winRate,
    this.isCrown = false,
    this.crownColor = Colors.grey,
    required this.avatarColor,
  });
}
