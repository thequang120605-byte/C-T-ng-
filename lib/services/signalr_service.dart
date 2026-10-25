import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../models/game_models.dart';
import 'auth_service.dart';
import 'config_service.dart';

class SignalRService {
  final ConfigService _config;
  final AuthService _auth;

  HubConnection? _hub;
  int? _currentGameId;
  bool _isInLobby = false;

  final StreamController<GameResponseDto> _gameUpdatedCtrl =
      StreamController<GameResponseDto>.broadcast();
  final StreamController<ChatMessageDto> _chatMessageCtrl =
      StreamController<ChatMessageDto>.broadcast();
  final StreamController<Map<String, dynamic>> _drawOfferedCtrl =
      StreamController<Map<String, dynamic>>.broadcast();
  final StreamController<Map<String, dynamic>> _drawDeclinedCtrl =
      StreamController<Map<String, dynamic>>.broadcast();
  final StreamController<Map<String, dynamic>> _matchEndedDrawCtrl =
      StreamController<Map<String, dynamic>>.broadcast();
  final StreamController<Map<String, dynamic>> _rematchStartedCtrl =
      StreamController<Map<String, dynamic>>.broadcast();
  final StreamController<void> _lobbyChangedCtrl =
      StreamController<void>.broadcast();
  final StreamController<Map<String, dynamic>> _challengeReceivedCtrl =
      StreamController<Map<String, dynamic>>.broadcast();
  final StreamController<Map<String, dynamic>> _spectatorJoinedCtrl =
      StreamController<Map<String, dynamic>>.broadcast();
  final StreamController<void> _reconnectedCtrl =
      StreamController<void>.broadcast();
  final StreamController<String> _connectionStateCtrl =
      StreamController<String>.broadcast();

  Stream<GameResponseDto> get onGameUpdated => _gameUpdatedCtrl.stream;
  Stream<ChatMessageDto> get onChatMessage => _chatMessageCtrl.stream;
  Stream<Map<String, dynamic>> get onDrawOffered => _drawOfferedCtrl.stream;
  Stream<Map<String, dynamic>> get onDrawDeclined => _drawDeclinedCtrl.stream;
  Stream<Map<String, dynamic>> get onMatchEndedDraw => _matchEndedDrawCtrl.stream;
  Stream<Map<String, dynamic>> get onRematchStarted => _rematchStartedCtrl.stream;
  Stream<void> get onLobbyChanged => _lobbyChangedCtrl.stream;
  Stream<Map<String, dynamic>> get onChallengeReceived => _challengeReceivedCtrl.stream;
  Stream<Map<String, dynamic>> get onSpectatorJoined => _spectatorJoinedCtrl.stream;
  Stream<void> get onReconnected => _reconnectedCtrl.stream;
  Stream<String> get onConnectionStateChanged => _connectionStateCtrl.stream;

  int? get currentGameId => _currentGameId;
  bool get isConnected => _hub?.state == HubConnectionState.Connected;

  SignalRService(this._config, this._auth) {
    _config.addListener(() {
      disconnectAsync();
    });
  }

  Future<void> connectAsync() async {
    if (isConnected) return;
    if (_hub?.state == HubConnectionState.Reconnecting) {
      throw Exception('Đang nối lại phòng. Vui lòng chờ hoặc thử đồng bộ lại.');
    }
    if (_hub != null) {
      try {
        await _hub!.stop();
      } catch (_) {}
      _hub = null;
    }

    final generation = _config.generation;
    final hub = HubConnectionBuilder()
        .withUrl(
          '${_config.baseUrl}/gameHub',
          options: HttpConnectionOptions(
            accessTokenFactory: () async =>
                (await _auth.getAccessTokenAsync()) ?? '',
          ),
        )
        .withAutomaticReconnect(retryDelays: [0, 2000, 5000, 10000])
        .build();

    _hub = hub;

    // 1. Cập nhật thế cờ thời gian thực
    hub.on('GameUpdated', (arguments) {
      if (!identical(_hub, hub) || generation != _config.generation) return;
      if (arguments != null &&
          arguments.isNotEmpty &&
          arguments[0] is Map<String, dynamic>) {
        final dto =
            GameResponseDto.fromJson(arguments[0] as Map<String, dynamic>);
        _gameUpdatedCtrl.add(dto);
      }
    });

    // 2. Chat trong ván đấu thời gian thực
    hub.on('ReceiveChatMessage', (arguments) {
      if (!identical(_hub, hub) || generation != _config.generation) return;
      if (arguments != null &&
          arguments.isNotEmpty &&
          arguments[0] is Map<String, dynamic>) {
        final dto =
            ChatMessageDto.fromJson(arguments[0] as Map<String, dynamic>);
        _chatMessageCtrl.add(dto);
      }
    });

    // 3. Đề nghị hòa & Phản hồi hòa
    hub.on('DrawOffered', (arguments) {
      if (!identical(_hub, hub) || generation != _config.generation) return;
      if (arguments != null &&
          arguments.isNotEmpty &&
          arguments[0] is Map<String, dynamic>) {
        _drawOfferedCtrl.add(arguments[0] as Map<String, dynamic>);
      }
    });

    hub.on('DrawDeclined', (arguments) {
      if (!identical(_hub, hub) || generation != _config.generation) return;
      if (arguments != null &&
          arguments.isNotEmpty &&
          arguments[0] is Map<String, dynamic>) {
        _drawDeclinedCtrl.add(arguments[0] as Map<String, dynamic>);
      }
    });

    hub.on('MatchEnded_Draw', (arguments) {
      if (!identical(_hub, hub) || generation != _config.generation) return;
      if (arguments != null &&
          arguments.isNotEmpty &&
          arguments[0] is Map<String, dynamic>) {
        _matchEndedDrawCtrl.add(arguments[0] as Map<String, dynamic>);
      }
    });

    // 4. Đấu lại (Rematch)
    hub.on('RematchStarted', (arguments) {
      if (!identical(_hub, hub) || generation != _config.generation) return;
      if (arguments != null &&
          arguments.isNotEmpty &&
          arguments[0] is Map<String, dynamic>) {
        _rematchStartedCtrl.add(arguments[0] as Map<String, dynamic>);
      }
    });

    // 5. Đồng bộ Sảnh phòng trực tiếp (Lobby)
    hub.on('RoomCreated', (_) => _lobbyChangedCtrl.add(null));
    hub.on('RoomLocked', (_) => _lobbyChangedCtrl.add(null));
    hub.on('RoomCancelled', (_) => _lobbyChangedCtrl.add(null));

    // 6. Nhận lời khiêu chiến
    hub.on('ReceiveChallenge', (arguments) {
      if (!identical(_hub, hub) || generation != _config.generation) return;
      if (arguments != null &&
          arguments.isNotEmpty &&
          arguments[0] is Map<String, dynamic>) {
        _challengeReceivedCtrl.add(arguments[0] as Map<String, dynamic>);
      }
    });

    // 7. Khán giả tham gia
    hub.on('SpectatorJoined', (arguments) {
      if (!identical(_hub, hub) || generation != _config.generation) return;
      if (arguments != null &&
          arguments.isNotEmpty &&
          arguments[0] is Map<String, dynamic>) {
        _spectatorJoinedCtrl.add(arguments[0] as Map<String, dynamic>);
      }
    });

    hub.onreconnecting(({error}) {
      _connectionStateCtrl.add('Mất kết nối · đang nối lại');
    });

    hub.onreconnected(({connectionId}) async {
      if (!identical(_hub, hub) || generation != _config.generation) return;
      try {
        if (_currentGameId != null) {
          await hub.invoke('JoinGame', args: [_currentGameId.toString()]);
        }
        if (_isInLobby) {
          await hub.invoke('JoinLobby');
        }
        _connectionStateCtrl.add('Đã kết nối');
        _reconnectedCtrl.add(null);
      } catch (ex) {
        debugPrint(ex.toString());
        _connectionStateCtrl.add('Chưa vào lại phòng · bấm Đồng bộ');
      }
    });

    hub.onclose(({error}) {
      _connectionStateCtrl.add('Ngoại tuyến · bấm Đồng bộ');
    });

    try {
      await hub.start();
      _connectionStateCtrl.add('Đã kết nối');
    } catch (e) {
      try {
        await hub.stop();
      } catch (_) {}
      if (identical(_hub, hub)) _hub = null;
      rethrow;
    }
  }

  Future<void> joinGameAsync(int gameId) async {
    if (gameId <= 0) return;
    await connectAsync();
    if (_hub == null || !isConnected) {
      throw Exception('Chưa kết nối phòng.');
    }
    if (_currentGameId != null && _currentGameId != gameId) {
      await _hub!.invoke('LeaveGame', args: [_currentGameId.toString()]);
    }
    await _hub!.invoke('JoinGame', args: [gameId.toString()]);
    _currentGameId = gameId;
  }

  Future<void> leaveGameAsync(int gameId) async {
    if (_currentGameId != gameId) return;
    _currentGameId = null;
    if (_hub != null && isConnected) {
      try {
        await _hub!
            .invoke('LeaveGame', args: [gameId.toString()])
            .timeout(const Duration(seconds: 2));
      } catch (_) {}
    }
  }

  Future<void> sendChatMessageAsync(int gameId, String sender, String message) async {
    if (_hub == null || !isConnected) return;
    try {
      await _hub!.invoke('SendChatMessage', args: [gameId.toString(), sender, message]);
    } catch (e) {
      debugPrint('Error sending chat message: $e');
    }
  }

  Future<void> joinLobbyAsync() async {
    _isInLobby = true;
    await connectAsync();
    if (_hub != null && isConnected) {
      try {
        await _hub!.invoke('JoinLobby');
      } catch (_) {}
    }
  }

  Future<void> leaveLobbyAsync() async {
    _isInLobby = false;
    if (_hub != null && isConnected) {
      try {
        await _hub!.invoke('LeaveLobby');
      } catch (_) {}
    }
  }

  Future<void> joinSpectatorAsync(int gameId, String viewerName) async {
    await connectAsync();
    if (_hub != null && isConnected) {
      try {
        await _hub!.invoke('JoinSpectator', args: [gameId.toString(), viewerName]);
      } catch (_) {}
    }
  }

  Future<void> registerUserAsync(String playerId) async {
    if (playerId.isEmpty) return;
    await connectAsync();
    if (_hub != null && isConnected) {
      try {
        await _hub!.invoke('RegisterUser', args: [playerId]);
      } catch (_) {}
    }
  }

  Future<void> sendChallengeAsync(String targetPlayerId, String fromName, String fromId, String roomCode) async {
    if (_hub == null || !isConnected) return;
    try {
      await _hub!.invoke('SendChallenge', args: [targetPlayerId, fromName, fromId, roomCode]);
    } catch (_) {}
  }

  Future<void> disconnectAsync() async {
    final old = _hub;
    _hub = null;
    _currentGameId = null;
    _isInLobby = false;
    if (old != null) {
      try {
        await old.stop();
      } catch (_) {}
    }
    _connectionStateCtrl.add('Chưa kết nối');
  }
}
