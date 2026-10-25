import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../services/app_services.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _userCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _passCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();
  bool _isBusy = false;
  bool _obscurePass = true;
  bool _obscureConfirm = true;
  String _errorMessage = '';

  @override
  void dispose() {
    _userCtrl.dispose();
    _emailCtrl.dispose();
    _passCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  Future<void> _register() async {
    if (_isBusy) return;
    final username = _userCtrl.text.trim();
    final email = _emailCtrl.text.trim();
    final password = _passCtrl.text;
    final confirm = _confirmCtrl.text;

    if (username.isEmpty || email.isEmpty || password.isEmpty) {
      setState(() => _errorMessage = 'Vui lòng nhập đầy đủ thông tin đăng ký.');
      return;
    }
    if (password != confirm) {
      setState(() => _errorMessage = 'Mật khẩu xác nhận chưa khớp.');
      return;
    }

    setState(() {
      _isBusy = true;
      _errorMessage = '';
    });
    try {
      final (ok, msg, _) = await AppServices.instance.auth
          .registerAsync(username, email, password);
      if (!mounted) return;
      if (!ok) {
        setState(() => _errorMessage = msg);
        return;
      }
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
                  // Nút Quay lại
                  Align(
                    alignment: Alignment.centerLeft,
                    child: GestureDetector(
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
                  ),
                  const SizedBox(height: 8),

                  // Logo Quân Tướng
                  Container(
                    width: 68,
                    height: 68,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      gradient: const LinearGradient(
                        colors: [Color(0xFFBA2213), Color(0xFF6B1107)],
                      ),
                      border: Border.all(color: const Color(0xFFFFD54F), width: 2),
                      boxShadow: const [
                        BoxShadow(color: Colors.black54, blurRadius: 8, offset: Offset(0, 3)),
                      ],
                    ),
                    child: Center(
                      child: Text(
                        '將',
                        style: GoogleFonts.maShanZheng(
                          fontSize: 36,
                          fontWeight: FontWeight.bold,
                          color: const Color(0xFFFFE082),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 10),

                  Text(
                    'GIA NHẬP KỲ ĐẠO',
                    style: GoogleFonts.cinzel(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: const Color(0xFFFFE082),
                      letterSpacing: 1.5,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Tạo danh tính để lưu giữ chiến tích và leo bảng xếp hạng',
                    textAlign: TextAlign.center,
                    style: GoogleFonts.philosopher(
                      fontSize: 12,
                      color: const Color(0xFFBCAAA4),
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Card Đăng ký Cổ Phong
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

                        Text('Tên đăng nhập', style: GoogleFonts.philosopher(color: const Color(0xFFFFECB3), fontSize: 13)),
                        const SizedBox(height: 6),
                        _buildInputField(controller: _userCtrl, hintText: 'Từ 3-30 ký tự', icon: Icons.person_outline),
                        const SizedBox(height: 12),

                        Text('Địa chỉ Email', style: GoogleFonts.philosopher(color: const Color(0xFFFFECB3), fontSize: 13)),
                        const SizedBox(height: 6),
                        _buildInputField(controller: _emailCtrl, hintText: 'example@gmail.com', icon: Icons.email_outlined),
                        const SizedBox(height: 12),

                        Text('Mật khẩu', style: GoogleFonts.philosopher(color: const Color(0xFFFFECB3), fontSize: 13)),
                        const SizedBox(height: 6),
                        _buildInputField(
                          controller: _passCtrl,
                          hintText: 'Tối thiểu 6 ký tự',
                          icon: Icons.lock_outline,
                          obscure: _obscurePass,
                          suffixIcon: IconButton(
                            icon: Icon(_obscurePass ? Icons.visibility_off : Icons.visibility, color: const Color(0xFFFFD54F), size: 18),
                            onPressed: () => setState(() => _obscurePass = !_obscurePass),
                          ),
                        ),
                        const SizedBox(height: 12),

                        Text('Xác nhận mật khẩu', style: GoogleFonts.philosopher(color: const Color(0xFFFFECB3), fontSize: 13)),
                        const SizedBox(height: 6),
                        _buildInputField(
                          controller: _confirmCtrl,
                          hintText: 'Nhập lại mật khẩu trên',
                          icon: Icons.lock_reset,
                          obscure: _obscureConfirm,
                          suffixIcon: IconButton(
                            icon: Icon(_obscureConfirm ? Icons.visibility_off : Icons.visibility, color: const Color(0xFFFFD54F), size: 18),
                            onPressed: () => setState(() => _obscureConfirm = !_obscureConfirm),
                          ),
                        ),
                        const SizedBox(height: 20),

                        // Nút Đăng Ký
                        SizedBox(
                          height: 46,
                          child: ElevatedButton(
                            onPressed: _isBusy ? null : _register,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: const Color(0xFFC0392B),
                              side: const BorderSide(color: Color(0xFFFFD54F), width: 1.2),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                              elevation: 4,
                            ),
                            child: _isBusy
                                ? const SizedBox(
                                    width: 20,
                                    height: 20,
                                    child: CircularProgressIndicator(strokeWidth: 2, color: Color(0xFFFFD54F)),
                                  )
                                : Text(
                                    'ĐĂNG KÝ NGAY',
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

                        Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(
                              'Đã có tài khoản? ',
                              style: GoogleFonts.philosopher(color: const Color(0xFFBCAAA4), fontSize: 13),
                            ),
                            GestureDetector(
                              onTap: () => Navigator.pop(context),
                              child: Text(
                                'Đăng nhập',
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
