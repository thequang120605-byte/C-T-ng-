using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class SeedPuzzles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Puzzles",
                columns: new[] { "Id", "Description", "Difficulty", "Fen", "Level", "MateIn", "Title" },
                values: new object[,]
                {
                    { 1, "Chiếu bí trong 1 nước (Xe cắm đáy)", 1, "4k4/9/9/9/9/9/9/9/9/4R4 r - - 0 1", 1, 1, "Tuyệt sát 1" },
                    { 2, "Song pháo sát cục", 1, "4k4/4a4/4a4/9/9/9/9/9/9/4C4 r - - 0 1", 2, 1, "Pháo lồng" },
                    { 3, "Kết hợp Xe Pháo", 2, "4k4/9/9/9/9/9/9/9/9/4R4 r - - 0 1", 3, 1, "Lưỡng chiếu" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Puzzles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Puzzles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Puzzles",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
