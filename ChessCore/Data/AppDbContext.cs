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
        public DbSet<GameAnalysis> GameAnalyses { get; set; }
        public DbSet<UserEloHistory> UserEloHistories { get; set; }
        public DbSet<Opening> Openings { get; set; }
        public DbSet<OpeningBookMove> OpeningBookMoves { get; set; }
        public DbSet<MasterGame> MasterGames { get; set; }

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

                entity.Property(e => e.GameMode)
                    .HasMaxLength(20)
                    .HasDefaultValue("PvP");
                entity.HasOne(g => g.Opening)
                    .WithMany()
                    .HasForeignKey(g => g.OpeningId)
                    .OnDelete(DeleteBehavior.SetNull);

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
                entity.Property(e => e.Judgment)
                    .HasMaxLength(30);
                entity.Property(e => e.BestAlternative)
                    .HasMaxLength(50);
            });

            // Cấu hình bảng GameAnalysis
            modelBuilder.Entity<GameAnalysis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.GameId).IsUnique();
                entity.Property(e => e.SummaryReviewJson).HasColumnType("longtext");
                entity.HasOne(e => e.Game)
                    .WithOne(g => g.Analysis)
                    .HasForeignKey<GameAnalysis>(e => e.GameId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình bảng UserEloHistory
            modelBuilder.Entity<UserEloHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Reason).HasMaxLength(150);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.EloHistories)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Game)
                    .WithMany()
                    .HasForeignKey(e => e.GameId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Cấu hình bảng Opening
            modelBuilder.Entity<Opening>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.StartingFen).IsRequired().HasMaxLength(255);
                entity.HasMany(e => e.BookMoves)
                    .WithOne(m => m.Opening)
                    .HasForeignKey(m => m.OpeningId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Cấu hình bảng OpeningBookMove
            modelBuilder.Entity<OpeningBookMove>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Fen).IsRequired().HasMaxLength(255);
                entity.Property(e => e.MoveNotation).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Explanation).HasMaxLength(500);
            });

            // Cấu hình bảng MasterGame
            modelBuilder.Entity<MasterGame>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.RedPlayer).IsRequired().HasMaxLength(100);
                entity.Property(e => e.BlackPlayer).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Event).HasMaxLength(150);
                entity.Property(e => e.Result).IsRequired().HasMaxLength(20);
                entity.Property(e => e.OpeningName).HasMaxLength(150);
                entity.Property(e => e.InitialFen).HasMaxLength(255);
                entity.Property(e => e.PgnContent).HasColumnType("longtext");
                entity.Property(e => e.MovesJson).HasColumnType("longtext");
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
                new CourseLesson { Id = 16, ModuleId = 2, Title = "Tổng hợp 1: Nhận diện và luật đi", Description = "Ôn tập đầy đủ 7 loại quân trên bàn cờ khai cuộc.", Content = "Bài tổng hợp 1 dùng bàn cờ đầy đủ 32 quân để kiểm tra ký hiệu, vị trí ban đầu và hướng di chuyển cơ bản của từng loại quân.", LessonType = "board", DisplayOrder = 10 },
                new CourseLesson { Id = 17, ModuleId = 2, Title = "Tổng hợp 2: Cản đường và ăn quân", Description = "Ôn tập đầy đủ vật cản, ngòi Pháo, chân Mã và luật ăn quân.", Content = "Bài tổng hợp 2 dùng bàn cờ đầy đủ 32 quân để kiểm tra vật cản, cách ăn quân và các giới hạn đặc biệt của từng quân.", LessonType = "board", DisplayOrder = 11 },
                new CourseLesson { Id = 18, ModuleId = 2, Title = "Tổng hợp 3: Phối hợp thực chiến", Description = "Ôn tập toàn bộ quân cờ và cách phối hợp trong thế khai cuộc.", Content = "Bài tổng hợp 3 dùng bàn cờ đầy đủ 32 quân để kiểm tra phối hợp lực lượng, an toàn Tướng và các quy tắc thực chiến.", LessonType = "board", DisplayOrder = 12 },
                new CourseLesson { Id = 19, ModuleId = 2, Title = "Tổng kết Module 2: Bàn cờ toàn diện", Description = "Thực hành đầy đủ 32 quân và kiểm tra toàn bộ luật di chuyển.", Content = "Bài tổng kết mở sau khi hoàn thành ba bài tổng hợp. Bàn cờ hiển thị đầy đủ 32 quân ở vị trí khai cuộc; bạn có thể chọn quân Đỏ để xem và thực hành các nước đi hợp lệ của Xe, Pháo, Mã, Tượng, Sĩ, Tướng và Binh.", LessonType = "board", DisplayOrder = 13 },
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
                new CourseQuizQuestion { Id = 17, LessonId = 16, Question = "Bàn cờ Tướng có bao nhiêu đường dọc?", OptionsJson = "[\"8\",\"9\",\"10\"]", CorrectAnswer = 1, Explanation = "Bàn cờ có 9 đường dọc.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 18, LessonId = 16, Question = "Quân nào đi chữ L?", OptionsJson = "[\"Mã\",\"Tượng\",\"Sĩ\"]", CorrectAnswer = 0, Explanation = "Mã đi theo hình chữ L.", DisplayOrder = 2 },
                new CourseQuizQuestion { Id = 19, LessonId = 16, Question = "Tướng Đỏ bắt đầu ở đâu?", OptionsJson = "[\"Hàng 1 cột 4\",\"Hàng 10 cột 5\",\"Hàng 10 cột 4\"]", CorrectAnswer = 2, Explanation = "Tướng Đỏ ở hàng cuối, cột giữa.", DisplayOrder = 3 },
                new CourseQuizQuestion { Id = 20, LessonId = 16, Question = "Sĩ di chuyển như thế nào?", OptionsJson = "[\"Chéo 1 ô trong cung\",\"Ngang 2 ô\",\"Dọc không giới hạn\"]", CorrectAnswer = 0, Explanation = "Sĩ đi chéo một ô trong cung.", DisplayOrder = 4 },
                new CourseQuizQuestion { Id = 21, LessonId = 16, Question = "Tượng có được qua sông không?", OptionsJson = "[\"Có\",\"Không\",\"Chỉ khi ăn quân\"]", CorrectAnswer = 1, Explanation = "Tượng không được qua sông.", DisplayOrder = 5 },
                new CourseQuizQuestion { Id = 22, LessonId = 16, Question = "Binh sau khi qua sông được đi ngang không?", OptionsJson = "[\"Có\",\"Không\",\"Chỉ đi chéo\"]", CorrectAnswer = 0, Explanation = "Binh được đi ngang sau khi qua sông.", DisplayOrder = 6 },
                new CourseQuizQuestion { Id = 23, LessonId = 16, Question = "Quân nào đi ngang dọc không giới hạn?", OptionsJson = "[\"Xe\",\"Mã\",\"Tốt\"]", CorrectAnswer = 0, Explanation = "Xe đi ngang hoặc dọc trên đường trống.", DisplayOrder = 7 },
                new CourseQuizQuestion { Id = 24, LessonId = 17, Question = "Xe có được đi xuyên qua quân khác không?", OptionsJson = "[\"Có\",\"Không\",\"Chỉ khi chiếu\"]", CorrectAnswer = 1, Explanation = "Xe bị chặn bởi quân trên đường đi.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 25, LessonId = 17, Question = "Pháo ăn quân cần bao nhiêu ngòi?", OptionsJson = "[\"Không cần\",\"Đúng một\",\"Hai hoặc hơn\"]", CorrectAnswer = 1, Explanation = "Pháo phải có đúng một quân làm ngòi.", DisplayOrder = 2 },
                new CourseQuizQuestion { Id = 26, LessonId = 17, Question = "Mã bị chặn bởi bộ phận nào?", OptionsJson = "[\"Chân Mã\",\"Mắt Tượng\",\"Cung\"]", CorrectAnswer = 0, Explanation = "Quân đứng ở chân Mã chặn hướng đi đó.", DisplayOrder = 3 },
                new CourseQuizQuestion { Id = 27, LessonId = 17, Question = "Tượng bị chặn bởi gì?", OptionsJson = "[\"Mắt Tượng\",\"Chân Mã\",\"Ngòi\"]", CorrectAnswer = 0, Explanation = "Quân ở điểm giữa đường chéo chặn mắt Tượng.", DisplayOrder = 4 },
                new CourseQuizQuestion { Id = 28, LessonId = 17, Question = "Quân cùng màu ở ô đích xử lý thế nào?", OptionsJson = "[\"Có thể ăn\",\"Không được đi vào\",\"Được nhảy qua\"]", CorrectAnswer = 1, Explanation = "Không thể đi vào ô đang có quân cùng phe.", DisplayOrder = 5 },
                new CourseQuizQuestion { Id = 29, LessonId = 17, Question = "Binh có được đi lùi không?", OptionsJson = "[\"Có\",\"Không\",\"Chỉ sau khi qua sông\"]", CorrectAnswer = 1, Explanation = "Binh không bao giờ được đi lùi.", DisplayOrder = 6 },
                new CourseQuizQuestion { Id = 30, LessonId = 17, Question = "Sĩ có được ra khỏi cung không?", OptionsJson = "[\"Có\",\"Không\",\"Chỉ khi ăn quân\"]", CorrectAnswer = 1, Explanation = "Sĩ chỉ hoạt động trong cung.", DisplayOrder = 7 },
                new CourseQuizQuestion { Id = 31, LessonId = 17, Question = "Quân nào có thể ăn theo đường thẳng nhưng cần ngòi?", OptionsJson = "[\"Pháo\",\"Mã\",\"Tượng\"]", CorrectAnswer = 0, Explanation = "Pháo cần đúng một ngòi khi ăn.", DisplayOrder = 8 },
                new CourseQuizQuestion { Id = 32, LessonId = 18, Question = "Phối hợp Xe và Pháo cần tạo điều kiện gì?", OptionsJson = "[\"Đường đi và ngòi Pháo\",\"Hai quân cùng ô\",\"Không có quân chắn nào\"]", CorrectAnswer = 0, Explanation = "Xe mở đường và quân làm ngòi hỗ trợ Pháo.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 33, LessonId = 18, Question = "Mã hỗ trợ tốt nhất khi nào?", OptionsJson = "[\"Khi không bị cản chân\",\"Khi ở ngoài bàn\",\"Khi bị chặn\"]", CorrectAnswer = 0, Explanation = "Mã cần ô chân thông thoáng.", DisplayOrder = 2 },
                new CourseQuizQuestion { Id = 34, LessonId = 18, Question = "Hai Tướng cùng cột cần điều kiện gì?", OptionsJson = "[\"Có quân chắn giữa\",\"Không có quân chắn\",\"Được nhìn nhau\"]", CorrectAnswer = 0, Explanation = "Hai Tướng không được đối mặt trực tiếp.", DisplayOrder = 3 },
                new CourseQuizQuestion { Id = 35, LessonId = 18, Question = "Mục tiêu của phối hợp quân là gì?", OptionsJson = "[\"Tạo ưu thế và hỗ trợ nhau\",\"Di chuyển một quân mãi\",\"Bỏ qua Tướng\"]", CorrectAnswer = 0, Explanation = "Các quân phối hợp để tạo ưu thế.", DisplayOrder = 4 },
                new CourseQuizQuestion { Id = 36, LessonId = 18, Question = "Khi ăn quân, quân bị ăn sẽ ở đâu?", OptionsJson = "[\"Vẫn ở ô cũ\",\"Bị loại khỏi bàn\",\"Chuyển sang bên kia\"]", CorrectAnswer = 1, Explanation = "Quân bị ăn được loại khỏi trạng thái bàn cờ.", DisplayOrder = 5 },
                new CourseQuizQuestion { Id = 37, LessonId = 18, Question = "Vị trí khai cuộc của Pháo Đỏ là gì?", OptionsJson = "[\"Hàng 8 cột 2 và 8\",\"Hàng 10 cột 2 và 8\",\"Hàng 7 cột 1 và 7\"]", CorrectAnswer = 0, Explanation = "Pháo Đỏ bắt đầu ở hàng áp chót, cột 1 và 7 theo chỉ số 0.", DisplayOrder = 6 },
                new CourseQuizQuestion { Id = 38, LessonId = 18, Question = "Quân nào không được qua sông?", OptionsJson = "[\"Tượng\",\"Xe\",\"Mã\"]", CorrectAnswer = 0, Explanation = "Tượng chỉ hoạt động ở nửa bàn của mình.", DisplayOrder = 7 },
                new CourseQuizQuestion { Id = 39, LessonId = 18, Question = "Nước đi làm lộ mặt Tướng là gì?", OptionsJson = "[\"Hợp lệ\",\"Không hợp lệ\",\"Luôn là chiếu hết\"]", CorrectAnswer = 1, Explanation = "Không được để hai Tướng đối mặt cùng cột.", DisplayOrder = 8 },
                new CourseQuizQuestion { Id = 56, LessonId = 16, Question = "Quân nào bảo vệ Tướng trong cung bằng nước đi chéo?", OptionsJson = "[\"Sĩ\",\"Xe\",\"Pháo\"]", CorrectAnswer = 0, Explanation = "Sĩ đi chéo một ô trong cung tướng.", DisplayOrder = 8 },
                new CourseQuizQuestion { Id = 57, LessonId = 16, Question = "Quân nào chỉ đi chéo hai ô và không qua sông?", OptionsJson = "[\"Tượng\",\"Mã\",\"Binh\"]", CorrectAnswer = 0, Explanation = "Tượng đi chéo hai ô và chỉ hoạt động ở phần sân của mình.", DisplayOrder = 9 },
                new CourseQuizQuestion { Id = 58, LessonId = 16, Question = "Quân nào đi thẳng trước khi qua sông?", OptionsJson = "[\"Binh\",\"Tướng\",\"Sĩ\"]", CorrectAnswer = 0, Explanation = "Binh đi thẳng một ô trước khi qua sông.", DisplayOrder = 10 },
                new CourseQuizQuestion { Id = 59, LessonId = 16, Question = "Ký hiệu nào là Tướng Đỏ?", OptionsJson = "[\"帥\",\"將\",\"仕\"]", CorrectAnswer = 0, Explanation = "帥 là ký hiệu Tướng Đỏ; 將 là ký hiệu Tướng Đen.", DisplayOrder = 11 },
                new CourseQuizQuestion { Id = 60, LessonId = 17, Question = "Quân nào không được nhảy qua quân chắn khi đi thẳng?", OptionsJson = "[\"Xe\",\"Mã\",\"Pháo khi ăn\"]", CorrectAnswer = 0, Explanation = "Xe không được đi xuyên qua bất kỳ quân nào.", DisplayOrder = 9 },
                new CourseQuizQuestion { Id = 61, LessonId = 17, Question = "Mắt Tượng bị chặn thì điều gì xảy ra?", OptionsJson = "[\"Nước đi chéo đó không hợp lệ\",\"Tượng được nhảy qua\",\"Tượng đổi thành Mã\"]", CorrectAnswer = 0, Explanation = "Quân ở điểm giữa đường chéo chặn hướng đi của Tượng.", DisplayOrder = 10 },
                new CourseQuizQuestion { Id = 62, LessonId = 17, Question = "Binh sau khi qua sông được đi ngang mấy ô mỗi nước?", OptionsJson = "[\"Một ô\",\"Hai ô\",\"Không giới hạn\"]", CorrectAnswer = 0, Explanation = "Binh chỉ được đi ngang một ô mỗi nước.", DisplayOrder = 11 },
                new CourseQuizQuestion { Id = 63, LessonId = 17, Question = "Quân cùng màu ở ô đích có được ăn không?", OptionsJson = "[\"Không\",\"Có\",\"Chỉ khi là Tướng\"]", CorrectAnswer = 0, Explanation = "Quân cùng màu không thể ăn nhau và ô đó không phải ô đích hợp lệ.", DisplayOrder = 12 },
                new CourseQuizQuestion { Id = 64, LessonId = 18, Question = "Khi phối hợp Xe và Pháo, quân nào có thể tạo ngòi cho Pháo?", OptionsJson = "[\"Một quân đứng giữa Pháo và mục tiêu\",\"Quân ở ngoài bàn\",\"Không cần quân nào\"]", CorrectAnswer = 0, Explanation = "Pháo cần đúng một quân làm ngòi giữa nó và quân bị ăn.", DisplayOrder = 9 },
                new CourseQuizQuestion { Id = 65, LessonId = 18, Question = "Nước đi của quân Đỏ phải bảo vệ điều gì?", OptionsJson = "[\"Tướng Đỏ không bị lộ mặt hoặc tự chiếu\",\"Chỉ quân Xe\",\"Ô ngoài bàn cờ\"]", CorrectAnswer = 0, Explanation = "Không được thực hiện nước đi làm Tướng Đỏ bị lộ mặt hoặc rơi vào thế nguy hiểm.", DisplayOrder = 10 },
                new CourseQuizQuestion { Id = 66, LessonId = 18, Question = "Quân nào có thể hỗ trợ kiểm soát nhiều đường thẳng?", OptionsJson = "[\"Xe\",\"Sĩ\",\"Tượng\"]", CorrectAnswer = 0, Explanation = "Xe kiểm soát các đường ngang và dọc khi không bị cản.", DisplayOrder = 11 },
                new CourseQuizQuestion { Id = 67, LessonId = 18, Question = "Khi một quân bị ăn, vị trí của quân đó được xử lý thế nào?", OptionsJson = "[\"Xóa khỏi bàn cờ\",\"Giữ lại dưới quân ăn\",\"Đổi màu\"]", CorrectAnswer = 0, Explanation = "Quân bị ăn bị loại khỏi trạng thái bàn cờ.", DisplayOrder = 12 },
                new CourseQuizQuestion { Id = 40, LessonId = 19, Question = "Bàn cờ khai cuộc có bao nhiêu quân?", OptionsJson = "[\"16\",\"32\",\"36\"]", CorrectAnswer = 1, Explanation = "Mỗi bên có 16 quân, cả bàn cờ có 32 quân.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 41, LessonId = 19, Question = "Xe đi như thế nào?", OptionsJson = "[\"Ngang hoặc dọc, không xuyên quân\",\"Chéo hai ô\",\"Theo hình chữ L\"]", CorrectAnswer = 0, Explanation = "Xe đi ngang hoặc dọc trên các đường trống.", DisplayOrder = 2 },
                new CourseQuizQuestion { Id = 42, LessonId = 19, Question = "Pháo đi thường như thế nào?", OptionsJson = "[\"Như Xe khi không ăn\",\"Chéo một ô\",\"Nhảy qua hai quân\"]", CorrectAnswer = 0, Explanation = "Pháo đi như Xe khi di chuyển đến ô trống.", DisplayOrder = 3 },
                new CourseQuizQuestion { Id = 43, LessonId = 19, Question = "Pháo cần bao nhiêu ngòi để ăn quân?", OptionsJson = "[\"Không có\",\"Đúng một\",\"Ít nhất hai\"]", CorrectAnswer = 1, Explanation = "Pháo phải nhảy qua đúng một quân làm ngòi.", DisplayOrder = 4 },
                new CourseQuizQuestion { Id = 44, LessonId = 19, Question = "Mã di chuyển theo hình nào?", OptionsJson = "[\"Chữ L\",\"Đường thẳng\",\"Đường chéo một ô\"]", CorrectAnswer = 0, Explanation = "Mã đi hai ô theo một hướng rồi một ô vuông góc.", DisplayOrder = 5 },
                new CourseQuizQuestion { Id = 45, LessonId = 19, Question = "Điều gì làm Mã không đi được một hướng?", OptionsJson = "[\"Bị cản chân Mã\",\"Có sông trên bàn\",\"Đứng trong cung\"]", CorrectAnswer = 0, Explanation = "Quân ở ô chân Mã sẽ chặn hướng đi đó.", DisplayOrder = 6 },
                new CourseQuizQuestion { Id = 46, LessonId = 19, Question = "Tượng đi bao nhiêu ô mỗi nước?", OptionsJson = "[\"Một ô ngang\",\"Hai ô chéo\",\"Ba ô dọc\"]", CorrectAnswer = 1, Explanation = "Tượng đi chéo đúng hai ô và không được qua sông.", DisplayOrder = 7 },
                new CourseQuizQuestion { Id = 47, LessonId = 19, Question = "Điểm giữa đường chéo của Tượng được gọi là gì?", OptionsJson = "[\"Mắt Tượng\",\"Ngòi Pháo\",\"Cung tướng\"]", CorrectAnswer = 0, Explanation = "Nếu mắt Tượng bị chặn thì hướng đó không hợp lệ.", DisplayOrder = 8 },
                new CourseQuizQuestion { Id = 48, LessonId = 19, Question = "Sĩ được đi ở đâu?", OptionsJson = "[\"Trong cung, chéo một ô\",\"Qua sông, ngang hai ô\",\"Toàn bàn, dọc không giới hạn\"]", CorrectAnswer = 0, Explanation = "Sĩ chỉ đi chéo một ô trong cung tướng.", DisplayOrder = 9 },
                new CourseQuizQuestion { Id = 49, LessonId = 19, Question = "Tướng được đi như thế nào?", OptionsJson = "[\"Ngang hoặc dọc một ô trong cung\",\"Chéo hai ô toàn bàn\",\"Theo hình chữ L\"]", CorrectAnswer = 0, Explanation = "Tướng chỉ đi ngang hoặc dọc một ô trong cung 3x3.", DisplayOrder = 10 },
                new CourseQuizQuestion { Id = 50, LessonId = 19, Question = "Binh trước khi qua sông được đi thế nào?", OptionsJson = "[\"Đi thẳng một ô\",\"Đi lùi một ô\",\"Đi chéo hai ô\"]", CorrectAnswer = 0, Explanation = "Binh chỉ đi thẳng về phía trước trước khi qua sông.", DisplayOrder = 11 },
                new CourseQuizQuestion { Id = 51, LessonId = 19, Question = "Binh sau khi qua sông được thêm nước đi nào?", OptionsJson = "[\"Đi ngang một ô\",\"Đi lùi một ô\",\"Nhảy qua quân\"]", CorrectAnswer = 0, Explanation = "Sau khi qua sông, Binh được đi ngang nhưng không được đi lùi.", DisplayOrder = 12 },
                new CourseQuizQuestion { Id = 52, LessonId = 19, Question = "Quân Đỏ có thể đi vào ô đang có quân Đỏ không?", OptionsJson = "[\"Có\",\"Không\",\"Chỉ khi chiếu tướng\"]", CorrectAnswer = 1, Explanation = "Quân cùng màu chiếm ô đích sẽ chặn nước đi.", DisplayOrder = 13 },
                new CourseQuizQuestion { Id = 53, LessonId = 19, Question = "Khi quân Đỏ ăn quân Đen, quân Đen sẽ thế nào?", OptionsJson = "[\"Bị loại khỏi bàn\",\"Đổi thành quân Đỏ\",\"Đứng chồng lên quân Đỏ\"]", CorrectAnswer = 0, Explanation = "Quân bị ăn được loại khỏi trạng thái bàn cờ.", DisplayOrder = 14 },
                new CourseQuizQuestion { Id = 54, LessonId = 19, Question = "Hai Tướng cùng cột phải có gì ở giữa?", OptionsJson = "[\"Ít nhất một quân chắn\",\"Không có quân nào\",\"Một ô trống duy nhất\"]", CorrectAnswer = 0, Explanation = "Hai Tướng không được đối mặt trực tiếp; phải có quân chắn giữa.", DisplayOrder = 15 },
                new CourseQuizQuestion { Id = 55, LessonId = 19, Question = "Khi quân Đen bị ăn hết trong bài thực hành, điều gì xảy ra?", OptionsJson = "[\"Hiển thị thông báo kết thúc\",\"Tự động quay về Module 1\",\"Cho quân Đen xuất hiện lại\"]", CorrectAnswer = 0, Explanation = "Bàn cờ thông báo hoàn thành và khóa tương tác khi không còn quân Đen.", DisplayOrder = 16 },
                new CourseQuizQuestion { Id = 5, LessonId = 8, Question = "Đâu là nguyên tắc khai cuộc tốt?", OptionsJson = "[\"Phát triển quân và kiểm soát trung tâm\",\"Di chuyển một quân liên tục\",\"Bỏ qua an toàn của Tướng\"]", CorrectAnswer = 0, Explanation = "Phát triển quân và kiểm soát trung tâm giúp các quân phối hợp tốt hơn.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 7, LessonId = 6, Question = "Chiếu hết nghĩa là gì?", OptionsJson = "[\"Tướng bị đe dọa nhưng còn đường thoát\",\"Tướng không bị đe dọa\",\"Bị chiếu và không còn nước hợp lệ để thoát\"]", CorrectAnswer = 2, Explanation = "Chiếu hết kết thúc ván vì bên bị chiếu không thể hóa giải.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 8, LessonId = 7, Question = "Bắt bí là trường hợp nào?", OptionsJson = "[\"Không còn nước hợp lệ nhưng không bị chiếu\",\"Bị chiếu hết\",\"Ăn được quân Xe\"]", CorrectAnswer = 0, Explanation = "Bắt bí là hòa cờ: không còn nước đi nhưng Tướng không bị chiếu.", DisplayOrder = 1 },
                new CourseQuizQuestion { Id = 9, LessonId = 9, Question = "Khi nào nên đổi quân?", OptionsJson = "[\"Luôn luôn đổi quân\",\"Khi đổi quân có lợi về vị trí hoặc vật chất\",\"Không bao giờ đổi quân\"]", CorrectAnswer = 1, Explanation = "Đổi quân cần phục vụ kế hoạch và đem lại lợi thế.", DisplayOrder = 1 }
            );

            // Seed dữ liệu Khai cuộc (Openings) chuẩn theo đề cương
            modelBuilder.Entity<Opening>().HasData(
                new Opening { Id = 1, Code = "ECCO-01", Name = "Pháo Đầu (Trung Pháo)", Description = "Thế trận công thủ toàn diện, Pháo vào trung lộ kiểm soát lộ 5, tạo sức ép thẳng vào cung Tướng.", StartingFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" },
                new Opening { Id = 2, Code = "ECCO-02", Name = "Bình Phong Mã", Description = "Trận hình phòng thủ kinh điển chống lại Pháo Đầu, hai Mã nhảy lên giữ chặt trung lộ và bảo vệ Tốt đầu vững chắc.", StartingFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" },
                new Opening { Id = 3, Code = "ECCO-03", Name = "Thuận Pháo", Description = "Hai bên cùng đưa Pháo vào cùng một lộ trung tâm, thế cờ đối công kịch liệt, ăn miếng trả miếng.", StartingFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" },
                new Opening { Id = 4, Code = "ECCO-04", Name = "Tiên Nhân Chỉ Lộ", Description = "Bên Đỏ khai cuộc bằng nước Tốt 7 tiến 1 hoặc Tốt 3 tiến 1 để thăm dò ý đồ chiến thuật của đối phương.", StartingFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" },
                new Opening { Id = 5, Code = "ECCO-05", Name = "Trận Thượng Tiên (Khởi Mã Cuộc)", Description = "Khai cuộc bằng nước phát triển Mã sớm, cấu trúc phòng thủ kiên cố, linh hoạt chuyển biến.", StartingFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" }
            );

            // Seed dữ liệu nước đi Khai cuộc (OpeningBookMoves)
            modelBuilder.Entity<OpeningBookMove>().HasData(
                new OpeningBookMove { Id = 1, OpeningId = 1, Fen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1", MoveNotation = "Pháo 2 bình 5 (C2.5)", MoveOrder = 1, WinCount = 12500, DrawCount = 8900, LossCount = 7600, WinRate = 58.4, Explanation = "Pháo vào giữa kiểm soát lộ trung tâm, sẵn sàng bắn Tốt đầu uy hiếp Tướng." },
                new OpeningBookMove { Id = 2, OpeningId = 1, Fen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1", MoveNotation = "Mã 8 tiến 7 (M8.7)", MoveOrder = 2, WinCount = 9800, DrawCount = 7200, LossCount = 6500, WinRate = 52.1, Explanation = "Bên Đen nhảy Mã bảo vệ Tốt đầu, hình thành thế trận Bình Phong Mã đối kháng." },
                new OpeningBookMove { Id = 3, OpeningId = 2, Fen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1", MoveNotation = "Mã 2 tiến 3 (M2.3)", MoveOrder = 2, WinCount = 8400, DrawCount = 6100, LossCount = 5900, WinRate = 51.5, Explanation = "Mã cánh phải phát triển kiểm soát trung lộ và bảo vệ Tốt đầu." },
                new OpeningBookMove { Id = 4, OpeningId = 3, Fen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1", MoveNotation = "Pháo 8 bình 5 (C8.5)", MoveOrder = 2, WinCount = 7100, DrawCount = 4500, LossCount = 6900, WinRate = 49.8, Explanation = "Đen cũng đưa Pháo vào giữa, tạo thế Thuận Pháo đối công trực diện." },
                new OpeningBookMove { Id = 5, OpeningId = 4, Fen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1", MoveNotation = "Binh 7 tiến 1 (P7.1)", MoveOrder = 1, WinCount = 11200, DrawCount = 9400, LossCount = 7800, WinRate = 54.2, Explanation = "Tiến Tốt mở đường cho Mã phát triển và thăm dò phản ứng của đối phương." }
            );

            // Seed dữ liệu ván đấu chuyên nghiệp mẫu (MasterGames)
            modelBuilder.Entity<MasterGame>().HasData(
                new MasterGame
                {
                    Id = 1,
                    Title = "Hồ Vinh Hoa vs Lữ Khâm - Chung kết Toàn quốc 1988",
                    RedPlayer = "Hồ Vinh Hoa",
                    BlackPlayer = "Lữ Khâm",
                    Event = "Giải Vô địch Cá nhân Cờ Tướng Trung Quốc",
                    EventDate = new System.DateTime(1988, 11, 20),
                    Result = "1-0",
                    OpeningName = "Thuận Pháo Hoành Xe đối Trực Xe",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"Giap Cap Lien Tai\"]\n[Site \"Quang Chau\"]\n[Date \"1988.11.20\"]\n[Round \"11\"]\n[Red \"Ho Vinh Hoa\"]\n[Black \"Lu Kham\"]\n[Result \"1-0\"]\n1. C2.5 C8.5 2. H2.3 H8.7 3. R1.2 R9.1 4. H8.7 H2.3",
                    MovesJson = "[]",
                    CreatedAt = new System.DateTime(2026, 10, 15, 6, 28, 4, 359, System.DateTimeKind.Utc).AddTicks(1051)
                },
                new MasterGame
                {
                    Id = 2,
                    Title = "Lại Lý Huynh vs Uông Dương Bắc - Trạng Cờ Đất Việt 2017",
                    RedPlayer = "Lại Lý Huynh",
                    BlackPlayer = "Uông Dương Bắc",
                    Event = "Trạng Cờ Đất Việt",
                    EventDate = new System.DateTime(2017, 7, 15),
                    Result = "1-0",
                    OpeningName = "Tiên Nhân Chỉ Lộ đối Tốt Đề Pháo",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"Trang Co Dat Viet\"]\n[Site \"Ha Noi\"]\n[Date \"2017.07.15\"]\n[Round \"Final\"]\n[Red \"Lai Ly Huynh\"]\n[Black \"Uong Duong Bac\"]\n[Result \"1-0\"]\n1. P7.1 P7.1 2. C2.5 H8.7 3. H2.3 R9.1",
                    MovesJson = "[]",
                    CreatedAt = new System.DateTime(2026, 10, 15, 6, 28, 4, 359, System.DateTimeKind.Utc).AddTicks(1358)
                }
            );
        }
    }
}
