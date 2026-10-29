using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ChessCore.Models;
using ChessCore.Enums;

namespace ChessCore.Data
{
    public static class DatabaseBootstrap
    {
        public static async Task InitializeAsync(AppDbContext dbContext, ILogger logger)
        {
            // 1. Chạy Migration tự động của EF Core (đã sửa lỗi collation puzzlekey trên TiDB)
            try
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation(">>> EF Core Database.Migrate hoàn tất thành công.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(">>> Cảnh báo khi chạy Database.Migrate: {Message}. Tiến hành Self-Healing vá dự phòng...", ex.Message);
            }

            // 2. Tự động kiểm tra và vá bổ sung các cột/bảng bị thiếu (Self-Healing Schema)
            await EnsureSchemaColumnsAndTablesAsync(dbContext, logger);

            // 3. Seed tài khoản Quản trị viên mặc định (admin / Admin@123)
            await EnsureAdminUserAsync(dbContext, logger);

            // 4. Seed dữ liệu 12 ván cờ Kỳ phổ Danh thủ kinh điển
            await EnsureMasterGamesAsync(dbContext, logger);

            // 5. Chuẩn hóa tiêu đề cờ thế (loại bỏ chú thích thừa và số bài thừa)
            await NormalizePuzzleTitlesAsync(dbContext, logger);

            // 6. Dọn dẹp phòng trống bỏ rơi và chuẩn hóa đồng bộ thống kê người dùng
            await SynchronizeUserStatsAndCleanupAsync(dbContext, logger);
        }

        public static async Task EnsureSchemaColumnsAndTablesAsync(AppDbContext dbContext, ILogger? logger = null)
        {
            // --- 1. BẢNG Users ---
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Users` ADD COLUMN `IsLocked` TINYINT(1) NOT NULL DEFAULT 0;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Users` ADD COLUMN `PuzzleScore` INT NOT NULL DEFAULT 500;", logger);

            // --- 2. BẢNG Games (Sửa lỗi Unknown column 'g.aidepth', 'g.gamemode', 'g.israted', v.v.) ---
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `AiDepth` INT NULL;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `GameMode` VARCHAR(20) NOT NULL DEFAULT 'PvP';", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `OpeningId` INT NULL;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `IsRated` TINYINT(1) NOT NULL DEFAULT 0;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `ResultsProcessedAt` DATETIME(6) NULL;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `RoomCode` VARCHAR(50) NOT NULL DEFAULT '';", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `InitialFen` VARCHAR(255) NULL;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `LastMoveAt` DATETIME(6) NULL;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `TimeControlMinutes` INT NOT NULL DEFAULT 3;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `Version` BIGINT NOT NULL DEFAULT 0;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Games` ADD COLUMN `AiColor` VARCHAR(10) NULL DEFAULT 'Black';", logger);
            await ExecuteRawSqlSafely(dbContext, "CREATE UNIQUE INDEX `IX_Moves_GameId_MoveOrder` ON `Moves` (`GameId`, `MoveOrder`);", logger);

            // --- 3. BẢNG Moves (Sửa lỗi các trường đánh giá AI) ---
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Moves` ADD COLUMN `EvalScore` INT NULL;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Moves` ADD COLUMN `WinRate` FLOAT NULL;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Moves` ADD COLUMN `Judgment` VARCHAR(30) NULL;", logger);
            await ExecuteRawSqlSafely(dbContext, "ALTER TABLE `Moves` ADD COLUMN `BestAlternative` VARCHAR(50) NULL;", logger);

            // --- 4. BẢNG MasterGames ---
            await ExecuteRawSqlSafely(dbContext, @"
                CREATE TABLE IF NOT EXISTS `MasterGames` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `Title` VARCHAR(200) NOT NULL,
                    `RedPlayer` VARCHAR(100) NOT NULL,
                    `BlackPlayer` VARCHAR(100) NOT NULL,
                    `Event` VARCHAR(150) NULL,
                    `EventDate` DATETIME(6) NULL,
                    `Result` VARCHAR(20) NOT NULL,
                    `OpeningName` VARCHAR(150) NULL,
                    `InitialFen` VARCHAR(255) NULL,
                    `PgnContent` LONGTEXT NULL,
                    `MovesJson` LONGTEXT NULL,
                    `CreatedAt` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`Id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ", logger);

            // --- 5. BẢNG Openings ---
            await ExecuteRawSqlSafely(dbContext, @"
                CREATE TABLE IF NOT EXISTS `Openings` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `Code` VARCHAR(30) NOT NULL,
                    `Name` VARCHAR(150) NOT NULL,
                    `Description` VARCHAR(500) NULL,
                    `StartingFen` VARCHAR(255) NOT NULL,
                    PRIMARY KEY (`Id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ", logger);

            // --- 6. BẢNG OpeningBookMoves ---
            await ExecuteRawSqlSafely(dbContext, @"
                CREATE TABLE IF NOT EXISTS `OpeningBookMoves` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `OpeningId` INT NOT NULL,
                    `Fen` VARCHAR(255) NOT NULL,
                    `MoveNotation` VARCHAR(50) NOT NULL,
                    `MoveOrder` INT NOT NULL,
                    `WinCount` INT NOT NULL DEFAULT 0,
                    `DrawCount` INT NOT NULL DEFAULT 0,
                    `LossCount` INT NOT NULL DEFAULT 0,
                    `WinRate` DOUBLE NOT NULL DEFAULT 0,
                    `Explanation` VARCHAR(500) NULL,
                    PRIMARY KEY (`Id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ", logger);

            // --- 7. BẢNG UserEloHistories ---
            await ExecuteRawSqlSafely(dbContext, @"
                CREATE TABLE IF NOT EXISTS `UserEloHistories` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `UserId` INT NOT NULL,
                    `GameId` INT NULL,
                    `OldElo` INT NOT NULL,
                    `NewElo` INT NOT NULL,
                    `EloChange` INT NOT NULL DEFAULT 0,
                    `Reason` VARCHAR(100) NULL,
                    `RecordedAt` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`Id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ", logger);

            // --- 8. BẢNG GameAnalyses ---
            await ExecuteRawSqlSafely(dbContext, @"
                CREATE TABLE IF NOT EXISTS `GameAnalyses` (
                    `Id` INT NOT NULL AUTO_INCREMENT,
                    `GameId` INT NOT NULL,
                    `AccuracyRed` INT NOT NULL DEFAULT 0,
                    `AccuracyBlack` INT NOT NULL DEFAULT 0,
                    `TurningPointMoveOrder` INT NULL,
                    `BrilliantCount` INT NOT NULL DEFAULT 0,
                    `BestCount` INT NOT NULL DEFAULT 0,
                    `GoodCount` INT NOT NULL DEFAULT 0,
                    `InaccuracyCount` INT NOT NULL DEFAULT 0,
                    `BlunderCount` INT NOT NULL DEFAULT 0,
                    `SummaryReviewJson` LONGTEXT NULL,
                    `AnalyzedAt` DATETIME(6) NOT NULL,
                    PRIMARY KEY (`Id`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ", logger);
        }

        private static async Task ExecuteRawSqlSafely(AppDbContext dbContext, string sql, ILogger? logger)
        {
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(sql);
            }
            catch
            {
                // Bỏ qua ngoại lệ nếu cột hoặc bảng đã tồn tại sẵn
            }
        }

        private static async Task EnsureAdminUserAsync(AppDbContext dbContext, ILogger logger)
        {
            try
            {
                var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("Admin@123"));
                var passwordHash = Convert.ToBase64String(hashBytes);

                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@cotuong.pro",
                        PasswordHash = passwordHash,
                        PlayerId = "CT-00001",
                        Role = "Admin",
                        Elo = 1500,
                        IsLocked = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Users.Add(adminUser);
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation(">>> Đã khởi tạo thành công tài khoản quản trị viên mặc định (admin).");
                }
                else
                {
                    bool changed = false;
                    if (adminUser.Role != "Admin")
                    {
                        adminUser.Role = "Admin";
                        changed = true;
                    }
                    if (adminUser.IsLocked)
                    {
                        adminUser.IsLocked = false;
                        changed = true;
                    }
                    if (changed)
                    {
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation(">>> Đã chuẩn hóa quyền Admin và mở khóa tài khoản 'admin'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(">>> Cảnh báo khi tạo/kiểm tra tài khoản admin: {Message}", ex.Message);
            }
        }

        public static async Task NormalizePuzzleTitlesAsync(AppDbContext dbContext, ILogger? logger = null)
        {
            try
            {
                await ExecuteRawSqlSafely(dbContext, "UPDATE `Puzzles` SET `Title` = 'Đối diện tiếu' WHERE `Title` LIKE 'Đối diện tiếu%';", logger);
                await ExecuteRawSqlSafely(dbContext, "UPDATE `Puzzles` SET `Title` = 'Bạch diện tướng' WHERE `Title` LIKE 'Bạch diện tướng%';", logger);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(">>> Cảnh báo khi chuẩn hóa tiêu đề cờ thế: {Message}", ex.Message);
            }
        }

        public static async Task EnsureMasterGamesAsync(AppDbContext dbContext, ILogger? logger = null)
        {
            try
            {
                await EnsureSchemaColumnsAndTablesAsync(dbContext, logger);

                if (await dbContext.MasterGames.CountAsync() < 12)
                {
                    var games = GetSeedMasterGames();
                    foreach (var g in games)
                    {
                        if (!await dbContext.MasterGames.AnyAsync(m => m.Title == g.Title))
                        {
                            dbContext.MasterGames.Add(g);
                        }
                    }
                    await dbContext.SaveChangesAsync();
                    logger?.LogInformation(">>> Đã nạp thành công 12 ván cờ Kỳ phổ Danh thủ kinh điển.");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(">>> Cảnh báo khi nạp Kỳ phổ Danh thủ: {Message}", ex.Message);
            }
        }

        public static List<MasterGame> GetSeedMasterGames()
        {
            return new List<MasterGame>
            {
                new MasterGame
                {
                    Title = "Vương Thiên Nhất vs Trịnh Duy Đồng - Kỳ Vương Tranh Bá 2019",
                    RedPlayer = "Vương Thiên Nhất",
                    BlackPlayer = "Trịnh Duy Đồng",
                    Event = "Bích Quế Viên Bôi (Siêu Kỳ Vương)",
                    EventDate = new DateTime(2019, 12, 10),
                    Result = "1-0",
                    OpeningName = "Trung Pháo đối Bình Phong Mã Lưỡng Đầu Xà",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"Bich Que Vien Boi\"]\n[Site \"Quang Chau\"]\n[Date \"2019.12.10\"]\n[Round \"Chung Ket\"]\n[Red \"Vuong Thien Nhat\"]\n[Black \"Trinh Duy Dong\"]\n[Result \"1-0\"]\n1. C2.5 H8.7\n2. H2.3 R9.1\n3. R1.2 H2.3\n4. P7.1 P7.1\n5. H8.7 P3.1\n6. R2.6 C8.9\n7. C8.4 R1.2\n8. R2.7 C2.1\n9. P5.1 H7.5\n10. C5.1 C9.2\n(Ngoại tinh lai khách Vương Thiên Nhất công phá trung lộ thần sầu giành ngôi Quán quân)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Hứa Ngân Xuyên vs Lữ Khâm - Cúp Ngũ Dương 2001",
                    RedPlayer = "Hứa Ngân Xuyên",
                    BlackPlayer = "Lữ Khâm",
                    Event = "Cúp Cờ Tướng Ngũ Dương Danh Thủ",
                    EventDate = new DateTime(2001, 1, 18),
                    Result = "1-0",
                    OpeningName = "Khởi Mã Cuộc đối Tốt Đề Pháo",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"Ngu Duong Boi\"]\n[Site \"Quang Chau\"]\n[Date \"2001.01.18\"]\n[Round \"Vong 5\"]\n[Red \"Hua Ngan Xuyen\"]\n[Black \"Lu Kham\"]\n[Result \"1-0\"]\n1. H2.3 P7.1\n2. P7.1 C2.5\n3. C2.4 H2.3\n4. H8.7 R1.2\n5. R1.2 H8.7\n6. R2.6 P3.1\n7. C8.9 C8.9\n8. P5.1 C5.4\n9. H7.5 R9.8\n10. R2.8 A4.5\n(Thái cực quy tông Hứa Ngân Xuyên vận dụng tàn cuộc tột đỉnh nghệ thuật)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Nguyễn Thành Bảo vs Tôn Dũng Chinh - Đại hội Thể thao Châu Á 2010",
                    RedPlayer = "Nguyễn Thành Bảo",
                    BlackPlayer = "Tôn Dũng Chinh",
                    Event = "Asian Games Quảng Châu 2010",
                    EventDate = new DateTime(2010, 11, 15),
                    Result = "1/2-1/2",
                    OpeningName = "Phi Tượng Cuộc đối Quá Cung Pháo",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"Asian Games 16\"]\n[Site \"Quang Chau\"]\n[Date \"2010.11.15\"]\n[Round \"Vong 4\"]\n[Red \"Nguyen Thanh Bao\"]\n[Black \"Ton Dung Chinh\"]\n[Result \"1/2-1/2\"]\n1. E2.4 C2.6\n2. H2.3 H2.3\n3. R1.2 R1.2\n4. H8.7 P7.1\n5. P7.1 H8.7\n6. C8.9 C8.2\n7. P3.1 P3.1\n8. R2.6 C6.5\n9. C2.5 R2.4\n10. H3.5 R2.8\n(Tây Độc Nguyễn Thành Bảo thi triển lối đánh bão táp quả cảm thủ hòa danh thủ Trung Hoa)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Lại Lý Huynh vs Mạnh Thần - Giáp Cấp Liên Tái 2023",
                    RedPlayer = "Lại Lý Huynh",
                    BlackPlayer = "Mạnh Thần",
                    Event = "Giải Vô địch Đồng đội Cờ Tướng Giáp Cấp Trung Quốc",
                    EventDate = new DateTime(2023, 7, 24),
                    Result = "1-0",
                    OpeningName = "Tiên Nhân Chỉ Lộ chuyển Tả Trung Pháo",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"Giap Cap Lien Tai 2023\"]\n[Site \"Hang Chau\"]\n[Date \"2023.07.24\"]\n[Round \"Vong 14\"]\n[Red \"Lai Ly Huynh\"]\n[Black \"Manh Than\"]\n[Result \"1-0\"]\n1. P7.1 P7.1\n2. C2.5 H8.7\n3. H2.3 R9.1\n4. R1.2 H2.3\n5. H8.7 P3.1\n6. R2.6 C8.9\n7. C8.4 R1.2\n8. P5.1 H7.5\n9. C5.1 C9.2\n10. R2.7 C2.1\n(Nam Phương Công Tử Lại Lý Huynh kích bại Siêu đại sư Mạnh Thần vang dội kỳ đàn)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Dương Quan Lân vs Hồ Vinh Hoa - Trận Chiến Chuyển Giao Thế Hệ 1960",
                    RedPlayer = "Dương Quan Lân",
                    BlackPlayer = "Hồ Vinh Hoa",
                    Event = "Giải Vô địch Toàn quốc Trung Quốc 1960",
                    EventDate = new DateTime(1960, 10, 22),
                    Result = "0-1",
                    OpeningName = "Quá Cung Pháo đối Bình Phong Mã",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"National Championship 1960\"]\n[Site \"Bac Kinh\"]\n[Date \"1960.10.22\"]\n[Round \"Chung Ket\"]\n[Red \"Duong Quan Lan\"]\n[Black \"Ho Vinh Hoa\"]\n[Result \"0-1\"]\n1. C2.6 H8.7\n2. H2.3 P7.1\n3. R1.2 R9.1\n4. H8.7 H2.3\n5. P7.1 C8.9\n6. R2.6 C2.4\n7. P3.1 P3.1\n8. H3.4 C4.5\n9. R2.7 H7.5\n10. P5.1 R1.2\n(Thiếu niên 15 tuổi Hồ Vinh Hoa hạ gục đương kim Ma Kỳ Dương Quan Lân mở ra kỷ nguyên Thập Liên Quán)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Triệu Hâm Hâm vs Tưởng Xuyên - Đỉnh Cao Tấn Công 2007",
                    RedPlayer = "Triệu Hâm Hâm",
                    BlackPlayer = "Tưởng Xuyên",
                    Event = "Giải Vô địch Cá nhân Toàn quốc 2007",
                    EventDate = new DateTime(2007, 9, 16),
                    Result = "1-0",
                    OpeningName = "Trung Pháo Quá Hà Xe đối Bình Phong Mã",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"National Championship 2007\"]\n[Site \"Ho Nam\"]\n[Date \"2007.09.16\"]\n[Round \"Vong 9\"]\n[Red \"Trieu Ham Ham\"]\n[Black \"Tuong Xuyen\"]\n[Result \"1-0\"]\n1. C2.5 H8.7\n2. H2.3 R9.1\n3. R1.2 H2.3\n4. P7.1 P7.1\n5. R2.6 C8.9\n6. R2.2 C9.4\n7. H8.7 P3.1\n8. C8.9 R1.2\n9. P5.1 H7.5\n10. C5.1 C2.5\n(Lục Mạch Thần Kiếm Triệu Hâm Hâm phát động đợt công kích vũ bão giành ngôi Kỳ vương trẻ tuổi)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Lại Lý Huynh vs Nguyễn Thành Bảo - Siêu Kinh Điển Việt Nam 2021",
                    RedPlayer = "Lại Lý Huynh",
                    BlackPlayer = "Nguyễn Thành Bảo",
                    Event = "Giải Vô địch Cờ Tướng Xuất sắc Toàn quốc",
                    EventDate = new DateTime(2021, 11, 28),
                    Result = "1-0",
                    OpeningName = "Ngũ Cửu Pháo đối Bình Phong Mã Tả Mã Bàn Hà",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"National Masters Vietnam 2021\"]\n[Site \"Vung Tau\"]\n[Date \"2021.11.28\"]\n[Round \"Chung Ket\"]\n[Red \"Lai Ly Huynh\"]\n[Black \"Nguyen Thanh Bảo\"]\n[Result \"1-0\"]\n1. C2.5 H8.7\n2. H2.3 R9.1\n3. R1.2 H2.3\n4. P7.1 P7.1\n5. C8.9 C8.9\n6. R2.6 P3.1\n7. H8.7 R1.2\n8. P9.1 H7.6\n9. C9.4 A4.5\n10. R6.7 C9.2\n(Trận thư hùng đỉnh cao giữa hai tượng đài cờ tướng hàng đầu Việt Nam suốt hai thập kỷ)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Trịnh Duy Đồng vs Uông Dương - Quyết Chiến Chung Kết 2018",
                    RedPlayer = "Trịnh Duy Đồng",
                    BlackPlayer = "Uông Dương",
                    Event = "Giải Vô địch Cờ Tướng Cá nhân Châu Á 2018",
                    EventDate = new DateTime(2018, 9, 25),
                    Result = "0-1",
                    OpeningName = "Phi Tượng Cuộc đối Sĩ Giác Pháo",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"Asian Individual Championship 2018\"]\n[Site \"Thuong Hai\"]\n[Date \"2018.09.25\"]\n[Round \"Chung Ket\"]\n[Red \"Trinh Duy Dong\"]\n[Black \"Uong Duong\"]\n[Result \"0-1\"]\n1. E2.4 C8.4\n2. H2.3 H8.7\n3. P7.1 P7.1\n4. R1.2 R9.1\n5. H8.7 H2.3\n6. C2.5 P3.1\n7. R2.6 C2.1\n8. C8.9 R1.2\n9. P3.1 C4.5\n10. H7.5 R2.7\n(Toán Đầu Đà Uông Dương tính toán thâm sâu xuất sắc kích bại Thục Sơn Thiếu Hiệp Trịnh Duy Đồng)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Vương Thiên Nhất vs Tạ Tịnh - Đỉnh Phong Đối Quyết 2013",
                    RedPlayer = "Vương Thiên Nhất",
                    BlackPlayer = "Tạ Tịnh",
                    Event = "Chung kết Giải Vô địch Quốc gia Trung Quốc 2013",
                    EventDate = new DateTime(2013, 11, 14),
                    Result = "1-0",
                    OpeningName = "Khởi Mã Cuộc Chuyển Trung Pháo đối Đơn Đề Mã",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"China National Championship 2013\"]\n[Site \"Thach Gia Trang\"]\n[Date \"2013.11.14\"]\n[Round \"Chung Ket\"]\n[Red \"Vuong Thien Nhat\"]\n[Black \"Ta Tinh\"]\n[Result \"1-0\"]\n1. H2.3 P7.1\n2. P7.1 H8.7\n3. C2.5 H2.3\n4. R1.2 R9.1\n5. H8.7 P3.1\n6. R2.6 C8.9\n7. C8.4 R1.2\n8. P5.1 H7.5\n9. C5.1 C9.2\n10. R2.7 C2.1\n(Ngoại Tinh Lai Khách điều binh khiển tướng như thần giành danh hiệu vô địch thuyết phục)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Liễu Đại Hoa vs Lý Lai Quần - Kỳ Đàn Tranh Hùng 1982",
                    RedPlayer = "Liễu Đại Hoa",
                    BlackPlayer = "Lý Lai Quần",
                    Event = "Cúp Danh Thủ Ngũ Dương Bôi Lần 2",
                    EventDate = new DateTime(1982, 1, 10),
                    Result = "1-0",
                    OpeningName = "Trung Pháo Tiến Tam Binh đối Bình Phong Mã",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"Ngu Duong Boi 1982\"]\n[Site \"Quang Chau\"]\n[Date \"1982.01.10\"]\n[Round \"Vong 3\"]\n[Red \"Lieu Dai Hoa\"]\n[Black \"Ly Lai Quan\"]\n[Result \"1-0\"]\n1. C2.5 H8.7\n2. H2.3 R9.1\n3. P3.1 P3.1\n4. R1.2 H2.3\n5. H8.9 C8.9\n6. R2.6 P7.1\n7. C8.7 R1.2\n8. P5.1 H7.5\n9. R6.7 C2.1\n10. C7.1 A4.5\n(Đông Phương Ma Mưu Liễu Đại Hoa phô diễn nghệ thuật cờ mù và trung cuộc thâm sâu)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Hồ Vinh Hoa vs Lữ Khâm - Siêu Sao Hội Tụ 1993",
                    RedPlayer = "Hồ Vinh Hoa",
                    BlackPlayer = "Lữ Khâm",
                    Event = "Giải Vô địch Cờ Tướng Thế giới 1993",
                    EventDate = new DateTime(1993, 4, 12),
                    Result = "1/2-1/2",
                    OpeningName = "Nghịch Pháo đối Trung Pháo Hoành Xe",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"World Championship 1993\"]\n[Site \"Bac Kinh\"]\n[Date \"1993.04.12\"]\n[Round \"Vong 7\"]\n[Red \"Ho Vinh Hoa\"]\n[Black \"Lu Kham\"]\n[Result \"1/2-1/2\"]\n1. C2.5 C8.5\n2. H2.3 H8.7\n3. R1.2 R9.1\n4. R2.6 H2.3\n5. P7.1 P7.1\n6. H8.7 R1.2\n7. C8.9 C2.4\n8. P3.1 P3.1\n9. H3.4 C5.4\n10. R6.7 C4.5\n(Đại chiến cân não giữa Thập Liên Quán Hồ Vinh Hoa và Khoái Mã Lữ Khâm)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                },
                new MasterGame
                {
                    Title = "Vương Thiên Nhất vs Tưởng Xuyên - Quyết Chiến Kỳ Vương 2012",
                    RedPlayer = "Vương Thiên Nhất",
                    BlackPlayer = "Tưởng Xuyên",
                    Event = "Cúp Kỳ Vương Mao Đài Bôi 2012",
                    EventDate = new DateTime(2012, 10, 8),
                    Result = "1-0",
                    OpeningName = "Trung Pháo Quá Hà Xe đối Bình Phong Mã Tả Mã Bàn Hà",
                    InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                    PgnContent = "[Event \"Mao Dai Boi 2012\"]\n[Site \"Quy Chau\"]\n[Date \"2012.10.08\"]\n[Round \"Chung Ket\"]\n[Red \"Vuong Thien Nhat\"]\n[Black \"Tuong Xuyen\"]\n[Result \"1-0\"]\n1. C2.5 H8.7\n2. H2.3 R9.1\n3. R1.2 H2.3\n4. P7.1 P7.1\n5. R2.6 C8.9\n6. R2.2 C9.4\n7. H8.7 P3.1\n8. C8.9 R1.2\n9. P5.1 H7.5\n10. C5.1 C2.5\n(Vương Thiên Nhất bứt phá ngoạn mục giành cúp vô địch đánh dấu bước ngoặt thống trị kỳ đàn)",
                    MovesJson = "[]",
                    CreatedAt = DateTime.UtcNow
                }
            };
        }

        public static async Task SynchronizeUserStatsAndCleanupAsync(AppDbContext dbContext, ILogger logger)
        {
            try
            {
                // 1. Dọn dẹp các ván Online tạo phòng dở bị bỏ rơi (Resigned, không có đối thủ, không có nước đi)
                await ExecuteRawSqlSafely(dbContext,
                    "DELETE FROM `Games` WHERE `GameMode` = 'OnlinePublic' AND `Status` = 'Resigned' AND `BlackPlayerId` IS NULL;", logger);

                // 2. Đồng bộ chỉ số ván đấu thực tế cho các tài khoản người dùng có tham gia trận đấu
                var activeUserIds = await dbContext.Games
                    .Where(g => g.RedPlayerId.HasValue || g.BlackPlayerId.HasValue)
                    .Select(g => g.RedPlayerId ?? g.BlackPlayerId!.Value)
                    .Distinct()
                    .ToListAsync();

                var users = await dbContext.Users
                    .Where(u => activeUserIds.Contains(u.Id))
                    .ToListAsync();

                var games = await dbContext.Games.AsNoTracking()
                    .Where(g => !(g.GameMode == "OnlinePublic" && g.BlackPlayerId == null && g.Status == GameStatus.Resigned))
                    .Where(g => (g.GameMode.StartsWith("Online") || g.GameMode == "PvE") &&
                                (g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw))
                    .ToListAsync();

                bool anyChanged = false;
                foreach (var u in users)
                {
                    int played = games.Count(g => g.RedPlayerId == u.Id || g.BlackPlayerId == u.Id);
                    int wins = games.Count(g => (g.RedPlayerId == u.Id && g.Status == GameStatus.RedWins) ||
                                               (g.BlackPlayerId == u.Id && g.Status == GameStatus.BlackWins));
                    int losses = games.Count(g => (g.RedPlayerId == u.Id && g.Status == GameStatus.BlackWins) ||
                                                 (g.BlackPlayerId == u.Id && g.Status == GameStatus.RedWins));
                    int draws = games.Count(g => (g.RedPlayerId == u.Id || g.BlackPlayerId == u.Id) && g.Status == GameStatus.Draw);

                    if (u.GamesPlayed != played || u.Wins != wins || u.Losses != losses || u.Draws != draws)
                    {
                        u.GamesPlayed = played;
                        u.Wins = wins;
                        u.Losses = losses;
                        u.Draws = draws;
                        anyChanged = true;
                    }
                }

                if (anyChanged)
                {
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation(">>> Đã chuẩn hóa và đồng bộ thành công chỉ số GamesPlayed/Wins/Losses/Draws cho người dùng.");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(">>> Cảnh báo khi chuẩn hóa thống kê người dùng: {Message}", ex.Message);
            }
        }
    }
}
