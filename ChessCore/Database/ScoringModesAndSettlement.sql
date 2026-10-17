START TRANSACTION;
ALTER TABLE `Games` MODIFY COLUMN `GameMode` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Legacy';

ALTER TABLE `Games` ADD `IsRated` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Games` ADD `ResultsProcessedAt` datetime(6) NULL;

UPDATE Games SET GameMode = CASE
    WHEN RedPlayerId IS NOT NULL AND BlackPlayerId IS NOT NULL
        AND RedPlayerId <> BlackPlayerId THEN 'Online'
    WHEN GameMode IN ('PvE', 'AiVsAi') THEN GameMode
    ELSE 'Legacy' END;
UPDATE Games SET ResultsProcessedAt = UTC_TIMESTAMP(6)
    WHERE Status IN ('RedWins', 'BlackWins', 'Draw');

CREATE INDEX `IX_Games_GameMode_Status` ON `Games` (`GameMode`, `Status`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261017055617_ScoringModesAndSettlement', '9.0.21');

COMMIT;

