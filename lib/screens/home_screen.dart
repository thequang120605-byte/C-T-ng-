import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';
import '../theme/app_theme.dart';
import 'course_academy_screen.dart';
import 'daily_quests_screen.dart';
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

  Future<void> _playWithAiSelected(int depth, String side, String timeControl) async {
    if (_isBusy) return;
    setState(() => _isBusy = true);
    try {
      final game = await AppServices.instance.api.createPveGameAsync(difficulty: depth, side: side);
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GameScreen(
            gameId: game.id,
            difficulty: depth,
            mode: 'PvE',
            seatHint: side,
            timeControl: timeControl,
          ),
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFFC0392B),
            content: Text('Lỗi khởi tạo ván đấu AI: $e', style: GoogleFonts.philosopher()),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _playWithAi(String timeControl) => _playWithAiSelected(_selectedDifficulty, 'Red', timeControl);

  Future<void> _playAiVsAiSelected({required int redDepth, required int blackDepth, required int speedMs}) async {
    if (_isBusy) return;
    setState(() => _isBusy = true);
    try {
      final game = await AppServices.instance.api.createAiVsAiGameAsync();
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GameScreen(
            gameId: game.id,
            difficulty: blackDepth,
            mode: 'AiVsAi',
            seatHint: 'Red',
            timeControl: 'AI vs AI',
            aiVsAiRedDepth: redDepth,
            aiVsAiBlackDepth: blackDepth,
            aiVsAiSpeedMs: speedMs,
          ),
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFFC0392B),
            content: Text('Lỗi khởi tạo AI vs AI: $e', style: GoogleFonts.philosopher()),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _playFromFen(String fen) async {
    if (_isBusy) return;
    setState(() => _isBusy = true);
    try {
      final game = await AppServices.instance.api.createGameFromFenAsync(fen);
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GameScreen(
            gameId: game.id,
            difficulty: _selectedDifficulty,
            mode: 'PvE',
            seatHint: 'Red',
            timeControl: 'Nạp FEN',
          ),
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFFC0392B),
            content: Text('Lỗi nạp FEN: $e', style: GoogleFonts.philosopher()),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _playDailyChallenge() async {
    if (_isBusy) return;
    setState(() => _isBusy = true);
    try {
      final puzzle = await AppServices.instance.api.getDailyPuzzleAsync();
      if (!mounted) return;
      if (puzzle != null) {
        final key = (puzzle['key'] ?? puzzle['id'] ?? '1').toString();
        final title = puzzle['title']?.toString() ?? 'Thử Thách Cờ Thế Hôm Nay';
        final goal = puzzle['description']?.toString() ?? 'Chiếu bí trong các nước';
        await Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => GameScreen(
              gameId: 0,
              puzzleKey: key,
              puzzleTitle: title,
              puzzleGoal: goal,
              timeControl: 'Cờ Thế',
            ),
          ),
        );
      } else {
        widget.onGoToPuzzle();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi tải thử thách hôm nay: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _playCoUp({required bool isPassAndPlay}) async {
    if (_isBusy) return;
    setState(() => _isBusy = true);
    try {
      final game = await AppServices.instance.api.createCoUpGameAsync(isPassAndPlay: isPassAndPlay);
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GameScreen(
            gameId: game.id,
            difficulty: _selectedDifficulty,
            mode: isPassAndPlay ? 'PassAndPlay' : 'CoUp',
            seatHint: 'Red',
            timeControl: 'Cờ Úp 10 Phút',
          ),
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFFC0392B),
            content: Text('Lỗi khởi tạo Cờ Úp: $e', style: GoogleFonts.philosopher()),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _playPassAndPlay(String timeControl) async {
    if (_isBusy) return;
    setState(() => _isBusy = true);
    try {
      final game = await AppServices.instance.api.createPassAndPlayGameAsync();
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GameScreen(
            gameId: game.id,
            difficulty: 1,
            mode: 'PassAndPlay',
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
            content: Text('Lỗi khởi tạo ván cờ: $e', style: GoogleFonts.philosopher()),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  void _showCoUpChoiceModal() {
    showModalBottomSheet(
      context: context,
      backgroundColor: const Color(0xFF1E1714),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
        side: BorderSide(color: Color(0xFF90CAF9), width: 1.5),
      ),
      builder: (ctx) => Container(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.help_outline, color: Color(0xFF90CAF9)),
                const SizedBox(width: 8),
                Text(
                  'CHẾ ĐỘ CỜ ÚP BÍ ẨN',
                  style: GoogleFonts.cinzel(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: const Color(0xFF90CAF9),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            ListTile(
              leading: const Icon(Icons.smart_toy, color: Color(0xFFFFD54F)),
              title: Text('Đấu Máy Cờ Úp (AI)', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
              subtitle: Text('Rèn luyện cờ úp với máy thông minh, lật quân ngẫu nhiên chuẩn luật', style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4), fontSize: 11)),
              onTap: () {
                Navigator.pop(ctx);
                _playCoUp(isPassAndPlay: false);
              },
            ),
            ListTile(
              leading: const Icon(Icons.people, color: Color(0xFF00E676)),
              title: Text('Đấu Đôi Cờ Úp Tại Chỗ (Pass & Play)', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
              subtitle: Text('2 người chơi trên cùng 1 máy, lật quân đối kháng trực diện', style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4), fontSize: 11)),
              onTap: () {
                Navigator.pop(ctx);
                _playCoUp(isPassAndPlay: true);
              },
            ),
            ListTile(
              leading: const Icon(Icons.public, color: Color(0xFF40C4FF)),
              title: Text('Vào Sảnh Trực Tuyến Cờ Úp', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
              subtitle: Text('Tìm phòng và thách đấu các kỳ thủ cờ úp khắp nơi', style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4), fontSize: 11)),
              onTap: () {
                Navigator.pop(ctx);
                widget.onGoToLobby();
              },
            ),
          ],
        ),
      ),
    );
  }

  void _showPracticeChoiceModal() {
    showModalBottomSheet(
      context: context,
      backgroundColor: const Color(0xFF1E1714),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
        side: BorderSide(color: Color(0xFFCE93D8), width: 1.5),
      ),
      builder: (ctx) => Container(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.sports_esports, color: Color(0xFFCE93D8)),
                const SizedBox(width: 8),
                Text(
                  'CHẾ ĐỘ NGOẠI TUYẾN',
                  style: GoogleFonts.cinzel(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: const Color(0xFFCE93D8),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            ListTile(
              leading: const Icon(Icons.smart_toy, color: Color(0xFFFFD54F)),
              title: Text('Đấu Với Máy 4 Cấp Độ (AI)', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
              subtitle: Text('Tập sự, Nghiệp dư, Kiện tướng, Thần sầu (kèm Gợi ý AI Coach)', style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4), fontSize: 11)),
              onTap: () {
                Navigator.pop(ctx);
                _showModeSelectionDialog(
                  title: 'Luyện Tập Với Máy',
                  onSelect: (tc) => _playWithAi(tc),
                );
              },
            ),
            ListTile(
              leading: const Icon(Icons.screen_rotation, color: Color(0xFF00E676)),
              title: Text('Đấu Đôi Tại Chỗ (Pass & Play - 2 Người)', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
              subtitle: Text('2 người chơi trên 1 máy, hỗ trợ xoay ngược quân 180° đối diện', style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4), fontSize: 11)),
              onTap: () {
                Navigator.pop(ctx);
                _showModeSelectionDialog(
                  title: 'Đấu Đôi Pass & Play',
                  onSelect: (tc) => _playPassAndPlay(tc),
                );
              },
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildDailyChallengeCard() {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF3E2723), Color(0xFF1B0000)],
        ),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFFFD54F), width: 1.2),
        boxShadow: const [
          BoxShadow(color: Colors.black54, blurRadius: 6, offset: Offset(0, 3)),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              gradient: LinearGradient(
                colors: [Color(0xFFFFD54F), Color(0xFFE65100)],
              ),
            ),
            child: const Center(
              child: Icon(Icons.military_tech, color: Colors.white, size: 26),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(
                      'THỬ THÁCH HÔM NAY',
                      style: GoogleFonts.cinzel(
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                        color: const Color(0xFFFFE082),
                      ),
                    ),
                    const SizedBox(width: 6),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                      decoration: BoxDecoration(
                        color: const Color(0xFF2E7D32),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        '+20 Elo',
                        style: GoogleFonts.philosopher(
                          fontSize: 9,
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  'Sát cục kinh điển • Phá thế giang hồ từ API',
                  style: GoogleFonts.philosopher(
                    fontSize: 11,
                    color: const Color(0xFFD7CCC8),
                  ),
                ),
              ],
            ),
          ),
          ElevatedButton(
            onPressed: _playDailyChallenge,
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              minimumSize: const Size(0, 32),
              side: const BorderSide(color: Color(0xFFFFD54F), width: 1),
            ),
            child: Text(
              'Phá Ải',
              style: GoogleFonts.philosopher(
                fontWeight: FontWeight.bold,
                fontSize: 11,
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _showXiangqiModeSelectionSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: const Color(0xFF1A1411),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        side: BorderSide(color: Color(0xFFFFD54F), width: 1.5),
      ),
      builder: (sheetCtx) {
        int selectedMode = 1; // 0: PvP, 1: PvE, 2: Online, 3: AiVsAi
        int pveDepth = 2; // 1: Dễ, 2: Trung bình, 3: Khó, 4: Siêu khó
        String pveSide = 'Red'; // 'Red' or 'Black'
        String timeControl = 'Cờ Chậm'; // 'Cờ Chớp' or 'Cờ Chậm'
        int redDepth = 2;
        int blackDepth = 3;
        int speedMs = 800;
        bool rotateBlack = false;

        return StatefulBuilder(
          builder: (ctx, setSheetState) {
            return Container(
              height: MediaQuery.of(sheetCtx).size.height * 0.88,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              child: Column(
                children: [
                  Center(
                    child: Container(
                      width: 40,
                      height: 4,
                      margin: const EdgeInsets.only(bottom: 10),
                      decoration: BoxDecoration(
                        color: const Color(0xFF8D6E63),
                        borderRadius: BorderRadius.circular(2),
                      ),
                    ),
                  ),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: const Color(0xFF2C1E18),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: const Color(0xFFFFD54F), width: 1),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(Icons.shield, color: Color(0xFFFFD54F), size: 14),
                            const SizedBox(width: 6),
                            Text(
                              'CỜ TƯỚNG VIỆT NAM',
                              style: GoogleFonts.cinzel(
                                fontSize: 11,
                                fontWeight: FontWeight.bold,
                                color: const Color(0xFFFFE082),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'CHỌN CHẾ ĐỘ CHƠI',
                    style: GoogleFonts.cinzel(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                      letterSpacing: 1.1,
                    ),
                  ),
                  Text(
                    'Hãy chọn cách bạn muốn tham gia ván cờ',
                    style: GoogleFonts.philosopher(
                      fontSize: 12,
                      color: const Color(0xFFBCAAA4),
                    ),
                  ),
                  const SizedBox(height: 12),

                  GridView.count(
                    crossAxisCount: 2,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    mainAxisSpacing: 8,
                    crossAxisSpacing: 8,
                    childAspectRatio: 2.1,
                    children: [
                      _buildModeButton(
                        title: 'Người vs Người',
                        subtitle: 'Pass & Play tại chỗ',
                        icon: Icons.people,
                        isSelected: selectedMode == 0,
                        onTap: () => setSheetState(() => selectedMode = 0),
                      ),
                      _buildModeButton(
                        title: 'Người vs Máy',
                        subtitle: 'AI Minimax 4 cấp độ',
                        icon: Icons.smart_toy,
                        isSelected: selectedMode == 1,
                        onTap: () => setSheetState(() => selectedMode = 1),
                      ),
                      _buildModeButton(
                        title: 'Chơi qua mạng',
                        subtitle: 'Phòng chờ & mã bí mật',
                        icon: Icons.public,
                        isSelected: selectedMode == 2,
                        onTap: () => setSheetState(() => selectedMode = 2),
                      ),
                      _buildModeButton(
                        title: 'AI vs AI',
                        subtitle: 'Hai máy tự đối đầu',
                        icon: Icons.memory,
                        isSelected: selectedMode == 3,
                        onTap: () => setSheetState(() => selectedMode = 3),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),

                  Expanded(
                    child: SingleChildScrollView(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (selectedMode == 1) ...[
                            _buildSectionHeader('ĐỘ KHÓ CỦA MÁY (MINIMAX DEPTH)', Icons.psychology),
                            const SizedBox(height: 6),
                            Row(
                              children: [
                                _buildDepthBtn(
                                  depth: 1,
                                  name: 'Dễ',
                                  label: 'Depth 1',
                                  icon: Icons.eco,
                                  color: const Color(0xFF22C55E),
                                  isSelected: pveDepth == 1,
                                  onTap: () => setSheetState(() => pveDepth = 1),
                                ),
                                const SizedBox(width: 6),
                                _buildDepthBtn(
                                  depth: 2,
                                  name: 'T.Bình',
                                  label: 'Depth 2',
                                  icon: Icons.shield,
                                  color: const Color(0xFFF59E0B),
                                  isSelected: pveDepth == 2,
                                  onTap: () => setSheetState(() => pveDepth = 2),
                                ),
                                const SizedBox(width: 6),
                                _buildDepthBtn(
                                  depth: 3,
                                  name: 'Khó',
                                  label: 'Depth 3',
                                  icon: Icons.local_fire_department,
                                  color: const Color(0xFFF97316),
                                  isSelected: pveDepth == 3,
                                  onTap: () => setSheetState(() => pveDepth = 3),
                                ),
                                const SizedBox(width: 6),
                                _buildDepthBtn(
                                  depth: 4,
                                  name: 'Thần Sầu',
                                  label: 'Depth 4',
                                  icon: Icons.local_fire_department,
                                  color: const Color(0xFFEF4444),
                                  isSelected: pveDepth == 4,
                                  onTap: () => setSheetState(() => pveDepth = 4),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            _buildSectionHeader('CHỌN BÊN THI ĐẤU', Icons.flag),
                            const SizedBox(height: 6),
                            Row(
                              children: [
                                Expanded(
                                  child: GestureDetector(
                                    onTap: () => setSheetState(() => pveSide = 'Red'),
                                    child: Container(
                                      padding: const EdgeInsets.symmetric(vertical: 8),
                                      decoration: BoxDecoration(
                                        color: pveSide == 'Red' ? const Color(0xFFBA2213) : const Color(0xFF221A15),
                                        borderRadius: BorderRadius.circular(8),
                                        border: Border.all(
                                          color: pveSide == 'Red' ? const Color(0xFFFFD54F) : const Color(0xFF5D4037),
                                        ),
                                      ),
                                      child: Center(
                                        child: Text(
                                          'Quân Đỏ (Đi trước)',
                                          style: GoogleFonts.philosopher(
                                            color: Colors.white,
                                            fontWeight: FontWeight.bold,
                                            fontSize: 12,
                                          ),
                                        ),
                                      ),
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: GestureDetector(
                                    onTap: () => setSheetState(() => pveSide = 'Black'),
                                    child: Container(
                                      padding: const EdgeInsets.symmetric(vertical: 8),
                                      decoration: BoxDecoration(
                                        color: pveSide == 'Black' ? const Color(0xFF374151) : const Color(0xFF221A15),
                                        borderRadius: BorderRadius.circular(8),
                                        border: Border.all(
                                          color: pveSide == 'Black' ? const Color(0xFFFFD54F) : const Color(0xFF5D4037),
                                        ),
                                      ),
                                      child: Center(
                                        child: Text(
                                          'Quân Đen (Đi sau)',
                                          style: GoogleFonts.philosopher(
                                            color: Colors.white,
                                            fontWeight: FontWeight.bold,
                                            fontSize: 12,
                                          ),
                                        ),
                                      ),
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            _buildSectionHeader('THỜI GIAN THI ĐẤU', Icons.timer),
                            const SizedBox(height: 6),
                            Row(
                              children: [
                                Expanded(
                                  child: _buildTimeChip(
                                    label: 'Cờ Chớp (5 Phút)',
                                    isSelected: timeControl == 'Cờ Chớp',
                                    onTap: () => setSheetState(() => timeControl = 'Cờ Chớp'),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: _buildTimeChip(
                                    label: 'Cờ Chậm (10 Phút)',
                                    isSelected: timeControl == 'Cờ Chậm',
                                    onTap: () => setSheetState(() => timeControl = 'Cờ Chậm'),
                                  ),
                                ),
                              ],
                            ),
                          ] else if (selectedMode == 0) ...[
                            _buildSectionHeader('THỜI GIAN THI ĐẤU', Icons.timer),
                            const SizedBox(height: 6),
                            Row(
                              children: [
                                Expanded(
                                  child: _buildTimeChip(
                                    label: 'Cờ Chớp (5 Phút)',
                                    isSelected: timeControl == 'Cờ Chớp',
                                    onTap: () => setSheetState(() => timeControl = 'Cờ Chớp'),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: _buildTimeChip(
                                    label: 'Cờ Chậm (10 Phút)',
                                    isSelected: timeControl == 'Cờ Chậm',
                                    onTap: () => setSheetState(() => timeControl = 'Cờ Chậm'),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            CheckboxListTile(
                              contentPadding: EdgeInsets.zero,
                              title: Text(
                                'Xoay ngược quân Đen 180°',
                                style: GoogleFonts.philosopher(color: Colors.white, fontSize: 13),
                              ),
                              subtitle: Text(
                                'Thuận tiện khi 2 người ngồi đối diện nhau',
                                style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4), fontSize: 11),
                              ),
                              value: rotateBlack,
                              activeColor: const Color(0xFFFFD54F),
                              checkColor: Colors.black,
                              onChanged: (val) => setSheetState(() => rotateBlack = val ?? false),
                            ),
                          ] else if (selectedMode == 2) ...[
                            Container(
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: const Color(0xFF221A15),
                                borderRadius: BorderRadius.circular(10),
                                border: Border.all(color: const Color(0xFF10B981), width: 1.2),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    children: [
                                      const Icon(Icons.network_check, color: Color(0xFF10B981), size: 18),
                                      const SizedBox(width: 6),
                                      Text(
                                        'SẢNH TRỰC TUYẾN 100% API + REALTIME',
                                        style: GoogleFonts.cinzel(
                                          fontWeight: FontWeight.bold,
                                          fontSize: 12,
                                          color: const Color(0xFF10B981),
                                        ),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 6),
                                  Text(
                                    'Vào sảnh trực tuyến để tạo bàn công khai, nhập mã bàn bí mật 8 ký tự hoặc xem trực tiếp các kỳ đài đỉnh cao.',
                                    style: GoogleFonts.philosopher(color: const Color(0xFFD7CCC8), fontSize: 12, height: 1.4),
                                  ),
                                ],
                              ),
                            ),
                          ] else if (selectedMode == 3) ...[
                            _buildSectionHeader('CẤU HÌNH ĐỐI ĐẦU AI VS AI', Icons.memory),
                            const SizedBox(height: 8),
                            Row(
                              children: [
                                Expanded(
                                  child: Container(
                                    padding: const EdgeInsets.all(10),
                                    decoration: BoxDecoration(
                                      color: const Color(0xFF281E19),
                                      borderRadius: BorderRadius.circular(8),
                                      border: Border.all(color: const Color(0xFFEF4444).withAlpha(120)),
                                    ),
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Text('Bot Đỏ (Đi trước)', style: GoogleFonts.philosopher(color: const Color(0xFFEF4444), fontWeight: FontWeight.bold, fontSize: 12)),
                                        const SizedBox(height: 4),
                                        DropdownButton<int>(
                                          value: redDepth,
                                          dropdownColor: const Color(0xFF281E19),
                                          style: GoogleFonts.philosopher(color: Colors.white, fontSize: 12),
                                          isExpanded: true,
                                          underline: const SizedBox(),
                                          items: const [
                                            DropdownMenuItem(value: 1, child: Text('Depth 1 (Dễ)')),
                                            DropdownMenuItem(value: 2, child: Text('Depth 2 (Vừa)')),
                                            DropdownMenuItem(value: 3, child: Text('Depth 3 (Khó)')),
                                            DropdownMenuItem(value: 4, child: Text('Depth 4 (Cao)')),
                                          ],
                                          onChanged: (v) => setSheetState(() => redDepth = v ?? 2),
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Container(
                                    padding: const EdgeInsets.all(10),
                                    decoration: BoxDecoration(
                                      color: const Color(0xFF281E19),
                                      borderRadius: BorderRadius.circular(8),
                                      border: Border.all(color: const Color(0xFF90CAF9).withAlpha(120)),
                                    ),
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Text('Bot Đen (Đi sau)', style: GoogleFonts.philosopher(color: const Color(0xFF90CAF9), fontWeight: FontWeight.bold, fontSize: 12)),
                                        const SizedBox(height: 4),
                                        DropdownButton<int>(
                                          value: blackDepth,
                                          dropdownColor: const Color(0xFF281E19),
                                          style: GoogleFonts.philosopher(color: Colors.white, fontSize: 12),
                                          isExpanded: true,
                                          underline: const SizedBox(),
                                          items: const [
                                            DropdownMenuItem(value: 1, child: Text('Depth 1 (Dễ)')),
                                            DropdownMenuItem(value: 2, child: Text('Depth 2 (Vừa)')),
                                            DropdownMenuItem(value: 3, child: Text('Depth 3 (Khó)')),
                                            DropdownMenuItem(value: 4, child: Text('Depth 4 (Cao)')),
                                          ],
                                          onChanged: (v) => setSheetState(() => blackDepth = v ?? 3),
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text('Tốc độ mỗi nước:', style: GoogleFonts.philosopher(color: const Color(0xFFD7CCC8), fontSize: 12)),
                                DropdownButton<int>(
                                  value: speedMs,
                                  dropdownColor: const Color(0xFF281E19),
                                  style: GoogleFonts.philosopher(color: const Color(0xFFFFD54F), fontSize: 12, fontWeight: FontWeight.bold),
                                  underline: const SizedBox(),
                                  items: const [
                                    DropdownMenuItem(value: 400, child: Text('Rất nhanh (0.4s)')),
                                    DropdownMenuItem(value: 800, child: Text('Bình thường (0.8s)')),
                                    DropdownMenuItem(value: 1500, child: Text('Chậm rãi (1.5s)')),
                                  ],
                                  onChanged: (v) => setSheetState(() => speedMs = v ?? 800),
                                ),
                              ],
                            ),
                          ],
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 10),
                  ElevatedButton(
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFFC0392B),
                      side: const BorderSide(color: Color(0xFFFFD54F), width: 1.5),
                      minimumSize: const Size(double.infinity, 44),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                    ),
                    onPressed: () {
                      Navigator.pop(sheetCtx);
                      if (selectedMode == 0) {
                        _playPassAndPlay(timeControl);
                      } else if (selectedMode == 1) {
                        _playWithAiSelected(pveDepth, pveSide, timeControl);
                      } else if (selectedMode == 2) {
                        widget.onGoToLobby();
                      } else if (selectedMode == 3) {
                        _playAiVsAiSelected(redDepth: redDepth, blackDepth: blackDepth, speedMs: speedMs);
                      }
                    },
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.play_arrow, color: Colors.white, size: 20),
                        const SizedBox(width: 6),
                        Text(
                          selectedMode == 2
                              ? 'VÀO SẢNH TRỰC TUYẾN'
                              : (selectedMode == 3 ? 'BẮT ĐẦU TỰ ĐỘNG ĐẤU' : 'BẮT ĐẦU VÁN CỜ'),
                          style: GoogleFonts.cinzel(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                            letterSpacing: 1.1,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 10),

                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton.icon(
                          icon: const Icon(Icons.grid_4x4, size: 14, color: Color(0xFFFFD54F)),
                          label: Text('Xếp Thế Cờ', style: GoogleFonts.philosopher(fontSize: 11, color: Colors.white)),
                          style: OutlinedButton.styleFrom(
                            side: const BorderSide(color: Color(0xFF5D4037)),
                            padding: const EdgeInsets.symmetric(vertical: 8),
                          ),
                          onPressed: () {
                            Navigator.pop(sheetCtx);
                            _showBoardEditorDialog();
                          },
                        ),
                      ),
                      const SizedBox(width: 6),
                      Expanded(
                        child: OutlinedButton.icon(
                          icon: const Icon(Icons.menu_book, size: 14, color: Color(0xFFFFD54F)),
                          label: Text('Khai Cuộc ECCO', style: GoogleFonts.philosopher(fontSize: 11, color: Colors.white)),
                          style: OutlinedButton.styleFrom(
                            side: const BorderSide(color: Color(0xFF5D4037)),
                            padding: const EdgeInsets.symmetric(vertical: 8),
                          ),
                          onPressed: () {
                            Navigator.pop(sheetCtx);
                            _showEccoOpeningExplorer();
                          },
                        ),
                      ),
                      const SizedBox(width: 6),
                      Expanded(
                        child: OutlinedButton.icon(
                          icon: const Icon(Icons.file_download, size: 14, color: Color(0xFFFFD54F)),
                          label: Text('Nạp FEN', style: GoogleFonts.philosopher(fontSize: 11, color: Colors.white)),
                          style: OutlinedButton.styleFrom(
                            side: const BorderSide(color: Color(0xFF5D4037)),
                            padding: const EdgeInsets.symmetric(vertical: 8),
                          ),
                          onPressed: () {
                            Navigator.pop(sheetCtx);
                            _showImportFenDialog();
                          },
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }

  void _showEccoOpeningExplorer() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: const Color(0xFF1E1714),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
        side: BorderSide(color: Color(0xFFFFD54F), width: 1.5),
      ),
      builder: (ctx) {
        int selectedOpeningIdx = 0;
        final openings = [
          {
            'code': 'ECCO-01',
            'title': 'Pháo Đầu (Trung Pháo)',
            'desc': 'Thế trận công thủ toàn diện, Pháo vào trung lộ kiểm soát lộ 5, tạo sức ép thẳng vào cung Tướng đối phương.',
            'fen': 'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1',
            'moves': [
              {'name': 'Pháo 2 bình 5 (C2.5)', 'win': '58.4%', 'draw': '24.1%', 'loss': '17.5%'},
              {'name': 'Pháo 8 bình 5 (C8.5)', 'win': '56.2%', 'draw': '25.0%', 'loss': '18.8%'},
            ],
          },
          {
            'code': 'ECCO-02',
            'title': 'Bình Phong Mã',
            'desc': 'Thế trận phòng ngự phản công kinh điển chống lại Trung Pháo. Hai Mã nhảy lên lộ 3 và 7 giữ chặt trung tâm và bảo vệ Tốt đầu vững chắc.',
            'fen': 'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1',
            'moves': [
              {'name': 'Mã 8 tiến 7 (M8.7)', 'win': '52.1%', 'draw': '30.4%', 'loss': '17.5%'},
              {'name': 'Mã 2 tiến 3 (M2.3)', 'win': '51.5%', 'draw': '31.2%', 'loss': '17.3%'},
            ],
          },
          {
            'code': 'ECCO-03',
            'title': 'Thuận Pháo (Đại chiến)',
            'desc': 'Hai bên cùng đưa Pháo vào cùng một lộ trung tâm, thế cờ đối công kịch liệt, ăn miếng trả miếng không khoan nhượng.',
            'fen': 'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1',
            'moves': [
              {'name': 'Pháo 8 bình 5 (C8.5)', 'win': '49.8%', 'draw': '22.5%', 'loss': '27.7%'},
            ],
          },
          {
            'code': 'ECCO-04',
            'title': 'Tiên Nhân Chỉ Lộ',
            'desc': 'Bên Đỏ khai cuộc bằng nước Tốt 7 tiến 1 hoặc Tốt 3 tiến 1 để thăm dò ý đồ chiến thuật của đối phương.',
            'fen': 'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1',
            'moves': [
              {'name': 'Binh 7 tiến 1 (P7.1)', 'win': '54.2%', 'draw': '28.6%', 'loss': '17.2%'},
            ],
          },
          {
            'code': 'ECCO-05',
            'title': 'Khởi Mã Cuộc (Trận Thượng Tiên)',
            'desc': 'Khai cuộc bằng nước phát triển Mã sớm, cấu trúc phòng thủ kiên cố, linh hoạt và chuyển biến sâu sắc.',
            'fen': 'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1',
            'moves': [
              {'name': 'Mã 2 tiến 3 (H2.3)', 'win': '53.0%', 'draw': '33.5%', 'loss': '13.5%'},
            ],
          },
          {
            'code': 'ECCO-06',
            'title': 'Phi Tượng Cuộc',
            'desc': 'Bên Đỏ bay Tượng lên lộ 5 củng cố trung lộ kiên cố, chuẩn bị các đòn hồi mã thương bất ngờ.',
            'fen': 'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1',
            'moves': [
              {'name': 'Tượng 3 tiến 5 (E3.5)', 'win': '51.0%', 'draw': '35.0%', 'loss': '14.0%'},
            ],
          },
        ];

        return StatefulBuilder(
          builder: (modalCtx, setModalState) {
            final cur = openings[selectedOpeningIdx];
            final movesList = cur['moves'] as List<Map<String, String>>;

            return Container(
              height: MediaQuery.of(ctx).size.height * 0.85,
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.menu_book, color: Color(0xFFFFD54F), size: 22),
                      const SizedBox(width: 8),
                      Text(
                        'TRA CỨU KHAI CUỘC ECCO',
                        style: GoogleFonts.cinzel(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: const Color(0xFFFFE082),
                        ),
                      ),
                      const Spacer(),
                      IconButton(
                        icon: const Icon(Icons.close, color: Colors.white70),
                        onPressed: () => Navigator.pop(modalCtx),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: List.generate(openings.length, (idx) {
                        final op = openings[idx];
                        final isSel = selectedOpeningIdx == idx;
                        return GestureDetector(
                          onTap: () => setModalState(() => selectedOpeningIdx = idx),
                          child: Container(
                            margin: const EdgeInsets.only(right: 8),
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                            decoration: BoxDecoration(
                              color: isSel ? const Color(0xFFC0392B) : const Color(0xFF2C1E18),
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(
                                color: isSel ? const Color(0xFFFFD54F) : const Color(0xFF5D4037),
                                width: 1.2,
                              ),
                            ),
                            child: Text(
                              op['code'] as String,
                              style: GoogleFonts.philosopher(
                                color: isSel ? Colors.white : const Color(0xFFD7CCC8),
                                fontWeight: FontWeight.bold,
                                fontSize: 12,
                              ),
                            ),
                          ),
                        );
                      }),
                    ),
                  ),
                  const SizedBox(height: 14),
                  Expanded(
                    child: SingleChildScrollView(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: const Color(0xFF281E19),
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(color: const Color(0xFF5D4037)),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  children: [
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                      decoration: BoxDecoration(
                                        color: const Color(0xFFFFD54F),
                                        borderRadius: BorderRadius.circular(4),
                                      ),
                                      child: Text(
                                        cur['code'] as String,
                                        style: GoogleFonts.cinzel(
                                          fontWeight: FontWeight.bold,
                                          fontSize: 10,
                                          color: Colors.black87,
                                        ),
                                      ),
                                    ),
                                    const SizedBox(width: 8),
                                    Expanded(
                                      child: Text(
                                        cur['title'] as String,
                                        style: GoogleFonts.philosopher(
                                          fontSize: 15,
                                          fontWeight: FontWeight.bold,
                                          color: const Color(0xFFFFE082),
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 8),
                                Text(
                                  cur['desc'] as String,
                                  style: GoogleFonts.philosopher(
                                    fontSize: 12,
                                    color: const Color(0xFFD7CCC8),
                                    height: 1.4,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: 14),
                          Text(
                            'CÁC PHƯƠNG ÁN KHAI CUỘC PHỔ BIẾN & TỶ LỆ THẮNG',
                            style: GoogleFonts.cinzel(
                              fontSize: 11,
                              fontWeight: FontWeight.bold,
                              color: const Color(0xFFFFD54F),
                            ),
                          ),
                          const SizedBox(height: 8),
                          ...movesList.map((m) => Container(
                            margin: const EdgeInsets.only(bottom: 8),
                            padding: const EdgeInsets.all(10),
                            decoration: BoxDecoration(
                              color: const Color(0xFF221A15),
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(color: const Color(0xFF4A3728)),
                            ),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Text(
                                    m['name'] ?? '',
                                    style: GoogleFonts.philosopher(
                                      color: Colors.white,
                                      fontWeight: FontWeight.bold,
                                      fontSize: 12.5,
                                    ),
                                  ),
                                ),
                                Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                      decoration: BoxDecoration(
                                        color: const Color(0xFF2E7D32),
                                        borderRadius: BorderRadius.circular(4),
                                      ),
                                      child: Text(
                                        'Thắng: ${m['win']}',
                                        style: GoogleFonts.philosopher(fontSize: 10, color: Colors.white, fontWeight: FontWeight.bold),
                                      ),
                                    ),
                                    const SizedBox(width: 4),
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                      decoration: BoxDecoration(
                                        color: const Color(0xFF616161),
                                        borderRadius: BorderRadius.circular(4),
                                      ),
                                      child: Text(
                                        'Hòa: ${m['draw']}',
                                        style: GoogleFonts.philosopher(fontSize: 10, color: Colors.white),
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          )),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 10),
                  ElevatedButton.icon(
                    icon: const Icon(Icons.sports_esports, color: Colors.white),
                    label: Text(
                      'Luyện Tập Thế Trận Này (API)',
                      style: GoogleFonts.philosopher(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                      ),
                    ),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFFC0392B),
                      side: const BorderSide(color: Color(0xFFFFD54F), width: 1.2),
                      minimumSize: const Size(double.infinity, 44),
                    ),
                    onPressed: () {
                      Navigator.pop(modalCtx);
                      _playFromFen(cur['fen'] as String);
                    },
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }

  void _showImportFenDialog() {
    final fenCtrl = TextEditingController(text: 'rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1');
    showDialog(
      context: context,
      builder: (dCtx) => AlertDialog(
        backgroundColor: const Color(0xFF1E1714),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: const BorderSide(color: Color(0xFFFFD54F), width: 1.5),
        ),
        title: Row(
          children: [
            const Icon(Icons.file_download, color: Color(0xFFFFD54F)),
            const SizedBox(width: 8),
            Text(
              'NẠP THẾ CỜ FEN',
              style: GoogleFonts.cinzel(color: const Color(0xFFFFE082), fontWeight: FontWeight.bold, fontSize: 16),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Dán chuỗi ký hiệu FEN chuẩn của cờ tướng để bắt đầu ván cờ trên máy chủ:',
              style: GoogleFonts.philosopher(color: const Color(0xFFD7CCC8), fontSize: 12),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: fenCtrl,
              maxLines: 3,
              style: GoogleFonts.sourceCodePro(color: Colors.white, fontSize: 11),
              decoration: InputDecoration(
                filled: true,
                fillColor: const Color(0xFF281E19),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                  borderSide: const BorderSide(color: Color(0xFF5D4037)),
                ),
                hintText: 'Nhập chuỗi FEN...',
                hintStyle: GoogleFonts.sourceCodePro(color: const Color(0xFF8D6E63), fontSize: 11),
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dCtx),
            child: Text('Hủy', style: GoogleFonts.philosopher(color: Colors.white70)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              side: const BorderSide(color: Color(0xFFFFD54F)),
            ),
            onPressed: () {
              final fen = fenCtrl.text.trim();
              if (fen.isNotEmpty) {
                Navigator.pop(dCtx);
                _playFromFen(fen);
              }
            },
            child: Text('Nạp & Bắt Đầu', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }

  void _showBoardEditorDialog() {
    final presets = [
      {'title': 'Thất Tinh Tụ Hội (Thế cờ giang hồ)', 'fen': '2b1k1b2/4a4/4b4/9/9/9/9/4B4/4A4/2B1K1B2 w - - 0 1'},
      {'title': 'Dã Mã Thao Điền (Tàn cuộc mã tốt)', 'fen': '3ak4/4a4/9/4N4/9/9/9/9/4A4/3K1A3 w - - 0 1'},
      {'title': 'Tam Anh Chiến Lữ Bố', 'fen': '4k4/4a4/9/9/9/9/9/4C4/4A4/4K4 w - - 0 1'},
    ];

    showModalBottomSheet(
      context: context,
      backgroundColor: const Color(0xFF1E1714),
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
        side: BorderSide(color: Color(0xFFFFD54F), width: 1.5),
      ),
      builder: (ctx) => Container(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.grid_4x4, color: Color(0xFFFFD54F)),
                const SizedBox(width: 8),
                Text(
                  'XẾP THẾ CỜ & TÀN CUỘC MẪU',
                  style: GoogleFonts.cinzel(fontSize: 15, fontWeight: FontWeight.bold, color: const Color(0xFFFFE082)),
                ),
              ],
            ),
            const SizedBox(height: 12),
            ...presets.map((p) => ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const Icon(Icons.star, color: Color(0xFFFFD54F), size: 20),
              title: Text(p['title']!, style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13)),
              trailing: const Icon(Icons.arrow_forward_ios, size: 14, color: Color(0xFFFFD54F)),
              onTap: () {
                Navigator.pop(ctx);
                _playFromFen(p['fen']!);
              },
            )),
          ],
        ),
      ),
    );
  }

  Widget _buildModeButton({
    required String title,
    required String subtitle,
    required IconData icon,
    required bool isSelected,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
        decoration: BoxDecoration(
          color: isSelected ? const Color(0xFFC0392B).withAlpha(80) : const Color(0xFF221A15),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(
            color: isSelected ? const Color(0xFFFFD54F) : const Color(0xFF5D4037),
            width: isSelected ? 1.5 : 1,
          ),
        ),
        child: Row(
          children: [
            Icon(icon, color: isSelected ? const Color(0xFFFFD54F) : const Color(0xFFBCAAA4), size: 22),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    title,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: GoogleFonts.philosopher(
                      fontSize: 12.5,
                      fontWeight: FontWeight.bold,
                      color: isSelected ? const Color(0xFFFFE082) : Colors.white,
                    ),
                  ),
                  Text(
                    subtitle,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: GoogleFonts.philosopher(
                      fontSize: 9.5,
                      color: const Color(0xFF8D6E63),
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

  Widget _buildSectionHeader(String title, IconData icon) {
    return Row(
      children: [
        Icon(icon, size: 14, color: const Color(0xFFFFD54F)),
        const SizedBox(width: 6),
        Text(
          title,
          style: GoogleFonts.cinzel(
            fontSize: 11,
            fontWeight: FontWeight.bold,
            color: const Color(0xFFFFD54F),
          ),
        ),
      ],
    );
  }

  Widget _buildDepthBtn({
    required int depth,
    required String name,
    required String label,
    required IconData icon,
    required Color color,
    required bool isSelected,
    required VoidCallback onTap,
  }) {
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 8),
          decoration: BoxDecoration(
            color: isSelected ? color.withAlpha(50) : const Color(0xFF221A15),
            borderRadius: BorderRadius.circular(8),
            border: Border.all(
              color: isSelected ? color : const Color(0xFF5D4037),
              width: isSelected ? 1.5 : 1,
            ),
          ),
          child: Column(
            children: [
              Icon(icon, color: color, size: 18),
              const SizedBox(height: 2),
              Text(
                name,
                style: GoogleFonts.philosopher(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: 11,
                ),
              ),
              Text(
                label,
                style: GoogleFonts.philosopher(
                  color: const Color(0xFF8D6E63),
                  fontSize: 9,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildTimeChip({
    required String label,
    required bool isSelected,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 8),
        decoration: BoxDecoration(
          color: isSelected ? const Color(0xFF00695C) : const Color(0xFF221A15),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(
            color: isSelected ? const Color(0xFF80CBC4) : const Color(0xFF5D4037),
          ),
        ),
        child: Center(
          child: Text(
            label,
            style: GoogleFonts.philosopher(
              color: isSelected ? Colors.white : const Color(0xFFD7CCC8),
              fontWeight: FontWeight.bold,
              fontSize: 11.5,
            ),
          ),
        ),
      ),
    );
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
              const SizedBox(height: 12),

              // 2.2. THỬ THÁCH CỜ THẾ HÔM NAY (+20 ELO) (Daily Challenge Card)
              _buildDailyChallengeCard(),
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
                  // Khối 1: CỜ TƯỚNG (Đỏ son hoàng gia - Đầy đủ 4 chế độ & Tiện ích)
                  _build3DModeCard(
                    title: 'CỜ TƯỚNG',
                    subtitle: 'Đấu trí kinh điển 1v1',
                    badge: 'PHỔ BIẾN',
                    badgeColor: const Color(0xFFFF9800),
                    char: '帥',
                    gradient: const [Color(0xFFBA2213), Color(0xFF6B1107)],
                    borderColor: const Color(0xFFFFD54F),
                    onTap: _showXiangqiModeSelectionSheet,
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
                    onTap: _showCoUpChoiceModal,
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

                  // Khối 4: LUYỆN TẬP (AI Thông Thái & Pass and Play)
                  _build3DModeCard(
                    title: 'LUYỆN TẬP',
                    subtitle: 'Đấu máy & Đấu đôi 2 người',
                    badge: 'OFFLINE',
                    badgeColor: const Color(0xFF9C27B0),
                    char: '將',
                    gradient: const [Color(0xFF4A148C), Color(0xFF311B92)],
                    borderColor: const Color(0xFFCE93D8),
                    onTap: _showPracticeChoiceModal,
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
                      onPressed: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(builder: (_) => const DailyQuestsScreen()),
                        );
                      },
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
          label: 'Học Cờ',
          color: const Color(0xFF40C4FF),
          onTap: () {
            Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => CourseAcademyScreen(onBack: () => Navigator.pop(context))),
            );
          },
        ),
        const SizedBox(width: 8),
        _buildFeaturePill(
          icon: Icons.military_tech,
          label: 'Nhiệm Vụ',
          color: const Color(0xFFFFD54F),
          onTap: () {
            Navigator.push(
              context,
              MaterialPageRoute(builder: (_) => const DailyQuestsScreen()),
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
                Icon(icon, color: color, size: 14),
                const SizedBox(width: 4),
                Text(
                  label,
                  style: GoogleFonts.philosopher(
                    fontSize: 10.5,
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
