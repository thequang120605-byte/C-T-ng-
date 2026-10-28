using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ChessCore.Data;
using ChessCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChessCore.Controllers
{
    [ApiController]
    public class QuestController : ControllerBase
    {
        private readonly AppDbContext _context;
        private static readonly ConcurrentDictionary<string, HashSet<string>> _claimedQuests = new();

        public QuestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("/api/quests/daily")]
        public async Task<IActionResult> GetDailyQuests()
        {
            int userId = GetCurrentUserId();
            User? user = null;
            if (userId > 0)
            {
                user = await _context.Users.FindAsync(userId);
            }

            string userKey = userId > 0 ? userId.ToString() : "guest";
            var claimedSet = _claimedQuests.GetOrAdd(userKey, _ => new HashSet<string>());

            bool CheckClaimed(string questId)
            {
                lock (claimedSet)
                {
                    return claimedSet.Contains(questId);
                }
            }

            var quests = new List<object>
            {
                new {
                    id = "login",
                    title = "☀️ Điểm Danh Kỳ Viện",
                    description = "Đăng nhập vào kỳ viện hôm nay",
                    rewardGold = 1000,
                    rewardGems = 2,
                    target = 1,
                    current = 1,
                    isClaimed = CheckClaimed("login")
                },
                new {
                    id = "rush",
                    title = "⚡ Thần Tốc Phá Ải",
                    description = "Giải đúng ít nhất 3 thế cờ trong Thần Tốc",
                    rewardGold = 1500,
                    rewardGems = 5,
                    target = 3,
                    current = Math.Min(3, user?.PuzzleScore != null ? (user.PuzzleScore / 100) : 1),
                    isClaimed = CheckClaimed("rush")
                },
                new {
                    id = "ai",
                    title = "🤖 Thử Thách Trí Tuệ",
                    description = "Chiến thắng 1 ván đấu với AI",
                    rewardGold = 2000,
                    rewardGems = 10,
                    target = 1,
                    current = Math.Min(1, user?.Wins ?? 0),
                    isClaimed = CheckClaimed("ai")
                },
                new {
                    id = "academy",
                    title = "🎓 Cầu Đạo Kỳ Nghệ",
                    description = "Hoàn thành 1 bài học tại Viện Kỳ Đạo",
                    rewardGold = 1000,
                    rewardGems = 3,
                    target = 1,
                    current = 1,
                    isClaimed = CheckClaimed("academy")
                },
                new {
                    id = "master",
                    title = "📜 Nghiên Cứu Đại Sư",
                    description = "Xem lại 1 kỳ phổ của Đại Sư giang hồ",
                    rewardGold = 1000,
                    rewardGems = 2,
                    target = 1,
                    current = 1,
                    isClaimed = CheckClaimed("master")
                },
                new {
                    id = "chat",
                    title = "💬 Giao Lưu Hảo Hữu",
                    description = "Gửi 1 tin nhắn nhanh hoặc emote trong trận",
                    rewardGold = 500,
                    rewardGems = 1,
                    target = 1,
                    current = 1,
                    isClaimed = CheckClaimed("chat")
                }
            };

            return Ok(new
            {
                total = quests.Count,
                quests
            });
        }

        // FIX BUG_009: Kiểm tra điều kiện hoàn thành nhiệm vụ & Chống spam claim
        [HttpPost("/api/quests/{id}/claim")]
        public async Task<IActionResult> ClaimQuest(string id)
        {
            int userId = GetCurrentUserId();
            string userKey = userId > 0 ? userId.ToString() : "guest";
            var claimedSet = _claimedQuests.GetOrAdd(userKey, _ => new HashSet<string>());

            lock (claimedSet)
            {
                if (claimedSet.Contains(id.ToLower()))
                {
                    return BadRequest(new { success = false, message = "Nhiệm vụ này đã nhận thưởng rồi, không thể nhận lại." });
                }
            }

            User? user = null;
            if (userId > 0)
            {
                user = await _context.Users.FindAsync(userId);
            }

            bool isCompleted = false;
            switch (id.ToLower())
            {
                case "ai":
                    isCompleted = (user != null && user.Wins >= 1);
                    break;
                case "pvp":
                    isCompleted = (user != null && user.GamesPlayed >= 1);
                    break;
                case "rush":
                    isCompleted = (user != null && user.PuzzleScore >= 300);
                    break;
                case "academy":
                case "master":
                case "chat":
                    isCompleted = (user != null);
                    break;
                default:
                    isCompleted = false;
                    break;
            }

            if (!isCompleted)
            {
                return BadRequest(new { success = false, message = "Bạn chưa hoàn thành mục tiêu của nhiệm vụ này để nhận thưởng." });
            }

            lock (claimedSet)
            {
                claimedSet.Add(id.ToLower());
            }

            if (user != null)
            {
                user.PuzzleScore += 100;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                questId = id,
                message = "Nhận thưởng thành công!",
                claimedAt = DateTime.UtcNow
            });
        }

        [HttpGet("/api/achievements")]
        public async Task<IActionResult> GetAchievements()
        {
            int userId = GetCurrentUserId();
            User? user = null;
            if (userId > 0)
            {
                user = await _context.Users.FindAsync(userId);
            }

            int gamesPlayed = user?.GamesPlayed ?? 1;
            int wins = user?.Wins ?? 0;
            int elo = user?.Elo ?? 1200;

            var achievements = new List<object>
            {
                new {
                    id = "novice",
                    title = "Tân Thủ Nhập Môn",
                    icon = "🥉",
                    description = "Tham gia ván cờ đầu tiên trong sự nghiệp",
                    isUnlocked = gamesPlayed >= 1,
                    badgeName = "Tập Sự"
                },
                new {
                    id = "rush_5",
                    title = "Tia Chớp Bàn Cờ",
                    icon = "⚡",
                    description = "Đạt chuỗi 5 câu đúng liên tục trong Thần Tốc",
                    isUnlocked = (user?.PuzzleScore ?? 0) >= 600,
                    badgeName = "Kỳ Thủ Nhanh"
                },
                new {
                    id = "scholar",
                    title = "Thông Tuệ Binh Pháp",
                    icon = "📚",
                    description = "Vượt qua 5 bài kiểm tra trắc nghiệm Viện Kỳ Đạo",
                    isUnlocked = true,
                    badgeName = "Kỳ Sĩ"
                },
                new {
                    id = "ai_slayer",
                    title = "Thiết Mã Phá Trận",
                    icon = "🥋",
                    description = "Kích bại AI ở độ khó Kiện Tướng",
                    isUnlocked = wins >= 3,
                    badgeName = "Chiến Tướng"
                },
                new {
                    id = "grandmaster",
                    title = "Tiêu Diêu Kỳ Thánh",
                    icon = "👑",
                    description = "Đạt 1500 ELO và tích lũy 50 ván thắng",
                    isUnlocked = elo >= 1500 && wins >= 50,
                    badgeName = "Kỳ Thánh"
                }
            };

            return Ok(new
            {
                total = achievements.Count,
                achievements
            });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id");
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }
            return 0;
        }
    }
}
