using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChessCore.Migrations
{
    /// <inheritdoc />
    public partial class AddChessFundamentalsCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Icon = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseModules", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CourseLessons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ModuleId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LessonType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BoardPosition = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseLessons_CourseModules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "CourseModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CourseQuizQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OptionsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorrectAnswer = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseQuizQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseQuizQuestions_CourseLessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "CourseLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserCourseProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    BestQuizScore = table.Column<int>(type: "int", nullable: false),
                    QuizAttempts = table.Column<int>(type: "int", nullable: false),
                    LastPosition = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCourseProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCourseProgresses_CourseLessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "CourseLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCourseProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "CourseModules",
                columns: new[] { "Id", "Description", "DisplayOrder", "Icon", "Title" },
                values: new object[,]
                {
                    { 1, "Làm quen với bàn cờ, cung, sông và các quân cờ.", 1, "fa-seedling", "Vỡ lòng & Làm quen" },
                    { 2, "Nắm vững cách đi và các quy tắc đặc biệt của từng quân.", 2, "fa-arrows-up-down-left-right", "Cách di chuyển & Ăn quân" },
                    { 3, "Hiểu chiếu tướng, chiếu hết, hòa cờ và cách ghi biên bản.", 3, "fa-scale-balanced", "Luật chơi & Nguyên tắc cơ bản" },
                    { 4, "Phát triển quân, phối hợp lực lượng và đổi quân có lợi.", 4, "fa-chess-knight", "Chiến thuật cơ bản & Trung cuộc" }
                });

            migrationBuilder.InsertData(
                table: "CourseLessons",
                columns: new[] { "Id", "BoardPosition", "Content", "Description", "DisplayOrder", "LessonType", "ModuleId", "Title" },
                values: new object[,]
                {
                    { 1, null, "Bàn cờ Tướng có 9 đường dọc và 10 đường ngang, ở giữa là con sông. Mỗi bên có một cung 3x3 để Tướng và Sĩ hoạt động.", "Sông, cung tướng và đường đi trên bàn cờ.", 1, "board", 1, "Bàn cờ và các khu vực" },
                    { 2, null, "Bảy loại quân gồm Tướng, Sĩ, Tượng, Xe, Pháo, Mã và Binh/Chốt. Quân Đỏ và quân Đen có mặt chữ khác nhau nhưng cách đi tương ứng.", "Tên gọi, mặt chữ và giá trị tương đối của các quân.", 2, "theory", 1, "Nhận biết 7 loại quân" },
                    { 3, null, "Xe đi ngang hoặc dọc không giới hạn. Pháo đi như Xe nhưng khi ăn phải có đúng một quân làm ngòi. Mã đi chữ L và có thể bị cản chân.", "Ba quân chủ lực và các quy tắc ăn quân.", 1, "board", 2, "Xe, Pháo và Mã" },
                    { 4, null, "Tướng đi một ô trong cung. Sĩ đi chéo trong cung. Tượng đi chéo hai ô và không được qua sông. Binh đi thẳng, qua sông được đi ngang.", "Các quân phòng thủ và quy tắc qua sông.", 2, "board", 2, "Tướng, Sĩ, Tượng và Binh" },
                    { 5, null, "Hai Tướng không được nhìn thẳng nhau trên cùng một cột nếu không có quân nào ở giữa. Nước đi làm lộ mặt Tướng là nước đi không hợp lệ.", "Quy tắc lộ diện tướng cần ghi nhớ.", 3, "board", 2, "Tướng đối mặt" },
                    { 6, null, "Chiếu là khi Tướng đang bị đe dọa bắt ở nước kế tiếp. Chiếu hết là khi bên bị chiếu không còn nước hợp lệ để thoát chiếu.", "Nhận biết tình huống bị chiếu và kết thúc ván.", 1, "theory", 3, "Chiếu tướng và chiếu hết" },
                    { 7, null, "Bắt bí là không còn nước hợp lệ nhưng Tướng không bị chiếu. Khi ghi biên bản, hãy ghi số nước, quân đi, hướng đi và ô đến theo quy ước của ván.", "Bắt bí, các trường hợp hòa và ký hiệu nước đi.", 2, "theory", 3, "Hòa cờ và ghi biên bản" },
                    { 8, null, "Ưu tiên đưa các quân hoạt động tốt, mở đường cho Xe, không di chuyển một quân quá nhiều lần và luôn chú ý an toàn của Tướng.", "Kiểm soát trung tâm và phát triển quân.", 1, "theory", 4, "Nguyên tắc khai cuộc" },
                    { 9, null, "Xe, Pháo, Mã nên hỗ trợ lẫn nhau. Chỉ đổi quân khi có lợi về vị trí, vật chất hoặc làm giảm áp lực lên Tướng của bạn.", "Xây dựng kế hoạch trung cuộc vững chắc.", 2, "theory", 4, "Phối hợp và đổi quân" }
                });

            migrationBuilder.InsertData(
                table: "CourseQuizQuestions",
                columns: new[] { "Id", "CorrectAnswer", "DisplayOrder", "Explanation", "LessonId", "OptionsJson", "Question" },
                values: new object[,]
                {
                    { 1, 1, 1, "Con sông nằm giữa hai bên và chia bàn cờ thành hai nửa.", 1, "[\"Cung tướng\",\"Con sông\",\"Đường biên\"]", "Khu vực nào nằm giữa hai bên bàn cờ?" },
                    { 2, 1, 1, "Xe đi thẳng ngang hoặc dọc trên các đường trống.", 2, "[\"Mã\",\"Xe\",\"Tượng\"]", "Quân nào có thể đi ngang hoặc dọc không giới hạn?" },
                    { 3, 1, 1, "Pháo phải nhảy qua đúng một quân làm ngòi để ăn.", 3, "[\"Hai ngòi\",\"Đúng một ngòi\",\"Không cần ngòi\"]", "Pháo cần gì khi ăn quân?" },
                    { 4, 1, 1, "Tượng chỉ hoạt động ở phần sân của mình.", 4, "[\"Có\",\"Không\",\"Chỉ khi chiếu tướng\"]", "Tượng có được qua sông không?" },
                    { 5, 0, 1, "Phát triển quân và kiểm soát trung tâm giúp các quân phối hợp tốt hơn.", 8, "[\"Phát triển quân và kiểm soát trung tâm\",\"Di chuyển một quân liên tục\",\"Bỏ qua an toàn của Tướng\"]", "Đâu là nguyên tắc khai cuộc tốt?" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseLessons_ModuleId_DisplayOrder",
                table: "CourseLessons",
                columns: new[] { "ModuleId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseModules_DisplayOrder",
                table: "CourseModules",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseQuizQuestions_LessonId_DisplayOrder",
                table: "CourseQuizQuestions",
                columns: new[] { "LessonId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCourseProgresses_LessonId",
                table: "UserCourseProgresses",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCourseProgresses_UserId_LessonId",
                table: "UserCourseProgresses",
                columns: new[] { "UserId", "LessonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseQuizQuestions");

            migrationBuilder.DropTable(
                name: "UserCourseProgresses");

            migrationBuilder.DropTable(
                name: "CourseLessons");

            migrationBuilder.DropTable(
                name: "CourseModules");
        }
    }
}
