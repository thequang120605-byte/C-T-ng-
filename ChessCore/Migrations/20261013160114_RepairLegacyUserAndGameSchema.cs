using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class RepairLegacyUserAndGameSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The repair is applied by Database/RepairLegacyDatabase.sql for
            // databases that were created before EF migration history existed.
            // Keeping this migration empty makes clean databases safe because
            // the earlier migrations already create the complete schema.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
