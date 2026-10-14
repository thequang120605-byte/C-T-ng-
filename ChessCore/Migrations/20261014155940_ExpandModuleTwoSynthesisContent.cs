using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class ExpandModuleTwoSynthesisContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Content", "Description" },
                values: new object[] { "Bài tổng hợp 1 dùng bàn cờ đầy đủ 32 quân để kiểm tra ký hiệu, vị trí ban đầu và hướng di chuyển cơ bản của từng loại quân.", "Ôn tập đầy đủ 7 loại quân trên bàn cờ khai cuộc." });

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Content", "Description" },
                values: new object[] { "Bài tổng hợp 2 dùng bàn cờ đầy đủ 32 quân để kiểm tra vật cản, cách ăn quân và các giới hạn đặc biệt của từng quân.", "Ôn tập đầy đủ vật cản, ngòi Pháo, chân Mã và luật ăn quân." });

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Content", "Description" },
                values: new object[] { "Bài tổng hợp 3 dùng bàn cờ đầy đủ 32 quân để kiểm tra phối hợp lực lượng, an toàn Tướng và các quy tắc thực chiến.", "Ôn tập toàn bộ quân cờ và cách phối hợp trong thế khai cuộc." });

            migrationBuilder.InsertData(
                table: "CourseQuizQuestions",
                columns: new[] { "Id", "CorrectAnswer", "DisplayOrder", "Explanation", "LessonId", "OptionsJson", "Question" },
                values: new object[,]
                {
                    { 56, 0, 8, "Sĩ đi chéo một ô trong cung tướng.", 16, "[\"Sĩ\",\"Xe\",\"Pháo\"]", "Quân nào bảo vệ Tướng trong cung bằng nước đi chéo?" },
                    { 57, 0, 9, "Tượng đi chéo hai ô và chỉ hoạt động ở phần sân của mình.", 16, "[\"Tượng\",\"Mã\",\"Binh\"]", "Quân nào chỉ đi chéo hai ô và không qua sông?" },
                    { 58, 0, 10, "Binh đi thẳng một ô trước khi qua sông.", 16, "[\"Binh\",\"Tướng\",\"Sĩ\"]", "Quân nào đi thẳng trước khi qua sông?" },
                    { 59, 0, 11, "帥 là ký hiệu Tướng Đỏ; 將 là ký hiệu Tướng Đen.", 16, "[\"帥\",\"將\",\"仕\"]", "Ký hiệu nào là Tướng Đỏ?" },
                    { 60, 0, 9, "Xe không được đi xuyên qua bất kỳ quân nào.", 17, "[\"Xe\",\"Mã\",\"Pháo khi ăn\"]", "Quân nào không được nhảy qua quân chắn khi đi thẳng?" },
                    { 61, 0, 10, "Quân ở điểm giữa đường chéo chặn hướng đi của Tượng.", 17, "[\"Nước đi chéo đó không hợp lệ\",\"Tượng được nhảy qua\",\"Tượng đổi thành Mã\"]", "Mắt Tượng bị chặn thì điều gì xảy ra?" },
                    { 62, 0, 11, "Binh chỉ được đi ngang một ô mỗi nước.", 17, "[\"Một ô\",\"Hai ô\",\"Không giới hạn\"]", "Binh sau khi qua sông được đi ngang mấy ô mỗi nước?" },
                    { 63, 0, 12, "Quân cùng màu không thể ăn nhau và ô đó không phải ô đích hợp lệ.", 17, "[\"Không\",\"Có\",\"Chỉ khi là Tướng\"]", "Quân cùng màu ở ô đích có được ăn không?" },
                    { 64, 0, 9, "Pháo cần đúng một quân làm ngòi giữa nó và quân bị ăn.", 18, "[\"Một quân đứng giữa Pháo và mục tiêu\",\"Quân ở ngoài bàn\",\"Không cần quân nào\"]", "Khi phối hợp Xe và Pháo, quân nào có thể tạo ngòi cho Pháo?" },
                    { 65, 0, 10, "Không được thực hiện nước đi làm Tướng Đỏ bị lộ mặt hoặc rơi vào thế nguy hiểm.", 18, "[\"Tướng Đỏ không bị lộ mặt hoặc tự chiếu\",\"Chỉ quân Xe\",\"Ô ngoài bàn cờ\"]", "Nước đi của quân Đỏ phải bảo vệ điều gì?" },
                    { 66, 0, 11, "Xe kiểm soát các đường ngang và dọc khi không bị cản.", 18, "[\"Xe\",\"Sĩ\",\"Tượng\"]", "Quân nào có thể hỗ trợ kiểm soát nhiều đường thẳng?" },
                    { 67, 0, 12, "Quân bị ăn bị loại khỏi trạng thái bàn cờ.", 18, "[\"Xóa khỏi bàn cờ\",\"Giữ lại dưới quân ăn\",\"Đổi màu\"]", "Khi một quân bị ăn, vị trí của quân đó được xử lý thế nào?" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Content", "Description" },
                values: new object[] { "Bài tổng hợp 1 gồm các câu hỏi lý thuyết về nhận diện quân, vị trí khai cuộc và hướng di chuyển cơ bản.", "Ôn tập vị trí, ký hiệu và luật đi của bảy loại quân." });

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Content", "Description" },
                values: new object[] { "Bài tổng hợp 2 kiểm tra cách xử lý vật cản, ăn quân đúng luật và các quy tắc đặc biệt.", "Ôn tập vật cản, ngòi Pháo, chân Mã và điều kiện ăn quân." });

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Content", "Description" },
                values: new object[] { "Bài tổng hợp 3 kết hợp kiến thức về toàn bộ quân cờ, khai cuộc và các tình huống thực hành.", "Ôn tập phối hợp Xe, Pháo, Mã và các quân phòng thủ." });
        }
    }
}
