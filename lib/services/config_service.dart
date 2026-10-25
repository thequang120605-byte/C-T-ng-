import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

class ConfigService extends ChangeNotifier {
  static const String _baseUrlKey = 'app_base_url';
  static const String _reduceMotionKey = 'reduce_motion';
  static const String _soundEnabledKey = 'sound_enabled';

  final SharedPreferences _prefs;
  int _generation = 0;

  ConfigService(this._prefs);

  int get generation => _generation;
  String get defaultEmulatorUrl => 'http://10.0.2.2:5000';
  String get defaultLanUrl => 'http://192.168.1.10:5000';

  String get baseUrl {
    final defaultUrl = kIsWeb ? 'http://localhost:5000' : defaultLanUrl;
    return _prefs.getString(_baseUrlKey) ?? defaultUrl;
  }

  set baseUrl(String value) {
    final normalized = normalize(value);
    if (normalized.toLowerCase() == baseUrl.toLowerCase()) return;
    _prefs.setString(_baseUrlKey, normalized);
    _generation++;
    notifyListeners();
  }

  bool get reduceMotion => _prefs.getBool(_reduceMotionKey) ?? false;

  set reduceMotion(bool value) {
    _prefs.setBool(_reduceMotionKey, value);
    notifyListeners();
  }

  bool get soundEnabled => _prefs.getBool(_soundEnabledKey) ?? true;

  set soundEnabled(bool value) {
    _prefs.setBool(_soundEnabledKey, value);
    notifyListeners();
  }

  static String normalize(String? value) {
    final raw = value?.trim() ?? '';
    final uri = Uri.tryParse(raw);
    if (uri == null ||
        !uri.hasAbsolutePath ||
        (uri.scheme != 'http' && uri.scheme != 'https') ||
        uri.userInfo.isNotEmpty ||
        uri.hasQuery ||
        uri.hasFragment ||
        (uri.path.isNotEmpty && uri.path != '/')) {
      throw FormatException(
        'Nhập địa chỉ máy chủ dạng https://ten-mien hoặc http://IP:cong, không kèm đường dẫn.',
      );
    }
    final portPart = uri.hasPort ? ':${uri.port}' : '';
    return '${uri.scheme}://${uri.host}$portPart';
  }
}
