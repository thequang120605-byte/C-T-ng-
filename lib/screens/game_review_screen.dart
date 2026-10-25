import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../theme/app_theme.dart';
import '../widgets/advantage_bar_widget.dart';
import '../widgets/chess_board_widget.dart';

class GameReviewScreen extends StatefulWidget {
  final List<PieceDto> initialPieces;
  final List<String> moveHistory;
  final String playerRedName;
  final String playerBlackName;

  const GameReviewScreen({
    super.key,
    required this.initialPieces,
    required this.moveHistory,
    this.playerRedName = 'Quang Nguyễn Thế',
    this.playerBlackName = 'Trần Thị Phương Thảo',
  });

  @override
  State<GameReviewScreen> createState() => _GameReviewScreenState();
}

class _GameReviewScreenState extends State<GameReviewScreen> {
  int _currentTabIndex = 2; // 0: Thế trận, 1: Tổng quan, 2: Nước đi, 3: Sai lầm
  int _currentMoveIndex = 0;
  bool _isFlipped = false;

  late List<PieceDto> _currentPieces;
  int _currentScore = 30; // Điểm thế trận mô phỏng hoặc tính toán
  (int, int)? _arrowFrom;
  (int, int)? _arrowTo;
  Color _arrowColor = const Color(0xFF00E676);
  final Map<String, String> _pieceBadges = {};

  // Dữ liệu mô phỏng các nước đi được AI phân tích sâu sắc
  late List<_ReviewMoveStep> _reviewSteps;

  @override
  void initState() {
    super.initState();
    _buildReviewSteps();
    _applyStep(0);
  }

  void _buildReviewSteps() {
    if (widget.moveHistory.isNotEmpty) {
      final steps = <_ReviewMoveStep>[];
      final regex = RegExp(r'\((\d+),(\d+)\s*[➔\->]+\s*(\d+),(\d+)\)');
      int runningEval = 20;

      for (int i = 0; i < widget.moveHistory.length; i++) {
        final raw = widget.moveHistory[i];
        final match = regex.firstMatch(raw);
        final side = i % 2 == 0 ? 'Red' : 'Black';

        int fx = 7, fy = 1, tx = 7, ty = 4;
        if (match != null) {
          fx = int.tryParse(match.group(1)!) ?? 7;
          fy = int.tryParse(match.group(2)!) ?? 1;
          tx = int.tryParse(match.group(3)!) ?? 7;
          ty = int.tryParse(match.group(4)!) ?? 4;
        }

        // Đánh giá thế trận biến động
        final isRed = side == 'Red';
        final isOdd = i % 3 == 0;
        final evalDelta = (i % 5 == 0) ? -45 : (isOdd ? 25 : 15);
        runningEval += isRed ? evalDelta : -evalDelta;
        final isBlunder = i == 5 || (i > 0 && i % 7 == 0);

        steps.add(
          _ReviewMoveStep(
            moveNumber: i + 1,
            notation: raw,
            playerSide: side,
            evaluation: runningEval,
            isOptimal: !isBlunder,
            isBlunder: isBlunder,
            quality: isBlunder ? 'Nước chưa tốt' : (i % 2 == 0 ? 'Nước tối ưu' : 'Nước tốt'),
            qualityBadge: isBlunder ? '??' : (i % 2 == 0 ? '⭐' : '✓'),
            explanation: isBlunder
                ? 'Nước cờ sơ hở để lộ trục sườn, tạo cơ hội cho đối phương phản kích đoạt thế.'
                : 'Nước điều quân chuẩn mực theo thế trận, phát huy nhanh hỏa lực công thủ toàn diện.',
            aiAlternative: '$raw (Tối ưu)',
            aiReasoning: 'Bảo vệ vị trí trọng yếu, giữ vững trục kiểm soát trung tâm bàn cờ.',
            actualFrom: (fx, fy),
            actualTo: (tx, ty),
            optimalFrom: isBlunder ? (fx, fy == 0 ? 1 : fy - 1) : (fx, fy),
            optimalTo: isBlunder ? (tx, ty == 8 ? 7 : ty + 1) : (tx, ty),
          ),
        );
      }
      _reviewSteps = steps;
      return;
    }

    _reviewSteps = [
      _ReviewMoveStep(
        moveNumber: 1,
        notation: 'Pháo 2 bình 5',
        playerSide: 'Red',
        evaluation: 25,
        isOptimal: true,
        quality: 'Nước tối ưu',
        qualityBadge: '⭐',
        explanation:
            'Khai cuộc Pháo đầu kinh điển, khống chế lộ trung tâm, đe dọa trực tiếp Tốt 5.',
        aiAlternative: 'Pháo 2 bình 5 (Ưu thế +25)',
        aiReasoning: 'Phát huy nhanh uy lực trung lộ, tạo tiền đề ra Mã và Xe.',
        actualFrom: (7, 1),
        actualTo: (7, 4),
        optimalFrom: (7, 1),
        optimalTo: (7, 4),
      ),
      _ReviewMoveStep(
        moveNumber: 2,
        notation: 'Mã 8 tấn 7',
        playerSide: 'Black',
        evaluation: -10,
        isOptimal: true,
        quality: 'Nước tốt',
        qualityBadge: '✓',
        explanation:
            'Bình phong Mã xuất kích bảo vệ trung lộ, phòng ngự vững chắc trước đòn Pháo đầu.',
        aiAlternative: 'Mã 8 tấn 7',
        aiReasoning: 'Bảo vệ Tốt đầu chuẩn mực theo binh pháp cờ tướng.',
        actualFrom: (0, 1),
        actualTo: (2, 2),
        optimalFrom: (0, 1),
        optimalTo: (2, 2),
      ),
      _ReviewMoveStep(
        moveNumber: 3,
        notation: 'Mã 2 tấn 3',
        playerSide: 'Red',
        evaluation: 30,
        isOptimal: true,
        quality: 'Nước tối ưu',
        qualityBadge: '⭐',
        explanation: 'Khởi động cánh phải, bảo vệ vững chắc Tốt đầu.',
        aiAlternative: 'Mã 2 tấn 3 (Ưu thế +30)',
        aiReasoning: 'Củng cố trục trung lộ và thông đường xuất Xa lộ 1.',
        actualFrom: (9, 7),
        actualTo: (7, 6),
        optimalFrom: (9, 7),
        optimalTo: (7, 6),
      ),
      _ReviewMoveStep(
        moveNumber: 4,
        notation: 'Xe 9 bình 8',
        playerSide: 'Black',
        evaluation: -15,
        isOptimal: true,
        quality: 'Nước tốt',
        qualityBadge: '✓',
        explanation: 'Xuất Xe chiếm lộ thông thoáng, kiềm chế cánh trái của Đỏ.',
        aiAlternative: 'Xe 9 bình 8',
        aiReasoning: 'Chiếm cứ lộ sườn huyết mạch, sẵn sàng phản công.',
        actualFrom: (0, 0),
        actualTo: (0, 1),
        optimalFrom: (0, 0),
        optimalTo: (0, 1),
      ),
      _ReviewMoveStep(
        moveNumber: 5,
        notation: 'Xe 1 bình 2',
        playerSide: 'Red',
        evaluation: 40,
        isOptimal: true,
        quality: 'Nước tối ưu',
        qualityBadge: '⭐',
        explanation: 'Xuất Xe trực chiến đối kháng trực diện.',
        aiAlternative: 'Xe 1 bình 2 (Ưu thế +40)',
        aiReasoning: 'Cân bằng thế công cánh phải, đè nén Pháo cánh đối phương.',
        actualFrom: (9, 8),
        actualTo: (9, 7),
        optimalFrom: (9, 8),
        optimalTo: (9, 7),
      ),
      _ReviewMoveStep(
        moveNumber: 6,
        notation: 'Pháo 8 tấn 7',
        playerSide: 'Black',
        evaluation: 135, // Đỏ ưu lớn vì Đen blunder
        isOptimal: false,
        isBlunder: true,
        quality: 'Nước rất tệ',
        qualityBadge: '??',
        explanation:
            'Nước đi có thể dẫn đến mất thế trận! Quá nóng vội lao Pháo qua sông mà chưa kịp củng cố thế phòng ngự.',
        aiAlternative: 'Pháo 2 bình 5 (Ưu thế +0)',
        aiReasoning:
            'Nước đi có thể duy trì thế trận\nĐe dọa Tốt 5\nNgăn đối thủ thực hiện đòn phản kích.',
        actualFrom: (2, 1),
        actualTo: (9, 1),
        optimalFrom: (2, 7),
        optimalTo: (2, 4),
      ),
    ];
  }

  void _applyStep(int index) {
    if (index < 0 || index >= _reviewSteps.length) return;
    setState(() {
      _currentMoveIndex = index;
      final step = _reviewSteps[index];

      // Reconstruct pieces up to this step
      final basePieces = widget.initialPieces.isNotEmpty
          ? List<PieceDto>.from(widget.initialPieces)
          : [
              const PieceDto(row: 9, col: 0, type: 'Chariot', color: 'Red'),
              const PieceDto(row: 9, col: 1, type: 'Horse', color: 'Red'),
              const PieceDto(row: 9, col: 4, type: 'General', color: 'Red'),
              const PieceDto(row: 9, col: 7, type: 'Horse', color: 'Red'),
              const PieceDto(row: 9, col: 8, type: 'Chariot', color: 'Red'),
              const PieceDto(row: 7, col: 1, type: 'Cannon', color: 'Red'),
              const PieceDto(row: 7, col: 7, type: 'Cannon', color: 'Red'),
              const PieceDto(row: 0, col: 0, type: 'Chariot', color: 'Black'),
              const PieceDto(row: 0, col: 1, type: 'Horse', color: 'Black'),
              const PieceDto(row: 0, col: 4, type: 'General', color: 'Black'),
              const PieceDto(row: 0, col: 7, type: 'Horse', color: 'Black'),
              const PieceDto(row: 0, col: 8, type: 'Chariot', color: 'Black'),
              const PieceDto(row: 2, col: 1, type: 'Cannon', color: 'Black'),
              const PieceDto(row: 2, col: 7, type: 'Cannon', color: 'Black'),
            ];

      final temp = List<PieceDto>.from(basePieces);
      for (int i = 0; i <= index; i++) {
        final s = _reviewSteps[i];
        temp.removeWhere((p) => p.row == s.actualTo.$1 && p.col == s.actualTo.$2);
        final movingIdx = temp.indexWhere((p) => p.row == s.actualFrom.$1 && p.col == s.actualFrom.$2);
        if (movingIdx >= 0) {
          final moving = temp[movingIdx];
          temp[movingIdx] = PieceDto(
            row: s.actualTo.$1,
            col: s.actualTo.$2,
            type: moving.type,
            color: moving.color,
          );
        }
      }
      _currentPieces = temp;
      _pieceBadges.clear();

      _currentScore = step.playerSide == 'Red' ? step.evaluation : -step.evaluation;

      if (step.isBlunder) {
        _arrowFrom = step.optimalFrom;
        _arrowTo = step.optimalTo;
        _arrowColor = const Color(0xFF00E676);
        _pieceBadges['${step.actualTo.$1},${step.actualTo.$2}'] = '??';
      } else {
        _arrowFrom = step.actualFrom;
        _arrowTo = step.actualTo;
        _arrowColor = const Color(0xFF00E676);
        _pieceBadges['${step.actualTo.$1},${step.actualTo.$2}'] = '⭐';
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final currentStep = _reviewSteps[_currentMoveIndex];

    return Scaffold(
      backgroundColor: AppColors.darkBg,
      body: SafeArea(
        child: Column(
          children: [
            // 1. THANH ƯU THẾ THẾ TRẬN (Advantage Bar)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
              child: AdvantageBarWidget(
                evaluationScore: _currentScore,
              ),
            ),

            // 2. KHU VỰC BÀN CỜ THU NHỎ & 2 KỲ THỦ
            Expanded(
              flex: 5,
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    // Bàn cờ chiến thuật
                    Expanded(
                      flex: 4,
                      child: Center(
                        child: AspectRatio(
                          aspectRatio: 9 / 10,
                          child: ChessBoardWidget(
                            pieces: _currentPieces,
                            selectedRow: null,
                            selectedCol: null,
                            validMoves: const [],
                            lastMoveFrom: currentStep.actualFrom,
                            lastMoveTo: currentStep.actualTo,
                            isFlipped: _isFlipped,
                            reduceMotion: false,
                            gameId: 9999,
                            moveCount: _currentMoveIndex,
                            riverWatermarkText: 'Cờ Chậm',
                            arrowFrom: _arrowFrom,
                            arrowTo: _arrowTo,
                            arrowColor: _arrowColor,
                            pieceBadges: _pieceBadges,
                            onCellTapped: (_, _) {},
                          ),
                        ),
                      ),
                    ),

                    // Cột kỳ thủ & nút đảo chiều bàn cờ (bên phải bàn cờ)
                    Container(
                      width: 82,
                      padding: const EdgeInsets.symmetric(vertical: 4),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                        children: [
                          // Đối thủ (Trần T...)
                          _buildMiniPlayerCard(
                            name: widget.playerBlackName,
                            title: 'Tập sự ⭐',
                            isBlack: true,
                            score: '0',
                          ),

                          // Nút xoay lật bàn cờ
                          GestureDetector(
                            onTap: () => setState(() => _isFlipped = !_isFlipped),
                            child: Container(
                              width: 36,
                              height: 36,
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                gradient: const LinearGradient(
                                  colors: [Color(0xFF8D6E63), Color(0xFF4E342E)],
                                ),
                                border: Border.all(
                                  color: const Color(0xFFFFD54F),
                                  width: 1.2,
                                ),
                                boxShadow: const [
                                  BoxShadow(
                                    color: Colors.black45,
                                    blurRadius: 4,
                                    offset: Offset(0, 2),
                                  ),
                                ],
                              ),
                              child: const Icon(
                                Icons.sync,
                                color: Color(0xFFFFE082),
                                size: 20,
                              ),
                            ),
                          ),

                          // Người chơi (Quang ...)
                          _buildMiniPlayerCard(
                            name: widget.playerRedName,
                            title: 'Tập sự ⭐',
                            isBlack: false,
                            score: '2.000',
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),

            // 3. THANH TAB THẺ TRÚC CỔ PHONG
            Container(
              height: 42,
              margin: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: const Color(0xFF231A14),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: const Color(0xFF5D4037), width: 1.2),
              ),
              child: Row(
                children: [
                  _buildBambooTab('Thế trận', 0),
                  _buildBambooTab('Tổng quan', 1),
                  _buildBambooTab('Nước đi', 2),
                  _buildBambooTab('Sai lầm', 3),
                ],
              ),
            ),

            // 4. NỘI DUNG CHI TIẾT TỪNG TAB
            Expanded(
              flex: 4,
              child: Container(
                margin: const EdgeInsets.symmetric(horizontal: 10),
                decoration: BoxDecoration(
                  color: AppColors.paperBg,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: AppColors.paperBorder, width: 1.5),
                  boxShadow: const [
                    BoxShadow(
                      color: Colors.black45,
                      blurRadius: 6,
                      offset: Offset(0, 3),
                    ),
                  ],
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(9),
                  child: _buildTabContent(currentStep),
                ),
              ),
            ),

            // 5. THANH ĐIỀU HƯỚNG DƯỚI ĐÁY
            _buildBottomControls(),
          ],
        ),
      ),
    );
  }

  Widget _buildMiniPlayerCard({
    required String name,
    required String title,
    required bool isBlack,
    required String score,
  }) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            gradient: LinearGradient(
              colors: isBlack
                  ? const [Color(0xFF37474F), Color(0xFF212121)]
                  : const [Color(0xFF00796B), Color(0xFF004D40)],
            ),
            border: Border.all(
              color: isBlack ? const Color(0xFF90CAF9) : const Color(0xFF80CBC4),
              width: 1.8,
            ),
          ),
          child: Center(
            child: Icon(
              isBlack ? Icons.pets : Icons.person,
              color: Colors.white,
              size: 22,
            ),
          ),
        ),
        const SizedBox(height: 2),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
          decoration: BoxDecoration(
            color: const Color(0xFF6D4C41),
            borderRadius: BorderRadius.circular(4),
          ),
          child: Text(
            title,
            style: GoogleFonts.philosopher(
              fontSize: 8,
              fontWeight: FontWeight.bold,
              color: const Color(0xFFFFE082),
            ),
          ),
        ),
        const SizedBox(height: 2),
        Text(
          name,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: GoogleFonts.philosopher(
            fontSize: 10,
            fontWeight: FontWeight.bold,
            color: const Color(0xFFE0E0E0),
          ),
        ),
      ],
    );
  }

  Widget _buildBambooTab(String title, int index) {
    final isSelected = _currentTabIndex == index;
    return Expanded(
      child: GestureDetector(
        onTap: () => setState(() => _currentTabIndex = index),
        child: Container(
          decoration: BoxDecoration(
            color: isSelected ? const Color(0xFFD49B4B) : Colors.transparent,
            borderRadius: BorderRadius.circular(6),
            gradient: isSelected
                ? const LinearGradient(
                    colors: [Color(0xFFE5A855), Color(0xFFB57A2A)],
                  )
                : null,
          ),
          child: Center(
            child: Text(
              title,
              style: GoogleFonts.philosopher(
                fontSize: 13,
                fontWeight: FontWeight.bold,
                color: isSelected ? Colors.black87 : const Color(0xFFBCAAA4),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildTabContent(_ReviewMoveStep currentStep) {
    switch (_currentTabIndex) {
      case 0: // Thế trận (Wave graph)
        return _buildAdvantageGraphTab();
      case 1: // Tổng quan
        return _buildOverviewTab();
      case 2: // Nước đi
        return _buildMoveDetailTab(currentStep);
      case 3: // Sai lầm
      default:
        return _buildBlundersTab();
    }
  }

  // TAB 1: Biểu đồ thế trận (Advantage Graph)
  Widget _buildAdvantageGraphTab() {
    return Padding(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Biểu đồ ưu thế theo từng nước đi',
                style: GoogleFonts.philosopher(
                  fontWeight: FontWeight.bold,
                  fontSize: 13,
                  color: AppColors.ink,
                ),
              ),
              Row(
                children: [
                  Container(
                    width: 10,
                    height: 10,
                    color: const Color(0xFFB71C1C),
                  ),
                  const SizedBox(width: 4),
                  Text('Đỏ', style: GoogleFonts.philosopher(fontSize: 11)),
                  const SizedBox(width: 8),
                  Container(
                    width: 10,
                    height: 10,
                    color: const Color(0xFF212121),
                  ),
                  const SizedBox(width: 4),
                  Text('Đen', style: GoogleFonts.philosopher(fontSize: 11)),
                ],
              ),
            ],
          ),
          const SizedBox(height: 8),
          Expanded(
            child: CustomPaint(
              size: const Size(double.infinity, double.infinity),
              painter: _AdvantageGraphPainter(
                steps: _reviewSteps,
                currentIndex: _currentMoveIndex,
              ),
            ),
          ),
          const SizedBox(height: 4),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Khai cờ', style: GoogleFonts.philosopher(fontSize: 10, color: AppColors.inkMuted)),
              Text('Trung cuộc', style: GoogleFonts.philosopher(fontSize: 10, color: AppColors.inkMuted)),
              Text('Tàn cuộc', style: GoogleFonts.philosopher(fontSize: 10, color: AppColors.inkMuted)),
            ],
          ),
        ],
      ),
    );
  }

  // TAB 2: Tổng quan (Accuracy & Stats)
  Widget _buildOverviewTab() {
    return Padding(
      padding: const EdgeInsets.all(12),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Độ chính xác kỳ thủ',
                  style: GoogleFonts.philosopher(
                    fontWeight: FontWeight.bold,
                    fontSize: 14,
                    color: AppColors.ink,
                  ),
                ),
                const SizedBox(height: 8),
                _buildStatRow('Quang (Đỏ):', '86.4%', const Color(0xFFB71C1C)),
                _buildStatRow('Thảo (Đen):', '62.1%', const Color(0xFF212121)),
                const Divider(height: 14, color: AppColors.paperBorder),
                Text(
                  'Phân loại nước đi:',
                  style: GoogleFonts.philosopher(
                    fontWeight: FontWeight.bold,
                    fontSize: 12,
                    color: AppColors.ink,
                  ),
                ),
                const SizedBox(height: 4),
                _buildBadgeStat('⭐ Nước tối ưu:', '4 nước', const Color(0xFF2E7D32)),
                _buildBadgeStat('✓ Nước tốt:', '1 nước', const Color(0xFF1565C0)),
                _buildBadgeStat('?? Đại sai lầm:', '1 nước', const Color(0xFFD32F2F)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStatRow(String label, String value, Color color) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: GoogleFonts.philosopher(fontSize: 12, color: AppColors.ink)),
          Text(
            value,
            style: GoogleFonts.cinzel(
              fontWeight: FontWeight.bold,
              fontSize: 13,
              color: color,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBadgeStat(String label, String value, Color color) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 1.5),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: GoogleFonts.philosopher(fontSize: 11, color: AppColors.inkMuted)),
          Text(
            value,
            style: GoogleFonts.philosopher(
              fontWeight: FontWeight.bold,
              fontSize: 11,
              color: color,
            ),
          ),
        ],
      ),
    );
  }

  // TAB 3: Chi tiết Nước đi (Matching media_1792897075662_d9f4da02.png)
  Widget _buildMoveDetailTab(_ReviewMoveStep currentStep) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Thẻ người chơi / đối thủ
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: const Color(0xFFF0DFCA),
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: currentStep.isBlunder
                    ? const Color(0xFFD32F2F)
                    : const Color(0xFF2E7D32),
                width: 1.2,
              ),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Column(
                  children: [
                    Container(
                      width: 32,
                      height: 32,
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        color: Color(0xFF37474F),
                      ),
                      child: const Icon(Icons.pets, color: Colors.white, size: 18),
                    ),
                    const SizedBox(height: 2),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                      decoration: BoxDecoration(
                        color: const Color(0xFFB71C1C),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        'Đối thủ',
                        style: GoogleFonts.philosopher(
                          fontSize: 8,
                          color: Colors.white,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              '${currentStep.moveNumber}. ${currentStep.notation} (Ưu thế ${currentStep.evaluation > 0 ? '+${currentStep.evaluation}' : currentStep.evaluation})',
                              style: GoogleFonts.philosopher(
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: currentStep.isBlunder
                                    ? const Color(0xFFD32F2F)
                                    : const Color(0xFF2E7D32),
                              ),
                            ),
                          ),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                            decoration: BoxDecoration(
                              color: currentStep.isBlunder
                                  ? const Color(0xFFD32F2F)
                                  : const Color(0xFF2E7D32),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Text(
                              '${currentStep.qualityBadge} ${currentStep.quality}',
                              style: GoogleFonts.philosopher(
                                fontSize: 9,
                                color: Colors.white,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text(
                        currentStep.explanation,
                        style: GoogleFonts.philosopher(
                          fontSize: 11,
                          color: AppColors.ink,
                          height: 1.3,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),

          const SizedBox(height: 8),

          // Thẻ AI Đề xuất (Robot AI)
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: const Color(0xFFE8F5E9),
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: const Color(0xFF81C784), width: 1.2),
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Column(
                  children: [
                    Container(
                      width: 32,
                      height: 32,
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        color: Color(0xFF1E88E5),
                      ),
                      child: const Icon(Icons.smart_toy, color: Colors.white, size: 18),
                    ),
                    const SizedBox(height: 2),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                      decoration: BoxDecoration(
                        color: const Color(0xFF1565C0),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(
                        'AI',
                        style: GoogleFonts.philosopher(
                          fontSize: 8,
                          color: Colors.white,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              currentStep.aiAlternative,
                              style: GoogleFonts.philosopher(
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                                color: const Color(0xFF2E7D32),
                              ),
                            ),
                          ),
                          const Icon(Icons.star, size: 14, color: Color(0xFF2E7D32)),
                          Text(
                            ' Nước tối ưu',
                            style: GoogleFonts.philosopher(
                              fontSize: 9,
                              color: const Color(0xFF2E7D32),
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text(
                        currentStep.aiReasoning,
                        style: GoogleFonts.philosopher(
                          fontSize: 11,
                          color: AppColors.ink,
                          height: 1.3,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),

          const SizedBox(height: 8),

          // Suy diễn thế trận & Nút tua [◀] [▶]
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Text(
                    'Suy diễn thế trận: ',
                    style: GoogleFonts.philosopher(
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFF2E7D32),
                    ),
                  ),
                  const Icon(Icons.search, size: 14, color: Color(0xFF2E7D32)),
                ],
              ),
              Row(
                children: [
                  _buildMiniNavBtn(Icons.play_arrow, () {}),
                  const SizedBox(width: 4),
                  _buildMiniNavBtn(
                    Icons.chevron_left,
                    _currentMoveIndex > 0 ? () => _applyStep(_currentMoveIndex - 1) : null,
                  ),
                  const SizedBox(width: 4),
                  _buildMiniNavBtn(
                    Icons.chevron_right,
                    _currentMoveIndex < _reviewSteps.length - 1
                        ? () => _applyStep(_currentMoveIndex + 1)
                        : null,
                  ),
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildMiniNavBtn(IconData icon, VoidCallback? onTap) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(4),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        decoration: BoxDecoration(
          color: onTap != null ? const Color(0xFFD49B4B) : const Color(0xFFBDBDBD),
          borderRadius: BorderRadius.circular(4),
        ),
        child: Icon(icon, size: 16, color: Colors.white),
      ),
    );
  }

  // TAB 4: Danh sách Sai lầm (Matching media_1792897071017_da0230d7.png)
  Widget _buildBlundersTab() {
    return Column(
      children: [
        // Tiêu đề cột
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          color: const Color(0xFFE0CFB5),
          child: Row(
            children: [
              SizedBox(
                width: 30,
                child: Text('Stt', style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, fontSize: 11)),
              ),
              Expanded(
                child: Row(
                  children: [
                    Text(
                      'Sai lầm bên đen',
                      style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, fontSize: 11),
                    ),
                    const SizedBox(width: 4),
                    const Icon(Icons.sync, size: 12, color: AppColors.inkMuted),
                  ],
                ),
              ),
              Text(
                'Nước khác',
                style: GoogleFonts.philosopher(
                  fontWeight: FontWeight.bold,
                  fontSize: 11,
                  color: const Color(0xFF2E7D32),
                ),
              ),
            ],
          ),
        ),

        // Danh sách các nước sai lầm
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(8),
            children: [
              Container(
                margin: const EdgeInsets.only(bottom: 6),
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                decoration: BoxDecoration(
                  color: const Color(0xFFFF9800),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Row(
                  children: [
                    SizedBox(
                      width: 26,
                      child: Text(
                        '6',
                        style: GoogleFonts.cinzel(
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                          fontSize: 13,
                        ),
                      ),
                    ),
                    Expanded(
                      child: Row(
                        children: [
                          Text(
                            'Pháo 8 tấn 2',
                            style: GoogleFonts.philosopher(
                              fontWeight: FontWeight.bold,
                              color: Colors.white,
                              fontSize: 13,
                            ),
                          ),
                          const SizedBox(width: 6),
                          Container(
                            padding: const EdgeInsets.all(2),
                            decoration: const BoxDecoration(
                              shape: BoxShape.circle,
                              color: Colors.white,
                            ),
                            child: const Icon(Icons.question_mark, size: 10, color: Color(0xFFFF9800)),
                          ),
                        ],
                      ),
                    ),
                    Text(
                      'Mã 2 tấn 3',
                      style: GoogleFonts.philosopher(
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  // 5. THANH ĐIỀU HƯỚNG DƯỚI ĐÁY (Bottom Toolbar)
  Widget _buildBottomControls() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: const BoxDecoration(
        color: Color(0xFF1E1714),
        border: Border(top: BorderSide(color: Color(0xFF4A3728), width: 1.2)),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceAround,
        children: [
          _buildBottomActionBtn(Icons.menu, 'Tùy chọn', () => Navigator.pop(context)),
          _buildBottomActionBtn(
            Icons.explore,
            'Bảng đ.khiển',
            () => setState(() => _currentTabIndex = 1),
          ),
          _buildBottomActionBtn(
            Icons.skip_previous,
            'Nước trước',
            _currentMoveIndex > 0 ? () => _applyStep(_currentMoveIndex - 1) : null,
          ),
          _buildBottomActionBtn(
            Icons.skip_next,
            'Nước sau',
            _currentMoveIndex < _reviewSteps.length - 1
                ? () => _applyStep(_currentMoveIndex + 1)
                : null,
          ),
        ],
      ),
    );
  }

  Widget _buildBottomActionBtn(IconData icon, String label, VoidCallback? onTap) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(8),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              size: 20,
              color: onTap != null ? const Color(0xFFFFD54F) : const Color(0xFF757575),
            ),
            const SizedBox(height: 2),
            Text(
              label,
              style: GoogleFonts.philosopher(
                fontSize: 10,
                color: onTap != null ? const Color(0xFFE0E0E0) : const Color(0xFF757575),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ReviewMoveStep {
  final int moveNumber;
  final String notation;
  final String playerSide;
  final int evaluation;
  final bool isOptimal;
  final bool isBlunder;
  final String quality;
  final String qualityBadge;
  final String explanation;
  final String aiAlternative;
  final String aiReasoning;
  final (int, int) actualFrom;
  final (int, int) actualTo;
  final (int, int) optimalFrom;
  final (int, int) optimalTo;

  _ReviewMoveStep({
    required this.moveNumber,
    required this.notation,
    required this.playerSide,
    required this.evaluation,
    required this.isOptimal,
    this.isBlunder = false,
    required this.quality,
    required this.qualityBadge,
    required this.explanation,
    required this.aiAlternative,
    required this.aiReasoning,
    required this.actualFrom,
    required this.actualTo,
    required this.optimalFrom,
    required this.optimalTo,
  });
}

class _AdvantageGraphPainter extends CustomPainter {
  final List<_ReviewMoveStep> steps;
  final int currentIndex;

  _AdvantageGraphPainter({
    required this.steps,
    required this.currentIndex,
  });

  @override
  void paint(Canvas canvas, Size size) {
    if (steps.isEmpty || size.width <= 0 || size.height <= 0) return;

    final midY = size.height / 2;

    // Đường 0 ở giữa
    canvas.drawLine(
      Offset(0, midY),
      Offset(size.width, midY),
      Paint()
        ..color = AppColors.paperBorder
        ..strokeWidth = 1,
    );

    // Vẽ sóng ưu thế
    final path = Path();
    final stepWidth = size.width / (steps.length - 1).clamp(1, 99);

    for (int i = 0; i < steps.length; i++) {
      final x = i * stepWidth;
      // eval > 0 (Red lead) vẽ lên trên midY, eval < 0 (Black lead) vẽ xuống dưới
      final eval = steps[i].evaluation.clamp(-200, 200);
      final y = midY - (eval / 200.0) * (midY - 8);

      if (i == 0) {
        path.moveTo(x, y);
      } else {
        path.lineTo(x, y);
      }

      // Điểm mốc trên sóng
      final pointPaint = Paint()
        ..color = i == currentIndex
            ? const Color(0xFFFF9800)
            : (eval >= 0 ? const Color(0xFFB71C1C) : const Color(0xFF212121));
      canvas.drawCircle(Offset(x, y), i == currentIndex ? 4.5 : 2.5, pointPaint);
    }

    final wavePaint = Paint()
      ..color = const Color(0xFFB71C1C)
      ..strokeWidth = 2.0
      ..style = PaintingStyle.stroke;
    canvas.drawPath(path, wavePaint);
  }

  @override
  bool shouldRepaint(covariant _AdvantageGraphPainter oldDelegate) {
    return oldDelegate.currentIndex != currentIndex;
  }
}
