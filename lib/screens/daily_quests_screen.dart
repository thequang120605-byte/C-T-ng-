import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';
import '../services/sound_service.dart';
import '../theme/app_theme.dart';

class DailyQuestsScreen extends StatefulWidget {
  const DailyQuestsScreen({super.key});

  @override
  State<DailyQuestsScreen> createState() => _DailyQuestsScreenState();
}

class _DailyQuestsScreenState extends State<DailyQuestsScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  late List<DailyQuestDto> _quests;
  late List<PlayerAchievementDto> _achievements;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _loadData();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);

    try {
      final remoteQuests = await AppServices.instance.api.getDailyQuestsAsync();
      final remoteAchievements = await AppServices.instance.api.getAchievementsAsync();

      if (mounted) {
        setState(() {
          _quests = remoteQuests.isNotEmpty ? remoteQuests : _buildFallbackQuests();
          _achievements = remoteAchievements.isNotEmpty ? remoteAchievements : _buildFallbackAchievements();
          _isLoading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _quests = _buildFallbackQuests();
          _achievements = _buildFallbackAchievements();
          _isLoading = false;
        });
      }
    }
  }

  List<DailyQuestDto> _buildFallbackQuests() {
    return [
      DailyQuestDto(
        id: 'login',
        title: '☀️ Điểm Danh Kỳ Viện',
        description: 'Đăng nhập vào game ngày hôm nay',
        rewardGold: 1000,
        rewardGems: 2,
        target: 1,
        current: 1,
        isClaimed: false,
      ),
      DailyQuestDto(
        id: 'rush',
        title: '⚡ Thần Tốc Phá Ải',
        description: 'Giải đúng 3 thế cờ trong Thần Tốc',
        rewardGold: 1500,
        rewardGems: 5,
        target: 3,
        current: 1,
        isClaimed: false,
      ),
      DailyQuestDto(
        id: 'ai',
        title: '🤖 Thử Thách Trí Tuệ',
        description: 'Chiến thắng 1 ván đấu với AI',
        rewardGold: 2000,
        rewardGems: 10,
        target: 1,
        current: 0,
        isClaimed: false,
      ),
      DailyQuestDto(
        id: 'academy',
        title: '🎓 Cầu Đạo Kỳ Nghệ',
        description: 'Hoàn thành 1 bài học tại Viện Kỳ Đạo',
        rewardGold: 1000,
        rewardGems: 3,
        target: 1,
        current: 1,
        isClaimed: false,
      ),
      DailyQuestDto(
        id: 'master',
        title: '📜 Nghiên Cứu Đại Sư',
        description: 'Xem lại 1 kỳ phổ của Đại Sư giang hồ',
        rewardGold: 1000,
        rewardGems: 2,
        target: 1,
        current: 1,
        isClaimed: false,
      ),
      DailyQuestDto(
        id: 'chat',
        title: '💬 Giao Lưu Hảo Hữu',
        description: 'Gửi 1 tin nhắn nhanh hoặc emote trong trận',
        rewardGold: 500,
        rewardGems: 1,
        target: 1,
        current: 1,
        isClaimed: false,
      ),
    ];
  }

  List<PlayerAchievementDto> _buildFallbackAchievements() {
    return const [
      PlayerAchievementDto(
        id: 'novice',
        title: 'Tân Thủ Nhập Môn',
        icon: '🥉',
        description: 'Tham gia ván cờ đầu tiên trong sự nghiệp',
        isUnlocked: true,
        badgeName: 'Tập Sự',
      ),
      PlayerAchievementDto(
        id: 'rush_5',
        title: 'Tia Chớp Bàn Cờ',
        icon: '⚡',
        description: 'Đạt chuỗi 5 câu đúng liên tục trong Thần Tốc',
        isUnlocked: false,
        badgeName: 'Kỳ Thủ Nhanh',
      ),
      PlayerAchievementDto(
        id: 'scholar',
        title: 'Thông Tuệ Binh Pháp',
        icon: '📚',
        description: 'Vượt qua 5 bài kiểm tra trắc nghiệm Viện Kỳ Đạo',
        isUnlocked: true,
        badgeName: 'Kỳ Sĩ',
      ),
      PlayerAchievementDto(
        id: 'ai_slayer',
        title: 'Thiết Mã Phá Trận',
        icon: '🥋',
        description: 'Kích bại AI ở độ khó Kiện Tướng',
        isUnlocked: false,
        badgeName: 'Chiến Tướng',
      ),
      PlayerAchievementDto(
        id: 'grandmaster',
        title: 'Tiêu Diêu Kỳ Thánh',
        icon: '👑',
        description: 'Đạt 1500 ELO và tích lũy 50 ván thắng',
        isUnlocked: false,
        badgeName: 'Kỳ Thánh',
      ),
    ];
  }

  Future<void> _claimReward(DailyQuestDto quest) async {
    if (quest.isClaimed || !quest.isCompleted) return;

    // Gửi yêu cầu nhận thưởng lên Backend REST API
    await AppServices.instance.api.claimQuestAsync(quest.id);

    SoundService.instance.playWin();

    SoundService.instance.playWin();

    setState(() {
      quest.isClaimed = true;
    });

    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: const Color(0xFF2E7D32),
          behavior: SnackBarBehavior.floating,
          content: Row(
            children: [
              const Icon(Icons.stars, color: Color(0xFFFFD54F)),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Nhận thành công +${quest.rewardGold} Vàng & +${quest.rewardGems} Kim Cương!',
                  style: GoogleFonts.philosopher(
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                  ),
                ),
              ),
            ],
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.darkBg,
      appBar: AppBar(
        backgroundColor: const Color(0xFF221A15),
        elevation: 0,
        title: Text(
          'NHIỆM VỤ & DANH HIỆU',
          style: GoogleFonts.cinzel(
            color: const Color(0xFFFFD54F),
            fontWeight: FontWeight.bold,
            letterSpacing: 1.5,
          ),
        ),
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: const Color(0xFFFFD54F),
          indicatorWeight: 3,
          labelColor: const Color(0xFFFFD54F),
          unselectedLabelColor: const Color(0xFFBCAAA4),
          labelStyle: GoogleFonts.philosopher(fontWeight: FontWeight.bold),
          tabs: const [
            Tab(icon: Icon(Icons.assignment), text: 'Nhiệm Vụ Ngày'),
            Tab(icon: Icon(Icons.military_tech), text: 'Danh Hiệu & Cúp'),
          ],
        ),
      ),
      body: _isLoading
          ? const Center(
              child: CircularProgressIndicator(color: Color(0xFFFFD54F)),
            )
          : TabBarView(
              controller: _tabController,
              children: [
                _buildQuestsTab(),
                _buildAchievementsTab(),
              ],
            ),
    );
  }

  Widget _buildQuestsTab() {
    final completedCount = _quests.where((q) => q.isClaimed).length;
    final totalCount = _quests.length;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Tổng quan tiến độ
        Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            gradient: const LinearGradient(
              colors: [Color(0xFF3E2723), Color(0xFF1D1410)],
            ),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: const Color(0xFF8D6E63), width: 1.2),
          ),
          child: Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: const BoxDecoration(
                  shape: BoxShape.circle,
                  color: Color(0xFF5D4037),
                ),
                child: const Center(
                  child: Icon(Icons.emoji_events, color: Color(0xFFFFD54F), size: 28),
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Tiến độ ngày: $completedCount/$totalCount hoàn thành',
                      style: GoogleFonts.philosopher(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: const Color(0xFFFFECB3),
                      ),
                    ),
                    const SizedBox(height: 6),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(
                        value: totalCount > 0 ? completedCount / totalCount : 0.0,
                        backgroundColor: const Color(0xFF271A15),
                        valueColor: const AlwaysStoppedAnimation<Color>(
                          Color(0xFFFFD54F),
                        ),
                        minHeight: 6,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 16),

        // Danh sách nhiệm vụ
        ..._quests.map((q) => _buildQuestCard(q)),
      ],
    );
  }

  Widget _buildQuestCard(DailyQuestDto quest) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFF271A15),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: quest.isClaimed
              ? const Color(0xFF4E342E)
              : quest.isCompleted
                  ? const Color(0xFFFFD54F)
                  : const Color(0xFF5D4037),
          width: quest.isCompleted && !quest.isClaimed ? 1.5 : 1.0,
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  quest.title,
                  style: GoogleFonts.philosopher(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: quest.isClaimed
                        ? const Color(0xFF8D6E63)
                        : Colors.white,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  quest.description,
                  style: GoogleFonts.philosopher(
                    fontSize: 11,
                    color: const Color(0xFFBCAAA4),
                  ),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Text(
                      '🪙 +${quest.rewardGold}',
                      style: GoogleFonts.cinzel(
                        fontSize: 11,
                        color: const Color(0xFFFFD54F),
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text(
                      '💎 +${quest.rewardGems}',
                      style: GoogleFonts.cinzel(
                        fontSize: 11,
                        color: const Color(0xFF40C4FF),
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Text(
                      '(${quest.current}/${quest.target})',
                      style: GoogleFonts.cinzel(
                        fontSize: 10,
                        color: const Color(0xFF8D6E63),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          if (quest.isClaimed)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: const Color(0xFF1E140F),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                'ĐÃ NHẬN',
                style: GoogleFonts.cinzel(
                  fontSize: 10,
                  fontWeight: FontWeight.bold,
                  color: const Color(0xFF8D6E63),
                ),
              ),
            )
          else if (quest.isCompleted)
            ElevatedButton(
              onPressed: () => _claimReward(quest),
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFFFFD54F),
                foregroundColor: Colors.black87,
                elevation: 4,
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              child: Text(
                'NHẬN',
                style: GoogleFonts.philosopher(
                  fontWeight: FontWeight.bold,
                  fontSize: 12,
                ),
              ),
            )
          else
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: const Color(0xFF3E2723),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                'CHƯA XONG',
                style: GoogleFonts.cinzel(
                  fontSize: 10,
                  color: const Color(0xFFBCAAA4),
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildAchievementsTab() {
    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: _achievements.length,
      itemBuilder: (context, index) {
        final a = _achievements[index];
        return Container(
          margin: const EdgeInsets.only(bottom: 12),
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: a.isUnlocked
                ? const Color(0xFF2E1C14)
                : const Color(0xFF1E140F),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: a.isUnlocked
                  ? const Color(0xFFFFD54F)
                  : const Color(0xFF4E342E),
              width: a.isUnlocked ? 1.4 : 1.0,
            ),
          ),
          child: Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: a.isUnlocked
                      ? const Color(0xFF5D4037)
                      : const Color(0xFF271A15),
                  border: Border.all(
                    color: a.isUnlocked
                        ? const Color(0xFFFFD54F)
                        : const Color(0xFF5D4037),
                  ),
                ),
                child: Center(
                  child: Text(
                    a.icon,
                    style: TextStyle(
                      fontSize: 24,
                      color: a.isUnlocked ? null : Colors.grey,
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Text(
                          a.title,
                          style: GoogleFonts.philosopher(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: a.isUnlocked
                                ? const Color(0xFFFFE082)
                                : const Color(0xFF8D6E63),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 6,
                            vertical: 1,
                          ),
                          decoration: BoxDecoration(
                            color: a.isUnlocked
                                ? const Color(0xFFC0392B)
                                : const Color(0xFF3E2723),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            a.badgeName,
                            style: GoogleFonts.philosopher(
                              fontSize: 9,
                              fontWeight: FontWeight.bold,
                              color: Colors.white,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      a.description,
                      style: GoogleFonts.philosopher(
                        fontSize: 11,
                        color: const Color(0xFFBCAAA4),
                      ),
                    ),
                  ],
                ),
              ),
              if (a.isUnlocked)
                const Icon(Icons.check_circle, color: Color(0xFF00E676), size: 20)
              else
                const Icon(Icons.lock_outline, color: Color(0xFF5D4037), size: 20),
            ],
          ),
        );
      },
    );
  }
}
