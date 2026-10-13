using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseQuizCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CourseQuizQuestions",
                columns: new[] { "Id", "CorrectAnswer", "DisplayOrder", "Explanation", "LessonId", "OptionsJson", "Question" },
                values: new object[,]
                {
                    { 6, 1, 1, "Giữa hai Tướng phải có ít nhất một quân chắn.", 5, "[\"Có, nếu cùng một cột\",\"Không, phải có quân chắn ở giữa\",\"Chỉ được khi đang chiếu\"]", "Hai Tướng có được nhìn thẳng nhau không?" },
                    { 7, 2, 1, "Chiếu hết kết thúc ván vì bên bị chiếu không thể hóa giải.", 6, "[\"Tướng bị đe dọa nhưng còn đường thoát\",\"Tướng không bị đe dọa\",\"Bị chiếu và không còn nước hợp lệ để thoát\"]", "Chiếu hết nghĩa là gì?" },
                    { 8, 0, 1, "Bắt bí là hòa cờ: không còn nước đi nhưng Tướng không bị chiếu.", 7, "[\"Không còn nước hợp lệ nhưng không bị chiếu\",\"Bị chiếu hết\",\"Ăn được quân Xe\"]", "Bắt bí là trường hợp nào?" },
                    { 9, 1, 1, "Đổi quân cần phục vụ kế hoạch và đem lại lợi thế.", 9, "[\"Luôn luôn đổi quân\",\"Khi đổi quân có lợi về vị trí hoặc vật chất\",\"Không bao giờ đổi quân\"]", "Khi nào nên đổi quân?" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 9);
        }
    }
}
