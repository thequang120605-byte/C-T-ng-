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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình bảng User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            });

            // Cấu hình bảng Game
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status)
                    .HasConversion<string>() // Lưu enum dạng string cho dễ đọc trong DB MySql
                    .HasMaxLength(30);
                entity.Property(e => e.CurrentTurn)
                    .HasConversion<string>()
                    .HasMaxLength(20);

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
        }
    }
}
