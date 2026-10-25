import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'home_screen.dart';
import 'leaderboard_screen.dart';
import 'online_lobby_screen.dart';
import 'puzzle_map_screen.dart';

class MainShell extends StatefulWidget {
  const MainShell({super.key});

  @override
  State<MainShell> createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  int _currentIndex = 2; // Mặc định ở Sảnh Cờ (tab trung tâm)

  void _switchTab(int index) {
    setState(() => _currentIndex = index);
  }

  void _showEventModal() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: const Color(0xFF1E1714),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: Color(0xFFFFD54F), width: 1.5),
        ),
        title: Center(
          child: Text(
            'CHUỖI SỰ KIỆN KỲ VƯƠNG',
            style: GoogleFonts.cinzel(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: const Color(0xFFFFE082),
            ),
          ),
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildEventItem(
              title: '🏆 Tranh Đoạt Minh Chủ Võ Lâm',
              desc: 'Thắng liên tiếp 5 ván online nhận 50,000 Vàng',
              progress: '3/5 ván',
            ),
            const Divider(color: Color(0xFF5D4037)),
            _buildEventItem(
              title: '🎯 Đột Phá 10 Ải Cờ Thế',
              desc: 'Đạt 3 sao tại 5 ải đầu nhận Danh hiệu "Phá Trận Kỳ Vương"',
              progress: '3/5 ải',
            ),
          ],
        ),
        actions: [
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx),
            style: ElevatedButton.styleFrom(
              backgroundColor: const Color(0xFFC0392B),
              side: const BorderSide(color: Color(0xFFFFD54F)),
            ),
            child: Text('Đóng', style: GoogleFonts.philosopher(color: Colors.white, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }

  Widget _buildEventItem({required String title, required String desc, required String progress}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: GoogleFonts.philosopher(
              fontSize: 13,
              fontWeight: FontWeight.bold,
              color: const Color(0xFFFFD54F),
            ),
          ),
          const SizedBox(height: 2),
          Text(
            desc,
            style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFFD7CCC8)),
          ),
          const SizedBox(height: 2),
          Text(
            'Tiến độ: $progress',
            style: GoogleFonts.philosopher(
              fontSize: 10,
              fontWeight: FontWeight.bold,
              color: const Color(0xFF81C784),
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: [
          // Tab 0: Sảnh Cờ (fallback nếu chọn event)
          HomeScreen(
            onGoToLobby: () => _switchTab(3),
            onGoToLeaderboard: () => _switchTab(1),
            onGoToPuzzle: () => _switchTab(4),
          ),
          // Tab 1: Xếp Hạng
          LeaderboardScreen(
            onBackToHome: () => _switchTab(2),
          ),
          // Tab 2: Sảnh Cờ Chính
          HomeScreen(
            onGoToLobby: () => _switchTab(3),
            onGoToLeaderboard: () => _switchTab(1),
            onGoToPuzzle: () => _switchTab(4),
          ),
          // Tab 3: Chọn Bàn
          const OnlineLobbyScreen(),
          // Tab 4: Vượt Ải Cờ Thế
          PuzzleMapScreen(
            onBackToHome: () => _switchTab(2),
          ),
        ],
      ),
      bottomNavigationBar: Container(
        decoration: const BoxDecoration(
          color: Color(0xFF1E1714),
          border: Border(
            top: BorderSide(color: Color(0xFF4A3728), width: 1.5),
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black54,
              blurRadius: 8,
              offset: Offset(0, -2),
            ),
          ],
        ),
        child: BottomNavigationBar(
          currentIndex: _currentIndex,
          onTap: (index) {
            if (index == 0) {
              _showEventModal();
            } else {
              _switchTab(index);
            }
          },
          backgroundColor: const Color(0xFF1E1714),
          selectedItemColor: const Color(0xFFFFD54F),
          unselectedItemColor: const Color(0xFF8D6E63),
          selectedLabelStyle: GoogleFonts.cinzel(fontWeight: FontWeight.bold, fontSize: 10),
          unselectedLabelStyle: GoogleFonts.cinzel(fontSize: 9),
          type: BottomNavigationBarType.fixed,
          elevation: 0,
          items: const [
            BottomNavigationBarItem(
              icon: Icon(Icons.card_giftcard),
              label: 'Sự Kiện',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.military_tech),
              activeIcon: Icon(Icons.military_tech, color: Color(0xFFFFD54F)),
              label: 'Xếp Hạng',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.home_outlined),
              activeIcon: Icon(Icons.home, color: Color(0xFFFFD54F)),
              label: 'Sảnh Cờ',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.grid_view),
              activeIcon: Icon(Icons.grid_view, color: Color(0xFFFFD54F)),
              label: 'Chọn Bàn',
            ),
            BottomNavigationBarItem(
              icon: Icon(Icons.map_outlined),
              activeIcon: Icon(Icons.map, color: Color(0xFFFFD54F)),
              label: 'Vượt Ải',
            ),
          ],
        ),
      ),
    );
  }
}
