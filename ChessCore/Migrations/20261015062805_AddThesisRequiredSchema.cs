using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class AddThesisRequiredSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BestAlternative",
                table: "Moves",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "EvalScore",
                table: "Moves",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Judgment",
                table: "Moves",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<float>(
                name: "WinRate",
                table: "Moves",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiDepth",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameMode",
                table: "Games",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PvP")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "OpeningId",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    AccuracyRed = table.Column<int>(type: "int", nullable: false),
                    AccuracyBlack = table.Column<int>(type: "int", nullable: false),
                    TurningPointMoveOrder = table.Column<int>(type: "int", nullable: true),
                    BrilliantCount = table.Column<int>(type: "int", nullable: false),
                    BestCount = table.Column<int>(type: "int", nullable: false),
                    GoodCount = table.Column<int>(type: "int", nullable: false),
                    InaccuracyCount = table.Column<int>(type: "int", nullable: false),
                    BlunderCount = table.Column<int>(type: "int", nullable: false),
                    SummaryReviewJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnalyzedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameAnalyses_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MasterGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RedPlayer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlackPlayer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Event = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Result = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpeningName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InitialFen = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PgnContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MovesJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterGames", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Openings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartingFen = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Openings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserEloHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GameId = table.Column<int>(type: "int", nullable: true),
                    OldElo = table.Column<int>(type: "int", nullable: false),
                    NewElo = table.Column<int>(type: "int", nullable: false),
                    EloChange = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecordedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEloHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEloHistories_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserEloHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OpeningBookMoves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OpeningId = table.Column<int>(type: "int", nullable: false),
                    Fen = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MoveNotation = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MoveOrder = table.Column<int>(type: "int", nullable: false),
                    WinCount = table.Column<int>(type: "int", nullable: false),
                    DrawCount = table.Column<int>(type: "int", nullable: false),
                    LossCount = table.Column<int>(type: "int", nullable: false),
                    WinRate = table.Column<double>(type: "double", nullable: false),
                    Explanation = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpeningBookMoves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpeningBookMoves_Openings_OpeningId",
                        column: x => x.OpeningId,
                        principalTable: "Openings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "MasterGames",
                columns: new[] { "Id", "BlackPlayer", "CreatedAt", "Event", "EventDate", "InitialFen", "MovesJson", "OpeningName", "PgnContent", "RedPlayer", "Result", "Title" },
                values: new object[,]
                {
                    { 1, "Lữ Khâm", new DateTime(2026, 10, 15, 6, 28, 4, 359, DateTimeKind.Utc).AddTicks(1051), "Giải Vô địch Cá nhân Cờ Tướng Trung Quốc", new DateTime(1988, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1", "[]", "Thuận Pháo Hoành Xe đối Trực Xe", "[Event \"Giap Cap Lien Tai\"]\n[Site \"Quang Chau\"]\n[Date \"1988.11.20\"]\n[Round \"11\"]\n[Red \"Ho Vinh Hoa\"]\n[Black \"Lu Kham\"]\n[Result \"1-0\"]\n1. C2.5 C8.5 2. H2.3 H8.7 3. R1.2 R9.1 4. H8.7 H2.3", "Hồ Vinh Hoa", "1-0", "Hồ Vinh Hoa vs Lữ Khâm - Chung kết Toàn quốc 1988" },
                    { 2, "Uông Dương Bắc", new DateTime(2026, 10, 15, 6, 28, 4, 359, DateTimeKind.Utc).AddTicks(1358), "Trạng Cờ Đất Việt", new DateTime(2017, 7, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1", "[]", "Tiên Nhân Chỉ Lộ đối Tốt Đề Pháo", "[Event \"Trang Co Dat Viet\"]\n[Site \"Ha Noi\"]\n[Date \"2017.07.15\"]\n[Round \"Final\"]\n[Red \"Lai Ly Huynh\"]\n[Black \"Uong Duong Bac\"]\n[Result \"1-0\"]\n1. P7.1 P7.1 2. C2.5 H8.7 3. H2.3 R9.1", "Lại Lý Huynh", "1-0", "Lại Lý Huynh vs Uông Dương Bắc - Trạng Cờ Đất Việt 2017" }
                });

            migrationBuilder.InsertData(
                table: "Openings",
                columns: new[] { "Id", "Code", "Description", "Name", "StartingFen" },
                values: new object[,]
                {
                    { 1, "ECCO-01", "Thế trận công thủ toàn diện, Pháo vào trung lộ kiểm soát lộ 5, tạo sức ép thẳng vào cung Tướng.", "Pháo Đầu (Trung Pháo)", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" },
                    { 2, "ECCO-02", "Trận hình phòng thủ kinh điển chống lại Pháo Đầu, hai Mã nhảy lên giữ chặt trung lộ và bảo vệ Tốt đầu vững chắc.", "Bình Phong Mã", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" },
                    { 3, "ECCO-03", "Hai bên cùng đưa Pháo vào cùng một lộ trung tâm, thế cờ đối công kịch liệt, ăn miếng trả miếng.", "Thuận Pháo", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" },
                    { 4, "ECCO-04", "Bên Đỏ khai cuộc bằng nước Tốt 7 tiến 1 hoặc Tốt 3 tiến 1 để thăm dò ý đồ chiến thuật của đối phương.", "Tiên Nhân Chỉ Lộ", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" },
                    { 5, "ECCO-05", "Khai cuộc bằng nước phát triển Mã sớm, cấu trúc phòng thủ kiên cố, linh hoạt chuyển biến.", "Trận Thượng Tiên (Khởi Mã Cuộc)", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1" }
                });

            migrationBuilder.InsertData(
                table: "OpeningBookMoves",
                columns: new[] { "Id", "DrawCount", "Explanation", "Fen", "LossCount", "MoveNotation", "MoveOrder", "OpeningId", "WinCount", "WinRate" },
                values: new object[,]
                {
                    { 1, 8900, "Pháo vào giữa kiểm soát lộ trung tâm, sẵn sàng bắn Tốt đầu uy hiếp Tướng.", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1", 7600, "Pháo 2 bình 5 (C2.5)", 1, 1, 12500, 58.399999999999999 },
                    { 2, 7200, "Bên Đen nhảy Mã bảo vệ Tốt đầu, hình thành thế trận Bình Phong Mã đối kháng.", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1", 6500, "Mã 8 tiến 7 (M8.7)", 2, 1, 9800, 52.100000000000001 },
                    { 3, 6100, "Mã cánh phải phát triển kiểm soát trung lộ và bảo vệ Tốt đầu.", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1", 5900, "Mã 2 tiến 3 (M2.3)", 2, 2, 8400, 51.5 },
                    { 4, 4500, "Đen cũng đưa Pháo vào giữa, tạo thế Thuận Pháo đối công trực diện.", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/4C1C2/9/RNBAKABNR b - - 1 1", 6900, "Pháo 8 bình 5 (C8.5)", 2, 3, 7100, 49.799999999999997 },
                    { 5, 9400, "Tiến Tốt mở đường cho Mã phát triển và thăm dò phản ứng của đối phương.", "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1", 7800, "Binh 7 tiến 1 (P7.1)", 1, 4, 11200, 54.200000000000003 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_OpeningId",
                table: "Games",
                column: "OpeningId");

            migrationBuilder.CreateIndex(
                name: "IX_GameAnalyses_GameId",
                table: "GameAnalyses",
                column: "GameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpeningBookMoves_OpeningId",
                table: "OpeningBookMoves",
                column: "OpeningId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEloHistories_GameId",
                table: "UserEloHistories",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEloHistories_UserId",
                table: "UserEloHistories",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Openings_OpeningId",
                table: "Games",
                column: "OpeningId",
                principalTable: "Openings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Openings_OpeningId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "GameAnalyses");

            migrationBuilder.DropTable(
                name: "MasterGames");

            migrationBuilder.DropTable(
                name: "OpeningBookMoves");

            migrationBuilder.DropTable(
                name: "UserEloHistories");

            migrationBuilder.DropTable(
                name: "Openings");

            migrationBuilder.DropIndex(
                name: "IX_Games_OpeningId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "BestAlternative",
                table: "Moves");

            migrationBuilder.DropColumn(
                name: "EvalScore",
                table: "Moves");

            migrationBuilder.DropColumn(
                name: "Judgment",
                table: "Moves");

            migrationBuilder.DropColumn(
                name: "WinRate",
                table: "Moves");

            migrationBuilder.DropColumn(
                name: "AiDepth",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GameMode",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "OpeningId",
                table: "Games");
        }
    }
}
