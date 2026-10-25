import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../offline/board.dart';
import '../offline/piece.dart';
import '../services/app_services.dart';
import '../theme/app_theme.dart';
import '../widgets/advantage_bar_widget.dart';
import '../widgets/chess_board_widget.dart';
import '../widgets/circular_turn_timer.dart';
import 'game_review_screen.dart';

class GameScreen extends StatefulWidget {
  final int gameId;
  final int difficulty;
  final String mode; // 'PvE', 'Online', or 'Puzzle'
  final String seatHint; // 'Red' or 'Black'
  final String timeControl; // '5 Phút' (Cờ Chớp) or '10 Phút' (Cờ Chậm)
  final String? puzzleKey;
  final String? puzzleTitle;
  final String? puzzleGoal;

  const GameScreen({
    super.key,
    required this.gameId,
    this.difficulty = 2,
    this.mode = 'PvE',
    this.seatHint = 'Red',
    this.timeControl = 'Cờ Chậm',
    this.puzzleKey,
    this.puzzleTitle,
    this.puzzleGoal,
  });

  @override
  State<GameScreen> createState() => _GameScreenState();
}

class _GameScreenState extends State<GameScreen> {
  late int _currentGameId;
  late int _difficulty;
  late String _gameMode;
  late String _seatHint;

  // Thuộc tính riêng cho Cờ Thế Vượt Màn
  bool get _isPuzzleMode => widget.puzzleKey != null && widget.puzzleKey!.isNotEmpty;
  PuzzleDetailDto? _puzzleDetail;
  final List<Map<String, int>> _puzzleMovesMade = [];
  List<ValidMoveDto> _puzzleLegalMoves = [];
  bool _isPuzzleSolved = false;
  (int, int)? _hintArrowFrom;
  (int, int)? _hintArrowTo;

  GameResponseDto? _game;
  AuthUserDto? _user;
  String _myPlayerColor = '';
  bool _isFlipped = false;

  int? _selectedRow;
  int? _selectedCol;
  List<ValidMoveDto> _validMoves = [];
  (int, int)? _lastMoveFrom;
  (int, int)? _lastMoveTo;

  bool _isBusy = false;
  bool _isAiThinking = false;
  String _statusMessage = 'Đang chuẩn bị bàn cờ…';
  String _connectionText = 'Chưa kết nối';
  int _boardEpoch = 0;

  // Timers (giả lập hoặc đồng bộ)
  int _playerRemainingSeconds = 596; // 09:56
  int _opponentRemainingSeconds = 591; // 09:51
  Timer? _countdownTimer;

  StreamSubscription? _updateSub;
  StreamSubscription? _reconnectSub;
  StreamSubscription? _stateSub;
  StreamSubscription? _chatSub;
  StreamSubscription? _drawOfferSub;
  StreamSubscription? _drawDeclineSub;
  StreamSubscription? _rematchSub;

  @override
  void initState() {
    super.initState();
    _currentGameId = widget.gameId;
    _difficulty = widget.difficulty;
    _gameMode = widget.mode;
    _seatHint = widget.seatHint;

    _startTimer();
    _initSession();
  }

  void _startTimer() {
    _countdownTimer?.cancel();
    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted || _isGameOver) return;
      setState(() {
        if (_isMyTurn) {
          if (_playerRemainingSeconds > 0) _playerRemainingSeconds--;
        } else {
          if (_opponentRemainingSeconds > 0) _opponentRemainingSeconds--;
        }
      });
    });
  }

  Future<void> _initSession() async {
    _user = await AppServices.instance.auth.getCurrentUserAsync();

    if (_isPuzzleMode) {
      _connectionText = 'Chế độ Cờ Thế';
      await _loadPuzzle();
      return;
    }

    if (_currentGameId > 0) {
      final signalR = AppServices.instance.signalR;
      _updateSub = signalR.onGameUpdated.listen((dto) {
        if (mounted && dto.id == _currentGameId) {
          _applyGameUpdate(dto);
        }
      });
      _reconnectSub = signalR.onReconnected.listen((_) {
        if (mounted) _reloadGame();
      });
      _stateSub = signalR.onConnectionStateChanged.listen((state) {
        if (mounted) setState(() => _connectionText = state);
      });

      // Nhận tin nhắn chat từ đối thủ thời gian thực
      _chatSub = signalR.onChatMessage.listen((msg) {
        if (mounted && msg.gameId == _currentGameId) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              backgroundColor: const Color(0xFF2C3E50),
              behavior: SnackBarBehavior.floating,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(10),
                side: const BorderSide(color: Color(0xFFFFD54F), width: 1),
              ),
              content: Row(
                children: [
                  const Icon(Icons.chat, color: Color(0xFFFFD54F), size: 18),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      '${msg.sender}: ${msg.message}',
                      style: GoogleFonts.philosopher(color: Colors.white, fontSize: 13),
                    ),
                  ),
                ],
              ),
              duration: const Duration(seconds: 4),
            ),
          );
        }
      });

      // Nhận lời xin hòa từ đối thủ thời gian thực
      _drawOfferSub = signalR.onDrawOffered.listen((data) {
        if (!mounted) return;
        final from = data['fromPlayerName']?.toString() ?? 'Đối thủ';
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (dCtx) => AlertDialog(
            backgroundColor: AppColors.darkSurface,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14),
              side: const BorderSide(color: Color(0xFFFFD54F), width: 1.5),
            ),
            title: Row(
              children: [
                const Icon(Icons.handshake, color: Color(0xFFFFD54F)),
                const SizedBox(width: 8),
                Text(
                  'ĐỀ NGHỊ HÒA VÁN CỜ',
                  style: GoogleFonts.cinzel(
                    color: const Color(0xFFFFE082),
                    fontWeight: FontWeight.bold,
                    fontSize: 15,
                  ),
                ),
              ],
            ),
            content: Text(
              '$from muốn xin hòa ván cờ này. Bạn có đồng ý bắt tay giảng hòa không?',
              style: GoogleFonts.philosopher(color: AppColors.paperLight, fontSize: 13),
            ),
            actions: [
              TextButton(
                onPressed: () async {
                  Navigator.pop(dCtx);
                  await AppServices.instance.api.respondDrawAsync(_currentGameId, false);
                },
                child: Text('Từ Chối', style: GoogleFonts.philosopher(color: Colors.redAccent)),
              ),
              ElevatedButton(
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF2E7D32),
                  side: const BorderSide(color: Color(0xFFFFD54F)),
                ),
                onPressed: () async {
                  Navigator.pop(dCtx);
                  await AppServices.instance.api.respondDrawAsync(_currentGameId, true);
                  _reloadGame();
                },
                child: Text('Đồng Ý Hòa', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
              ),
            ],
          ),
        );
      });

      // Nhận thông báo đối thủ từ chối hòa
      _drawDeclineSub = signalR.onDrawDeclined.listen((_) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              backgroundColor: const Color(0xFFB71C1C),
              content: Text('Đối thủ đã từ chối lời xin hòa! Tiếp tục thi đấu.', style: GoogleFonts.philosopher()),
            ),
          );
        }
      });

      // Nhận sự kiện bắt đầu ván mới (Rematch)
      _rematchSub = signalR.onRematchStarted.listen((data) {
        if (!mounted) return;
        final newId = data['newGameId'] as int? ?? 0;
        if (newId > 0) {
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(
              builder: (_) => GameScreen(
                gameId: newId,
                difficulty: _difficulty,
                mode: _gameMode,
                seatHint: _myPlayerColor == 'Red' ? 'Black' : 'Red',
                timeControl: widget.timeControl,
              ),
            ),
          );
        }
      });

      try {
        await signalR.joinGameAsync(_currentGameId);
      } catch (_) {
        if (mounted) {
          setState(() => _connectionText = 'Chưa đồng bộ trực tiếp');
        }
      }
    } else {
      _connectionText = 'Ngoại tuyến';
    }

    await _reloadGame();
  }

  @override
  void dispose() {
    _countdownTimer?.cancel();
    _updateSub?.cancel();
    _reconnectSub?.cancel();
    _stateSub?.cancel();
    _chatSub?.cancel();
    _drawOfferSub?.cancel();
    _drawDeclineSub?.cancel();
    _rematchSub?.cancel();
    if (_currentGameId > 0) {
      AppServices.instance.signalR.leaveGameAsync(_currentGameId);
    }
    super.dispose();
  }

  Future<void> _reloadGame() async {
    if (!mounted) return;
    setState(() => _isBusy = true);
    final epoch = _boardEpoch;
    try {
      final dto =
          await AppServices.instance.api.getGameAsync(_currentGameId);
      if (mounted && epoch == _boardEpoch) {
        _applyGameUpdate(dto);
      }
    } catch (e) {
      if (mounted) setState(() => _statusMessage = e.toString());
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _loadPuzzle() async {
    setState(() {
      _isBusy = true;
      _hintArrowFrom = null;
      _hintArrowTo = null;
      _selectedRow = null;
      _selectedCol = null;
      _validMoves = [];
      _isPuzzleSolved = false;
    });
    try {
      final detail = await AppServices.instance.api.getPuzzleDetailAsync(widget.puzzleKey!);
      if (detail != null && mounted) {
        _puzzleDetail = detail;
        _puzzleMovesMade.clear();
        _puzzleLegalMoves = detail.legalMoves;
        _myPlayerColor = detail.attacker;
        _isFlipped = _myPlayerColor.toLowerCase() == 'black';
        _statusMessage = detail.goal.isNotEmpty
            ? detail.goal
            : '${detail.title} - Chiếu bí trong ${detail.mateIn} nước';

        _game = GameResponseDto(
          id: 0,
          gameMode: 'Puzzle',
          currentTurn: detail.attacker,
          status: 'InProgress',
          boardPieces: detail.pieces,
          moves: [],
          isCheck: false,
          redPlayerUsername: detail.attacker.toLowerCase() == 'red'
              ? (_user?.username.isNotEmpty == true ? _user!.username : 'Kỳ Thủ')
              : 'Trần Thị Phương Thảo (AI)',
          blackPlayerUsername: detail.attacker.toLowerCase() == 'black'
              ? (_user?.username.isNotEmpty == true ? _user!.username : 'Kỳ Thủ')
              : 'Đối Thủ Cờ Thế',
        );
      }
    } catch (e) {
      if (mounted) setState(() => _statusMessage = 'Lỗi nạp cờ thế: $e');
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _makePuzzleMove(int fromX, int fromY, int toX, int toY) async {
    final isCapture = _game?.boardPieces.any((p) => p.row == toX && p.col == toY) ?? false;
    if (isCapture) {
      HapticFeedback.mediumImpact();
      AppServices.instance.sound.playCapture();
    } else {
      HapticFeedback.lightImpact();
      AppServices.instance.sound.playMove();
    }
    setState(() => _isBusy = true);

    _puzzleMovesMade.add({
      'fromRow': fromX,
      'fromCol': fromY,
      'toRow': toX,
      'toCol': toY,
    });

    final updatedPieces = List<PieceDto>.from(_game?.boardPieces ?? []);
    updatedPieces.removeWhere((p) => p.row == toX && p.col == toY);
    final movingIdx = updatedPieces.indexWhere((p) => p.row == fromX && p.col == fromY);
    if (movingIdx >= 0) {
      final moving = updatedPieces[movingIdx];
      updatedPieces[movingIdx] = PieceDto(
        row: toX,
        col: toY,
        type: moving.type,
        color: moving.color,
      );
    }

    _selectedRow = null;
    _selectedCol = null;
    _validMoves = [];
    _hintArrowFrom = null;
    _hintArrowTo = null;
    _lastMoveFrom = (fromX, fromY);
    _lastMoveTo = (toX, toY);

    if (_game != null) {
      _game = GameResponseDto(
        id: _game!.id,
        gameMode: _game!.gameMode,
        currentTurn: _game!.currentTurn,
        status: _game!.status,
        boardPieces: updatedPieces,
        moves: _game!.moves,
        isCheck: _game!.isCheck,
        redPlayerUsername: _game!.redPlayerUsername,
        blackPlayerUsername: _game!.blackPlayerUsername,
      );
    }
    setState(() {});

    try {
      final res = await AppServices.instance.api.submitPuzzleMoveAsync(
        widget.puzzleKey!,
        _puzzleMovesMade,
      );

      if (!mounted) return;

      if (res == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            backgroundColor: Color(0xFFC0392B),
            content: Text('Không thể kết nối đến máy chủ.'),
          ),
        );
        _puzzleMovesMade.removeLast();
        await _loadPuzzle();
        return;
      }

      final result = res['result']?.toString() ?? 'wrong';
      final message = res['message']?.toString() ?? '';

      if (result == 'solved') {
        _isPuzzleSolved = true;
        _statusMessage = 'Chiếu bí! Đã hoàn thành ải cờ thế.';
        if (res['state'] != null && res['state']['pieces'] != null) {
          final rawPieces = res['state']['pieces'] as List<dynamic>;
          final finalPieces = rawPieces
              .map((p) => PieceDto.fromJson(p as Map<String, dynamic>))
              .toList();
          _game = GameResponseDto(
            id: 0,
            gameMode: 'Puzzle',
            currentTurn: _myPlayerColor,
            status: 'RedWins',
            boardPieces: finalPieces,
            moves: _game?.moves ?? [],
            isCheck: true,
            redPlayerUsername: _game?.redPlayerUsername,
            blackPlayerUsername: _game?.blackPlayerUsername,
          );
        }
        setState(() {});
        _showPuzzleVictoryModal(message);
      } else if (result == 'correct') {
        _statusMessage = message.isNotEmpty ? message : 'Nước đi chính xác! Tiếp tục hóa giải.';

        if (res['reply'] != null) {
          final reply = res['reply'] as Map<String, dynamic>;
          final rfX = (reply['fromRow'] ?? 0) as int;
          final rfY = (reply['fromCol'] ?? 0) as int;
          final rtX = (reply['toRow'] ?? 0) as int;
          final rtY = (reply['toCol'] ?? 0) as int;

          await Future.delayed(const Duration(milliseconds: 350));
          if (!mounted) return;

          _lastMoveFrom = (rfX, rfY);
          _lastMoveTo = (rtX, rtY);
        }

        if (res['state'] != null) {
          final state = res['state'] as Map<String, dynamic>;
          final rawPieces = state['pieces'] as List<dynamic>? ?? [];
          final rawLegal = state['legalMoves'] as List<dynamic>? ?? [];
          _puzzleLegalMoves = rawLegal
              .map((m) => ValidMoveDto.fromJson(m as Map<String, dynamic>))
              .toList();
          final nextPieces = rawPieces
              .map((p) => PieceDto.fromJson(p as Map<String, dynamic>))
              .toList();

          _game = GameResponseDto(
            id: 0,
            gameMode: 'Puzzle',
            currentTurn: _myPlayerColor,
            status: 'InProgress',
            boardPieces: nextPieces,
            moves: _game?.moves ?? [],
            isCheck: state['checkedColor'] != null,
            redPlayerUsername: _game?.redPlayerUsername,
            blackPlayerUsername: _game?.blackPlayerUsername,
          );
        }
        setState(() {});
      } else if (result == 'wrong') {
        AppServices.instance.sound.playIllegal();
        _statusMessage = message;
        if (res['reply'] != null) {
          final reply = res['reply'] as Map<String, dynamic>;
          final rfX = (reply['fromRow'] ?? 0) as int;
          final rfY = (reply['fromCol'] ?? 0) as int;
          final rtX = (reply['toRow'] ?? 0) as int;
          final rtY = (reply['toCol'] ?? 0) as int;
          _lastMoveFrom = (rfX, rfY);
          _lastMoveTo = (rtX, rtY);
        }
        if (res['state'] != null && res['state']['pieces'] != null) {
          final rawPieces = res['state']['pieces'] as List<dynamic>;
          final afterCounterPieces = rawPieces
              .map((p) => PieceDto.fromJson(p as Map<String, dynamic>))
              .toList();
          _game = GameResponseDto(
            id: 0,
            gameMode: 'Puzzle',
            currentTurn: _myPlayerColor,
            status: 'InProgress',
            boardPieces: afterCounterPieces,
            moves: _game?.moves ?? [],
            isCheck: false,
            redPlayerUsername: _game?.redPlayerUsername,
            blackPlayerUsername: _game?.blackPlayerUsername,
          );
        }
        setState(() {});

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFFC0392B),
            duration: const Duration(seconds: 4),
            content: Row(
              children: [
                const Icon(Icons.warning_amber_rounded, color: Colors.white, size: 20),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    message.isNotEmpty ? message : 'Nước đi chưa chuẩn xác! Bị phản đòn.',
                    style: GoogleFonts.philosopher(fontWeight: FontWeight.bold),
                  ),
                ),
              ],
            ),
            action: SnackBarAction(
              label: 'Thử lại',
              textColor: const Color(0xFFFFD54F),
              onPressed: () => _loadPuzzle(),
            ),
          ),
        );
      } else {
        _puzzleMovesMade.removeLast();
        await _loadPuzzle();
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFFC0392B),
            content: Text(message.isNotEmpty ? message : 'Nước đi không hợp lệ!'),
          ),
        );
      }
    } catch (e) {
      if (mounted) setState(() => _statusMessage = e.toString());
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _requestPuzzleHint() async {
    if (_isBusy || !_isPuzzleMode) return;
    setState(() => _isBusy = true);
    try {
      final res = await AppServices.instance.api.getPuzzleHintAsync(
        widget.puzzleKey!,
        _puzzleMovesMade,
      );
      if (res != null && res['available'] == true && mounted) {
        final fR = (res['fromRow'] ?? 0) as int;
        final fC = (res['fromCol'] ?? 0) as int;
        final tR = (res['toRow'] ?? 0) as int;
        final tC = (res['toCol'] ?? 0) as int;
        final msg = res['message']?.toString() ?? 'Gợi ý nước đi';

        setState(() {
          _selectedRow = fR;
          _selectedCol = fC;
          _validMoves = [ValidMoveDto(row: tR, col: tC, isCapture: false)];
          _hintArrowFrom = (fR, fC);
          _hintArrowTo = (tR, tC);
          _statusMessage = msg;
        });

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: const Color(0xFF1565C0),
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10),
              side: const BorderSide(color: Color(0xFFFFD54F), width: 1.2),
            ),
            content: Row(
              children: [
                const Icon(Icons.lightbulb, color: Color(0xFFFFD54F), size: 20),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(msg, style: GoogleFonts.philosopher(fontWeight: FontWeight.bold)),
                ),
              ],
            ),
          ),
        );
      }
    } catch (_) {} finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _showPuzzleSolutionModal() async {
    if (!_isPuzzleMode) return;
    setState(() => _isBusy = true);
    try {
      final res = await AppServices.instance.api.getPuzzleSolutionAsync(widget.puzzleKey!);
      if (!mounted) return;
      if (res == null || res['steps'] == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Chưa có lời giải chi tiết cho thế cờ này.')),
        );
        return;
      }

      final steps = res['steps'] as List<dynamic>;

      showModalBottomSheet(
        context: context,
        backgroundColor: AppColors.darkSurface,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
          side: BorderSide(color: AppColors.gold, width: 1.2),
        ),
        builder: (ctx) => Container(
          padding: const EdgeInsets.all(16),
          height: 420,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.auto_stories, color: AppColors.gold),
                  const SizedBox(width: 8),
                  Text(
                    'Đáp Án & Lời Giải Chi Tiết',
                    style: GoogleFonts.philosopher(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: AppColors.goldLight,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Expanded(
                child: ListView.builder(
                  itemCount: steps.length,
                  itemBuilder: (context, idx) {
                    final s = steps[idx] as Map<String, dynamic>;
                    final stepNum = s['step'] ?? (idx + 1);
                    final att = s['attackerMove'] as Map<String, dynamic>? ?? {};
                    final def = s['defenderMove'] as Map<String, dynamic>?;
                    final attDesc = att['description'] ?? '';
                    final defDesc = def?['description'] ?? 'Chiếu bí!';

                    return Container(
                      margin: const EdgeInsets.only(bottom: 8),
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: const Color(0xFF1E1A16),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: const Color(0xFF3E2723)),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Nước $stepNum:',
                            style: GoogleFonts.cinzel(
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: const Color(0xFFFFD54F),
                            ),
                          ),
                          const SizedBox(height: 4),
                          Row(
                            children: [
                              const Icon(Icons.arrow_right, color: Colors.redAccent, size: 16),
                              Expanded(
                                child: Text(
                                  'Đỏ: $attDesc',
                                  style: GoogleFonts.philosopher(
                                    color: Colors.redAccent,
                                    fontSize: 13,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ),
                            ],
                          ),
                          if (defDesc.isNotEmpty) ...[
                            const SizedBox(height: 2),
                            Row(
                              children: [
                                const Icon(Icons.arrow_right, color: Colors.lightBlueAccent, size: 16),
                                Expanded(
                                  child: Text(
                                    'Đen: $defDesc',
                                    style: GoogleFonts.philosopher(
                                      color: Colors.lightBlueAccent,
                                      fontSize: 13,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ],
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      );
    } catch (_) {} finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  void _showPuzzleVictoryModal(String message) {
    HapticFeedback.heavyImpact();
    AppServices.instance.sound.playWin();
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (dCtx) => AlertDialog(
        backgroundColor: AppColors.darkSurface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFFFD54F), width: 2),
        ),
        title: Column(
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: List.generate(
                3,
                (_) => const Padding(
                  padding: EdgeInsets.symmetric(horizontal: 2),
                  child: Icon(Icons.star, color: Color(0xFFFFD54F), size: 30),
                ),
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'CHIẾN THẮNG ẢI CỜ THẾ!',
              textAlign: TextAlign.center,
              style: GoogleFonts.maShanZheng(
                color: const Color(0xFFFFE082),
                fontSize: 22,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              message.isNotEmpty
                  ? message
                  : 'Xuất sắc! Các hạ đã phá vỡ thế cờ hiểm hóc này một cách trọn vẹn.',
              textAlign: TextAlign.center,
              style: GoogleFonts.philosopher(
                color: AppColors.paperLight,
                fontSize: 14,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
              decoration: BoxDecoration(
                color: const Color(0xFF2C241D),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: const Color(0xFFFFD54F), width: 1),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.military_tech, color: Color(0xFFFFD54F), size: 20),
                  const SizedBox(width: 6),
                  Text(
                    'Đã lưu tiến trình vượt ải',
                    style: GoogleFonts.philosopher(
                      color: const Color(0xFFFFECB3),
                      fontWeight: FontWeight.bold,
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              side: const BorderSide(color: Color(0xFFFFD54F), width: 1.2),
            ),
            onPressed: () {
              Navigator.pop(dCtx);
              Navigator.pop(context, true);
            },
            child: Text(
              'Trở Về Bản Đồ Vượt Ải',
              style: GoogleFonts.philosopher(
                color: Colors.white,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _applyGameUpdate(GameResponseDto dto) {
    _boardEpoch++;
    _game = dto;

    final resolvedColor =
        GameSnapshotRules.resolveColor(dto, _user, _gameMode, _seatHint);
    if (resolvedColor != _myPlayerColor) {
      _myPlayerColor = resolvedColor;
      _isFlipped = _myPlayerColor == 'Black';
    }

    _selectedRow = null;
    _selectedCol = null;
    _validMoves = [];

    if (dto.moves.isNotEmpty) {
      final last = dto.moves.last;
      _lastMoveFrom = (last.fromX, last.fromY);
      _lastMoveTo = (last.toX, last.toY);
    } else {
      _lastMoveFrom = null;
      _lastMoveTo = null;
    }

    if (dto.isCheck) {
      HapticFeedback.heavyImpact();
      AppServices.instance.sound.playCheck();
    }
    _statusMessage = _isGameOver ? _gameOverTitle : _currentTurnText;
    if (_isGameOver) {
      final winner = _game?.status == 'RedWins' ? 'Red' : 'Black';
      if (_myPlayerColor.isNotEmpty && winner == _myPlayerColor) {
        AppServices.instance.sound.playWin();
      }
    }
    if (mounted) setState(() {});
  }

  bool get _isGameOver =>
      _isPuzzleMode
          ? false
          : (_game?.status == 'RedWins' ||
              _game?.status == 'BlackWins' ||
              _game?.status == 'Draw');

  String get _gameOverTitle {
    if (_isPuzzleMode) return _isPuzzleSolved ? 'CHIẾN THẮNG ẢI' : 'CỜ THẾ';
    if (_game == null) return '';
    if (_game!.status == 'Draw') return 'VÁN CỜ HÒA';
    final winner = _game!.status == 'RedWins' ? 'Red' : 'Black';
    if (_myPlayerColor.isEmpty) return 'VÁN CỜ KẾT THÚC';
    return winner == _myPlayerColor ? 'CHIẾN THẮNG' : 'HẸN VÁN TIẾP THEO';
  }

  String get _gameOverMessage {
    if (_game == null) return '';
    switch (_game!.endReason) {
      case 'Checkmate':
        return 'Ván cờ kết thúc do chiếu bí.';
      case 'NoLegalMoves':
        return 'Bên đến lượt không còn nước đi hợp lệ.';
      case 'Repetition':
        return 'Thế cờ lặp lại ba lần. Ván cờ hòa.';
      case 'Resigned':
        return 'Bạn đã xin thua ván cờ.';
      default:
        return 'Ván cờ đã kết thúc. Bạn có thể xem lại phân tích hoặc bắt đầu ván mới.';
    }
  }

  bool get _isMyTurn =>
      _isPuzzleMode ? !_isPuzzleSolved : GameSnapshotRules.isMyTurn(_game, _myPlayerColor);

  String get _opponentName => _isPuzzleMode
      ? (_puzzleDetail?.title ?? widget.puzzleTitle ?? 'Cờ Thế Trận')
      : (_gameMode == 'PvE'
          ? 'Trần Thị Phương Thảo (AI)'
          : (_myPlayerColor == 'Black'
                  ? _game?.redPlayerUsername
                  : _game?.blackPlayerUsername) ??
              'Trần Thị Phương Thảo');

  String get _currentTurnText {
    if (_isPuzzleMode) {
      return _isPuzzleSolved
          ? 'Đã hoàn thành ải cờ thế!'
          : (_puzzleDetail?.goal.isNotEmpty == true ? _puzzleDetail!.goal : 'Lượt của bạn');
    }
    if (_game == null) return 'Đang tải ván cờ…';
    if (_isGameOver) return _gameOverTitle;
    final isRed = _game!.currentTurn.toLowerCase() == 'red';
    final myTurnSuffix = _isMyTurn ? ' (Bạn đi)' : '';
    return 'Lượt ${isRed ? "Đỏ" : "Đen"}$myTurnSuffix';
  }

  int _calculateAdvantageScore() {
    final pieces = _game?.boardPieces;
    if (pieces == null || pieces.isEmpty) return 30; // Mặc định Đỏ ưu nhẹ khi khai cuộc
    int redScore = 0;
    int blackScore = 0;
    for (final p in pieces) {
      int val = 0;
      switch (p.type.toLowerCase()) {
        case 'king': val = 10000; break;
        case 'rook': val = 900; break;
        case 'cannon': val = 450; break;
        case 'horse': val = 400; break;
        case 'elephant': val = 200; break;
        case 'advisor': val = 200; break;
        case 'pawn': val = 100; break;
      }
      if (p.color.toLowerCase() == 'red') {
        redScore += val;
      } else {
        blackScore += val;
      }
    }
    final diff = (redScore - blackScore) ~/ 10;
    return diff.clamp(-500, 500);
  }

  Future<void> _onCellTapped(int row, int col) async {
    if (_isBusy || _isAiThinking || !_isMyTurn || _isGameOver) return;
    if (_isPuzzleMode && _isPuzzleSolved) return;

    if (_selectedRow == row && _selectedCol == col) {
      setState(() {
        _selectedRow = null;
        _selectedCol = null;
        _validMoves = [];
        _hintArrowFrom = null;
        _hintArrowTo = null;
      });
      return;
    }

    if (_selectedRow != null &&
        _selectedCol != null &&
        _validMoves.any((m) => m.row == row && m.col == col)) {
      if (_isPuzzleMode) {
        await _makePuzzleMove(_selectedRow!, _selectedCol!, row, col);
      } else {
        await _makeMove(_selectedRow!, _selectedCol!, row, col);
      }
      return;
    }

    final piece = _game?.boardPieces.cast<PieceDto?>().firstWhere(
          (p) => p?.row == row && p?.col == col,
          orElse: () => null,
        );
    if (piece == null || piece.color.toLowerCase() != _myPlayerColor.toLowerCase()) {
      setState(() {
        _selectedRow = null;
        _selectedCol = null;
        _validMoves = [];
      });
      return;
    }

    setState(() {
      _selectedRow = row;
      _selectedCol = col;
      _validMoves = [];
      _hintArrowFrom = null;
      _hintArrowTo = null;
    });
    HapticFeedback.selectionClick();

    if (_isPuzzleMode) {
      final moves = _puzzleLegalMoves
          .where((m) => m.fromRow == row && m.fromCol == col)
          .map((m) => ValidMoveDto(row: m.row, col: m.col, isCapture: m.isCapture))
          .toList();
      setState(() => _validMoves = moves);
      return;
    }

    final epoch = _boardEpoch;
    try {
      final moves = await AppServices.instance.api
          .getValidMovesAsync(_currentGameId, row, col);
      if (mounted &&
          epoch == _boardEpoch &&
          _selectedRow == row &&
          _selectedCol == col) {
        setState(() => _validMoves = moves);
      }
    } catch (_) {}
  }

  Future<void> _makeMove(int fromX, int fromY, int toX, int toY) async {
    final isCapture = _game?.boardPieces.any((p) => p.row == toX && p.col == toY) ?? false;
    if (isCapture) {
      HapticFeedback.mediumImpact();
      AppServices.instance.sound.playCapture();
    } else {
      HapticFeedback.lightImpact();
      AppServices.instance.sound.playMove();
    }
    setState(() => _isBusy = true);
    final epoch = _boardEpoch;
    try {
      final dto = await AppServices.instance.api.makeMoveAsync(
        _currentGameId,
        MakeMoveRequestDto(fromX: fromX, fromY: fromY, toX: toX, toY: toY),
      );
      if (mounted && epoch == _boardEpoch) {
        _applyGameUpdate(dto);
        if (_gameMode == 'PvE' &&
            dto.status == 'InProgress' &&
            dto.currentTurn.toLowerCase() == 'black') {
          _triggerAiMove();
        }
      }
    } catch (e) {
      if (mounted) setState(() => _statusMessage = e.toString());
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _triggerAiMove() async {
    if (_isAiThinking || _currentGameId <= 0) return;
    setState(() => _isAiThinking = true);
    final epoch = _boardEpoch;
    try {
      final dto = await AppServices.instance.api
          .computerMoveAsync(_currentGameId, _difficulty);
      if (mounted && epoch == _boardEpoch) {
        _applyGameUpdate(dto);
      }
    } catch (_) {} finally {
      if (mounted) setState(() => _isAiThinking = false);
    }
  }

  void _openReviewScreen() {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => GameReviewScreen(
          initialPieces: _game?.boardPieces ?? [],
          moveHistory: _game?.moves.map((m) => m.notation).toList() ?? [],
          playerRedName: _user?.username.isNotEmpty == true ? _user!.username : 'Quang Nguyễn Thế',
          playerBlackName: _opponentName,
        ),
      ),
    );
  }

  void _showQuickChatModal() {
    final phrases = [
      'Các hạ kỳ nghệ thật cao cường!',
      'Xin thứ lỗi, ta đi nhầm một nước!',
      'Đường cờ biến ảo khôn lường!',
      'Trận này đấu trí thật sảng khoái!',
      'Nhanh lên chút nào, ta đợi đã lâu!',
      'Cảm ơn đã chỉ giáo kỳ nghệ!',
    ];

    showModalBottomSheet(
      context: context,
      backgroundColor: AppColors.darkSurface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
        side: BorderSide(color: AppColors.gold, width: 1.2),
      ),
      builder: (ctx) => Container(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.chat_bubble_outline, color: AppColors.gold),
                const SizedBox(width: 8),
                Text(
                  'Tin Nhắn Nhanh',
                  style: GoogleFonts.philosopher(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.goldLight,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            ...phrases.map(
              (p) => ListTile(
                title: Text(
                  p,
                  style: GoogleFonts.philosopher(
                    color: AppColors.paperLight,
                    fontSize: 14,
                  ),
                ),
                leading: const Icon(Icons.send, color: AppColors.gold, size: 18),
                onTap: () {
                  Navigator.pop(ctx);
                  final senderName = _user?.username.isNotEmpty == true ? _user!.username : 'Bạn';
                  if (_currentGameId > 0) {
                    AppServices.instance.signalR.sendChatMessageAsync(_currentGameId, senderName, p);
                  }
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      backgroundColor: const Color(0xFF2E7D32),
                      content: Text('Đã gửi: "$p"'),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showMoveHistoryModal() {
    showModalBottomSheet(
      context: context,
      backgroundColor: AppColors.darkSurface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
        side: BorderSide(color: AppColors.gold, width: 1.2),
      ),
      builder: (ctx) => Container(
        padding: const EdgeInsets.all(16),
        height: 380,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.history_edu, color: AppColors.gold),
                const SizedBox(width: 8),
                Text(
                  'Biên Bản Nước Đi',
                  style: GoogleFonts.philosopher(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.goldLight,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Expanded(
              child: _game?.moves.isEmpty ?? true
                  ? Center(
                      child: Text(
                        'Chưa có nước đi nào được thực hiện',
                        style: GoogleFonts.philosopher(color: AppColors.inkMuted),
                      ),
                    )
                  : ListView.builder(
                      itemCount: _game!.moves.length,
                      itemBuilder: (context, idx) {
                        final m = _game!.moves[idx];
                        final isRed = m.color.toLowerCase() == 'red';
                        return Padding(
                          padding: const EdgeInsets.symmetric(vertical: 4),
                          child: Row(
                            children: [
                              SizedBox(
                                width: 36,
                                child: Text(
                                  '${idx + 1}.',
                                  style: GoogleFonts.cinzel(
                                    fontWeight: FontWeight.bold,
                                    color: AppColors.goldLight,
                                  ),
                                ),
                              ),
                              Container(
                                width: 10,
                                height: 10,
                                decoration: BoxDecoration(
                                  shape: BoxShape.circle,
                                  color: isRed
                                      ? const Color(0xFFC0392B)
                                      : const Color(0xFF2C3E50),
                                ),
                              ),
                              const SizedBox(width: 8),
                              Text(
                                m.notation,
                                style: GoogleFonts.philosopher(
                                  fontSize: 14,
                                  fontWeight: FontWeight.bold,
                                  color: isRed
                                      ? const Color(0xFFFFB74D)
                                      : Colors.white,
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                    ),
            ),
          ],
        ),
      ),
    );
  }

  void _copyFenToClipboard() {
    try {
      final pieces = _game?.boardPieces ?? [];
      final board = Board.fromPieceList(
        pieces,
        currentTurn: _game?.currentTurn.toLowerCase() == 'black' ? PieceColor.black : PieceColor.red,
      );
      final fen = board.toFen();
      Clipboard.setData(ClipboardData(text: fen));
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: const Color(0xFF2E7D32),
          content: Text('Đã sao chép chuỗi FEN bàn cờ vào bộ nhớ tạm:\n$fen', style: GoogleFonts.philosopher(fontSize: 12)),
        ),
      );
    } catch (_) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Không thể tạo FEN bàn cờ lúc này.')),
      );
    }
  }

  void _copyRoomCodeToClipboard() {
    final code = _game?.roomCode ?? '';
    if (code.isNotEmpty) {
      Clipboard.setData(ClipboardData(text: code));
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: const Color(0xFF2E7D32),
          content: Text('Đã sao chép mã phòng: $code', style: GoogleFonts.philosopher()),
        ),
      );
    }
  }

  String _buildFenFromCurrentBoard() {
    try {
      final pieces = _game?.boardPieces ?? [];
      final board = Board.fromPieceList(
        pieces,
        currentTurn: _game?.currentTurn.toLowerCase() == 'black'
            ? PieceColor.black
            : PieceColor.red,
      );
      return board.toFen();
    } catch (_) {
      return Board.startFen;
    }
  }

  void _showShareVictoryCardModal() {
    HapticFeedback.selectionClick();
    final pgn = _game?.moves.map((m) => m.notation).join(' ') ?? '';
    final fen = _buildFenFromCurrentBoard();
    final winner = _game?.status == 'RedWins'
        ? 'Đỏ thắng'
        : (_game?.status == 'BlackWins' ? 'Đen thắng' : 'Hòa ván');

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
            'THIỆP CHIẾN TÍCH KỲ ĐÀI',
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
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [Color(0xFF2C1E18), Color(0xFF140F0D)],
                ),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFFFD54F), width: 1.2),
              ),
              child: Column(
                children: [
                  const Text('👑', style: TextStyle(fontSize: 28)),
                  const SizedBox(height: 4),
                  Text(
                    'KỲ ĐẠO CHIẾN BẢO',
                    style: GoogleFonts.cinzel(
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFFFFD54F),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '${_user?.username ?? "Bạn"} vs $_opponentName',
                    style: GoogleFonts.philosopher(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                  Text(
                    'Kết quả: $winner • ${_game?.moves.length ?? 0} Nước đi',
                    style: GoogleFonts.philosopher(fontSize: 12, color: const Color(0xFF66BB6A)),
                  ),
                  const Divider(color: Color(0xFF5D4037)),
                  Text(
                    'Phần mềm Cờ Tướng Cổ Phong Hoàng Gia ChessCore',
                    style: GoogleFonts.philosopher(
                      fontSize: 10,
                      color: const Color(0xFF8D6E63),
                      fontStyle: FontStyle.italic,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 14),
            OutlinedButton.icon(
              icon: const Icon(Icons.copy, size: 16, color: Color(0xFFFFD54F)),
              label: Text('Sao Chép Biên Bản PGN', style: GoogleFonts.philosopher(color: const Color(0xFFFFD54F))),
              style: OutlinedButton.styleFrom(
                side: const BorderSide(color: Color(0xFF5D4037)),
                minimumSize: const Size(double.infinity, 38),
              ),
              onPressed: () {
                Clipboard.setData(ClipboardData(text: pgn.isNotEmpty ? pgn : fen));
                Navigator.pop(ctx);
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    backgroundColor: const Color(0xFF2E7D32),
                    content: Text('Đã sao chép kỳ phổ vào bộ nhớ tạm!', style: GoogleFonts.philosopher()),
                  ),
                );
              },
            ),
            const SizedBox(height: 8),
            OutlinedButton.icon(
              icon: const Icon(Icons.grid_4x4, size: 16, color: Color(0xFF81D4FA)),
              label: Text('Sao Chép Chuỗi FEN', style: GoogleFonts.philosopher(color: const Color(0xFF81D4FA))),
              style: OutlinedButton.styleFrom(
                side: const BorderSide(color: Color(0xFF5D4037)),
                minimumSize: const Size(double.infinity, 38),
              ),
              onPressed: () {
                _copyFenToClipboard();
                Navigator.pop(ctx);
              },
            ),
          ],
        ),
        actions: [
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx),
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              side: const BorderSide(color: Color(0xFFFFD54F)),
            ),
            child: Text('Đóng', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }

  void _showGameMenuModal() {
    showModalBottomSheet(
      context: context,
      backgroundColor: AppColors.darkSurface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
        side: BorderSide(color: AppColors.gold, width: 1.2),
      ),
      builder: (ctx) => Container(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.sync, color: AppColors.gold),
              title: Text('Đổi chiều bàn cờ', style: GoogleFonts.philosopher(color: AppColors.paperLight)),
              onTap: () {
                Navigator.pop(ctx);
                setState(() => _isFlipped = !_isFlipped);
              },
            ),
            if (_game?.roomCode.isNotEmpty == true)
              ListTile(
                leading: const Icon(Icons.copy, color: Color(0xFFFFD54F)),
                title: Text('Sao chép mã phòng (${_game!.roomCode})', style: GoogleFonts.philosopher(color: const Color(0xFFFFD54F))),
                onTap: () {
                  Navigator.pop(ctx);
                  _copyRoomCodeToClipboard();
                },
              ),
            ListTile(
              leading: const Icon(Icons.share, color: Color(0xFF81D4FA)),
              title: Text('Sao chép FEN bàn cờ hiện tại', style: GoogleFonts.philosopher(color: const Color(0xFF81D4FA))),
              onTap: () {
                Navigator.pop(ctx);
                _copyFenToClipboard();
              },
            ),
            ListTile(
              leading: const Icon(Icons.card_membership, color: Color(0xFFFFD54F)),
              title: Text('Thiệp chiến tích & Xuất PGN', style: GoogleFonts.philosopher(color: const Color(0xFFFFD54F))),
              onTap: () {
                Navigator.pop(ctx);
                _showShareVictoryCardModal();
              },
            ),
            if (_isPuzzleMode) ...[
              ListTile(
                leading: const Icon(Icons.restart_alt, color: Color(0xFF42A5F5)),
                title: Text('Đặt lại thế cờ ban đầu', style: GoogleFonts.philosopher(color: const Color(0xFF42A5F5))),
                onTap: () {
                  Navigator.pop(ctx);
                  _loadPuzzle();
                },
              ),
              ListTile(
                leading: const Icon(Icons.auto_stories, color: Color(0xFFFFD54F)),
                title: Text('Xem đáp án & lời giải', style: GoogleFonts.philosopher(color: const Color(0xFFFFD54F))),
                onTap: () {
                  Navigator.pop(ctx);
                  _showPuzzleSolutionModal();
                },
              ),
            ] else ...[
              ListTile(
                leading: const Icon(Icons.handshake, color: AppColors.gold),
                title: Text('Xin hòa ván cờ', style: GoogleFonts.philosopher(color: AppColors.paperLight)),
                onTap: () async {
                  Navigator.pop(ctx);
                  try {
                    await AppServices.instance.api.offerDrawAsync(_currentGameId);
                    if (mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          backgroundColor: const Color(0xFFD49B4B),
                          content: Text('Đã gửi đề nghị hòa tới đối thủ (chờ 15s)...', style: GoogleFonts.philosopher()),
                        ),
                      );
                    }
                  } catch (e) {
                    if (mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(backgroundColor: Colors.red, content: Text(e.toString())),
                      );
                    }
                  }
                },
              ),
              if (_gameMode == 'PvE')
                ListTile(
                  leading: const Icon(Icons.undo, color: Color(0xFF42A5F5)),
                  title: Text('Đi lại nước cờ (Undo)', style: GoogleFonts.philosopher(color: const Color(0xFF42A5F5))),
                  onTap: () async {
                    Navigator.pop(ctx);
                    try {
                      final res = await AppServices.instance.api.undoAsync(_currentGameId);
                      _applyGameUpdate(res);
                    } catch (e) {
                      if (mounted) {
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(backgroundColor: Colors.red, content: Text(e.toString())),
                        );
                      }
                    }
                  },
                ),
              ListTile(
                leading: const Icon(Icons.flag, color: Color(0xFFE53935)),
                title: Text('Xin thua (Đầu hàng)', style: GoogleFonts.philosopher(color: const Color(0xFFE53935))),
                onTap: () async {
                  Navigator.pop(ctx);
                  try {
                    await AppServices.instance.api.resignAsync(_currentGameId);
                    _reloadGame();
                  } catch (_) {}
                },
              ),
              ListTile(
                leading: const Icon(Icons.analytics, color: Color(0xFF00E676)),
                title: Text('Phân tích ván đấu (AI Review)', style: GoogleFonts.philosopher(color: const Color(0xFF00E676))),
                onTap: () {
                  Navigator.pop(ctx);
                  _openReviewScreen();
                },
              ),
            ],
            ListTile(
              leading: const Icon(Icons.exit_to_app, color: AppColors.paperLight),
              title: Text('Rời bàn cờ', style: GoogleFonts.philosopher(color: AppColors.paperLight)),
              onTap: () {
                Navigator.pop(ctx);
                Navigator.pop(context);
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF16120F),
      body: SafeArea(
        child: Stack(
          children: [
            Column(
              children: [
                // 1. HEADER ĐỐI THỦ (Matching media_1792897057216_bad991d2.png)
                _buildOpponentHeader(),

                // 2. THANH ƯU THẾ THẾ TRẬN (Advantage Bar)
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                  child: AdvantageBarWidget(
                    evaluationScore: _calculateAdvantageScore(),
                  ),
                ),

                // 3. BÀN CỜ GỖ 3D CHÍNH
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 4),
                    child: Stack(
                      alignment: Alignment.center,
                      children: [
                        ChessBoardWidget(
                          pieces: _game?.boardPieces ?? [],
                          selectedRow: _selectedRow,
                          selectedCol: _selectedCol,
                          validMoves: _validMoves,
                          lastMoveFrom: _lastMoveFrom,
                          lastMoveTo: _lastMoveTo,
                          isFlipped: _isFlipped,
                          reduceMotion: false,
                          gameId: _currentGameId,
                          moveCount: _game?.moves.length ?? 0,
                          riverWatermarkText: _isPuzzleMode
                              ? (_puzzleDetail?.category ?? 'Sát cục')
                              : widget.timeControl,
                          arrowFrom: _hintArrowFrom,
                          arrowTo: _hintArrowTo,
                          arrowColor: const Color(0xFFFFD54F),
                          onCellTapped: _onCellTapped,
                        ),

                        // Cảnh báo CHIẾU TƯỚNG
                        if (_game?.isCheck == true)
                          Positioned(
                            top: 14,
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
                              decoration: BoxDecoration(
                                gradient: const LinearGradient(
                                  colors: [Color(0xFFC0392B), Color(0xFF8E0000)],
                                ),
                                borderRadius: BorderRadius.circular(16),
                                border: Border.all(color: const Color(0xFFFFD54F), width: 1.2),
                                boxShadow: const [
                                  BoxShadow(
                                    color: Colors.black54,
                                    blurRadius: 6,
                                    offset: Offset(0, 2),
                                  ),
                                ],
                              ),
                              child: Text(
                                '⚔️ CHIẾU TƯỚNG',
                                style: GoogleFonts.philosopher(
                                  color: const Color(0xFFFFE082),
                                  fontWeight: FontWeight.bold,
                                  fontSize: 13,
                                  letterSpacing: 1.2,
                                ),
                              ),
                            ),
                          ),
                      ],
                    ),
                  ),
                ),

                // 4. FOOTER NGƯỜI CHƠI & BỘ NÚT ĐIỀU KHIỂN
                _buildPlayerFooter(),
              ],
            ),

            // OVERLAY KẾT THÚC VÁN CỜ
            if (_isGameOver) _buildGameOverOverlay(),
          ],
        ),
      ),
    );
  }

  // Header Đối Thủ
  Widget _buildOpponentHeader() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          // Avatar Đối Thủ
          Stack(
            alignment: Alignment.bottomCenter,
            children: [
              Container(
                width: 52,
                height: 52,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: LinearGradient(
                    colors: _isPuzzleMode
                        ? [const Color(0xFFD84315), const Color(0xFFBF360C)]
                        : [const Color(0xFF0288D1), const Color(0xFF01579B)],
                  ),
                  border: Border.all(
                    color: _isPuzzleMode ? const Color(0xFFFFD54F) : const Color(0xFF81D4FA),
                    width: 2,
                  ),
                  boxShadow: const [
                    BoxShadow(color: Colors.black45, blurRadius: 4, offset: Offset(0, 2)),
                  ],
                ),
                child: Center(
                  child: Icon(
                    _isPuzzleMode ? Icons.military_tech : Icons.pets,
                    color: Colors.white,
                    size: 26,
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    colors: [Color(0xFF8D6E63), Color(0xFF5D4037)],
                  ),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: const Color(0xFFFFD54F), width: 1),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      _isPuzzleMode ? (_puzzleDetail?.levelName ?? 'Cờ Thế') : 'Tập sự',
                      style: GoogleFonts.philosopher(
                        fontSize: 8,
                        fontWeight: FontWeight.bold,
                        color: const Color(0xFFFFECB3),
                      ),
                    ),
                    const SizedBox(width: 2),
                    const Icon(Icons.star, size: 8, color: Color(0xFFFFD54F)),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(width: 10),

          // Tên + Quân ăn được + Vàng
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        _opponentName,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.philosopher(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: const Color(0xFF81C784),
                        ),
                      ),
                    ),
                    // Quân cờ ăn được
                    const Icon(Icons.shield, size: 14, color: Color(0xFFE57373)),
                    const SizedBox(width: 2),
                    Text(
                      '0',
                      style: GoogleFonts.cinzel(
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                        color: Colors.white70,
                      ),
                    ),
                    const SizedBox(width: 10),
                    // Vàng 🪙
                    Container(
                      padding: const EdgeInsets.all(2),
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle,
                        color: Color(0xFFFFD54F),
                      ),
                      child: const Icon(Icons.monetization_on, size: 12, color: Color(0xFF795548)),
                    ),
                    const SizedBox(width: 3),
                    Text(
                      '2.000',
                      style: GoogleFonts.cinzel(
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                        color: const Color(0xFFFFD54F),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 3),
                // Đồng hồ đối thủ hoặc Mục tiêu cờ thế
                Row(
                  children: [
                    Icon(
                      _isPuzzleMode ? Icons.flag_circle : Icons.history_toggle_off,
                      size: 14,
                      color: const Color(0xFFFFD54F),
                    ),
                    const SizedBox(width: 4),
                    Text(
                      _isPuzzleMode
                          ? 'Mục tiêu: ${_puzzleDetail?.mateIn ?? 1} nước chiếu bí'
                          : _formatTime(_opponentRemainingSeconds),
                      style: GoogleFonts.cinzel(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        color: const Color(0xFFFFD54F),
                      ),
                    ),
                    if (_isAiThinking || _isBusy) ...[
                      const SizedBox(width: 8),
                      const SizedBox(
                        width: 12,
                        height: 12,
                        child: CircularProgressIndicator(strokeWidth: 2, color: Color(0xFFFFD54F)),
                      ),
                    ] else if (_connectionText.isNotEmpty) ...[
                      const SizedBox(width: 8),
                      Text(
                        '($_connectionText)',
                        style: GoogleFonts.philosopher(fontSize: 10, color: const Color(0xFF78909C)),
                      ),
                    ],
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // Footer Người Chơi
  Widget _buildPlayerFooter() {
    final playerName = _user?.username.isNotEmpty == true ? _user!.username : 'Quang Nguyễn Thế';

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          // Dòng thông báo 180 phút & Hàng nút kim loại
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                if (_statusMessage.isNotEmpty &&
                    _statusMessage != _currentTurnText &&
                    _statusMessage != _gameOverTitle)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 4),
                    child: Text(
                      _statusMessage,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.philosopher(
                        fontSize: 10,
                        color: const Color(0xFFFFB74D),
                      ),
                    ),
                  ),
                // Dòng sức khỏe 180 phút
                Padding(
                  padding: const EdgeInsets.only(bottom: 6),
                  child: Text(
                    '12+ Chơi quá 180 phút một ngày sẽ ảnh hưởng xấu đến sức khỏe.',
                    style: GoogleFonts.philosopher(
                      fontSize: 9,
                      color: const Color(0xFF78909C),
                    ),
                  ),
                ),

                // Hàng nút kim loại bo tròn
                Row(
                  children: _isPuzzleMode
                      ? [
                          _buildMetallicRoundBtn(Icons.lightbulb, _requestPuzzleHint),
                          const SizedBox(width: 8),
                          _buildMetallicRoundBtn(Icons.restart_alt, _loadPuzzle),
                          const SizedBox(width: 8),
                          _buildMetallicRoundBtn(Icons.auto_stories, _showPuzzleSolutionModal),
                          const SizedBox(width: 8),
                          _buildMetallicRoundBtn(Icons.more_horiz, _showGameMenuModal),
                        ]
                      : [
                          _buildMetallicRoundBtn(Icons.more_horiz, _showGameMenuModal),
                          const SizedBox(width: 8),
                          _buildMetallicRoundBtn(Icons.chat_bubble_outline, _showQuickChatModal),
                          const SizedBox(width: 8),
                          _buildMetallicRoundBtn(Icons.edit_note, _showMoveHistoryModal),
                          const SizedBox(width: 8),
                          _buildMetallicRoundBtn(Icons.analytics, _openReviewScreen),
                        ],
                ),
              ],
            ),
          ),

          // Avatar Kỳ Thủ với Vòng tròn đếm ngược (Circular Turn Timer)
          CircularTurnTimer(
            isMyTurn: _isMyTurn,
            totalSeconds: 600,
            remainingSeconds: _playerRemainingSeconds,
            playerName: playerName,
            rankTitle: 'Tập sự',
            avatarLetter: playerName.isNotEmpty ? playerName[0].toUpperCase() : 'Q',
          ),
        ],
      ),
    );
  }

  Widget _buildMetallicRoundBtn(IconData icon, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 38,
        height: 38,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          gradient: const LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFF37474F), Color(0xFF212121), Color(0xFF0F1416)],
          ),
          border: Border.all(
            color: const Color(0xFF78909C),
            width: 1.2,
          ),
          boxShadow: const [
            BoxShadow(
              color: Colors.black54,
              blurRadius: 4,
              offset: Offset(0, 2),
            ),
          ],
        ),
        child: Center(
          child: Icon(icon, color: const Color(0xFFECEFF1), size: 19),
        ),
      ),
    );
  }

  // Modal Overlay Kết Thúc Ván Cờ
  Widget _buildGameOverOverlay() {
    return Container(
      color: Colors.black87,
      alignment: Alignment.center,
      padding: const EdgeInsets.all(24),
      child: Container(
        padding: const EdgeInsets.all(22),
        decoration: BoxDecoration(
          color: AppColors.darkSurface,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppColors.gold, width: 2),
          boxShadow: const [
            BoxShadow(color: Colors.black87, blurRadius: 16, offset: Offset(0, 6)),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              _gameOverTitle,
              style: GoogleFonts.maShanZheng(
                fontSize: 28,
                fontWeight: FontWeight.bold,
                color: const Color(0xFFFFD54F),
              ),
            ),
            const SizedBox(height: 8),
            Text(
              _gameOverMessage,
              textAlign: TextAlign.center,
              style: GoogleFonts.philosopher(
                fontSize: 14,
                color: AppColors.paperLight,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 20),
            ElevatedButton.icon(
              icon: const Icon(Icons.analytics, color: Colors.white),
              label: Text(
                'Phân Tích Ván Đấu (AI Review)',
                style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.white),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF1E88E5),
                padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
              onPressed: _openReviewScreen,
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                Expanded(
                  child: ElevatedButton(
                    onPressed: () {
                      Navigator.pop(context);
                      Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (_) => GameScreen(
                            gameId: _currentGameId > 0 ? _currentGameId : -1,
                            difficulty: _difficulty,
                            mode: _gameMode,
                            seatHint: _seatHint,
                          ),
                        ),
                      );
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFFC0392B),
                      side: const BorderSide(color: Color(0xFFFFD54F), width: 1.2),
                    ),
                    child: Text('Ván Tiếp Theo', style: GoogleFonts.philosopher(color: Colors.white)),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: ElevatedButton(
                    onPressed: () => Navigator.pop(context),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: const Color(0xFF424242),
                    ),
                    child: Text('Trở Về Sảnh', style: GoogleFonts.philosopher(color: Colors.white)),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  String _formatTime(int sec) {
    final m = (sec ~/ 60).toString().padLeft(2, '0');
    final s = (sec % 60).toString().padLeft(2, '0');
    return '$m:$s';
  }
}
