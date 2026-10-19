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
            new() { Level = 1, Name = "Làm quen",  Rule = "Sát cục nhập môn, chiếu bí trong 1 nước (Bài 1 - 100)" },
            new() { Level = 2, Name = "Cơ bản",    Rule = "Sát cục cơ bản, chiếu bí trong 1–2 nước (Bài 101 - 200)" },
            new() { Level = 3, Name = "Trung cấp", Rule = "Sát cục trung cấp, chiếu bí trong 2 nước (Bài 201 - 300)" },
            new() { Level = 4, Name = "Nâng cao",  Rule = "Sát cục nâng cao, chiếu bí trong 2–3 nước (Bài 301 - 400)" },
            new() { Level = 5, Name = "Chuyên sâu", Rule = "Sát cục chuyên sâu, chiếu bí trong 3 nước hiểm hóc (Bài 401 - 500)" },
        };

        /// <summary>Tỉ lệ bài cần hoàn thành ở một cấp để mở cấp kế tiếp.</summary>
        public const double UnlockRatio = 0.8;

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

        private void Finish()
        {
            // Lọc các bài đã có lời giải sát cục cưỡng bức (MateIn 1..3)
            var m1 = _all.Where(p => p.MateIn == 1)
                         .OrderBy(p => p.Pieces)
                         .ThenBy(p => p.Source == "basic" ? 1 : (p.Source == "advanced" ? 2 : 3))
                         .ThenBy(p => p.OriginalIndex)
                         .ToList();

            var m2 = _all.Where(p => p.MateIn == 2)
                         .OrderBy(p => p.Pieces)
                         .ThenBy(p => p.Source == "basic" ? 1 : (p.Source == "advanced" ? 2 : 3))
                         .ThenBy(p => p.OriginalIndex)
                         .ToList();

            var m3 = _all.Where(p => p.MateIn == 3)
                         .OrderBy(p => p.Pieces)
                         .ThenBy(p => p.Source == "basic" ? 1 : (p.Source == "advanced" ? 2 : 3))
                         .ThenBy(p => p.OriginalIndex)
                         .ToList();

            // Phân bổ đều và mượt mà thành 500 bài (5 cấp, mỗi cấp đúng 100 bài):
            // Cấp 1 (Bài 1 - 100): 100 bài chiếu bí 1 nước nhập môn
            var l1 = m1.Take(100).ToList();

            // Cấp 2 (Bài 101 - 200): 60 bài chiếu bí 1 nước tiếp theo + 40 bài chiếu bí 2 nước
            var l2 = m1.Skip(100).Take(60).Concat(m2.Take(40)).ToList();

            // Cấp 3 (Bài 201 - 300): 100 bài chiếu bí 2 nước tiếp theo
            var l3 = m2.Skip(40).Take(100).ToList();

            // Cấp 4 (Bài 301 - 400): 36 bài chiếu bí 2 nước tiếp theo + 64 bài chiếu bí 3 nước
            var l4 = m2.Skip(140).Take(36).Concat(m3.Take(64)).ToList();

            // Cấp 5 (Bài 401 - 500): 100 bài chiếu bí 3 nước đỉnh cao
            var l5 = m3.Skip(64).Take(100).ToList();

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
