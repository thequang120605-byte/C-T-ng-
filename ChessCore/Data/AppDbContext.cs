using Microsoft.EntityFrameworkCore;
using ChessCore.Models;
using ChessCore.Enums;

namespace ChessCore.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Move> Moves { get; set; }
        public DbSet<Puzzle> Puzzles { get; set; }
        public DbSet<PuzzleProgress> PuzzleProgresses { get; set; }
        public DbSet<CourseModule> CourseModules { get; set; }
        public DbSet<CourseLesson> CourseLessons { get; set; }
        public DbSet<CourseQuizQuestion> CourseQuizQuestions { get; set; }
        public DbSet<UserCourseProgress> UserCourseProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PuzzleProgress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PuzzleKey).IsRequired().HasMaxLength(40);
                entity.HasIndex(e => new { e.UserId, e.PuzzleKey }).IsUnique();
            });

            modelBuilder.Entity<CourseModule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Icon).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.DisplayOrder).IsUnique();
            });

            modelBuilder.Entity<CourseLesson>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Content).IsRequired().HasColumnType("longtext");
                entity.Property(e => e.LessonType).IsRequired().HasMaxLength(30);
                entity.Property(e => e.BoardPosition).HasColumnType("longtext");
                entity.HasIndex(e => new { e.ModuleId, e.DisplayOrder }).IsUnique();
                entity.HasOne(e => e.Module)
                    .WithMany(e => e.Lessons)
                    .HasForeignKey(e => e.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CourseQuizQuestion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Question).IsRequired().HasMaxLength(500);
                entity.Property(e => e.OptionsJson).IsRequired().HasColumnType("longtext");
                entity.Property(e => e.Explanation).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => new { e.LessonId, e.DisplayOrder }).IsUnique();
                entity.HasOne(e => e.Lesson)
                    .WithMany(e => e.QuizQuestions)
                    .HasForeignKey(e => e.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserCourseProgress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.LessonId }).IsUnique();
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Lesson)
                    .WithMany(e => e.Progresses)
                    .HasForeignKey(e => e.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình bảng User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.PlayerId).HasMaxLength(20);
                
                // Đảm bảo PlayerId là duy nhất
                entity.HasIndex(e => e.PlayerId).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Cấu hình bảng Game
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RoomCode).IsRequired().HasMaxLength(32);
                entity.HasIndex(e => e.RoomCode).IsUnique();
                entity.Property(e => e.Status)
                    .HasConversion<string>() // Lưu enum dạng string cho dễ đọc trong DB MySql
                    .HasMaxLength(30);
                entity.Property(e => e.CurrentTurn)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                // Quan hệ Game -> User (RedPlayer)
                entity.HasOne(g => g.RedPlayer)
                    .WithMany()
                    .HasForeignKey(g => g.RedPlayerId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Quan hệ Game -> User (BlackPlayer)
                entity.HasOne(g => g.BlackPlayer)
                    .WithMany()
                    .HasForeignKey(g => g.BlackPlayerId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Quan hệ 1-N: Game -> Moves
                entity.HasMany(g => g.Moves)
                    .WithOne()
                    .HasForeignKey(m => m.GameId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình bảng Move
            modelBuilder.Entity<Move>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PieceType)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                entity.Property(e => e.PieceColor)
                    .HasConversion<string>()
                    .HasMaxLength(20);
            });

            // Data Cờ Thế (Puzzles)
            modelBuilder.Entity<Puzzle>().HasData(
                new Puzzle { Id = 1, Level = 1, Title = "Tuyệt sát 1", Description = "Chiếu bí trong 1 nước (Xe cắm đáy)", Fen = "4k4/9/9/9/9/9/9/9/9/4R4 r - - 0 1", MateIn = 1, Difficulty = 1 },
                new Puzzle { Id = 2, Level = 2, Title = "Pháo lồng", Description = "Song pháo sát cục", Fen = "4k4/4a4/4a4/9/9/9/9/9/9/4C4 r - - 0 1", MateIn = 1, Difficulty = 1 },
                new Puzzle { Id = 3, Level = 3, Title = "Lưỡng chiếu", Description = "Kết hợp Xe Pháo", Fen = "4k4/9/9/9/9/9/9/9/9/4R4 r - - 0 1", MateIn = 1, Difficulty = 2 }
            );

            modelBuilder.Entity<CourseModule>().HasData(
                new CourseModule { Id = 1, Title = "Vỡ lòng & Làm quen", Description = "Làm quen với bàn cờ, cung, sông và các quân cờ.", Icon = "fa-seedling", DisplayOrder = 1 },
                new CourseModule { Id = 2, Title = "Nhận biết quân & Nước đi", Description = "Học từng quân riêng lẻ, sau đó luyện phối hợp 2–3 quân trên thế khai cuộc chuẩn.", Icon = "fa-arrows-up-down-left-right", DisplayOrder = 2 },
                new CourseModule { Id = 3, Title = "Luật chơi & Nguyên tắc cơ bản", Description = "Hiểu chiếu tướng, chiếu hết, hòa cờ và cách ghi biên bản.", Icon = "fa-scale-balanced", DisplayOrder = 3 },
                new CourseModule { Id = 4, Title = "Chiến thuật cơ bản & Trung cuộc", Description = "Phát triển quân, phối hợp lực lượng và đổi quân có lợi.", Icon = "fa-chess-knight", DisplayOrder = 4 }
            );

            modelBuilder.Entity<CourseLesson>().HasData(
                new CourseLesson { Id = 1, ModuleId = 1, Title = "Bàn cờ và các khu vực", Description = "Sông, cung tướng và đường đi trên bàn cờ.", Content = "Bàn cờ Tướng có 9 đường dọc và 10 đường ngang, ở giữa là con sông. Mỗi bên có một cung 3x3 để Tướng và Sĩ hoạt động.", LessonType = "board", DisplayOrder = 1 },
                new CourseLesson { Id = 2, ModuleId = 1, Title = "Nhận biết 7 loại quân", Description = "Tên gọi, mặt chữ và giá trị tương đối của các quân.", Content = "Bảy loại quân gồm Tướng, Sĩ, Tượng, Xe, Pháo, Mã và Binh/Chốt. Quân Đỏ và quân Đen có mặt chữ khác nhau nhưng cách đi tương ứng.", LessonType = "theory", DisplayOrder = 2 },
                new CourseLesson { Id = 3, ModuleId = 2, Title = "Xe: đi ngang và dọc", Description = "Nhận biết Xe và thực hành đường đi của Xe.", Content = "Xe đi ngang hoặc dọc không giới hạn, nhưng không được đi xuyên qua quân khác. Xe Đỏ bắt đầu ở hàng cuối, cột 0; Xe Đen bắt đầu ở hàng đầu, cột 0. Bạn điều khiển Xe Đỏ, còn Xe Đen là quân đối diện để quan sát và thực hành ăn quân.", LessonType = "board", DisplayOrder = 1 },
                new CourseLesson { Id = 4, ModuleId = 2, Title = "Pháo: đi và ăn qua ngòi", Description = "Phân biệt nước đi thường và nước ăn của Pháo.", Content = "Pháo đi như Xe khi không ăn. Khi ăn, Pháo phải nhảy qua đúng một quân làm ngòi.", LessonType = "board", DisplayOrder = 2 },
                new CourseLesson { Id = 5, ModuleId = 2, Title = "Mã: chữ L và cản chân", Description = "Thực hành tám hướng đi của Mã.", Content = "Mã đi theo hình chữ L. Nếu chân Mã bị chặn, hướng đó không đi được. Mã Đỏ bắt đầu ở hàng cuối, cột 1 và 7.", LessonType = "board", DisplayOrder = 3 },
                new CourseLesson { Id = 10, ModuleId = 2, Title = "Tướng: một bước trong cung", Description = "Học giới hạn cung và cách đi của Tướng.", Content = "Tướng đi ngang hoặc dọc một ô trong cung 3x3. Tướng Đỏ bắt đầu ở hàng 9, cột 4.", LessonType = "board", DisplayOrder = 4 },
                new CourseLesson { Id = 11, ModuleId = 2, Title = "Sĩ: đi chéo trong cung", Description = "Học bốn đường chéo hợp lệ của Sĩ.", Content = "Sĩ đi chéo một ô và chỉ hoạt động trong cung. Sĩ Đỏ bắt đầu ở hàng 9, cột 3 và 5.", LessonType = "board", DisplayOrder = 5 },
                new CourseLesson { Id = 12, ModuleId = 2, Title = "Tượng: chéo hai ô", Description = "Học mắt Tượng và giới hạn qua sông.", Content = "Tượng đi chéo hai ô, không được qua sông và không được đi khi mắt Tượng bị chặn.", LessonType = "board", DisplayOrder = 6 },
                new CourseLesson { Id = 13, ModuleId = 2, Title = "Binh: qua sông đổi cách đi", Description = "Thực hành Binh trước và sau khi qua sông.", Content = "Binh đi thẳng trước khi qua sông. Sau khi qua sông, Binh được đi ngang nhưng không đi lùi.", LessonType = "board", DisplayOrder = 7 },
                new CourseLesson { Id = 14, ModuleId = 2, Title = "Kết hợp Xe và Pháo", Description = "Ghép hai quân chủ lực trên thế khai cuộc.", Content = "Luyện phối hợp Xe và Pháo từ đúng vị trí khai cuộc của quân Đỏ.", LessonType = "board", DisplayOrder = 8 },
                new CourseLesson { Id = 15, ModuleId = 2, Title = "Kết hợp Xe, Pháo và Mã", Description = "Bài thực hành phối hợp ba quân.", Content = "Sau khi nắm từng quân, hãy phối hợp Xe, Pháo và Mã để mở đường và ăn quân.", LessonType = "board", DisplayOrder = 9 },
                new CourseLesson { Id = 6, ModuleId = 3, Title = "Chiếu tướng và chiếu hết", Description = "Nhận biết tình huống bị chiếu và kết thúc ván.", Content = "Chiếu là khi Tướng đang bị đe dọa bắt ở nước kế tiếp. Chiếu hết là khi bên bị chiếu không còn nước hợp lệ để thoát chiếu.", LessonType = "theory", DisplayOrder = 1 },
                new CourseLesson { Id = 7, ModuleId = 3, Title = "Hòa cờ và ghi biên bản", Description = "Bắt bí, các trường hợp hòa và ký hiệu nước đi.", Content = "Bắt bí là không còn nước hợp lệ nhưng Tướng không bị chiếu. Khi ghi biên bản, hãy ghi số nước, quân đi, hướng đi và ô đến theo quy ước của ván.", LessonType = "theory", DisplayOrder = 2 },
                new CourseLesson { Id = 8, ModuleId = 4, Title = "Nguyên tắc khai cuộc", Description = "Kiểm soát trung tâm và phát triển quân.", Content = "Ưu tiên đưa các quân hoạt động tốt, mở đường cho Xe, không di chuyển một quân quá nhiều lần và luôn chú ý an toàn của Tướng.", LessonType = "theory", DisplayOrder = 1 },
                new CourseLesson { Id = 9, ModuleId = 4, Title = "Phối hợp và đổi quân", Description = "Xây dựng kế hoạch trung cuộc vững chắc.", Content = "Xe, Pháo, Mã nên hỗ trợ lẫn nhau. Chỉ đổi quân khi có lợi về vị trí, vật chất hoặc làm giảm áp lực lên Tướng của bạn.", LessonType = "theory", DisplayOrder = 2 }
            );

            modelBuilder.Entity<CourseQuizQuestion>().HasData(
                new CourseQuizQuestion { Id = 1, LessonId = 1, Question = "Khu vực nào nằm giữa hai bên bàn cờ?", OptionsJson = "[\"Cung tướng\",\"Con sông\",\"Đường biên\"]", CorrectAnswer = 1, Explanation = "Con sông nằm giữa hai bên và chia bàn cờ thành hai nửa.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 2, LessonId = 2, Question = "Quân nào có thể đi ngang hoặc dọc không giới hạn?", OptionsJson = "[\"Mã\",\"Xe\",\"Tượng\"]", CorrectAnswer = 1, Explanation = "Xe đi thẳng ngang hoặc dọc trên các đường trống.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 3, LessonId = 3, Question = "Xe đi như thế nào?", OptionsJson = "[\"Chéo\",\"Ngang hoặc dọc\",\"Theo hình chữ L\"]", CorrectAnswer = 1, Explanation = "Xe đi ngang hoặc dọc trên các đường trống.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 4, LessonId = 4, Question = "Pháo cần gì khi ăn quân?", OptionsJson = "[\"Hai ngòi\",\"Đúng một ngòi\",\"Không cần ngòi\"]", CorrectAnswer = 1, Explanation = "Pháo phải nhảy qua đúng một quân làm ngòi để ăn.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 6, LessonId = 5, Question = "Mã bị cản bởi gì?", OptionsJson = "[\"Chân Mã\",\"Mắt Tượng\",\"Cung tướng\"]", CorrectAnswer = 0, Explanation = "Mã không đi được nếu chân Mã bị chặn.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 11, LessonId = 10, Question = "Tướng được đi ở đâu?", OptionsJson = "[\"Ngoài bàn cờ\",\"Trong cung 3x3\",\"Qua sông\"]", CorrectAnswer = 1, Explanation = "Tướng chỉ đi ngang hoặc dọc một ô trong cung.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 12, LessonId = 11, Question = "Sĩ đi như thế nào?", OptionsJson = "[\"Chéo một ô trong cung\",\"Ngang không giới hạn\",\"Chéo hai ô qua sông\"]", CorrectAnswer = 0, Explanation = "Sĩ đi chéo một ô và không ra khỏi cung.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 13, LessonId = 12, Question = "Tượng có được qua sông không?", OptionsJson = "[\"Có\",\"Không\",\"Chỉ khi chiếu tướng\"]", CorrectAnswer = 1, Explanation = "Tượng chỉ hoạt động ở phần sân của mình.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 14, LessonId = 13, Question = "Binh sau khi qua sông được đi thêm hướng nào?", OptionsJson = "[\"Đi lùi\",\"Đi ngang\",\"Đi chéo hai ô\"]", CorrectAnswer = 1, Explanation = "Sau khi qua sông, Binh được đi ngang nhưng không đi lùi.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 15, LessonId = 14, Question = "Khi phối hợp Xe và Pháo cần chú ý gì?", OptionsJson = "[\"Mở đường và tạo ngòi cho Pháo\",\"Đặt hai quân cùng một ô\",\"Cho Xe đi chéo\"]", CorrectAnswer = 0, Explanation = "Xe mở đường và quân làm ngòi giúp Pháo ăn đúng luật.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 16, LessonId = 15, Question = "Mục tiêu của bài phối hợp ba quân là gì?", OptionsJson = "[\"Cho các quân hỗ trợ lẫn nhau\",\"Chỉ di chuyển Tướng\",\"Không được ăn quân\"]", CorrectAnswer = 0, Explanation = "Xe, Pháo và Mã cần phối hợp thay vì hoạt động rời rạc.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 5, LessonId = 8, Question = "Đâu là nguyên tắc khai cuộc tốt?", OptionsJson = "[\"Phát triển quân và kiểm soát trung tâm\",\"Di chuyển một quân liên tục\",\"Bỏ qua an toàn của Tướng\"]", CorrectAnswer = 0, Explanation = "Phát triển quân và kiểm soát trung tâm giúp các quân phối hợp tốt hơn.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 7, LessonId = 6, Question = "Chiếu hết nghĩa là gì?", OptionsJson = "[\"Tướng bị đe dọa nhưng còn đường thoát\",\"Tướng không bị đe dọa\",\"Bị chiếu và không còn nước hợp lệ để thoát\"]", CorrectAnswer = 2, Explanation = "Chiếu hết kết thúc ván vì bên bị chiếu không thể hóa giải.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 8, LessonId = 7, Question = "Bắt bí là trường hợp nào?", OptionsJson = "[\"Không còn nước hợp lệ nhưng không bị chiếu\",\"Bị chiếu hết\",\"Ăn được quân Xe\"]", CorrectAnswer = 0, Explanation = "Bắt bí là hòa cờ: không còn nước đi nhưng Tướng không bị chiếu.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 9, LessonId = 9, Question = "Khi nào nên đổi quân?", OptionsJson = "[\"Luôn luôn đổi quân\",\"Khi đổi quân có lợi về vị trí hoặc vật chất\",\"Không bao giờ đổi quân\"]", CorrectAnswer = 1, Explanation = "Đổi quân cần phục vụ kế hoạch và đem lại lợi thế.", DisplayOrder = 1 }
            );
        }
    }
}
