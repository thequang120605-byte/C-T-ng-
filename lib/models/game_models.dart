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
  final DateTime createdAt;
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
    required this.createdAt,
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

  const ValidMoveDto({
    required this.row,
    required this.col,
    required this.isCapture,
  });

  factory ValidMoveDto.fromJson(Map<String, dynamic> json) {
    return ValidMoveDto(
      row: (json['row'] ?? json['Row'] ?? 0) as int,
      col: (json['col'] ?? json['Col'] ?? 0) as int,
      isCapture: (json['isCapture'] ?? json['IsCapture'] ?? false) as bool,
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
