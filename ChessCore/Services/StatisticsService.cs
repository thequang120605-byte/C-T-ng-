using ChessCore.Data;
using ChessCore.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
namespace ChessCore.Services;

public sealed class ModeStatistics
{
    public string Mode { get; set; } = "";
    public string Label => GameModes.Label(Mode);
    public int Created { get; set; }
    public int Finished { get; set; }
    public int InProgress { get; set; }
    public int Waiting { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public int RedWins { get; set; }
    public int BlackWins { get; set; }
    public int RatedFinished { get; set; }
    public int ResultPoints => 2 * Wins + Draws; // Informational 2/1/0 tally; not Elo or an official tournament.
    public double WinRate => Finished == 0 ? 0 : Math.Round(100d * Wins / Finished, 1);
}
public sealed class LearningStatistics
{
    public int TotalLessons { get; set; }
    public int CompletedLessonRecords { get; set; }
    public int QuizAttempts { get; set; }
    public double AverageBestQuizScore { get; set; }
    public int PuzzlesSolved { get; set; }
    public int PuzzleAttempts { get; set; }
    public int UserCount { get; set; } = 1;
    public double AverageCompletedLessons => UserCount > 0 ? Math.Round((double)CompletedLessonRecords / UserCount, 1) : 0;
    public double AveragePuzzlesSolved => UserCount > 0 ? Math.Round((double)PuzzlesSolved / UserCount, 1) : 0;
}
public sealed class StatisticsService(AppDbContext db)
{
    public async Task<List<ModeStatistics>> ModesAsync(int? userId = null, CancellationToken ct = default)
    {
        var query = db.Games.AsNoTracking();
        if (userId.HasValue) query = query.Where(g => g.RedPlayerId == userId || g.BlackPlayerId == userId);

        var rawGames = await query
            .Where(g => !(g.GameMode == "OnlinePublic" && g.BlackPlayerId == null && g.Status == GameStatus.Resigned))
            .Select(g => new {
            g.GameMode,
            g.Status,
            g.RedPlayerId,
            g.BlackPlayerId,
            g.IsRated,
            g.InitialFen
        }).ToListAsync(ct);

        var grouped = rawGames
            .GroupBy(g => GameModes.Normalize(g.GameMode, g.RedPlayerId, g.BlackPlayerId))
            .ToDictionary(
                grp => grp.Key,
                grp => new ModeStatistics
                {
                    Mode = grp.Key,
                    Created = grp.Count(),
                    Finished = grp.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw),
                    Waiting = grp.Count(g => g.Status == GameStatus.WaitingForPlayers),
                    InProgress = grp.Count(g => g.Status == GameStatus.InProgress),
                    Draws = grp.Count(g => g.Status == GameStatus.Draw),
                    RedWins = grp.Count(g => g.Status == GameStatus.RedWins),
                    BlackWins = grp.Count(g => g.Status == GameStatus.BlackWins),
                    Wins = userId == null ? 0 : grp.Count(g => (g.RedPlayerId == userId && g.Status == GameStatus.RedWins)
                        || (g.BlackPlayerId == userId && g.Status == GameStatus.BlackWins)),
                    Losses = userId == null ? 0 : grp.Count(g => (g.RedPlayerId == userId && g.Status == GameStatus.BlackWins)
                        || (g.BlackPlayerId == userId && g.Status == GameStatus.RedWins)),
                    RatedFinished = grp.Count(g => GameModes.Normalize(g.GameMode) == GameModes.Online 
                        && g.InitialFen == null && g.IsRated 
                        && (g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw))
                }
            );

        var rows = new List<ModeStatistics>();
        foreach (var mode in GameModes.StandardModes)
        {
            if (grouped.TryGetValue(mode, out var stat))
            {
                rows.Add(stat);
            }
            else
            {
                rows.Add(new ModeStatistics { Mode = mode });
            }
        }

        return rows;
    }

    public async Task<List<ModeStatistics>> ModesForUsersAsync(IReadOnlyCollection<int> userIds, CancellationToken ct = default)
    {
        if (userIds == null || userIds.Count == 0) return await ModesAsync(null, ct);
        if (userIds.Count == 1) return await ModesAsync(userIds.First(), ct);

        var query = db.Games.AsNoTracking()
            .Where(g => (g.RedPlayerId.HasValue && userIds.Contains(g.RedPlayerId.Value)) 
                     || (g.BlackPlayerId.HasValue && userIds.Contains(g.BlackPlayerId.Value)));

        var rawGames = await query
            .Where(g => !(g.GameMode == "OnlinePublic" && g.BlackPlayerId == null && g.Status == GameStatus.Resigned))
            .Select(g => new {
                g.GameMode,
                g.Status,
                g.RedPlayerId,
                g.BlackPlayerId,
                g.IsRated,
                g.InitialFen
            }).ToListAsync(ct);

        var grouped = rawGames
            .GroupBy(g => GameModes.Normalize(g.GameMode, g.RedPlayerId, g.BlackPlayerId))
            .ToDictionary(
                grp => grp.Key,
                grp => new ModeStatistics
                {
                    Mode = grp.Key,
                    Created = grp.Count(),
                    Finished = grp.Count(g => g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw),
                    Waiting = grp.Count(g => g.Status == GameStatus.WaitingForPlayers),
                    InProgress = grp.Count(g => g.Status == GameStatus.InProgress),
                    Draws = grp.Count(g => g.Status == GameStatus.Draw),
                    RedWins = grp.Count(g => g.Status == GameStatus.RedWins),
                    BlackWins = grp.Count(g => g.Status == GameStatus.BlackWins),
                    Wins = grp.Count(g => (g.RedPlayerId.HasValue && userIds.Contains(g.RedPlayerId.Value) && g.Status == GameStatus.RedWins)
                                       || (g.BlackPlayerId.HasValue && userIds.Contains(g.BlackPlayerId.Value) && g.Status == GameStatus.BlackWins)),
                    Losses = grp.Count(g => (g.RedPlayerId.HasValue && userIds.Contains(g.RedPlayerId.Value) && g.Status == GameStatus.BlackWins)
                                         || (g.BlackPlayerId.HasValue && userIds.Contains(g.BlackPlayerId.Value) && g.Status == GameStatus.RedWins)),
                    RatedFinished = grp.Count(g => GameModes.Normalize(g.GameMode) == GameModes.Online 
                        && g.InitialFen == null && g.IsRated 
                        && (g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw))
                }
            );

        var rows = new List<ModeStatistics>();
        foreach (var mode in GameModes.StandardModes)
        {
            if (grouped.TryGetValue(mode, out var stat))
            {
                rows.Add(stat);
            }
            else
            {
                rows.Add(new ModeStatistics { Mode = mode });
            }
        }

        return rows;
    }

    public async Task<LearningStatistics> LearningForUsersAsync(IReadOnlyCollection<int> userIds, CancellationToken ct = default)
    {
        var lessons = db.UserCourseProgresses.AsNoTracking();
        var puzzles = db.PuzzleProgresses.AsNoTracking();
        int uCount = (userIds != null && userIds.Count > 0) ? userIds.Distinct().Count() : 1;
        if (userIds != null && userIds.Count > 0)
        {
            lessons = lessons.Where(p => userIds.Contains(p.UserId));
            puzzles = puzzles.Where(p => userIds.Contains(p.UserId));
        }
        return new LearningStatistics {
            UserCount = uCount,
            TotalLessons = await db.CourseLessons.CountAsync(ct),
            CompletedLessonRecords = await lessons.CountAsync(p => p.IsCompleted, ct),
            QuizAttempts = await lessons.SumAsync(p => (int?)p.QuizAttempts, ct) ?? 0,
            AverageBestQuizScore = Math.Round(await lessons.Where(p => p.QuizAttempts > 0)
                .AverageAsync(p => (double?)p.BestQuizScore, ct) ?? 0, 1),
            PuzzlesSolved = await puzzles.CountAsync(p => p.CompletedAt != null, ct),
            PuzzleAttempts = await puzzles.SumAsync(p => (int?)p.Attempts, ct) ?? 0
        };
    }

    public async Task<ChessCore.ViewModels.AdvancedStatsResponseDto> GetAdvancedStatsAsync(IReadOnlyCollection<int> userIds, CancellationToken ct = default)
    {
        var selectedIds = userIds.Distinct().ToList();
        var users = await db.Users.AsNoTracking()
            .Where(u => selectedIds.Contains(u.Id))
            .OrderByDescending(u => u.Elo)
            .ThenBy(u => u.Id)
            .ToListAsync(ct);

        await ApplyDisplayCountersAsync(users);

        var puzzleSolvedCounts = await db.PuzzleProgresses.AsNoTracking()
            .Where(p => p.CompletedAt != null && selectedIds.Contains(p.UserId))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var lessonCompletedCounts = await db.UserCourseProgresses.AsNoTracking()
            .Where(p => p.IsCompleted && selectedIds.Contains(p.UserId))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        int rankIdx = 1;
        var userDtos = users.Select(u =>
        {
            double uWinRate = u.GamesPlayed > 0 ? Math.Round((double)u.Wins * 100.0 / u.GamesPlayed, 1) : 0;
            string rankTitle = u.Elo switch
            {
                >= 1500 => "Đặc cấp Đại sư",
                >= 1400 => "Kiện tướng",
                >= 1250 => "Kỳ thủ Giỏi",
                >= 1150 => "Kỳ thủ Nghiệp dư",
                >= 1050 => "Người mới chơi",
                _ => "Tập sự"
            };
            return new ChessCore.ViewModels.UserAccountOptionDto
            {
                Id = u.Id,
                Username = u.Username,
                PlayerId = u.PlayerId,
                Elo = u.Elo,
                Title = rankTitle,
                GamesPlayed = u.GamesPlayed,
                Wins = u.Wins,
                Losses = u.Losses,
                Draws = u.Draws,
                WinRate = uWinRate,
                PuzzlesSolved = puzzleSolvedCounts.TryGetValue(u.Id, out int pCount) ? pCount : 0,
                LessonsCompleted = lessonCompletedCounts.TryGetValue(u.Id, out int lCount) ? lCount : 0,
                Rank = rankIdx++
            };
        }).ToList();

        var modeStats = await ModesForUsersAsync(selectedIds, ct);
        var learning = await LearningForUsersAsync(selectedIds, ct);

        var rankedModes = modeStats.Where(m => m.Mode != "AiVsAi").ToList();
        int totalWins = rankedModes.Sum(m => m.Wins);
        int totalLosses = rankedModes.Sum(m => m.Losses);
        int totalDraws = modeStats.Sum(m => m.Draws);
        int totalFinished = rankedModes.Sum(m => m.Finished);
        double avgWinRate = totalFinished > 0 ? Math.Round((double)totalWins * 100.0 / totalFinished, 1) : 0;

        int totalGamesSum = userDtos.Sum(u => u.GamesPlayed);
        int totalPuzzlesSum = userDtos.Sum(u => u.PuzzlesSolved);
        double avgElo = userDtos.Count > 0 ? Math.Round(userDtos.Average(u => u.Elo), 0) : 1200;
        int maxElo = userDtos.Count > 0 ? userDtos.Max(u => u.Elo) : 1200;
        int minElo = userDtos.Count > 0 ? userDtos.Min(u => u.Elo) : 1200;

        string displayEloText = userDtos.Count == 1 
            ? userDtos[0].Elo.ToString()
            : $"{avgElo:0}";

        var modeDist = modeStats.ToDictionary(m => m.Mode, m => m.Created);

        // =====================================================================
        // THỐNG KÊ CHI TIẾT ĐẤU MÁY THEO ĐỘ SÂU AI (DEPTH 1, 2, 3, 4)
        // =====================================================================
        var pveGames = await db.Games.AsNoTracking()
            .Where(g => (g.GameMode == GameModes.PvE || g.GameMode == "PvE")
                     && ((g.RedPlayerId.HasValue && selectedIds.Contains(g.RedPlayerId.Value))
                      || (g.BlackPlayerId.HasValue && selectedIds.Contains(g.BlackPlayerId.Value))))
            .Select(g => new {
                g.Id,
                g.Status,
                g.RedPlayerId,
                g.BlackPlayerId,
                AiDepth = (g.AiDepth.HasValue && g.AiDepth.Value >= 1 && g.AiDepth.Value <= 4) ? g.AiDepth.Value : 1
            })
            .ToListAsync(ct);

        // 1. Thống kê theo Depth cho từng User được chọn
        foreach (var u in userDtos)
        {
            var uPveGames = pveGames.Where(g => g.RedPlayerId == u.Id || g.BlackPlayerId == u.Id).ToList();
            var uDepthList = new List<ChessCore.ViewModels.AiDepthStatDto>();

            for (int d = 1; d <= 4; d++)
            {
                var udGames = uPveGames.Where(g => g.AiDepth == d).ToList();
                int total = udGames.Count;
                int wins = udGames.Count(g => (g.RedPlayerId == u.Id && g.Status == GameStatus.RedWins)
                                           || (g.BlackPlayerId == u.Id && g.Status == GameStatus.BlackWins));
                int losses = udGames.Count(g => (g.RedPlayerId == u.Id && g.Status == GameStatus.BlackWins)
                                             || (g.BlackPlayerId == u.Id && g.Status == GameStatus.RedWins));
                int draws = udGames.Count(g => g.Status == GameStatus.Draw);
                int finished = wins + losses + draws;
                double wr = finished > 0 ? Math.Round((double)wins * 100.0 / finished, 1) : 0;

                string dName = d switch
                {
                    1 => "Cấp 1 - Dễ (Depth 1)",
                    2 => "Cấp 2 - Trung bình (Depth 2)",
                    3 => "Cấp 3 - Khó (Depth 3)",
                    4 => "Cấp 4 - Siêu khó (Depth 4)",
                    _ => $"Depth {d}"
                };

                var statDto = new ChessCore.ViewModels.AiDepthStatDto
                {
                    Depth = d,
                    Name = dName,
                    TotalGames = total,
                    Wins = wins,
                    Losses = losses,
                    Draws = draws,
                    WinRate = wr
                };
                uDepthList.Add(statDto);

                switch (d)
                {
                    case 1:
                        u.Depth1Games = total; u.Depth1Wins = wins; u.Depth1Losses = losses; u.Depth1WinRate = wr;
                        break;
                    case 2:
                        u.Depth2Games = total; u.Depth2Wins = wins; u.Depth2Losses = losses; u.Depth2WinRate = wr;
                        break;
                    case 3:
                        u.Depth3Games = total; u.Depth3Wins = wins; u.Depth3Losses = losses; u.Depth3WinRate = wr;
                        break;
                    case 4:
                        u.Depth4Games = total; u.Depth4Wins = wins; u.Depth4Losses = losses; u.Depth4WinRate = wr;
                        break;
                }
            }

            u.DepthStats = uDepthList;
            u.TotalPveGames = uPveGames.Count;
            u.PveWins = uDepthList.Sum(x => x.Wins);
            u.PveLosses = uDepthList.Sum(x => x.Losses);
            u.PveDraws = uDepthList.Sum(x => x.Draws);
            int uPveFinished = u.PveWins + u.PveLosses + u.PveDraws;
            u.PveWinRate = uPveFinished > 0 ? Math.Round((double)u.PveWins * 100.0 / uPveFinished, 1) : 0;
        }

        // 2. Thống kê theo Depth tổng hợp cho cả nhóm (Group Summary)
        var depthSummary = new List<ChessCore.ViewModels.AiDepthStatDto>();
        for (int d = 1; d <= 4; d++)
        {
            var dGames = pveGames.Where(g => g.AiDepth == d).ToList();
            int dTotal = dGames.Count;
            int dWins = dGames.Count(g => (g.RedPlayerId.HasValue && selectedIds.Contains(g.RedPlayerId.Value) && g.Status == GameStatus.RedWins)
                                       || (g.BlackPlayerId.HasValue && selectedIds.Contains(g.BlackPlayerId.Value) && g.Status == GameStatus.BlackWins));
            int dLosses = dGames.Count(g => (g.RedPlayerId.HasValue && selectedIds.Contains(g.RedPlayerId.Value) && g.Status == GameStatus.BlackWins)
                                         || (g.BlackPlayerId.HasValue && selectedIds.Contains(g.BlackPlayerId.Value) && g.Status == GameStatus.RedWins));
            int dDraws = dGames.Count(g => g.Status == GameStatus.Draw);
            int dFinished = dWins + dLosses + dDraws;
            double dWinRate = dFinished > 0 ? Math.Round((double)dWins * 100.0 / dFinished, 1) : 0;
            string dName = d switch
            {
                1 => "Cấp 1 - Dễ (Depth 1)",
                2 => "Cấp 2 - Trung bình (Depth 2)",
                3 => "Cấp 3 - Khó (Depth 3)",
                4 => "Cấp 4 - Siêu khó (Depth 4)",
                _ => $"Depth {d}"
            };
            depthSummary.Add(new ChessCore.ViewModels.AiDepthStatDto
            {
                Depth = d,
                Name = dName,
                TotalGames = dTotal,
                Wins = dWins,
                Losses = dLosses,
                Draws = dDraws,
                WinRate = dWinRate
            });
        }

        return new ChessCore.ViewModels.AdvancedStatsResponseDto
        {
            Kpis = new ChessCore.ViewModels.AdvancedKpiDto
            {
                TotalGames = totalGamesSum,
                WinRate = avgWinRate,
                AvgElo = avgElo,
                MaxElo = maxElo,
                MinElo = minElo,
                PuzzlesSolved = totalPuzzlesSum,
                SelectedCount = userDtos.Count,
                DisplayEloText = displayEloText
            },
            ModeStats = modeStats,
            Users = userDtos,
            Learning = learning,
            Donut = new ChessCore.ViewModels.AdvancedDonutDto
            {
                Wins = totalWins,
                Losses = totalLosses,
                Draws = totalDraws,
                WinRate = avgWinRate
            },
            ModeDistribution = modeDist,
            AiDepthSummary = depthSummary
        };
    }

    public async Task<List<ChessCore.ViewModels.AiDepthStatDto>> GetAiDepthStatsAsync(IReadOnlyCollection<int> userIds, CancellationToken ct = default)
    {
        var selectedIds = userIds.Distinct().ToList();
        var pveGames = await db.Games.AsNoTracking()
            .Where(g => (g.GameMode == GameModes.PvE || g.GameMode == "PvE")
                     && ((g.RedPlayerId.HasValue && selectedIds.Contains(g.RedPlayerId.Value))
                      || (g.BlackPlayerId.HasValue && selectedIds.Contains(g.BlackPlayerId.Value))))
            .Select(g => new {
                g.Id,
                g.Status,
                g.RedPlayerId,
                g.BlackPlayerId,
                AiDepth = (g.AiDepth.HasValue && g.AiDepth.Value >= 1 && g.AiDepth.Value <= 4) ? g.AiDepth.Value : 1
            })
            .ToListAsync(ct);

        var depthSummary = new List<ChessCore.ViewModels.AiDepthStatDto>();
        for (int d = 1; d <= 4; d++)
        {
            var dGames = pveGames.Where(g => g.AiDepth == d).ToList();
            int dTotal = dGames.Count;
            int dWins = dGames.Count(g => (g.RedPlayerId.HasValue && selectedIds.Contains(g.RedPlayerId.Value) && g.Status == GameStatus.RedWins)
                                       || (g.BlackPlayerId.HasValue && selectedIds.Contains(g.BlackPlayerId.Value) && g.Status == GameStatus.BlackWins));
            int dLosses = dGames.Count(g => (g.RedPlayerId.HasValue && selectedIds.Contains(g.RedPlayerId.Value) && g.Status == GameStatus.BlackWins)
                                         || (g.BlackPlayerId.HasValue && selectedIds.Contains(g.BlackPlayerId.Value) && g.Status == GameStatus.RedWins));
            int dDraws = dGames.Count(g => g.Status == GameStatus.Draw);
            int dFinished = dWins + dLosses + dDraws;
            double dWinRate = dFinished > 0 ? Math.Round((double)dWins * 100.0 / dFinished, 1) : 0;
            string dName = d switch
            {
                1 => "Cấp 1 - Dễ (Depth 1)",
                2 => "Cấp 2 - Trung bình (Depth 2)",
                3 => "Cấp 3 - Khó (Depth 3)",
                4 => "Cấp 4 - Siêu khó (Depth 4)",
                _ => $"Depth {d}"
            };
            depthSummary.Add(new ChessCore.ViewModels.AiDepthStatDto
            {
                Depth = d,
                Name = dName,
                TotalGames = dTotal,
                Wins = dWins,
                Losses = dLosses,
                Draws = dDraws,
                WinRate = dWinRate
            });
        }
        return depthSummary;
    }
    public async Task<LearningStatistics> LearningAsync(int? userId = null, CancellationToken ct = default)
    {
        var lessons = db.UserCourseProgresses.AsNoTracking();
        var puzzles = db.PuzzleProgresses.AsNoTracking();
        if (userId.HasValue) { lessons = lessons.Where(p => p.UserId == userId); puzzles = puzzles.Where(p => p.UserId == userId); }
        return new LearningStatistics {
            UserCount = 1,
            TotalLessons = await db.CourseLessons.CountAsync(ct),
            CompletedLessonRecords = await lessons.CountAsync(p => p.IsCompleted, ct),
            QuizAttempts = await lessons.SumAsync(p => (int?)p.QuizAttempts, ct) ?? 0,
            AverageBestQuizScore = Math.Round(await lessons.Where(p => p.QuizAttempts > 0)
                .AverageAsync(p => (double?)p.BestQuizScore, ct) ?? 0, 1),
            PuzzlesSolved = await puzzles.CountAsync(p => p.CompletedAt != null, ct),
            PuzzleAttempts = await puzzles.SumAsync(p => (int?)p.Attempts, ct) ?? 0
        };
    }
    // Display counters are rebuilt from recorded, completed human games.
    // Keep the supplied entities detached so these don't overwrite legacy data.
    public async Task ApplyDisplayCountersAsync(IReadOnlyCollection<ChessCore.Models.User> users)
    {
        var ids = users.Select(u => u.Id).ToArray();
        var games = db.Games.AsNoTracking().Where(g =>
            !(g.GameMode == "OnlinePublic" && g.BlackPlayerId == null && g.Status == GameStatus.Resigned) &&
            (g.GameMode.StartsWith("Online") || g.GameMode == GameModes.PvE || g.GameMode == "PvP" || g.GameMode == "Local") &&
            (g.Status == GameStatus.RedWins || g.Status == GameStatus.BlackWins || g.Status == GameStatus.Draw));
        var red = await games.Where(g => g.RedPlayerId.HasValue && ids.Contains(g.RedPlayerId.Value))
            .GroupBy(g => g.RedPlayerId!.Value).Select(g => new { Id = g.Key, Played = g.Count(),
                Wins = g.Count(x => x.Status == GameStatus.RedWins),
                Losses = g.Count(x => x.Status == GameStatus.BlackWins),
                Draws = g.Count(x => x.Status == GameStatus.Draw) }).ToListAsync();
        var black = await games.Where(g => g.BlackPlayerId.HasValue && ids.Contains(g.BlackPlayerId.Value))
            .GroupBy(g => g.BlackPlayerId!.Value).Select(g => new { Id = g.Key, Played = g.Count(),
                Wins = g.Count(x => x.Status == GameStatus.BlackWins),
                Losses = g.Count(x => x.Status == GameStatus.RedWins),
                Draws = g.Count(x => x.Status == GameStatus.Draw) }).ToListAsync();
        var totals = red.Concat(black).GroupBy(r => r.Id).ToDictionary(g => g.Key,
            g => (Played: g.Sum(x => x.Played), Wins: g.Sum(x => x.Wins),
                  Losses: g.Sum(x => x.Losses), Draws: g.Sum(x => x.Draws)));
        foreach (var u in users)
        {
            if (totals.TryGetValue(u.Id, out var t))
            {
                u.GamesPlayed = t.Played; u.Wins = t.Wins; u.Losses = t.Losses; u.Draws = t.Draws;
            }
        }
    }

    public static byte[] Csv(IEnumerable<ModeStatistics> rows, bool admin, LearningStatistics? learning = null)
    {
        var text = new StringBuilder("Chế độ,Đã tạo,Đang chơi,Chờ,Đã kết thúc,Đỏ thắng,Đen thắng,Hòa,Online có xếp hạng,Thắng cá nhân,Thua cá nhân,Tỷ lệ thắng cá nhân\r\n");
        foreach (var r in rows)
            text.AppendLine(string.Join(",", r.Label, r.Created, r.InProgress, r.Waiting,
                r.Finished, r.RedWins, r.BlackWins, r.Draws, r.RatedFinished,
                admin || (r.Mode != GameModes.Online && r.Mode != GameModes.PvE) ? "" : r.Wins.ToString(), admin || (r.Mode != GameModes.Online && r.Mode != GameModes.PvE) ? "" : r.Losses.ToString(),
                admin || (r.Mode != GameModes.Online && r.Mode != GameModes.PvE) ? "" : r.WinRate.ToString(CultureInfo.InvariantCulture)));
        if (learning != null)
        {
            text.AppendLine();
            text.AppendLine("Học tập,Tổng bài học,Bản ghi hoàn thành,Lượt quiz,TB điểm tốt nhất %,Bản ghi cờ thế đã giải,Lượt lời giải");
            text.AppendLine(string.Join(",", admin ? "Toàn hệ thống" : "Cá nhân", learning.TotalLessons,
                learning.CompletedLessonRecords, learning.QuizAttempts,
                learning.AverageBestQuizScore.ToString(CultureInfo.InvariantCulture),
                learning.PuzzlesSolved, learning.PuzzleAttempts));
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }
}
