using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleTwoFinalSynthesis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CourseLessons",
                columns: new[] { "Id", "BoardPosition", "Content", "Description", "DisplayOrder", "LessonType", "ModuleId", "Title" },
                values: new object[] { 19, null, "Bài tổng kết mở sau khi hoàn thành ba bài tổng hợp. Bàn cờ hiển thị đầy đủ 32 quân ở vị trí khai cuộc; bạn có thể chọn quân Đỏ để xem và thực hành các nước đi hợp lệ của Xe, Pháo, Mã, Tượng, Sĩ, Tướng và Binh.", "Thực hành đầy đủ 32 quân và kiểm tra toàn bộ luật di chuyển.", 13, "board", 2, "Tổng kết Module 2: Bàn cờ toàn diện" });

            migrationBuilder.InsertData(
                table: "CourseQuizQuestions",
                columns: new[] { "Id", "CorrectAnswer", "DisplayOrder", "Explanation", "LessonId", "OptionsJson", "Question" },
                values: new object[,]
                {
                    { 40, 1, 1, "Mỗi bên có 16 quân, cả bàn cờ có 32 quân.", 19, "[\"16\",\"32\",\"36\"]", "Bàn cờ khai cuộc có bao nhiêu quân?" },
                    { 41, 0, 2, "Xe đi ngang hoặc dọc trên các đường trống.", 19, "[\"Ngang hoặc dọc, không xuyên quân\",\"Chéo hai ô\",\"Theo hình chữ L\"]", "Xe đi như thế nào?" },
                    { 42, 0, 3, "Pháo đi như Xe khi di chuyển đến ô trống.", 19, "[\"Như Xe khi không ăn\",\"Chéo một ô\",\"Nhảy qua hai quân\"]", "Pháo đi thường như thế nào?" },
                    { 43, 1, 4, "Pháo phải nhảy qua đúng một quân làm ngòi.", 19, "[\"Không có\",\"Đúng một\",\"Ít nhất hai\"]", "Pháo cần bao nhiêu ngòi để ăn quân?" },
                    { 44, 0, 5, "Mã đi hai ô theo một hướng rồi một ô vuông góc.", 19, "[\"Chữ L\",\"Đường thẳng\",\"Đường chéo một ô\"]", "Mã di chuyển theo hình nào?" },
                    { 45, 0, 6, "Quân ở ô chân Mã sẽ chặn hướng đi đó.", 19, "[\"Bị cản chân Mã\",\"Có sông trên bàn\",\"Đứng trong cung\"]", "Điều gì làm Mã không đi được một hướng?" },
                    { 46, 1, 7, "Tượng đi chéo đúng hai ô và không được qua sông.", 19, "[\"Một ô ngang\",\"Hai ô chéo\",\"Ba ô dọc\"]", "Tượng đi bao nhiêu ô mỗi nước?" },
                    { 47, 0, 8, "Nếu mắt Tượng bị chặn thì hướng đó không hợp lệ.", 19, "[\"Mắt Tượng\",\"Ngòi Pháo\",\"Cung tướng\"]", "Điểm giữa đường chéo của Tượng được gọi là gì?" },
                    { 48, 0, 9, "Sĩ chỉ đi chéo một ô trong cung tướng.", 19, "[\"Trong cung, chéo một ô\",\"Qua sông, ngang hai ô\",\"Toàn bàn, dọc không giới hạn\"]", "Sĩ được đi ở đâu?" },
                    { 49, 0, 10, "Tướng chỉ đi ngang hoặc dọc một ô trong cung 3x3.", 19, "[\"Ngang hoặc dọc một ô trong cung\",\"Chéo hai ô toàn bàn\",\"Theo hình chữ L\"]", "Tướng được đi như thế nào?" },
                    { 50, 0, 11, "Binh chỉ đi thẳng về phía trước trước khi qua sông.", 19, "[\"Đi thẳng một ô\",\"Đi lùi một ô\",\"Đi chéo hai ô\"]", "Binh trước khi qua sông được đi thế nào?" },
                    { 51, 0, 12, "Sau khi qua sông, Binh được đi ngang nhưng không được đi lùi.", 19, "[\"Đi ngang một ô\",\"Đi lùi một ô\",\"Nhảy qua quân\"]", "Binh sau khi qua sông được thêm nước đi nào?" },
                    { 52, 1, 13, "Quân cùng màu chiếm ô đích sẽ chặn nước đi.", 19, "[\"Có\",\"Không\",\"Chỉ khi chiếu tướng\"]", "Quân Đỏ có thể đi vào ô đang có quân Đỏ không?" },
                    { 53, 0, 14, "Quân bị ăn được loại khỏi trạng thái bàn cờ.", 19, "[\"Bị loại khỏi bàn\",\"Đổi thành quân Đỏ\",\"Đứng chồng lên quân Đỏ\"]", "Khi quân Đỏ ăn quân Đen, quân Đen sẽ thế nào?" },
                    { 54, 0, 15, "Hai Tướng không được đối mặt trực tiếp; phải có quân chắn giữa.", 19, "[\"Ít nhất một quân chắn\",\"Không có quân nào\",\"Một ô trống duy nhất\"]", "Hai Tướng cùng cột phải có gì ở giữa?" },
                    { 55, 0, 16, "Bàn cờ thông báo hoàn thành và khóa tương tác khi không còn quân Đen.", 19, "[\"Hiển thị thông báo kết thúc\",\"Tự động quay về Module 1\",\"Cho quân Đen xuất hiện lại\"]", "Khi quân Đen bị ăn hết trong bài thực hành, điều gì xảy ra?" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 19);
        }
    }
}
