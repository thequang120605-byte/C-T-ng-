import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import '../models/game_models.dart';
import '../services/app_services.dart';

class CourseAcademyScreen extends StatefulWidget {
  final VoidCallback onBack;

  const CourseAcademyScreen({super.key, required this.onBack});

  @override
  State<CourseAcademyScreen> createState() => _CourseAcademyScreenState();
}

class _CourseAcademyScreenState extends State<CourseAcademyScreen> {
  List<CourseModuleDto> _modules = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadCourses();
  }

  Future<void> _loadCourses() async {
    setState(() => _isLoading = true);
    try {
      final list = await AppServices.instance.api.getCoursesAsync();
      if (mounted) setState(() => _modules = list);
    } catch (_) {} finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _openLesson(CourseLessonDto lesson) async {
    HapticFeedback.selectionClick();
    final detail = await AppServices.instance.api.getCourseLessonDetailAsync(lesson.id);
    if (!mounted || detail == null) return;

    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => _LessonDetailScreen(lesson: detail),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF140F0D),
      body: SafeArea(
        child: Column(
          children: [
            // Top Bar
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              decoration: const BoxDecoration(
                color: Color(0xFF1E1714),
                border: Border(bottom: BorderSide(color: Color(0xFF4A3728), width: 1.5)),
              ),
              child: Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.arrow_back_ios, color: Color(0xFFFFD54F), size: 18),
                    onPressed: widget.onBack,
                  ),
                  const SizedBox(width: 4),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'VIỆN KỲ ĐẠO',
                          style: GoogleFonts.cinzel(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: const Color(0xFFFFE082),
                            letterSpacing: 1.2,
                          ),
                        ),
                        Text(
                          'Học cờ từ cơ bản đến đỉnh cao cùng đại sư',
                          style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFF8D6E63)),
                        ),
                      ],
                    ),
                  ),
                  const Icon(Icons.school, color: Color(0xFFFFD54F), size: 24),
                ],
              ),
            ),

            // Danh sách các Module khóa học
            Expanded(
              child: _isLoading
                  ? const Center(child: CircularProgressIndicator(color: Color(0xFFFFD54F)))
                  : ListView.builder(
                      padding: const EdgeInsets.all(14),
                      itemCount: _modules.length,
                      itemBuilder: (ctx, idx) {
                        final mod = _modules[idx];
                        return _buildModuleCard(mod);
                      },
                    ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildModuleCard(CourseModuleDto mod) {
    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: const Color(0xFF1E1714),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFF3E2723), width: 1.2),
        boxShadow: const [
          BoxShadow(color: Colors.black45, blurRadius: 4, offset: Offset(0, 2)),
        ],
      ),
      child: Theme(
        data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
        child: ExpansionTile(
          initiallyExpanded: mod.displayOrder == 1,
          leading: Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: const Color(0xFFC0392B).withValues(alpha: 0.2),
              border: Border.all(color: const Color(0xFFFFD54F), width: 1),
            ),
            child: Center(
              child: Text(
                '${mod.displayOrder}',
                style: GoogleFonts.cinzel(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: const Color(0xFFFFD54F),
                ),
              ),
            ),
          ),
          title: Text(
            mod.title,
            style: GoogleFonts.philosopher(
              fontSize: 15,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
          subtitle: Text(
            '${mod.totalLessons} Bài học • ${mod.description}',
            style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFF8D6E63)),
          ),
          children: mod.lessons.map((lesson) {
            return ListTile(
              contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 2),
              leading: const Icon(Icons.play_circle_outline, color: Color(0xFFFFD54F), size: 20),
              title: Text(
                lesson.title,
                style: GoogleFonts.philosopher(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: const Color(0xFFD7CCC8),
                ),
              ),
              subtitle: Text(
                lesson.description,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: GoogleFonts.philosopher(fontSize: 11, color: const Color(0xFF6D4C41)),
              ),
              trailing: const Icon(Icons.arrow_forward_ios, size: 12, color: Color(0xFFFFD54F)),
              onTap: () => _openLesson(lesson),
            );
          }).toList(),
        ),
      ),
    );
  }
}

// Màn hình chi tiết bài học & Trắc nghiệm cờ
class _LessonDetailScreen extends StatefulWidget {
  final CourseLessonDetailDto lesson;

  const _LessonDetailScreen({required this.lesson});

  @override
  State<_LessonDetailScreen> createState() => _LessonDetailScreenState();
}

class _LessonDetailScreenState extends State<_LessonDetailScreen> {
  int _selectedQuizAnswer = -1;
  bool _hasSubmitted = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF140F0D),
      appBar: AppBar(
        backgroundColor: const Color(0xFF1E1714),
        title: Text(
          widget.lesson.title,
          style: GoogleFonts.philosopher(fontSize: 15, fontWeight: FontWeight.bold, color: Colors.white),
        ),
        iconTheme: const IconThemeData(color: Color(0xFFFFD54F)),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Thẻ Lý Thuyết Bài Học
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: const Color(0xFF1E1714),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFF3E2723)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.auto_stories, color: Color(0xFFFFD54F), size: 18),
                      const SizedBox(width: 8),
                      Text(
                        'Lý Thuyết Chiến Thuật',
                        style: GoogleFonts.philosopher(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: const Color(0xFFFFD54F),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  Text(
                    widget.lesson.content.isNotEmpty
                        ? widget.lesson.content
                        : widget.lesson.description,
                    style: GoogleFonts.philosopher(
                      fontSize: 13,
                      color: const Color(0xFFD7CCC8),
                      height: 1.5,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // Câu hỏi trắc nghiệm nếu có
            if (widget.lesson.questions.isNotEmpty) ...[
              Text(
                'BÀI TẬP VẬN DỤNG',
                style: GoogleFonts.cinzel(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: const Color(0xFFFFE082),
                  letterSpacing: 1.1,
                ),
              ),
              const SizedBox(height: 10),

              ...widget.lesson.questions.map((q) {
                List<String> options = [];
                try {
                  final decoded = jsonDecode(q.optionsJson);
                  if (decoded is List) options = decoded.map((e) => e.toString()).toList();
                } catch (_) {
                  options = ['Nước A: Pháo 2 bình 5', 'Nước B: Mã 8 tấn 7', 'Nước C: Xe 9 bình 8'];
                }

                return Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: const Color(0xFF1E1714),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: const Color(0xFF3E2723)),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        q.question,
                        style: GoogleFonts.philosopher(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: Colors.white,
                        ),
                      ),
                      const SizedBox(height: 12),

                      ...List.generate(options.length, (optIdx) {
                        final isSelected = _selectedQuizAnswer == optIdx;
                        final isCorrect = q.correctAnswer == optIdx;

                        Color btnColor = const Color(0xFF2C1E18);
                        Color borderColor = const Color(0xFF5D4037);

                        if (_hasSubmitted) {
                          if (isCorrect) {
                            btnColor = const Color(0xFF2E7D32).withValues(alpha: 0.3);
                            borderColor = const Color(0xFF66BB6A);
                          } else if (isSelected) {
                            btnColor = const Color(0xFFC0392B).withValues(alpha: 0.3);
                            borderColor = const Color(0xFFEF5350);
                          }
                        } else if (isSelected) {
                          borderColor = const Color(0xFFFFD54F);
                        }

                        return GestureDetector(
                          onTap: () {
                            if (!_hasSubmitted) {
                              setState(() => _selectedQuizAnswer = optIdx);
                            }
                          },
                          child: Container(
                            margin: const EdgeInsets.only(bottom: 8),
                            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                            decoration: BoxDecoration(
                              color: btnColor,
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(color: borderColor, width: 1.2),
                            ),
                            child: Row(
                              children: [
                                Text(
                                  '${String.fromCharCode(65 + optIdx)}.',
                                  style: GoogleFonts.cinzel(
                                    fontSize: 13,
                                    fontWeight: FontWeight.bold,
                                    color: const Color(0xFFFFD54F),
                                  ),
                                ),
                                const SizedBox(width: 10),
                                Expanded(
                                  child: Text(
                                    options[optIdx],
                                    style: GoogleFonts.philosopher(
                                      fontSize: 13,
                                      color: Colors.white,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      }),

                      const SizedBox(height: 10),
                      if (!_hasSubmitted)
                        ElevatedButton(
                          onPressed: _selectedQuizAnswer >= 0
                              ? () {
                                  setState(() => _hasSubmitted = true);
                                  if (_selectedQuizAnswer == q.correctAnswer) {
                                    AppServices.instance.sound.playWin();
                                  } else {
                                    AppServices.instance.sound.playIllegal();
                                  }
                                }
                              : null,
                          style: ElevatedButton.styleFrom(
                            backgroundColor: const Color(0xFFC0392B),
                            side: const BorderSide(color: Color(0xFFFFD54F)),
                          ),
                          child: Text(
                            'Kiểm Tra Kết Quả',
                            style: GoogleFonts.philosopher(fontWeight: FontWeight.bold, color: Colors.white),
                          ),
                        )
                      else ...[
                        Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: const Color(0xFF140F0D),
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(color: const Color(0xFF5D4037)),
                          ),
                          child: Text(
                            '💡 Lời giải chi tiết: ${q.explanation.isNotEmpty ? q.explanation : "Nước đi tối ưu nhất giúp chiếm cứ lộ trọng yếu và làm tê liệt thế trận đối phương."}',
                            style: GoogleFonts.philosopher(
                              fontSize: 12,
                              color: const Color(0xFFFFD54F),
                              fontStyle: FontStyle.italic,
                            ),
                          ),
                        ),
                      ],
                    ],
                  ),
                );
              }),
            ],
          ],
        ),
      ),
    );
  }
}
