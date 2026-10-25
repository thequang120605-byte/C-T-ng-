import 'package:flutter/material.dart';
import 'screens/main_shell.dart';
import 'services/app_services.dart';
import 'theme/app_theme.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await AppServices.instance.init();
  runApp(const ChessCoreApp());
}

class ChessCoreApp extends StatelessWidget {
  const ChessCoreApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Kỳ Đạo Pixel',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.build(),
      home: const MainShell(),
    );
  }
}
