-- ==========================================================================
-- BẢN ĐỒNG BỘ DUY NHẤT TÀI KHOẢN: chuquoctrung0607@gmail.com (CT-84428)
-- Thời gian khởi tạo: 2026-11-11 22:25:07
-- Mục tiêu: Đồng bộ an toàn lên Render Cloud / TiDB (co-tuong-pro.onrender.com)
-- Đặc điểm an toàn:
--   1. Không làm ảnh hưởng bất kỳ tài khoản hay ván đấu của người dùng khác
--   2. Không ghi đè Elo hoặc xóa các trận Online bạn đã chơi trên Render
--   3. Xóa triệt để 12 trận đầu hàng cố ý dưới 5 nước đi
--   4. Nạp đầy đủ 39 trận PvE hợp lệ (>= 5 nước), 1369 nước đi, 19 bài học & 20 cờ thế
--   5. Tự động định danh GameId theo RoomCode, tránh hoàn toàn xung đột Primary Key trên Render
-- ==========================================================================

-- BƯỚC 1: Đảm bảo tài khoản chuquoctrung0607@gmail.com tồn tại & lấy @uid
INSERT IGNORE INTO `users` (`Username`, `Email`, `CreatedAt`, `PasswordHash`, `PlayerId`, `Role`, `Elo`, `GamesPlayed`, `Wins`, `Losses`, `Draws`, `PuzzleScore`, `IsLocked`)
VALUES ('QuocTrung', 'chuquoctrung0607@gmail.com', '2026-11-11 12:06:27.000000', 'GOcf87qa1xwqQ0NrhrOg+eY+HivTYIoUnloFBybBpus=', 'CT-84428', 'Player', 1200, 39, 4, 35, 0, 500, 0);
SET @uid = (SELECT `Id` FROM `users` WHERE `Email` = 'chuquoctrung0607@gmail.com' LIMIT 1);

-- BƯỚC 2: Xóa 12 ván đầu hàng dưới 5 nước đi của riêng tài khoản chuquoctrung0607@gmail.com
-- (Giữ FOREIGN_KEY_CHECKS = 1 để ON DELETE CASCADE hoạt động sạch sẽ)
DELETE m FROM `moves` m
INNER JOIN `games` g ON m.`GameId` = g.`Id`
WHERE (g.`RedPlayerId` = @uid OR g.`BlackPlayerId` = @uid)
  AND g.`GameMode` = 'PvE'
  AND g.`RoomCode` IN ('FK5CMTSV', 'B4984BME', '92HMKW8F', 'SFHUUQ75', 'PFV7M89E', 'UTGENEUB', 'PRRA8RX5', 'Z9Q5A5MZ', 'LD6CPUN8', 'N2EZL7KU', 'RCT3V7FX', 'XDYJYNLY');

DELETE FROM `games`
WHERE (`RedPlayerId` = @uid OR `BlackPlayerId` = @uid)
  AND `GameMode` = 'PvE'
  AND `RoomCode` IN ('FK5CMTSV', 'B4984BME', '92HMKW8F', 'SFHUUQ75', 'PFV7M89E', 'UTGENEUB', 'PRRA8RX5', 'Z9Q5A5MZ', 'LD6CPUN8', 'N2EZL7KU', 'RCT3V7FX', 'XDYJYNLY');

-- Xóa thêm bất kỳ trận PvE nào khác của @uid có tổng số nước đi < 5
DELETE m FROM `moves` m
INNER JOIN (
    SELECT g.`Id`
    FROM `games` g
    LEFT JOIN `moves` m2 ON g.`Id` = m2.`GameId`
    WHERE (g.`RedPlayerId` = @uid OR g.`BlackPlayerId` = @uid) AND g.`GameMode` = 'PvE'
    GROUP BY g.`Id`
    HAVING COUNT(m2.`Id`) < 5
) AS bad ON m.`GameId` = bad.`Id`;

DELETE FROM `games`
WHERE (`RedPlayerId` = @uid OR `BlackPlayerId` = @uid)
  AND `GameMode` = 'PvE'
  AND (SELECT COUNT(*) FROM `moves` m WHERE m.`GameId` = `games`.`Id`) < 5;

SET FOREIGN_KEY_CHECKS = 0;
-- BƯỚC 3: Đồng bộ 39 ván đấu PvE hợp lệ (>= 5 nước) và toàn bộ nước đi theo RoomCode
-- Ván RoomCode=46HKJKN7 (Local Id=164, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 05:43:21.480406', '2026-11-11 05:45:30.887214', NULL, '', @uid, NULL, '46HKJKN7', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 05:45:30.903348', 3, 6, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '46HKJKN7' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 9, 2, 7, 4, 'Elephant', 'Red', 0, '2026-11-11 05:43:30.250160', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 1, '2026-11-11 05:43:30.288389', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 5, 2, 'Elephant', 'Red', 2, '2026-11-11 05:43:37.504504', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 05:43:37.532183', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 7, 4, 'Elephant', 'Red', 4, '2026-11-11 05:43:40.000210', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 3, 'Rook', 'Black', 5, '2026-11-11 05:43:40.037813', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 8, 0, 'Rook', 'Red', 6, '2026-11-11 05:43:48.712678', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 7, '2026-11-11 05:43:48.766265', NULL, NULL, NULL, NULL),
  (@gid, 8, 0, 8, 3, 'Rook', 'Red', 8, '2026-11-11 05:43:50.721179', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 6, 4, 'Cannon', 'Black', 9, '2026-11-11 05:43:50.776329', NULL, NULL, NULL, NULL);

-- Ván RoomCode=U9PR666V (Local Id=165, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 06:07:09.502968', '2026-11-11 06:10:18.307076', NULL, '', @uid, NULL, 'U9PR666V', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 06:10:18.321098', 3, 4, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'U9PR666V' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 0, '2026-11-11 06:07:11.486990', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 1, '2026-11-11 06:07:11.534480', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 2, '2026-11-11 06:07:22.423360', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 3, '2026-11-11 06:07:22.470661', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 4, '2026-11-11 06:08:46.343569', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 5, 4, 'Cannon', 'Black', 5, '2026-11-11 06:08:46.437969', NULL, NULL, NULL, NULL);

-- Ván RoomCode=GY6S4MDU (Local Id=167, RedWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('RedWins', 'Black', '2026-11-11 06:37:29.547658', '2026-11-11 06:59:52.271975', NULL, '', @uid, NULL, 'GY6S4MDU', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 06:59:52.285868', 3, 46, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'GY6S4MDU' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 7, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 06:38:28.285879', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 9, 1, 'Cannon', 'Black', 1, '2026-11-11 06:38:28.307652', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 2, '2026-11-11 06:38:32.653641', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 1, 'Cannon', 'Black', 3, '2026-11-11 06:38:32.676477', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 9, 0, 'Rook', 'Red', 4, '2026-11-11 06:39:39.165297', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 6, 'Cannon', 'Black', 5, '2026-11-11 06:39:39.185599', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 3, 4, 'Cannon', 'Red', 6, '2026-11-11 06:41:35.451628', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 8, 'Elephant', 'Black', 7, '2026-11-11 06:41:35.477289', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 8, '2026-11-11 06:43:27.821126', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 0, 6, 'Elephant', 'Black', 9, '2026-11-11 06:43:27.841088', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 10, '2026-11-11 06:43:31.229806', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Pawn', 'Black', 11, '2026-11-11 06:43:31.252155', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 1, 'Cannon', 'Red', 12, '2026-11-11 06:43:42.996505', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 6, 6, 'Cannon', 'Black', 13, '2026-11-11 06:43:43.018850', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 14, '2026-11-11 06:46:37.190490', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 15, '2026-11-11 06:46:37.211112', NULL, NULL, NULL, NULL),
  (@gid, 3, 1, 7, 1, 'Cannon', 'Red', 16, '2026-11-11 06:47:06.588611', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 17, '2026-11-11 06:47:06.611833', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 18, '2026-11-11 06:47:12.189716', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 2, 8, 'Rook', 'Black', 19, '2026-11-11 06:47:12.215167', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 0, 2, 'Cannon', 'Red', 20, '2026-11-11 06:47:26.588567', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 1, 4, 'King', 'Black', 21, '2026-11-11 06:47:26.613197', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 0, 5, 'Cannon', 'Red', 22, '2026-11-11 06:48:43.958141', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 6, 7, 'Cannon', 'Black', 23, '2026-11-11 06:48:43.979856', NULL, NULL, NULL, NULL),
  (@gid, 0, 5, 0, 1, 'Cannon', 'Red', 24, '2026-11-11 06:49:07.613222', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 2, 5, 'Rook', 'Black', 25, '2026-11-11 06:49:07.632798', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 0, 6, 'Cannon', 'Red', 26, '2026-11-11 06:49:17.934682', NULL, NULL, NULL, NULL),
  (@gid, 2, 5, 9, 5, 'Rook', 'Black', 27, '2026-11-11 06:49:17.956446', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 28, '2026-11-11 06:49:40.254737', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 9, 6, 'Rook', 'Black', 29, '2026-11-11 06:49:40.280476', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 8, 3, 'King', 'Red', 30, '2026-11-11 06:50:10.693675', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 9, 7, 'Rook', 'Black', 31, '2026-11-11 06:50:10.719366', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 32, '2026-11-11 06:50:12.695218', NULL, NULL, NULL, NULL),
  (@gid, 6, 7, 2, 7, 'Cannon', 'Black', 33, '2026-11-11 06:50:12.727372', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 2, 7, 'Rook', 'Red', 34, '2026-11-11 06:50:17.472060', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 0, 0, 'Rook', 'Black', 35, '2026-11-11 06:50:17.490206', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 0, 0, 'Cannon', 'Red', 36, '2026-11-11 06:50:21.086988', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 37, '2026-11-11 06:50:21.105504', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 8, 'Rook', 'Red', 38, '2026-11-11 06:51:28.144206', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 5, 6, 'Pawn', 'Black', 39, '2026-11-11 06:51:28.167534', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 0, 2, 'Cannon', 'Red', 40, '2026-11-11 06:51:56.815604', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 7, 'Pawn', 'Black', 41, '2026-11-11 06:51:56.834069', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 1, 2, 'Cannon', 'Red', 42, '2026-11-11 06:52:12.854650', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 4, 0, 'Pawn', 'Black', 43, '2026-11-11 06:52:12.879715', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 44, '2026-11-11 06:52:24.624550', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 5, 6, 'Pawn', 'Black', 45, '2026-11-11 06:52:24.642161', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 46, '2026-11-11 06:52:53.439497', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 4, 8, 'Pawn', 'Black', 47, '2026-11-11 06:52:53.457890', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 4, 8, 'Rook', 'Red', 48, '2026-11-11 06:53:37.158425', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 0, 4, 'King', 'Black', 49, '2026-11-11 06:53:37.183801', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 0, 8, 'Rook', 'Red', 50, '2026-11-11 06:53:41.398703', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 1, 4, 'King', 'Black', 51, '2026-11-11 06:53:41.416061', NULL, NULL, NULL, NULL),
  (@gid, 1, 2, 4, 2, 'Cannon', 'Red', 52, '2026-11-11 06:53:54.032287', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 1, 5, 'King', 'Black', 53, '2026-11-11 06:53:54.052608', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 4, 4, 'Cannon', 'Red', 54, '2026-11-11 06:53:57.608112', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 1, 4, 'Advisor', 'Black', 55, '2026-11-11 06:53:57.634172', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 5, 'Rook', 'Red', 56, '2026-11-11 06:54:10.295954', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 0, 5, 'King', 'Black', 57, '2026-11-11 06:54:10.313083', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 1, 4, 'Cannon', 'Red', 58, '2026-11-11 06:54:18.438962', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 7, 'Pawn', 'Black', 59, '2026-11-11 06:54:18.456492', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 4, 1, 'Pawn', 'Red', 60, '2026-11-11 06:55:56.040650', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 6, 7, 'Pawn', 'Black', 61, '2026-11-11 06:55:56.059928', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 62, '2026-11-11 06:55:58.816182', NULL, NULL, NULL, NULL),
  (@gid, 6, 7, 6, 6, 'Pawn', 'Black', 63, '2026-11-11 06:55:58.839867', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 3, 1, 'Pawn', 'Red', 64, '2026-11-11 06:56:13.263063', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 6, 7, 'Pawn', 'Black', 65, '2026-11-11 06:56:13.281379', NULL, NULL, NULL, NULL),
  (@gid, 3, 1, 2, 1, 'Pawn', 'Red', 66, '2026-11-11 06:56:15.835665', NULL, NULL, NULL, NULL),
  (@gid, 6, 7, 6, 6, 'Pawn', 'Black', 67, '2026-11-11 06:56:15.862065', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 2, 'Pawn', 'Red', 68, '2026-11-11 06:56:18.319663', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 7, 6, 'Pawn', 'Black', 69, '2026-11-11 06:56:18.343551', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 1, 2, 'Pawn', 'Red', 70, '2026-11-11 06:56:20.159887', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 7, 5, 'Pawn', 'Black', 71, '2026-11-11 06:56:20.190732', NULL, NULL, NULL, NULL),
  (@gid, 1, 2, 1, 3, 'Pawn', 'Red', 72, '2026-11-11 06:56:23.793029', NULL, NULL, NULL, NULL),
  (@gid, 0, 5, 1, 5, 'King', 'Black', 73, '2026-11-11 06:56:23.812144', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 0, 4, 'Cannon', 'Red', 74, '2026-11-11 06:56:54.503495', NULL, NULL, NULL, NULL),
  (@gid, 7, 5, 7, 6, 'Pawn', 'Black', 75, '2026-11-11 06:56:54.529610', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 76, '2026-11-11 06:58:48.793292', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 0, 5, 'King', 'Black', 77, '2026-11-11 06:58:48.823767', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 1, 4, 'Pawn', 'Red', 78, '2026-11-11 06:58:59.545511', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 7, 7, 'Pawn', 'Black', 79, '2026-11-11 06:58:59.571427', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 7, 0, 'Elephant', 'Red', 80, '2026-11-11 06:59:12.548543', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 8, 7, 'Pawn', 'Black', 81, '2026-11-11 06:59:12.574726', NULL, NULL, NULL, NULL),
  (@gid, 7, 0, 5, 2, 'Elephant', 'Red', 82, '2026-11-11 06:59:24.511925', NULL, NULL, NULL, NULL),
  (@gid, 8, 7, 8, 6, 'Pawn', 'Black', 83, '2026-11-11 06:59:24.534529', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 7, 4, 'Elephant', 'Red', 84, '2026-11-11 06:59:46.879939', NULL, NULL, NULL, NULL),
  (@gid, 8, 6, 9, 6, 'Pawn', 'Black', 85, '2026-11-11 06:59:46.899078', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 9, 6, 'Elephant', 'Red', 86, '2026-11-11 06:59:52.271974', NULL, NULL, NULL, NULL);

-- Ván RoomCode=3H743N2M (Local Id=169, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 07:00:55.152140', '2026-11-11 07:12:12.551987', NULL, '', @uid, NULL, '3H743N2M', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 07:12:12.563599', 3, 16, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '3H743N2M' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 07:01:45.008047', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 07:01:45.131095', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 2, '2026-11-11 07:02:56.466768', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 07:02:56.622730', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 6, 'Cannon', 'Red', 4, '2026-11-11 07:03:16.817468', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 5, '2026-11-11 07:03:16.898464', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 5, 6, 'Cannon', 'Red', 6, '2026-11-11 07:05:28.320838', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 7, '2026-11-11 07:05:28.486662', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 8, '2026-11-11 07:06:03.977207', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 9, '2026-11-11 07:06:04.077954', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 8, 'Cannon', 'Red', 10, '2026-11-11 07:06:23.632817', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 11, '2026-11-11 07:06:23.801964', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 3, 8, 'Cannon', 'Red', 12, '2026-11-11 07:06:29.072867', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 5, 'Rook', 'Black', 13, '2026-11-11 07:06:29.238003', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 14, '2026-11-11 07:07:23.370520', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 3, 5, 'Rook', 'Black', 15, '2026-11-11 07:07:23.551914', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 4, 'Cannon', 'Red', 16, '2026-11-11 07:08:02.349877', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 9, 6, 'Cannon', 'Black', 17, '2026-11-11 07:08:02.463620', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 18, '2026-11-11 07:08:28.752594', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 5, 4, 'Cannon', 'Black', 19, '2026-11-11 07:08:28.868353', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 20, '2026-11-11 07:08:34.833851', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 8, 7, 'Rook', 'Black', 21, '2026-11-11 07:08:34.945527', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 7, 4, 'King', 'Red', 22, '2026-11-11 07:08:44.281319', NULL, NULL, NULL, NULL),
  (@gid, 8, 7, 7, 7, 'Rook', 'Black', 23, '2026-11-11 07:08:44.417381', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 8, 4, 'King', 'Red', 24, '2026-11-11 07:08:55.267206', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 8, 7, 'Rook', 'Black', 25, '2026-11-11 07:08:55.428844', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 7, 4, 'King', 'Red', 26, '2026-11-11 07:09:12.318104', NULL, NULL, NULL, NULL),
  (@gid, 8, 7, 7, 7, 'Rook', 'Black', 27, '2026-11-11 07:09:12.496590', NULL, NULL, NULL, NULL);

-- Ván RoomCode=8KUUZUHP (Local Id=170, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 07:12:19.049729', '2026-11-11 07:26:59.293928', NULL, '', @uid, NULL, '8KUUZUHP', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 07:26:59.304982', 3, 27, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '8KUUZUHP' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 07:12:25.969313', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 07:12:26.098603', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 2, '2026-11-11 07:12:47.298007', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 07:12:47.452029', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 4, '2026-11-11 07:13:57.380801', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 6, 6, 'Cannon', 'Black', 5, '2026-11-11 07:13:57.522362', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 6, 'Cannon', 'Red', 6, '2026-11-11 07:14:31.698404', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 7, '2026-11-11 07:14:31.822063', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 8, '2026-11-11 07:17:29.888179', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 3, 6, 'Horse', 'Black', 9, '2026-11-11 07:17:29.983965', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 3, 7, 'Cannon', 'Red', 10, '2026-11-11 07:19:16.347433', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 11, '2026-11-11 07:19:16.514671', NULL, NULL, NULL, NULL),
  (@gid, 3, 7, 3, 4, 'Cannon', 'Red', 12, '2026-11-11 07:20:22.674671', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 2, 6, 'Cannon', 'Black', 13, '2026-11-11 07:20:22.729346', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 3, 8, 'Cannon', 'Red', 14, '2026-11-11 07:20:35.755676', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 8, 7, 'Rook', 'Black', 15, '2026-11-11 07:20:35.930193', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 16, '2026-11-11 07:21:06.395844', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 5, 7, 'Horse', 'Black', 17, '2026-11-11 07:21:06.634379', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 4, 1, 'Rook', 'Red', 18, '2026-11-11 07:21:21.530986', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 9, 6, 'Cannon', 'Black', 19, '2026-11-11 07:21:21.645205', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 20, '2026-11-11 07:21:30.722934', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 9, 8, 'Cannon', 'Black', 21, '2026-11-11 07:21:30.893538', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 8, 5, 'Horse', 'Red', 22, '2026-11-11 07:21:38.539558', NULL, NULL, NULL, NULL),
  (@gid, 8, 7, 8, 5, 'Rook', 'Black', 23, '2026-11-11 07:21:38.678389', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 4, 4, 'Rook', 'Red', 24, '2026-11-11 07:21:53.604561', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 4, 'Elephant', 'Black', 25, '2026-11-11 07:21:53.636661', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 2, 4, 'Rook', 'Red', 26, '2026-11-11 07:21:56.275467', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 4, 'Elephant', 'Black', 27, '2026-11-11 07:21:56.322537', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 9, 8, 'Cannon', 'Red', 28, '2026-11-11 07:22:30.179423', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 6, 5, 'Horse', 'Black', 29, '2026-11-11 07:22:30.268300', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 4, 4, 'Pawn', 'Red', 30, '2026-11-11 07:23:55.684961', NULL, NULL, NULL, NULL),
  (@gid, 8, 5, 8, 8, 'Rook', 'Black', 31, '2026-11-11 07:23:55.814396', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 6, 'Cannon', 'Red', 32, '2026-11-11 07:23:59.820668', NULL, NULL, NULL, NULL),
  (@gid, 6, 5, 8, 6, 'Horse', 'Black', 33, '2026-11-11 07:23:59.891621', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 9, 5, 'King', 'Red', 34, '2026-11-11 07:24:19.832224', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 9, 6, 'Cannon', 'Black', 35, '2026-11-11 07:24:19.879806', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 3, 4, 'Pawn', 'Red', 36, '2026-11-11 07:24:44.413514', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 0, 6, 'Elephant', 'Black', 37, '2026-11-11 07:24:44.478080', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 3, 5, 'Pawn', 'Red', 38, '2026-11-11 07:24:54.140569', NULL, NULL, NULL, NULL),
  (@gid, 8, 8, 9, 8, 'Rook', 'Black', 39, '2026-11-11 07:24:54.183755', NULL, NULL, NULL, NULL),
  (@gid, 3, 5, 2, 5, 'Pawn', 'Red', 40, '2026-11-11 07:25:08.564672', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 41, '2026-11-11 07:25:08.620432', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 42, '2026-11-11 07:25:42.955839', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 2, 'Rook', 'Black', 43, '2026-11-11 07:25:43.046873', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 6, 4, 'Horse', 'Red', 44, '2026-11-11 07:25:57.557098', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 6, 8, 'Rook', 'Black', 45, '2026-11-11 07:25:57.638639', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 4, 3, 'Horse', 'Red', 46, '2026-11-11 07:26:22.350315', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 6, 5, 'Rook', 'Black', 47, '2026-11-11 07:26:22.396192', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 7, 5, 'Advisor', 'Red', 48, '2026-11-11 07:26:59.238127', NULL, NULL, NULL, NULL),
  (@gid, 6, 5, 7, 5, 'Rook', 'Black', 49, '2026-11-11 07:26:59.286094', NULL, NULL, NULL, NULL);

-- Ván RoomCode=JT55M92H (Local Id=171, RedWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('RedWins', 'Black', '2026-11-11 07:27:46.899470', '2026-11-11 07:41:43.200060', NULL, '', @uid, NULL, 'JT55M92H', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 07:41:43.207646', 3, 44, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'JT55M92H' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 0, '2026-11-11 07:27:57.557169', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 1, '2026-11-11 07:27:57.579025', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 2, '2026-11-11 07:28:00.547939', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 9, 1, 'Cannon', 'Black', 3, '2026-11-11 07:28:00.565689', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 4, '2026-11-11 07:28:08.263871', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 8, 'Elephant', 'Black', 5, '2026-11-11 07:28:08.289771', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 9, 8, 'Rook', 'Red', 6, '2026-11-11 07:28:19.572779', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 1, 5, 'Horse', 'Black', 7, '2026-11-11 07:28:19.589625', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 8, '2026-11-11 07:28:56.841294', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 1, 4, 'Advisor', 'Black', 9, '2026-11-11 07:28:56.874482', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 10, '2026-11-11 07:28:58.636601', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 6, 'Rook', 'Black', 11, '2026-11-11 07:28:58.652545', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 12, '2026-11-11 07:29:10.596756', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 4, 4, 'Pawn', 'Black', 13, '2026-11-11 07:29:10.619730', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 8, 'Cannon', 'Red', 14, '2026-11-11 07:29:54.309196', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 2, 7, 'Horse', 'Black', 15, '2026-11-11 07:29:54.332527', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 16, '2026-11-11 07:30:30.709608', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 1, 5, 'Horse', 'Black', 17, '2026-11-11 07:30:30.732759', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 7, 0, 'Cannon', 'Red', 18, '2026-11-11 07:30:43.229819', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 19, '2026-11-11 07:30:43.256785', NULL, NULL, NULL, NULL),
  (@gid, 7, 0, 7, 4, 'Cannon', 'Red', 20, '2026-11-11 07:31:03.846773', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 0, 3, 'King', 'Black', 21, '2026-11-11 07:31:03.879803', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 4, 4, 'Cannon', 'Red', 22, '2026-11-11 07:31:23.014363', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 2, 7, 'Horse', 'Black', 23, '2026-11-11 07:31:23.042411', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 24, '2026-11-11 07:31:55.988331', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 0, 8, 'Rook', 'Black', 25, '2026-11-11 07:31:56.052822', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 0, 8, 'Cannon', 'Red', 26, '2026-11-11 07:32:00.598285', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 0, 8, 'Horse', 'Black', 27, '2026-11-11 07:32:00.616691', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 3, 2, 'Pawn', 'Red', 28, '2026-11-11 07:35:26.257227', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Pawn', 'Black', 29, '2026-11-11 07:35:26.290359', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 4, 6, 'Pawn', 'Red', 30, '2026-11-11 07:35:28.542303', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 31, '2026-11-11 07:35:28.560839', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 3, 6, 'Pawn', 'Red', 32, '2026-11-11 07:36:43.798634', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 33, '2026-11-11 07:36:43.822566', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 4, 8, 'Cannon', 'Red', 34, '2026-11-11 07:36:48.751900', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 0, 4, 'King', 'Black', 35, '2026-11-11 07:36:48.770792', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 0, 8, 'Cannon', 'Red', 36, '2026-11-11 07:36:54.294731', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 0, 6, 'Elephant', 'Black', 37, '2026-11-11 07:36:54.319711', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Cannon', 'Red', 38, '2026-11-11 07:38:33.086887', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 0, 3, 'Horse', 'Black', 39, '2026-11-11 07:38:33.112371', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 1, 0, 'Cannon', 'Red', 40, '2026-11-11 07:38:37.230967', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 0, 2, 'Elephant', 'Black', 41, '2026-11-11 07:38:37.248343', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 0, 0, 'Cannon', 'Red', 42, '2026-11-11 07:38:49.559943', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 4, 0, 'Pawn', 'Black', 43, '2026-11-11 07:38:49.583722', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 9, 0, 'Rook', 'Red', 44, '2026-11-11 07:39:03.391946', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 2, 5, 'Advisor', 'Black', 45, '2026-11-11 07:39:03.408274', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 2, 2, 'Pawn', 'Red', 46, '2026-11-11 07:39:09.783680', NULL, NULL, NULL, NULL),
  (@gid, 0, 5, 1, 4, 'Advisor', 'Black', 47, '2026-11-11 07:39:09.811412', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 1, 2, 'Pawn', 'Red', 48, '2026-11-11 07:39:48.023673', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 8, 'Elephant', 'Black', 49, '2026-11-11 07:39:48.047519', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Cannon', 'Red', 50, '2026-11-11 07:39:54.831637', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 0, 6, 'Elephant', 'Black', 51, '2026-11-11 07:39:54.858088', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 2, 6, 'Pawn', 'Red', 52, '2026-11-11 07:40:14.727313', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 0, 5, 'Advisor', 'Black', 53, '2026-11-11 07:40:14.751687', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 2, 5, 'Pawn', 'Red', 54, '2026-11-11 07:40:17.183839', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 8, 'Elephant', 'Black', 55, '2026-11-11 07:40:17.245452', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 2, 0, 'Cannon', 'Red', 56, '2026-11-11 07:40:30.367590', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 57, '2026-11-11 07:40:30.393688', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 58, '2026-11-11 07:40:42.391513', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 4, 6, 'Elephant', 'Black', 59, '2026-11-11 07:40:42.416197', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 60, '2026-11-11 07:40:45.176308', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 2, 2, 'Horse', 'Black', 61, '2026-11-11 07:40:45.201718', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 62, '2026-11-11 07:40:51.799961', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 2, 8, 'Elephant', 'Black', 63, '2026-11-11 07:40:51.826130', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 3, 0, 'Pawn', 'Red', 64, '2026-11-11 07:40:58.162700', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 3, 0, 'Horse', 'Black', 65, '2026-11-11 07:40:58.181421', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 3, 0, 'Rook', 'Red', 66, '2026-11-11 07:41:00.257578', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 4, 2, 'Elephant', 'Black', 67, '2026-11-11 07:41:00.281446', NULL, NULL, NULL, NULL),
  (@gid, 1, 2, 1, 3, 'Pawn', 'Red', 68, '2026-11-11 07:41:14.656544', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 2, 4, 'Elephant', 'Black', 69, '2026-11-11 07:41:14.679980', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 3, 4, 'Rook', 'Red', 70, '2026-11-11 07:41:22.064784', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 4, 6, 'Elephant', 'Black', 71, '2026-11-11 07:41:22.092081', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 2, 4, 'Rook', 'Red', 72, '2026-11-11 07:41:23.641681', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 2, 4, 'Elephant', 'Black', 73, '2026-11-11 07:41:23.662099', NULL, NULL, NULL, NULL),
  (@gid, 2, 5, 2, 4, 'Pawn', 'Red', 74, '2026-11-11 07:41:28.521485', NULL, NULL, NULL, NULL),
  (@gid, 0, 5, 1, 4, 'Advisor', 'Black', 75, '2026-11-11 07:41:28.551203', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 1, 4, 'Pawn', 'Red', 76, '2026-11-11 07:41:32.104590', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 0, 5, 'King', 'Black', 77, '2026-11-11 07:41:32.122544', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 0, 3, 'Pawn', 'Red', 78, '2026-11-11 07:41:43.200060', NULL, NULL, NULL, NULL);

-- Ván RoomCode=PVHSP7DD (Local Id=172, RedWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('RedWins', 'Black', '2026-11-11 07:41:53.880057', '2026-11-11 07:50:25.418414', NULL, '', @uid, NULL, 'PVHSP7DD', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 07:50:25.431909', 3, 43, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'PVHSP7DD' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 0, '2026-11-11 07:43:31.055086', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 9, 1, 'Cannon', 'Black', 1, '2026-11-11 07:43:31.074093', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 2, '2026-11-11 07:43:36.206935', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 3, '2026-11-11 07:43:36.231930', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 4, '2026-11-11 07:43:37.793257', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 5, '2026-11-11 07:43:37.815860', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 6, '2026-11-11 07:43:57.857039', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 4, 2, 'Elephant', 'Black', 7, '2026-11-11 07:43:57.883850', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 8, '2026-11-11 07:44:05.608240', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 1, 3, 'Horse', 'Black', 9, '2026-11-11 07:44:05.635518', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 10, '2026-11-11 07:44:16.481275', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 11, '2026-11-11 07:44:16.500879', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 3, 2, 'Pawn', 'Red', 12, '2026-11-11 07:44:18.351471', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 3, 2, 'Horse', 'Black', 13, '2026-11-11 07:44:18.368114', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 3, 1, 'Rook', 'Red', 14, '2026-11-11 07:44:47.935743', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 2, 0, 'Rook', 'Black', 15, '2026-11-11 07:44:47.959644', NULL, NULL, NULL, NULL),
  (@gid, 3, 1, 3, 2, 'Rook', 'Red', 16, '2026-11-11 07:44:53.727903', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 0, 0, 'Rook', 'Black', 17, '2026-11-11 07:44:53.745693', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 4, 'Rook', 'Red', 18, '2026-11-11 07:45:00.713806', NULL, NULL, NULL, NULL),
  (@gid, 0, 5, 1, 4, 'Advisor', 'Black', 19, '2026-11-11 07:45:00.732752', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 3, 6, 'Rook', 'Red', 20, '2026-11-11 07:45:06.015224', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 3, 6, 'Horse', 'Black', 21, '2026-11-11 07:45:06.031755', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 22, '2026-11-11 07:45:15.591810', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 4, 'Elephant', 'Black', 23, '2026-11-11 07:45:15.609453', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 3, 6, 'Cannon', 'Red', 24, '2026-11-11 07:45:20.791529', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 2, 5, 'Advisor', 'Black', 25, '2026-11-11 07:45:20.816764', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 2, 6, 'Cannon', 'Red', 26, '2026-11-11 07:45:33.264584', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 6, 'Rook', 'Black', 27, '2026-11-11 07:45:33.290396', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 2, 4, 'Cannon', 'Red', 28, '2026-11-11 07:45:37.240378', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 4, 0, 'Pawn', 'Black', 29, '2026-11-11 07:45:37.258968', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 30, '2026-11-11 07:45:44.768823', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 2, 0, 'Rook', 'Black', 31, '2026-11-11 07:45:44.793406', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 5, 4, 'Cannon', 'Red', 32, '2026-11-11 07:46:26.048095', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 4, 0, 'Rook', 'Black', 33, '2026-11-11 07:46:26.064519', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 2, 4, 'Cannon', 'Red', 34, '2026-11-11 07:46:39.656432', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 4, 3, 'Rook', 'Black', 35, '2026-11-11 07:46:39.683415', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 36, '2026-11-11 07:46:53.352047', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 5, 6, 'Rook', 'Black', 37, '2026-11-11 07:46:53.379370', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 38, '2026-11-11 07:46:55.212065', NULL, NULL, NULL, NULL),
  (@gid, 4, 3, 9, 3, 'Rook', 'Black', 39, '2026-11-11 07:46:55.243739', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 40, '2026-11-11 07:47:06.151928', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 3, 'Rook', 'Black', 41, '2026-11-11 07:47:06.177335', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 7, 4, 'King', 'Red', 42, '2026-11-11 07:47:43.712622', NULL, NULL, NULL, NULL),
  (@gid, 8, 3, 9, 3, 'Rook', 'Black', 43, '2026-11-11 07:47:43.738269', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 44, '2026-11-11 07:48:00.417469', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 9, 6, 'Rook', 'Black', 45, '2026-11-11 07:48:00.443022', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 9, 6, 'Rook', 'Red', 46, '2026-11-11 07:48:04.412297', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 4, 8, 'Pawn', 'Black', 47, '2026-11-11 07:48:04.431866', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 4, 6, 'Pawn', 'Red', 48, '2026-11-11 07:48:12.561614', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 1, 4, 'King', 'Black', 49, '2026-11-11 07:48:12.577650', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 4, 4, 'Cannon', 'Red', 50, '2026-11-11 07:48:27.120924', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 1, 3, 'King', 'Black', 51, '2026-11-11 07:48:27.137086', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 4, 8, 'Cannon', 'Red', 52, '2026-11-11 07:48:28.570109', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 1, 4, 'Advisor', 'Black', 53, '2026-11-11 07:48:28.596021', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 3, 6, 'Pawn', 'Red', 54, '2026-11-11 07:48:31.225094', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 2, 3, 'King', 'Black', 55, '2026-11-11 07:48:31.241820', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 2, 6, 'Pawn', 'Red', 56, '2026-11-11 07:48:35.346257', NULL, NULL, NULL, NULL),
  (@gid, 2, 3, 1, 3, 'King', 'Black', 57, '2026-11-11 07:48:35.370793', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 2, 8, 'Cannon', 'Red', 58, '2026-11-11 07:48:48.489008', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 2, 3, 'King', 'Black', 59, '2026-11-11 07:48:48.507255', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 2, 5, 'Pawn', 'Red', 60, '2026-11-11 07:48:50.369754', NULL, NULL, NULL, NULL),
  (@gid, 2, 3, 1, 3, 'King', 'Black', 61, '2026-11-11 07:48:50.393454', NULL, NULL, NULL, NULL),
  (@gid, 2, 5, 1, 5, 'Pawn', 'Red', 62, '2026-11-11 07:49:07.785539', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 0, 5, 'Advisor', 'Black', 63, '2026-11-11 07:49:07.804278', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 0, 5, 'Pawn', 'Red', 64, '2026-11-11 07:49:12.297363', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 0, 3, 'King', 'Black', 65, '2026-11-11 07:49:12.317169', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 0, 8, 'Cannon', 'Red', 66, '2026-11-11 07:49:15.841387', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 1, 3, 'King', 'Black', 67, '2026-11-11 07:49:15.858035', NULL, NULL, NULL, NULL),
  (@gid, 0, 5, 0, 4, 'Pawn', 'Red', 68, '2026-11-11 07:49:19.186589', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 1, 4, 'King', 'Black', 69, '2026-11-11 07:49:19.210482', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 4, 4, 'Pawn', 'Red', 70, '2026-11-11 07:49:29.825468', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 0, 4, 'King', 'Black', 71, '2026-11-11 07:49:29.851221', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 1, 2, 'Cannon', 'Red', 72, '2026-11-11 07:49:43.265925', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 0, 3, 'King', 'Black', 73, '2026-11-11 07:49:43.284537', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 3, 4, 'Pawn', 'Red', 74, '2026-11-11 07:49:45.097400', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 0, 4, 'King', 'Black', 75, '2026-11-11 07:49:45.121555', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 1, 6, 'Rook', 'Red', 76, '2026-11-11 07:49:51.065919', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 0, 3, 'King', 'Black', 77, '2026-11-11 07:49:51.082902', NULL, NULL, NULL, NULL),
  (@gid, 1, 2, 0, 2, 'Cannon', 'Red', 78, '2026-11-11 07:50:04.337939', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 0, 4, 'King', 'Black', 79, '2026-11-11 07:50:04.365572', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 2, 4, 'Pawn', 'Red', 80, '2026-11-11 07:50:16.965910', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 0, 5, 'King', 'Black', 81, '2026-11-11 07:50:16.993044', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 1, 4, 'Pawn', 'Red', 82, '2026-11-11 07:50:25.418413', NULL, NULL, NULL, NULL);

-- Ván RoomCode=2PR7X5MN (Local Id=173, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 07:51:18.703744', '2026-11-11 07:56:01.523456', NULL, '', @uid, NULL, '2PR7X5MN', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 07:56:01.536508', 3, 9, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '2PR7X5MN' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 07:52:36.640773', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 07:52:36.767389', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 2, '2026-11-11 07:52:56.561953', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 07:52:56.716985', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 6, 'Cannon', 'Red', 4, '2026-11-11 07:53:01.225472', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 5, '2026-11-11 07:53:01.321338', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 3, 5, 'Cannon', 'Red', 6, '2026-11-11 07:53:19.650039', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 9, 6, 'Cannon', 'Black', 7, '2026-11-11 07:53:19.734485', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 8, '2026-11-11 07:53:29.491984', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 9, 8, 'Cannon', 'Black', 9, '2026-11-11 07:53:29.606891', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 10, '2026-11-11 07:53:48.536967', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 11, '2026-11-11 07:53:48.644603', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 12, '2026-11-11 07:54:06.330491', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 2, 2, 'Cannon', 'Black', 13, '2026-11-11 07:54:06.482456', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 14, '2026-11-11 07:56:01.463566', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 9, 2, 'Cannon', 'Black', 15, '2026-11-11 07:56:01.516097', NULL, NULL, NULL, NULL);

-- Ván RoomCode=JU7RVAZD (Local Id=174, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 07:56:16.026611', '2026-11-11 08:01:09.799332', NULL, '', @uid, NULL, 'JU7RVAZD', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 08:01:09.810708', 3, 14, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'JU7RVAZD' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 07:56:38.378978', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 07:56:38.489432', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 2, '2026-11-11 07:56:53.312943', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 07:56:53.471330', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 6, 'Cannon', 'Red', 4, '2026-11-11 07:57:08.930254', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 5, '2026-11-11 07:57:09.027254', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 5, 6, 'Cannon', 'Red', 6, '2026-11-11 07:57:32.009982', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 7, '2026-11-11 07:57:32.192969', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 2, 'Cannon', 'Red', 8, '2026-11-11 07:57:42.667207', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 4, 3, 'Horse', 'Black', 9, '2026-11-11 07:57:42.826577', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 10, '2026-11-11 07:59:05.674466', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 11, '2026-11-11 07:59:05.869706', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 12, '2026-11-11 07:59:16.394928', NULL, NULL, NULL, NULL),
  (@gid, 4, 3, 6, 4, 'Horse', 'Black', 13, '2026-11-11 07:59:16.552538', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 4, 3, 'Pawn', 'Red', 14, '2026-11-11 07:59:37.322989', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 7, 2, 'Horse', 'Black', 15, '2026-11-11 07:59:37.483149', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 16, '2026-11-11 08:00:22.172262', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 6, 4, 'Horse', 'Black', 17, '2026-11-11 08:00:22.270320', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 18, '2026-11-11 08:00:40.592227', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 7, 2, 'Horse', 'Black', 19, '2026-11-11 08:00:40.716605', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 7, 0, 'Rook', 'Red', 20, '2026-11-11 08:00:49.891074', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 6, 4, 'Horse', 'Black', 21, '2026-11-11 08:00:50.009945', NULL, NULL, NULL, NULL),
  (@gid, 4, 3, 3, 3, 'Pawn', 'Red', 22, '2026-11-11 08:01:06.963296', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 6, 'Horse', 'Black', 23, '2026-11-11 08:01:07.117742', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 24, '2026-11-11 08:01:09.748138', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 9, 6, 'Cannon', 'Black', 25, '2026-11-11 08:01:09.792841', NULL, NULL, NULL, NULL);

-- Ván RoomCode=LM7Y5R6L (Local Id=175, RedWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('RedWins', 'Red', '2026-11-11 08:01:41.611634', '2026-11-11 08:10:48.406559', NULL, '', @uid, NULL, 'LM7Y5R6L', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 08:10:48.415853', 3, 26, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'LM7Y5R6L' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 0, '2026-11-11 08:01:48.139447', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 1, '2026-11-11 08:01:48.266183', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 2, '2026-11-11 08:02:06.554170', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 08:02:06.696173', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 5, 3, 'Horse', 'Red', 4, '2026-11-11 08:02:51.259018', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 3, 'Rook', 'Black', 5, '2026-11-11 08:02:51.387472', NULL, NULL, NULL, NULL),
  (@gid, 5, 3, 3, 2, 'Horse', 'Red', 6, '2026-11-11 08:03:03.147020', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 4, 3, 'Rook', 'Black', 7, '2026-11-11 08:03:03.347672', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 8, '2026-11-11 08:03:17.483764', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 6, 7, 'Cannon', 'Black', 9, '2026-11-11 08:03:17.671729', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 10, '2026-11-11 08:05:40.803482', NULL, NULL, NULL, NULL),
  (@gid, 6, 7, 6, 4, 'Cannon', 'Black', 11, '2026-11-11 08:05:40.941153', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 3, 'Advisor', 'Red', 12, '2026-11-11 08:06:17.395191', NULL, NULL, NULL, NULL),
  (@gid, 4, 3, 7, 3, 'Rook', 'Black', 13, '2026-11-11 08:06:17.627112', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 2, 0, 'Horse', 'Red', 14, '2026-11-11 08:07:46.507551', NULL, NULL, NULL, NULL),
  (@gid, 7, 3, 7, 2, 'Rook', 'Black', 15, '2026-11-11 08:07:46.606683', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 16, '2026-11-11 08:08:07.228916', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 7, 4, 'Rook', 'Black', 17, '2026-11-11 08:08:07.394534', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 18, '2026-11-11 08:08:10.868289', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 7, 0, 'Rook', 'Black', 19, '2026-11-11 08:08:10.935928', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 7, 5, 'Advisor', 'Red', 20, '2026-11-11 08:08:21.955857', NULL, NULL, NULL, NULL),
  (@gid, 7, 0, 9, 0, 'Rook', 'Black', 21, '2026-11-11 08:08:22.018812', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 22, '2026-11-11 08:08:55.004061', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 2, 'Rook', 'Black', 23, '2026-11-11 08:08:55.087410', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 3, 2, 'Pawn', 'Red', 24, '2026-11-11 08:09:11.644465', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 3, 2, 'Rook', 'Black', 25, '2026-11-11 08:09:11.757544', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 6, 7, 'Cannon', 'Red', 26, '2026-11-11 08:09:21.412169', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 6, 7, 'Cannon', 'Black', 27, '2026-11-11 08:09:21.478799', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 28, '2026-11-11 08:09:34.309057', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 8, 2, 'Rook', 'Black', 29, '2026-11-11 08:09:34.356795', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 7, 4, 'King', 'Red', 30, '2026-11-11 08:09:39.669000', NULL, NULL, NULL, NULL),
  (@gid, 8, 2, 7, 2, 'Rook', 'Black', 31, '2026-11-11 08:09:39.728004', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 8, 4, 'King', 'Red', 32, '2026-11-11 08:09:44.260016', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 7, 5, 'Rook', 'Black', 33, '2026-11-11 08:09:44.323946', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 8, 8, 'Rook', 'Red', 34, '2026-11-11 08:10:00.301605', NULL, NULL, NULL, NULL),
  (@gid, 7, 5, 9, 5, 'Rook', 'Black', 35, '2026-11-11 08:10:00.415212', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 4, 'Elephant', 'Red', 36, '2026-11-11 08:10:05.220280', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 9, 7, 'Rook', 'Black', 37, '2026-11-11 08:10:05.296803', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 5, 6, 'Elephant', 'Red', 38, '2026-11-11 08:10:10.380816', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 39, '2026-11-11 08:10:10.448657', NULL, NULL, NULL, NULL),
  (@gid, 8, 8, 6, 8, 'Rook', 'Red', 40, '2026-11-11 08:10:39.596588', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 8, 7, 'Rook', 'Black', 41, '2026-11-11 08:10:39.645553', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 7, 4, 'King', 'Red', 42, '2026-11-11 08:10:42.941311', NULL, NULL, NULL, NULL),
  (@gid, 8, 7, 7, 7, 'Rook', 'Black', 43, '2026-11-11 08:10:43.006601', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 8, 4, 'King', 'Red', 44, '2026-11-11 08:10:44.761941', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 8, 7, 'Rook', 'Black', 45, '2026-11-11 08:10:44.825449', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 4, 'King', 'Red', 46, '2026-11-11 08:10:46.540956', NULL, NULL, NULL, NULL),
  (@gid, 8, 7, 9, 7, 'Rook', 'Black', 47, '2026-11-11 08:10:46.584209', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 48, '2026-11-11 08:10:48.338843', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 8, 7, 'Rook', 'Black', 49, '2026-11-11 08:10:48.399646', NULL, NULL, NULL, NULL);

-- Ván RoomCode=6NFL2G84 (Local Id=176, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 08:11:01.372692', '2026-11-11 08:19:58.065544', NULL, '', @uid, NULL, '6NFL2G84', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 08:19:58.074601', 3, 34, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '6NFL2G84' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 0, '2026-11-11 08:11:11.396477', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 1, '2026-11-11 08:11:11.515172', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 2, '2026-11-11 08:11:21.125718', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 1, 4, 'Horse', 'Black', 3, '2026-11-11 08:11:21.237941', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 4, '2026-11-11 08:11:26.341552', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 4, 2, 'Pawn', 'Black', 5, '2026-11-11 08:11:26.466724', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 6, '2026-11-11 08:11:42.492880', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 7, '2026-11-11 08:11:42.671523', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 7, 4, 'Cannon', 'Red', 8, '2026-11-11 08:12:06.044664', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 6, 4, 'Cannon', 'Black', 9, '2026-11-11 08:12:06.172801', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 10, '2026-11-11 08:12:13.022574', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 11, '2026-11-11 08:12:13.167638', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 12, '2026-11-11 08:12:15.471366', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 5, 2, 'Pawn', 'Black', 13, '2026-11-11 08:12:15.542969', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 6, 7, 'Cannon', 'Red', 14, '2026-11-11 08:12:48.805339', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 6, 7, 'Cannon', 'Black', 15, '2026-11-11 08:12:48.860149', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 6, 7, 'Rook', 'Red', 16, '2026-11-11 08:12:54.830522', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 2, 0, 'Rook', 'Black', 17, '2026-11-11 08:12:54.887415', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 18, '2026-11-11 08:13:39.965566', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 2, 5, 'Rook', 'Black', 19, '2026-11-11 08:13:40.039044', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 20, '2026-11-11 08:14:44.198253', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 4, 0, 'Pawn', 'Black', 21, '2026-11-11 08:14:44.302007', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 4, 0, 'Rook', 'Red', 22, '2026-11-11 08:14:45.997220', NULL, NULL, NULL, NULL),
  (@gid, 2, 5, 2, 1, 'Rook', 'Black', 23, '2026-11-11 08:14:46.125333', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 0, 0, 'Rook', 'Red', 24, '2026-11-11 08:14:52.230489', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 9, 1, 'Rook', 'Black', 25, '2026-11-11 08:14:52.311053', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 0, 2, 'Rook', 'Red', 26, '2026-11-11 08:15:00.348936', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 9, 2, 'Rook', 'Black', 27, '2026-11-11 08:15:00.409584', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 3, 'Advisor', 'Red', 28, '2026-11-11 08:15:05.349348', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 6, 2, 'Rook', 'Black', 29, '2026-11-11 08:15:05.444453', NULL, NULL, NULL, NULL),
  (@gid, 6, 7, 6, 2, 'Rook', 'Red', 30, '2026-11-11 08:15:16.422288', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 6, 2, 'Pawn', 'Black', 31, '2026-11-11 08:15:16.463144', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 6, 2, 'Rook', 'Red', 32, '2026-11-11 08:15:19.469903', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 33, '2026-11-11 08:15:19.520960', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 6, 6, 'Rook', 'Red', 34, '2026-11-11 08:15:27.103732', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 2, 8, 'Rook', 'Black', 35, '2026-11-11 08:15:27.132994', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 36, '2026-11-11 08:15:35.934112', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 4, 4, 'Pawn', 'Black', 37, '2026-11-11 08:15:35.977835', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 6, 4, 'Rook', 'Red', 38, '2026-11-11 08:16:24.246776', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 4, 8, 'Pawn', 'Black', 39, '2026-11-11 08:16:24.298254', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 4, 4, 'Cannon', 'Red', 40, '2026-11-11 08:16:28.101954', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 2, 2, 'Horse', 'Black', 41, '2026-11-11 08:16:28.184882', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 4, 6, 'Pawn', 'Red', 42, '2026-11-11 08:17:07.767087', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 5, 8, 'Pawn', 'Black', 43, '2026-11-11 08:17:07.837591', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 3, 6, 'Pawn', 'Red', 44, '2026-11-11 08:17:12.654316', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 1, 8, 'Horse', 'Black', 45, '2026-11-11 08:17:12.694322', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 3, 5, 'Pawn', 'Red', 46, '2026-11-11 08:18:20.768767', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 2, 6, 'Rook', 'Black', 47, '2026-11-11 08:18:20.870423', NULL, NULL, NULL, NULL),
  (@gid, 3, 5, 3, 4, 'Pawn', 'Red', 48, '2026-11-11 08:18:26.727091', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 1, 4, 'Advisor', 'Black', 49, '2026-11-11 08:18:26.773782', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 6, 2, 'Rook', 'Red', 50, '2026-11-11 08:18:44.231044', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 4, 6, 'Rook', 'Black', 51, '2026-11-11 08:18:44.356615', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 2, 2, 'Rook', 'Red', 52, '2026-11-11 08:18:47.390542', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 4, 4, 'Rook', 'Black', 53, '2026-11-11 08:18:47.430128', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 54, '2026-11-11 08:18:52.614998', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 3, 4, 'Rook', 'Black', 55, '2026-11-11 08:18:52.662266', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 0, 2, 'Rook', 'Red', 56, '2026-11-11 08:19:00.639001', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 0, 3, 'Advisor', 'Black', 57, '2026-11-11 08:19:00.666803', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 1, 2, 'Rook', 'Red', 58, '2026-11-11 08:19:30.575674', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 3, 7, 'Horse', 'Black', 59, '2026-11-11 08:19:30.618954', NULL, NULL, NULL, NULL),
  (@gid, 1, 2, 1, 8, 'Rook', 'Red', 60, '2026-11-11 08:19:35.430827', NULL, NULL, NULL, NULL),
  (@gid, 3, 7, 1, 8, 'Horse', 'Black', 61, '2026-11-11 08:19:35.467331', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 4, 'Elephant', 'Red', 62, '2026-11-11 08:19:43.398827', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 7, 4, 'Rook', 'Black', 63, '2026-11-11 08:19:43.438544', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 9, 5, 'King', 'Red', 64, '2026-11-11 08:19:50.182724', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 2, 6, 'Horse', 'Black', 65, '2026-11-11 08:19:50.207009', NULL, NULL, NULL, NULL);

-- Ván RoomCode=SYKFSJWJ (Local Id=177, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 08:19:59.303386', '2026-11-11 08:27:31.973528', NULL, '', @uid, NULL, 'SYKFSJWJ', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 08:27:31.981684', 3, 28, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'SYKFSJWJ' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 0, '2026-11-11 08:20:20.662928', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 1, '2026-11-11 08:20:20.818320', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 2, '2026-11-11 08:20:59.277660', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 1, 4, 'Horse', 'Black', 3, '2026-11-11 08:20:59.375581', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 4, '2026-11-11 08:21:03.503284', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 4, 'Elephant', 'Black', 5, '2026-11-11 08:21:03.663277', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 6, 'Cannon', 'Red', 6, '2026-11-11 08:21:21.783389', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 7, '2026-11-11 08:21:21.962401', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 8, '2026-11-11 08:21:55.807138', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 1, 5, 'Horse', 'Black', 9, '2026-11-11 08:21:55.927377', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Cannon', 'Red', 10, '2026-11-11 08:22:31.663529', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 11, '2026-11-11 08:22:31.778790', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 8, 'Cannon', 'Red', 12, '2026-11-11 08:22:46.839936', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 4, 6, 'Elephant', 'Black', 13, '2026-11-11 08:22:46.973628', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 14, '2026-11-11 08:22:59.063587', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 2, 8, 'Elephant', 'Black', 15, '2026-11-11 08:22:59.194971', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 4, 6, 'Pawn', 'Red', 16, '2026-11-11 08:23:04.598570', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 7, 6, 'Cannon', 'Black', 17, '2026-11-11 08:23:04.739105', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 18, '2026-11-11 08:23:15.544656', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 2, 'Cannon', 'Black', 19, '2026-11-11 08:23:15.693677', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 20, '2026-11-11 08:23:21.639356', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 7, 2, 'Cannon', 'Black', 21, '2026-11-11 08:23:21.778618', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 7, 4, 'Elephant', 'Red', 22, '2026-11-11 08:23:35.408325', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 5, 7, 'Rook', 'Black', 23, '2026-11-11 08:23:35.517394', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 4, 8, 'Pawn', 'Red', 24, '2026-11-11 08:23:56.896680', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 4, 6, 'Elephant', 'Black', 25, '2026-11-11 08:23:57.059359', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 3, 8, 'Pawn', 'Red', 26, '2026-11-11 08:24:01.375766', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 2, 0, 'Rook', 'Black', 27, '2026-11-11 08:24:01.461192', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 28, '2026-11-11 08:24:28.873921', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 7, 7, 'Cannon', 'Black', 29, '2026-11-11 08:24:28.966012', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 3, 2, 'Pawn', 'Red', 30, '2026-11-11 08:24:36.151777', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 2, 3, 'Rook', 'Black', 31, '2026-11-11 08:24:36.233990', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 8, 8, 'Cannon', 'Red', 32, '2026-11-11 08:24:58.784150', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 5, 8, 'Rook', 'Black', 33, '2026-11-11 08:24:58.937296', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 34, '2026-11-11 08:25:29.607673', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 3, 8, 'Rook', 'Black', 35, '2026-11-11 08:25:29.808924', NULL, NULL, NULL, NULL),
  (@gid, 8, 8, 8, 7, 'Cannon', 'Red', 36, '2026-11-11 08:25:43.297370', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 9, 8, 'Rook', 'Black', 37, '2026-11-11 08:25:43.360837', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 38, '2026-11-11 08:25:58.256102', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 8, 8, 'Rook', 'Black', 39, '2026-11-11 08:25:58.400533', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 3, 0, 'Pawn', 'Red', 40, '2026-11-11 08:26:04.361793', NULL, NULL, NULL, NULL),
  (@gid, 8, 8, 8, 7, 'Rook', 'Black', 41, '2026-11-11 08:26:04.501985', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 42, '2026-11-11 08:26:16.184065', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 3, 5, 'Horse', 'Black', 43, '2026-11-11 08:26:16.281667', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 2, 0, 'Pawn', 'Red', 44, '2026-11-11 08:26:29.911703', NULL, NULL, NULL, NULL),
  (@gid, 8, 7, 8, 5, 'Rook', 'Black', 45, '2026-11-11 08:26:30.030335', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 1, 0, 'Pawn', 'Red', 46, '2026-11-11 08:27:00.048723', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 9, 7, 'Cannon', 'Black', 47, '2026-11-11 08:27:00.208783', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 1, 'Pawn', 'Red', 48, '2026-11-11 08:27:16.361267', NULL, NULL, NULL, NULL),
  (@gid, 2, 3, 8, 3, 'Rook', 'Black', 49, '2026-11-11 08:27:16.453377', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 3, 'Pawn', 'Red', 50, '2026-11-11 08:27:31.889149', NULL, NULL, NULL, NULL),
  (@gid, 8, 5, 9, 5, 'Rook', 'Black', 51, '2026-11-11 08:27:31.967395', NULL, NULL, NULL, NULL);

-- Ván RoomCode=7ZDTRGL3 (Local Id=178, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 08:27:38.679452', '2026-11-11 08:29:07.761592', NULL, '', @uid, NULL, '7ZDTRGL3', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 08:29:07.769550', 3, 7, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '7ZDTRGL3' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 0, '2026-11-11 08:27:41.711491', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 1, '2026-11-11 08:27:41.850224', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 2, '2026-11-11 08:28:10.319174', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 3, '2026-11-11 08:28:10.466726', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 4, 4, 'Pawn', 'Red', 4, '2026-11-11 08:28:23.304123', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 4, 4, 'Cannon', 'Black', 5, '2026-11-11 08:28:23.407084', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 3, 4, 'Cannon', 'Red', 6, '2026-11-11 08:28:31.816103', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 3, 4, 'Horse', 'Black', 7, '2026-11-11 08:28:31.976330', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 8, '2026-11-11 08:28:39.604510', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 4, 8, 'Cannon', 'Black', 9, '2026-11-11 08:28:39.716824', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 10, '2026-11-11 08:28:52.679876', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 9, 8, 'Cannon', 'Black', 11, '2026-11-11 08:28:52.756792', NULL, NULL, NULL, NULL);

-- Ván RoomCode=CZZ7VML7 (Local Id=180, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 08:29:26.087643', '2026-11-11 08:37:55.250217', NULL, '', @uid, NULL, 'CZZ7VML7', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 08:37:55.256276', 3, 24, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'CZZ7VML7' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 08:29:33.815751', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 4, 1, 'Cannon', 'Black', 1, '2026-11-11 08:29:34.700377', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 3, 4, 'Cannon', 'Red', 2, '2026-11-11 08:29:55.695573', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 4, 8, 'Cannon', 'Black', 3, '2026-11-11 08:29:56.504911', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 8, 'Cannon', 'Red', 4, '2026-11-11 08:31:34.056108', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 4, 4, 'Cannon', 'Black', 5, '2026-11-11 08:31:34.970600', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 6, '2026-11-11 08:31:43.353403', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 4, 6, 'Cannon', 'Black', 7, '2026-11-11 08:31:44.534749', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 8, '2026-11-11 08:31:58.760894', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 9, '2026-11-11 08:31:59.680756', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 4, 4, 'Cannon', 'Red', 10, '2026-11-11 08:32:34.769958', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 7, 6, 'Cannon', 'Black', 11, '2026-11-11 08:32:35.377833', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 12, '2026-11-11 08:33:05.760833', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 13, '2026-11-11 08:33:06.385663', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 14, '2026-11-11 08:33:24.209531', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 6, 6, 'Cannon', 'Black', 15, '2026-11-11 08:33:24.682949', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 7, 5, 'Advisor', 'Red', 16, '2026-11-11 08:34:53.304852', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 17, '2026-11-11 08:34:53.917138', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 4, 2, 'Cannon', 'Red', 18, '2026-11-11 08:35:12.498114', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 4, 2, 'Pawn', 'Black', 19, '2026-11-11 08:35:12.959622', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 20, '2026-11-11 08:35:26.341278', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 5, 2, 'Pawn', 'Black', 21, '2026-11-11 08:35:27.178688', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 5, 3, 'Horse', 'Red', 22, '2026-11-11 08:35:33.673738', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 5, 3, 'Pawn', 'Black', 23, '2026-11-11 08:35:34.457652', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 7, 7, 'Cannon', 'Red', 24, '2026-11-11 08:35:54.105391', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 4, 'Rook', 'Black', 25, '2026-11-11 08:35:54.787151', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 4, 8, 'Pawn', 'Red', 26, '2026-11-11 08:36:06.675569', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 4, 8, 'Pawn', 'Black', 27, '2026-11-11 08:36:07.336228', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 4, 8, 'Rook', 'Red', 28, '2026-11-11 08:36:08.954136', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 4, 8, 'Rook', 'Black', 29, '2026-11-11 08:36:09.377738', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 8, 'Elephant', 'Red', 30, '2026-11-11 08:36:19.642540', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 6, 4, 'Rook', 'Black', 31, '2026-11-11 08:36:20.251616', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 32, '2026-11-11 08:36:27.297997', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 4, 6, 'Cannon', 'Black', 33, '2026-11-11 08:36:28.170496', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 34, '2026-11-11 08:36:52.714214', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 4, 4, 'Cannon', 'Black', 35, '2026-11-11 08:36:53.581888', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 36, '2026-11-11 08:36:56.689655', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 6, 0, 'Rook', 'Black', 37, '2026-11-11 08:36:57.522524', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 3, 'Advisor', 'Red', 38, '2026-11-11 08:37:39.770780', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 9, 0, 'Rook', 'Black', 39, '2026-11-11 08:37:40.390954', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 3, 0, 'Pawn', 'Red', 40, '2026-11-11 08:37:47.162897', NULL, NULL, NULL, NULL),
  (@gid, 5, 3, 5, 4, 'Pawn', 'Black', 41, '2026-11-11 08:37:47.749383', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 42, '2026-11-11 08:37:54.658930', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 2, 'Rook', 'Black', 43, '2026-11-11 08:37:55.245945', NULL, NULL, NULL, NULL);

-- Ván RoomCode=VFKJCRQA (Local Id=181, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 08:38:01.546549', '2026-11-11 08:41:18.330407', NULL, '', @uid, NULL, 'VFKJCRQA', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 08:41:18.336873', 3, 13, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'VFKJCRQA' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 08:38:08.120945', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 4, 1, 'Cannon', 'Black', 1, '2026-11-11 08:38:08.853982', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 2, '2026-11-11 08:38:17.785250', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 4, 4, 'Cannon', 'Black', 3, '2026-11-11 08:38:18.626311', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 7, 4, 'Cannon', 'Red', 4, '2026-11-11 08:38:27.617682', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 4, 6, 'Cannon', 'Black', 5, '2026-11-11 08:38:28.383146', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 8, 'Elephant', 'Red', 6, '2026-11-11 08:38:51.393410', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 8, 'Cannon', 'Black', 7, '2026-11-11 08:38:52.026380', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 8, '2026-11-11 08:38:57.026103', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 4, 0, 'Cannon', 'Black', 9, '2026-11-11 08:38:57.466215', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 7, 0, 'Cannon', 'Red', 10, '2026-11-11 08:39:45.137612', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 4, 2, 'Cannon', 'Black', 11, '2026-11-11 08:39:45.768746', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 12, '2026-11-11 08:40:09.385748', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 9, 2, 'Cannon', 'Black', 13, '2026-11-11 08:40:09.821659', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 14, '2026-11-11 08:40:27.091147', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 9, 0, 'Cannon', 'Black', 15, '2026-11-11 08:40:27.451184', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 8, 3, 'Horse', 'Red', 16, '2026-11-11 08:40:44.363117', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 17, '2026-11-11 08:40:45.445535', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 9, 6, 'Elephant', 'Red', 18, '2026-11-11 08:40:52.626997', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 1, 3, 'Rook', 'Black', 19, '2026-11-11 08:40:53.916299', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 9, 3, 'King', 'Red', 20, '2026-11-11 08:41:02.819669', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 2, 1, 'Cannon', 'Black', 21, '2026-11-11 08:41:04.231833', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 4, 8, 'Pawn', 'Red', 22, '2026-11-11 08:41:17.649780', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 9, 1, 'Cannon', 'Black', 23, '2026-11-11 08:41:18.325644', NULL, NULL, NULL, NULL);

-- Ván RoomCode=KZ9HKS8Q (Local Id=182, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 08:41:34.350055', '2026-11-11 08:48:06.528999', NULL, '', @uid, NULL, 'KZ9HKS8Q', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 08:48:06.538183', 3, 22, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'KZ9HKS8Q' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 0, '2026-11-11 08:41:45.554651', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 4, 1, 'Cannon', 'Black', 1, '2026-11-11 08:41:49.160072', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 8, 'Cannon', 'Red', 2, '2026-11-11 08:42:00.571079', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 3, '2026-11-11 08:42:04.612492', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 4, '2026-11-11 08:42:39.281881', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 4, 0, 'Cannon', 'Black', 5, '2026-11-11 08:42:43.066676', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 0, 'Cannon', 'Red', 6, '2026-11-11 08:42:56.635564', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Pawn', 'Black', 7, '2026-11-11 08:42:59.672438', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 4, 6, 'Pawn', 'Red', 8, '2026-11-11 08:43:02.938378', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 7, 6, 'Cannon', 'Black', 9, '2026-11-11 08:43:04.709702', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 4, 'Elephant', 'Red', 10, '2026-11-11 08:43:19.957124', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 7, 0, 'Cannon', 'Black', 11, '2026-11-11 08:43:20.836726', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 7, 0, 'Rook', 'Red', 12, '2026-11-11 08:43:23.748310', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 7, 0, 'Cannon', 'Black', 13, '2026-11-11 08:43:24.650076', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 7, 0, 'Elephant', 'Red', 14, '2026-11-11 08:43:37.306956', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 4, 'Elephant', 'Black', 15, '2026-11-11 08:43:38.185616', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 3, 6, 'Pawn', 'Red', 16, '2026-11-11 08:43:52.475746', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 2, 0, 'Rook', 'Black', 17, '2026-11-11 08:43:53.568336', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 3, 7, 'Pawn', 'Red', 18, '2026-11-11 08:44:00.614101', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 2, 1, 'Rook', 'Black', 19, '2026-11-11 08:44:01.529173', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 8, 3, 'Horse', 'Red', 20, '2026-11-11 08:44:19.436747', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 21, '2026-11-11 08:44:21.060972', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 22, '2026-11-11 08:44:45.013038', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 23, '2026-11-11 08:44:47.624909', NULL, NULL, NULL, NULL),
  (@gid, 3, 7, 3, 8, 'Pawn', 'Red', 24, '2026-11-11 08:45:14.956299', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 1, 3, 'Rook', 'Black', 25, '2026-11-11 08:45:16.733483', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 2, 8, 'Pawn', 'Red', 26, '2026-11-11 08:45:32.844052', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 8, 3, 'Rook', 'Black', 27, '2026-11-11 08:45:34.139517', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 28, '2026-11-11 08:46:03.420915', NULL, NULL, NULL, NULL),
  (@gid, 8, 3, 6, 3, 'Rook', 'Black', 29, '2026-11-11 08:46:04.696011', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 2, 7, 'Pawn', 'Red', 30, '2026-11-11 08:46:16.292357', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 4, 5, 'Horse', 'Black', 31, '2026-11-11 08:46:17.746148', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 4, 8, 'Pawn', 'Red', 32, '2026-11-11 08:46:22.099530', NULL, NULL, NULL, NULL),
  (@gid, 6, 3, 6, 4, 'Rook', 'Black', 33, '2026-11-11 08:46:25.422783', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 7, 5, 'Advisor', 'Red', 34, '2026-11-11 08:46:47.427989', NULL, NULL, NULL, NULL),
  (@gid, 4, 5, 5, 3, 'Horse', 'Black', 35, '2026-11-11 08:46:50.888400', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Pawn', 'Red', 36, '2026-11-11 08:47:04.333958', NULL, NULL, NULL, NULL),
  (@gid, 5, 3, 7, 4, 'Horse', 'Black', 37, '2026-11-11 08:47:05.603472', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 7, 4, 'Cannon', 'Red', 38, '2026-11-11 08:47:14.382927', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 7, 4, 'Rook', 'Black', 39, '2026-11-11 08:47:15.202794', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 40, '2026-11-11 08:47:25.060816', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 9, 1, 'Rook', 'Black', 41, '2026-11-11 08:47:25.333032', NULL, NULL, NULL, NULL);

-- Ván RoomCode=G9HS4TWF (Local Id=186, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 09:44:25.574467', '2026-11-11 09:53:09.188594', NULL, '', @uid, NULL, 'G9HS4TWF', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 09:53:09.206489', 3, 24, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'G9HS4TWF' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 09:44:47.207977', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 09:44:47.335942', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 2, '2026-11-11 09:45:11.495436', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 09:45:11.635259', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 4, '2026-11-11 09:45:31.022529', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 6, 6, 'Cannon', 'Black', 5, '2026-11-11 09:45:31.168715', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 6, 'Cannon', 'Red', 6, '2026-11-11 09:45:43.671658', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 7, '2026-11-11 09:45:43.827660', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 5, 6, 'Cannon', 'Red', 8, '2026-11-11 09:46:04.006578', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 2, 6, 'Cannon', 'Black', 9, '2026-11-11 09:46:04.188299', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 10, '2026-11-11 09:46:52.662717', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 11, '2026-11-11 09:46:52.783583', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 7, 8, 'Rook', 'Red', 12, '2026-11-11 09:47:12.136167', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 4, 0, 'Pawn', 'Black', 13, '2026-11-11 09:47:12.369099', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 4, 'Cannon', 'Red', 14, '2026-11-11 09:47:21.239189', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 4, 'Elephant', 'Black', 15, '2026-11-11 09:47:21.313438', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 16, '2026-11-11 09:47:44.392465', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 9, 6, 'Cannon', 'Black', 17, '2026-11-11 09:47:44.515240', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 18, '2026-11-11 09:48:09.110648', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 4, 4, 'Pawn', 'Black', 19, '2026-11-11 09:48:09.290750', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 5, 5, 'Cannon', 'Red', 20, '2026-11-11 09:48:41.624821', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 5, 7, 'Rook', 'Black', 21, '2026-11-11 09:48:41.820003', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 22, '2026-11-11 09:49:14.906859', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 5, 5, 'Rook', 'Black', 23, '2026-11-11 09:49:15.084386', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 24, '2026-11-11 09:49:51.535188', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 9, 3, 'Cannon', 'Black', 25, '2026-11-11 09:49:51.726625', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 9, 3, 'Horse', 'Red', 26, '2026-11-11 09:50:07.001950', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 4, 6, 'Cannon', 'Black', 27, '2026-11-11 09:50:07.190026', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 4, 8, 'Pawn', 'Red', 28, '2026-11-11 09:50:52.400193', NULL, NULL, NULL, NULL),
  (@gid, 5, 5, 9, 5, 'Rook', 'Black', 29, '2026-11-11 09:50:52.549145', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 3, 8, 'Pawn', 'Red', 30, '2026-11-11 09:51:06.928505', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 9, 3, 'Rook', 'Black', 31, '2026-11-11 09:51:07.057448', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 2, 8, 'Pawn', 'Red', 32, '2026-11-11 09:51:32.248639', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 9, 7, 'Rook', 'Black', 33, '2026-11-11 09:51:32.391790', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 8, 8, 'Rook', 'Red', 34, '2026-11-11 09:52:06.496405', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 7, 'Rook', 'Black', 35, '2026-11-11 09:52:06.597761', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 7, 4, 'Elephant', 'Red', 36, '2026-11-11 09:52:20.615747', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 4, 0, 'Cannon', 'Black', 37, '2026-11-11 09:52:20.733949', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 38, '2026-11-11 09:52:27.761297', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 8, 0, 'Cannon', 'Black', 39, '2026-11-11 09:52:27.869287', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 8, 0, 'Rook', 'Red', 40, '2026-11-11 09:52:32.441732', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 8, 0, 'Rook', 'Black', 41, '2026-11-11 09:52:32.480380', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 4, 'King', 'Red', 42, '2026-11-11 09:52:40.682803', NULL, NULL, NULL, NULL),
  (@gid, 8, 0, 8, 8, 'Rook', 'Black', 43, '2026-11-11 09:52:40.730073', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 2, 7, 'Pawn', 'Red', 44, '2026-11-11 09:52:56.074800', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 9, 7, 'Rook', 'Black', 45, '2026-11-11 09:52:56.111056', NULL, NULL, NULL, NULL);

-- Ván RoomCode=2VRQ2G8D (Local Id=187, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 09:53:11.112842', '2026-11-11 10:05:38.429009', NULL, '', @uid, NULL, '2VRQ2G8D', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 10:05:38.437738', 3, 28, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '2VRQ2G8D' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 09:53:22.327990', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 1, '2026-11-11 09:53:22.458847', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 2, '2026-11-11 09:53:35.745431', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 09:53:35.890144', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 3, 4, 'Cannon', 'Red', 4, '2026-11-11 09:54:17.345851', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 3, 4, 'Horse', 'Black', 5, '2026-11-11 09:54:17.411850', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 4, 4, 'Pawn', 'Red', 6, '2026-11-11 09:54:23.489570', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 7, '2026-11-11 09:54:23.650104', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 8, '2026-11-11 09:54:29.083318', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 4, 4, 'Cannon', 'Black', 9, '2026-11-11 09:54:29.209082', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 3, 7, 'Cannon', 'Red', 10, '2026-11-11 09:55:02.481813', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Pawn', 'Black', 11, '2026-11-11 09:55:02.588666', NULL, NULL, NULL, NULL),
  (@gid, 3, 7, 3, 2, 'Cannon', 'Red', 12, '2026-11-11 09:55:07.810783', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 4, 0, 'Cannon', 'Black', 13, '2026-11-11 09:55:07.885419', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 7, 0, 'Rook', 'Red', 14, '2026-11-11 09:56:33.776517', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 7, 0, 'Cannon', 'Black', 15, '2026-11-11 09:56:33.863150', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 0, 'Horse', 'Red', 16, '2026-11-11 09:56:34.960721', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 0, 'Horse', 'Black', 17, '2026-11-11 09:56:34.995464', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 4, 2, 'Cannon', 'Red', 18, '2026-11-11 09:57:31.097072', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 2, 8, 'Rook', 'Black', 19, '2026-11-11 09:57:31.153271', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 0, 7, 'Rook', 'Red', 20, '2026-11-11 09:57:52.112869', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 2, 6, 'Rook', 'Black', 21, '2026-11-11 09:57:52.180164', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 22, '2026-11-11 09:58:42.144719', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 0, 1, 'Rook', 'Black', 23, '2026-11-11 09:58:42.259464', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 4, 4, 'Cannon', 'Red', 24, '2026-11-11 09:59:32.281708', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 2, 2, 'Horse', 'Black', 25, '2026-11-11 09:59:32.336223', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 26, '2026-11-11 10:00:09.673769', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 4, 1, 'Rook', 'Black', 27, '2026-11-11 10:00:09.751620', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 5, 4, 'Cannon', 'Red', 28, '2026-11-11 10:00:32.458840', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 5, 6, 'Pawn', 'Black', 29, '2026-11-11 10:00:32.522197', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 30, '2026-11-11 10:00:48.650251', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 5, 6, 'Rook', 'Black', 31, '2026-11-11 10:00:48.728003', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 6, 4, 'Cannon', 'Red', 32, '2026-11-11 10:03:21.460724', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 6, 6, 'Rook', 'Black', 33, '2026-11-11 10:03:21.600097', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 7, 4, 'Cannon', 'Red', 34, '2026-11-11 10:03:32.626638', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 9, 6, 'Rook', 'Black', 35, '2026-11-11 10:03:32.776986', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 0, 6, 'Rook', 'Red', 36, '2026-11-11 10:04:15.043601', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 0, 6, 'Rook', 'Black', 37, '2026-11-11 10:04:15.095943', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 38, '2026-11-11 10:04:30.481911', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 5, 6, 'Rook', 'Black', 39, '2026-11-11 10:04:30.562047', NULL, NULL, NULL, NULL),
  (@gid, 7, 0, 6, 2, 'Horse', 'Red', 40, '2026-11-11 10:04:47.394821', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 2, 'Rook', 'Black', 41, '2026-11-11 10:04:47.477779', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 2, 4, 'Cannon', 'Red', 42, '2026-11-11 10:04:55.819414', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 4, 'Elephant', 'Black', 43, '2026-11-11 10:04:55.884964', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 4, 8, 'Pawn', 'Red', 44, '2026-11-11 10:05:04.579083', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 6, 2, 'Rook', 'Black', 45, '2026-11-11 10:05:04.625658', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 7, 4, 'Elephant', 'Red', 46, '2026-11-11 10:05:08.410936', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 4, 8, 'Pawn', 'Black', 47, '2026-11-11 10:05:08.446628', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 48, '2026-11-11 10:05:14.059355', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 4, 4, 'Rook', 'Black', 49, '2026-11-11 10:05:14.093661', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 3, 0, 'Pawn', 'Red', 50, '2026-11-11 10:05:27.440962', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 7, 4, 'Rook', 'Black', 51, '2026-11-11 10:05:27.488195', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 52, '2026-11-11 10:05:32.196071', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 3, 0, 'Horse', 'Black', 53, '2026-11-11 10:05:32.240211', NULL, NULL, NULL, NULL);

-- Ván RoomCode=BX2XCBJC (Local Id=188, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 10:05:40.250409', '2026-11-11 10:10:26.205921', NULL, '', @uid, NULL, 'BX2XCBJC', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 10:10:26.215438', 3, 16, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'BX2XCBJC' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 10:05:53.899174', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 10:05:54.015385', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 2, '2026-11-11 10:05:59.786224', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 10:05:59.928642', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 6, 'Cannon', 'Red', 4, '2026-11-11 10:06:03.802317', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 5, '2026-11-11 10:06:03.883749', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Cannon', 'Red', 6, '2026-11-11 10:06:16.787565', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 7, '2026-11-11 10:06:16.966405', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 8, '2026-11-11 10:07:06.972563', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 9, '2026-11-11 10:07:07.064253', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 0, 6, 'Cannon', 'Red', 10, '2026-11-11 10:07:22.523474', NULL, NULL, NULL, NULL),
  (@gid, 0, 5, 1, 4, 'Advisor', 'Black', 11, '2026-11-11 10:07:22.593653', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 8, 'Cannon', 'Red', 12, '2026-11-11 10:08:52.083436', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 0, 6, 'Rook', 'Black', 13, '2026-11-11 10:08:52.167498', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 3, 8, 'Cannon', 'Red', 14, '2026-11-11 10:08:54.883989', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 7, 6, 'Cannon', 'Black', 15, '2026-11-11 10:08:55.028986', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 16, '2026-11-11 10:09:26.259548', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 4, 3, 'Horse', 'Black', 17, '2026-11-11 10:09:26.382465', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 18, '2026-11-11 10:09:37.747339', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 5, 6, 'Rook', 'Black', 19, '2026-11-11 10:09:37.883175', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 20, '2026-11-11 10:09:57.782531', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 4, 'Rook', 'Black', 21, '2026-11-11 10:09:57.935684', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 22, '2026-11-11 10:10:05.580496', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 2, 2, 'Cannon', 'Black', 23, '2026-11-11 10:10:05.784359', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 3, 0, 'Cannon', 'Red', 24, '2026-11-11 10:10:26.131160', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 9, 2, 'Cannon', 'Black', 25, '2026-11-11 10:10:26.197771', NULL, NULL, NULL, NULL);

-- Ván RoomCode=9TJ336DN (Local Id=189, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 10:10:48.371269', '2026-11-11 10:11:34.983432', NULL, '', @uid, NULL, '9TJ336DN', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 10:11:34.990962', 3, 4, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '9TJ336DN' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 9, 6, 7, 4, 'Elephant', 'Red', 0, '2026-11-11 10:11:00.474976', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 1, '2026-11-11 10:11:00.594263', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 5, 2, 'Elephant', 'Red', 2, '2026-11-11 10:11:09.243712', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 10:11:09.404534', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 0, 7, 'Cannon', 'Red', 4, '2026-11-11 10:11:31.852707', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 5, '2026-11-11 10:11:31.926951', NULL, NULL, NULL, NULL);

-- Ván RoomCode=9AS58YQT (Local Id=190, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 10:11:35.951409', '2026-11-11 10:16:31.168673', NULL, '', @uid, NULL, '9AS58YQT', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 10:16:31.181733', 3, 13, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '9AS58YQT' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 0, '2026-11-11 10:11:44.916278', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 2, 'Cannon', 'Black', 1, '2026-11-11 10:11:45.050981', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 7, 4, 'Cannon', 'Red', 2, '2026-11-11 10:11:54.036456', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 6, 2, 'Cannon', 'Black', 3, '2026-11-11 10:11:54.162115', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 3, 4, 'Cannon', 'Red', 4, '2026-11-11 10:12:02.172042', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 5, '2026-11-11 10:12:02.307914', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 3, 5, 'Cannon', 'Red', 6, '2026-11-11 10:12:25.203771', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 1, 4, 'Horse', 'Black', 7, '2026-11-11 10:12:25.418596', NULL, NULL, NULL, NULL),
  (@gid, 3, 5, 4, 5, 'Cannon', 'Red', 8, '2026-11-11 10:12:38.028747', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 4, 'Cannon', 'Black', 9, '2026-11-11 10:12:38.160636', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 10, '2026-11-11 10:12:43.890297', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 0, 1, 'Rook', 'Black', 11, '2026-11-11 10:12:44.008659', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 0, 'Cannon', 'Red', 12, '2026-11-11 10:12:51.940708', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 4, 1, 'Rook', 'Black', 13, '2026-11-11 10:12:52.083439', NULL, NULL, NULL, NULL),
  (@gid, 7, 0, 3, 0, 'Cannon', 'Red', 14, '2026-11-11 10:13:02.756583', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 4, 5, 'Rook', 'Black', 15, '2026-11-11 10:13:02.905306', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 3, 6, 'Cannon', 'Red', 16, '2026-11-11 10:13:23.860171', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 2, 0, 'Cannon', 'Black', 17, '2026-11-11 10:13:23.961187', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 3, 4, 'Cannon', 'Red', 18, '2026-11-11 10:14:06.788332', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 2, 2, 'Horse', 'Black', 19, '2026-11-11 10:14:06.853627', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 2, 4, 'Cannon', 'Red', 20, '2026-11-11 10:16:12.013226', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 9, 0, 'Cannon', 'Black', 21, '2026-11-11 10:16:12.074314', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 22, '2026-11-11 10:16:31.118477', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 2, 'Cannon', 'Black', 23, '2026-11-11 10:16:31.162250', NULL, NULL, NULL, NULL);

-- Ván RoomCode=YQVAJEDY (Local Id=191, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 10:16:45.204220', '2026-11-11 10:20:17.663903', NULL, '', @uid, NULL, 'YQVAJEDY', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 10:20:17.681189', 3, 9, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'YQVAJEDY' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 0, '2026-11-11 10:16:52.771853', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 2, 'Cannon', 'Black', 1, '2026-11-11 10:16:52.879680', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 3, 6, 'Cannon', 'Red', 2, '2026-11-11 10:17:05.884876', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 10:17:06.030824', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 4, '2026-11-11 10:17:30.797285', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 0, 'Horse', 'Black', 5, '2026-11-11 10:17:30.920245', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 6, '2026-11-11 10:18:04.517194', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 7, '2026-11-11 10:18:04.684256', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 5, 6, 'Cannon', 'Red', 8, '2026-11-11 10:18:16.757914', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 1, 5, 'Rook', 'Black', 9, '2026-11-11 10:18:16.972592', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 10, '2026-11-11 10:19:22.029561', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 4, 2, 'Cannon', 'Black', 11, '2026-11-11 10:19:22.154326', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 0, 'Cannon', 'Red', 12, '2026-11-11 10:19:52.720236', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 9, 2, 'Cannon', 'Black', 13, '2026-11-11 10:19:52.895461', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 14, '2026-11-11 10:20:13.437593', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 9, 0, 'Cannon', 'Black', 15, '2026-11-11 10:20:13.621877', NULL, NULL, NULL, NULL);

-- Ván RoomCode=HUNTG5KL (Local Id=193, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 10:21:37.096618', '2026-11-11 10:24:18.836360', NULL, '', @uid, NULL, 'HUNTG5KL', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 10:24:18.846360', 3, 7, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'HUNTG5KL' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 0, '2026-11-11 10:22:04.069741', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 1, '2026-11-11 10:22:04.208324', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 2, '2026-11-11 10:22:31.692840', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 3, '2026-11-11 10:22:31.836846', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 4, '2026-11-11 10:22:50.006005', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 5, 4, 'Cannon', 'Black', 5, '2026-11-11 10:22:50.202314', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 3, 4, 'Cannon', 'Red', 6, '2026-11-11 10:23:00.909508', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 3, 4, 'Horse', 'Black', 7, '2026-11-11 10:23:01.009690', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 5, 7, 'Cannon', 'Red', 8, '2026-11-11 10:23:59.006870', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 4, 6, 'Horse', 'Black', 9, '2026-11-11 10:23:59.092848', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 5, 8, 'Cannon', 'Red', 10, '2026-11-11 10:24:18.782584', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 4, 'Cannon', 'Black', 11, '2026-11-11 10:24:18.829536', NULL, NULL, NULL, NULL);

-- Ván RoomCode=47WMT23G (Local Id=194, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 10:24:26.431201', '2026-11-11 10:28:03.644755', NULL, '', @uid, NULL, '47WMT23G', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 10:28:03.657746', 3, 14, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '47WMT23G' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 10:25:08.854728', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 10:25:08.972648', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 2, '2026-11-11 10:25:23.438500', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 10:25:23.584175', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 6, 'Cannon', 'Red', 4, '2026-11-11 10:25:26.647083', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 5, '2026-11-11 10:25:26.727842', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 5, 6, 'Cannon', 'Red', 6, '2026-11-11 10:25:40.110631', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 7, '2026-11-11 10:25:40.313565', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 0, 'Cannon', 'Red', 8, '2026-11-11 10:25:49.877980', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 9, 6, 'Cannon', 'Black', 9, '2026-11-11 10:25:49.950732', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 10, '2026-11-11 10:26:08.528301', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 9, 8, 'Cannon', 'Black', 11, '2026-11-11 10:26:08.690201', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 5, 'Advisor', 'Red', 12, '2026-11-11 10:26:17.007027', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 0, 1, 'Rook', 'Black', 13, '2026-11-11 10:26:17.109192', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 5, 8, 'Cannon', 'Red', 14, '2026-11-11 10:26:29.112381', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 15, '2026-11-11 10:26:29.212806', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 9, 8, 'Cannon', 'Red', 16, '2026-11-11 10:26:32.351292', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 8, 1, 'Rook', 'Black', 17, '2026-11-11 10:26:32.483638', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 8, 'Cannon', 'Red', 18, '2026-11-11 10:26:51.183967', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 9, 7, 'Rook', 'Black', 19, '2026-11-11 10:26:51.263533', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 3, 8, 'Cannon', 'Red', 20, '2026-11-11 10:27:15.887032', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 9, 6, 'Rook', 'Black', 21, '2026-11-11 10:27:16.167099', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 3, 0, 'Cannon', 'Red', 22, '2026-11-11 10:27:44.463300', NULL, NULL, NULL, NULL),
  (@gid, 8, 1, 8, 5, 'Rook', 'Black', 23, '2026-11-11 10:27:44.608314', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 0, 0, 'Cannon', 'Red', 24, '2026-11-11 10:28:03.591382', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 9, 5, 'Rook', 'Black', 25, '2026-11-11 10:28:03.639113', NULL, NULL, NULL, NULL);

-- Ván RoomCode=2URNJ7HK (Local Id=195, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 10:28:05.294368', '2026-11-11 10:31:33.759383', NULL, '', @uid, NULL, '2URNJ7HK', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 10:31:33.768491', 3, 7, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '2URNJ7HK' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 0, '2026-11-11 10:28:14.774599', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 2, 'Cannon', 'Black', 1, '2026-11-11 10:28:14.896296', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 3, 6, 'Cannon', 'Red', 2, '2026-11-11 10:28:36.790557', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 10:28:36.951869', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 3, 2, 'Cannon', 'Red', 4, '2026-11-11 10:29:40.174823', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 0, 'Horse', 'Black', 5, '2026-11-11 10:29:40.257974', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 4, 2, 'Cannon', 'Red', 6, '2026-11-11 10:30:17.255959', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 7, '2026-11-11 10:30:17.387471', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 4, 8, 'Cannon', 'Red', 8, '2026-11-11 10:30:53.440150', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 9, 2, 'Cannon', 'Black', 9, '2026-11-11 10:30:53.486611', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 10, '2026-11-11 10:31:26.255834', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 9, 0, 'Cannon', 'Black', 11, '2026-11-11 10:31:26.321549', NULL, NULL, NULL, NULL);

-- Ván RoomCode=RXD77HUZ (Local Id=197, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 10:50:16.783686', '2026-11-11 10:58:45.331672', NULL, '', @uid, NULL, 'RXD77HUZ', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 10:58:45.348710', 3, 10, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'RXD77HUZ' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 10:51:41.772003', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 1, '2026-11-11 10:51:41.978925', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 2, '2026-11-11 10:52:30.445400', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 10:52:30.562729', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 4, '2026-11-11 10:53:04.020808', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 5, 'Rook', 'Black', 5, '2026-11-11 10:53:04.169129', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 6, '2026-11-11 10:54:31.332144', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 7, '2026-11-11 10:54:31.609746', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 3, 1, 'Rook', 'Red', 8, '2026-11-11 10:55:23.725135', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 9, '2026-11-11 10:55:23.959405', NULL, NULL, NULL, NULL),
  (@gid, 3, 1, 3, 2, 'Rook', 'Red', 10, '2026-11-11 10:56:29.247234', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 7, 5, 'Rook', 'Black', 11, '2026-11-11 10:56:29.564719', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 12, '2026-11-11 10:58:12.518041', NULL, NULL, NULL, NULL),
  (@gid, 7, 5, 7, 6, 'Rook', 'Black', 13, '2026-11-11 10:58:12.612805', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 14, '2026-11-11 10:58:25.648285', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 15, '2026-11-11 10:58:25.960841', NULL, NULL, NULL, NULL);

-- Ván RoomCode=EZNML87Q (Local Id=198, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 10:58:46.565516', '2026-11-11 11:06:39.260517', NULL, '', @uid, NULL, 'EZNML87Q', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 11:06:39.275928', 3, 9, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'EZNML87Q' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 10:59:05.030249', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 1, '2026-11-11 10:59:05.135087', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 2, '2026-11-11 10:59:12.716285', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 10:59:12.837345', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 4, '2026-11-11 10:59:25.557613', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 5, 'Rook', 'Black', 5, '2026-11-11 10:59:25.756016', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 5, 3, 'Horse', 'Red', 6, '2026-11-11 10:59:59.294530', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 5, 5, 'Rook', 'Black', 7, '2026-11-11 10:59:59.494912', NULL, NULL, NULL, NULL),
  (@gid, 5, 3, 3, 2, 'Horse', 'Red', 8, '2026-11-11 11:01:10.271327', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 9, '2026-11-11 11:01:10.507014', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 10, '2026-11-11 11:01:14.742305', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 5, 'Cannon', 'Black', 11, '2026-11-11 11:01:14.987254', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 1, 1, 'Horse', 'Red', 12, '2026-11-11 11:03:37.998711', NULL, NULL, NULL, NULL),
  (@gid, 2, 5, 9, 5, 'Cannon', 'Black', 13, '2026-11-11 11:03:38.211769', NULL, NULL, NULL, NULL);

-- Ván RoomCode=Y7XFBHXK (Local Id=199, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 11:08:02.734403', '2026-11-11 11:11:45.320295', NULL, '', @uid, NULL, 'Y7XFBHXK', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 11:11:45.331749', 3, 11, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'Y7XFBHXK' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 11:08:06.301912', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 1, '2026-11-11 11:08:06.409319', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 2, '2026-11-11 11:08:31.996842', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 11:08:32.132223', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 4, '2026-11-11 11:08:33.087584', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 5, 'Rook', 'Black', 5, '2026-11-11 11:08:33.258409', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 5, 3, 'Horse', 'Red', 6, '2026-11-11 11:08:43.854145', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 5, 5, 'Rook', 'Black', 7, '2026-11-11 11:08:44.050232', NULL, NULL, NULL, NULL),
  (@gid, 5, 3, 3, 2, 'Horse', 'Red', 8, '2026-11-11 11:09:07.335265', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 9, '2026-11-11 11:09:07.569067', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 10, '2026-11-11 11:09:09.736674', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 5, 'Cannon', 'Black', 11, '2026-11-11 11:09:09.993552', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 1, 1, 'Horse', 'Red', 12, '2026-11-11 11:09:17.879286', NULL, NULL, NULL, NULL),
  (@gid, 2, 5, 9, 5, 'Cannon', 'Black', 13, '2026-11-11 11:09:18.075212', NULL, NULL, NULL, NULL),
  (@gid, 1, 1, 0, 3, 'Horse', 'Red', 14, '2026-11-11 11:09:45.639596', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 9, 7, 'Cannon', 'Black', 15, '2026-11-11 11:09:45.740744', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 16, '2026-11-11 11:10:45.495526', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 17, '2026-11-11 11:10:45.675875', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 18, '2026-11-11 11:11:17.368367', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 8, 7, 'Rook', 'Black', 19, '2026-11-11 11:11:17.425120', NULL, NULL, NULL, NULL);

-- Ván RoomCode=CYW4W53M (Local Id=200, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 11:11:47.110057', '2026-11-11 11:20:47.905691', NULL, '', @uid, NULL, 'CYW4W53M', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 11:20:47.921483', 3, 12, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'CYW4W53M' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 11:11:49.912428', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 1, '2026-11-11 11:11:50.035528', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 2, '2026-11-11 11:11:57.583338', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 11:11:57.714835', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 4, '2026-11-11 11:11:59.001381', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 5, 'Rook', 'Black', 5, '2026-11-11 11:11:59.187673', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 6, '2026-11-11 11:14:46.760005', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 7, '2026-11-11 11:14:46.961807', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 3, 1, 'Rook', 'Red', 8, '2026-11-11 11:16:59.665109', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 9, '2026-11-11 11:16:59.924510', NULL, NULL, NULL, NULL),
  (@gid, 3, 1, 3, 0, 'Rook', 'Red', 10, '2026-11-11 11:18:29.385415', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 4, 2, 'Pawn', 'Black', 11, '2026-11-11 11:18:29.696863', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 12, '2026-11-11 11:18:34.752503', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 7, 5, 'Rook', 'Black', 13, '2026-11-11 11:18:35.105434', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 8, 'Horse', 'Red', 14, '2026-11-11 11:20:33.162481', NULL, NULL, NULL, NULL),
  (@gid, 7, 5, 7, 7, 'Rook', 'Black', 15, '2026-11-11 11:20:33.565788', NULL, NULL, NULL, NULL);

-- Ván RoomCode=4Y5CZDDG (Local Id=201, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 11:20:48.913453', '2026-11-11 11:44:21.343810', NULL, '', @uid, NULL, '4Y5CZDDG', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 11:44:21.360082', 3, 26, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = '4Y5CZDDG' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 11:20:51.377180', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 1, '2026-11-11 11:20:51.486168', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 2, '2026-11-11 11:20:55.360593', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 11:20:55.483421', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 4, '2026-11-11 11:20:56.425371', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 5, 'Rook', 'Black', 5, '2026-11-11 11:20:56.590191', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 6, '2026-11-11 11:21:01.472985', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 7, '2026-11-11 11:21:01.688913', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 3, 1, 'Rook', 'Red', 8, '2026-11-11 11:21:02.985344', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 9, '2026-11-11 11:21:03.255816', NULL, NULL, NULL, NULL),
  (@gid, 3, 1, 3, 0, 'Rook', 'Red', 10, '2026-11-11 11:21:08.337669', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 4, 2, 'Pawn', 'Black', 11, '2026-11-11 11:21:08.610420', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 12, '2026-11-11 11:21:10.713692', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 7, 5, 'Rook', 'Black', 13, '2026-11-11 11:21:11.095883', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 3, 4, 'Cannon', 'Red', 14, '2026-11-11 11:22:46.081505', NULL, NULL, NULL, NULL),
  (@gid, 7, 5, 7, 2, 'Rook', 'Black', 15, '2026-11-11 11:22:46.416132', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 3, 8, 'Cannon', 'Red', 16, '2026-11-11 11:25:34.892553', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 3, 8, 'Rook', 'Black', 17, '2026-11-11 11:25:35.195334', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 8, 'Cannon', 'Red', 18, '2026-11-11 11:26:15.307257', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 3, 7, 'Rook', 'Black', 19, '2026-11-11 11:26:15.461532', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 3, 2, 'Pawn', 'Red', 20, '2026-11-11 11:26:35.386682', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 7, 7, 'Rook', 'Black', 21, '2026-11-11 11:26:35.601449', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 2, 2, 'Pawn', 'Red', 22, '2026-11-11 11:26:49.931208', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 4, 1, 'Cannon', 'Black', 23, '2026-11-11 11:26:50.076006', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 1, 2, 'Pawn', 'Red', 24, '2026-11-11 11:27:37.641588', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 4, 4, 'Cannon', 'Black', 25, '2026-11-11 11:27:37.798654', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 26, '2026-11-11 11:27:51.573154', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 9, 7, 'Rook', 'Black', 27, '2026-11-11 11:27:51.762377', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 28, '2026-11-11 11:27:53.782366', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 29, '2026-11-11 11:27:53.842281', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 4, 'Elephant', 'Red', 30, '2026-11-11 11:28:24.003733', NULL, NULL, NULL, NULL),
  (@gid, 3, 7, 7, 7, 'Rook', 'Black', 31, '2026-11-11 11:28:24.172241', NULL, NULL, NULL, NULL),
  (@gid, 1, 2, 1, 1, 'Pawn', 'Red', 32, '2026-11-11 11:28:40.687238', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 1, 3, 'Horse', 'Black', 33, '2026-11-11 11:28:40.848918', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 2, 0, 'Rook', 'Red', 34, '2026-11-11 11:28:53.893245', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 3, 4, 'Horse', 'Black', 35, '2026-11-11 11:28:54.054517', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 0, 0, 'Rook', 'Red', 36, '2026-11-11 11:29:19.116207', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 8, 'Rook', 'Black', 37, '2026-11-11 11:29:19.230521', NULL, NULL, NULL, NULL),
  (@gid, 1, 1, 1, 2, 'Pawn', 'Red', 38, '2026-11-11 11:29:34.694255', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 7, 4, 'Cannon', 'Black', 39, '2026-11-11 11:29:34.794718', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 9, 5, 'King', 'Red', 40, '2026-11-11 11:29:41.427480', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 7, 1, 'Cannon', 'Black', 41, '2026-11-11 11:29:41.523329', NULL, NULL, NULL, NULL),
  (@gid, 1, 2, 1, 3, 'Pawn', 'Red', 42, '2026-11-11 11:29:48.067718', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 1, 3, 'Horse', 'Black', 43, '2026-11-11 11:29:48.128938', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 0, 3, 'Rook', 'Red', 44, '2026-11-11 11:29:54.100018', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 0, 3, 'King', 'Black', 45, '2026-11-11 11:29:54.125352', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 46, '2026-11-11 11:32:49.678002', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 6, 8, 'Rook', 'Black', 47, '2026-11-11 11:32:49.738432', NULL, NULL, NULL, NULL);

-- Ván RoomCode=LMT5JSAA (Local Id=202, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 13:47:52.512589', '2026-11-11 13:51:52.860811', NULL, '', @uid, NULL, 'LMT5JSAA', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 13:51:52.881643', 3, 24, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'LMT5JSAA' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 5, 1, 'Cannon', 'Red', 0, '2026-11-11 13:48:10.360690', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 9, 1, 'Cannon', 'Black', 1, '2026-11-11 13:48:10.612245', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 2, '2026-11-11 13:48:16.719500', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 3, '2026-11-11 13:48:16.844186', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 4, '2026-11-11 13:48:25.011282', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 5, '2026-11-11 13:48:25.096739', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 8, 'Cannon', 'Red', 6, '2026-11-11 13:48:29.099463', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 7, '2026-11-11 13:48:29.176055', NULL, NULL, NULL, NULL),
  (@gid, 5, 1, 5, 0, 'Cannon', 'Red', 8, '2026-11-11 13:48:37.913835', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 9, '2026-11-11 13:48:37.983708', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 10, '2026-11-11 13:49:25.794678', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 1, 4, 'Horse', 'Black', 11, '2026-11-11 13:49:25.934697', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 5, 4, 'Cannon', 'Red', 12, '2026-11-11 13:49:45.658905', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 4, 4, 'Pawn', 'Black', 13, '2026-11-11 13:49:45.756918', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 1, 4, 'Cannon', 'Red', 14, '2026-11-11 13:50:04.987177', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 1, 4, 'Rook', 'Black', 15, '2026-11-11 13:50:05.103885', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 4, 8, 'Pawn', 'Red', 16, '2026-11-11 13:50:10.140510', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 4, 8, 'Pawn', 'Black', 17, '2026-11-11 13:50:10.213473', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 0, 7, 'Rook', 'Red', 18, '2026-11-11 13:50:23.633897', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 4, 'Elephant', 'Black', 19, '2026-11-11 13:50:23.692701', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 0, 5, 'Rook', 'Red', 20, '2026-11-11 13:50:28.371382', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 0, 5, 'King', 'Black', 21, '2026-11-11 13:50:28.402063', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 22, '2026-11-11 13:50:32.234085', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 1, 5, 'Rook', 'Black', 23, '2026-11-11 13:50:32.293347', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 4, 6, 'Pawn', 'Red', 24, '2026-11-11 13:50:33.906787', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 9, 5, 'Rook', 'Black', 25, '2026-11-11 13:50:33.959048', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 26, '2026-11-11 13:50:48.616283', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 9, 3, 'Rook', 'Black', 27, '2026-11-11 13:50:48.735297', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 9, 8, 'Cannon', 'Red', 28, '2026-11-11 13:50:56.594788', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 9, 6, 'Rook', 'Black', 29, '2026-11-11 13:50:56.641013', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 8, 8, 'Cannon', 'Red', 30, '2026-11-11 13:51:22.314370', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 8, 6, 'Rook', 'Black', 31, '2026-11-11 13:51:22.380759', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 4, 'King', 'Red', 32, '2026-11-11 13:51:25.941445', NULL, NULL, NULL, NULL),
  (@gid, 8, 6, 8, 8, 'Rook', 'Black', 33, '2026-11-11 13:51:26.019263', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 9, 3, 'King', 'Red', 34, '2026-11-11 13:51:30.211153', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Pawn', 'Black', 35, '2026-11-11 13:51:30.263033', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 36, '2026-11-11 13:51:32.358763', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 5, 4, 'Pawn', 'Black', 37, '2026-11-11 13:51:32.406220', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 38, '2026-11-11 13:51:33.614233', NULL, NULL, NULL, NULL),
  (@gid, 8, 8, 9, 8, 'Rook', 'Black', 39, '2026-11-11 13:51:33.676322', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 3, 'King', 'Red', 40, '2026-11-11 13:51:39.257870', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 5, 8, 'Pawn', 'Black', 41, '2026-11-11 13:51:39.321361', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 7, 0, 'Elephant', 'Red', 42, '2026-11-11 13:51:48.363333', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 1, 'Rook', 'Black', 43, '2026-11-11 13:51:48.401770', NULL, NULL, NULL, NULL);

-- Ván RoomCode=YBW78XQZ (Local Id=203, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 13:52:00.409623', '2026-11-11 13:54:29.891584', NULL, '', @uid, NULL, 'YBW78XQZ', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 14:01:12.757503', 3, 5, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'YBW78XQZ' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 13:52:11.657889', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 1, '2026-11-11 13:52:11.781362', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 2, '2026-11-11 13:52:25.434243', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 13:52:25.574006', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 4, '2026-11-11 13:52:26.872012', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 5, 'Rook', 'Black', 5, '2026-11-11 13:52:27.031653', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 6, '2026-11-11 13:54:29.699054', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 7, '2026-11-11 13:54:29.882797', NULL, NULL, NULL, NULL);

-- Ván RoomCode=QQZPQCE4 (Local Id=204, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 14:01:18.431142', '2026-11-11 14:05:11.629089', NULL, '', @uid, NULL, 'QQZPQCE4', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 14:05:11.642405', 3, 14, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'QQZPQCE4' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 14:01:32.853135', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 14:01:32.985545', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 2, '2026-11-11 14:01:34.988306', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 6, 6, 'Cannon', 'Black', 3, '2026-11-11 14:01:35.113604', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 5, 7, 'Cannon', 'Red', 4, '2026-11-11 14:02:20.590475', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 6, 2, 'Cannon', 'Black', 5, '2026-11-11 14:02:20.768560', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 5, 8, 'Cannon', 'Red', 6, '2026-11-11 14:02:26.942713', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 9, 2, 'Cannon', 'Black', 7, '2026-11-11 14:02:27.005221', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 8, '2026-11-11 14:02:35.277382', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 9, '2026-11-11 14:02:35.426348', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 0, 'Horse', 'Red', 10, '2026-11-11 14:03:05.684068', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 7, 1, 'Cannon', 'Black', 11, '2026-11-11 14:03:05.751714', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 2, 'Rook', 'Red', 12, '2026-11-11 14:03:10.333194', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 0, 'Horse', 'Black', 13, '2026-11-11 14:03:10.460806', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 5, 7, 'Horse', 'Red', 14, '2026-11-11 14:04:05.756747', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 15, '2026-11-11 14:04:05.873859', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 3, 6, 'Horse', 'Red', 16, '2026-11-11 14:04:43.782193', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 8, 7, 'Rook', 'Black', 17, '2026-11-11 14:04:43.852000', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 4, 'King', 'Red', 18, '2026-11-11 14:04:51.526778', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 3, 6, 'Horse', 'Black', 19, '2026-11-11 14:04:51.604317', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 5, 6, 'Cannon', 'Red', 20, '2026-11-11 14:05:02.542204', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 4, 'Horse', 'Black', 21, '2026-11-11 14:05:02.677033', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 22, '2026-11-11 14:05:08.524889', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 5, 6, 'Horse', 'Black', 23, '2026-11-11 14:05:08.625024', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 4, 8, 'Pawn', 'Red', 24, '2026-11-11 14:05:11.568712', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 7, 5, 'Horse', 'Black', 25, '2026-11-11 14:05:11.622971', NULL, NULL, NULL, NULL);

-- Ván RoomCode=RYGMT36A (Local Id=205, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 14:05:25.684964', '2026-11-11 14:09:51.433337', NULL, '', @uid, NULL, 'RYGMT36A', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 14:09:51.456399', 3, 27, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'RYGMT36A' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 0, '2026-11-11 14:05:31.597622', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 1, '2026-11-11 14:05:31.732098', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 3, 1, 'Cannon', 'Red', 2, '2026-11-11 14:06:10.926371', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 3, '2026-11-11 14:06:11.115994', NULL, NULL, NULL, NULL),
  (@gid, 3, 1, 3, 4, 'Cannon', 'Red', 4, '2026-11-11 14:06:14.124834', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 3, 4, 'Horse', 'Black', 5, '2026-11-11 14:06:14.238127', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 6, '2026-11-11 14:06:30.108837', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 4, 'Rook', 'Black', 7, '2026-11-11 14:06:30.260010', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 8, 8, 'Rook', 'Red', 8, '2026-11-11 14:06:37.414034', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 9, '2026-11-11 14:06:37.525927', NULL, NULL, NULL, NULL),
  (@gid, 8, 8, 8, 7, 'Rook', 'Red', 10, '2026-11-11 14:06:44.967235', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 7, 'Cannon', 'Black', 11, '2026-11-11 14:06:45.061136', NULL, NULL, NULL, NULL),
  (@gid, 8, 7, 7, 7, 'Rook', 'Red', 12, '2026-11-11 14:06:46.201245', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 6, 1, 'Cannon', 'Black', 13, '2026-11-11 14:06:46.380908', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 3, 7, 'Rook', 'Red', 14, '2026-11-11 14:07:00.433360', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 1, 5, 'Horse', 'Black', 15, '2026-11-11 14:07:00.547584', NULL, NULL, NULL, NULL),
  (@gid, 3, 7, 3, 8, 'Rook', 'Red', 16, '2026-11-11 14:07:04.819119', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 3, 8, 'Rook', 'Black', 17, '2026-11-11 14:07:04.882797', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 4, 8, 'Pawn', 'Red', 18, '2026-11-11 14:07:07.813607', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 6, 4, 'Rook', 'Black', 19, '2026-11-11 14:07:07.866022', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 8, 4, 'Advisor', 'Red', 20, '2026-11-11 14:07:13.757336', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 4, 8, 'Rook', 'Black', 21, '2026-11-11 14:07:13.804121', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 22, '2026-11-11 14:07:28.525216', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 6, 2, 'Rook', 'Black', 23, '2026-11-11 14:07:28.599549', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 7, 0, 'Rook', 'Red', 24, '2026-11-11 14:07:39.861911', NULL, NULL, NULL, NULL),
  (@gid, 4, 8, 4, 5, 'Rook', 'Black', 25, '2026-11-11 14:07:40.012215', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 26, '2026-11-11 14:08:06.774765', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Pawn', 'Black', 27, '2026-11-11 14:08:06.883818', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 4, 6, 'Pawn', 'Red', 28, '2026-11-11 14:08:08.317281', NULL, NULL, NULL, NULL),
  (@gid, 4, 5, 4, 6, 'Rook', 'Black', 29, '2026-11-11 14:08:08.387354', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 4, 'Elephant', 'Red', 30, '2026-11-11 14:08:17.496054', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 4, 2, 'Pawn', 'Black', 31, '2026-11-11 14:08:17.587758', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 32, '2026-11-11 14:08:20.039095', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 33, '2026-11-11 14:08:20.135546', NULL, NULL, NULL, NULL),
  (@gid, 7, 0, 7, 1, 'Rook', 'Red', 34, '2026-11-11 14:08:32.216168', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 4, 2, 'Rook', 'Black', 35, '2026-11-11 14:08:32.301918', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 6, 1, 'Rook', 'Red', 36, '2026-11-11 14:08:34.053749', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 6, 1, 'Rook', 'Black', 37, '2026-11-11 14:08:34.103657', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 8, 0, 'Horse', 'Red', 38, '2026-11-11 14:09:08.247848', NULL, NULL, NULL, NULL),
  (@gid, 6, 1, 8, 1, 'Rook', 'Black', 39, '2026-11-11 14:09:08.292670', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 40, '2026-11-11 14:09:30.861801', NULL, NULL, NULL, NULL),
  (@gid, 8, 1, 8, 0, 'Rook', 'Black', 41, '2026-11-11 14:09:30.925150', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 42, '2026-11-11 14:09:42.558975', NULL, NULL, NULL, NULL),
  (@gid, 8, 0, 8, 3, 'Rook', 'Black', 43, '2026-11-11 14:09:42.613259', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 3, 0, 'Pawn', 'Red', 44, '2026-11-11 14:09:45.100815', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 9, 2, 'Rook', 'Black', 45, '2026-11-11 14:09:45.143791', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 3, 'Advisor', 'Red', 46, '2026-11-11 14:09:51.391875', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 9, 3, 'Rook', 'Black', 47, '2026-11-11 14:09:51.427283', NULL, NULL, NULL, NULL);

-- Ván RoomCode=Z5TYHB8U (Local Id=206, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 14:09:57.256505', '2026-11-11 14:16:55.432414', NULL, '', @uid, NULL, 'Z5TYHB8U', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 14:16:55.442437', 3, 25, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'Z5TYHB8U' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 14:10:31.231068', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 14:10:31.341942', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 2, '2026-11-11 14:10:45.621744', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 6, 6, 'Cannon', 'Black', 3, '2026-11-11 14:10:45.733924', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 4, '2026-11-11 14:10:56.977736', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Pawn', 'Black', 5, '2026-11-11 14:10:57.139846', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 5, 2, 'Pawn', 'Red', 6, '2026-11-11 14:11:25.102750', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 5, 6, 'Pawn', 'Black', 7, '2026-11-11 14:11:25.252988', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 8, '2026-11-11 14:11:59.543591', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 3, 'Cannon', 'Black', 9, '2026-11-11 14:11:59.731749', NULL, NULL, NULL, NULL),
  (@gid, 5, 2, 4, 2, 'Pawn', 'Red', 10, '2026-11-11 14:12:10.366716', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 4, 'Elephant', 'Black', 11, '2026-11-11 14:12:10.537296', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 4, 'Elephant', 'Red', 12, '2026-11-11 14:12:41.559033', NULL, NULL, NULL, NULL),
  (@gid, 2, 3, 7, 3, 'Cannon', 'Black', 13, '2026-11-11 14:12:41.670142', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 5, 6, 'Elephant', 'Red', 14, '2026-11-11 14:12:46.103801', NULL, NULL, NULL, NULL),
  (@gid, 7, 3, 7, 7, 'Cannon', 'Black', 15, '2026-11-11 14:12:46.181635', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 5, 3, 'Horse', 'Red', 16, '2026-11-11 14:13:17.393132', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 1, 3, 'Horse', 'Black', 17, '2026-11-11 14:13:17.516679', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 18, '2026-11-11 14:13:32.648168', NULL, NULL, NULL, NULL),
  (@gid, 1, 3, 3, 2, 'Horse', 'Black', 19, '2026-11-11 14:13:32.808917', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 3, 2, 'Pawn', 'Red', 20, '2026-11-11 14:13:46.751639', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 21, '2026-11-11 14:13:46.868297', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 6, 'Rook', 'Red', 22, '2026-11-11 14:14:30.215883', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 9, 6, 'Cannon', 'Black', 23, '2026-11-11 14:14:30.301278', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 24, '2026-11-11 14:14:40.504554', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 5, 6, 'Cannon', 'Black', 25, '2026-11-11 14:14:40.602527', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 26, '2026-11-11 14:15:03.407714', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 3, 'Cannon', 'Black', 27, '2026-11-11 14:15:03.458288', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 3, 'Pawn', 'Red', 28, '2026-11-11 14:15:08.689741', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 29, '2026-11-11 14:15:08.785890', NULL, NULL, NULL, NULL),
  (@gid, 3, 3, 3, 4, 'Pawn', 'Red', 30, '2026-11-11 14:15:09.834126', NULL, NULL, NULL, NULL),
  (@gid, 5, 3, 5, 8, 'Cannon', 'Black', 31, '2026-11-11 14:15:09.930402', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 2, 4, 'Pawn', 'Red', 32, '2026-11-11 14:15:17.152478', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 2, 0, 'Rook', 'Black', 33, '2026-11-11 14:15:17.233023', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 5, 'Advisor', 'Red', 34, '2026-11-11 14:15:41.161587', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 9, 8, 'Cannon', 'Black', 35, '2026-11-11 14:15:41.244651', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 36, '2026-11-11 14:15:58.559765', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 1, 3, 'Rook', 'Black', 37, '2026-11-11 14:15:58.660153', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 7, 4, 'Elephant', 'Red', 38, '2026-11-11 14:16:26.289911', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 2, 4, 'Rook', 'Black', 39, '2026-11-11 14:16:26.398721', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 40, '2026-11-11 14:16:40.424339', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 5, 4, 'Rook', 'Black', 41, '2026-11-11 14:16:40.543971', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 0, 1, 'Rook', 'Red', 42, '2026-11-11 14:16:45.002845', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 7, 4, 'Rook', 'Black', 43, '2026-11-11 14:16:45.109659', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 8, 5, 'King', 'Red', 44, '2026-11-11 14:16:55.352505', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 7, 5, 'Rook', 'Black', 45, '2026-11-11 14:16:55.425270', NULL, NULL, NULL, NULL);

-- Ván RoomCode=WFCQMNAS (Local Id=207, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 14:17:09.559170', '2026-11-11 14:20:58.990032', NULL, '', @uid, NULL, 'WFCQMNAS', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 14:20:59.012680', 3, 21, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'WFCQMNAS' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 7, 7, 8, 'Cannon', 'Red', 0, '2026-11-11 14:17:19.783962', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 9, 1, 'Cannon', 'Black', 1, '2026-11-11 14:17:19.919666', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 2, '2026-11-11 14:17:22.537356', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 14:17:22.612148', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 4, '2026-11-11 14:17:43.367977', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 6, 4, 'Cannon', 'Black', 5, '2026-11-11 14:17:43.481863', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 3, 4, 'Cannon', 'Red', 6, '2026-11-11 14:17:46.833646', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 7, '2026-11-11 14:17:46.954580', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 8, '2026-11-11 14:18:10.233453', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 6, 5, 'Cannon', 'Black', 9, '2026-11-11 14:18:10.313905', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 2, 1, 'Rook', 'Red', 10, '2026-11-11 14:18:33.943852', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 3, 4, 'Horse', 'Black', 11, '2026-11-11 14:18:33.998289', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Rook', 'Red', 12, '2026-11-11 14:18:44.056303', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 4, 'Elephant', 'Black', 13, '2026-11-11 14:18:44.087519', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 5, 6, 'Pawn', 'Red', 14, '2026-11-11 14:19:09.999850', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 15, '2026-11-11 14:19:10.097018', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 5, 7, 'Horse', 'Red', 16, '2026-11-11 14:19:29.648857', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 1, 5, 'Horse', 'Black', 17, '2026-11-11 14:19:29.763277', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 18, '2026-11-11 14:19:39.890651', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 0, 7, 'Rook', 'Black', 19, '2026-11-11 14:19:40.026944', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 3, 8, 'Horse', 'Red', 20, '2026-11-11 14:19:42.752999', NULL, NULL, NULL, NULL),
  (@gid, 6, 5, 6, 8, 'Cannon', 'Black', 21, '2026-11-11 14:19:42.831744', NULL, NULL, NULL, NULL),
  (@gid, 7, 8, 7, 4, 'Cannon', 'Red', 22, '2026-11-11 14:19:55.673982', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 3, 8, 'Cannon', 'Black', 23, '2026-11-11 14:19:55.738565', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 2, 4, 'Cannon', 'Red', 24, '2026-11-11 14:20:00.737859', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 9, 8, 'Cannon', 'Black', 25, '2026-11-11 14:20:00.810849', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 2, 1, 'Cannon', 'Red', 26, '2026-11-11 14:20:15.568879', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 5, 7, 'Rook', 'Black', 27, '2026-11-11 14:20:15.677402', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 0, 1, 'Cannon', 'Red', 28, '2026-11-11 14:20:21.592192', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 1, 'Rook', 'Black', 29, '2026-11-11 14:20:21.687593', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 0, 3, 'Cannon', 'Red', 30, '2026-11-11 14:20:23.217368', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 0, 3, 'Horse', 'Black', 31, '2026-11-11 14:20:23.276414', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 4, 6, 'Pawn', 'Red', 32, '2026-11-11 14:20:42.505813', NULL, NULL, NULL, NULL),
  (@gid, 1, 1, 1, 5, 'Rook', 'Black', 33, '2026-11-11 14:20:42.576439', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 3, 6, 'Pawn', 'Red', 34, '2026-11-11 14:20:43.970593', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 9, 5, 'Rook', 'Black', 35, '2026-11-11 14:20:44.036765', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 36, '2026-11-11 14:20:50.336774', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 9, 4, 'Rook', 'Black', 37, '2026-11-11 14:20:50.405876', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 8, 3, 'King', 'Red', 38, '2026-11-11 14:20:58.946103', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 5, 3, 'Rook', 'Black', 39, '2026-11-11 14:20:58.983335', NULL, NULL, NULL, NULL);

-- Ván RoomCode=MNWGD7CS (Local Id=208, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 14:21:09.480271', '2026-11-11 14:28:48.044684', NULL, '', @uid, NULL, 'MNWGD7CS', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 14:28:48.059280', 3, 24, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'MNWGD7CS' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 7, 5, 7, 'Cannon', 'Red', 0, '2026-11-11 14:21:47.687853', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 9, 7, 'Cannon', 'Black', 1, '2026-11-11 14:21:47.898084', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 9, 7, 'Rook', 'Red', 2, '2026-11-11 14:21:49.461047', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 9, 1, 'Cannon', 'Black', 3, '2026-11-11 14:21:49.601265', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 9, 1, 'Rook', 'Red', 4, '2026-11-11 14:21:53.028399', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 1, 0, 'Rook', 'Black', 5, '2026-11-11 14:21:53.096807', NULL, NULL, NULL, NULL),
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 6, '2026-11-11 14:22:12.937472', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 2, 'Horse', 'Black', 7, '2026-11-11 14:22:13.044198', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 8, '2026-11-11 14:22:27.730095', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 8, 'Rook', 'Black', 9, '2026-11-11 14:22:27.849408', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 0, 2, 'Cannon', 'Red', 10, '2026-11-11 14:22:32.505116', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 1, 4, 'Advisor', 'Black', 11, '2026-11-11 14:22:32.527462', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 0, 5, 'Cannon', 'Red', 12, '2026-11-11 14:22:38.890481', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 0, 5, 'Advisor', 'Black', 13, '2026-11-11 14:22:38.959826', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 5, 8, 'Cannon', 'Red', 14, '2026-11-11 14:22:50.090652', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 8, 'Elephant', 'Black', 15, '2026-11-11 14:22:50.152384', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 3, 1, 'Rook', 'Red', 16, '2026-11-11 14:23:27.369270', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 4, 8, 'Pawn', 'Black', 17, '2026-11-11 14:23:27.486345', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 5, 4, 'Cannon', 'Red', 18, '2026-11-11 14:23:53.435602', NULL, NULL, NULL, NULL),
  (@gid, 0, 5, 1, 4, 'Advisor', 'Black', 19, '2026-11-11 14:23:53.472646', NULL, NULL, NULL, NULL),
  (@gid, 3, 1, 1, 1, 'Rook', 'Red', 20, '2026-11-11 14:24:26.937415', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 4, 4, 'Pawn', 'Black', 21, '2026-11-11 14:24:27.051637', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 2, 7, 'Rook', 'Red', 22, '2026-11-11 14:25:49.771915', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 5, 4, 'Pawn', 'Black', 23, '2026-11-11 14:25:49.864522', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 5, 4, 'Pawn', 'Red', 24, '2026-11-11 14:25:51.892915', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 4, 3, 'Horse', 'Black', 25, '2026-11-11 14:25:51.976104', NULL, NULL, NULL, NULL),
  (@gid, 1, 1, 0, 1, 'Rook', 'Red', 26, '2026-11-11 14:26:16.403023', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 0, 3, 'Advisor', 'Black', 27, '2026-11-11 14:26:16.431673', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 4, 4, 'Pawn', 'Red', 28, '2026-11-11 14:26:56.922124', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 1, 5, 'Horse', 'Black', 29, '2026-11-11 14:26:57.046344', NULL, NULL, NULL, NULL),
  (@gid, 4, 4, 3, 4, 'Pawn', 'Red', 30, '2026-11-11 14:27:51.397578', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 2, 7, 'Horse', 'Black', 31, '2026-11-11 14:27:51.503070', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 4, 'Elephant', 'Red', 32, '2026-11-11 14:28:03.675684', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 1, 5, 'Horse', 'Black', 33, '2026-11-11 14:28:03.781963', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 2, 4, 'Pawn', 'Red', 34, '2026-11-11 14:28:14.381150', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 3, 4, 'Horse', 'Black', 35, '2026-11-11 14:28:14.452207', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 2, 5, 'Pawn', 'Red', 36, '2026-11-11 14:28:21.458663', NULL, NULL, NULL, NULL),
  (@gid, 3, 4, 1, 3, 'Horse', 'Black', 37, '2026-11-11 14:28:21.544360', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 0, 3, 'Rook', 'Red', 38, '2026-11-11 14:28:25.434887', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 0, 3, 'King', 'Black', 39, '2026-11-11 14:28:25.459248', NULL, NULL, NULL, NULL),
  (@gid, 2, 5, 2, 4, 'Pawn', 'Red', 40, '2026-11-11 14:28:32.242552', NULL, NULL, NULL, NULL),
  (@gid, 4, 3, 2, 4, 'Horse', 'Black', 41, '2026-11-11 14:28:32.282001', NULL, NULL, NULL, NULL);

-- Ván RoomCode=YXZGUWM2 (Local Id=209, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 14:28:49.334054', '2026-11-11 14:37:09.633407', NULL, '', @uid, NULL, 'YXZGUWM2', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 14:40:18.549299', 3, 15, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'YXZGUWM2' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 2, 'Cannon', 'Red', 0, '2026-11-11 14:29:09.994392', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 2, 6, 'Cannon', 'Black', 1, '2026-11-11 14:29:10.128423', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 3, 2, 'Cannon', 'Red', 2, '2026-11-11 14:29:16.099093', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 4, 'Cannon', 'Black', 3, '2026-11-11 14:29:16.245186', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 6, 'Cannon', 'Red', 4, '2026-11-11 14:29:26.499479', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 8, 'Horse', 'Black', 5, '2026-11-11 14:29:26.586309', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 6, '2026-11-11 14:30:07.558123', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 6, 6, 'Cannon', 'Black', 7, '2026-11-11 14:30:07.681275', NULL, NULL, NULL, NULL),
  (@gid, 9, 6, 7, 4, 'Elephant', 'Red', 8, '2026-11-11 14:30:22.458735', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 6, 4, 'Cannon', 'Black', 9, '2026-11-11 14:30:22.572136', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 10, '2026-11-11 14:30:39.226635', NULL, NULL, NULL, NULL),
  (@gid, 2, 8, 3, 6, 'Horse', 'Black', 11, '2026-11-11 14:30:39.387935', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 3, 6, 'Cannon', 'Red', 12, '2026-11-11 14:30:53.900133', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 6, 3, 'Cannon', 'Black', 13, '2026-11-11 14:30:54.034344', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 4, 6, 'Cannon', 'Red', 14, '2026-11-11 14:31:34.196709', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 4, 'Elephant', 'Black', 15, '2026-11-11 14:31:34.296538', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 5, 6, 'Cannon', 'Red', 16, '2026-11-11 14:32:29.867958', NULL, NULL, NULL, NULL),
  (@gid, 0, 8, 1, 8, 'Rook', 'Black', 17, '2026-11-11 14:32:29.991170', NULL, NULL, NULL, NULL),
  (@gid, 5, 6, 5, 8, 'Cannon', 'Red', 18, '2026-11-11 14:32:34.348364', NULL, NULL, NULL, NULL),
  (@gid, 1, 8, 1, 0, 'Rook', 'Black', 19, '2026-11-11 14:32:34.473276', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 5, 6, 'Elephant', 'Red', 20, '2026-11-11 14:35:21.596973', NULL, NULL, NULL, NULL),
  (@gid, 1, 0, 1, 5, 'Rook', 'Black', 21, '2026-11-11 14:35:21.706524', NULL, NULL, NULL, NULL),
  (@gid, 5, 8, 5, 7, 'Cannon', 'Red', 22, '2026-11-11 14:36:24.485272', NULL, NULL, NULL, NULL),
  (@gid, 1, 5, 1, 7, 'Rook', 'Black', 23, '2026-11-11 14:36:24.609576', NULL, NULL, NULL, NULL),
  (@gid, 5, 7, 5, 8, 'Cannon', 'Red', 24, '2026-11-11 14:37:09.516016', NULL, NULL, NULL, NULL),
  (@gid, 3, 8, 4, 8, 'Pawn', 'Black', 25, '2026-11-11 14:37:09.625443', NULL, NULL, NULL, NULL);

-- Ván RoomCode=HJT4Z4VJ (Local Id=210, BlackWins)
INSERT IGNORE INTO `games` (`Status`, `CurrentTurn`, `CreatedAt`, `LastMoveAt`, `BlackPlayerId`, `GameType`, `RedPlayerId`, `Winner`, `RoomCode`, `InitialFen`, `AiDepth`, `GameMode`, `OpeningId`, `IsRated`, `ResultsProcessedAt`, `TimeControlMinutes`, `Version`, `AiColor`) VALUES ('BlackWins', 'Red', '2026-11-11 14:40:20.981795', '2026-11-11 14:57:04.963059', NULL, '', @uid, NULL, 'HJT4Z4VJ', NULL, NULL, 'PvE', NULL, 0, '2026-11-11 14:57:04.972703', 3, 40, 'Black');
SET @gid = (SELECT `Id` FROM `games` WHERE `RoomCode` = 'HJT4Z4VJ' LIMIT 1);
INSERT IGNORE INTO `moves` (`GameId`, `FromX`, `FromY`, `ToX`, `ToY`, `PieceType`, `PieceColor`, `MoveOrder`, `MovedAt`, `BestAlternative`, `EvalScore`, `Judgment`, `WinRate`) VALUES
  (@gid, 7, 1, 7, 4, 'Cannon', 'Red', 0, '2026-11-11 14:40:27.084615', NULL, NULL, NULL, NULL),
  (@gid, 0, 7, 2, 6, 'Horse', 'Black', 1, '2026-11-11 14:40:27.217926', NULL, NULL, NULL, NULL),
  (@gid, 7, 7, 7, 6, 'Cannon', 'Red', 2, '2026-11-11 14:41:02.517283', NULL, NULL, NULL, NULL),
  (@gid, 2, 1, 2, 2, 'Cannon', 'Black', 3, '2026-11-11 14:41:02.636405', NULL, NULL, NULL, NULL),
  (@gid, 9, 0, 8, 0, 'Rook', 'Red', 4, '2026-11-11 14:41:49.590844', NULL, NULL, NULL, NULL),
  (@gid, 2, 7, 6, 7, 'Cannon', 'Black', 5, '2026-11-11 14:41:49.786066', NULL, NULL, NULL, NULL),
  (@gid, 6, 8, 5, 8, 'Pawn', 'Red', 6, '2026-11-11 14:42:30.797712', NULL, NULL, NULL, NULL),
  (@gid, 6, 7, 6, 4, 'Cannon', 'Black', 7, '2026-11-11 14:42:30.930760', NULL, NULL, NULL, NULL),
  (@gid, 9, 5, 8, 4, 'Advisor', 'Red', 8, '2026-11-11 14:42:47.580747', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 6, 2, 'Cannon', 'Black', 9, '2026-11-11 14:42:47.659580', NULL, NULL, NULL, NULL),
  (@gid, 8, 0, 8, 2, 'Rook', 'Red', 10, '2026-11-11 14:42:50.390601', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 6, 6, 'Cannon', 'Black', 11, '2026-11-11 14:42:50.446224', NULL, NULL, NULL, NULL),
  (@gid, 8, 2, 3, 2, 'Rook', 'Red', 12, '2026-11-11 14:42:52.733950', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 2, 0, 'Horse', 'Black', 13, '2026-11-11 14:42:52.818789', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 3, 1, 'Rook', 'Red', 14, '2026-11-11 14:43:47.757485', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 1, 2, 'Horse', 'Black', 15, '2026-11-11 14:43:47.835995', NULL, NULL, NULL, NULL),
  (@gid, 3, 1, 4, 1, 'Rook', 'Red', 16, '2026-11-11 14:44:53.598542', NULL, NULL, NULL, NULL),
  (@gid, 1, 2, 3, 3, 'Horse', 'Black', 17, '2026-11-11 14:44:53.684607', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 4, 3, 'Rook', 'Red', 18, '2026-11-11 14:45:16.191801', NULL, NULL, NULL, NULL),
  (@gid, 2, 6, 1, 4, 'Horse', 'Black', 19, '2026-11-11 14:45:16.342681', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 3, 6, 'Cannon', 'Red', 20, '2026-11-11 14:45:37.535232', NULL, NULL, NULL, NULL),
  (@gid, 0, 0, 0, 1, 'Rook', 'Black', 21, '2026-11-11 14:45:37.669437', NULL, NULL, NULL, NULL),
  (@gid, 9, 1, 7, 2, 'Horse', 'Red', 22, '2026-11-11 14:46:11.759654', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 6, 2, 'Cannon', 'Black', 23, '2026-11-11 14:46:11.872863', NULL, NULL, NULL, NULL),
  (@gid, 7, 2, 6, 4, 'Horse', 'Red', 24, '2026-11-11 14:46:28.344127', NULL, NULL, NULL, NULL),
  (@gid, 3, 3, 4, 1, 'Horse', 'Black', 25, '2026-11-11 14:46:28.612220', NULL, NULL, NULL, NULL),
  (@gid, 3, 6, 3, 0, 'Cannon', 'Red', 26, '2026-11-11 14:46:38.390644', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 2, 2, 'Horse', 'Black', 27, '2026-11-11 14:46:38.687053', NULL, NULL, NULL, NULL),
  (@gid, 4, 3, 4, 2, 'Rook', 'Red', 28, '2026-11-11 14:47:14.055649', NULL, NULL, NULL, NULL),
  (@gid, 6, 6, 6, 5, 'Cannon', 'Black', 29, '2026-11-11 14:47:14.480090', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 2, 2, 'Rook', 'Red', 30, '2026-11-11 14:47:24.696131', NULL, NULL, NULL, NULL),
  (@gid, 4, 1, 2, 2, 'Horse', 'Black', 31, '2026-11-11 14:47:24.821879', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 3, 2, 'Cannon', 'Red', 32, '2026-11-11 14:48:15.920882', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 4, 2, 'Cannon', 'Black', 33, '2026-11-11 14:48:16.251945', NULL, NULL, NULL, NULL),
  (@gid, 6, 0, 5, 0, 'Pawn', 'Red', 34, '2026-11-11 14:49:01.760301', NULL, NULL, NULL, NULL),
  (@gid, 6, 5, 2, 5, 'Cannon', 'Black', 35, '2026-11-11 14:49:02.028408', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 4, 3, 'Horse', 'Red', 36, '2026-11-11 14:49:26.904307', NULL, NULL, NULL, NULL),
  (@gid, 0, 3, 1, 4, 'Advisor', 'Black', 37, '2026-11-11 14:49:26.983019', NULL, NULL, NULL, NULL),
  (@gid, 5, 0, 4, 0, 'Pawn', 'Red', 38, '2026-11-11 14:50:24.328689', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 39, '2026-11-11 14:50:24.514328', NULL, NULL, NULL, NULL),
  (@gid, 4, 0, 3, 0, 'Pawn', 'Red', 40, '2026-11-11 14:51:02.456164', NULL, NULL, NULL, NULL),
  (@gid, 2, 2, 3, 0, 'Horse', 'Black', 41, '2026-11-11 14:51:02.568641', NULL, NULL, NULL, NULL),
  (@gid, 4, 3, 5, 5, 'Horse', 'Red', 42, '2026-11-11 14:51:28.592212', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 4, 5, 'Cannon', 'Black', 43, '2026-11-11 14:51:28.750066', NULL, NULL, NULL, NULL),
  (@gid, 5, 5, 6, 7, 'Horse', 'Red', 44, '2026-11-11 14:51:47.288038', NULL, NULL, NULL, NULL),
  (@gid, 0, 1, 0, 2, 'Rook', 'Black', 45, '2026-11-11 14:51:47.467721', NULL, NULL, NULL, NULL),
  (@gid, 6, 7, 4, 6, 'Horse', 'Red', 46, '2026-11-11 14:52:26.721959', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 3, 2, 'Rook', 'Black', 47, '2026-11-11 14:52:26.811562', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 2, 5, 'Horse', 'Red', 48, '2026-11-11 14:52:35.523204', NULL, NULL, NULL, NULL),
  (@gid, 0, 4, 0, 3, 'King', 'Black', 49, '2026-11-11 14:52:35.560427', NULL, NULL, NULL, NULL),
  (@gid, 9, 8, 8, 8, 'Rook', 'Red', 50, '2026-11-11 14:53:27.570058', NULL, NULL, NULL, NULL),
  (@gid, 1, 4, 2, 5, 'Advisor', 'Black', 51, '2026-11-11 14:53:27.697526', NULL, NULL, NULL, NULL),
  (@gid, 9, 7, 7, 6, 'Horse', 'Red', 52, '2026-11-11 14:53:34.417861', NULL, NULL, NULL, NULL),
  (@gid, 4, 5, 4, 6, 'Cannon', 'Black', 53, '2026-11-11 14:53:34.502118', NULL, NULL, NULL, NULL),
  (@gid, 7, 6, 6, 4, 'Horse', 'Red', 54, '2026-11-11 14:53:41.818892', NULL, NULL, NULL, NULL),
  (@gid, 3, 2, 6, 2, 'Rook', 'Black', 55, '2026-11-11 14:53:41.944966', NULL, NULL, NULL, NULL),
  (@gid, 6, 4, 4, 3, 'Horse', 'Red', 56, '2026-11-11 14:54:06.658344', NULL, NULL, NULL, NULL),
  (@gid, 6, 2, 4, 2, 'Rook', 'Black', 57, '2026-11-11 14:54:06.729849', NULL, NULL, NULL, NULL),
  (@gid, 4, 3, 2, 4, 'Horse', 'Red', 58, '2026-11-11 14:54:37.362180', NULL, NULL, NULL, NULL),
  (@gid, 0, 6, 2, 4, 'Elephant', 'Black', 59, '2026-11-11 14:54:37.394242', NULL, NULL, NULL, NULL),
  (@gid, 7, 4, 2, 4, 'Cannon', 'Red', 60, '2026-11-11 14:54:55.414116', NULL, NULL, NULL, NULL),
  (@gid, 2, 0, 0, 2, 'Elephant', 'Black', 61, '2026-11-11 14:54:55.479919', NULL, NULL, NULL, NULL),
  (@gid, 2, 4, 2, 0, 'Cannon', 'Red', 62, '2026-11-11 14:55:22.161748', NULL, NULL, NULL, NULL),
  (@gid, 4, 6, 4, 4, 'Cannon', 'Black', 63, '2026-11-11 14:55:22.223857', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 9, 5, 'Advisor', 'Red', 64, '2026-11-11 14:55:34.265725', NULL, NULL, NULL, NULL),
  (@gid, 0, 2, 2, 0, 'Elephant', 'Black', 65, '2026-11-11 14:55:34.349593', NULL, NULL, NULL, NULL),
  (@gid, 8, 8, 8, 5, 'Rook', 'Red', 66, '2026-11-11 14:56:20.625707', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 9, 2, 'Rook', 'Black', 67, '2026-11-11 14:56:20.680806', NULL, NULL, NULL, NULL),
  (@gid, 8, 5, 2, 5, 'Rook', 'Red', 68, '2026-11-11 14:56:23.706542', NULL, NULL, NULL, NULL),
  (@gid, 3, 0, 4, 2, 'Horse', 'Black', 69, '2026-11-11 14:56:23.776271', NULL, NULL, NULL, NULL),
  (@gid, 2, 5, 2, 0, 'Rook', 'Red', 70, '2026-11-11 14:56:28.202302', NULL, NULL, NULL, NULL),
  (@gid, 9, 2, 9, 3, 'Rook', 'Black', 71, '2026-11-11 14:56:28.249045', NULL, NULL, NULL, NULL),
  (@gid, 9, 4, 8, 4, 'King', 'Red', 72, '2026-11-11 14:56:32.338352', NULL, NULL, NULL, NULL),
  (@gid, 4, 2, 5, 4, 'Horse', 'Black', 73, '2026-11-11 14:56:32.425593', NULL, NULL, NULL, NULL),
  (@gid, 8, 4, 8, 5, 'King', 'Red', 74, '2026-11-11 14:56:54.139329', NULL, NULL, NULL, NULL),
  (@gid, 5, 4, 6, 6, 'Horse', 'Black', 75, '2026-11-11 14:56:54.199980', NULL, NULL, NULL, NULL),
  (@gid, 8, 5, 7, 5, 'King', 'Red', 76, '2026-11-11 14:57:04.907452', NULL, NULL, NULL, NULL),
  (@gid, 9, 3, 9, 5, 'Rook', 'Black', 77, '2026-11-11 14:57:04.955211', NULL, NULL, NULL, NULL);

-- BƯỚC 4: Đồng bộ 19 tiến độ bài học Khóa học cho @uid
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 10, 1, 100, 2, 0, '2026-11-11 05:35:40.395270', '2026-11-11 09:08:12.458560', '2026-11-11 09:08:13.042003');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 1, 1, 100, 1, 0, '2026-11-11 05:42:07.570006', '2026-11-11 09:07:10.742748', '2026-11-11 09:07:10.742676');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 2, 1, 100, 2, 0, '2026-11-11 09:07:22.483470', '2026-11-11 09:07:22.484774', '2026-11-11 09:07:23.207283');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 3, 1, 100, 4, 0, '2026-11-11 09:07:30.289892', '2026-11-11 09:07:33.721343', '2026-11-11 09:07:33.721342');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 4, 1, 100, 2, 0, '2026-11-11 09:07:41.183733', '2026-11-11 09:07:41.183784', '2026-11-11 09:07:41.889047');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 5, 1, 100, 1, 0, '2026-11-11 09:07:50.449198', '2026-11-11 09:07:50.449265', '2026-11-11 09:07:50.449264');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 11, 1, 100, 2, 0, '2026-11-11 09:08:17.065485', '2026-11-11 09:08:17.065557', '2026-11-11 09:08:17.649134');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 12, 1, 100, 2, 0, '2026-11-11 09:08:31.145933', '2026-11-11 09:08:31.146025', '2026-11-11 09:08:31.674540');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 13, 1, 100, 2, 0, '2026-11-11 09:08:37.305798', '2026-11-11 09:08:37.305874', '2026-11-11 09:08:37.769507');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 14, 1, 100, 2, 0, '2026-11-11 09:08:50.592796', '2026-11-11 09:08:50.592860', '2026-11-11 09:08:51.463772');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 15, 1, 100, 1, 0, '2026-11-11 09:09:11.919896', '2026-11-11 09:09:11.919969', '2026-11-11 09:09:11.919968');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 16, 1, 100, 3, 0, '2026-11-11 09:10:03.105601', '2026-11-11 09:10:03.105653', '2026-11-11 09:10:12.322205');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 17, 1, 100, 5, 0, '2026-11-11 09:14:43.947082', '2026-11-11 09:14:43.947153', '2026-11-11 09:15:01.946397');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 18, 1, 100, 7, 0, '2026-11-11 09:16:51.090546', '2026-11-11 09:16:51.090607', '2026-11-11 09:20:44.147106');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 19, 1, 100, 1, 0, '2026-11-11 09:22:30.942076', '2026-11-11 09:22:30.942136', '2026-11-11 09:22:30.942136');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 6, 1, 100, 2, 0, '2026-11-11 09:22:45.142078', '2026-11-11 09:22:45.142194', '2026-11-11 09:22:45.693181');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 7, 1, 100, 4, 0, '2026-11-11 09:22:55.162863', '2026-11-11 09:23:11.197334', '2026-11-11 09:23:11.627712');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 8, 1, 100, 2, 0, '2026-11-11 09:23:31.378852', '2026-11-11 09:23:31.378919', '2026-11-11 09:23:32.290807');
INSERT IGNORE INTO `usercourseprogresses` (`UserId`, `LessonId`, `IsCompleted`, `BestQuizScore`, `QuizAttempts`, `LastPosition`, `StartedAt`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 9, 1, 100, 2, 0, '2026-11-11 09:23:38.252171', '2026-11-11 09:23:38.252350', '2026-11-11 09:23:39.028879');

-- BƯỚC 5: Đồng bộ 20 tiến độ Cờ thế đã giải cho @uid
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:98', 1, '2026-11-11 08:50:50.262779', '2026-11-11 08:50:50.262816');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:376', 0, '2026-11-11 08:51:17.621073', '2026-11-11 08:51:17.621073');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:485', 5, '2026-11-11 08:52:37.525804', '2026-11-11 08:52:37.525804');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:2966', 0, '2026-11-11 08:53:01.069937', '2026-11-11 08:53:01.069937');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:1095', 0, '2026-11-11 08:53:05.372153', '2026-11-11 08:53:05.372154');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:2516', 0, '2026-11-11 08:53:17.268392', '2026-11-11 08:53:17.268392');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:1441', 4, '2026-11-11 08:54:48.868839', '2026-11-11 08:54:48.868840');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:374', 2, '2026-11-11 08:55:20.380317', '2026-11-11 08:55:20.380318');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'basic:23', 0, '2026-11-11 08:55:32.420301', '2026-11-11 08:55:32.420301');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:2542', 0, '2026-11-11 08:55:41.175724', '2026-11-11 08:55:41.175724');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:1582', 0, '2026-11-11 08:55:55.333790', '2026-11-11 08:55:55.333790');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:1757', 0, '2026-11-11 08:56:19.261576', '2026-11-11 08:56:19.261576');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:1397', 0, '2026-11-11 08:56:28.700681', '2026-11-11 08:56:28.700681');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:6948', 4, '2026-11-11 08:59:08.629184', '2026-11-11 08:59:08.629184');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:3863', 0, '2026-11-11 08:59:22.991461', '2026-11-11 08:59:22.991462');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:2017', 2, '2026-11-11 08:59:55.774065', '2026-11-11 08:59:55.774065');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:234', 0, '2026-11-11 09:00:45.382949', '2026-11-11 09:00:45.382950');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:3477', 0, '2026-11-11 09:03:56.119855', '2026-11-11 09:03:56.119855');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:1967', 0, '2026-11-11 09:04:37.439573', '2026-11-11 09:04:37.439573');
INSERT IGNORE INTO `puzzleprogresses` (`UserId`, `PuzzleKey`, `Attempts`, `CompletedAt`, `UpdatedAt`) VALUES (@uid, 'endgame:3013', 1, '2026-11-11 09:06:44.543207', '2026-11-11 09:06:44.543208');

-- BƯỚC 6: Tính toán lại chính xác GamesPlayed / Wins / Losses / Draws dựa trên tất cả trận hợp lệ (PvE + Online)
UPDATE `users` u
SET 
    u.`GamesPlayed` = (SELECT COUNT(*) FROM `games` g WHERE (g.`RedPlayerId` = u.`Id` OR g.`BlackPlayerId` = u.`Id`) AND g.`Status` IN ('RedWins', 'BlackWins', 'Draw')),
    u.`Wins` = (SELECT COUNT(*) FROM `games` g WHERE ((g.`RedPlayerId` = u.`Id` AND g.`Status` = 'RedWins') OR (g.`BlackPlayerId` = u.`Id` AND g.`Status` = 'BlackWins'))),
    u.`Losses` = (SELECT COUNT(*) FROM `games` g WHERE ((g.`RedPlayerId` = u.`Id` AND g.`Status` = 'BlackWins') OR (g.`BlackPlayerId` = u.`Id` AND g.`Status` = 'RedWins'))),
    u.`Draws` = (SELECT COUNT(*) FROM `games` g WHERE (g.`RedPlayerId` = u.`Id` OR g.`BlackPlayerId` = u.`Id`) AND g.`Status` = 'Draw')
WHERE u.`Id` = @uid;

SET FOREIGN_KEY_CHECKS = 1;
-- Hoàn tất đồng bộ dữ liệu cho tài khoản chuquoctrung0607@gmail.com.