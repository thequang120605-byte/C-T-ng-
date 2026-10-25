import 'dart:convert';
import 'package:http/http.dart' as http;
import 'auth_service.dart';
import 'config_service.dart';

class ApiService {
  final ConfigService _config;
  final AuthService _auth;

  ApiService(this._config, this._auth);

  Future<bool> pingAsync() async {
    final origin = _config.baseUrl;
    try {
      final res = await http
          .get(Uri.parse('$origin/api/health'))
          .timeout(const Duration(seconds: 5));
      if (res.statusCode >= 200 && res.statusCode < 300) return true;
      if (res.statusCode != 404) return false;
      final fallback = await http
          .get(Uri.parse(origin))
          .timeout(const Duration(seconds: 5));
      return fallback.statusCode >= 200 && fallback.statusCode < 300;
    } catch (_) {
      return false;
    }
  }

  Future<dynamic> getJsonAsync(String endpoint) =>
      _sendAsync('GET', endpoint, null);

  Future<dynamic> postJsonAsync(String endpoint, [Object? data]) =>
      _sendAsync('POST', endpoint, data != null ? jsonEncode(data) : null);

  Future<dynamic> _sendAsync(
      String method, String endpoint, String? bodyJson) async {
    final generation = _config.generation;
    final origin = _config.baseUrl;
    final cleanEndpoint =
        endpoint.startsWith('/') ? endpoint.substring(1) : endpoint;
    final uri = Uri.parse('$origin/$cleanEndpoint');

    for (int attempt = 0; attempt < 2; attempt++) {
      final access = await _auth.getAccessTokenAsync();
      if (generation != _config.generation) {
        throw Exception('Máy chủ đã thay đổi.');
      }
      final headers = <String, String>{
        'Accept': 'application/json',
        if (bodyJson != null) 'Content-Type': 'application/json',
        if (access != null && access.trim().isNotEmpty)
          'Authorization': 'Bearer $access',
      };

      final http.Response res;
      try {
        if (method == 'POST') {
          res = await http
              .post(uri, headers: headers, body: bodyJson)
              .timeout(const Duration(seconds: 40));
        } else {
          res = await http
              .get(uri, headers: headers)
              .timeout(const Duration(seconds: 40));
        }
      } catch (e) {
        final err = e.toString().toLowerCase();
        if (err.contains('failed to fetch') ||
            err.contains('connection refused') ||
            err.contains('socketexception') ||
            err.contains('clientexception')) {
          throw Exception(
              'Chưa kết nối được máy chủ backend ($origin). Vui lòng khởi động backend server.');
        }
        rethrow;
      }

      if (res.statusCode >= 200 && res.statusCode < 300) {
        if (generation != _config.generation) {
          throw Exception('Máy chủ đã thay đổi.');
        }
        if (res.body.trim().isEmpty) return null;
        try {
          return jsonDecode(res.body);
        } catch (_) {
          throw Exception(
              'Dữ liệu máy chủ không khớp ứng dụng. Kiểm tra hợp đồng API mobile.');
        }
      }

      if (res.statusCode == 401 &&
          attempt == 0 &&
          await _auth.refreshTokenAsync(access)) {
        continue;
      }
      throw Exception(readError(res.statusCode, res.body));
    }
    throw Exception('Phiên đăng nhập đã hết hạn.');
  }

  static String readError(int status, String body) {
    if (status == 401) {
      return 'Phiên đăng nhập đã hết hạn hoặc máy chủ chưa hỗ trợ đăng nhập mobile.';
    }
    if (status == 403) return 'Bạn không có quyền thực hiện thao tác này.';
    if (status == 404) return 'Không tìm thấy dữ liệu hoặc API mobile trên máy chủ.';
    try {
      final map = jsonDecode(body);
      if (map is Map<String, dynamic>) {
        for (final key in ['message', 'error', 'title']) {
          final val = map[key]?.toString();
          if (val != null && val.trim().isNotEmpty) return val;
        }
      }
    } catch (_) {}
    return 'Máy chủ trả lỗi $status. Vui lòng thử lại.';
  }
}
