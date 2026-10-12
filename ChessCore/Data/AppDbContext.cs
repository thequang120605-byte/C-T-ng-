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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PuzzleProgress>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PuzzleKey).IsRequired().HasMaxLength(40);
                entity.HasIndex(e => new { e.UserId, e.PuzzleKey }).IsUnique();
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
        }
    }
}
