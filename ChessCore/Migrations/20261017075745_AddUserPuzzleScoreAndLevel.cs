using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPuzzleScoreAndLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PuzzleScore",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuzzleScore",
                table: "Users");
        }
    }
}
