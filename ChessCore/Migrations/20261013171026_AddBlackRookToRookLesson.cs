using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBlackRookToRookLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 3,
                column: "Content",
                value: "Xe đi ngang hoặc dọc không giới hạn, nhưng không được đi xuyên qua quân khác. Xe Đỏ bắt đầu ở hàng cuối, cột 0; Xe Đen bắt đầu ở hàng đầu, cột 0. Bạn điều khiển Xe Đỏ, còn Xe Đen là quân đối diện để quan sát và thực hành ăn quân.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 3,
                column: "Content",
                value: "Xe đi ngang hoặc dọc không giới hạn, nhưng không được đi xuyên qua quân khác. Quân Đỏ bắt đầu ở hàng cuối, cột 0 và 8.");
        }
    }
}
