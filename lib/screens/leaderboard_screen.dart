import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';
import '../theme/app_theme.dart';

class LeaderboardScreen extends StatefulWidget {
  final VoidCallback onBackToHome;

  const LeaderboardScreen({super.key, required this.onBackToHome});

  @override
  State<LeaderboardScreen> createState() => _LeaderboardScreenState();
}

class _LeaderboardScreenState extends State<LeaderboardScreen> {
  int _selectedTab = 0; // 0: Cờ Tướng (Elo), 1: Cờ Thế (Giải đố)
  bool _isLoading = true;
  String? _errorMessage;

  List<LeaderboardItemDto> _classicRanks = [];
  List<LeaderboardItemDto> _puzzleRanks = [];
  AuthUserDto? _currentUser;

  @override
  void initState() {
    super.initState();
    _loadAllData();
  }

  Future<void> _loadAllData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      // Tải thông tin người chơi hiện tại
      _currentUser = await AppServices.instance.auth.getCurrentUserAsync();

      // Tải dữ liệu thật từ Backend API
      final mode = _selectedTab == 0 ? 'classic' : 'puzzle';
      final list = await AppServices.instance.api.getLeaderboardAsync(
        mode: mode,
        limit: 25,
      );

      if (mounted) {
        setState(() {
          if (_selectedTab == 0) {
            _classicRanks = list;
          } else {
            _puzzleRanks = list;
          }
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _errorMessage = 'Không thể kết nối máy chủ để tải bảng xếp hạng.';
        });
      }
    }
  }

  void _onTabChanged(int index) {
    if (_selectedTab == index) return;
    setState(() {
      _selectedTab = index;
    });
    // Nếu tab chưa có dữ liệu thì tải ngay
    if ((_selectedTab == 0 && _classicRanks.isEmpty) ||
        (_selectedTab == 1 && _puzzleRanks.isEmpty)) {
      _loadAllData();
    }
  }

  @override
  Widget build(BuildContext context) {
    final list = _selectedTab == 0 ? _classicRanks : _puzzleRanks;

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
                  IconButton(
                    icon: const Icon(Icons.refresh, color: AppColors.goldLight, size: 22),
                    onPressed: _loadAllData,
                  ),
                ],
              ),
            ),

            // Tab chọn Cờ Tướng / Cờ Thế dạng cuộn thư
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
                  _buildTopTab('KỲ THỦ ELO', 0),
                  _buildTopTab('GIẢI CỜ THẾ', 1),
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
                            width: 65,
                            child: Text(
                              _selectedTab == 0 ? 'ELO' : 'ĐIỂM SỐ',
                              textAlign: TextAlign.center,
                              style: GoogleFonts.cinzel(
                                fontWeight: FontWeight.bold,
                                fontSize: 11,
                                color: AppColors.ink,
                              ),
                            ),
                          ),
                          SizedBox(
                            width: 65,
                            child: Text(
                              _selectedTab == 0 ? 'TỶ LỆ' : 'ĐÃ GIẢI',
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
                      child: _isLoading
                          ? const Center(
                              child: CircularProgressIndicator(color: AppColors.gold),
                            )
                          : (_errorMessage != null
                              ? Center(
                                  child: Padding(
                                    padding: const EdgeInsets.all(20),
                                    child: Column(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        const Icon(Icons.cloud_off, color: Color(0xFF8D6E63), size: 40),
                                        const SizedBox(height: 10),
                                        Text(
                                          _errorMessage!,
                                          textAlign: TextAlign.center,
                                          style: GoogleFonts.philosopher(
                                            color: AppColors.ink,
                                            fontSize: 13,
                                          ),
                                        ),
                                        const SizedBox(height: 10),
                                        ElevatedButton(
                                          style: ElevatedButton.styleFrom(
                                            backgroundColor: const Color(0xFF8D6E63),
                                          ),
                                          onPressed: _loadAllData,
                                          child: const Text('Thử Lại', style: TextStyle(color: Colors.white)),
                                        ),
                                      ],
                                    ),
                                  ),
                                )
                              : (list.isEmpty
                                  ? Center(
                                      child: Text(
                                        'Chưa có dữ liệu kỳ thủ nào.',
                                        style: GoogleFonts.philosopher(
                                          color: AppColors.muted,
                                          fontSize: 14,
                                        ),
                                      ),
                                    )
                                  : ListView.separated(
                                      padding: const EdgeInsets.symmetric(vertical: 4),
                                      itemCount: list.length,
                                      separatorBuilder: (_, _) => const Divider(
                                        height: 1,
                                        color: Color(0xFFDDCDB3),
                                        indent: 14,
                                        endIndent: 14,
                                      ),
                                      itemBuilder: (context, idx) {
                                        final item = list[idx];
                                        return _buildRankRow(item);
                                      },
                                    ))),
                    ),
                  ],
                ),
              ),
            ),

            // Ghim Hàng Kỳ Thủ Bản Thân Dưới Đáy (Dữ liệu thật từ Auth Profile)
            _buildMyRankFooter(),
          ],
        ),
      ),
    );
  }

  Widget _buildTopTab(String title, int index) {
    final isSelected = (_selectedTab == index);

    return Expanded(
      child: GestureDetector(
        onTap: () => _onTabChanged(index),
        child: Container(
          decoration: BoxDecoration(
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
                letterSpacing: 0.8,
              ),
            ),
          ),
        ),
      ),
    );
  }

  Color _getAvatarColor(String username) {
    final colors = [
      const Color(0xFFC0392B),
      const Color(0xFF2980B9),
      const Color(0xFF27AE60),
      const Color(0xFF8E44AD),
      const Color(0xFFD35400),
      const Color(0xFF16A085),
      const Color(0xFF2C3E50),
    ];
    return colors[username.hashCode.abs() % colors.length];
  }

  Widget _buildRankRow(LeaderboardItemDto item) {
    final isTop3 = item.rank <= 3;
    final crownColor = item.rank == 1
        ? const Color(0xFFFFD54F)
        : (item.rank == 2 ? const Color(0xFFE0E0E0) : const Color(0xFFCD7F32));
    final avatarColor = _getAvatarColor(item.username);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
      color: item.isCurrentUser ? const Color(0xFFFFE082).withAlpha(80) : Colors.transparent,
      child: Row(
        children: [
          // Cột Hạng
          SizedBox(
            width: 44,
            child: isTop3
                ? Stack(
                    alignment: Alignment.center,
                    children: [
                      Icon(Icons.shield, color: crownColor, size: 28),
                      Text(
                        '${item.rank}',
                        style: GoogleFonts.cinzel(
                          fontWeight: FontWeight.bold,
                          fontSize: 12,
                          color: Colors.black87,
                        ),
                      ),
                    ],
                  )
                : Text(
                    '${item.rank}',
                    style: GoogleFonts.cinzel(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFF795548),
                    ),
                  ),
          ),

          // Avatar & Tên & Danh hiệu
          Container(
            width: 32,
            height: 32,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: avatarColor,
              border: Border.all(
                color: isTop3 ? crownColor : Colors.black12,
                width: 1.5,
              ),
            ),
            child: Center(
              child: Text(
                item.username.isNotEmpty ? item.username[0].toUpperCase() : 'K',
                style: GoogleFonts.cinzel(
                  fontSize: 13,
                  fontWeight: FontWeight.bold,
                  color: Colors.white,
                ),
              ),
            ),
          ),
          const SizedBox(width: 8),

          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Flexible(
                      child: Text(
                        item.username,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.philosopher(
                          fontSize: 13,
                          fontWeight: FontWeight.bold,
                          color: item.isCurrentUser ? const Color(0xFFB71C1C) : AppColors.ink,
                        ),
                      ),
                    ),
                    if (item.isCurrentUser) ...[
                      const SizedBox(width: 4),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                        decoration: BoxDecoration(
                          color: const Color(0xFFC0392B),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: const Text(
                          'Bạn',
                          style: TextStyle(fontSize: 9, color: Colors.white, fontWeight: FontWeight.bold),
                        ),
                      ),
                    ],
                  ],
                ),
                Text(
                  item.title,
                  style: GoogleFonts.philosopher(
                    fontSize: 10,
                    color: const Color(0xFF8D6E63),
                  ),
                ),
              ],
            ),
          ),

          // Cột Elo hoặc Điểm số
          SizedBox(
            width: 65,
            child: Text(
              _selectedTab == 0 ? '${item.elo}' : '${item.score}',
              textAlign: TextAlign.center,
              style: GoogleFonts.cinzel(
                fontSize: 12.5,
                fontWeight: FontWeight.bold,
                color: const Color(0xFF8D5B1B),
              ),
            ),
          ),

          // Cột Tỷ Lệ Thắng hoặc Số Bài Đã Giải
          SizedBox(
            width: 65,
            child: Text(
              _selectedTab == 0 ? item.winRate : '${item.solvedCount} bài',
              textAlign: TextAlign.right,
              style: GoogleFonts.philosopher(
                fontSize: 11.5,
                fontWeight: FontWeight.bold,
                color: const Color(0xFF2E7D32),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMyRankFooter() {
    final user = _currentUser;
    final username = user?.username.isNotEmpty == true ? user!.username : 'Kỳ thủ khách';
    final elo = user?.elo ?? 1200;
    final wins = user?.wins ?? 0;
    final total = (user?.wins ?? 0) + (user?.losses ?? 0) + (user?.draws ?? 0);
    final winRate = total > 0 ? '${(wins * 100 / total).toStringAsFixed(1)}%' : '0%';

    return Container(
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
                user != null ? '★' : '?',
                style: GoogleFonts.cinzel(
                  fontWeight: FontWeight.bold,
                  color: Colors.white,
                  fontSize: 12,
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
                  '$username (Bạn)',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.philosopher(
                    fontWeight: FontWeight.bold,
                    fontSize: 13,
                    color: const Color(0xFFFFE082),
                  ),
                ),
                Text(
                  user != null ? 'Tài khoản chính thức' : 'Chưa đăng nhập',
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
                '$elo ELO',
                style: GoogleFonts.cinzel(
                  fontWeight: FontWeight.bold,
                  fontSize: 13,
                  color: const Color(0xFFFFD54F),
                ),
              ),
              Text(
                'Thắng: $winRate',
                style: GoogleFonts.philosopher(
                  fontSize: 10,
                  color: const Color(0xFF81C784),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
