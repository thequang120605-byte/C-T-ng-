import 'dart:async';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';
import '../theme/app_theme.dart';
import 'game_screen.dart';
import 'login_screen.dart';

class OnlineLobbyScreen extends StatefulWidget {
  const OnlineLobbyScreen({super.key});

  @override
  State<OnlineLobbyScreen> createState() => _OnlineLobbyScreenState();
}

class _OnlineLobbyScreenState extends State<OnlineLobbyScreen> {
  final _roomCodeCtrl = TextEditingController();
  List<RoomItemDto> _rooms = [];
  bool _isBusy = false;
  String _errorMessage = '';

  int _selectedTierIndex = 0; // 0: Tập sự, 1: Tá điền, 2: Cao thủ, 3: Đại gia
  StreamSubscription? _lobbySub;

  final List<_LobbyTier> _tiers = [
    _LobbyTier(name: 'Tập sự', minBet: 1000, maxBet: 5000, icon: Icons.grass),
    _LobbyTier(name: 'Tá điền', minBet: 10000, maxBet: 20000, icon: Icons.agriculture),
    _LobbyTier(name: 'Cao thủ', minBet: 50000, maxBet: 100000, icon: Icons.military_tech),
    _LobbyTier(name: 'Đại gia', minBet: 200000, maxBet: 500000, icon: Icons.diamond),
  ];

  @override
  void initState() {
    super.initState();
    _loadRooms();
    _setupLobbySync();
  }

  void _setupLobbySync() {
    AppServices.instance.signalR.joinLobbyAsync();
    _lobbySub = AppServices.instance.signalR.onLobbyChanged.listen((_) {
      if (mounted) _loadRooms();
    });
  }

  @override
  void dispose() {
    _lobbySub?.cancel();
    AppServices.instance.signalR.leaveLobbyAsync();
    _roomCodeCtrl.dispose();
    super.dispose();
  }

  Future<bool> _requireLogin() async {
    final auth = AppServices.instance.auth;
    if (await auth.isAuthenticatedAsync()) return true;
    if (!mounted) return false;
    setState(() => _errorMessage = 'Vui lòng đăng nhập để tạo hoặc tham gia bàn online.');
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const LoginScreen()),
    );
    return ok == true;
  }

  Future<void> _loadRooms() async {
    if (_isBusy) return;
    setState(() {
      _isBusy = true;
      _errorMessage = '';
    });
    try {
      final list = await AppServices.instance.api.getOnlineRoomsAsync();
      if (mounted) setState(() => _rooms = list);
    } catch (e) {
      if (mounted) setState(() => _errorMessage = e.toString());
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _createRoom() async {
    if (_isBusy || !await _requireLogin()) return;
    setState(() {
      _isBusy = true;
      _errorMessage = '';
    });
    try {
      final game = await AppServices.instance.api.createOnlineGameAsync();
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GameScreen(
            gameId: game.id,
            difficulty: 1,
            mode: 'Online',
            seatHint: 'Red',
          ),
        ),
      );
      _loadRooms();
    } catch (e) {
      if (mounted) setState(() => _errorMessage = e.toString());
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _joinByCode([String? directCode]) async {
    if (_isBusy || !await _requireLogin()) return;
    final code = (directCode ?? _roomCodeCtrl.text).trim().toUpperCase();
    if (code.isEmpty || code.length > 32) {
      setState(() => _errorMessage = 'Nhập mã phòng hợp lệ.');
      return;
    }
    setState(() {
      _isBusy = true;
      _errorMessage = '';
    });
    try {
      final game = await AppServices.instance.api.joinOnlineRoomAsync(code);
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GameScreen(
            gameId: game.id,
            difficulty: 1,
            mode: 'Online',
            seatHint: 'Black',
          ),
        ),
      );
      _loadRooms();
    } catch (e) {
      if (mounted) setState(() => _errorMessage = e.toString());
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _quickMatch() async {
    if (_rooms.isNotEmpty) {
      final availableRoom = _rooms.firstWhere(
        (r) => r.isJoinable,
        orElse: () => _rooms.first,
      );
      await _joinByCode(availableRoom.roomCode);
    } else {
      await _createRoom();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF16120F),
      body: SafeArea(
        child: Column(
          children: [
            // 1. TOP HEADER CỔ PHONG
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: const BoxDecoration(
                color: Color(0xFF221A15),
                border: Border(
                  bottom: BorderSide(color: Color(0xFF4A3728), width: 1.2),
                ),
              ),
              child: Row(
                children: [
                  Text(
                    'CHỌN BÀN THI ĐẤU',
                    style: GoogleFonts.cinzel(
                      fontSize: 17,
                      fontWeight: FontWeight.bold,
                      color: AppColors.goldLight,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const Spacer(),
                  // Vàng hiện có
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.black45,
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: AppColors.gold, width: 1),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.monetization_on, color: Color(0xFFFFD54F), size: 14),
                        const SizedBox(width: 4),
                        Text(
                          '120.000',
                          style: GoogleFonts.cinzel(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: const Color(0xFFFFD54F),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            // 2. TABS PHÂN CẤP (Tập sự, Tá điền, Cao thủ, Đại gia)
            Container(
              height: 44,
              margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              decoration: BoxDecoration(
                color: const Color(0xFF231A14),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFF5D4037), width: 1.2),
              ),
              child: Row(
                children: List.generate(_tiers.length, (idx) {
                  final tier = _tiers[idx];
                  final isSelected = _selectedTierIndex == idx;
                  return Expanded(
                    child: GestureDetector(
                      onTap: () => setState(() => _selectedTierIndex = idx),
                      child: Container(
                        decoration: BoxDecoration(
                          color: isSelected ? const Color(0xFFD49B4B) : Colors.transparent,
                          borderRadius: BorderRadius.circular(8),
                          gradient: isSelected
                              ? const LinearGradient(
                                  colors: [Color(0xFFE5A855), Color(0xFFB57A2A)],
                                )
                              : null,
                        ),
                        child: Center(
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(
                                tier.icon,
                                size: 14,
                                color: isSelected ? Colors.black87 : const Color(0xFFBCAAA4),
                              ),
                              const SizedBox(width: 4),
                              Text(
                                tier.name,
                                style: GoogleFonts.philosopher(
                                  fontSize: 12,
                                  fontWeight: FontWeight.bold,
                                  color: isSelected ? Colors.black87 : const Color(0xFFBCAAA4),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  );
                }),
              ),
            ),

            // 3. THANH NHẬP MÃ PHÒNG NHANH
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
              child: Row(
                children: [
                  Expanded(
                    child: Container(
                      height: 40,
                      decoration: BoxDecoration(
                        color: const Color(0xFF281E19),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: const Color(0xFF5D4037)),
                      ),
                      padding: const EdgeInsets.symmetric(horizontal: 10),
                      alignment: Alignment.center,
                      child: TextField(
                        controller: _roomCodeCtrl,
                        style: GoogleFonts.cinzel(color: Colors.white, fontSize: 13),
                        decoration: InputDecoration(
                          hintText: 'Nhập mã bàn chơi bí mật…',
                          hintStyle: GoogleFonts.philosopher(color: const Color(0xFF8D6E63), fontSize: 12),
                          border: InputBorder.none,
                          isDense: true,
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  ElevatedButton(
                    onPressed: () => _joinByCode(),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFFC0392B),
                      side: const BorderSide(color: Color(0xFFFFD54F), width: 1.2),
                      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                    ),
                    child: Text('Vào', style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.white)),
                  ),
                ],
              ),
            ),

            if (_errorMessage.isNotEmpty)
              Container(
                margin: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: const Color(0xFFB71C1C).withAlpha(180),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  _errorMessage,
                  style: GoogleFonts.philosopher(color: Colors.white, fontSize: 12),
                ),
              ),

            // 4. DANH SÁCH BÀN CỜ
            Expanded(
              child: _isBusy && _rooms.isEmpty
                  ? const Center(child: CircularProgressIndicator(color: AppColors.gold))
                  : _buildRoomList(),
            ),

            // 5. BOTTOM TOOLBAR (Tạo bàn, Chơi ngay, Làm mới)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              decoration: const BoxDecoration(
                color: Color(0xFF221A15),
                border: Border(top: BorderSide(color: Color(0xFF4A3728), width: 1.2)),
              ),
              child: Row(
                children: [
                  // Nút Làm Mới
                  OutlinedButton.icon(
                    onPressed: _loadRooms,
                    icon: const Icon(Icons.refresh, size: 16, color: AppColors.goldLight),
                    label: Text('Làm Mới', style: GoogleFonts.philosopher(color: AppColors.goldLight, fontSize: 12)),
                    style: OutlinedButton.styleFrom(
                      side: const BorderSide(color: Color(0xFF8D6E63)),
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                    ),
                  ),
                  const SizedBox(width: 10),

                  // Nút Tạo Bàn
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: _createRoom,
                      icon: const Icon(Icons.add, size: 18, color: Colors.white),
                      label: Text('Tạo Bàn', style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.white)),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFFB71C1C),
                        side: const BorderSide(color: Color(0xFFFFD54F), width: 1.2),
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),

                  // Nút Chơi Ngay (Quick Match)
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: _quickMatch,
                      icon: const Icon(Icons.flash_on, size: 18, color: Colors.black87),
                      label: Text('Chơi Ngay', style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.black87)),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFFFFD54F),
                        side: const BorderSide(color: Colors.white, width: 1.2),
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

  Widget _buildRoomList() {
    // Nếu chưa có phòng thật trên backend, sinh các phòng ảo theo cấp bậc đã chọn
    final tier = _tiers[_selectedTierIndex];
    final displayList = _rooms.isNotEmpty ? _rooms : _getMockRoomsForTier(_selectedTierIndex);

    return ListView.builder(
      padding: const EdgeInsets.all(12),
      itemCount: displayList.length,
      itemBuilder: (context, idx) {
        final room = displayList[idx];
        return _buildRoomCard(room, tier);
      },
    );
  }

  List<RoomItemDto> _getMockRoomsForTier(int tierIndex) {
    final now = DateTime.now();
    switch (tierIndex) {
      case 0: // Tập sự (1k - 5k)
        return [
          RoomItemDto(
            id: 101,
            roomCode: 'TAB-101',
            createdAt: now,
            creatorUsername: 'Trần Phương Thảo',
            creatorElo: 1250,
            status: 'WaitingForPlayers',
            playerCount: 1,
            isJoinable: true,
          ),
          RoomItemDto(
            id: 102,
            roomCode: 'TAB-102',
            createdAt: now.subtract(const Duration(minutes: 1)),
            creatorUsername: 'Lê Minh Quân',
            creatorElo: 1280,
            status: 'WaitingForPlayers',
            playerCount: 1,
            isJoinable: true,
          ),
          RoomItemDto(
            id: 103,
            roomCode: 'TAB-103',
            createdAt: now.subtract(const Duration(minutes: 5)),
            creatorUsername: 'Đặng Tuấn Anh',
            creatorElo: 1310,
            status: 'InProgress',
            playerCount: 2,
            isJoinable: false,
          ),
        ];
      case 1: // Tá điền (10k - 20k)
        return [
          RoomItemDto(
            id: 201,
            roomCode: 'TD-201',
            createdAt: now,
            creatorUsername: 'Hoàng Phi Hổ',
            creatorElo: 1650,
            status: 'WaitingForPlayers',
            playerCount: 1,
            isJoinable: true,
          ),
          RoomItemDto(
            id: 202,
            roomCode: 'TD-202',
            createdAt: now.subtract(const Duration(minutes: 2)),
            creatorUsername: 'Nguyễn Đình Triết',
            creatorElo: 1620,
            status: 'WaitingForPlayers',
            playerCount: 1,
            isJoinable: true,
          ),
        ];
      case 2: // Cao thủ (50k - 100k)
        return [
          RoomItemDto(
            id: 301,
            roomCode: 'CT-301',
            createdAt: now,
            creatorUsername: 'Tiêu Dao Tử',
            creatorElo: 2150,
            status: 'WaitingForPlayers',
            playerCount: 1,
            isJoinable: true,
          ),
        ];
      default: // Đại gia
        return [
          RoomItemDto(
            id: 999,
            roomCode: 'VIP-999',
            createdAt: now,
            creatorUsername: 'Vương Gia Hoàng Triều',
            creatorElo: 2450,
            status: 'WaitingForPlayers',
            playerCount: 1,
            isJoinable: true,
          ),
        ];
    }
  }

  Widget _buildRoomCard(RoomItemDto room, _LobbyTier tier) {
    final isFull = room.playerCount >= 2;
    final betText = '${tier.minBet ~/ 1000}k';

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFF221A15),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isFull ? const Color(0xFF424242) : const Color(0xFF8D6E63),
          width: 1.2,
        ),
        boxShadow: const [
          BoxShadow(color: Colors.black45, blurRadius: 4, offset: Offset(0, 2)),
        ],
      ),
      child: Row(
        children: [
          // Icon Bàn Cờ Cổ Phong
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              gradient: const LinearGradient(
                colors: [Color(0xFFE5A855), Color(0xFFB57A2A)],
              ),
              border: Border.all(color: const Color(0xFFFFD54F), width: 1.2),
            ),
            child: const Center(
              child: Text(
                '帥',
                style: TextStyle(
                  fontFamily: 'serif',
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                  color: Color(0xFF8E0000),
                ),
              ),
            ),
          ),
          const SizedBox(width: 12),

          // Thông tin bàn
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1.5),
                      decoration: BoxDecoration(
                        color: const Color(0xFFC0392B),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        'Cờ Tướng',
                        style: GoogleFonts.philosopher(fontSize: 9, color: Colors.white, fontWeight: FontWeight.bold),
                      ),
                    ),
                    const SizedBox(width: 6),
                    Text(
                      room.roomCode,
                      style: GoogleFonts.cinzel(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: AppColors.goldLight,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 3),
                Text(
                  'Chủ phòng: ${room.creatorUsername.isNotEmpty ? room.creatorUsername : "Ẩn danh"}',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.philosopher(fontSize: 12, color: const Color(0xFFBCAAA4)),
                ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    const Icon(Icons.monetization_on, size: 13, color: Color(0xFFFFD54F)),
                    const SizedBox(width: 3),
                    Text(
                      'Cược: $betText Vàng',
                      style: GoogleFonts.cinzel(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: const Color(0xFFFFD54F),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Icon(
                      Icons.people,
                      size: 13,
                      color: isFull ? const Color(0xFFE53935) : const Color(0xFF00E676),
                    ),
                    const SizedBox(width: 3),
                    Text(
                      '${room.playerCount}/2 ghế',
                      style: GoogleFonts.philosopher(
                        fontSize: 11,
                        color: isFull ? const Color(0xFFE53935) : const Color(0xFF00E676),
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),

          // Nút Vào Bàn
          ElevatedButton(
            onPressed: isFull ? null : () => _joinByCode(room.roomCode),
            style: ElevatedButton.styleFrom(
              backgroundColor: isFull ? const Color(0xFF424242) : const Color(0xFFC0392B),
              side: BorderSide(
                color: isFull ? Colors.transparent : const Color(0xFFFFD54F),
                width: 1.2,
              ),
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
            ),
            child: Text(
              isFull ? 'Đang đấu' : 'Vào Bàn',
              style: GoogleFonts.philosopher(
                fontWeight: FontWeight.bold,
                color: Colors.white,
                fontSize: 12,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _LobbyTier {
  final String name;
  final int minBet;
  final int maxBet;
  final IconData icon;

  _LobbyTier({
    required this.name,
    required this.minBet,
    required this.maxBet,
    required this.icon,
  });
}
