import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../services/app_services.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  late final TextEditingController _urlCtrl;
  String _connectionStatus = 'Nhập địa chỉ máy chủ ChessCore của bạn';
  Color _statusColor = const Color(0xFFBCAAA4);
  bool _isBusy = false;
  late bool _reduceMotion;
  late bool _soundEnabled;

  @override
  void initState() {
    super.initState();
    final cfg = AppServices.instance.config;
    _urlCtrl = TextEditingController(text: cfg.baseUrl);
    _reduceMotion = cfg.reduceMotion;
    _soundEnabled = cfg.soundEnabled;
  }

  @override
  void dispose() {
    _urlCtrl.dispose();
    super.dispose();
  }

  void _save() {
    final cfg = AppServices.instance.config;
    try {
      cfg.baseUrl = _urlCtrl.text;
      _urlCtrl.text = cfg.baseUrl;
      setState(() {
        _connectionStatus = 'Đã lưu thành công địa chỉ máy chủ.';
        _statusColor = const Color(0xFF66BB6A);
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: const Color(0xFF2E7D32),
          content: Text('Đã lưu cấu hình máy chủ!', style: GoogleFonts.philosopher()),
        ),
      );
    } catch (e) {
      setState(() {
        _connectionStatus = e.toString().replaceFirst('FormatException: ', '');
        _statusColor = const Color(0xFFEF5350);
      });
    }
  }

  Future<void> _testConnection() async {
    if (_isBusy) return;
    setState(() => _isBusy = true);
    try {
      final cfg = AppServices.instance.config;
      cfg.baseUrl = _urlCtrl.text;
      _urlCtrl.text = cfg.baseUrl;
      final ok = await AppServices.instance.api.pingAsync();
      if (!mounted) return;
      setState(() {
        _connectionStatus = ok
            ? 'Kết nối thành công! Máy chủ ChessCore đang hoạt động hoàn hảo.'
            : 'Chưa kết nối được. Vui lòng kiểm tra địa chỉ IP và mạng Wi-Fi.';
        _statusColor = ok ? const Color(0xFF66BB6A) : const Color(0xFFEF5350);
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _connectionStatus = e.toString().replaceFirst('FormatException: ', '');
        _statusColor = const Color(0xFFEF5350);
      });
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cfg = AppServices.instance.config;
    return Scaffold(
      backgroundColor: const Color(0xFF140F0D),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 640),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Top Header
                  Row(
                    children: [
                      GestureDetector(
                        onTap: () => Navigator.pop(context),
                        child: Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: const Color(0xFF2C1E18),
                            border: Border.all(color: const Color(0xFF8D6E63), width: 1),
                          ),
                          child: const Icon(Icons.arrow_back_ios_new, size: 16, color: Color(0xFFFFD54F)),
                        ),
                      ),
                      const SizedBox(width: 14),
                      Text(
                        'CÀI ĐẶT HỆ THỐNG',
                        style: GoogleFonts.cinzel(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: const Color(0xFFFFE082),
                          letterSpacing: 1.5,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 18),

                  // Card Cấu Hình Server
                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                        colors: [Color(0xFF2C1E18), Color(0xFF1E1511), Color(0xFF140F0D)],
                      ),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: const Color(0xFFFFD54F), width: 1.5),
                      boxShadow: const [
                        BoxShadow(color: Colors.black54, blurRadius: 10, offset: Offset(0, 4)),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Row(
                          children: [
                            const Icon(Icons.dns, color: Color(0xFFFFD54F), size: 20),
                            const SizedBox(width: 8),
                            Text(
                              'Địa Chỉ Máy Chủ (Base URL)',
                              style: GoogleFonts.philosopher(
                                color: const Color(0xFFFFECB3),
                                fontSize: 15,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),

                        // Text Field URL
                        Container(
                          decoration: BoxDecoration(
                            color: const Color(0xFF140E0B),
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(color: const Color(0xFF4E342E), width: 1.2),
                          ),
                          padding: const EdgeInsets.symmetric(horizontal: 12),
                          child: TextField(
                            controller: _urlCtrl,
                            style: GoogleFonts.philosopher(color: const Color(0xFFFFECB3), fontSize: 14),
                            decoration: InputDecoration(
                              hintText: 'http://localhost:5000',
                              hintStyle: GoogleFonts.philosopher(color: const Color(0xFF6D4C41), fontSize: 13),
                              border: InputBorder.none,
                            ),
                          ),
                        ),
                        const SizedBox(height: 14),

                        Text(
                          'Chọn nhanh theo môi trường kết nối:',
                          style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4), fontSize: 12),
                        ),
                        const SizedBox(height: 8),

                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton(
                                onPressed: () => setState(() => _urlCtrl.text = cfg.defaultEmulatorUrl),
                                style: OutlinedButton.styleFrom(
                                  side: const BorderSide(color: Color(0xFF5D4037)),
                                  padding: const EdgeInsets.symmetric(vertical: 10),
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                ),
                                child: Text(
                                  'Máy Ảo (10.0.2.2)',
                                  style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFFFFD54F)),
                                ),
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: OutlinedButton(
                                onPressed: () => setState(() => _urlCtrl.text = cfg.defaultLanUrl),
                                style: OutlinedButton.styleFrom(
                                  side: const BorderSide(color: Color(0xFF5D4037)),
                                  padding: const EdgeInsets.symmetric(vertical: 10),
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                                ),
                                child: Text(
                                  'Máy Thật (LAN Wi-Fi)',
                                  style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFFFFD54F)),
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),

                        // Nút Lưu & Kiểm tra
                        Row(
                          children: [
                            Expanded(
                              child: ElevatedButton(
                                onPressed: _save,
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: const Color(0xFF00695C),
                                  side: const BorderSide(color: Color(0xFF80CBC4), width: 1),
                                  padding: const EdgeInsets.symmetric(vertical: 12),
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                                ),
                                child: Text(
                                  'Lưu Địa Chỉ',
                                  style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.white),
                                ),
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: ElevatedButton(
                                onPressed: _testConnection,
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: const Color(0xFFC0392B),
                                  side: const BorderSide(color: Color(0xFFFFD54F), width: 1),
                                  padding: const EdgeInsets.symmetric(vertical: 12),
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                                ),
                                child: _isBusy
                                    ? const SizedBox(
                                        width: 18,
                                        height: 18,
                                        child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                                      )
                                    : Text(
                                        'Kiểm Tra Kết Nối',
                                        style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.white),
                                      ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),

                        // Thông báo trạng thái kết nối
                        Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: const Color(0xFF140E0B),
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(color: _statusColor.withValues(alpha: 0.4)),
                          ),
                          child: Text(
                            _connectionStatus,
                            textAlign: TextAlign.center,
                            style: GoogleFonts.philosopher(
                              color: _statusColor,
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Card Tùy Chọn Trải Nghiệm Chơi
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: const Color(0xFF1E1714),
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: const Color(0xFF3E2723), width: 1),
                    ),
                    child: Column(
                      children: [
                        // Tùy chọn Âm thanh hiệu ứng
                        Row(
                          children: [
                            const Icon(Icons.volume_up, color: Color(0xFFFFD54F), size: 20),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    'Âm thanh hiệu ứng (Sound Effects)',
                                    style: GoogleFonts.philosopher(
                                      fontWeight: FontWeight.bold,
                                      color: Colors.white,
                                      fontSize: 14,
                                    ),
                                  ),
                                  Text(
                                    'Tiếng gõ quân cờ, ăn quân và chuông báo chiếu tướng',
                                    style: GoogleFonts.philosopher(
                                      fontSize: 11,
                                      color: const Color(0xFF8D6E63),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            Switch(
                              value: _soundEnabled,
                              activeThumbColor: const Color(0xFFFFD54F),
                              activeTrackColor: const Color(0xFFC0392B),
                              onChanged: (v) {
                                setState(() => _soundEnabled = v);
                                cfg.soundEnabled = v;
                                if (v) {
                                  AppServices.instance.sound.playMove();
                                }
                              },
                            ),
                          ],
                        ),
                        const Divider(color: Color(0xFF3E2723), height: 24),

                        // Tùy chọn Giảm chuyển động
                        Row(
                          children: [
                            const Icon(Icons.motion_photos_off, color: Color(0xFFFFD54F), size: 20),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    'Giảm chuyển động (Reduce Motion)',
                                    style: GoogleFonts.philosopher(
                                      fontWeight: FontWeight.bold,
                                      color: Colors.white,
                                      fontSize: 14,
                                    ),
                                  ),
                                  Text(
                                    'Tắt hiệu ứng lướt quân cờ để tăng tốc độ trên máy cấu hình yếu',
                                    style: GoogleFonts.philosopher(
                                      fontSize: 11,
                                      color: const Color(0xFF8D6E63),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            Switch(
                              value: _reduceMotion,
                              activeThumbColor: const Color(0xFFFFD54F),
                              activeTrackColor: const Color(0xFFC0392B),
                              onChanged: (v) {
                                setState(() => _reduceMotion = v);
                                cfg.reduceMotion = v;
                              },
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  Text(
                    '💡 Thiết bị di động và máy chủ cần cùng nằm trong một mạng nội bộ Wi-Fi hoặc sử dụng địa chỉ IP công khai.',
                    textAlign: TextAlign.center,
                    style: GoogleFonts.philosopher(color: const Color(0xFF8D6E63), fontSize: 11, fontStyle: FontStyle.italic),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
