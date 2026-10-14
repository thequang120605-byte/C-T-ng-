START TRANSACTION;
-- The existing database already contains the pre-course schema, but its
-- migration history is incomplete. These rows baseline those existing migrations
-- so EF Core does not try to recreate Games, Users, or Puzzles.
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES
('20260907151941_InitialCreate', '9.0.20'),
('20261008204222_Phase2_AuthAndGameUsers', '9.0.20'),
('20261009002326_Phase3_Phase4_InitialFen_Puzzle', '9.0.20'),
('20261009004435_SeedPuzzles', '9.0.20'),
('20261009060552_PuzzleProgress', '9.0.20'),
('20261013120000_AddGameRoomCode', '9.0.20');
CREATE TABLE `CourseModules` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Icon` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_CourseModules` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `CourseLessons` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ModuleId` int NOT NULL,
    `Title` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Content` longtext CHARACTER SET utf8mb4 NOT NULL,
    `LessonType` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    `BoardPosition` longtext CHARACTER SET utf8mb4 NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_CourseLessons` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CourseLessons_CourseModules_ModuleId` FOREIGN KEY (`ModuleId`) REFERENCES `CourseModules` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `CourseQuizQuestions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `LessonId` int NOT NULL,
    `Question` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `OptionsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CorrectAnswer` int NOT NULL,
    `Explanation` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayOrder` int NOT NULL,
    CONSTRAINT `PK_CourseQuizQuestions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CourseQuizQuestions_CourseLessons_LessonId` FOREIGN KEY (`LessonId`) REFERENCES `CourseLessons` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `UserCourseProgresses` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `LessonId` int NOT NULL,
    `IsCompleted` tinyint(1) NOT NULL,
    `BestQuizScore` int NOT NULL,
    `QuizAttempts` int NOT NULL,
    `LastPosition` int NOT NULL,
    `StartedAt` datetime(6) NOT NULL,
    `CompletedAt` datetime(6) NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_UserCourseProgresses` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_UserCourseProgresses_CourseLessons_LessonId` FOREIGN KEY (`LessonId`) REFERENCES `CourseLessons` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserCourseProgresses_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

INSERT INTO `CourseModules` (`Id`, `Description`, `DisplayOrder`, `Icon`, `Title`)
VALUES (1, 'Làm quen với bàn cờ, cung, sông và các quân cờ.', 1, 'fa-seedling', 'Vỡ lòng & Làm quen'),
(2, 'Nắm vững cách đi và các quy tắc đặc biệt của từng quân.', 2, 'fa-arrows-up-down-left-right', 'Cách di chuyển & Ăn quân'),
(3, 'Hiểu chiếu tướng, chiếu hết, hòa cờ và cách ghi biên bản.', 3, 'fa-scale-balanced', 'Luật chơi & Nguyên tắc cơ bản'),
(4, 'Phát triển quân, phối hợp lực lượng và đổi quân có lợi.', 4, 'fa-chess-knight', 'Chiến thuật cơ bản & Trung cuộc');

INSERT INTO `CourseLessons` (`Id`, `BoardPosition`, `Content`, `Description`, `DisplayOrder`, `LessonType`, `ModuleId`, `Title`)
VALUES (1, NULL, 'Bàn cờ Tướng có 9 đường dọc và 10 đường ngang, ở giữa là con sông. Mỗi bên có một cung 3x3 để Tướng và Sĩ hoạt động.', 'Sông, cung tướng và đường đi trên bàn cờ.', 1, 'board', 1, 'Bàn cờ và các khu vực'),
(2, NULL, 'Bảy loại quân gồm Tướng, Sĩ, Tượng, Xe, Pháo, Mã và Binh/Chốt. Quân Đỏ và quân Đen có mặt chữ khác nhau nhưng cách đi tương ứng.', 'Tên gọi, mặt chữ và giá trị tương đối của các quân.', 2, 'theory', 1, 'Nhận biết 7 loại quân'),
(3, NULL, 'Xe đi ngang hoặc dọc không giới hạn. Pháo đi như Xe nhưng khi ăn phải có đúng một quân làm ngòi. Mã đi chữ L và có thể bị cản chân.', 'Ba quân chủ lực và các quy tắc ăn quân.', 1, 'board', 2, 'Xe, Pháo và Mã'),
(4, NULL, 'Tướng đi một ô trong cung. Sĩ đi chéo trong cung. Tượng đi chéo hai ô và không được qua sông. Binh đi thẳng, qua sông được đi ngang.', 'Các quân phòng thủ và quy tắc qua sông.', 2, 'board', 2, 'Tướng, Sĩ, Tượng và Binh'),
(5, NULL, 'Hai Tướng không được nhìn thẳng nhau trên cùng một cột nếu không có quân nào ở giữa. Nước đi làm lộ mặt Tướng là nước đi không hợp lệ.', 'Quy tắc lộ diện tướng cần ghi nhớ.', 3, 'board', 2, 'Tướng đối mặt'),
(6, NULL, 'Chiếu là khi Tướng đang bị đe dọa bắt ở nước kế tiếp. Chiếu hết là khi bên bị chiếu không còn nước hợp lệ để thoát chiếu.', 'Nhận biết tình huống bị chiếu và kết thúc ván.', 1, 'theory', 3, 'Chiếu tướng và chiếu hết'),
(7, NULL, 'Bắt bí là không còn nước hợp lệ nhưng Tướng không bị chiếu. Khi ghi biên bản, hãy ghi số nước, quân đi, hướng đi và ô đến theo quy ước của ván.', 'Bắt bí, các trường hợp hòa và ký hiệu nước đi.', 2, 'theory', 3, 'Hòa cờ và ghi biên bản'),
(8, NULL, 'Ưu tiên đưa các quân hoạt động tốt, mở đường cho Xe, không di chuyển một quân quá nhiều lần và luôn chú ý an toàn của Tướng.', 'Kiểm soát trung tâm và phát triển quân.', 1, 'theory', 4, 'Nguyên tắc khai cuộc'),
(9, NULL, 'Xe, Pháo, Mã nên hỗ trợ lẫn nhau. Chỉ đổi quân khi có lợi về vị trí, vật chất hoặc làm giảm áp lực lên Tướng của bạn.', 'Xây dựng kế hoạch trung cuộc vững chắc.', 2, 'theory', 4, 'Phối hợp và đổi quân');

INSERT INTO `CourseQuizQuestions` (`Id`, `CorrectAnswer`, `DisplayOrder`, `Explanation`, `LessonId`, `OptionsJson`, `Question`)
VALUES (1, 1, 1, 'Con sông nằm giữa hai bên và chia bàn cờ thành hai nửa.', 1, '["Cung tướng","Con sông","Đường biên"]', 'Khu vực nào nằm giữa hai bên bàn cờ?'),
(2, 1, 1, 'Xe đi thẳng ngang hoặc dọc trên các đường trống.', 2, '["Mã","Xe","Tượng"]', 'Quân nào có thể đi ngang hoặc dọc không giới hạn?'),
(3, 1, 1, 'Pháo phải nhảy qua đúng một quân làm ngòi để ăn.', 3, '["Hai ngòi","Đúng một ngòi","Không cần ngòi"]', 'Pháo cần gì khi ăn quân?'),
(4, 1, 1, 'Tượng chỉ hoạt động ở phần sân của mình.', 4, '["Có","Không","Chỉ khi chiếu tướng"]', 'Tượng có được qua sông không?'),
(5, 0, 1, 'Phát triển quân và kiểm soát trung tâm giúp các quân phối hợp tốt hơn.', 8, '["Phát triển quân và kiểm soát trung tâm","Di chuyển một quân liên tục","Bỏ qua an toàn của Tướng"]', 'Đâu là nguyên tắc khai cuộc tốt?');

CREATE UNIQUE INDEX `IX_CourseLessons_ModuleId_DisplayOrder` ON `CourseLessons` (`ModuleId`, `DisplayOrder`);

CREATE UNIQUE INDEX `IX_CourseModules_DisplayOrder` ON `CourseModules` (`DisplayOrder`);

CREATE UNIQUE INDEX `IX_CourseQuizQuestions_LessonId_DisplayOrder` ON `CourseQuizQuestions` (`LessonId`, `DisplayOrder`);

CREATE INDEX `IX_UserCourseProgresses_LessonId` ON `UserCourseProgresses` (`LessonId`);

CREATE UNIQUE INDEX `IX_UserCourseProgresses_UserId_LessonId` ON `UserCourseProgresses` (`UserId`, `LessonId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261013154955_AddChessFundamentalsCourse', '9.0.20');

COMMIT;
