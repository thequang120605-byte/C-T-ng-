using System.Windows.Input;
using ChessCore.Mobile.Models;

namespace ChessCore.Mobile.ViewModels;

public sealed class CourseViewModel : BaseViewModel
{
    private readonly List<CourseLesson> _lessons = new()
    {
        // MODULE 1: Nhập Môn Cờ Tướng & Luật Chơi
        new()
        {
            Id = 1, ModuleId = 1, ModuleTitle = "Module 1: Nhập Môn & Luật Chơi",
            LessonTitle = "Bài 1: Sông & Bàn Cờ Tướng",
            Content = "Bàn cờ tướng gồm 9 đường dọc và 10 đường ngang cắt nhau tại 90 điểm. Khoảng trống ở giữa hàng 5 và 6 gọi là Sở Hà - Hán Giới (Sông), ngăn cách trận địa hai bên Đỏ và Đen.",
            Question = "Khoảng trống giữa hàng 5 và 6 trên bàn cờ tướng có tên gọi là gì?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Cửu Cung" },
                new() { Index = 1, Text = "B. Sở Hà - Hán Giới (Sông)" },
                new() { Index = 2, Text = "C. Hào thành biên giới" }
            },
            CorrectOptionIndex = 1,
            Explanation = "Khoảng trống giữa hai nửa bàn cờ gọi là Sở Hà - Hán Giới, lấy tích từ chiến tranh Hán - Sở tranh hùng thời cổ đại."
        },
        new()
        {
            Id = 2, ModuleId = 1, ModuleTitle = "Module 1: Nhập Môn & Luật Chơi",
            LessonTitle = "Bài 2: Tướng & Sĩ Trong Cửu Cung",
            Content = "Tướng và Sĩ là hai quân thủ hiểm của mỗi bên, chỉ được phép di chuyển bên trong phạm vi Cửu Cung (ô vuông 3x3). Tướng đi từng ô ngang/dọc, Sĩ đi chéo 1 ô. Hai Tướng không bao giờ được nhìn thẳng mặt nhau trên cùng một cột mà không có quân cản.",
            Question = "Quân Tướng trong Cờ Tướng có được phép rời khỏi Cửu Cung không?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Được phép khi đối phương chiếu tướng" },
                new() { Index = 1, Text = "B. Tuyệt đối không, Tướng chỉ di chuyển trong Cửu Cung" },
                new() { Index = 2, Text = "C. Được phép sau khi đã qua sông" }
            },
            CorrectOptionIndex = 1,
            Explanation = "Theo luật Cờ Tướng chuẩn quốc tế, Tướng và Sĩ chỉ được phép di chuyển trong phạm vi 9 điểm của Cửu Cung."
        },
        new()
        {
            Id = 3, ModuleId = 1, ModuleTitle = "Module 1: Nhập Môn & Luật Chơi",
            LessonTitle = "Bài 3: Quy Tắc Đi Của Tượng & Cản Tượng",
            Content = "Tượng đi chéo theo hình chữ điền (2x2 ô). Tượng không được qua sông, chỉ phòng thủ trên phần sân nhà. Nếu có một quân nằm ngay chính giữa đường chéo của chữ điền, Tượng sẽ bị 'chặn mắt' và không thể đi theo hướng đó.",
            Question = "Quân Tượng có thể vượt qua sông sang phần sân đối phương được không?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Không bao giờ, Tượng không được qua sông" },
                new() { Index = 1, Text = "B. Có, nếu đối phương đã mất hết Sĩ" },
                new() { Index = 2, Text = "C. Được phép nếu sông không có quân cản" }
            },
            CorrectOptionIndex = 0,
            Explanation = "Tượng là quân thuần túy phòng thủ, tuân theo luật 'Tượng bất quá hà' (Tượng không qua sông)."
        },
        new()
        {
            Id = 4, ModuleId = 1, ModuleTitle = "Module 1: Nhập Môn & Luật Chơi",
            LessonTitle = "Bài 4: Sức Mạnh Xe & Pháo",
            Content = "Xe là quân mạnh nhất bàn cờ, đi ngang dọc tùy ý miễn không bị cản. Pháo di chuyển giống Xe khi không ăn quân, nhưng khi ăn quân bắt buộc phải nhảy qua đúng một quân trung gian gọi là 'ngòi'.",
            Question = "Để ăn được quân đối phương, quân Pháo cần điều kiện gì?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Đi thẳng vào quân đó như Xe" },
                new() { Index = 1, Text = "B. Nhảy qua đúng một quân bất kỳ làm ngòi" },
                new() { Index = 2, Text = "C. Phải nhảy qua ít nhất hai quân" }
            },
            CorrectOptionIndex = 1,
            Explanation = "Đặc trưng độc đáo của Pháo là cần có đúng một quân chắn giữa (quân của mình hoặc của đối thủ) để làm ngòi bắn."
        },
        new()
        {
            Id = 5, ModuleId = 1, ModuleTitle = "Module 1: Nhập Môn & Luật Chơi",
            LessonTitle = "Bài 5: Nước Đi Quân Mã & Cản Mã",
            Content = "Mã đi theo hình chữ nhật (1 ô thẳng rồi 1 ô chéo). Nếu có một quân đứng ngay ô thẳng kề sát Mã theo hướng định đi, Mã sẽ bị 'cản chân' (cản mã cước) và không thể nhảy sang hai điểm phía sau.",
            Question = "Khi nào quân Mã bị cản chân không thể nhảy?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Khi có quân đứng sát ngay ô thẳng phía trước hướng nhảy" },
                new() { Index = 1, Text = "B. Khi ô đích đến có quân đối phương" },
                new() { Index = 2, Text = "C. Khi Mã nhảy qua sông" }
            },
            CorrectOptionIndex = 0,
            Explanation = "Mã bị cản chân khi có bất kỳ quân nào (của ta hoặc của bạn) án ngữ ngay tại ô liền kề theo phương ngang hoặc dọc."
        },
        new()
        {
            Id = 6, ModuleId = 1, ModuleTitle = "Module 1: Nhập Môn & Luật Chơi",
            LessonTitle = "Bài 6: Quân Tốt Trước & Sau Qua Sông",
            Content = "Khi chưa qua sông, Tốt chỉ có thể đi thẳng 1 ô về phía trước. Sau khi đã qua sông, Tốt được phép đi thẳng 1 ô hoặc đi ngang 1 ô. Tốt không bao giờ được phép đi lùi.",
            Question = "Sau khi qua sông, quân Tốt có thể di chuyển theo những hướng nào?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Chỉ đi thẳng như cũ" },
                new() { Index = 1, Text = "B. Đi thẳng hoặc đi ngang sang hai bên, không được lùi" },
                new() { Index = 2, Text = "C. Đi cả 4 hướng tiến, lùi, trái, phải" }
            },
            CorrectOptionIndex = 1,
            Explanation = "Tốt qua sông như 'hổ mọc thêm cánh', được đi cả thẳng lẫn ngang sang trái/phải, tạo nên sức ép rất lớn lên cửu cung."
        },

        // MODULE 2: Các Thế Sát Pháp Căn Bản
        new()
        {
            Id = 7, ModuleId = 2, ModuleTitle = "Module 2: Sát Pháp Căn Bản",
            LessonTitle = "Bài 7: Sát Pháp Mã Hậu Pháo",
            Content = "Mã Hậu Pháo là đòn phối hợp kinh điển: Mã đứng ở hàng trên khống chế các ô thoát của Tướng, đồng thời làm ngòi cho Pháo đứng ngay sau lưng bắn chiếu bí.",
            Question = "Trong đòn Mã Hậu Pháo, vai trò của quân Mã là gì?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Chiếu bí trực tiếp Tướng" },
                new() { Index = 1, Text = "B. Vừa khống chế ô thoát vừa làm ngòi cho Pháo chiếu bí" },
                new() { Index = 2, Text = "C. Phòng thủ sân nhà" }
            },
            CorrectOptionIndex = 1,
            Explanation = "Mã đứng trước vừa khóa đường né của Tướng vừa đóng vai trò làm ngòi tuyệt đối an toàn cho Pháo phía sau chiếu dứt điểm."
        },
        new()
        {
            Id = 8, ModuleId = 2, ModuleTitle = "Module 2: Sát Pháp Căn Bản",
            LessonTitle = "Bài 8: Sát Pháp Thiết Môn Thuyên",
            Content = "Thiết Môn Thuyên (Then Cửa Sắt) dùng Pháo trấn giữ trung lộ, ngăn Tướng di chuyển sang cánh khác, sau đó dùng Xe đâm thẳng xuống đáy cung chiếu bí.",
            Question = "Vì sao Tướng đối phương không thể chạy thoát trong thế Thiết Môn Thuyên?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Vì Pháo trung lộ đã khóa chặt đường sang bên kia cung" },
                new() { Index = 1, Text = "B. Vì Tướng bị quân Sĩ của mình cản đường" },
                new() { Index = 2, Text = "C. Vì Mã đã ăn hết quân phòng thủ" }
            },
            CorrectOptionIndex = 0,
            Explanation = "Pháo giữ trung lộ như then cửa sắt vững chắc, Xe đâm chiếu đáy khiến Tướng không còn đường trốn thoát."
        },
        new()
        {
            Id = 9, ModuleId = 2, ModuleTitle = "Module 2: Sát Pháp Căn Bản",
            LessonTitle = "Bài 9: Sát Pháp Trọng Pháo",
            Content = "Trọng Pháo là thế hai Pháo cùng nằm trên một cột hoặc hàng chiếu thẳng vào Tướng. Pháo sau lấy Pháo trước làm ngòi, đối phương không thể ăn Pháo sau vì bị che chắn.",
            Question = "Trong thế Trọng Pháo, Pháo sau mượn quân nào làm ngòi?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Quân Tướng đối phương" },
                new() { Index = 1, Text = "B. Chính quân Pháo đồng minh phía trước" },
                new() { Index = 2, Text = "C. Quân Xe đối phương" }
            },
            CorrectOptionIndex = 1,
            Explanation = "Hai Pháo trùng nhau tạo thành hỏa lực liên hoàn cực mạnh, Pháo này làm ngòi cho Pháo kia chiếu bí không thể chống đỡ."
        },
        new()
        {
            Id = 10, ModuleId = 2, ModuleTitle = "Module 2: Sát Pháp Căn Bản",
            LessonTitle = "Bài 10: Song Xa Phá Thành",
            Content = "Hai quân Xe phối hợp: một Xe đâm chiếu ép Tướng lên tầng 2 hoặc ra góc, Xe thứ hai tiến tới kết liễu hoặc cắt đứt đường lui của Tướng đối phương.",
            Question = "Nguyên tắc cốt lõi khi sử dụng Song Xa tấn công là gì?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Xe trước chiếu mở đường, Xe sau khóa cánh hoặc đâm kết liễu" },
                new() { Index = 1, Text = "B. Tách hai Xe ra hai góc xa nhau" },
                new() { Index = 2, Text = "C. Luôn đổi Xe lấy Tượng của bạn" }
            },
            CorrectOptionIndex = 0,
            Explanation = "Hai Xe đồng bộ yểm trợ nhau tạo thành thế công phá thành trì mãnh liệt nhất trong Cờ Tướng."
        },

        // MODULE 3: Khai Cuộc & Chiến Thuật
        new()
        {
            Id = 11, ModuleId = 3, ModuleTitle = "Module 3: Khai Cuộc & Chiến Thuật",
            LessonTitle = "Bài 11: Pháo Đầu đối Bình Phong Mã",
            Content = "Pháo Đầu (Trung Pháo) là thế khai cuộc công sát mạnh mẽ nhất nhằm vào trung lộ. Đối lại, Bình Phong Mã là trận đồ phòng thủ vững chắc và toàn diện nhất lịch sử Cờ Tướng.",
            Question = "Thế cờ nào được xem là đối trọng phòng thủ vững chắc nhất trước Pháo Đầu?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Đơn Đề Mã" },
                new() { Index = 1, Text = "B. Bình Phong Mã (Hai Mã cùng giữ trung lộ)" },
                new() { Index = 2, Text = "C. Điệp Pháo" }
            },
            CorrectOptionIndex = 1,
            Explanation = "Bình Phong Mã với hai Mã hướng tâm bảo vệ nhau, kết hợp Tượng và Sĩ tạo thành thế trận công thủ toàn diện."
        },
        new()
        {
            Id = 12, ModuleId = 3, ModuleTitle = "Module 3: Khai Cuộc & Chiến Thuật",
            LessonTitle = "Bài 12: Thuận Pháo & Nghịch Pháo",
            Content = "Thuận Pháo là khi hai bên cùng đưa Pháo vào trung lộ cùng một hướng. Nghịch Pháo là hai bên đưa Pháo vào trung lộ nhưng ở hai hướng ngược nhau, thường dẫn đến cuộc đối công quyết liệt một mất một còn.",
            Question = "Thế trận Nghịch Pháo thường mang đặc điểm gì?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Hòa hoãn và thăm dò lâu dài" },
                new() { Index = 1, Text = "B. Đối công ác liệt, thắng thua phân định nhanh chóng" },
                new() { Index = 2, Text = "C. Thuần túy phòng ngự thủ hòa" }
            },
            CorrectOptionIndex = 1,
            Explanation = "Nghịch Pháo đưa hai bên vào thế trận bất đối xứng, hai bên đua nhau công sát thành lũy đối phương vô cùng khốc liệt."
        },
        new()
        {
            Id = 13, ModuleId = 3, ModuleTitle = "Module 3: Khai Cuộc & Chiến Thuật",
            LessonTitle = "Bài 13: Quy Tắc Xuất Xe Nhanh",
            Content = "Khẩu quyết khai cuộc: 'Tam bộ xuất xa' - Trong 3 nước đầu nên tìm cách xuất Xe ra lộ thông thoáng (lộ 2, 4, 6, 8) để chiếm cứ điểm, kiểm soát sông và hạn chế quân đối phương phát triển.",
            Question = "Tại sao cần ưu tiên xuất quân Xe sớm trong giai đoạn khai cuộc?",
            Options = new()
            {
                new() { Index = 0, Text = "A. Xe có tầm hoạt động rộng nhất, kiểm soát các lộ xung yếu" },
                new() { Index = 1, Text = "B. Để đổi Xe với Tốt của đối phương" },
                new() { Index = 2, Text = "C. Để bảo vệ quân Tướng" }
            },
            CorrectOptionIndex = 0,
            Explanation = "Xe chậm ra trận thì như tướng soái thiếu binh hùng, Xe ra sớm kiểm soát trung lộ và tuyến sông quyết định thế trận."
        }
    };

    private int _currentIndex;
    private int? _selectedOptionIndex;
    private bool _isSubmitted;
    private bool _isCorrect;
    private string _feedbackText = "";

    public CourseViewModel()
    {
        SelectOptionCommand = new Command<int>(OnSelectOption);
        SubmitCommand = new Command(OnSubmit);
        ResetQuizCommand = new Command(OnReset);
        NextLessonCommand = new Command(OnNextLesson);
        PrevLessonCommand = new Command(OnPrevLesson);
        SelectModuleCommand = new Command<int>(OnSelectModule);
        BackCommand = new AsyncCommand(async () =>
        {
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//home");
        }, _ => { });
    }

    public ICommand SelectOptionCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand ResetQuizCommand { get; }
    public ICommand NextLessonCommand { get; }
    public ICommand PrevLessonCommand { get; }
    public ICommand SelectModuleCommand { get; }
    public ICommand BackCommand { get; }

    public int CurrentIndex => _currentIndex;
    public int LessonNumber => _currentIndex + 1;
    public int TotalLessons => _lessons.Count;
    public CourseLesson CurrentLesson => _lessons[_currentIndex];

    public string ModuleTitle => CurrentLesson.ModuleTitle;
    public string LessonTitle => CurrentLesson.LessonTitle;
    public string Content => CurrentLesson.Content;
    public string Question => CurrentLesson.Question;
    public List<CourseQuizOption> Options => CurrentLesson.Options;
    public string Explanation => CurrentLesson.Explanation;

    public int? SelectedOptionIndex { get => _selectedOptionIndex; private set => SetProperty(ref _selectedOptionIndex, value); }
    public bool IsSubmitted { get => _isSubmitted; private set => SetProperty(ref _isSubmitted, value); }
    public bool IsCorrect { get => _isCorrect; private set => SetProperty(ref _isCorrect, value); }
    public string FeedbackText { get => _feedbackText; private set => SetProperty(ref _feedbackText, value); }

    public bool CanGoNext => _currentIndex < _lessons.Count - 1;
    public bool CanGoPrev => _currentIndex > 0;

    public string ProgressText => $"Bài {LessonNumber} / {TotalLessons}";

    public Color GetOptionBackground(int optionIndex)
    {
        if (!_isSubmitted)
        {
            return _selectedOptionIndex == optionIndex ? Color.FromArgb("#D4AF37") : Color.FromArgb("#FFF9EF");
        }

        if (optionIndex == CurrentLesson.CorrectOptionIndex)
        {
            return Color.FromArgb("#2D6A4F"); // Correct answer green
        }

        if (_selectedOptionIndex == optionIndex && !_isCorrect)
        {
            return Color.FromArgb("#A34032"); // Wrong answer red
        }

        return Color.FromArgb("#FFF9EF");
    }

    public Color GetOptionTextColor(int optionIndex)
    {
        if (!_isSubmitted)
        {
            return _selectedOptionIndex == optionIndex ? Color.FromArgb("#1C1814") : Color.FromArgb("#342B23");
        }

        if (optionIndex == CurrentLesson.CorrectOptionIndex || (_selectedOptionIndex == optionIndex && !_isCorrect))
        {
            return Color.FromArgb("#FFF9EF");
        }

        return Color.FromArgb("#342B23");
    }

    private void OnSelectOption(int index)
    {
        if (_isSubmitted) return;
        SelectedOptionIndex = index;
        NotifyOptionColors();
    }

    private void OnSubmit()
    {
        if (!SelectedOptionIndex.HasValue)
        {
            FeedbackText = "Vui lòng chạm chọn một đáp án trước khi nộp bài!";
            return;
        }

        IsSubmitted = true;
        IsCorrect = SelectedOptionIndex.Value == CurrentLesson.CorrectOptionIndex;

        FeedbackText = IsCorrect
            ? "🎉 CHÍNH XÁC! Bạn đã trả lời đúng câu hỏi."
            : "❌ CHƯA ĐÚNG. Hãy đọc giải thích bên dưới để ghi nhớ.";

        NotifyOptionColors();
    }

    private void OnReset()
    {
        SelectedOptionIndex = null;
        IsSubmitted = false;
        IsCorrect = false;
        FeedbackText = "";
        NotifyOptionColors();
    }

    private void OnNextLesson()
    {
        if (CanGoNext)
        {
            _currentIndex++;
            OnReset();
            NotifyLesson();
        }
    }

    private void OnPrevLesson()
    {
        if (CanGoPrev)
        {
            _currentIndex--;
            OnReset();
            NotifyLesson();
        }
    }

    private void OnSelectModule(int moduleId)
    {
        var firstLesson = _lessons.FindIndex(l => l.ModuleId == moduleId);
        if (firstLesson >= 0)
        {
            _currentIndex = firstLesson;
            OnReset();
            NotifyLesson();
        }
    }

    private void NotifyOptionColors()
    {
        OnPropertyChanged(nameof(SelectedOptionIndex));
        OnPropertyChanged(nameof(IsSubmitted));
        OnPropertyChanged(nameof(IsCorrect));
        OnPropertyChanged(nameof(FeedbackText));
        // Force refresh collection
        OnPropertyChanged(nameof(Options));
    }

    private void NotifyLesson()
    {
        OnPropertyChanged(nameof(CurrentIndex));
        OnPropertyChanged(nameof(LessonNumber));
        OnPropertyChanged(nameof(CurrentLesson));
        OnPropertyChanged(nameof(ModuleTitle));
        OnPropertyChanged(nameof(LessonTitle));
        OnPropertyChanged(nameof(Content));
        OnPropertyChanged(nameof(Question));
        OnPropertyChanged(nameof(Explanation));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrev));
        OnPropertyChanged(nameof(ProgressText));
        NotifyOptionColors();
    }
}
