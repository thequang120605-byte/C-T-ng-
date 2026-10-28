using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class AddIsLockedToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "GameMode",
                table: "Games",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PvP",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Legacy")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "GameMode",
                table: "Games",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Legacy",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "PvP")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
