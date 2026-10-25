import 'package:flutter/material.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';
import '../theme/app_theme.dart';

class ProfileScreen extends StatefulWidget {
  final VoidCallback onBackToHome;

  const ProfileScreen({super.key, required this.onBackToHome});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  AuthUserDto? _user;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  Future<void> _loadProfile() async {
    setState(() => _isLoading = true);
    try {
      final user = await AppServices.instance.auth.fetchUserProfileAsync();
      if (mounted) setState(() => _user = user);
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final username = _user?.username.isNotEmpty == true
        ? _user!.username
        : 'Kỳ thủ khách';
    final playerId = _user?.playerId.isNotEmpty == true ? _user!.playerId : '—';
    final email = _user?.email.isNotEmpty == true ? _user!.email : 'Chưa đăng nhập';
    final elo = _user != null && _user!.elo > 0 ? '${_user!.elo}' : '—';
    final wins = _user?.wins ?? 0;
    final losses = _user?.losses ?? 0;
    final draws = _user?.draws ?? 0;
    final totalGames = wins + losses + draws;
    final winRate = totalGames > 0
        ? ((wins * 1000 ~/ totalGames) / 10.0).toStringAsFixed(1)
        : '0.0';

    return Scaffold(
      backgroundColor: AppColors.paper,
      body: SafeArea(
        child: _isLoading
            ? const Center(
                child: CircularProgressIndicator(color: AppColors.gold),
              )
            : SingleChildScrollView(
                padding: const EdgeInsets.all(20),
                child: Center(
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 600),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        // Card Thông tin cá nhân
                        Container(
                          padding: const EdgeInsets.all(24),
                          decoration: AppTheme.cardDecoration(radius: 16),
                          child: Column(
                            children: [
                              const Text('👤', style: TextStyle(fontSize: 48)),
                              const SizedBox(height: 10),
                              Text(
                                username,
                                style: const TextStyle(
                                  fontSize: 22,
                                  fontWeight: FontWeight.bold,
                                  color: AppColors.ink,
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                'Mã định danh: $playerId',
                                style: const TextStyle(
                                  fontSize: 13,
                                  color: AppColors.muted,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                'Email: $email',
                                style: const TextStyle(
                                  fontSize: 12,
                                  color: AppColors.muted,
                                ),
                              ),
                              const SizedBox(height: 16),

                              // Elo badge card
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 20,
                                  vertical: 10,
                                ),
                                decoration: BoxDecoration(
                                  color: const Color(0xFF451A03),
                                  borderRadius: BorderRadius.circular(12),
                                  border: Border.all(
                                    color: const Color(0xFF9F422F),
                                  ),
                                ),
                                child: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    const Text(
                                      'HỆ SỐ ELO: ',
                                      style: TextStyle(
                                        fontSize: 13,
                                        fontWeight: FontWeight.bold,
                                        color: Color(0xFF9A6B2D),
                                      ),
                                    ),
                                    Text(
                                      elo,
                                      style: const TextStyle(
                                        fontSize: 24,
                                        fontWeight: FontWeight.bold,
                                        color: Color(0xFF9A6B2D),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 20),

                        // Card Thống kê chiến tích
                        Container(
                          padding: const EdgeInsets.all(20),
                          decoration: AppTheme.cardDecoration(radius: 16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                'Thống Kê Chiến Tích',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  color: AppColors.ink,
                                ),
                              ),
                              const SizedBox(height: 16),
                              Row(
                                mainAxisAlignment:
                                    MainAxisAlignment.spaceAround,
                                children: [
                                  _buildStatItem('Tổng số ván', '$totalGames',
                                      AppColors.ink),
                                  _buildStatItem(
                                      'Thắng', '$wins', const Color(0xFF31765D)),
                                  _buildStatItem(
                                      'Hòa', '$draws', const Color(0xFF936D28)),
                                  _buildStatItem(
                                      'Thua', '$losses', const Color(0xFFEF4444)),
                                ],
                              ),
                              const SizedBox(height: 16),
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 14,
                                  vertical: 10,
                                ),
                                decoration: BoxDecoration(
                                  color: AppColors.paper,
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Row(
                                  mainAxisAlignment:
                                      MainAxisAlignment.spaceBetween,
                                  children: [
                                    const Text(
                                      'Tỷ lệ thắng trận:',
                                      style: TextStyle(
                                        color: Color(0xFF655541),
                                        fontSize: 14,
                                      ),
                                    ),
                                    Text(
                                      '$winRate%',
                                      style: const TextStyle(
                                        color: Color(0xFF31765D),
                                        fontSize: 16,
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 20),

                        TextButton(
                          onPressed: widget.onBackToHome,
                          child: const Text(
                            'Quay lại Trang Chủ',
                            style: TextStyle(
                              color: AppColors.muted,
                              fontSize: 13,
                            ),
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

  Widget _buildStatItem(String label, String value, Color color) {
    return Column(
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 11, color: AppColors.muted),
        ),
        const SizedBox(height: 4),
        Text(
          value,
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: color,
          ),
        ),
      ],
    );
  }
}
