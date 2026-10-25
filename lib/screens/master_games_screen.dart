import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';
import '../services/offline_game_service.dart';
import '../widgets/chess_board_widget.dart';

class MasterGamesScreen extends StatefulWidget {
  final VoidCallback onBack;

  const MasterGamesScreen({super.key, required this.onBack});

  @override
  State<MasterGamesScreen> createState() => _MasterGamesScreenState();
}

class _MasterGamesScreenState extends State<MasterGamesScreen> {
  List<MasterGameItemDto> _games = [];
  bool _isLoading = true;
  String _selectedFilter = 'Tất cả';

  @override
  void initState() {
    super.initState();
    _loadMasterGames();
  }

  Future<void> _loadMasterGames() async {
    setState(() => _isLoading = true);
    try {
      final list = await AppServices.instance.api.getMasterGamesAsync();
      if (mounted) setState(() => _games = list);
    } catch (_) {} finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  List<MasterGameItemDto> get _filteredGames {
    if (_selectedFilter == 'Tất cả') return _games;
    return _games.where((g) {
      return g.redPlayer.contains(_selectedFilter) ||
          g.blackPlayer.contains(_selectedFilter) ||
          g.title.contains(_selectedFilter);
    }).toList();
  }

  void _openGameViewer(MasterGameItemDto game) {
    HapticFeedback.selectionClick();
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _MasterGameViewerModal(game: game),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF140F0D),
      body: SafeArea(
        child: Column(
          children: [
            // Top Bar Cổ Phong
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              decoration: const BoxDecoration(
                color: Color(0xFF1E1714),
                border: Border(bottom: BorderSide(color: Color(0xFF4A3728), width: 1.5)),
              ),
              child: Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_ios, color: Color(0xFFFFD54F), size: 20),
                    onPressed: widget.onBack,
                  ),
                  const SizedBox(width: 4),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'KỲ PHỔ DANH THỦ',
                          style: GoogleFonts.cinzel(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: const Color(0xFFFFE082),
                            letterSpacing: 1.2,
                          ),
                        ),
                        Text(
                          'Các ván cờ kinh điển đỉnh cao thế giới & Việt Nam',
                          style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFF8D6E63)),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: const Color(0xFF2C1E18),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFFFFD54F), width: 1),
                    ),
                    child: Text(
                      '${_games.length} Ván',
                      style: GoogleFonts.cinzel(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: const Color(0xFFFFD54F),
                      ),
                    ),
                  ),
                ],
              ),
            ),

            // Thanh Lọc Danh Thủ (Vương Thiên Nhất, Lại Lý Huynh, Hứa Ngân Xuyên...)
            Container(
              height: 44,
              margin: const EdgeInsets.symmetric(vertical: 8),
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 14),
                children: [
                  'Tất cả',
                  'Lại Lý Huynh',
                  'Vương Thiên Nhất',
                  'Hứa Ngân Xuyên',
                  'Nguyễn Thành Bảo',
                  'Trịnh Duy Đồng',
                ].map((name) {
                  final isSelected = _selectedFilter == name;
                  return Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: ChoiceChip(
                      label: Text(
                        name,
                        style: GoogleFonts.philosopher(
                          fontSize: 12,
                          fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                          color: isSelected ? Colors.black87 : const Color(0xFFD7CCC8),
                        ),
                      ),
                      selected: isSelected,
                      selectedColor: const Color(0xFFFFD54F),
                      backgroundColor: const Color(0xFF1E1714),
                      side: BorderSide(
                        color: isSelected ? const Color(0xFFFFD54F) : const Color(0xFF4A3728),
                      ),
                      onSelected: (val) {
                        if (val) setState(() => _selectedFilter = name);
                      },
                    ),
                  );
                }).toList(),
              ),
            ),

            // Danh Sách Kỳ Phổ
            Expanded(
              child: _isLoading
                  ? const Center(child: CircularProgressIndicator(color: Color(0xFFFFD54F)))
                  : _filteredGames.isEmpty
                      ? Center(
                          child: Text(
                            'Chưa tìm thấy ván cờ phù hợp',
                            style: GoogleFonts.philosopher(color: const Color(0xFF8D6E63)),
                          ),
                        )
                      : ListView.builder(
                          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
                          itemCount: _filteredGames.length,
                          itemBuilder: (ctx, idx) {
                            final g = _filteredGames[idx];
                            return _buildMasterGameCard(g);
                          },
                        ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMasterGameCard(MasterGameItemDto g) {
    final isRedWin = g.result.contains('1-0');
    final isBlackWin = g.result.contains('0-1');
    final resultText = isRedWin ? 'Đỏ thắng' : (isBlackWin ? 'Đen thắng' : 'Hòa');
    final resultColor = isRedWin
        ? const Color(0xFFE53935)
        : (isBlackWin ? const Color(0xFF42A5F5) : const Color(0xFFFFA726));

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: const Color(0xFF1E1714),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFF3E2723), width: 1.2),
        boxShadow: const [
          BoxShadow(color: Colors.black45, blurRadius: 4, offset: Offset(0, 2)),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          borderRadius: BorderRadius.circular(14),
          onTap: () => _openGameViewer(g),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Event & Date Tag
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Row(
                        children: [
                          const Icon(Icons.emoji_events, color: Color(0xFFFFD54F), size: 16),
                          const SizedBox(width: 6),
                          Expanded(
                            child: Text(
                              g.eventName.isNotEmpty ? g.eventName : 'Giải Vô Địch Danh Thủ',
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: GoogleFonts.philosopher(
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: const Color(0xFFFFD54F),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: resultColor.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(6),
                        border: Border.all(color: resultColor, width: 0.8),
                      ),
                      child: Text(
                        '$resultText (${g.result})',
                        style: GoogleFonts.philosopher(
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                          color: resultColor,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 10),

                // Tiêu đề ván cờ
                Text(
                  g.title,
                  style: GoogleFonts.philosopher(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                    height: 1.3,
                  ),
                ),
                const SizedBox(height: 6),

                // Kỳ Thủ Đỏ vs Kỳ Thủ Đen
                Row(
                  children: [
                    _buildPlayerBadge(g.redPlayer, true),
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 8),
                      child: Text(
                        'VS',
                        style: GoogleFonts.cinzel(
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                          color: const Color(0xFF8D6E63),
                        ),
                      ),
                    ),
                    _buildPlayerBadge(g.blackPlayer, false),
                  ],
                ),
                const SizedBox(height: 10),

                // Trận hình khai cuộc & Chi tiết
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        'Khai cuộc: ${g.openingName.isNotEmpty ? g.openingName : "Chính quy"}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.philosopher(
                          fontSize: 11,
                          color: const Color(0xFF8D6E63),
                          fontStyle: FontStyle.italic,
                        ),
                      ),
                    ),
                    Row(
                      children: [
                        Text(
                          'Xem kỳ phổ',
                          style: GoogleFonts.philosopher(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: const Color(0xFFFFD54F),
                          ),
                        ),
                        const Icon(Icons.arrow_forward_ios, size: 12, color: Color(0xFFFFD54F)),
                      ],
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildPlayerBadge(String name, bool isRed) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: isRed
            ? const Color(0xFFC0392B).withValues(alpha: 0.2)
            : const Color(0xFF2C3E50).withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(
          color: isRed ? const Color(0xFFE57373) : const Color(0xFF90CAF9),
          width: 0.8,
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            isRed ? '帥 Đỏ: ' : '將 Đen: ',
            style: GoogleFonts.philosopher(
              fontSize: 11,
              fontWeight: FontWeight.bold,
              color: isRed ? const Color(0xFFFF8A80) : const Color(0xFF90CAF9),
            ),
          ),
          Text(
            name,
            style: GoogleFonts.philosopher(
              fontSize: 12,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
        ],
      ),
    );
  }
}

// Modal Xem Kỳ Phổ Chi Tiết & Từng Nước Đi
class _MasterGameViewerModal extends StatefulWidget {
  final MasterGameItemDto game;

  const _MasterGameViewerModal({required this.game});

  @override
  State<_MasterGameViewerModal> createState() => _MasterGameViewerModalState();
}

class _MasterGameViewerModalState extends State<_MasterGameViewerModal> {
  late List<PieceDto> _currentPieces;
  int _currentMoveIndex = 0;
  List<String> _moves = [];

  @override
  void initState() {
    super.initState();
    _currentPieces = OfflineGameService.defaultStartingPieces();
    _parsePgnMoves();
  }

  void _parsePgnMoves() {
    final lines = widget.game.pgnContent.split('\n');
    final moveLines = lines.where((l) => !l.startsWith('[') && l.trim().isNotEmpty).toList();
    _moves = [];
    for (final line in moveLines) {
      if (line.startsWith('(')) continue; // Ghi chú ván cờ
      final parts = line.split(RegExp(r'\s+'));
      for (final p in parts) {
        if (!p.contains('.') && p.trim().isNotEmpty) {
          _moves.add(p.trim());
        }
      }
    }
    if (_moves.isEmpty) {
      _moves = [
        'Pháo 2 bình 5', 'Mã 8 tấn 7', 'Mã 2 tấn 3', 'Xe 9 bình 8',
        'Xe 1 bình 2', 'Binh 7 tấn 1', 'Binh 7 tấn 1', 'Mã 2 tấn 3',
        'Pháo 8 bình 9', 'Xe 1 tiến 1'
      ];
    }
  }

  void _stepForward() {
    if (_currentMoveIndex < _moves.length) {
      HapticFeedback.lightImpact();
      AppServices.instance.sound.playMove();
      setState(() => _currentMoveIndex++);
    }
  }

  void _stepBackward() {
    if (_currentMoveIndex > 0) {
      HapticFeedback.lightImpact();
      AppServices.instance.sound.playMove();
      setState(() => _currentMoveIndex--);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      height: MediaQuery.of(context).size.height * 0.88,
      decoration: const BoxDecoration(
        color: Color(0xFF1E1714),
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        border: Border(top: BorderSide(color: Color(0xFFFFD54F), width: 1.5)),
      ),
      child: Column(
        children: [
          // Drag handle
          Container(
            width: 40,
            height: 4,
            margin: const EdgeInsets.symmetric(vertical: 8),
            decoration: BoxDecoration(
              color: const Color(0xFF5D4037),
              borderRadius: BorderRadius.circular(2),
            ),
          ),

          // Header
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        widget.game.title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.philosopher(
                          fontSize: 15,
                          fontWeight: FontWeight.bold,
                          color: const Color(0xFFFFD54F),
                        ),
                      ),
                      Text(
                        '${widget.game.redPlayer} (Đỏ) vs ${widget.game.blackPlayer} (Đen) • Kết quả: ${widget.game.result}',
                        style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFF8D6E63)),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close, color: Colors.white, size: 20),
                  onPressed: () => Navigator.pop(context),
                ),
              ],
            ),
          ),
          const Divider(color: Color(0xFF3E2723)),

          // Bàn cờ thu nhỏ xem diễn biến
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Column(
                children: [
                  AspectRatio(
                    aspectRatio: 9 / 10,
                    child: Container(
                      decoration: BoxDecoration(
                        color: const Color(0xFFD4A373),
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(color: const Color(0xFF5D4037), width: 2),
                      ),
                      child: ChessBoardWidget(
                        pieces: _currentPieces,
                        selectedRow: null,
                        selectedCol: null,
                        validMoves: const [],
                        lastMoveFrom: null,
                        lastMoveTo: null,
                        isFlipped: false,
                        reduceMotion: false,
                        gameId: 0,
                        moveCount: _currentMoveIndex,
                        riverWatermarkText: 'Danh Thủ',
                        onCellTapped: (r, c) {},
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),

                  // Nước đi hiện tại
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: const Color(0xFF140F0D),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: const Color(0xFF3E2723)),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          'Nước ${_currentMoveIndex + 1}/${_moves.length}:',
                          style: GoogleFonts.philosopher(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: const Color(0xFFFFD54F),
                          ),
                        ),
                        Text(
                          _currentMoveIndex < _moves.length ? _moves[_currentMoveIndex] : 'Hết ván',
                          style: GoogleFonts.philosopher(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 12),

                  // Lời bình ván cờ
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFF2C1E18),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: const Color(0xFF5D4037)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            const Icon(Icons.menu_book, color: Color(0xFFFFD54F), size: 16),
                            const SizedBox(width: 6),
                            Text(
                              'Bình Luận Chuyên Môn',
                              style: GoogleFonts.philosopher(
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: const Color(0xFFFFD54F),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 6),
                        Text(
                          widget.game.pgnContent.contains('(')
                              ? widget.game.pgnContent.substring(
                                  widget.game.pgnContent.indexOf('(') + 1,
                                  widget.game.pgnContent.lastIndexOf(')'),
                                )
                              : 'Ván đấu đỉnh cao mẫu mực về việc khai thác trung lộ và điều quân biến ảo.',
                          style: GoogleFonts.philosopher(
                            fontSize: 12,
                            color: const Color(0xFFD7CCC8),
                            height: 1.4,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                ],
              ),
            ),
          ),

          // Thanh Điều Khiển Bước Đi (Tới, Lùi, Về Đầu, Về Cuối)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            decoration: const BoxDecoration(
              color: Color(0xFF140F0D),
              border: Border(top: BorderSide(color: Color(0xFF3E2723))),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              children: [
                IconButton(
                  icon: const Icon(Icons.first_page, color: Color(0xFFFFD54F)),
                  onPressed: () => setState(() => _currentMoveIndex = 0),
                ),
                IconButton(
                  icon: const Icon(Icons.chevron_left, color: Color(0xFFFFD54F), size: 30),
                  onPressed: _stepBackward,
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: const Color(0xFF2C1E18),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    '$_currentMoveIndex / ${_moves.length}',
                    style: GoogleFonts.cinzel(
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFFFFD54F),
                    ),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.chevron_right, color: Color(0xFFFFD54F), size: 30),
                  onPressed: _stepForward,
                ),
                IconButton(
                  icon: const Icon(Icons.last_page, color: Color(0xFFFFD54F)),
                  onPressed: () => setState(() => _currentMoveIndex = _moves.length),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
