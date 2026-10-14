using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleTwoSynthesisLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CourseLessons",
                columns: new[] { "Id", "BoardPosition", "Content", "Description", "DisplayOrder", "LessonType", "ModuleId", "Title" },
                values: new object[,]
                {
                    { 16, null, "Bài tổng hợp 1 gồm các câu hỏi lý thuyết về nhận diện quân, vị trí khai cuộc và hướng di chuyển cơ bản.", "Ôn tập vị trí, ký hiệu và luật đi của bảy loại quân.", 10, "board", 2, "Tổng hợp 1: Nhận diện và luật đi" },
                    { 17, null, "Bài tổng hợp 2 kiểm tra cách xử lý vật cản, ăn quân đúng luật và các quy tắc đặc biệt.", "Ôn tập vật cản, ngòi Pháo, chân Mã và điều kiện ăn quân.", 11, "board", 2, "Tổng hợp 2: Cản đường và ăn quân" },
                    { 18, null, "Bài tổng hợp 3 kết hợp kiến thức về toàn bộ quân cờ, khai cuộc và các tình huống thực hành.", "Ôn tập phối hợp Xe, Pháo, Mã và các quân phòng thủ.", 12, "board", 2, "Tổng hợp 3: Phối hợp thực chiến" }
                });

            migrationBuilder.InsertData(
                table: "CourseQuizQuestions",
                columns: new[] { "Id", "CorrectAnswer", "DisplayOrder", "Explanation", "LessonId", "OptionsJson", "Question" },
                values: new object[,]
                {
                    { 17, 1, 1, "Bàn cờ có 9 đường dọc.", 16, "[\"8\",\"9\",\"10\"]", "Bàn cờ Tướng có bao nhiêu đường dọc?" },
                    { 18, 0, 2, "Mã đi theo hình chữ L.", 16, "[\"Mã\",\"Tượng\",\"Sĩ\"]", "Quân nào đi chữ L?" },
                    { 19, 2, 3, "Tướng Đỏ ở hàng cuối, cột giữa.", 16, "[\"Hàng 1 cột 4\",\"Hàng 10 cột 5\",\"Hàng 10 cột 4\"]", "Tướng Đỏ bắt đầu ở đâu?" },
                    { 20, 0, 4, "Sĩ đi chéo một ô trong cung.", 16, "[\"Chéo 1 ô trong cung\",\"Ngang 2 ô\",\"Dọc không giới hạn\"]", "Sĩ di chuyển như thế nào?" },
                    { 21, 1, 5, "Tượng không được qua sông.", 16, "[\"Có\",\"Không\",\"Chỉ khi ăn quân\"]", "Tượng có được qua sông không?" },
                    { 22, 0, 6, "Binh được đi ngang sau khi qua sông.", 16, "[\"Có\",\"Không\",\"Chỉ đi chéo\"]", "Binh sau khi qua sông được đi ngang không?" },
                    { 23, 0, 7, "Xe đi ngang hoặc dọc trên đường trống.", 16, "[\"Xe\",\"Mã\",\"Tốt\"]", "Quân nào đi ngang dọc không giới hạn?" },
                    { 24, 1, 1, "Xe bị chặn bởi quân trên đường đi.", 17, "[\"Có\",\"Không\",\"Chỉ khi chiếu\"]", "Xe có được đi xuyên qua quân khác không?" },
                    { 25, 1, 2, "Pháo phải có đúng một quân làm ngòi.", 17, "[\"Không cần\",\"Đúng một\",\"Hai hoặc hơn\"]", "Pháo ăn quân cần bao nhiêu ngòi?" },
                    { 26, 0, 3, "Quân đứng ở chân Mã chặn hướng đi đó.", 17, "[\"Chân Mã\",\"Mắt Tượng\",\"Cung\"]", "Mã bị chặn bởi bộ phận nào?" },
                    { 27, 0, 4, "Quân ở điểm giữa đường chéo chặn mắt Tượng.", 17, "[\"Mắt Tượng\",\"Chân Mã\",\"Ngòi\"]", "Tượng bị chặn bởi gì?" },
                    { 28, 1, 5, "Không thể đi vào ô đang có quân cùng phe.", 17, "[\"Có thể ăn\",\"Không được đi vào\",\"Được nhảy qua\"]", "Quân cùng màu ở ô đích xử lý thế nào?" },
                    { 29, 1, 6, "Binh không bao giờ được đi lùi.", 17, "[\"Có\",\"Không\",\"Chỉ sau khi qua sông\"]", "Binh có được đi lùi không?" },
                    { 30, 1, 7, "Sĩ chỉ hoạt động trong cung.", 17, "[\"Có\",\"Không\",\"Chỉ khi ăn quân\"]", "Sĩ có được ra khỏi cung không?" },
                    { 31, 0, 8, "Pháo cần đúng một ngòi khi ăn.", 17, "[\"Pháo\",\"Mã\",\"Tượng\"]", "Quân nào có thể ăn theo đường thẳng nhưng cần ngòi?" },
                    { 32, 0, 1, "Xe mở đường và quân làm ngòi hỗ trợ Pháo.", 18, "[\"Đường đi và ngòi Pháo\",\"Hai quân cùng ô\",\"Không có quân chắn nào\"]", "Phối hợp Xe và Pháo cần tạo điều kiện gì?" },
                    { 33, 0, 2, "Mã cần ô chân thông thoáng.", 18, "[\"Khi không bị cản chân\",\"Khi ở ngoài bàn\",\"Khi bị chặn\"]", "Mã hỗ trợ tốt nhất khi nào?" },
                    { 34, 0, 3, "Hai Tướng không được đối mặt trực tiếp.", 18, "[\"Có quân chắn giữa\",\"Không có quân chắn\",\"Được nhìn nhau\"]", "Hai Tướng cùng cột cần điều kiện gì?" },
                    { 35, 0, 4, "Các quân phối hợp để tạo ưu thế.", 18, "[\"Tạo ưu thế và hỗ trợ nhau\",\"Di chuyển một quân mãi\",\"Bỏ qua Tướng\"]", "Mục tiêu của phối hợp quân là gì?" },
                    { 36, 1, 5, "Quân bị ăn được loại khỏi trạng thái bàn cờ.", 18, "[\"Vẫn ở ô cũ\",\"Bị loại khỏi bàn\",\"Chuyển sang bên kia\"]", "Khi ăn quân, quân bị ăn sẽ ở đâu?" },
                    { 37, 0, 6, "Pháo Đỏ bắt đầu ở hàng áp chót, cột 1 và 7 theo chỉ số 0.", 18, "[\"Hàng 8 cột 2 và 8\",\"Hàng 10 cột 2 và 8\",\"Hàng 7 cột 1 và 7\"]", "Vị trí khai cuộc của Pháo Đỏ là gì?" },
                    { 38, 0, 7, "Tượng chỉ hoạt động ở nửa bàn của mình.", 18, "[\"Tượng\",\"Xe\",\"Mã\"]", "Quân nào không được qua sông?" },
                    { 39, 1, 8, "Không được để hai Tướng đối mặt cùng cột.", 18, "[\"Hợp lệ\",\"Không hợp lệ\",\"Luôn là chiếu hết\"]", "Nước đi làm lộ mặt Tướng là gì?" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "CourseQuizQuestions",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "CourseLessons",
                keyColumn: "Id",
                keyValue: 18);
        }
    }
}
