SET NAMES utf8mb4;
USE `chess_db`;

UPDATE `CourseModules` SET
    `Title` = 'Vỡ lòng & Làm quen',
    `Description` = 'Làm quen với bàn cờ, cung, sông và các quân cờ.'
WHERE `Id` = 1;
UPDATE `CourseModules` SET
    `Title` = 'Cách di chuyển & Ăn quân',
    `Description` = 'Nắm vững cách đi và các quy tắc đặc biệt của từng quân.'
WHERE `Id` = 2;
UPDATE `CourseModules` SET
    `Title` = 'Luật chơi & Nguyên tắc cơ bản',
    `Description` = 'Hiểu chiếu tướng, chiếu hết, hòa cờ và cách ghi biên bản.'
WHERE `Id` = 3;
UPDATE `CourseModules` SET
    `Title` = 'Chiến thuật cơ bản & Trung cuộc',
    `Description` = 'Phát triển quân, phối hợp lực lượng và đổi quân có lợi.'
WHERE `Id` = 4;

UPDATE `CourseLessons` SET
    `Title` = 'Bàn cờ và các khu vực',
    `Description` = 'Sông, cung tướng và đường đi trên bàn cờ.',
    `Content` = 'Bàn cờ Tướng có 9 đường dọc và 10 đường ngang, ở giữa là con sông. Mỗi bên có một cung 3x3 để Tướng và Sĩ hoạt động.'
WHERE `Id` = 1;
UPDATE `CourseLessons` SET
    `Title` = 'Nhận biết 7 loại quân',
    `Description` = 'Tên gọi, mặt chữ và giá trị tương đối của các quân.',
    `Content` = 'Bảy loại quân gồm Tướng, Sĩ, Tượng, Xe, Pháo, Mã và Binh/Chốt. Quân Đỏ và quân Đen có mặt chữ khác nhau nhưng cách đi tương ứng.'
WHERE `Id` = 2;
UPDATE `CourseLessons` SET
    `Title` = 'Xe, Pháo và Mã',
    `Description` = 'Ba quân chủ lực và các quy tắc ăn quân.',
    `Content` = 'Xe đi ngang hoặc dọc không giới hạn. Pháo đi như Xe nhưng khi ăn phải có đúng một quân làm ngòi. Mã đi chữ L và có thể bị cản chân.'
WHERE `Id` = 3;
UPDATE `CourseLessons` SET
    `Title` = 'Tướng, Sĩ, Tượng và Binh',
    `Description` = 'Các quân phòng thủ và quy tắc qua sông.',
    `Content` = 'Tướng đi một ô trong cung. Sĩ đi chéo trong cung. Tượng đi chéo hai ô và không được qua sông. Binh đi thẳng, qua sông được đi ngang.'
WHERE `Id` = 4;
UPDATE `CourseLessons` SET
    `Title` = 'Tướng đối mặt',
    `Description` = 'Quy tắc lộ diện tướng cần ghi nhớ.',
    `Content` = 'Hai Tướng không được nhìn thẳng nhau trên cùng một cột nếu không có quân nào ở giữa. Nước đi làm lộ mặt Tướng là nước đi không hợp lệ.'
WHERE `Id` = 5;
UPDATE `CourseLessons` SET
    `Title` = 'Chiếu tướng và chiếu hết',
    `Description` = 'Nhận biết tình huống bị chiếu và kết thúc ván.',
    `Content` = 'Chiếu là khi Tướng đang bị đe dọa bắt ở nước kế tiếp. Chiếu hết là khi bên bị chiếu không còn nước hợp lệ để thoát chiếu.'
WHERE `Id` = 6;
UPDATE `CourseLessons` SET
    `Title` = 'Hòa cờ và ghi biên bản',
    `Description` = 'Bắt bí, các trường hợp hòa và ký hiệu nước đi.',
    `Content` = 'Bắt bí là không còn nước hợp lệ nhưng Tướng không bị chiếu. Khi ghi biên bản, hãy ghi số nước, quân đi, hướng đi và ô đến theo quy ước của ván.'
WHERE `Id` = 7;
UPDATE `CourseLessons` SET
    `Title` = 'Nguyên tắc khai cuộc',
    `Description` = 'Kiểm soát trung tâm và phát triển quân.',
    `Content` = 'Ưu tiên đưa các quân hoạt động tốt, mở đường cho Xe, không di chuyển một quân quá nhiều lần và luôn chú ý an toàn của Tướng.'
WHERE `Id` = 8;
UPDATE `CourseLessons` SET
    `Title` = 'Phối hợp và đổi quân',
    `Description` = 'Xây dựng kế hoạch trung cuộc vững chắc.',
    `Content` = 'Xe, Pháo, Mã nên hỗ trợ lẫn nhau. Chỉ đổi quân khi có lợi về vị trí, vật chất hoặc làm giảm áp lực lên Tướng của bạn.'
WHERE `Id` = 9;

UPDATE `CourseQuizQuestions` SET
    `Question` = 'Khu vực nào nằm giữa hai bên bàn cờ?',
    `OptionsJson` = '["Cung tướng","Con sông","Đường biên"]',
    `Explanation` = 'Con sông nằm giữa hai bên và chia bàn cờ thành hai nửa.'
WHERE `Id` = 1;
UPDATE `CourseQuizQuestions` SET
    `Question` = 'Quân nào có thể đi ngang hoặc dọc không giới hạn?',
    `OptionsJson` = '["Mã","Xe","Tượng"]',
    `Explanation` = 'Xe đi thẳng ngang hoặc dọc trên các đường trống.'
WHERE `Id` = 2;
UPDATE `CourseQuizQuestions` SET
    `Question` = 'Pháo cần gì khi ăn quân?',
    `OptionsJson` = '["Hai ngòi","Đúng một ngòi","Không cần ngòi"]',
    `Explanation` = 'Pháo phải nhảy qua đúng một quân làm ngòi để ăn.'
WHERE `Id` = 3;
UPDATE `CourseQuizQuestions` SET
    `Question` = 'Tượng có được qua sông không?',
    `OptionsJson` = '["Có","Không","Chỉ khi chiếu tướng"]',
    `Explanation` = 'Tượng chỉ hoạt động ở phần sân của mình.'
WHERE `Id` = 4;
UPDATE `CourseQuizQuestions` SET
    `Question` = 'Đâu là nguyên tắc khai cuộc tốt?',
    `OptionsJson` = '["Phát triển quân và kiểm soát trung tâm","Di chuyển một quân liên tục","Bỏ qua an toàn của Tướng"]',
    `Explanation` = 'Phát triển quân và kiểm soát trung tâm giúp các quân phối hợp tốt hơn.'
WHERE `Id` = 5;
