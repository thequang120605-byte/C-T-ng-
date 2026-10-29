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
            // TiDB Cloud đã mặc định hỗ trợ utf8mb4. Bỏ lệnh CONVERT COLLATE trên bảng có index để tránh lỗi TiDB 80004005.
            migrationBuilder.Sql(
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
        }
    }
}
