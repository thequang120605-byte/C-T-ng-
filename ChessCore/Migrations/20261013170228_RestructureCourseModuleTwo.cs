using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class RestructureCourseModuleTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Content", "Description", "Title" },
                values: new object[] { "Xe đi ngang hoặc dọc không giới hạn, nhưng không được đi xuyên qua quân khác. Quân Đỏ bắt đầu ở hàng cuối, cột 0 và 8.", "Nhận biết Xe và thực hành đường đi của Xe.", "Xe: đi ngang và dọc" });

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Content", "Description", "Title" },
                values: new object[] { "Pháo đi như Xe khi không ăn. Khi ăn, Pháo phải nhảy qua đúng một quân làm ngòi.", "Phân biệt nước đi thường và nước ăn của Pháo.", "Pháo: đi và ăn qua ngòi" });

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Content", "Description", "Title" },
                values: new object[] { "Mã đi theo hình chữ L. Nếu chân Mã bị chặn, hướng đó không đi được. Mã Đỏ bắt đầu ở hàng cuối, cột 1 và 7.", "Thực hành tám hướng đi của Mã.", "Mã: chữ L và cản chân" });

            migrationBuilder.InsertData(
                table: "CourseLessons",
                columns: new[] { "Id", "BoardPosition", "Content", "Description", "DisplayOrder", "LessonType", "ModuleId", "Title" },
                values: new object[,]
                {
                    { 10, null, "Tướng đi ngang hoặc dọc một ô trong cung 3x3. Tướng Đỏ bắt đầu ở hàng 9, cột 4.", "Học giới hạn cung và cách đi của Tướng.", 4, "board", 2, "Tướng: một bước trong cung" },
                    { 11, null, "Sĩ đi chéo một ô và chỉ hoạt động trong cung. Sĩ Đỏ bắt đầu ở hàng 9, cột 3 và 5.", "Học bốn đường chéo hợp lệ của Sĩ.", 5, "board", 2, "Sĩ: đi chéo trong cung" },
                    { 12, null, "Tượng đi chéo hai ô, không được qua sông và không được đi khi mắt Tượng bị chặn.", "Học mắt Tượng và giới hạn qua sông.", 6, "board", 2, "Tượng: chéo hai ô" },
                    { 13, null, "Binh đi thẳng trước khi qua sông. Sau khi qua sông, Binh được đi ngang nhưng không đi lùi.", "Thực hành Binh trước và sau khi qua sông.", 7, "board", 2, "Binh: qua sông đổi cách đi" },
                    { 14, null, "Luyện phối hợp Xe và Pháo từ đúng vị trí khai cuộc của quân Đỏ.", "Ghép hai quân chủ lực trên thế khai cuộc.", 8, "board", 2, "Kết hợp Xe và Pháo" },
                    { 15, null, "Sau khi nắm từng quân, hãy phối hợp Xe, Pháo và Mã để mở đường và ăn quân.", "Bài thực hành phối hợp ba quân.", 9, "board", 2, "Kết hợp Xe, Pháo và Mã" }
                });

            migrationBuilder.UpdateData(
                table: "CourseModules",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Title" },
                values: new object[] { "Học từng quân riêng lẻ, sau đó luyện phối hợp 2–3 quân trên thế khai cuộc chuẩn.", "Nhận biết quân & Nước đi" });

            migrationBuilder.UpdateData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Explanation", "OptionsJson", "Question" },
                values: new object[] { "Xe đi ngang hoặc dọc trên các đường trống.", "[\"Chéo\",\"Ngang hoặc dọc\",\"Theo hình chữ L\"]", "Xe đi như thế nào?" });

            migrationBuilder.UpdateData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Explanation", "OptionsJson", "Question" },
                values: new object[] { "Pháo phải nhảy qua đúng một quân làm ngòi để ăn.", "[\"Hai ngòi\",\"Đúng một ngòi\",\"Không cần ngòi\"]", "Pháo cần gì khi ăn quân?" });

            migrationBuilder.UpdateData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CorrectAnswer", "Explanation", "OptionsJson", "Question" },
                values: new object[] { 0, "Mã không đi được nếu chân Mã bị chặn.", "[\"Chân Mã\",\"Mắt Tượng\",\"Cung tướng\"]", "Mã bị cản bởi gì?" });

            migrationBuilder.InsertData(
                table: "CourseQuizQuestions",
                columns: new[] { "Id", "CorrectAnswer", "DisplayOrder", "Explanation", "LessonId", "OptionsJson", "Question" },
                values: new object[,]
                {
                    { 11, 1, 1, "Tướng chỉ đi ngang hoặc dọc một ô trong cung.", 10, "[\"Ngoài bàn cờ\",\"Trong cung 3x3\",\"Qua sông\"]", "Tướng được đi ở đâu?" },
                    { 12, 0, 1, "Sĩ đi chéo một ô và không ra khỏi cung.", 11, "[\"Chéo một ô trong cung\",\"Ngang không giới hạn\",\"Chéo hai ô qua sông\"]", "Sĩ đi như thế nào?" },
                    { 13, 1, 1, "Tượng chỉ hoạt động ở phần sân của mình.", 12, "[\"Có\",\"Không\",\"Chỉ khi chiếu tướng\"]", "Tượng có được qua sông không?" },
                    { 14, 1, 1, "Sau khi qua sông, Binh được đi ngang nhưng không đi lùi.", 13, "[\"Đi lùi\",\"Đi ngang\",\"Đi chéo hai ô\"]", "Binh sau khi qua sông được đi thêm hướng nào?" },
                    { 15, 0, 1, "Xe mở đường và quân làm ngòi giúp Pháo ăn đúng luật.", 14, "[\"Mở đường và tạo ngòi cho Pháo\",\"Đặt hai quân cùng một ô\",\"Cho Xe đi chéo\"]", "Khi phối hợp Xe và Pháo cần chú ý gì?" },
                    { 16, 0, 1, "Xe, Pháo và Mã cần phối hợp thay vì hoạt động rời rạc.", 15, "[\"Cho các quân hỗ trợ lẫn nhau\",\"Chỉ di chuyển Tướng\",\"Không được ăn quân\"]", "Mục tiêu của bài phối hợp ba quân là gì?" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Content", "Description", "Title" },
                values: new object[] { "Xe đi ngang hoặc dọc không giới hạn. Pháo đi như Xe nhưng khi ăn phải có đúng một quân làm ngòi. Mã đi chữ L và có thể bị cản chân.", "Ba quân chủ lực và các quy tắc ăn quân.", "Xe, Pháo và Mã" });

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Content", "Description", "Title" },
                values: new object[] { "Tướng đi một ô trong cung. Sĩ đi chéo trong cung. Tượng đi chéo hai ô và không được qua sông. Binh đi thẳng, qua sông được đi ngang.", "Các quân phòng thủ và quy tắc qua sông.", "Tướng, Sĩ, Tượng và Binh" });

            migrationBuilder.UpdateData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Content", "Description", "Title" },
                values: new object[] { "Hai Tướng không được nhìn thẳng nhau trên cùng một cột nếu không có quân nào ở giữa. Nước đi làm lộ mặt Tướng là nước đi không hợp lệ.", "Quy tắc lộ diện tướng cần ghi nhớ.", "Tướng đối mặt" });

            migrationBuilder.UpdateData(
                table: "CourseModules",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Title" },
                values: new object[] { "Nắm vững cách đi và các quy tắc đặc biệt của từng quân.", "Cách di chuyển & Ăn quân" });

            migrationBuilder.UpdateData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Explanation", "OptionsJson", "Question" },
                values: new object[] { "Pháo phải nhảy qua đúng một quân làm ngòi để ăn.", "[\"Hai ngòi\",\"Đúng một ngòi\",\"Không cần ngòi\"]", "Pháo cần gì khi ăn quân?" });

            migrationBuilder.UpdateData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Explanation", "OptionsJson", "Question" },
                values: new object[] { "Tượng chỉ hoạt động ở phần sân của mình.", "[\"Có\",\"Không\",\"Chỉ khi chiếu tướng\"]", "Tượng có được qua sông không?" });

            migrationBuilder.UpdateData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CorrectAnswer", "Explanation", "OptionsJson", "Question" },
                values: new object[] { 1, "Giữa hai Tướng phải có ít nhất một quân chắn.", "[\"Có, nếu cùng một cột\",\"Không, phải có quân chắn ở giữa\",\"Chỉ được khi đang chiếu\"]", "Hai Tướng có được nhìn thẳng nhau không?" });
        }
    }
}
