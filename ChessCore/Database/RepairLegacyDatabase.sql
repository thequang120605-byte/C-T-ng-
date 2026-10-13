USE `chess_db`;

UPDATE `Users`
SET `PasswordHash` = COALESCE(`PasswordHash`, ''),
    `Role` = COALESCE(`Role`, 'Player'),
    `PlayerId` = CONCAT('CT-', LPAD(`Id`, 5, '0'))
WHERE `PlayerId` IS NULL OR `PlayerId` = '';

ALTER TABLE `Users`
    MODIFY COLUMN `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    MODIFY COLUMN `PlayerId` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    MODIFY COLUMN `Role` longtext CHARACTER SET utf8mb4 NOT NULL;

CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);
CREATE UNIQUE INDEX `IX_Users_PlayerId` ON `Users` (`PlayerId`);
CREATE UNIQUE INDEX `IX_Users_Username` ON `Users` (`Username`);

UPDATE `Games` g
LEFT JOIN `Users` u ON u.`Id` = g.`BlackPlayerId`
SET g.`BlackPlayerId` = NULL
WHERE g.`BlackPlayerId` IS NOT NULL AND u.`Id` IS NULL;

UPDATE `Games` g
LEFT JOIN `Users` u ON u.`Id` = g.`RedPlayerId`
SET g.`RedPlayerId` = NULL
WHERE g.`RedPlayerId` IS NOT NULL AND u.`Id` IS NULL;

CREATE UNIQUE INDEX `IX_Games_RoomCode` ON `Games` (`RoomCode`);
CREATE INDEX `IX_Games_BlackPlayerId` ON `Games` (`BlackPlayerId`);
CREATE INDEX `IX_Games_RedPlayerId` ON `Games` (`RedPlayerId`);

ALTER TABLE `Games`
    ADD CONSTRAINT `FK_Games_Users_BlackPlayerId`
        FOREIGN KEY (`BlackPlayerId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
    ADD CONSTRAINT `FK_Games_Users_RedPlayerId`
        FOREIGN KEY (`RedPlayerId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL;

CREATE TABLE `Puzzles` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Level` int NOT NULL,
    `Title` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
    `Fen` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `MateIn` int NOT NULL,
    `Difficulty` int NOT NULL,
    PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

INSERT INTO `Puzzles` (`Id`, `Level`, `Title`, `Description`, `Fen`, `MateIn`, `Difficulty`) VALUES
(1, 1, 'Tuyệt sát 1', 'Chiếu bí trong 1 nước (Xe cắm đáy)', '4k4/9/9/9/9/9/9/9/4R4 r - - 0 1', 1, 1),
(2, 2, 'Pháo lồng', 'Song pháo sát cục', '4k4/4a4/4a4/9/9/9/9/9/9/4C4 r - - 0 1', 1, 1),
(3, 3, 'Lưỡng chiếu', 'Kết hợp Xe Pháo', '4k4/9/9/9/9/9/9/9/9/4R4 r - - 0 1', 1, 2);

CREATE TABLE `PuzzleProgresses` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` int NOT NULL,
    `PuzzleKey` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `Attempts` int NOT NULL,
    `CompletedAt` datetime(6) NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_PuzzleProgresses_UserId_PuzzleKey` (`UserId`, `PuzzleKey`)
) CHARACTER SET=utf8mb4;

INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261013160114_RepairLegacyUserAndGameSchema', '9.0.20');
