import 'package:shared_preferences/shared_preferences.dart';
import 'api_service.dart';
import 'auth_service.dart';
import 'config_service.dart';
import 'hybrid_api_service.dart';
import 'offline_game_service.dart';
import 'signalr_service.dart';

class AppServices {
  late final ConfigService config;
  late final AuthService auth;
  late final ApiService remoteApi;
  late final OfflineGameService offlineGame;
  late final HybridApiService api;
  late final SignalRService signalR;

  static final AppServices instance = AppServices._();
  AppServices._();

  Future<void> init() async {
    final prefs = await SharedPreferences.getInstance();
    config = ConfigService(prefs);
    auth = AuthService(config, prefs);
    remoteApi = ApiService(config, auth);
    offlineGame = OfflineGameService();
    api = HybridApiService(remoteApi, offlineGame);
    signalR = SignalRService(config, auth);
  }
}
