using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChessCore.Engine;
using ChessCore.Enums;
using ChessCore.Models;

namespace ChessCore.Services
{
    /// <summary>Một bài Cờ Thế đã chuẩn hóa từ dataset (không sửa file gốc).</summary>
    public class PuzzleEntry
    {
        /// <summary>Định danh ổn định: "basic:12" = bản ghi thứ 12 của basic-checkmates.json.</summary>
        public string Key { get; set; } = "";
        public string Source { get; set; } = "";
        public int OriginalIndex { get; set; }
        public string OriginalName { get; set; } = "";
        public string Fen { get; set; } = "";
        public PieceColor Attacker { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string TacticalMotif { get; set; } = "";
        public int Pieces { get; set; }

        /// <summary>Số nước chiếu bí cưỡng bức đã được MateSolver xác minh (1..3); 0 = chưa xác minh.</summary>
        public int MateIn { get; set; }

        /// <summary>Cấp độ 1..5; 0 = chưa phân loại.</summary>
        public int Level { get; set; }

        /// <summary>Thứ tự trong cấp độ (bắt đầu từ 1).</summary>
        public int Number { get; set; }
    }

    public class PuzzleLevelDef
    {
        public int Level { get; set; }
        public string Name { get; set; } = "";
        public string Rule { get; set; } = "";
    }

    /// <summary>
    /// Đọc Data/Puzzles, chuẩn hóa, xác minh lời giải chiếu bí bằng MateSolver
    /// và phân cấp độ. Kết quả phân tích được cache ở Data/PuzzleCache.
    /// </summary>
    public class PuzzleCampaignService
    {
        public const int AnalysisDepth = MateSolver.MaxSupportedDepth;
        private const int CacheVersion = 1;

        public static readonly PuzzleLevelDef[] Levels =
        {
            new() { Level = 1, Name = "Làm quen",  Rule = "Sát cục đa binh chủng & thử thách 2 nước (Bài 1 - 100)" },
            new() { Level = 2, Name = "Cơ bản",    Rule = "Sát cục cơ bản, phối hợp 1–2 nước (Bài 101 - 200)" },
            new() { Level = 3, Name = "Trung cấp", Rule = "Sát cục trung cấp, thế cờ thực chiến (Bài 201 - 300)" },
            new() { Level = 4, Name = "Nâng cao",  Rule = "Sát cục nâng cao, 3 nước hiểm hóc (Bài 301 - 400)" },
            new() { Level = 5, Name = "Chuyên sâu", Rule = "Sát cục chuyên sâu & giang hồ đỉnh cao (Bài 401 - 500)" },
        };

        /// <summary>Tỉ lệ bài cần hoàn thành ở một cấp để mở cấp kế tiếp (100% để đảm bảo tuần tự).</summary>
        public const double UnlockRatio = 1.0;

        private readonly ILogger<PuzzleCampaignService> _logger;
        private readonly object _lock = new();

        private List<PuzzleEntry> _all = new();
        private Dictionary<string, PuzzleEntry> _byKey = new();
        private List<PuzzleEntry> _playable = new();

        public bool Ready { get; private set; }
        public string AnalysisState { get; private set; } = "Chưa bắt đầu";
        public int Analyzed { get; private set; }
        public int ToAnalyze { get; private set; }

        public Dictionary<string, int> Stats { get; } = new();
        public Dictionary<string, int> RejectReasons { get; } = new();

        public PuzzleCampaignService(ILogger<PuzzleCampaignService> logger)
        {
            _logger = logger;
        }

        // =====================================================
        // LOAD
        // =====================================================

        public void Start(string dataDir, string cacheDir)
        {
            try
            {
                ReadAll(dataDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không đọc được dataset Cờ Thế");
                AnalysisState = "Lỗi đọc dataset: " + ex.Message;
                return;
            }

            Task.Run(() => AnalyzeAndFinish(cacheDir));
        }

        private static string ReadText(string path) =>
            File.ReadAllText(path, Encoding.UTF8).Replace("\0", "");

        private void Reject(string reason)
        {
            lock (_lock)
            {
                RejectReasons[reason] = RejectReasons.GetValueOrDefault(reason) + 1;
            }
        }

        private void ReadAll(string dataDir)
        {
            var seen = new HashSet<string>();
            var list = new List<PuzzleEntry>();

            foreach (var (source, file) in new[]
            {
                ("basic", "basic-checkmates.json"),
                ("advanced", "advanced-checkmates.json"),
            })
            {
                string path = Path.Combine(dataDir, file);
                int read = 0, valid = 0;

                if (!File.Exists(path))
                {
                    Reject($"{source}: thiếu file {file}");
                    continue;
                }

                using JsonDocument doc = JsonDocument.Parse(ReadText(path));
                int index = -1;

                foreach (JsonElement el in doc.RootElement.EnumerateArray())
                {
                    index++;
                    read++;

                    try
                    {
                        string fen = el.TryGetProperty("fen", out var f) ? f.GetString() ?? "" : "";
                        string name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";

                        if (!Board.TryParseFen(fen, out Board? board, out string error) || board == null)
                        {
                            Reject($"{source}: FEN không hợp lệ ({error})");
                            continue;
                        }

                        string normalized = board.ToFen();
                        if (!seen.Add(normalized))
                        {
                            Reject($"{source}: trùng thế cờ");
                            continue;
                        }

                        valid++;
                        list.Add(new PuzzleEntry
                        {
                            Key = $"{source}:{index}",
                            Source = source,
                            OriginalIndex = index,
                            OriginalName = name,
                            Fen = normalized,
                            Attacker = board.CurrentTurn,
                            Pieces = CountPieces(board),
                            Category = source == "basic" ? "Sát cục cơ bản" : "Sát cục nâng cao",
                            Title = PuzzleTitles.Translate(source, name, index + 1),
                        });
                    }
                    catch (Exception ex)
                    {
                        Reject($"{source}: bản ghi lỗi ({ex.GetType().Name})");
                    }
                }

                Stats[$"{source}_read"] = read;
                Stats[$"{source}_valid"] = valid;
            }

            // Cờ tàn thực chiến: Nạp các thế cờ tàn chuẩn để mở rộng kho cờ thế
            string endPath = Path.Combine(dataDir, "endgames_all.json");
            if (File.Exists(endPath))
            {
                int read = 0, valid = 0, withMove = 0;
                using JsonDocument doc = JsonDocument.Parse(ReadText(endPath));
                int index = -1;
                foreach (JsonElement el in doc.RootElement.EnumerateArray())
                {
                    index++;
                    read++;
                    try
                    {
                        string fen = el.TryGetProperty("fen", out var f) ? f.GetString() ?? "" : "";
                        string type = el.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                        if (el.TryGetProperty("bestMove", out var bm) && !string.IsNullOrWhiteSpace(bm.GetString()?.Replace("\0", "")))
                            withMove++;

                        if (!Board.TryParseFen(fen, out Board? board, out _) || board == null)
                            continue;

                        string normalized = board.ToFen();
                        if (!seen.Add(normalized))
                            continue;

                        valid++;
                        list.Add(new PuzzleEntry
                        {
                            Key = $"endgame:{index}",
                            Source = "endgame",
                            OriginalIndex = index,
                            OriginalName = type,
                            Fen = normalized,
                            Attacker = board.CurrentTurn,
                            Pieces = CountPieces(board),
                            Category = "Cờ tàn thực chiến",
                            Title = PuzzleTitles.Translate("endgame", type, index + 1),
                        });
                    }
                    catch
                    {
                        // Bỏ qua bản ghi lỗi trong endgames
                    }
                }
                Stats["endgames_read"] = read;
                Stats["endgames_valid"] = valid;
                Stats["endgames_with_best_move"] = withMove;
            }

            _all = list;
            ToAnalyze = list.Count;
        }

        private static int CountPieces(Board board)
        {
            int count = 0;
            for (int r = 0; r < 10; r++)
                for (int c = 0; c < 9; c++)
                    if (board.Grid[r, c] != null) count++;
            return count;
        }

        // =====================================================
        // ANALYZE
        // =====================================================

        private class CacheFile
        {
            public int Version { get; set; }
            public int Depth { get; set; }
            public Dictionary<string, int> MateIn { get; set; } = new();
        }

        private void AnalyzeAndFinish(string cacheDir)
        {
            try
            {
                string cachePath = Path.Combine(cacheDir, "mate-analysis.json");
                var known = new ConcurrentDictionary<string, int>();

                if (File.Exists(cachePath))
                {
                    try
                    {
                        var cache = JsonSerializer.Deserialize<CacheFile>(File.ReadAllText(cachePath));
                        if (cache != null && cache.Version == CacheVersion && cache.Depth == AnalysisDepth)
                            foreach (var kv in cache.MateIn) known[kv.Key] = kv.Value;
                    }
                    catch { /* cache hỏng: phân tích lại */ }
                }

                var pending = _all.Where(p => !known.ContainsKey(p.Fen)).ToList();
                ToAnalyze = pending.Count;
                AnalysisState = pending.Count == 0
                    ? "Đã có kết quả phân tích"
                    : "Đang xác minh lời giải chiếu bí bằng engine...";

                Parallel.ForEach(pending, new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
                }, entry =>
                {
                    int mate = 0;
                    try
                    {
                        Board board = Board.FromFen(entry.Fen);
                        int d = MateSolver.MateDistance(board, board.CurrentTurn, AnalysisDepth);
                        mate = d > 0 ? d : 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không phân tích được {Key}", entry.Key);
                    }

                    known[entry.Fen] = mate;
                    lock (_lock) { Analyzed++; }
                });

                if (pending.Count > 0)
                {
                    Directory.CreateDirectory(cacheDir);
                    File.WriteAllText(cachePath, JsonSerializer.Serialize(new CacheFile
                    {
                        Version = CacheVersion,
                        Depth = AnalysisDepth,
                        MateIn = known.ToDictionary(k => k.Key, v => v.Value),
                    }));
                }

                foreach (var p in _all)
                    p.MateIn = known.GetValueOrDefault(p.Fen);

                Finish();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi phân tích Cờ Thế");
                AnalysisState = "Lỗi phân tích: " + ex.Message;
            }
        }

        public static string GetTacticalMotif(PuzzleEntry p)
        {
            string board = p.Fen.Split(' ')[0];
            bool hasR = board.Contains('R');
            bool hasC = board.Contains('C');
            bool hasN = board.Contains('N') || board.Contains('H');
            bool hasP = board.Contains('P');

            if (hasC && hasN && !hasR) return "Pháo - Mã";
            if (hasC && !hasR && !hasN && !hasP) return "Pháo";
            if (hasN && !hasR && !hasC && !hasP) return "Mã";
            if (hasR && hasC && !hasN) return "Xe - Pháo";
            if (hasR && hasN && !hasC) return "Xe - Mã";
            if (hasP && !hasR && !hasC && !hasN) return "Binh";
            if (hasC && hasP && !hasR && !hasN) return "Pháo - Tốt";
            if (hasN && hasP && !hasR && !hasC) return "Mã - Tốt";
            if (hasR && hasP && !hasC && !hasN) return "Xe - Tốt";
            if (hasR && !hasC && !hasN && !hasP) return "Xe";
            return "Tổng hợp";
        }

        private static readonly string[] MotifOrder =
        {
            "Pháo",
            "Mã",
            "Pháo - Mã",
            "Xe - Pháo",
            "Xe - Mã",
            "Pháo - Tốt",
            "Mã - Tốt",
            "Binh",
            "Tổng hợp",
            "Xe - Tốt",
            "Xe"
        };

        private static List<PuzzleEntry> BuildInterleavedList(IEnumerable<PuzzleEntry> pool)
        {
            var poolList = pool.ToList();
            var buckets = new Dictionary<string, List<PuzzleEntry>>();

            foreach (var m in MotifOrder)
            {
                buckets[m] = poolList
                    .Where(p => GetTacticalMotif(p) == m)
                    .OrderBy(p => p.Source == "basic" ? 1 : (p.Source == "advanced" ? 2 : 3))
                    .ThenBy(p => p.Pieces)
                    .ThenBy(p => p.OriginalIndex)
                    .ToList();
            }

            var result = new List<PuzzleEntry>();
            while (true)
            {
                int added = 0;
                foreach (var m in MotifOrder)
                {
                    if (buckets.TryGetValue(m, out var bucket) && bucket.Count > 0)
                    {
                        result.Add(bucket[0]);
                        bucket.RemoveAt(0);
                        added++;
                    }
                }
                if (added == 0) break;
            }

            return result;
        }

        private void Finish()
        {
            // Sắp xếp các pool MateIn 1..3 theo thuật toán đa dạng hóa binh chủng xen kẽ
            var m1Pool = BuildInterleavedList(_all.Where(p => p.MateIn == 1));
            var m2Pool = BuildInterleavedList(_all.Where(p => p.MateIn == 2));
            var m3Pool = BuildInterleavedList(_all.Where(p => p.MateIn == 3));

            static List<PuzzleEntry> TakeFrom(List<PuzzleEntry> source, int count)
            {
                int take = Math.Min(count, source.Count);
                var items = source.Take(take).ToList();
                source.RemoveRange(0, take);
                return items;
            }

            // Phân bổ đều đặn và hấp dẫn thành 500 bài (5 cấp, mỗi cấp đúng 100 bài):
            // Cấp 1 (Bài 1 - 100): 40 bài sát cục 1 nước đa binh chủng (Pháo, Mã, Pháo-Mã, Xe-Pháo, Xe-Mã, Binh, Xe)
            //                      + 60 bài sát cục 2 nước nhập môn kích thích tư duy, xoá bỏ hoàn toàn cảm giác lặp xe nhàm chán
            var l1 = TakeFrom(m1Pool, 40).Concat(TakeFrom(m2Pool, 60)).ToList();

            // Cấp 2 (Bài 101 - 200): 50 bài sát cục 1 nước đa binh chủng + 50 bài sát cục 2 nước
            var l2 = TakeFrom(m1Pool, 50).Concat(TakeFrom(m2Pool, 50)).ToList();

            // Cấp 3 (Bài 201 - 300): 34 bài sát cục 1 nước + 66 bài sát cục 2 nước (dùng hết toàn bộ m2Pool)
            var l3 = TakeFrom(m1Pool, 34).Concat(TakeFrom(m2Pool, m2Pool.Count)).ToList();

            // Cấp 4 (Bài 301 - 400): 100 bài sát cục 3 nước chuyên sâu
            var l4 = TakeFrom(m3Pool, 100).ToList();

            // Cấp 5 (Bài 401 - 500): Toàn bộ sát cục 3 nước còn lại (96 bài) + 4 bài sát cục đặc sắc từ m1Pool
            var l5 = TakeFrom(m3Pool, m3Pool.Count).Concat(TakeFrom(m1Pool, 4)).ToList();

            var levelsList = new[] { l1, l2, l3, l4, l5 };
            var playable = new List<PuzzleEntry>();

            for (int lv = 0; lv < levelsList.Length; lv++)
            {
                int levelNum = lv + 1;
                var list = levelsList[lv];
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i];
                    p.Level = levelNum;
                    p.Number = i + 1;
                    string motif = GetTacticalMotif(p);
                    p.TacticalMotif = motif;

                    if (p.Source == "endgame")
                    {
                        p.Category = motif switch
                        {
                            "Pháo - Mã" => "Mã Hậu Pháo & Pháo Mã",
                            "Pháo" => "Sát pháp Pháo & Trùng Pháo",
                            "Mã" => "Sát pháp Mã & Ngọa Tào",
                            "Xe - Pháo" => "Sát cục Xa Pháo phối hợp",
                            "Xe - Mã" => "Sát cục Xa Mã phối hợp",
                            "Xe - Tốt" => "Sát cục Xe Tốt áp thành",
                            "Pháo - Tốt" => "Sát cục Pháo Tốt kết hợp",
                            "Mã - Tốt" => "Sát cục Mã Tốt kết hợp",
                            "Binh" => "Sát pháp Binh / Tốt khóa cung",
                            "Xe" => "Sát cục Thiết Môn Thuyên & Xa",
                            _ => "Sát pháp phối hợp tổng hợp"
                        };
                    }
                    playable.Add(p);
                }
            }

            Stats["verified"] = playable.Count;
            Stats["unverified"] = _all.Count - playable.Count;
            for (int m = 1; m <= AnalysisDepth; m++)
                Stats[$"mate_in_{m}"] = playable.Count(p => p.MateIn == m);
            for (int l = 1; l <= Levels.Length; l++)
                Stats[$"level_{l}"] = playable.Count(p => p.Level == l);

            _byKey = _all.ToDictionary(p => p.Key);
            _playable = playable;
            AnalysisState = "Sẵn sàng";
            Ready = true;
        }

        // =====================================================
        // QUERY
        // =====================================================

        public IReadOnlyList<PuzzleEntry> Playable => _playable;

        public PuzzleEntry? Get(string key) =>
            _byKey.TryGetValue(key, out var e) ? e : null;

        public IReadOnlyList<PuzzleEntry> InLevel(int level) =>
            _playable.Where(p => p.Level == level).ToList();

        public static int RequiredToUnlockNext(int levelTotal) =>
            levelTotal == 0 ? 0 : (int)Math.Ceiling(levelTotal * UnlockRatio);

        /// <summary>Cấp nào được mở khi đã hoàn thành các bài trong <paramref name="done"/>.</summary>
        public bool IsLevelUnlocked(int level, ISet<string> done)
        {
            if (level <= 1) return true;

            for (int l = 1; l < level; l++)
            {
                var items = InLevel(l);
                if (items.Count == 0) continue; // cấp rỗng không chặn cấp sau
                int completed = items.Count(p => done.Contains(p.Key));
                if (completed < RequiredToUnlockNext(items.Count)) return false;
            }

            return true;
        }

        /// <summary>Kiểm tra xem một bài có được phép chơi theo tiến trình tuyến tính hay không.</summary>
        public bool IsPuzzleAccessible(PuzzleEntry entry, ISet<string> done)
        {
            if (entry.Level <= 0) return false;
            if (!IsLevelUnlocked(entry.Level, done)) return false;

            var levelPuzzles = InLevel(entry.Level);
            int idx = -1;
            for (int i = 0; i < levelPuzzles.Count; i++)
            {
                if (levelPuzzles[i].Key == entry.Key)
                {
                    idx = i;
                    break;
                }
            }

            if (idx <= 0) return true; // Bài đầu tiên của cấp luôn mở khi cấp mở

            // Chỉ cho phép chơi bài này nếu bài ngay trước đó đã được giải xong
            var prevKey = levelPuzzles[idx - 1].Key;
            return done.Contains(prevKey);
        }

        /// <summary>
        /// Chuẩn hóa tiến độ hoàn thành trong Campaign:
        /// Trong chế độ Campaign vượt ải 500 bài tuần tự, tiến trình phải là chuỗi liên tục từ bài 1 Cấp 1 trở đi.
        /// Nếu người chơi đã hoàn thành tổng cộng N bài (hoặc có các bài giải rải rác từ trước khi phân cấp mới),
        /// N bài hoàn thành đó sẽ được chuẩn hóa chính xác thành N bài đầu tiên của Campaign (Cấp 1 từ bài 1 đến N).
        /// </summary>
        public HashSet<string> GetLinearCampaignCompletedKeys(ISet<string> rawCompletedKeys)
        {
            var validPlayableKeys = _playable.Select(p => p.Key).ToHashSet();
            int validCount = rawCompletedKeys.Count(k => validPlayableKeys.Contains(k));

            if (validCount <= 0) return new HashSet<string>();

            // Lấy đúng validCount bài đầu tiên trong Playable (bắt đầu từ Bài 1 Cấp 1)
            var linearKeys = _playable.Take(validCount).Select(p => p.Key).ToHashSet();
            return linearKeys;
        }
    }

    /// <summary>
    /// Việt hóa tên bài. Bảng ánh xạ thuật ngữ (Hán-Việt theo cách gọi quen thuộc trong
    /// Cờ Tướng Việt Nam). Không khớp được thuật ngữ nào thì dùng tên trung tính.
    /// Tên gốc luôn được giữ trong <see cref="PuzzleEntry.OriginalName"/>.
    /// </summary>
    public static class PuzzleTitles
    {
        // Khóa dài đặt trước để khớp chính xác hơn.
        private static readonly (string Zh, string Vi)[] Terms =
        {
            ("单车破双象", "Xe đơn phá hai tượng"),
            ("双车胁士", "Song xa hiếp sĩ"),
            ("双车错", "Song xa thác"),
            ("双照将", "Song chiếu tướng"),
            ("三车闹士", "Tam xa náo sĩ"),
            ("二鬼拍门", "Nhị quỷ phách môn"),
            ("对面笑", "Đối diện tiếu (tướng đối mặt)"),
            ("白脸将", "Bạch diện tướng (tướng đối mặt)"),
            ("海底捞月", "Hải để lao nguyệt"),
            ("卧槽马", "Ngọa tào mã"),
            ("高钓马", "Cao điếu mã"),
            ("钓鱼马", "Điếu ngư mã"),
            ("八角马", "Bát giác mã"),
            ("拔簧马", "Bạt hoàng mã"),
            ("立马车", "Lập mã xa"),
            ("列马车", "Liệt mã xa"),
            ("马后炮", "Mã hậu pháo"),
            ("车马冷着", "Xa mã lãnh trước"),
            ("双马饮泉", "Song mã ẩm tuyền"),
            ("闷宫", "Muộn cung"),
            ("闷杀", "Muộn sát"),
            ("铁门栓", "Thiết môn thuyên"),
            ("铁门闩", "Thiết môn thuyên"),
            ("天地炮", "Thiên địa pháo"),
            ("炮辗丹沙", "Pháo triển đan sa"),
            ("大刀挖心", "Đại đao oản tâm"),
            ("大刀剜心", "Đại đao oản tâm"),
            ("大胆穿心", "Đại đảm xuyên tâm"),
            ("夹车炮", "Giáp xa pháo"),
            ("重炮", "Trùng pháo"),
            ("车炮抽杀", "Xa pháo trừu sát"),
            ("侧面虎", "Trắc diện hổ"),
            ("三进兵", "Tam tiến tốt"),
            ("组合运用", "Phối hợp vận dụng"),
            ("其它的杀着", "Sát pháp tổng hợp"),
        };

        private static readonly Dictionary<char, string> PieceNames = new()
        {
            ['车'] = "Xe", ['車'] = "Xe",
            ['马'] = "Mã", ['馬'] = "Mã",
            ['炮'] = "Pháo", ['砲'] = "Pháo",
            ['兵'] = "Tốt", ['卒'] = "Tốt",
            ['仕'] = "Sĩ", ['士'] = "Sĩ",
            ['相'] = "Tượng", ['象'] = "Tượng",
            ['帅'] = "Tướng", ['帥'] = "Tướng",
            ['将'] = "Tướng", ['將'] = "Tướng",
        };

        public static string Translate(string source, string originalName, int fallbackNumber)
        {
            string neutral = $"Thế cờ số {fallbackNumber}";
            if (string.IsNullOrWhiteSpace(originalName)) return neutral;

            if (source == "endgame")
            {
                // Thí dụ: "單車 vs 單砲", "單車單兵 vs 單士2卒"
                string trans = originalName;
                foreach (var kv in PieceNames)
                {
                    trans = trans.Replace(kv.Key.ToString(), kv.Value + " ");
                }
                trans = trans.Replace("單", "1 ")
                             .Replace("双", "2 ")
                             .Replace("雙", "2 ")
                             .Replace("vs", "đấu")
                             .Replace("  ", " ")
                             .Trim();
                return $"Cờ tàn {trans}";
            }

            string head = originalName.Split(" - ")[0];

            if (source == "advanced")
            {
                // Dạng "双炮马类(01)": liệt kê quân tấn công, 双/单 áp dụng cho quân đứng ngay sau.
                var m = Regex.Match(head, @"^(?<p>[^类\(（]+)类\s*[\(（]?(?<n>\d+)?");
                if (m.Success)
                {
                    string label = TranslatePieces(m.Groups["p"].Value);
                    if (label != null)
                    {
                        string n = m.Groups["n"].Success ? $" ({m.Groups["n"].Value})" : "";
                        return $"Sát cục {label}{n}";
                    }
                }
            }

            foreach (var (zh, vi) in Terms)
            {
                if (head.Contains(zh))
                {
                    var num = Regex.Match(head, @"(?:基本杀法\s*)?(\d+)");
                    string suffix = num.Success && !head.StartsWith("单车破双象")
                        ? $" (bài {int.Parse(num.Groups[1].Value)})"
                        : "";
                    return vi + suffix;
                }
            }

            return neutral;
        }

        private static string? TranslatePieces(string zh)
        {
            if (zh.Contains("其它")) return "tổng hợp";

            var parts = new List<string>();
            int count = 1;
            foreach (char ch in zh)
            {
                if (ch == '双') { count = 2; continue; }
                if (ch == '单') { count = 1; continue; }
                if (!PieceNames.TryGetValue(ch, out var name)) return null;
                parts.Add(count > 1 ? $"{count} {name}" : name);
                count = 1;
            }

            return parts.Count == 0 ? null : string.Join(" + ", parts);
        }
    }
}
