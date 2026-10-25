import 'package:flutter/material.dart';
import '../services/app_services.dart';
import '../theme/app_theme.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  late final TextEditingController _urlCtrl;
  String _connectionStatus = 'Nhập địa chỉ máy chủ của bạn';
  Color _statusColor = const Color(0xFF75634E);
  bool _isBusy = false;
  late bool _reduceMotion;

  @override
  void initState() {
    super.initState();
    final cfg = AppServices.instance.config;
    _urlCtrl = TextEditingController(text: cfg.baseUrl);
    _reduceMotion = cfg.reduceMotion;
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
        _connectionStatus = 'Đã lưu. Khi đổi máy chủ, hãy đăng nhập lại.';
        _statusColor = AppColors.success;
      });
    } catch (e) {
      setState(() {
        _connectionStatus = e.toString().replaceFirst('FormatException: ', '');
        _statusColor = AppColors.danger;
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
            ? 'Máy chủ phản hồi. Bạn có thể quay lại sảnh.'
            : 'Chưa kết nối được. Kiểm tra địa chỉ và mạng Wi-Fi.';
        _statusColor = ok ? AppColors.success : AppColors.danger;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _connectionStatus = e.toString().replaceFirst('FormatException: ', '');
        _statusColor = AppColors.danger;
      });
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cfg = AppServices.instance.config;
    return Scaffold(
      backgroundColor: AppColors.paper,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 640),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text(
                    'Cấu Hình Địa Chỉ Server',
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: AppColors.ink,
                    ),
                  ),
                  const SizedBox(height: 6),
                  const Text(
                    'Thiết lập địa chỉ kết nối đến Backend ChessCore',
                    style: TextStyle(fontSize: 13, color: AppColors.muted),
                  ),
                  const SizedBox(height: 20),
                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: AppTheme.cardDecoration(radius: 16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Text(
                          'Địa chỉ máy chủ (Base URL):',
                          style: TextStyle(
                            color: Color(0xFF655541),
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const SizedBox(height: 10),
                        Container(
                          decoration: BoxDecoration(
                            color: AppColors.paper,
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(color: AppColors.line),
                          ),
                          padding: const EdgeInsets.symmetric(horizontal: 12),
                          child: TextField(
                            controller: _urlCtrl,
                            style: const TextStyle(
                              color: AppColors.ink,
                              fontSize: 15,
                            ),
                            decoration: const InputDecoration(
                              hintText: 'http://10.0.2.2:5000',
                              hintStyle: TextStyle(color: AppColors.muted),
                              border: InputBorder.none,
                            ),
                          ),
                        ),
                        const SizedBox(height: 16),
                        const Text(
                          'Chọn nhanh theo môi trường:',
                          style:
                              TextStyle(color: AppColors.muted, fontSize: 12),
                        ),
                        const SizedBox(height: 8),
                        Row(
                          children: [
                            Expanded(
                              child: SizedBox(
                                height: 48,
                                child: ElevatedButton(
                                  onPressed: () => setState(() =>
                                      _urlCtrl.text = cfg.defaultEmulatorUrl),
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: AppColors.line,
                                    foregroundColor: AppColors.ink,
                                    shape: RoundedRectangleBorder(
                                      borderRadius: BorderRadius.circular(6),
                                    ),
                                  ),
                                  child: const Text(
                                    'Máy Ảo (10.0.2.2)',
                                    style: TextStyle(fontSize: 12),
                                  ),
                                ),
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: SizedBox(
                                height: 48,
                                child: ElevatedButton(
                                  onPressed: () => setState(
                                      () => _urlCtrl.text = cfg.defaultLanUrl),
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: AppColors.line,
                                    foregroundColor: AppColors.ink,
                                    shape: RoundedRectangleBorder(
                                      borderRadius: BorderRadius.circular(6),
                                    ),
                                  ),
                                  child: const Text(
                                    'Máy Thật (LAN Wi-Fi)',
                                    style: TextStyle(fontSize: 12),
                                  ),
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        Row(
                          children: [
                            Expanded(
                              child: SizedBox(
                                height: 48,
                                child: ElevatedButton(
                                  onPressed: _save,
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: AppColors.teal,
                                    foregroundColor: Colors.white,
                                    shape: RoundedRectangleBorder(
                                      borderRadius: BorderRadius.circular(8),
                                    ),
                                  ),
                                  child: const Text(
                                    'Lưu Địa Chỉ',
                                    style:
                                        TextStyle(fontWeight: FontWeight.bold),
                                  ),
                                ),
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: SizedBox(
                                height: 48,
                                child: ElevatedButton(
                                  onPressed: _testConnection,
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor: AppColors.primary,
                                    foregroundColor: Colors.white,
                                    shape: RoundedRectangleBorder(
                                      borderRadius: BorderRadius.circular(8),
                                    ),
                                  ),
                                  child: const Text(
                                    'Kiểm Tra Kết Nối',
                                    style:
                                        TextStyle(fontWeight: FontWeight.bold),
                                  ),
                                ),
                              ),
                            ),
                          ],
                        ),
                        if (_isBusy) ...[
                          const SizedBox(height: 14),
                          const Center(
                            child: SizedBox(
                              height: 24,
                              width: 24,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            ),
                          ),
                        ],
                        const SizedBox(height: 16),
                        Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: AppColors.paper,
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Text(
                            _connectionStatus,
                            textAlign: TextAlign.center,
                            style: TextStyle(
                              color: _statusColor,
                              fontSize: 13,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),
                  Container(
                    padding: const EdgeInsets.all(18),
                    decoration: AppTheme.cardDecoration(),
                    child: Row(
                      children: [
                        const Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Giảm chuyển động',
                                style: TextStyle(fontWeight: FontWeight.bold),
                              ),
                              SizedBox(height: 4),
                              Text(
                                'Tắt hiệu ứng di chuyển quân cờ',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: AppColors.muted,
                                ),
                              ),
                            ],
                          ),
                        ),
                        Switch(
                          value: _reduceMotion,
                          activeThumbColor: AppColors.primary,
                          onChanged: (v) {
                            setState(() => _reduceMotion = v);
                            cfg.reduceMotion = v;
                          },
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'Điện thoại và máy chủ cần kết nối được với nhau. Đổi máy chủ sẽ kết thúc phiên đăng nhập hiện tại.',
                    style: TextStyle(color: AppColors.muted, fontSize: 13),
                  ),
                  const SizedBox(height: 16),
                  TextButton(
                    onPressed: () => Navigator.of(context).pop(),
                    child: const Text(
                      'Quay lại Trang Chủ',
                      style: TextStyle(color: AppColors.muted, fontSize: 13),
                    ),
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
