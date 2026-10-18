using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class ScoringModesAndSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<bool>(
                name: "IsRated",
                table: "Games",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultsProcessedAt",
                table: "Games",
                type: "datetime(6)",
                nullable: true);

            // Never award old results again. Preserve existing ratings as a legacy baseline.
            migrationBuilder.Sql("""
                UPDATE Games SET GameMode = CASE
                    WHEN RedPlayerId IS NOT NULL AND BlackPlayerId IS NOT NULL
                        AND RedPlayerId <> BlackPlayerId THEN 'Online'
                    WHEN GameMode IN ('PvE', 'AiVsAi') THEN GameMode
                    ELSE 'Legacy' END;
                UPDATE Games SET ResultsProcessedAt = UTC_TIMESTAMP(6)
                    WHERE Status IN ('RedWins', 'BlackWins', 'Draw');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Games_GameMode_Status",
                table: "Games",
                columns: new[] { "GameMode", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_GameMode_Status",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IsRated",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ResultsProcessedAt",
                table: "Games");

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
    }
}
