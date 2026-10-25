import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

class AppColors {
  // Nền tối cổ phong sang trọng
  static const Color darkBg = Color(0xFF14100E);
  static const Color darkSurface = Color(0xFF1E1714);
  static const Color darkCard = Color(0xFF281E1A);
  static const Color darkBorder = Color(0xFF4A3728);

  // Cuộn thư giấy hoàng gia / Giấy dó
  static const Color paperBg = Color(0xFFF5EAD4);
  static const Color paperDark = Color(0xFFE4D3B4);
  static const Color paperLight = Color(0xFFFBF4E7);
  static const Color paperBorder = Color(0xFFBCA67F);

  // Vàng hoàng kim & Đồng cổ
  static const Color goldLight = Color(0xFFFFE082);
  static const Color gold = Color(0xFFE5B869);
  static const Color goldDark = Color(0xFFB38634);
  static const Color bronze = Color(0xFF8D6E63);

  // Mực & Chu sa
  static const Color vermilion = Color(0xFFBA2D1D); // Đỏ cờ Tướng
  static const Color vermilionDark = Color(0xFF7E1C11);
  static const Color ink = Color(0xFF1C1917);      // Mực đen cờ Tướng
  static const Color inkMuted = Color(0xFF6B5E51);
  static const Color jade = Color(0xFF16A085);     // Xanh ngọc bích
  static const Color jadeDark = Color(0xFF0E6655);
  static const Color amber = Color(0xFFE67E22);

  // Bàn cờ gỗ 3D
  static const Color woodLight = Color(0xFFE8C896);
  static const Color woodBase = Color(0xFFDEB881);
  static const Color woodDark = Color(0xFF9E6B38);
  static const Color woodBorder = Color(0xFF613B16);
  static const Color gridLine = Color(0xFF5A361A);
  
  // Legacy aliases for backward compatibility
  static const Color paper = paperBg;
  static const Color card = darkCard;
  static const Color muted = inkMuted;
  static const Color line = darkBorder;
  static const Color dark = darkBg;
  static const Color primary = vermilion;
  static const Color quietBg = darkSurface;
  static const Color teal = jade;
  static const Color danger = Color(0xFFC0392B);
  static const Color success = Color(0xFF27AE60);
}

class AppTheme {
  static ThemeData build() {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      scaffoldBackgroundColor: AppColors.darkBg,
      colorScheme: const ColorScheme.dark(
        surface: AppColors.darkSurface,
        primary: AppColors.gold,
        secondary: AppColors.vermilion,
        onSurface: AppColors.paperLight,
        outline: AppColors.darkBorder,
      ),
      textTheme: GoogleFonts.philosopherTextTheme(
        const TextTheme(
          displayLarge: TextStyle(color: AppColors.goldLight, fontWeight: FontWeight.bold),
          displayMedium: TextStyle(color: AppColors.gold, fontWeight: FontWeight.bold),
          titleLarge: TextStyle(color: AppColors.paperLight, fontWeight: FontWeight.w700),
          titleMedium: TextStyle(color: AppColors.paperLight, fontWeight: FontWeight.w600),
          bodyLarge: TextStyle(color: AppColors.paperLight),
          bodyMedium: TextStyle(color: Color(0xFFD6C8B4)),
          bodySmall: TextStyle(color: AppColors.inkMuted),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.goldDark,
          foregroundColor: AppColors.darkBg,
          elevation: 3,
          padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
            side: const BorderSide(color: AppColors.goldLight, width: 1.2),
          ),
          textStyle: GoogleFonts.philosopher(
            fontWeight: FontWeight.bold,
            fontSize: 14,
          ),
        ),
      ),
    );
  }

  // Kiểu chữ Thư pháp Cổ phong cho Tiêu đề
  static TextStyle calligraphy({
    double fontSize = 20,
    Color color = AppColors.goldLight,
    FontWeight fontWeight = FontWeight.bold,
    List<Shadow>? shadows,
  }) {
    return GoogleFonts.cinzel(
      fontSize: fontSize,
      fontWeight: fontWeight,
      color: color,
      letterSpacing: 1.2,
      shadows: shadows ?? [
        const Shadow(
          color: Colors.black87,
          offset: Offset(1, 2),
          blurRadius: 4,
        ),
      ],
    );
  }

  // Kiểu chữ Hán Cổ phong cho Quân cờ
  static TextStyle pieceChinese({
    required double fontSize,
    required Color color,
  }) {
    return GoogleFonts.maShanZheng(
      fontSize: fontSize,
      fontWeight: FontWeight.bold,
      color: color,
      height: 1.0,
    );
  }

  // Kiểu chữ số Tọa độ cổ
  static TextStyle coordinate({double fontSize = 12, Color color = AppColors.woodDark}) {
    return GoogleFonts.cinzel(
      fontSize: fontSize,
      fontWeight: FontWeight.w700,
      color: color,
    );
  }

  // Card decoration dùng cho các màn hình Form & Profile
  static BoxDecoration cardDecoration({
    double radius = 16,
    Color? color,
    Color? borderColor,
  }) {
    return BoxDecoration(
      color: color ?? AppColors.darkCard,
      borderRadius: BorderRadius.circular(radius),
      border: Border.all(
        color: borderColor ?? AppColors.darkBorder,
        width: 1.2,
      ),
      boxShadow: const [
        BoxShadow(
          color: Colors.black45,
          blurRadius: 8,
          offset: Offset(0, 3),
        ),
      ],
    );
  }

  static ButtonStyle quietButtonStyle({double fontSize = 13}) {
    return ElevatedButton.styleFrom(
      backgroundColor: AppColors.darkSurface,
      foregroundColor: AppColors.goldLight,
      elevation: 0,
      minimumSize: const Size(48, 42),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: const BorderSide(color: AppColors.darkBorder, width: 1),
      ),
      textStyle: GoogleFonts.philosopher(
        fontSize: fontSize,
        fontWeight: FontWeight.bold,
      ),
    );
  }

  // Khung viền mạ vàng / đồng hoàng gia
  static BoxDecoration imperialBorder({
    Color bgColor = AppColors.darkSurface,
    Color borderColor = AppColors.gold,
    double radius = 12,
  }) {
    return BoxDecoration(
      color: bgColor,
      borderRadius: BorderRadius.circular(radius),
      border: Border.all(color: borderColor.withAlpha(200), width: 1.5),
      boxShadow: [
        BoxShadow(
          color: borderColor.withAlpha(40),
          blurRadius: 8,
          spreadRadius: 1,
        ),
        const BoxShadow(
          color: Colors.black54,
          blurRadius: 6,
          offset: Offset(0, 3),
        ),
      ],
    );
  }
}
