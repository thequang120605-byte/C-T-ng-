import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';
import '../widgets/chess_board_widget.dart';

class PuzzleRushScreen extends StatefulWidget {
  final VoidCallback onBack;

  const PuzzleRushScreen({super.key, required this.onBack});

  @override
  State<PuzzleRushScreen> createState() => _PuzzleRushScreenState();
}

class _PuzzleRushScreenState extends State<PuzzleRushScreen> {
  int _secondsLeft = 180; // 3 phút
  Timer? _timer;
  int _score = 0;
  int _streak = 0;
  int _maxStreak = 0;
  int _lives = 3;
  bool _isGameOver = false;

  List<String> _puzzleKeys = [];
  int _currentPuzzleIndex = 0;
  PuzzleDetailDto? _currentPuzzle;
  List<PieceDto> _currentPieces = [];
  (int, int)? _selectedSquare;
  List<ValidMoveDto> _validMoves = [];
  bool _isBusy = false;

  @override
  void initState() {
    super.initState();
    _startRush();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _startRush() async {
    setState(() {
      _secondsLeft = 180;
      _score = 0;
      _streak = 0;
      _maxStreak = 0;
      _lives = 3;
      _isGameOver = false;
      _isBusy = true;
    });

    try {
      final catalog = await AppServices.instance.api.getPuzzleCatalogAsync();
      if (catalog != null && catalog.levels.isNotEmpty) {
        _puzzleKeys = [];
        for (final lvl in catalog.levels) {
          _puzzleKeys.addAll(lvl.keys);
        }
      }
      if (_puzzleKeys.isEmpty) {
        _puzzleKeys = ['stage_01', 'stage_02', 'stage_03', 'stage_04', 'stage_05'];
      }
      _currentPuzzleIndex = 0;
      await _loadCurrentPuzzle();
      _startTimer();
    } catch (_) {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  void _startTimer() {
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (_secondsLeft <= 1) {
        t.cancel();
        _endGame('Hết thời gian tranh tài!');
      } else {
        setState(() => _secondsLeft--);
      }
    });
  }

  Future<void> _loadCurrentPuzzle() async {
    if (_puzzleKeys.isEmpty) return;
    final key = _puzzleKeys[_currentPuzzleIndex % _puzzleKeys.length];
    final detail = await AppServices.instance.api.getPuzzleDetailAsync(key);
    if (!mounted) return;
    if (detail != null) {
      _currentPuzzle = detail;
      _currentPieces = List<PieceDto>.from(detail.pieces);
      _selectedSquare = null;
      _validMoves = detail.legalMoves;
      _isBusy = false;
      setState(() {});
    }
  }

  void _handleSquareTapped(int row, int col) {
    if (_isGameOver || _isBusy) return;

    if (_selectedSquare == null) {
      final p = _currentPieces.where((pc) => pc.row == row && pc.col == col).firstOrNull;
      if (p != null && p.color.toLowerCase() == 'red') {
        HapticFeedback.selectionClick();
        setState(() => _selectedSquare = (row, col));
      }
      return;
    }

    final from = _selectedSquare!;
    if (from.$1 == row && from.$2 == col) {
      setState(() => _selectedSquare = null);
      return;
    }

    _tryMove(from.$1, from.$2, row, col);
  }

  Future<void> _tryMove(int fR, int fC, int tR, int tC) async {
    if (_currentPuzzle == null) return;
    setState(() => _isBusy = true);
    HapticFeedback.lightImpact();

    try {
      final res = await AppServices.instance.api.submitPuzzleMoveAsync(
        _currentPuzzle!.key,
        [
          {'fromRow': fR, 'fromCol': fC, 'toRow': tR, 'toCol': tC}
        ],
      );

      if (!mounted) return;
      final result = res?['result']?.toString() ?? 'wrong';

      if (result == 'solved' || result == 'correct') {
        AppServices.instance.sound.playWin();
        _streak++;
        if (_streak > _maxStreak) _maxStreak = _streak;
        final bonus = _streak * 20;
        _score += (100 + bonus);

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFF2E7D32),
            duration: const Duration(milliseconds: 600),
            content: Text(
              '🎯 Xuất sắc! +${100 + bonus} điểm (Combo x$_streak)',
              style: GoogleFonts.philosopher(fontWeight: FontWeight.bold),
            ),
          ),
        );

        await Future.delayed(const Duration(milliseconds: 400));
        if (mounted) {
          _currentPuzzleIndex++;
          await _loadCurrentPuzzle();
        }
      } else {
        AppServices.instance.sound.playIllegal();
        HapticFeedback.heavyImpact();
        _lives--;
        _streak = 0;

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFFC0392B),
            duration: const Duration(milliseconds: 700),
            content: Text(
              '❌ Nước đi chưa tối ưu! Mất 1 mạng',
              style: GoogleFonts.philosopher(fontWeight: FontWeight.bold),
            ),
          ),
        );

        if (_lives <= 0) {
          _endGame('Hết mạng thi đấu!');
        } else {
          _selectedSquare = null;
          _isBusy = false;
          setState(() {});
        }
      }
    } catch (_) {
      _selectedSquare = null;
      _isBusy = false;
      setState(() {});
    }
  }

  void _endGame(String reason) {
    _timer?.cancel();
    _isGameOver = true;
    setState(() {});
    AppServices.instance.sound.playWin();

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => AlertDialog(
        backgroundColor: const Color(0xFF1E1714),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFFFD54F), width: 2),
        ),
        title: Column(
          children: [
            const Icon(Icons.flash_on, color: Color(0xFFFFD54F), size: 48),
            const SizedBox(height: 6),
            Text(
              'KẾT QUẢ THẦN TỐC',
              style: GoogleFonts.cinzel(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: const Color(0xFFFFE082),
              ),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              reason,
              style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4), fontSize: 13),
            ),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
              decoration: BoxDecoration(
                color: const Color(0xFF140F0D),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFFFD54F), width: 1.5),
              ),
              child: Column(
                children: [
                  Text('TỔNG ĐIỂM', style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFF8D6E63))),
                  Text(
                    '$_score',
                    style: GoogleFonts.cinzel(
                      fontSize: 34,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFFFFD54F),
                    ),
                  ),
                  Text('🔥 Chuỗi liên trảm cao nhất: $_maxStreak',
                      style: GoogleFonts.philosopher(fontSize: 12, color: const Color(0xFF81C784))),
                ],
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context);
              widget.onBack();
            },
            child: Text('Trở Về', style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4))),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(context);
              _startRush();
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              side: const BorderSide(color: Color(0xFFFFD54F)),
            ),
            child: Text(
              'Chơi Lại',
              style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.white),
            ),
          ),
        ],
      ),
    );
  }

  String get _formattedTime {
    final m = _secondsLeft ~/ 60;
    final s = _secondsLeft % 60;
    return '${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF140F0D),
      body: SafeArea(
        child: Column(
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: const BoxDecoration(
                color: Color(0xFF1E1714),
                border: Border(bottom: BorderSide(color: Color(0xFF4A3728), width: 1.5)),
              ),
              child: Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_ios, color: Color(0xFFFFD54F), size: 18),
                    onPressed: widget.onBack,
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: _secondsLeft < 30 ? const Color(0xFFC0392B) : const Color(0xFF2C1E18),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: _secondsLeft < 30 ? Colors.redAccent : const Color(0xFFFFD54F),
                        width: 1.2,
                      ),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.timer, color: Colors.white, size: 16),
                        const SizedBox(width: 4),
                        Text(
                          _formattedTime,
                          style: GoogleFonts.cinzel(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const Spacer(),
                  Row(
                    children: List.generate(3, (idx) {
                      final hasHeart = idx < _lives;
                      return Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 2),
                        child: Icon(
                          hasHeart ? Icons.favorite : Icons.favorite_border,
                          color: hasHeart ? const Color(0xFFE53935) : const Color(0xFF5D4037),
                          size: 20,
                        ),
                      );
                    }),
                  ),
                  const SizedBox(width: 12),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: const Color(0xFF140F0D),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: const Color(0xFFFFD54F)),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.bolt, color: Color(0xFFFFD54F), size: 16),
                        Text(
                          '$_score',
                          style: GoogleFonts.cinzel(
                            fontSize: 15,
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
            if (_streak > 1)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(vertical: 4),
                color: const Color(0xFFC0392B),
                child: Center(
                  child: Text(
                    '🔥 LIÊN TRẢM x$_streak (Thưởng +${_streak * 20} điểm)',
                    style: GoogleFonts.philosopher(
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                      fontSize: 12,
                    ),
                  ),
                ),
              ),
            Expanded(
              child: Center(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 12),
                  child: AspectRatio(
                    aspectRatio: 9 / 10,
                    child: Container(
                      decoration: BoxDecoration(
                        color: const Color(0xFFD4A373),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: const Color(0xFF4A3728), width: 3),
                        boxShadow: const [
                          BoxShadow(color: Colors.black54, blurRadius: 8, offset: Offset(0, 4)),
                        ],
                      ),
                      child: ChessBoardWidget(
                        pieces: _currentPieces,
                        selectedRow: _selectedSquare?.$1,
                        selectedCol: _selectedSquare?.$2,
                        validMoves: _validMoves,
                        lastMoveFrom: null,
                        lastMoveTo: null,
                        isFlipped: false,
                        reduceMotion: false,
                        gameId: 0,
                        moveCount: _currentPuzzleIndex,
                        riverWatermarkText: 'Thần Tốc',
                        onCellTapped: _handleSquareTapped,
                      ),
                    ),
                  ),
                ),
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              color: const Color(0xFF1E1714),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Ải ${_currentPuzzleIndex + 1}: ${_currentPuzzle?.title ?? "Cờ thế xuất kích"}',
                    style: GoogleFonts.philosopher(color: const Color(0xFFD7CCC8), fontSize: 13),
                  ),
                  Text(
                    'Quân Đỏ đi trước',
                    style: GoogleFonts.philosopher(
                      color: const Color(0xFFFFD54F),
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
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
}
