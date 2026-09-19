using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class AddGameMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BlackPlayerId",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameType",
                table: "Games",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "RedPlayerId",
                table: "Games",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Winner",
                table: "Games",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlackPlayerId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GameType",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RedPlayerId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Winner",
                table: "Games");
        }
    }
}
