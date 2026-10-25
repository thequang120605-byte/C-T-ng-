import '../models/game_models.dart';
import 'api_service.dart';
import 'offline_game_service.dart';

class HybridApiService {
  final ApiService _remote;
  final OfflineGameService _local;

  HybridApiService(this._remote, this._local);

  Future<bool> pingAsync() => _remote.pingAsync();

  Future<GameResponseDto> getGameAsync(int gameId) async {
    if (gameId < 0) {
      return _local.getAsync(gameId);
    }
    final json = await _remote.getJsonAsync('api/games/$gameId');
    return GameResponseDto.fromJson(json as Map<String, dynamic>);
  }

  Future<List<ValidMoveDto>> getValidMovesAsync(
      int gameId, int row, int col) async {
    if (gameId < 0) {
      return _local.validMovesAsync(gameId, row, col);
    }
    final list = await _remote
        .getJsonAsync('api/games/$gameId/valid-moves?row=$row&col=$col');
    if (list is! List) return [];
    return list
        .map((e) => ValidMoveDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<GameResponseDto> createOfflineGameAsync() {
    return _local.createAsync();
  }

  Future<GameResponseDto> createCoUpGameAsync({bool isPassAndPlay = false}) {
    return _local.createCoUpAsync(isPassAndPlay: isPassAndPlay);
  }

  Future<GameResponseDto> createPassAndPlayGameAsync() {
    return _local.createPassAndPlayAsync();
  }

  Future<({int fromRow, int fromCol, int toRow, int toCol, String explanation})?>
      getAiCoachHintAsync(int gameId) {
    return _local.getAiCoachHintAsync(gameId);
  }

  Future<GameResponseDto> createGameFromFenAsync(String fen) {
    return _local.createFromFenAsync(fen);
  }

  Future<GameResponseDto> createOnlineGameAsync() async {
    final json = await _remote.postJsonAsync('api/games/online');
    return GameResponseDto.fromJson(json as Map<String, dynamic>);
  }

  Future<GameResponseDto> createPveGameAsync(int gameId) async {
    if (gameId < 0) return _local.createAsync();
    final json = await _remote.postJsonAsync('api/games?mode=PvE');
    return GameResponseDto.fromJson(json as Map<String, dynamic>);
  }

  Future<GameResponseDto> joinOnlineRoomAsync(String code) async {
    final encoded = Uri.encodeComponent(code);
    final json = await _remote.postJsonAsync('api/games/$encoded/join');
    return JoinResponseParser.parse(json as Map<String, dynamic>);
  }

  Future<List<RoomItemDto>> getOnlineRoomsAsync() async {
    final list = await _remote.getJsonAsync('api/games/online/rooms');
    if (list is! List) return [];
    return list
        .map((e) => RoomItemDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<GameResponseDto> makeMoveAsync(
      int gameId, MakeMoveRequestDto move) async {
    if (gameId < 0) {
      return _local.moveAsync(gameId, move);
    }
    final json =
        await _remote.postJsonAsync('api/games/$gameId/moves', move.toJson());
    return GameResponseDto.fromJson(json as Map<String, dynamic>);
  }

  Future<GameResponseDto> computerMoveAsync(int gameId, int difficulty) async {
    if (gameId < 0) {
      return _local.computerAsync(gameId, difficulty);
    }
    final json = await _remote.postJsonAsync(
      'api/games/$gameId/computer-move?difficulty=$difficulty&computerColor=black',
    );
    return GameResponseDto.fromJson(json as Map<String, dynamic>);
  }

  Future<GameResponseDto> resignAsync(int gameId) async {
    if (gameId < 0) {
      return _local.resignAsync(gameId);
    }
    final json = await _remote.postJsonAsync('api/games/$gameId/resign');
    return GameResponseDto.fromJson(json as Map<String, dynamic>);
  }

  // 1. Xin hòa ván cờ (Draw Offer)
  Future<dynamic> offerDrawAsync(int gameId) async {
    if (gameId < 0) {
      return _local.offerDrawAsync(gameId);
    }
    return _remote.postJsonAsync('api/games/$gameId/draw/offer');
  }

  // 2. Phản hồi đề nghị hòa (Respond Draw)
  Future<dynamic> respondDrawAsync(int gameId, bool accepted) async {
    if (gameId < 0) return null;
    return _remote.postJsonAsync(
      'api/games/$gameId/draw/respond',
      {'accepted': accepted, 'isAccepted': accepted},
    );
  }

  // 3. Xử thắng rụng kim / Hết giờ (Claim Timeout)
  Future<dynamic> claimTimeoutAsync(int gameId) async {
    if (gameId < 0) return null;
    return _remote.postJsonAsync('api/games/$gameId/claim-timeout');
  }

  // 4. Đấu lại (Rematch)
  Future<GameResponseDto> rematchAsync(int gameId) async {
    if (gameId < 0) {
      return _local.createAsync();
    }
    final json = await _remote.postJsonAsync('api/games/$gameId/rematch');
    return GameResponseDto.fromJson(json as Map<String, dynamic>);
  }

  // 5. Đi lại nước cờ (Undo)
  Future<GameResponseDto> undoAsync(int gameId) async {
    if (gameId < 0) {
      return _local.undoAsync(gameId);
    }
    final json = await _remote.postJsonAsync('api/games/$gameId/undo');
    return GameResponseDto.fromJson(json as Map<String, dynamic>);
  }

  // 6. Hủy phòng chờ (Cancel Waiting Room)
  Future<dynamic> cancelWaitingRoomAsync(int gameId) async {
    if (gameId < 0) return null;
    return _remote.postJsonAsync('api/games/$gameId/cancel-wait');
  }

  // 7. Lấy danh sách trận đấu trực tiếp (Live Matches)
  Future<List<LiveMatchDto>> getLiveMatchesAsync() async {
    final list = await _remote.getJsonAsync('api/games/online/live');
    if (list is! List) return [];
    return list
        .map((e) => LiveMatchDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  // 8. Gợi ý nước đi từ AI server (Hint)
  Future<dynamic> getAiHintAsync(int gameId) async {
    if (gameId < 0) return null;
    return _remote.getJsonAsync('api/games/$gameId/hint?mode=full');
  }

  // 9. Lấy danh sách Cờ Thế từ Database (Puzzles)
  Future<dynamic> getPuzzlesAsync() async {
    return _remote.getJsonAsync('api/puzzles');
  }

  // 10. Bảng Xếp Hạng Kỳ Thủ thật từ Database (Leaderboard API)
  Future<List<LeaderboardItemDto>> getLeaderboardAsync({
    String mode = 'classic',
    int limit = 20,
  }) async {
    try {
      final json = await _remote.getJsonAsync('api/leaderboard?mode=$mode&limit=$limit');
      if (json is Map<String, dynamic>) {
        final list = json['leaderboard'] as List<dynamic>? ?? [];
        return list
            .map((e) => LeaderboardItemDto.fromJson(e as Map<String, dynamic>))
            .toList();
      }
    } catch (_) {}
    return [];
  }

  // 11. Toàn bộ danh mục ải Cờ Thế Vượt Màn từ Backend (/api/puzzle-run/catalog)
  Future<PuzzleCatalogDto?> getPuzzleCatalogAsync() async {
    try {
      final json = await _remote.getJsonAsync('api/puzzle-run/catalog');
      if (json is Map<String, dynamic>) {
        return PuzzleCatalogDto.fromJson(json);
      }
    } catch (_) {}
    return null;
  }

  // 12. Chi tiết thế cờ từ Backend (/api/puzzle-run/puzzles/{key})
  Future<PuzzleDetailDto?> getPuzzleDetailAsync(String key) async {
    try {
      final json = await _remote.getJsonAsync('api/puzzle-run/puzzles/$key');
      if (json is Map<String, dynamic>) {
        return PuzzleDetailDto.fromJson(json);
      }
    } catch (_) {}
    return null;
  }

  // 13. Gửi nước đi giải cờ thế lên Backend (/api/puzzle-run/puzzles/{key}/move)
  Future<Map<String, dynamic>?> submitPuzzleMoveAsync(
    String key,
    List<Map<String, int>> moves,
  ) async {
    try {
      final json = await _remote.postJsonAsync(
        'api/puzzle-run/puzzles/$key/move',
        {'moves': moves},
      );
      if (json is Map<String, dynamic>) return json;
    } catch (_) {}
    return null;
  }

  // 14. Nhận gợi ý cờ thế từ Backend (/api/puzzle-run/puzzles/{key}/hint)
  Future<Map<String, dynamic>?> getPuzzleHintAsync(
    String key,
    List<Map<String, int>> moves,
  ) async {
    try {
      final json = await _remote.postJsonAsync(
        'api/puzzle-run/puzzles/$key/hint',
        {'moves': moves},
      );
      if (json is Map<String, dynamic>) return json;
    } catch (_) {}
    return null;
  }

  // 15. Lấy đáp án giải cờ thế từng bước từ Backend (/api/puzzle-run/puzzles/{key}/solution)
  Future<Map<String, dynamic>?> getPuzzleSolutionAsync(String key) async {
    try {
      final json = await _remote.getJsonAsync('api/puzzle-run/puzzles/$key/solution');
      if (json is Map<String, dynamic>) return json;
    } catch (_) {}
    return null;
  }

  // 16. Lấy lịch sử các ván cờ từ Backend (/api/games/history)
  Future<List<MatchHistoryItemDto>> getMatchHistoryAsync({int? userId, int limit = 20}) async {
    try {
      final query = userId != null ? 'userId=$userId&limit=$limit' : 'limit=$limit';
      final json = await _remote.getJsonAsync('api/games/history?$query');
      if (json is Map<String, dynamic>) {
        final list = json['matches'] as List<dynamic>? ?? [];
        return list.map((e) => MatchHistoryItemDto.fromJson(e as Map<String, dynamic>)).toList();
      }
    } catch (_) {}
    return [];
  }

  // 17. Lấy danh sách kỳ phổ danh thủ từ Backend (/api/master-games)
  Future<List<MasterGameItemDto>> getMasterGamesAsync() async {
    try {
      final json = await _remote.getJsonAsync('api/master-games');
      if (json is Map<String, dynamic>) {
        final list = json['games'] as List<dynamic>? ?? [];
        return list.map((e) => MasterGameItemDto.fromJson(e as Map<String, dynamic>)).toList();
      }
    } catch (_) {}
    return [];
  }

  // 18. Lấy chi tiết kỳ phổ danh thủ từ Backend (/api/master-games/{id})
  Future<MasterGameItemDto?> getMasterGameDetailAsync(int id) async {
    try {
      final json = await _remote.getJsonAsync('api/master-games/$id');
      if (json is Map<String, dynamic>) {
        return MasterGameItemDto.fromJson(json);
      }
    } catch (_) {}
    return null;
  }

  // 19. Lấy danh sách khóa học kỳ đạo từ Backend (/api/courses)
  Future<List<CourseModuleDto>> getCoursesAsync() async {
    try {
      final json = await _remote.getJsonAsync('api/courses');
      if (json is Map<String, dynamic>) {
        final list = json['modules'] as List<dynamic>? ?? [];
        return list.map((e) => CourseModuleDto.fromJson(e as Map<String, dynamic>)).toList();
      }
    } catch (_) {}
    return [];
  }

  // 20. Lấy chi tiết bài học và câu hỏi trắc nghiệm (/api/courses/lessons/{id})
  Future<CourseLessonDetailDto?> getCourseLessonDetailAsync(int id) async {
    try {
      final json = await _remote.getJsonAsync('api/courses/lessons/$id');
      if (json is Map<String, dynamic>) {
        return CourseLessonDetailDto.fromJson(json);
      }
    } catch (_) {}
    return null;
  }

  // 21. Lấy danh sách nhiệm vụ hàng ngày từ Backend (/api/quests/daily)
  Future<List<DailyQuestDto>> getDailyQuestsAsync() async {
    try {
      final json = await _remote.getJsonAsync('api/quests/daily');
      if (json is Map<String, dynamic>) {
        final list = json['quests'] as List<dynamic>? ?? [];
        return list
            .map((e) => DailyQuestDto.fromJson(e as Map<String, dynamic>))
            .toList();
      }
    } catch (_) {}
    return [];
  }

  // 22. Nhận thưởng nhiệm vụ hàng ngày (/api/quests/{id}/claim)
  Future<bool> claimQuestAsync(String questId) async {
    try {
      final json = await _remote.postJsonAsync('api/quests/$questId/claim');
      if (json is Map<String, dynamic>) {
        return json['success'] as bool? ?? false;
      }
    } catch (_) {}
    return false;
  }

  // 23. Lấy danh hiệu & thành tựu người chơi từ Backend (/api/achievements)
  Future<List<PlayerAchievementDto>> getAchievementsAsync() async {
    try {
      final json = await _remote.getJsonAsync('api/achievements');
      if (json is Map<String, dynamic>) {
        final list = json['achievements'] as List<dynamic>? ?? [];
        return list
            .map((e) => PlayerAchievementDto.fromJson(e as Map<String, dynamic>))
            .toList();
      }
    } catch (_) {}
    return [];
  }

  // 24. Lấy phân tích ván cờ bằng AI Minimax từ Backend (/api/games/{id}/review)
  Future<GameReviewDto?> getGameReviewAsync(int gameId) async {
    try {
      final json = await _remote.getJsonAsync('api/games/$gameId/review');
      if (json is Map<String, dynamic>) {
        return GameReviewDto.fromJson(json);
      }
    } catch (_) {}
    return null;
  }

  // 25. Phân tích ván cờ tùy biến từ các nước đi qua AI Minimax (/api/games/review)
  Future<GameReviewDto?> analyzeMovesReviewAsync(
    List<String> moves, {
    String? redName,
    String? blackName,
    int? gameId,
  }) async {
    try {
      final json = await _remote.postJsonAsync(
        'api/games/review',
        {
          'gameId': gameId ?? 1,
          'redPlayerName': redName,
          'blackPlayerName': blackName,
          'moves': moves,
        },
      );
      if (json is Map<String, dynamic>) {
        return GameReviewDto.fromJson(json);
      }
    } catch (_) {}
    return null;
  }
}
