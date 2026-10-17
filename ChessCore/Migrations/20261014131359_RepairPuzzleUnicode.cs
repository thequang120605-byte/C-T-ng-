using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class RepairPuzzleUnicode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `Puzzles` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" +
                "ALTER TABLE `PuzzleProgresses` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" +
                "UPDATE `Puzzles` SET `Title` = CASE `Id` " +
                "WHEN 1 THEN 'Tuyệt sát 1' " +
                "WHEN 2 THEN 'Pháo lồng' " +
                "WHEN 3 THEN 'Lưỡng chiếu' ELSE `Title` END, " +
                "`Description` = CASE `Id` " +
                "WHEN 1 THEN 'Chiếu bí trong 1 nước (Xe cắm đáy)' " +
                "WHEN 2 THEN 'Song pháo sát cục' " +
                "WHEN 3 THEN 'Kết hợp Xe Pháo' ELSE `Description` END " +
                "WHERE `Id` IN (1, 2, 3);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `Puzzles` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" +
                "ALTER TABLE `PuzzleProgresses` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
        }
    }
}
