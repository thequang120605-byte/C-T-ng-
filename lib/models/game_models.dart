class PieceDto {
  final int row;
  final int col;
  final String type;
  final String color;

  const PieceDto({
    required this.row,
    required this.col,
    required this.type,
    required this.color,
  });

  factory PieceDto.fromJson(Map<String, dynamic> json) {
    return PieceDto(
      row: (json['row'] ?? json['Row'] ?? 0) as int,
      col: (json['col'] ?? json['Col'] ?? 0) as int,
      type: (json['type'] ?? json['Type'] ?? '').toString(),
      color: (json['color'] ?? json['Color'] ?? '').toString(),
    );
  }
}

class MoveHistoryDto {
  final int id;
  final int fromX;
  final int fromY;
  final int toX;
  final int toY;
  final String pieceType;
  final String pieceColor;
  final int moveOrder;
  final DateTime movedAt;

  String get notation => '$pieceType ($fromX,$fromY ➔ $toX,$toY)';
  String get color => pieceColor;

  const MoveHistoryDto({
    required this.id,
    required this.fromX,
    required this.fromY,
    required this.toX,
    required this.toY,
    required this.pieceType,
    required this.pieceColor,
    required this.moveOrder,
    required this.movedAt,
  });

  factory MoveHistoryDto.fromJson(Map<String, dynamic> json) {
    return MoveHistoryDto(
      id: (json['id'] ?? json['Id'] ?? 0) as int,
      fromX: (json['fromX'] ?? json['FromX'] ?? 0) as int,
      fromY: (json['fromY'] ?? json['FromY'] ?? 0) as int,
      toX: (json['toX'] ?? json['ToX'] ?? 0) as int,
      toY: (json['toY'] ?? json['ToY'] ?? 0) as int,
      pieceType: (json['pieceType'] ?? json['PieceType'] ?? '').toString(),
      pieceColor: (json['pieceColor'] ?? json['PieceColor'] ?? '').toString(),
      moveOrder: (json['moveOrder'] ?? json['MoveOrder'] ?? 0) as int,
      movedAt: DateTime.tryParse(
              (json['movedAt'] ?? json['MovedAt'] ?? '').toString()) ??
          DateTime.now().toUtc(),
    );
  }
}

class HeuristicEvaluationDto {
  final int h1;
  final int h2;
  final int h3;
  final int h4;
  int get total => h1 + h2 + h3 + h4;

  const HeuristicEvaluationDto({
    this.h1 = 0,
    this.h2 = 0,
    this.h3 = 0,
    this.h4 = 0,
  });

  factory HeuristicEvaluationDto.fromJson(Map<String, dynamic>? json) {
    if (json == null) return const HeuristicEvaluationDto();
    return HeuristicEvaluationDto(
      h1: (json['h1'] ?? json['H1'] ?? 0) as int,
      h2: (json['h2'] ?? json['H2'] ?? 0) as int,
      h3: (json['h3'] ?? json['H3'] ?? 0) as int,
      h4: (json['h4'] ?? json['H4'] ?? 0) as int,
    );
  }
}

class GameResponseDto {
  final int id;
  final String roomCode;
  final String status;
  final String currentTurn;
  final bool isCheck;
  final bool hasNoLegalMoves;
  final bool isCheckmate;
  final String endReason;
  final String? initialFen;
  final HeuristicEvaluationDto evaluation;
  final String gameMode;
  final bool isRated;
  final int redScore;
  final int blackScore;
  final int neuralScore;
  final DateTime? createdAt;
  final DateTime? lastMoveAt;
  final int remainingSeconds;
  final List<PieceDto> boardPieces;
  final List<MoveHistoryDto> moves;
  final int? revision;
  final int? redUserId;
  final int? blackUserId;
  final String? redPlayerId;
  final String? blackPlayerId;
  final String? redPlayerUsername;
  final String? blackPlayerUsername;

  const GameResponseDto({
    required this.id,
    this.roomCode = '',
    this.status = 'InProgress',
    this.currentTurn = 'Red',
    this.isCheck = false,
    this.hasNoLegalMoves = false,
    this.isCheckmate = false,
    this.endReason = '',
    this.initialFen,
    this.evaluation = const HeuristicEvaluationDto(),
    this.gameMode = 'PvE',
    this.isRated = false,
    this.redScore = 0,
    this.blackScore = 0,
    this.neuralScore = 0,
    this.createdAt,
    this.lastMoveAt,
    this.remainingSeconds = 180,
    this.boardPieces = const [],
    this.moves = const [],
    this.revision,
    this.redUserId,
    this.blackUserId,
    this.redPlayerId,
    this.blackPlayerId,
    this.redPlayerUsername,
    this.blackPlayerUsername,
  });

  factory GameResponseDto.fromJson(Map<String, dynamic> json) {
    final piecesRaw = (json['boardPieces'] ?? json['BoardPieces']) as List?;
    final movesRaw = (json['moves'] ?? json['Moves']) as List?;
    final evalRaw =
        (json['evaluation'] ?? json['Evaluation']) as Map<String, dynamic>?;

    return GameResponseDto(
      id: (json['id'] ?? json['Id'] ?? 0) as int,
      roomCode: (json['roomCode'] ?? json['RoomCode'] ?? '').toString(),
      status: (json['status'] ?? json['Status'] ?? 'InProgress').toString(),
      currentTurn:
          (json['currentTurn'] ?? json['CurrentTurn'] ?? 'Red').toString(),
      isCheck: (json['isCheck'] ?? json['IsCheck'] ?? false) as bool,
      hasNoLegalMoves:
          (json['hasNoLegalMoves'] ?? json['HasNoLegalMoves'] ?? false) as bool,
      isCheckmate:
          (json['isCheckmate'] ?? json['IsCheckmate'] ?? false) as bool,
      endReason: (json['endReason'] ?? json['EndReason'] ?? '').toString(),
      initialFen: (json['initialFen'] ?? json['InitialFen'])?.toString(),
      evaluation: HeuristicEvaluationDto.fromJson(evalRaw),
      gameMode: (json['gameMode'] ?? json['GameMode'] ?? 'PvE').toString(),
      isRated: (json['isRated'] ?? json['IsRated'] ?? false) as bool,
      redScore: (json['redScore'] ?? json['RedScore'] ?? 0) as int,
      blackScore: (json['blackScore'] ?? json['BlackScore'] ?? 0) as int,
      neuralScore: (json['neuralScore'] ?? json['NeuralScore'] ?? 0) as int,
      createdAt: DateTime.tryParse(
              (json['createdAt'] ?? json['CreatedAt'] ?? '').toString()) ??
          DateTime.now().toUtc(),
      lastMoveAt: DateTime.tryParse(
          (json['lastMoveAt'] ?? json['LastMoveAt'] ?? '').toString()),
      remainingSeconds:
          (json['remainingSeconds'] ?? json['RemainingSeconds'] ?? 180) as int,
      boardPieces: piecesRaw
              ?.map((e) => PieceDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
      moves: movesRaw
              ?.map((e) => MoveHistoryDto.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
      revision: (json['revision'] ?? json['Revision']) as int?,
      redUserId: (json['redUserId'] ?? json['RedUserId']) as int?,
      blackUserId: (json['blackUserId'] ?? json['BlackUserId']) as int?,
      redPlayerId: (json['redPlayerId'] ?? json['RedPlayerId'])?.toString(),
      blackPlayerId:
          (json['blackPlayerId'] ?? json['BlackPlayerId'])?.toString(),
      redPlayerUsername:
          (json['redPlayerUsername'] ?? json['RedPlayerUsername'])?.toString(),
      blackPlayerUsername:
          (json['blackPlayerUsername'] ?? json['BlackPlayerUsername'])
              ?.toString(),
    );
  }
}

class MakeMoveRequestDto {
  final int fromX;
  final int fromY;
  final int toX;
  final int toY;

  const MakeMoveRequestDto({
    required this.fromX,
    required this.fromY,
    required this.toX,
    required this.toY,
  });

  Map<String, dynamic> toJson() => {
        'fromX': fromX,
        'fromY': fromY,
        'toX': toX,
        'toY': toY,
      };
}

class ValidMoveDto {
  final int row;
  final int col;
  final bool isCapture;
  final int? fromRow;
  final int? fromCol;

  const ValidMoveDto({
    required this.row,
    required this.col,
    required this.isCapture,
    this.fromRow,
    this.fromCol,
  });

  factory ValidMoveDto.fromJson(Map<String, dynamic> json) {
    return ValidMoveDto(
      row: (json['row'] ?? json['Row'] ?? json['toRow'] ?? 0) as int,
      col: (json['col'] ?? json['Col'] ?? json['toCol'] ?? 0) as int,
      isCapture: (json['isCapture'] ?? json['IsCapture'] ?? false) as bool,
      fromRow: (json['fromRow'] ?? json['FromRow']) as int?,
      fromCol: (json['fromCol'] ?? json['FromCol']) as int?,
    );
  }
}

class AuthUserDto {
  final int id;
  final String username;
  final String email;
  final String playerId;
  final String role;
  final int elo;
  final int wins;
  final int losses;
  final int draws;
  final int puzzleScore;

  const AuthUserDto({
    this.id = 0,
    this.username = '',
    this.email = '',
    this.playerId = '',
    this.role = 'Player',
    this.elo = 1200,
    this.wins = 0,
    this.losses = 0,
    this.draws = 0,
    this.puzzleScore = 0,
  });

  factory AuthUserDto.fromJson(Map<String, dynamic> json) {
    return AuthUserDto(
      id: (json['id'] ?? json['Id'] ?? 0) as int,
      username: (json['username'] ?? json['Username'] ?? '').toString(),
      email: (json['email'] ?? json['Email'] ?? '').toString(),
      playerId: (json['playerId'] ?? json['PlayerId'] ?? '').toString(),
      role: (json['role'] ?? json['Role'] ?? 'Player').toString(),
      elo: (json['elo'] ?? json['Elo'] ?? 1200) as int,
      wins: (json['wins'] ?? json['Wins'] ?? 0) as int,
      losses: (json['losses'] ?? json['Losses'] ?? 0) as int,
      draws: (json['draws'] ?? json['Draws'] ?? 0) as int,
      puzzleScore: (json['puzzleScore'] ?? json['PuzzleScore'] ?? 0) as int,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'username': username,
        'email': email,
        'playerId': playerId,
        'role': role,
        'elo': elo,
        'wins': wins,
        'losses': losses,
        'draws': draws,
        'puzzleScore': puzzleScore,
      };
}

class AuthResponseDto {
  final bool success;
  final String token;
  final String refreshToken;
  final AuthUserDto user;

  const AuthResponseDto({
    this.success = false,
    this.token = '',
    this.refreshToken = '',
    this.user = const AuthUserDto(),
  });

  factory AuthResponseDto.fromJson(Map<String, dynamic> json) {
    final userRaw = (json['user'] ?? json['User']) as Map<String, dynamic>?;
    return AuthResponseDto(
      success: (json['success'] ?? json['Success'] ?? false) as bool,
      token: (json['token'] ?? json['Token'] ?? '').toString(),
      refreshToken:
          (json['refreshToken'] ?? json['RefreshToken'] ?? '').toString(),
      user: userRaw != null ? AuthUserDto.fromJson(userRaw) : const AuthUserDto(),
    );
  }
}

class RoomItemDto {
  final int id;
  final String roomCode;
  final DateTime createdAt;
  final String creatorUsername;
  final int creatorElo;
  final int playerCount;
  final String status;
  final bool isJoinable;

  const RoomItemDto({
    required this.id,
    required this.roomCode,
    required this.createdAt,
    required this.creatorUsername,
    required this.creatorElo,
    this.playerCount = 1,
    this.status = 'WaitingForPlayers',
    this.isJoinable = true,
  });

  factory RoomItemDto.fromJson(Map<String, dynamic> json) {
    return RoomItemDto(
      id: (json['id'] ?? json['Id'] ?? 0) as int,
      roomCode: (json['roomCode'] ?? json['RoomCode'] ?? '').toString(),
      createdAt: DateTime.tryParse(
              (json['createdAt'] ?? json['CreatedAt'] ?? '').toString()) ??
          DateTime.now().toUtc(),
      creatorUsername:
          (json['creatorUsername'] ?? json['CreatorUsername'] ?? '').toString(),
      creatorElo: (json['creatorElo'] ?? json['CreatorElo'] ?? 1200) as int,
      playerCount: (json['playerCount'] ?? json['PlayerCount'] ?? 1) as int,
      status: (json['status'] ?? json['Status'] ?? 'WaitingForPlayers').toString(),
      isJoinable: (json['isJoinable'] ?? json['IsJoinable'] ?? true) as bool,
    );
  }
}

class GameSnapshotRules {
  static bool _matches(String? a, String? b) =>
      a != null &&
      b != null &&
      a.trim().isNotEmpty &&
      b.trim().isNotEmpty &&
      a.toLowerCase() == b.toLowerCase();

  static String resolveColor(
    GameResponseDto game,
    AuthUserDto? user,
    String mode,
    String confirmedSeat,
  ) {
    if (mode == 'PvE') return 'Red';
    final hasIdentity = game.redUserId != null ||
        game.blackUserId != null ||
        (game.redPlayerId?.isNotEmpty ?? false) ||
        (game.blackPlayerId?.isNotEmpty ?? false) ||
        (game.redPlayerUsername?.isNotEmpty ?? false) ||
        (game.blackPlayerUsername?.isNotEmpty ?? false);

    if (user != null && user.id > 0) {
      final id = user.id.toString();
      if (game.redUserId == user.id ||
          _matches(game.redPlayerId, user.playerId) ||
          _matches(game.redPlayerId, id) ||
          _matches(game.redPlayerUsername, user.username)) {
        return 'Red';
      }
      if (game.blackUserId == user.id ||
          _matches(game.blackPlayerId, user.playerId) ||
          _matches(game.blackPlayerId, id) ||
          _matches(game.blackPlayerUsername, user.username)) {
        return 'Black';
      }
    }

    if (!hasIdentity && (confirmedSeat == 'Red' || confirmedSeat == 'Black')) {
      return confirmedSeat;
    }
    return '';
  }

  static bool isMyTurn(GameResponseDto? game, String color) =>
      game != null &&
      game.status == 'InProgress' &&
      color.trim().isNotEmpty &&
      _matches(game.currentTurn, color);
}

class JoinResponseParser {
  static GameResponseDto parse(Map<String, dynamic> root) {
    final payload = (root['game'] ?? root['Game'] ?? root) as Map<String, dynamic>;
    final dto = GameResponseDto.fromJson(payload);
    if (dto.id <= 0) {
      throw Exception('Máy chủ chưa trả mã ván hợp lệ.');
    }
    return dto;
  }
}

class ChatMessageDto {
  final int gameId;
  final String sender;
  final String message;
  final String sentAt;

  const ChatMessageDto({
    required this.gameId,
    required this.sender,
    required this.message,
    required this.sentAt,
  });

  factory ChatMessageDto.fromJson(Map<String, dynamic> json) {
    return ChatMessageDto(
      gameId: int.tryParse(json['gameId']?.toString() ?? '0') ?? 0,
      sender: json['sender']?.toString() ?? 'Người chơi',
      message: json['message']?.toString() ?? '',
      sentAt: json['sentAt']?.toString() ?? '',
    );
  }
}

class LiveMatchDto {
  final int id;
  final String roomCode;
  final String redPlayer;
  final String blackPlayer;
  final int redScore;
  final int blackScore;
  final int moveCount;

  const LiveMatchDto({
    required this.id,
    required this.roomCode,
    required this.redPlayer,
    required this.blackPlayer,
    this.redScore = 0,
    this.blackScore = 0,
    this.moveCount = 0,
  });

  factory LiveMatchDto.fromJson(Map<String, dynamic> json) {
    return LiveMatchDto(
      id: (json['id'] ?? json['Id'] ?? 0) as int,
      roomCode: (json['roomCode'] ?? json['RoomCode'] ?? '').toString(),
      redPlayer: (json['redPlayer'] ?? json['RedPlayer'] ?? 'Đỏ').toString(),
      blackPlayer: (json['blackPlayer'] ?? json['BlackPlayer'] ?? 'Đen').toString(),
      redScore: (json['redScore'] ?? json['RedScore'] ?? 0) as int,
      blackScore: (json['blackScore'] ?? json['BlackScore'] ?? 0) as int,
      moveCount: (json['moveCount'] ?? json['MoveCount'] ?? 0) as int,
    );
  }
}

class LeaderboardItemDto {
  final int rank;
  final int userId;
  final String username;
  final int elo;
  final int totalGames;
  final int winGames;
  final String winRate;
  final String title;
  final bool isCurrentUser;
  final int score;
  final int solvedCount;

  const LeaderboardItemDto({
    required this.rank,
    required this.userId,
    required this.username,
    required this.elo,
    this.totalGames = 0,
    this.winGames = 0,
    this.winRate = '0%',
    required this.title,
    this.isCurrentUser = false,
    this.score = 0,
    this.solvedCount = 0,
  });

  factory LeaderboardItemDto.fromJson(Map<String, dynamic> json) {
    return LeaderboardItemDto(
      rank: (json['rank'] ?? json['Rank'] ?? 0) as int,
      userId: (json['userId'] ?? json['UserId'] ?? 0) as int,
      username: (json['username'] ?? json['Username'] ?? 'Kỳ thủ').toString(),
      elo: (json['elo'] ?? json['Elo'] ?? 1200) as int,
      totalGames: (json['totalGames'] ?? json['TotalGames'] ?? 0) as int,
      winGames: (json['winGames'] ?? json['WinGames'] ?? 0) as int,
      winRate: (json['winRate'] ?? json['WinRate'] ?? '0%').toString(),
      title: (json['title'] ?? json['Title'] ?? 'Kỳ Thủ').toString(),
      isCurrentUser: (json['isCurrentUser'] ?? json['IsCurrentUser'] ?? false) as bool,
      score: (json['score'] ?? json['Score'] ?? 0) as int,
      solvedCount: (json['solvedCount'] ?? json['SolvedCount'] ?? 0) as int,
    );
  }
}

class PuzzleLevelDto {
  final int level;
  final String name;
  final String rule;
  final int total;
  final int required;
  final int completed;
  final List<String> keys;

  const PuzzleLevelDto({
    required this.level,
    required this.name,
    required this.rule,
    required this.total,
    required this.required,
    required this.completed,
    required this.keys,
  });

  factory PuzzleLevelDto.fromJson(Map<String, dynamic> json) {
    final rawKeys = json['keys'] as List<dynamic>? ?? [];
    return PuzzleLevelDto(
      level: (json['level'] ?? 1) as int,
      name: (json['name'] ?? '').toString(),
      rule: (json['rule'] ?? '').toString(),
      total: (json['total'] ?? 0) as int,
      required: (json['required'] ?? 0) as int,
      completed: (json['completed'] ?? 0) as int,
      keys: rawKeys.map((e) => e.toString()).toList(),
    );
  }
}

class PuzzleCatalogDto {
  final bool ready;
  final bool loggedIn;
  final List<PuzzleLevelDto> levels;
  final List<String> completedKeys;

  const PuzzleCatalogDto({
    required this.ready,
    required this.loggedIn,
    required this.levels,
    required this.completedKeys,
  });

  factory PuzzleCatalogDto.fromJson(Map<String, dynamic> json) {
    final rawLevels = json['levels'] as List<dynamic>? ?? [];
    final rawCompleted = json['completed'] as List<dynamic>? ?? [];
    return PuzzleCatalogDto(
      ready: (json['ready'] ?? false) as bool,
      loggedIn: (json['loggedIn'] ?? false) as bool,
      levels: rawLevels.map((e) => PuzzleLevelDto.fromJson(e as Map<String, dynamic>)).toList(),
      completedKeys: rawCompleted.map((e) => e.toString()).toList(),
    );
  }
}

class PuzzleDetailDto {
  final String key;
  final String title;
  final String category;
  final int level;
  final String levelName;
  final int number;
  final int mateIn;
  final String attacker;
  final String goal;
  final List<PieceDto> pieces;
  final List<ValidMoveDto> legalMoves;

  const PuzzleDetailDto({
    required this.key,
    required this.title,
    required this.category,
    required this.level,
    required this.levelName,
    required this.number,
    required this.mateIn,
    required this.attacker,
    required this.goal,
    required this.pieces,
    required this.legalMoves,
  });

  factory PuzzleDetailDto.fromJson(Map<String, dynamic> json) {
    final state = json['state'] as Map<String, dynamic>? ?? {};
    final rawPieces = state['pieces'] as List<dynamic>? ?? [];
    final rawLegal = state['legalMoves'] as List<dynamic>? ?? [];

    return PuzzleDetailDto(
      key: (json['key'] ?? '').toString(),
      title: (json['title'] ?? 'Cờ Thế').toString(),
      category: (json['category'] ?? 'Sát cục').toString(),
      level: (json['level'] ?? 1) as int,
      levelName: (json['levelName'] ?? '').toString(),
      number: (json['number'] ?? 1) as int,
      mateIn: (json['mateIn'] ?? 1) as int,
      attacker: (json['attacker'] ?? 'Red').toString(),
      goal: (json['goal'] ?? '').toString(),
      pieces: rawPieces.map((p) => PieceDto.fromJson(p as Map<String, dynamic>)).toList(),
      legalMoves: rawLegal.map((m) => ValidMoveDto.fromJson(m as Map<String, dynamic>)).toList(),
    );
  }
}

class MatchHistoryItemDto {
  final int gameId;
  final String roomCode;
  final String gameMode;
  final String opponentName;
  final String myColor;
  final String outcome; // 'Win', 'Loss', 'Draw', 'Playing'
  final String outcomeText; // 'Thắng', 'Thua', 'Hòa', 'Đang chơi'
  final String status;
  final String playedAt;
  final int totalMoves;

  const MatchHistoryItemDto({
    required this.gameId,
    required this.roomCode,
    required this.gameMode,
    required this.opponentName,
    required this.myColor,
    required this.outcome,
    required this.outcomeText,
    required this.status,
    required this.playedAt,
    required this.totalMoves,
  });

  factory MatchHistoryItemDto.fromJson(Map<String, dynamic> json) {
    return MatchHistoryItemDto(
      gameId: (json['gameId'] ?? 0) as int,
      roomCode: (json['roomCode'] ?? '').toString(),
      gameMode: (json['gameMode'] ?? 'PvE').toString(),
      opponentName: (json['opponentName'] ?? 'Đối thủ').toString(),
      myColor: (json['myColor'] ?? 'Red').toString(),
      outcome: (json['outcome'] ?? 'Draw').toString(),
      outcomeText: (json['outcomeText'] ?? 'Hòa').toString(),
      status: (json['status'] ?? '').toString(),
      playedAt: (json['playedAt'] ?? '').toString(),
      totalMoves: (json['totalMoves'] ?? 0) as int,
    );
  }
}

class MasterGameItemDto {
  final int id;
  final String title;
  final String redPlayer;
  final String blackPlayer;
  final String eventName;
  final String eventDate;
  final String result;
  final String openingName;
  final String initialFen;
  final String pgnContent;

  const MasterGameItemDto({
    required this.id,
    required this.title,
    required this.redPlayer,
    required this.blackPlayer,
    required this.eventName,
    required this.eventDate,
    required this.result,
    required this.openingName,
    required this.initialFen,
    required this.pgnContent,
  });

  factory MasterGameItemDto.fromJson(Map<String, dynamic> json) {
    return MasterGameItemDto(
      id: (json['id'] ?? 0) as int,
      title: json['title']?.toString() ?? '',
      redPlayer: json['redPlayer']?.toString() ?? '',
      blackPlayer: json['blackPlayer']?.toString() ?? '',
      eventName: json['eventName']?.toString() ?? '',
      eventDate: json['eventDate']?.toString() ?? '',
      result: json['result']?.toString() ?? '',
      openingName: json['openingName']?.toString() ?? '',
      initialFen: json['initialFen']?.toString() ?? '',
      pgnContent: json['pgnContent']?.toString() ?? '',
    );
  }
}

class CourseModuleDto {
  final int id;
  final String title;
  final String description;
  final int displayOrder;
  final int totalLessons;
  final List<CourseLessonDto> lessons;

  const CourseModuleDto({
    required this.id,
    required this.title,
    required this.description,
    required this.displayOrder,
    required this.totalLessons,
    required this.lessons,
  });

  factory CourseModuleDto.fromJson(Map<String, dynamic> json) {
    return CourseModuleDto(
      id: (json['id'] ?? 0) as int,
      title: json['title']?.toString() ?? '',
      description: json['description']?.toString() ?? '',
      displayOrder: (json['displayOrder'] ?? 0) as int,
      totalLessons: (json['totalLessons'] ?? 0) as int,
      lessons: (json['lessons'] as List<dynamic>? ?? [])
          .map((l) => CourseLessonDto.fromJson(l as Map<String, dynamic>))
          .toList(),
    );
  }
}

class CourseLessonDto {
  final int id;
  final int moduleId;
  final String title;
  final String description;
  final String lessonType;
  final String? boardPosition;
  final int displayOrder;

  const CourseLessonDto({
    required this.id,
    required this.moduleId,
    required this.title,
    required this.description,
    required this.lessonType,
    this.boardPosition,
    required this.displayOrder,
  });

  factory CourseLessonDto.fromJson(Map<String, dynamic> json) {
    return CourseLessonDto(
      id: (json['id'] ?? 0) as int,
      moduleId: (json['moduleId'] ?? 0) as int,
      title: json['title']?.toString() ?? '',
      description: json['description']?.toString() ?? '',
      lessonType: json['lessonType']?.toString() ?? 'theory',
      boardPosition: json['boardPosition']?.toString(),
      displayOrder: (json['displayOrder'] ?? 0) as int,
    );
  }
}

class CourseLessonDetailDto {
  final int id;
  final int moduleId;
  final String title;
  final String description;
  final String content;
  final String lessonType;
  final String? boardPosition;
  final List<CourseQuizDto> questions;

  const CourseLessonDetailDto({
    required this.id,
    required this.moduleId,
    required this.title,
    required this.description,
    required this.content,
    required this.lessonType,
    this.boardPosition,
    required this.questions,
  });

  factory CourseLessonDetailDto.fromJson(Map<String, dynamic> json) {
    return CourseLessonDetailDto(
      id: (json['id'] ?? 0) as int,
      moduleId: (json['moduleId'] ?? 0) as int,
      title: json['title']?.toString() ?? '',
      description: json['description']?.toString() ?? '',
      content: json['content']?.toString() ?? '',
      lessonType: json['lessonType']?.toString() ?? 'theory',
      boardPosition: json['boardPosition']?.toString(),
      questions: (json['questions'] as List<dynamic>? ?? [])
          .map((q) => CourseQuizDto.fromJson(q as Map<String, dynamic>))
          .toList(),
    );
  }
}

class CourseQuizDto {
  final int id;
  final String question;
  final String optionsJson;
  final int correctAnswer;
  final String explanation;

  const CourseQuizDto({
    required this.id,
    required this.question,
    required this.optionsJson,
    required this.correctAnswer,
    required this.explanation,
  });

  factory CourseQuizDto.fromJson(Map<String, dynamic> json) {
    return CourseQuizDto(
      id: (json['id'] ?? 0) as int,
      question: json['question']?.toString() ?? '',
      optionsJson: json['optionsJson']?.toString() ?? '[]',
      correctAnswer: (json['correctAnswer'] ?? 0) as int,
      explanation: json['explanation']?.toString() ?? '',
    );
  }
}
