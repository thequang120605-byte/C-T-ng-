using ChessCore.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessCore.Migrations
{
    public partial class AddGameRoomCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoomCode",
                table: "Games",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Preserve a unique, eight-character share code for existing games.
            migrationBuilder.Sql("UPDATE Games SET RoomCode = LPAD(UPPER(HEX(Id)), 8, '0') WHERE RoomCode IS NULL OR RoomCode = '';");

            migrationBuilder.AlterColumn<string>(
                name: "RoomCode",
                table: "Games",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Games_RoomCode",
                table: "Games",
                column: "RoomCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_RoomCode",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "RoomCode",
                table: "Games");
        }
    }
}
