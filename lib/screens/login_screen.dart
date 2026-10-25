import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../services/app_services.dart';
import 'register_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _userCtrl = TextEditingController();
  final _passCtrl = TextEditingController();
  bool _isBusy = false;
  bool _obscurePass = true;
  String _errorMessage = '';

  @override
  void dispose() {
    _userCtrl.dispose();
    _passCtrl.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    if (_isBusy) return;
    final username = _userCtrl.text.trim();
    final password = _passCtrl.text;
    if (username.isEmpty || password.isEmpty) {
      setState(() => _errorMessage = 'Vui lòng nhập tài khoản và mật khẩu.');
      return;
    }
    setState(() {
      _isBusy = true;
      _errorMessage = '';
    });
    try {
      final (ok, msg, _) =
          await AppServices.instance.auth.loginAsync(username, password);
      if (!mounted) return;
      if (!ok) {
        setState(() => _errorMessage = msg);
        return;
      }
      _passCtrl.clear();
      Navigator.of(context).pop(true);
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF140F0D),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  // Logo Quân Tướng Hoàng Gia
                  Container(
                    width: 76,
                    height: 76,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      gradient: const LinearGradient(
                        colors: [Color(0xFFBA2213), Color(0xFF6B1107)],
                      ),
                      border: Border.all(color: const Color(0xFFFFD54F), width: 2.5),
                      boxShadow: const [
                        BoxShadow(
                          color: Colors.black54,
                          blurRadius: 10,
                          offset: Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Center(
                      child: Text(
                        '帥',
                        style: GoogleFonts.maShanZheng(
                          fontSize: 42,
                          fontWeight: FontWeight.bold,
                          color: const Color(0xFFFFE082),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),

                  Text(
                    'CỜ TƯỚNG HOÀNG GIA',
                    style: GoogleFonts.cinzel(
                      fontSize: 22,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFFFFE082),
                      letterSpacing: 2.0,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Đỉnh Cao Trí Tuệ • Tinh Hoa Kỳ Đạo',
                    style: GoogleFonts.philosopher(
                      fontSize: 13,
                      color: const Color(0xFFBCAAA4),
                      fontStyle: FontStyle.italic,
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Khung Card Đăng Nhập Cổ Phong
                  Container(
                    padding: const EdgeInsets.all(22),
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                        colors: [Color(0xFF2C1E18), Color(0xFF1E1511), Color(0xFF18110D)],
                      ),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: const Color(0xFFFFD54F), width: 1.5),
                      boxShadow: const [
                        BoxShadow(color: Colors.black87, blurRadius: 12, offset: Offset(0, 6)),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Center(
                          child: Text(
                            'ĐĂNG NHẬP KỲ THỦ',
                            style: GoogleFonts.cinzel(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: const Color(0xFFFFE082),
                              letterSpacing: 1.2,
                            ),
                          ),
                        ),
                        const SizedBox(height: 4),
                        Center(
                          child: Container(
                            width: 60,
                            height: 2,
                            color: const Color(0xFFFFD54F),
                          ),
                        ),
                        const SizedBox(height: 18),

                        if (_errorMessage.isNotEmpty) ...[
                          Container(
                            padding: const EdgeInsets.all(10),
                            decoration: BoxDecoration(
                              color: const Color(0xFFB71C1C).withValues(alpha: 0.3),
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(color: Colors.redAccent),
                            ),
                            child: Row(
                              children: [
                                const Icon(Icons.error_outline, color: Colors.redAccent, size: 18),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    _errorMessage,
                                    style: GoogleFonts.philosopher(
                                      color: Colors.white,
                                      fontSize: 12,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: 14),
                        ],

                        Text(
                          'Tên đăng nhập hoặc Email',
                          style: GoogleFonts.philosopher(
                            color: const Color(0xFFFFECB3),
                            fontSize: 13,
                          ),
                        ),
                        const SizedBox(height: 6),
                        _buildInputField(
                          controller: _userCtrl,
                          hintText: 'Nhập username / email',
                          icon: Icons.person_outline,
                        ),
                        const SizedBox(height: 14),

                        Text(
                          'Mật khẩu',
                          style: GoogleFonts.philosopher(
                            color: const Color(0xFFFFECB3),
                            fontSize: 13,
                          ),
                        ),
                        const SizedBox(height: 6),
                        _buildInputField(
                          controller: _passCtrl,
                          hintText: 'Nhập mật khẩu',
                          icon: Icons.lock_outline,
                          obscure: _obscurePass,
                          suffixIcon: IconButton(
                            icon: Icon(
                              _obscurePass ? Icons.visibility_off : Icons.visibility,
                              color: const Color(0xFFFFD54F),
                              size: 18,
                            ),
                            onPressed: () => setState(() => _obscurePass = !_obscurePass),
                          ),
                        ),
                        const SizedBox(height: 20),

                        // Nút Đăng Nhập Đỏ Son Hoàng Gia
                        SizedBox(
                          height: 46,
                          child: ElevatedButton(
                            onPressed: _isBusy ? null : _login,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color(0xFFC0392B),
                              side: const BorderSide(color: Color(0xFFFFD54F), width: 1.2),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(10),
                              ),
                              elevation: 4,
                            ),
                            child: _isBusy
                                ? const SizedBox(
                                    width: 20,
                                    height: 20,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                      color: Color(0xFFFFD54F),
                                    ),
                                  )
                                : Text(
                                    'ĐĂNG NHẬP',
                                    style: GoogleFonts.cinzel(
                                      fontWeight: FontWeight.bold,
                                      fontSize: 15,
                                      color: Colors.white,
                                      letterSpacing: 1.2,
                                    ),
                                  ),
                          ),
                        ),
                        const SizedBox(height: 16),

                        // Chuyển sang Đăng ký
                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(
                              'Chưa có tài khoản? ',
                              style: GoogleFonts.philosopher(
                                color: const Color(0xFFBCAAA4),
                                fontSize: 13,
                              ),
                            ),
                            GestureDetector(
                              onTap: () async {
                                final ok = await Navigator.of(context).push<bool>(
                                  MaterialPageRoute(builder: (_) => const RegisterScreen()),
                                );
                                if (ok == true && context.mounted) {
                                  Navigator.of(context).pop(true);
                                }
                              },
                              child: Text(
                                'Đăng ký ngay',
                                style: GoogleFonts.philosopher(
                                  color: const Color(0xFFFFD54F),
                                  fontWeight: FontWeight.bold,
                                  fontSize: 13,
                                  decoration: TextDecoration.underline,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),

                        // Nút Khách
                        SizedBox(
                          height: 42,
                          child: OutlinedButton(
                            onPressed: () => Navigator.of(context).pop(false),
                            style: OutlinedButton.styleFrom(
                              side: const BorderSide(color: Color(0xFF5D4037)),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(8),
                              ),
                            ),
                            child: Text(
                              'Tiếp Tục Chơi Ngay (Khách)',
                              style: GoogleFonts.philosopher(
                                color: const Color(0xFFBCAAA4),
                                fontSize: 12,
                              ),
                            ),
                          ),
                        ),
                      ],
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

  Widget _buildInputField({
    required TextEditingController controller,
    required String hintText,
    required IconData icon,
    bool obscure = false,
    Widget? suffixIcon,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: const Color(0xFF140E0B),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: const Color(0xFF4E342E), width: 1.2),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: TextField(
        controller: controller,
        obscureText: obscure,
        style: GoogleFonts.philosopher(color: const Color(0xFFFFECB3), fontSize: 14),
        decoration: InputDecoration(
          icon: Icon(icon, color: const Color(0xFFFFD54F), size: 18),
          hintText: hintText,
          hintStyle: GoogleFonts.philosopher(color: const Color(0xFF6D4C41), fontSize: 13),
          border: InputBorder.none,
          suffixIcon: suffixIcon,
        ),
      ),
    );
  }
}
