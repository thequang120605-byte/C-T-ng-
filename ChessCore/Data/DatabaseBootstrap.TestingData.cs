using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ChessCore.Models;
using ChessCore.Enums;

namespace ChessCore.Data
{
    public static partial class DatabaseBootstrap
    {
        public static async Task EnsureRichTestingDataForTopThreeUsersAsync(AppDbContext dbContext, ILogger? logger = null, bool force = false)
        {
            try
            {
                var theQuang = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == 1 || u.Username == "TheQuang");
                var trongNguyen = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == 27 || u.Username == "TrongNguyen34");
                var quocTrung = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == 43 || u.Username == "QuocTrung");

                if (theQuang == null || trongNguyen == null || quocTrung == null)
                {
                    logger?.LogWarning(">>> Chưa tìm thấy đủ 3 tài khoản để nạp dữ liệu phong phú.");
                    return;
                }

                // Kiểm tra xem đã nạp dữ liệu mới nhất (TheQuang tỷ lệ thắng cao 62.9%, D1=9, QT D1=33) chưa
                int tqD1 = await dbContext.Games.CountAsync(g => (g.RedPlayerId == theQuang.Id || g.BlackPlayerId == theQuang.Id) && g.AiDepth == 1);
                int qtD1 = await dbContext.Games.CountAsync(g => (g.RedPlayerId == quocTrung.Id || g.BlackPlayerId == quocTrung.Id) && g.AiDepth == 1);

                int puzCount = await dbContext.PuzzleProgresses.CountAsync(p => p.UserId == theQuang.Id);
                int tqCourseCount = await dbContext.UserCourseProgresses.CountAsync(c => c.UserId == theQuang.Id && c.IsCompleted);
                int tnPuzCount = await dbContext.PuzzleProgresses.CountAsync(p => p.UserId == trongNguyen.Id);
                bool hasExactCampaignSync = await dbContext.PuzzleProgresses.AnyAsync(p => p.UserId == theQuang.Id && p.PuzzleKey == Campaign500Keys[0]);
                if (!force && tqD1 == 26 && qtD1 == 33 && puzCount == 324 && tnPuzCount == 246 && tqCourseCount == 19 && hasExactCampaignSync)
                {
                    logger?.LogInformation(">>> Dữ liệu 3 tài khoản đã phong phú, tỷ lệ thắng TheQuang cao, không cần nạp lại.");
                    return;
                }

                logger?.LogInformation(">>> Đang nạp lại dữ liệu phong phú: TheQuang chơi giỏi thắng cao (62.9%)...");

                var targetIds = new[] { theQuang.Id, trongNguyen.Id, quocTrung.Id };

                // 1. Dọn dẹp Games, Moves, Analyses, UserEloHistories cũ của 3 tài khoản này
                var oldGames = await dbContext.Games
                    .Where(g => (g.RedPlayerId.HasValue && targetIds.Contains(g.RedPlayerId.Value))
                             || (g.BlackPlayerId.HasValue && targetIds.Contains(g.BlackPlayerId.Value)))
                    .ToListAsync();
                var oldGameIds = oldGames.Select(g => g.Id).ToList();

                if (oldGameIds.Count > 0)
                {
                    var oldMoves = await dbContext.Moves.Where(m => oldGameIds.Contains(m.GameId)).ToListAsync();
                    dbContext.Moves.RemoveRange(oldMoves);

                    var oldAnalyses = await dbContext.GameAnalyses.Where(a => oldGameIds.Contains(a.GameId)).ToListAsync();
                    dbContext.GameAnalyses.RemoveRange(oldAnalyses);

                    dbContext.Games.RemoveRange(oldGames);
                }

                var oldEloHistories = await dbContext.UserEloHistories.Where(h => targetIds.Contains(h.UserId)).ToListAsync();
                dbContext.UserEloHistories.RemoveRange(oldEloHistories);

                var oldCourses = await dbContext.UserCourseProgresses.Where(c => targetIds.Contains(c.UserId)).ToListAsync();
                dbContext.UserCourseProgresses.RemoveRange(oldCourses);

                var oldPuz = await dbContext.PuzzleProgresses.Where(p => targetIds.Contains(p.UserId)).ToListAsync();
                dbContext.PuzzleProgresses.RemoveRange(oldPuz);

                await dbContext.SaveChangesAsync();

                // 2. Tạo các ván đấu PvE Depth 1..4:
                // TheQuang (Kỳ thủ giỏi - thắng cao): D1: 26 (21W, 4D, 1L), D2: 18 (13W, 3D, 2L), D3: 14 (5W, 3D, 6L), D4: 7 (0W, 2D, 5L) -> PvE 65 (39W, 12D, 14L)
                // TrongNguyen34 (Toàn diện, chăm chỉ): D1: 24 (19W, 2D, 3L), D2: 17 (9W, 2D, 6L), D3: 11 (1W, 2D, 8L), D4: 5 (0W, 1D, 4L) -> PvE 57 (29W, 7D, 21L)
                // QuocTrung (Mới chơi): D1: 33 (22W, 4D, 7L), D2: 13 (4W, 2D, 7L), D3: 7 (1W, 1D, 5L), D4: 4 (0W, 0D, 4L) -> PvE 57 (27W, 7D, 23L)
                var baseDate = DateTime.UtcNow.AddDays(-38);
                int gameSeedCounter = 150;

                var userPlans = new[]
                {
                    new
                    {
                        User = theQuang,
                        InitialElo = 1215,
                        TargetElo = 1285,
                        D1 = (W: 21, D: 4, L: 1),
                        D2 = (W: 13, D: 3, L: 2),
                        D3 = (W: 5, D: 3, L: 6),
                        D4 = (W: 0, D: 2, L: 5)
                    },
                    new
                    {
                        User = trongNguyen,
                        InitialElo = 1202,
                        TargetElo = 1260,
                        D1 = (W: 19, D: 2, L: 3),
                        D2 = (W: 9, D: 2, L: 6),
                        D3 = (W: 1, D: 2, L: 8),
                        D4 = (W: 0, D: 1, L: 4)
                    },
                    new
                    {
                        User = quocTrung,
                        InitialElo = 1145,
                        TargetElo = 1165,
                        D1 = (W: 22, D: 4, L: 7),
                        D2 = (W: 4, D: 2, L: 7),
                        D3 = (W: 1, D: 1, L: 5),
                        D4 = (W: 0, D: 0, L: 4)
                    }
                };

                var userGamesMap = new Dictionary<int, List<(Game game, bool isWin, bool isDraw, bool isLoss, string reason)>>();
                foreach (var p in userPlans)
                {
                    userGamesMap[p.User.Id] = new List<(Game game, bool isWin, bool isDraw, bool isLoss, string reason)>();
                }

                // Nạp PvE
                foreach (var plan in userPlans)
                {
                    int uId = plan.User.Id;
                    var depths = new[]
                    {
                        (Depth: 1, plan.D1.W, plan.D1.D, plan.D1.L),
                        (Depth: 2, plan.D2.W, plan.D2.D, plan.D2.L),
                        (Depth: 3, plan.D3.W, plan.D3.D, plan.D3.L),
                        (Depth: 4, plan.D4.W, plan.D4.D, plan.D4.L)
                    };

                    int gameIndex = 0;
                    foreach (var (depth, wCount, dCount, lCount) in depths)
                    {
                        var results = new List<string>();
                        for (int i = 0; i < wCount; i++) results.Add("W");
                        for (int i = 0; i < dCount; i++) results.Add("D");
                        for (int i = 0; i < lCount; i++) results.Add("L");

                        var rng = new Random(uId * 100 + depth * 11 + 5);
                        results = results.OrderBy(_ => rng.Next()).ToList();

                        for (int rIdx = 0; rIdx < results.Count; rIdx++)
                        {
                            string res = results[rIdx];
                            gameIndex++;
                            double dayOffset = (depth - 1) * 7.2 + (rIdx * 0.35) + (uId % 4) * 0.18;
                            DateTime gTime = baseDate.AddDays(Math.Min(dayOffset, 37.0)).AddMinutes(gameIndex * 31);

                            bool playAsRed = (rIdx % 3 != 0);
                            GameStatus gStatus = res switch
                            {
                                "W" => playAsRed ? GameStatus.RedWins : GameStatus.BlackWins,
                                "D" => GameStatus.Draw,
                                _   => playAsRed ? GameStatus.BlackWins : GameStatus.RedWins
                            };

                            var game = new Game
                            {
                                RoomCode = $"PVE-D{depth}-{uId}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                                GameMode = "PvE",
                                AiDepth = depth,
                                AiColor = playAsRed ? "Black" : "Red",
                                Status = gStatus,
                                CurrentTurn = PieceColor.Red,
                                RedPlayerId = playAsRed ? uId : null,
                                BlackPlayerId = playAsRed ? null : uId,
                                IsRated = true,
                                CreatedAt = gTime,
                                LastMoveAt = gTime.AddMinutes(12),
                                ResultsProcessedAt = gTime.AddMinutes(13),
                                TimeControlMinutes = 3,
                                Version = 16,
                                InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1"
                            };

                            string depthName = depth switch
                            {
                                1 => "Cấp 1 (Dễ)",
                                2 => "Cấp 2 (Trung bình)",
                                3 => "Cấp 3 (Khó)",
                                _ => "Cấp 4 (Siêu khó)"
                            };
                            string reason = res switch
                            {
                                "W" => $"Thắng máy AI {depthName}",
                                "D" => $"Hòa máy AI {depthName}",
                                _   => $"Thua máy AI {depthName}"
                            };

                            userGamesMap[uId].Add((game, res == "W", res == "D", res == "L", reason));
                        }
                    }
                }

                // 3. Tạo các ván PvP đối kháng Online
                // TheQuang chơi xuất sắc, áp đảo trong các trận đối kháng:
                // TheQuang vs TrongNguyen34: 4 ván (TQ thắng 3, TN thắng 1)
                // TheQuang vs QuocTrung: 4 ván (TQ thắng 3, QT thắng 1)
                // TrongNguyen34 vs QuocTrung: 5 ván (TN thắng 3, QT thắng 1, Hòa 1)
                var pvpMatches = new List<(int p1, int p2, string res1, string note)>
                {
                    (theQuang.Id, trongNguyen.Id, "W", "Đại chiến Kỳ vương TheQuang vs TrongNguyen34"),
                    (theQuang.Id, trongNguyen.Id, "W", "Trung cuộc biến hóa TheQuang vs TrongNguyen34"),
                    (trongNguyen.Id, theQuang.Id, "W", "Nỗ lực gỡ hòa TrongNguyen34 vs TheQuang"),
                    (theQuang.Id, trongNguyen.Id, "W", "Thư hùng tàn cục TheQuang vs TrongNguyen34"),

                    (theQuang.Id, quocTrung.Id, "W", "Giao hữu xếp hạng TheQuang vs QuocTrung"),
                    (theQuang.Id, quocTrung.Id, "W", "Đối công vũ bão TheQuang vs QuocTrung"),
                    (quocTrung.Id, theQuang.Id, "W", "Bứt phá bất ngờ QuocTrung vs TheQuang"),
                    (theQuang.Id, quocTrung.Id, "W", "Chiến thắng kinh nghiệm TheQuang vs QuocTrung"),

                    (trongNguyen.Id, quocTrung.Id, "W", "Khóa chặt thế trận TrongNguyen34 vs QuocTrung"),
                    (trongNguyen.Id, quocTrung.Id, "W", "Tấn công dồn dập TrongNguyen34 vs QuocTrung"),
                    (quocTrung.Id, trongNguyen.Id, "W", "Đòn phản công sắc bén QuocTrung vs TrongNguyen34"),
                    (trongNguyen.Id, quocTrung.Id, "D", "Cầm chân danh thủ TrongNguyen34 vs QuocTrung"),
                    (trongNguyen.Id, quocTrung.Id, "W", "Sát cục tàn cuộc TrongNguyen34 vs QuocTrung")
                };

                int pvpIdx = 0;
                foreach (var match in pvpMatches)
                {
                    pvpIdx++;
                    DateTime gTime = baseDate.AddDays(8 + pvpIdx * 1.8);
                    bool p1Wins = match.res1 == "W";
                    bool isDraw = match.res1 == "D";
                    GameStatus pvpStatus = isDraw ? GameStatus.Draw : (p1Wins ? GameStatus.RedWins : GameStatus.BlackWins);

                    var pvpGame = new Game
                    {
                        RoomCode = $"PVP-{match.p1}-{match.p2}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                        GameMode = "OnlinePublic",
                        Status = pvpStatus,
                        CurrentTurn = PieceColor.Red,
                        RedPlayerId = match.p1,
                        BlackPlayerId = match.p2,
                        IsRated = true,
                        CreatedAt = gTime,
                        LastMoveAt = gTime.AddMinutes(15),
                        ResultsProcessedAt = gTime.AddMinutes(16),
                        TimeControlMinutes = 10,
                        Version = 24,
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1"
                    };

                    string p1Reason = isDraw ? "Hòa trận PvP Online" : (p1Wins ? "Thắng trận PvP Online" : "Thua trận PvP Online");
                    string p2Reason = isDraw ? "Hòa trận PvP Online" : (!p1Wins ? "Thắng trận PvP Online" : "Thua trận PvP Online");

                    userGamesMap[match.p1].Add((pvpGame, p1Wins, isDraw, !p1Wins && !isDraw, p1Reason));
                    userGamesMap[match.p2].Add((pvpGame, !p1Wins && !isDraw, isDraw, p1Wins, p2Reason));
                }

                // Thêm các ván PvP với các kỳ thủ khác trên cộng đồng:
                // TheQuang: 6 ván (5 thắng, 1 hòa, 0 thua) -> PvP tổng 14 ván (11W, 1D, 2L)
                // TrongNguyen34: 7 ván (4 thắng, 2 hòa, 1 thua) -> PvP tổng 16 ván (8W, 3D, 5L)
                // QuocTrung: 3 ván (0 thắng, 0 hòa, 3 thua) -> PvP tổng 12 ván (2W, 1D, 9L)
                var soloPvpExtra = new[]
                {
                    // TheQuang: thắng áp đảo đối thủ bên ngoài
                    (theQuang.Id, "W", 14.0, "Thắng đối thủ cộng đồng (Hà Nội)"),
                    (theQuang.Id, "W", 18.0, "Thắng đối thủ cộng đồng (Đà Nẵng)"),
                    (theQuang.Id, "D", 22.0, "Hòa kịch tính đối thủ kiện tướng"),
                    (theQuang.Id, "W", 27.0, "Thắng áp đảo cao thủ phòng mở"),
                    (theQuang.Id, "W", 31.0, "Thắng thuyết phục đối thủ kỳ hữu"),
                    (theQuang.Id, "W", 35.0, "Thắng đối thủ kỳ đài online"),

                    // TrongNguyen34
                    (trongNguyen.Id, "W", 11.0, "Thắng đối thủ phòng công khai #102"),
                    (trongNguyen.Id, "W", 15.0, "Chiến thắng đối thủ kỳ đài"),
                    (trongNguyen.Id, "D", 19.0, "Hòa thế trận tàn cuộc nghẹt thở"),
                    (trongNguyen.Id, "L", 23.0, "Thua đối thủ xếp hạng #208"),
                    (trongNguyen.Id, "W", 26.0, "Thắng chớp nhoáng đối thủ online"),
                    (trongNguyen.Id, "D", 29.0, "Hòa cờ tàn pháo tốt"),
                    (trongNguyen.Id, "W", 34.0, "Thắng đối thủ kỳ hữu CLB"),

                    // QuocTrung: thua đối thủ cao cờ bên ngoài
                    (quocTrung.Id, "L", 12.0, "Thua đối thủ xếp hạng #301"),
                    (quocTrung.Id, "L", 21.0, "Thua đối thủ kỳ cựu"),
                    (quocTrung.Id, "L", 30.0, "Thua đối thủ cao cờ phòng 104")
                };

                int extraIdx = 0;
                foreach (var extra in soloPvpExtra)
                {
                    extraIdx++;
                    DateTime gTime = baseDate.AddDays(extra.Item3);
                    bool isWin = extra.Item2 == "W";
                    bool isDraw = extra.Item2 == "D";
                    GameStatus status = isDraw ? GameStatus.Draw : (isWin ? GameStatus.RedWins : GameStatus.BlackWins);

                    var extraGame = new Game
                    {
                        RoomCode = $"PVP-NET-{extra.Item1}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                        GameMode = "OnlinePublic",
                        Status = status,
                        CurrentTurn = PieceColor.Red,
                        RedPlayerId = extra.Item1,
                        BlackPlayerId = null,
                        IsRated = true,
                        CreatedAt = gTime,
                        LastMoveAt = gTime.AddMinutes(11),
                        ResultsProcessedAt = gTime.AddMinutes(12),
                        TimeControlMinutes = 5,
                        Version = 18,
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1"
                    };

                    string reason = isDraw ? "Hòa trận PvP Online" : (isWin ? "Thắng trận PvP Online" : "Thua trận PvP Online");
                    userGamesMap[extra.Item1].Add((extraGame, isWin, isDraw, !isWin && !isDraw, reason));
                }

                // Lưu toàn bộ games vào DB
                var allNewGames = userGamesMap.Values.SelectMany(list => list.Select(x => x.game)).Distinct().ToList();
                dbContext.Games.AddRange(allNewGames);
                await dbContext.SaveChangesAsync();

                // Tạo Moves cho tất cả games mới
                var allNewMoves = new List<Move>();
                foreach (var g in allNewGames)
                {
                    PieceColor humanColor = g.RedPlayerId.HasValue ? PieceColor.Red : PieceColor.Black;
                    var moves = CreateRealisticMoves(g.Id, g.CreatedAt, humanColor, g.Status, ++gameSeedCounter);
                    allNewMoves.AddRange(moves);
                }
                dbContext.Moves.AddRange(allNewMoves);
                await dbContext.SaveChangesAsync();

                // 4. Tạo UserEloHistories mô phỏng đồ thị phong độ sinh động
                var allEloHistories = new List<UserEloHistory>();
                foreach (var plan in userPlans)
                {
                    int uId = plan.User.Id;
                    var sortedGames = userGamesMap[uId]
                        .OrderBy(x => x.game.ResultsProcessedAt ?? x.game.CreatedAt)
                        .ToList();

                    int currentElo = plan.InitialElo;
                    int targetElo = plan.TargetElo;
                    int totalGames = sortedGames.Count;

                    for (int i = 0; i < totalGames; i++)
                    {
                        var item = sortedGames[i];
                        int oldElo = currentElo;
                        int eloChange = 0;

                        if (i == totalGames - 1)
                        {
                            eloChange = targetElo - currentElo;
                            currentElo = targetElo;
                        }
                        else
                        {
                            if (item.isWin)
                            {
                                eloChange = (currentElo < targetElo) ? 12 : 7;
                            }
                            else if (item.isDraw)
                            {
                                eloChange = 0;
                            }
                            else
                            {
                                eloChange = (currentElo > targetElo) ? -11 : -6;
                            }
                            currentElo += eloChange;
                        }

                        allEloHistories.Add(new UserEloHistory
                        {
                            UserId = uId,
                            GameId = item.game.Id,
                            OldElo = oldElo,
                            NewElo = currentElo,
                            EloChange = eloChange,
                            Reason = item.reason,
                            RecordedAt = item.game.ResultsProcessedAt ?? item.game.CreatedAt.AddMinutes(15)
                        });
                    }

                    plan.User.Elo = targetElo;
                }

                dbContext.UserEloHistories.AddRange(allEloHistories);
                await dbContext.SaveChangesAsync();

                // 5. Cập nhật Khóa học (UserCourseProgresses)
                // Yêu cầu: CẢ 3 TÀI KHOẢN ĐỀU HOÀN THÀNH 19/19 BÀI HỌC (100%)
                var coursesToAdd = new List<UserCourseProgress>();
                for (int lessonId = 1; lessonId <= 19; lessonId++)
                {
                    // TrongNguyen34: 19/19 bài hoàn thành (100%)
                    coursesToAdd.Add(new UserCourseProgress
                    {
                        UserId = trongNguyen.Id,
                        LessonId = lessonId,
                        IsCompleted = true,
                        BestQuizScore = lessonId % 3 == 0 ? 95 : 100,
                        QuizAttempts = lessonId % 4 == 0 ? 2 : 1,
                        StartedAt = baseDate.AddDays(lessonId * 1.6),
                        CompletedAt = baseDate.AddDays(lessonId * 1.6).AddMinutes(45),
                        UpdatedAt = baseDate.AddDays(lessonId * 1.6).AddMinutes(45)
                    });

                    // QuocTrung: 19/19 bài hoàn thành (100%)
                    coursesToAdd.Add(new UserCourseProgress
                    {
                        UserId = quocTrung.Id,
                        LessonId = lessonId,
                        IsCompleted = true,
                        BestQuizScore = 80 + (lessonId % 4) * 5,
                        QuizAttempts = 1 + (lessonId % 3),
                        StartedAt = baseDate.AddDays(lessonId * 1.7),
                        CompletedAt = baseDate.AddDays(lessonId * 1.7).AddMinutes(50),
                        UpdatedAt = baseDate.AddDays(lessonId * 1.7).AddMinutes(50)
                    });

                    // TheQuang: 19/19 bài hoàn thành (100%)
                    coursesToAdd.Add(new UserCourseProgress
                    {
                        UserId = theQuang.Id,
                        LessonId = lessonId,
                        IsCompleted = true,
                        BestQuizScore = 85 + (lessonId % 3) * 5,
                        QuizAttempts = 1 + (lessonId % 2),
                        StartedAt = baseDate.AddDays(lessonId * 1.8),
                        CompletedAt = baseDate.AddDays(lessonId * 1.8).AddMinutes(40),
                        UpdatedAt = baseDate.AddDays(lessonId * 1.8).AddMinutes(40)
                    });
                }
                dbContext.UserCourseProgresses.AddRange(coursesToAdd);
                await dbContext.SaveChangesAsync();

                // 6. Cập nhật Cờ thế (PuzzleProgresses) đồng bộ 100% với Campaign 500 bài:
                // Sử dụng chính xác 500 key từ Campaign500Keys theo đúng thứ tự Cấp 1 -> Cấp 5
                // Đảm bảo số lượng cờ thế hiển thị trên trang Thử thách Cờ Thế (/Puzzle) và Dashboard trùng khớp 100%:
                // - TheQuang: 324/500 bài (Cấp 1: 100/100, Cấp 2: 100/100, Cấp 3: 100/100, Cấp 4: 24/100)
                // - TrongNguyen34: 246/500 bài (Cấp 1: 100/100, Cấp 2: 100/100, Cấp 3: 46/100)
                // - QuocTrung: 215/500 bài (Cấp 1: 100/100, Cấp 2: 100/100, Cấp 3: 15/100)
                var puzzlesToAdd = new List<PuzzleProgress>();

                // TheQuang: 324 bài (300+ bài thế, cao nhất, xứng đáng kỳ thủ Giỏi Top 1)
                for (int k = 0; k < 324 && k < Campaign500Keys.Length; k++)
                {
                    puzzlesToAdd.Add(new PuzzleProgress
                    {
                        UserId = theQuang.Id,
                        PuzzleKey = Campaign500Keys[k],
                        Attempts = 1 + (k % 2),
                        CompletedAt = baseDate.AddHours(k * 2.1),
                        UpdatedAt = baseDate.AddHours(k * 2.1)
                    });
                }

                // TrongNguyen34: 246 bài (200+ bài thế, xếp hạng 2)
                for (int k = 0; k < 246 && k < Campaign500Keys.Length; k++)
                {
                    puzzlesToAdd.Add(new PuzzleProgress
                    {
                        UserId = trongNguyen.Id,
                        PuzzleKey = Campaign500Keys[k],
                        Attempts = 1 + (k % 2),
                        CompletedAt = baseDate.AddHours(k * 2.7),
                        UpdatedAt = baseDate.AddHours(k * 2.7)
                    });
                }

                // QuocTrung: 215 bài (200+ bài thế, xếp hạng 3)
                for (int k = 0; k < 215 && k < Campaign500Keys.Length; k++)
                {
                    puzzlesToAdd.Add(new PuzzleProgress
                    {
                        UserId = quocTrung.Id,
                        PuzzleKey = Campaign500Keys[k],
                        Attempts = 1 + (k % 3),
                        CompletedAt = baseDate.AddHours(k * 3.1),
                        UpdatedAt = baseDate.AddHours(k * 3.1)
                    });
                }

                dbContext.PuzzleProgresses.AddRange(puzzlesToAdd);

                theQuang.PuzzleScore = 3240;
                trongNguyen.PuzzleScore = 2460;
                quocTrung.PuzzleScore = 2150;

                await dbContext.SaveChangesAsync();

                // 7. Đồng bộ GamesPlayed/Wins/Losses/Draws vào bảng Users
                await SynchronizeUserStatsAndCleanupAsync(dbContext, logger);

                logger?.LogInformation(">>> Đã nạp thành công bộ dữ liệu TheQuang chơi giỏi thắng cao!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> ERROR IN EnsureRichTestingDataForTopThreeUsersAsync: " + ex.ToString());
                logger?.LogWarning(">>> Cảnh báo khi nạp dữ liệu 3 tài khoản: {Message}", ex.Message);
            }
        }

        private static List<Move> CreateRealisticMoves(int gameId, DateTime startedAt, PieceColor userColor, GameStatus status, int seed)
        {
            var moves = new List<Move>();
            int variant = Math.Abs(seed) % 4;

            var stepDefs = variant switch
            {
                0 => new (int fx, int fy, int tx, int ty, PieceType pt, PieceColor pc)[]
                {
                    (7, 1, 7, 4, PieceType.Cannon, PieceColor.Red),   // Pháo 2 bình 5
                    (0, 1, 2, 2, PieceType.Horse, PieceColor.Black),  // Mã 8 tấn 7
                    (9, 1, 7, 2, PieceType.Horse, PieceColor.Red),    // Mã 2 tấn 3
                    (0, 0, 0, 1, PieceType.Rook, PieceColor.Black),   // Xe 9 bình 8
                    (9, 0, 9, 1, PieceType.Rook, PieceColor.Red),     // Xe 1 bình 2
                    (3, 6, 4, 6, PieceType.Pawn, PieceColor.Black),   // Binh 7 tấn 1
                    (6, 6, 5, 6, PieceType.Pawn, PieceColor.Red),     // Binh 7 tấn 1
                    (0, 7, 2, 6, PieceType.Horse, PieceColor.Black),  // Mã 2 tấn 3
                    (7, 7, 7, 8, PieceType.Cannon, PieceColor.Red),   // Pháo 8 bình 9
                    (2, 1, 2, 3, PieceType.Cannon, PieceColor.Black), // Pháo 2 bình 4
                    (9, 7, 7, 6, PieceType.Horse, PieceColor.Red),    // Mã 8 tấn 7
                    (0, 5, 1, 4, PieceType.Advisor, PieceColor.Black) // Sĩ 6 tấn 5
                },
                1 => new (int fx, int fy, int tx, int ty, PieceType pt, PieceColor pc)[]
                {
                    (7, 1, 7, 4, PieceType.Cannon, PieceColor.Red),   // Pháo 2 bình 5
                    (2, 7, 2, 4, PieceType.Cannon, PieceColor.Black), // Pháo 8 bình 5 (Thuận pháo)
                    (9, 1, 7, 2, PieceType.Horse, PieceColor.Red),    // Mã 2 tấn 3
                    (0, 1, 2, 2, PieceType.Horse, PieceColor.Black),  // Mã 8 tấn 7
                    (9, 0, 9, 1, PieceType.Rook, PieceColor.Red),     // Xe 1 bình 2
                    (0, 0, 0, 1, PieceType.Rook, PieceColor.Black),   // Xe 9 bình 8
                    (9, 1, 5, 1, PieceType.Rook, PieceColor.Red),     // Xe 2 tấn 4 (Tuần hà xa)
                    (0, 7, 2, 6, PieceType.Horse, PieceColor.Black),  // Mã 2 tấn 3
                    (6, 2, 5, 2, PieceType.Pawn, PieceColor.Red),     // Binh 3 tấn 1
                    (3, 2, 4, 2, PieceType.Pawn, PieceColor.Black),   // Binh 3 tấn 1
                    (9, 7, 7, 6, PieceType.Horse, PieceColor.Red),    // Mã 8 tấn 7
                    (0, 8, 0, 7, PieceType.Rook, PieceColor.Black)    // Xe 1 bình 2
                },
                2 => new (int fx, int fy, int tx, int ty, PieceType pt, PieceColor pc)[]
                {
                    (9, 1, 7, 2, PieceType.Horse, PieceColor.Red),    // Mã 2 tấn 3 (Khởi mã cuộc)
                    (2, 7, 2, 4, PieceType.Cannon, PieceColor.Black), // Pháo 8 bình 5
                    (6, 6, 5, 6, PieceType.Pawn, PieceColor.Red),     // Binh 7 tấn 1
                    (0, 1, 2, 2, PieceType.Horse, PieceColor.Black),  // Mã 8 tấn 7
                    (7, 1, 7, 4, PieceType.Cannon, PieceColor.Red),   // Pháo 2 bình 5
                    (0, 7, 2, 6, PieceType.Horse, PieceColor.Black),  // Mã 2 tấn 3
                    (9, 0, 9, 1, PieceType.Rook, PieceColor.Red),     // Xe 1 bình 2
                    (0, 0, 0, 1, PieceType.Rook, PieceColor.Black),   // Xe 9 bình 8
                    (9, 7, 7, 6, PieceType.Horse, PieceColor.Red),    // Mã 8 tấn 7
                    (0, 1, 4, 1, PieceType.Rook, PieceColor.Black),   // Xe 8 tấn 4 (Kỵ hà xa)
                    (6, 2, 5, 2, PieceType.Pawn, PieceColor.Red),     // Binh 3 tấn 1
                    (0, 3, 1, 4, PieceType.Advisor, PieceColor.Black) // Sĩ 4 tấn 5
                },
                _ => new (int fx, int fy, int tx, int ty, PieceType pt, PieceColor pc)[]
                {
                    (9, 2, 7, 4, PieceType.Elephant, PieceColor.Red), // Tượng 3 tấn 5 (Phi tượng cuộc)
                    (3, 6, 4, 6, PieceType.Pawn, PieceColor.Black),   // Binh 7 tấn 1
                    (9, 1, 7, 2, PieceType.Horse, PieceColor.Red),    // Mã 2 tấn 3
                    (0, 1, 2, 2, PieceType.Horse, PieceColor.Black),  // Mã 8 tấn 7
                    (7, 1, 7, 3, PieceType.Cannon, PieceColor.Red),   // Pháo 2 bình 4 (Quá cung pháo)
                    (0, 0, 0, 1, PieceType.Rook, PieceColor.Black),   // Xe 9 bình 8
                    (9, 0, 9, 1, PieceType.Rook, PieceColor.Red),     // Xe 1 bình 2
                    (0, 7, 2, 6, PieceType.Horse, PieceColor.Black),  // Mã 2 tấn 3
                    (6, 2, 5, 2, PieceType.Pawn, PieceColor.Red),     // Binh 3 tấn 1
                    (2, 7, 6, 7, PieceType.Cannon, PieceColor.Black), // Pháo 8 tiến 4 (Quá hà pháo)
                    (9, 7, 7, 6, PieceType.Horse, PieceColor.Red),    // Mã 8 tấn 7
                    (2, 1, 2, 3, PieceType.Cannon, PieceColor.Black)  // Pháo 2 bình 4
                }
            };

            for (int i = 0; i < stepDefs.Length; i++)
            {
                var s = stepDefs[i];
                string judgment = i == 0 ? "Best" : (i % 4 == 0 ? "Good" : (i % 6 == 0 ? "Inaccuracy" : "Best"));
                float winRate = 50.0f + (float)Math.Sin(i + seed) * 12.0f;
                int evalScore = (int)(Math.Sin(i + seed) * 95);

                moves.Add(new Move
                {
                    GameId = gameId,
                    FromX = s.fx,
                    FromY = s.fy,
                    ToX = s.tx,
                    ToY = s.ty,
                    PieceType = s.pt,
                    PieceColor = s.pc,
                    MoveOrder = i,
                    MovedAt = startedAt.AddSeconds(i * 25),
                    Judgment = judgment,
                    WinRate = (float)Math.Clamp(winRate, 5f, 95f),
                    EvalScore = evalScore
                });
            }

            return moves;
        }

        private static readonly string[] Campaign500Keys = new string[]
        {
            "endgame:98", "endgame:376", "endgame:485", "endgame:2966", "endgame:1095", "endgame:2516", "endgame:1441", "endgame:374",
            "basic:23", "endgame:2542", "endgame:1582", "endgame:1757", "endgame:1397", "endgame:6948", "endgame:3863", "endgame:2017",
            "endgame:234", "endgame:3477", "endgame:1967", "endgame:3013", "endgame:3110", "endgame:3090", "endgame:2205", "endgame:243",
            "endgame:4742", "endgame:3015", "endgame:3997", "endgame:4763", "endgame:3853", "endgame:308", "endgame:5044", "endgame:6630",
            "endgame:4286", "endgame:5397", "endgame:5167", "endgame:1795", "endgame:5200", "endgame:3943", "endgame:6076", "endgame:6614",
            "endgame:3683", "endgame:39", "basic:53", "advanced:324", "advanced:477", "endgame:1399", "endgame:5639", "advanced:282",
            "basic:28", "endgame:870", "advanced:194", "endgame:5299", "endgame:2632", "basic:50", "advanced:74", "advanced:288",
            "endgame:1784", "endgame:5925", "endgame:190", "advanced:366", "endgame:1010", "endgame:258", "endgame:6526", "endgame:623",
            "advanced:5", "advanced:113", "advanced:68", "endgame:1893", "endgame:6675", "endgame:2590", "advanced:368", "endgame:1193",
            "endgame:298", "endgame:4401", "endgame:5995", "advanced:6", "advanced:105", "endgame:538", "endgame:2073", "endgame:4762",
            "endgame:5815", "advanced:367", "endgame:4519", "endgame:424", "endgame:6243", "endgame:954", "endgame:6809", "advanced:325",
            "endgame:5719", "endgame:2984", "endgame:2046", "advanced:450", "endgame:4761", "endgame:425", "endgame:7124", "endgame:2911",
            "endgame:4743", "endgame:1774", "endgame:3530", "endgame:4158", "endgame:6224", "endgame:2150", "endgame:2617", "endgame:6753",
            "endgame:245", "endgame:6897", "endgame:2182", "endgame:3963", "endgame:1381", "endgame:646", "endgame:7148", "endgame:2238",
            "endgame:2963", "endgame:2269", "endgame:341", "endgame:3010", "endgame:6635", "endgame:4253", "endgame:836", "endgame:3187",
            "endgame:938", "endgame:4580", "endgame:1025", "endgame:3325", "endgame:4703", "endgame:1186", "endgame:3618", "endgame:5169",
            "endgame:1817", "endgame:4377", "endgame:6481", "endgame:1916", "endgame:5270", "endgame:2670", "endgame:6654", "endgame:3204",
            "endgame:6874", "endgame:3385", "endgame:6895", "endgame:5854", "endgame:7083", "endgame:6307", "endgame:154", "endgame:6876",
            "endgame:268", "endgame:7252", "endgame:356", "endgame:850", "endgame:660", "endgame:1325", "endgame:1444", "endgame:4925",
            "endgame:867", "endgame:6110", "endgame:3255", "endgame:3188", "endgame:5913", "endgame:6885", "endgame:5596", "endgame:1577",
            "endgame:4642", "endgame:252", "endgame:1468", "endgame:6247", "endgame:1584", "endgame:6807", "endgame:777", "endgame:2797",
            "endgame:6263", "endgame:1728", "endgame:477", "endgame:6719", "endgame:2998", "endgame:2086", "endgame:2033", "endgame:3797",
            "endgame:3164", "endgame:2196", "endgame:6692", "endgame:6833", "endgame:2371", "endgame:1294", "endgame:93", "endgame:2494",
            "endgame:3657", "endgame:183", "endgame:2705", "endgame:4770", "endgame:687", "endgame:2818", "endgame:5548", "endgame:797",
            "endgame:2922", "endgame:6059", "endgame:1031", "endgame:3506", "endgame:4980", "endgame:2547", "endgame:4060", "endgame:3226",
            "endgame:733", "endgame:1846", "endgame:752", "endgame:2306", "endgame:879", "endgame:2453", "endgame:917", "endgame:2518",
            "endgame:1305", "endgame:2700", "endgame:1472", "endgame:2904", "endgame:1661", "endgame:3167", "endgame:1859", "endgame:3668",
            "endgame:1885", "endgame:3689", "endgame:2001", "endgame:4097", "endgame:2040", "endgame:4167", "endgame:2079", "endgame:4841",
            "endgame:2190", "endgame:5351", "endgame:2201", "endgame:5700", "endgame:2213", "endgame:6412", "endgame:2314", "endgame:6865",
            "endgame:2558", "endgame:6975", "endgame:4166", "endgame:3966", "endgame:4211", "endgame:4135", "endgame:4391", "endgame:4733",
            "endgame:5340", "endgame:4738", "endgame:5735", "endgame:4765", "endgame:5810", "endgame:4994", "endgame:6289", "endgame:5101",
            "endgame:6608", "endgame:5985", "endgame:7223", "endgame:249", "endgame:563", "endgame:1085", "endgame:1144", "endgame:1212",
            "endgame:1353", "endgame:1599", "endgame:1707", "endgame:1943", "endgame:2005", "endgame:2119", "endgame:2204", "endgame:2526",
            "endgame:2612", "endgame:2652", "endgame:2858", "endgame:2934", "endgame:3314", "endgame:4358", "endgame:4722", "endgame:5122",
            "endgame:5422", "endgame:5457", "endgame:7121", "endgame:7291", "endgame:68", "endgame:275", "endgame:862", "endgame:914",
            "endgame:1094", "endgame:1128", "endgame:1204", "endgame:1221", "endgame:1750", "endgame:1872", "endgame:2497", "endgame:2698",
            "endgame:3052", "endgame:3390", "endgame:3433", "endgame:3745", "endgame:3903", "endgame:4875", "endgame:6128", "endgame:6642",
            "endgame:3546", "endgame:6694", "endgame:7171", "endgame:3171", "advanced:27", "endgame:1152", "basic:8", "advanced:103",
            "advanced:482", "endgame:884", "endgame:572", "endgame:6160", "advanced:331", "advanced:207", "basic:62", "endgame:277",
            "endgame:5252", "advanced:8", "advanced:16", "advanced:486", "endgame:5033", "endgame:3606", "endgame:522", "advanced:358",
            "endgame:147", "basic:18", "endgame:2384", "endgame:3388", "advanced:244", "advanced:101", "advanced:160", "endgame:1137",
            "endgame:7086", "endgame:6253", "advanced:365", "endgame:1660", "basic:41", "endgame:4204", "endgame:5300", "advanced:7",
            "advanced:284", "advanced:162", "endgame:173", "endgame:7313", "endgame:5284", "advanced:369", "endgame:2835", "endgame:291",
            "endgame:4593", "endgame:6637", "endgame:4146", "advanced:299", "advanced:165", "endgame:2276", "advanced:388", "endgame:3109",
            "endgame:900", "endgame:5800", "endgame:6669", "advanced:76", "advanced:285", "endgame:3300", "advanced:441", "endgame:4088",
            "endgame:1767", "endgame:6702", "endgame:357", "advanced:99", "advanced:480", "endgame:3926", "advanced:449", "endgame:5448",
            "endgame:2163", "endgame:335", "endgame:1956", "advanced:104", "advanced:161", "endgame:6074", "advanced:544", "endgame:5684",
            "endgame:2197", "endgame:2868", "endgame:2028", "advanced:305", "advanced:479", "endgame:6089", "endgame:2674", "endgame:6425",
            "endgame:3912", "advanced:326", "advanced:487", "endgame:1371", "endgame:2828", "endgame:6720", "endgame:4719", "advanced:328",
            "advanced:510", "endgame:2299", "endgame:2899", "endgame:5634", "endgame:7197", "advanced:327", "advanced:297", "endgame:3152",
            "endgame:3458", "endgame:2854", "advanced:97", "advanced:500", "endgame:3172", "endgame:4016", "advanced:50", "advanced:164",
            "endgame:4198", "endgame:4354", "advanced:53", "advanced:481", "endgame:4269", "endgame:4435", "advanced:48", "advanced:511",
            "endgame:6055", "endgame:4965", "endgame:3236", "advanced:60", "endgame:3117", "endgame:5761", "endgame:2919", "endgame:2790",
            "endgame:3137", "endgame:5962", "endgame:3860", "endgame:3429", "endgame:3593", "endgame:6357", "endgame:6431", "endgame:4648",
            "endgame:6836", "endgame:6397", "endgame:6520", "endgame:1523", "endgame:2903", "endgame:6590", "endgame:3721", "endgame:210",
            "endgame:4638", "endgame:525", "endgame:6283", "endgame:586", "endgame:6679", "endgame:637", "endgame:3073", "endgame:659",
            "endgame:6391", "endgame:1037", "endgame:1179", "endgame:1226", "endgame:1233", "endgame:1236", "endgame:1316", "endgame:1456",
            "endgame:1924", "endgame:2302", "endgame:2775", "endgame:2970", "endgame:2990", "endgame:3021", "endgame:3272", "endgame:3379",
            "endgame:3591", "endgame:3666", "endgame:3737", "endgame:4496", "endgame:4958", "endgame:5091", "endgame:5126", "endgame:5237",
            "endgame:5502", "endgame:5592", "endgame:5765", "endgame:5941", "endgame:6252", "endgame:6875", "endgame:2050", "endgame:2289",
            "endgame:2678", "endgame:3494", "endgame:5485", "endgame:5756", "endgame:6427", "endgame:6633", "endgame:6641", "endgame:7010",
            "endgame:7301", "endgame:996", "endgame:1214", "endgame:2477", "endgame:3101", "endgame:6336", "endgame:502", "endgame:1572",
            "endgame:2793", "endgame:2754", "endgame:2809", "endgame:5748",
        };
    }
}
