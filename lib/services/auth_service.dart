import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../models/game_models.dart';
import 'api_service.dart';
import 'config_service.dart';

class AuthService extends ChangeNotifier {
  static const String _accessTokenKey = 'jwt_access_token';
  static const String _refreshTokenKey = 'jwt_refresh_token';
  static const String _userInfoKey = 'cached_user_info';
  static const String _originKey = 'auth_server_origin';

  final ConfigService _config;
  final SharedPreferences _prefs;
  AuthUserDto? _currentUser;
  int _sessionEpoch = 0;

  AuthService(this._config, this._prefs) {
    _config.addListener(() {
      _sessionEpoch++;
      _clearLocal();
      notifyListeners();
    });
    if (!_originMatches()) {
      _clearLocal();
    }
  }

  bool _originMatches() {
    final savedOrigin = _prefs.getString(_originKey) ?? '';
    return savedOrigin.toLowerCase() == _config.baseUrl.toLowerCase();
  }

  void _clearLocal() {
    _currentUser = null;
    _prefs.remove(_originKey);
    _prefs.remove(_userInfoKey);
    _prefs.remove(_accessTokenKey);
    _prefs.remove(_refreshTokenKey);
  }

  Future<String?> getAccessTokenAsync() async {
    final generation = _config.generation;
    if (!_originMatches()) return null;
    final token = _prefs.getString(_accessTokenKey);
    return generation == _config.generation && _originMatches() ? token : null;
  }

  Future<bool> isAuthenticatedAsync() async {
    final token = await getAccessTokenAsync();
    return token != null && token.trim().isNotEmpty;
  }

  Future<AuthUserDto?> getCurrentUserAsync() async {
    if (!_originMatches()) return null;
    if (_currentUser != null) return _currentUser;
    try {
      final raw = _prefs.getString(_userInfoKey);
      if (raw == null || raw == 'null') return null;
      _currentUser =
          AuthUserDto.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      _currentUser = null;
    }
    return _currentUser;
  }

  Future<AuthUserDto?> fetchUserProfileAsync() async {
    final generation = _config.generation;
    final epoch = _sessionEpoch;
    final origin = _config.baseUrl;
    try {
      final token = await getAccessTokenAsync();
      if (token == null || token.trim().isEmpty) return null;
      final res = await http.get(
        Uri.parse('$origin/api/auth/me'),
        headers: {'Authorization': 'Bearer $token'},
      ).timeout(const Duration(seconds: 15));
      if (res.statusCode < 200 || res.statusCode >= 300) {
        return await getCurrentUserAsync();
      }
      final map = jsonDecode(res.body) as Map<String, dynamic>;
      if (map['isAuthenticated'] == false) return null;
      final user = AuthUserDto.fromJson(map);
      if (generation != _config.generation || epoch != _sessionEpoch) {
        return null;
      }
      if (user.id <= 0) return await getCurrentUserAsync();
      _currentUser = user;
      await _prefs.setString(_userInfoKey, jsonEncode(user.toJson()));
      notifyListeners();
      return user;
    } catch (_) {
      return await getCurrentUserAsync();
    }
  }

  Future<(bool, String, AuthUserDto?)> loginAsync(
      String username, String password) {
    return _authenticateAsync('login', {
      'username': username.trim(),
      'password': password,
    });
  }

  Future<(bool, String, AuthUserDto?)> registerAsync(
      String username, String email, String password) {
    return _authenticateAsync('register', {
      'username': username.trim(),
      'email': email.trim(),
      'password': password,
    });
  }

  Future<(bool, String, AuthUserDto?)> _authenticateAsync(
      String action, Map<String, dynamic> payload) async {
    final generation = _config.generation;
    final epoch = _sessionEpoch;
    final origin = _config.baseUrl;
    try {
      final res = await http
          .post(
            Uri.parse('$origin/api/auth/$action'),
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode(payload),
          )
          .timeout(const Duration(seconds: 15));

      if (res.statusCode < 200 || res.statusCode >= 300) {
        return (false, ApiService.readError(res.statusCode, res.body), null);
      }
      final dto = AuthResponseDto.fromJson(
          jsonDecode(res.body) as Map<String, dynamic>);
      if (dto.token.trim().isEmpty || dto.user.id <= 0) {
        return (false, 'Máy chủ chưa trả phiên đăng nhập mobile hợp lệ.', null);
      }
      if (generation != _config.generation || epoch != _sessionEpoch) {
        return (false, 'Phiên kết nối đã thay đổi.', null);
      }
      await _prefs.setString(_accessTokenKey, dto.token);
      if (dto.refreshToken.trim().isNotEmpty) {
        await _prefs.setString(_refreshTokenKey, dto.refreshToken);
      } else {
        await _prefs.remove(_refreshTokenKey);
      }
      _currentUser = dto.user;
      await _prefs.setString(_userInfoKey, jsonEncode(dto.user.toJson()));
      await _prefs.setString(_originKey, origin);
      notifyListeners();
      return (true, 'Đăng nhập thành công.', dto.user);
    } catch (_) {
      return (
        false,
        'Không thể đăng nhập. Kiểm tra địa chỉ máy chủ và API mobile.',
        null
      );
    }
  }

  Future<bool> refreshTokenAsync([String? failedToken]) async {
    final generation = _config.generation;
    final epoch = _sessionEpoch;
    final origin = _config.baseUrl;
    try {
      if (!_originMatches()) return false;
      final current = await getAccessTokenAsync();
      if (failedToken != null && current != null && current != failedToken) {
        return true;
      }
      final refresh = _prefs.getString(_refreshTokenKey);
      if (refresh == null || refresh.trim().isEmpty) return false;
      final res = await http
          .post(
            Uri.parse('$origin/api/auth/refresh-token'),
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({'refreshToken': refresh}),
          )
          .timeout(const Duration(seconds: 15));
      if (res.statusCode < 200 || res.statusCode >= 300) return false;
      final map = jsonDecode(res.body) as Map<String, dynamic>;
      final token = (map['token'] ?? '').toString();
      if (token.trim().isEmpty) return false;
      if (generation != _config.generation || epoch != _sessionEpoch) {
        return false;
      }
      await _prefs.setString(_accessTokenKey, token);
      final rt = (map['refreshToken'] ?? '').toString();
      if (rt.trim().isNotEmpty) {
        await _prefs.setString(_refreshTokenKey, rt);
      }
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> logoutAsync() async {
    _sessionEpoch++;
    try {
      final origin = _config.baseUrl;
      final token = await getAccessTokenAsync();
      final refresh =
          _originMatches() ? _prefs.getString(_refreshTokenKey) : null;
      _clearLocal();
      notifyListeners();
      if (refresh != null && refresh.trim().isNotEmpty) {
        await http
            .post(
              Uri.parse('$origin/api/auth/logout'),
              headers: {
                'Content-Type': 'application/json',
                if (token != null) 'Authorization': 'Bearer $token',
              },
              body: jsonEncode({'refreshToken': refresh}),
            )
            .timeout(const Duration(seconds: 3));
      }
    } catch (_) {
      _clearLocal();
      notifyListeners();
    }
  }
}
