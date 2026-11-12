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

                if (!force && tqD1 == 9 && qtD1 == 33)
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
                // TheQuang (Kỳ thủ giỏi - thắng cao): D1: 9 (8W, 1D, 0L), D2: 18 (14W, 2D, 2L), D3: 14 (6W, 3D, 5L), D4: 7 (0W, 2D, 5L) -> PvE 48 (28W, 8D, 12L)
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
                        D1 = (W: 8, D: 1, L: 0),
                        D2 = (W: 14, D: 2, L: 2),
                        D3 = (W: 6, D: 3, L: 5),
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
                var coursesToAdd = new List<UserCourseProgress>();
                for (int lessonId = 1; lessonId <= 19; lessonId++)
                {
                    // TrongNguyen: hoàn thành 19/19 bài, điểm 95-100%
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

                    // QuocTrung: hoàn thành 17/19 bài, điểm 80-95%
                    if (lessonId <= 17)
                    {
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
                    }
                    else if (lessonId == 18)
                    {
                        coursesToAdd.Add(new UserCourseProgress
                        {
                            UserId = quocTrung.Id,
                            LessonId = lessonId,
                            IsCompleted = false,
                            BestQuizScore = 60,
                            QuizAttempts = 1,
                            StartedAt = baseDate.AddDays(31),
                            CompletedAt = null,
                            UpdatedAt = baseDate.AddDays(31)
                        });
                    }

                    // TheQuang: hoàn thành 15/19 bài, điểm 85-95%
                    if (lessonId <= 15)
                    {
                        coursesToAdd.Add(new UserCourseProgress
                        {
                            UserId = theQuang.Id,
                            LessonId = lessonId,
                            IsCompleted = true,
                            BestQuizScore = 85 + (lessonId % 3) * 5,
                            QuizAttempts = 1 + (lessonId % 2),
                            StartedAt = baseDate.AddDays(lessonId * 1.9),
                            CompletedAt = baseDate.AddDays(lessonId * 1.9).AddMinutes(40),
                            UpdatedAt = baseDate.AddDays(lessonId * 1.9).AddMinutes(40)
                        });
                    }
                    else if (lessonId == 16)
                    {
                        coursesToAdd.Add(new UserCourseProgress
                        {
                            UserId = theQuang.Id,
                            LessonId = lessonId,
                            IsCompleted = false,
                            BestQuizScore = 70,
                            QuizAttempts = 1,
                            StartedAt = baseDate.AddDays(30),
                            CompletedAt = null,
                            UpdatedAt = baseDate.AddDays(30)
                        });
                    }
                }
                dbContext.UserCourseProgresses.AddRange(coursesToAdd);
                await dbContext.SaveChangesAsync();

                // 6. Cập nhật Cờ thế (PuzzleProgresses) với số lẻ tự nhiên:
                // TheQuang: 43 thế (attempts 65)
                // TrongNguyen: 87 thế (attempts 131)
                // QuocTrung: 76 thế (attempts 152)
                var puzKeys = await dbContext.PuzzleProgresses.Select(p => p.PuzzleKey).Distinct().ToListAsync();
                if (puzKeys.Count < 100)
                {
                    for (int k = 1; k <= 100; k++)
                    {
                        string candidate = $"basic:{k}";
                        if (!puzKeys.Contains(candidate)) puzKeys.Add(candidate);
                    }
                }

                var puzzlesToAdd = new List<PuzzleProgress>();

                // TrongNguyen34: 87 bài
                for (int k = 0; k < Math.Min(87, puzKeys.Count); k++)
                {
                    puzzlesToAdd.Add(new PuzzleProgress
                    {
                        UserId = trongNguyen.Id,
                        PuzzleKey = puzKeys[k],
                        Attempts = 1 + (k % 2),
                        CompletedAt = baseDate.AddHours(k * 8.2),
                        UpdatedAt = baseDate.AddHours(k * 8.2)
                    });
                }

                // QuocTrung: 76 bài
                for (int k = 0; k < Math.Min(76, puzKeys.Count); k++)
                {
                    puzzlesToAdd.Add(new PuzzleProgress
                    {
                        UserId = quocTrung.Id,
                        PuzzleKey = puzKeys[k],
                        Attempts = 1 + (k % 3),
                        CompletedAt = baseDate.AddHours(k * 9.3),
                        UpdatedAt = baseDate.AddHours(k * 9.3)
                    });
                }

                // TheQuang: 43 bài
                for (int k = 0; k < Math.Min(43, puzKeys.Count); k++)
                {
                    puzzlesToAdd.Add(new PuzzleProgress
                    {
                        UserId = theQuang.Id,
                        PuzzleKey = puzKeys[k],
                        Attempts = 1 + (k % 2),
                        CompletedAt = baseDate.AddHours(k * 14.5),
                        UpdatedAt = baseDate.AddHours(k * 14.5)
                    });
                }

                dbContext.PuzzleProgresses.AddRange(puzzlesToAdd);

                theQuang.PuzzleScore = 735;
                trongNguyen.PuzzleScore = 1145;
                quocTrung.PuzzleScore = 985;

                await dbContext.SaveChangesAsync();

                // 7. Đồng bộ GamesPlayed/Wins/Losses/Draws vào bảng Users
                await SynchronizeUserStatsAndCleanupAsync(dbContext, logger);

                logger?.LogInformation(">>> Đã nạp thành công bộ dữ liệu TheQuang chơi giỏi thắng cao!");
            }
            catch (Exception ex)
            {
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
                    (1, 2, 4, 2, PieceType.Cannon, PieceColor.Red),   // C2.5
                    (7, 9, 6, 7, PieceType.Horse, PieceColor.Black),  // H8.7
                    (1, 0, 2, 2, PieceType.Horse, PieceColor.Red),    // H2.3
                    (8, 9, 8, 8, PieceType.Rook, PieceColor.Black),   // R9.1
                    (0, 0, 1, 0, PieceType.Rook, PieceColor.Red),     // R1.2
                    (1, 9, 2, 7, PieceType.Horse, PieceColor.Black),  // H2.3
                    (6, 3, 6, 4, PieceType.Pawn, PieceColor.Red),     // P7.1
                    (6, 6, 6, 5, PieceType.Pawn, PieceColor.Black),   // P7.1
                    (7, 0, 6, 2, PieceType.Horse, PieceColor.Red),    // H8.7
                    (2, 6, 2, 5, PieceType.Pawn, PieceColor.Black),   // P3.1
                    (1, 0, 1, 5, PieceType.Rook, PieceColor.Red),     // R2.6
                    (7, 7, 8, 7, PieceType.Cannon, PieceColor.Black), // C8.9
                    (7, 2, 3, 2, PieceType.Cannon, PieceColor.Red),   // C8.4
                    (0, 9, 1, 9, PieceType.Rook, PieceColor.Black),   // R1.2
                    (4, 3, 4, 4, PieceType.Pawn, PieceColor.Red),     // P5.1
                    (6, 7, 4, 6, PieceType.Horse, PieceColor.Black)   // H7.5
                },
                1 => new (int fx, int fy, int tx, int ty, PieceType pt, PieceColor pc)[]
                {
                    (2, 0, 4, 2, PieceType.Elephant, PieceColor.Red), // E2.4
                    (1, 7, 5, 7, PieceType.Cannon, PieceColor.Black), // C2.6
                    (1, 0, 2, 2, PieceType.Horse, PieceColor.Red),    // H2.3
                    (1, 9, 2, 7, PieceType.Horse, PieceColor.Black),  // H2.3
                    (0, 0, 1, 0, PieceType.Rook, PieceColor.Red),     // R1.2
                    (0, 9, 1, 9, PieceType.Rook, PieceColor.Black),   // R1.2
                    (7, 0, 6, 2, PieceType.Horse, PieceColor.Red),    // H8.7
                    (6, 6, 6, 5, PieceType.Pawn, PieceColor.Black),   // P7.1
                    (6, 3, 6, 4, PieceType.Pawn, PieceColor.Red),     // P7.1
                    (7, 9, 6, 7, PieceType.Horse, PieceColor.Black),  // H8.7
                    (7, 2, 8, 2, PieceType.Cannon, PieceColor.Red),   // C8.9
                    (7, 7, 7, 5, PieceType.Cannon, PieceColor.Black), // C8.2
                    (2, 3, 2, 4, PieceType.Pawn, PieceColor.Red),     // P3.1
                    (2, 6, 2, 5, PieceType.Pawn, PieceColor.Black)    // P3.1
                },
                2 => new (int fx, int fy, int tx, int ty, PieceType pt, PieceColor pc)[]
                {
                    (6, 3, 6, 4, PieceType.Pawn, PieceColor.Red),     // P7.1
                    (6, 6, 6, 5, PieceType.Pawn, PieceColor.Black),   // P7.1
                    (1, 2, 4, 2, PieceType.Cannon, PieceColor.Red),   // C2.5
                    (7, 9, 6, 7, PieceType.Horse, PieceColor.Black),  // H8.7
                    (1, 0, 2, 2, PieceType.Horse, PieceColor.Red),    // H2.3
                    (8, 9, 8, 8, PieceType.Rook, PieceColor.Black),   // R9.1
                    (0, 0, 1, 0, PieceType.Rook, PieceColor.Red),     // R1.2
                    (1, 9, 2, 7, PieceType.Horse, PieceColor.Black),  // H2.3
                    (7, 0, 6, 2, PieceType.Horse, PieceColor.Red),    // H8.7
                    (2, 6, 2, 5, PieceType.Pawn, PieceColor.Black),   // P3.1
                    (1, 0, 1, 5, PieceType.Rook, PieceColor.Red),     // R2.6
                    (7, 7, 8, 7, PieceType.Cannon, PieceColor.Black), // C8.9
                    (7, 2, 3, 2, PieceType.Cannon, PieceColor.Red),   // C8.4
                    (0, 9, 1, 9, PieceType.Rook, PieceColor.Black)    // R1.2
                },
                _ => new (int fx, int fy, int tx, int ty, PieceType pt, PieceColor pc)[]
                {
                    (1, 0, 2, 2, PieceType.Horse, PieceColor.Red),    // H2.3
                    (6, 6, 6, 5, PieceType.Pawn, PieceColor.Black),   // P7.1
                    (6, 3, 6, 4, PieceType.Pawn, PieceColor.Red),     // P7.1
                    (1, 7, 4, 7, PieceType.Cannon, PieceColor.Black), // C2.5
                    (1, 2, 3, 2, PieceType.Cannon, PieceColor.Red),   // C2.4
                    (1, 9, 2, 7, PieceType.Horse, PieceColor.Black),  // H2.3
                    (7, 0, 6, 2, PieceType.Horse, PieceColor.Red),    // H8.7
                    (0, 9, 1, 9, PieceType.Rook, PieceColor.Black),   // R1.2
                    (0, 0, 1, 0, PieceType.Rook, PieceColor.Red),     // R1.2
                    (7, 9, 6, 7, PieceType.Horse, PieceColor.Black),  // H8.7
                    (1, 0, 1, 5, PieceType.Rook, PieceColor.Red),     // R2.6
                    (2, 6, 2, 5, PieceType.Pawn, PieceColor.Black),   // P3.1
                    (7, 2, 8, 2, PieceType.Cannon, PieceColor.Red),   // C8.9
                    (7, 7, 8, 7, PieceType.Cannon, PieceColor.Black)  // C8.9
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
    }
}
