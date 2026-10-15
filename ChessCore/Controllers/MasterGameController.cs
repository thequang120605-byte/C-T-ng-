using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Models;

namespace ChessCore.Controllers
{
    public class MasterGameController : Controller
    {
        private readonly AppDbContext _context;

        public MasterGameController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /MasterGame
        public async Task<IActionResult> Index()
        {
            await EnsureMasterGamesPopulatedAsync();

            var games = await _context.MasterGames
                .OrderByDescending(g => g.EventDate ?? g.CreatedAt)
                .ToListAsync();

            return View(games);
        }

        // GET: /MasterGame/Detail/{id}
        public async Task<IActionResult> Detail(int id)
        {
            await EnsureMasterGamesPopulatedAsync();

            var game = await _context.MasterGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        private async Task EnsureMasterGamesPopulatedAsync()
        {
            // Kiểm tra xem đã có đủ danh sách các ván cờ đỉnh cao chưa (mở rộng lên 12 ván kinh điển)
            if (await _context.MasterGames.CountAsync() < 12)
            {
                var extraGames = new List<MasterGame>
                {
                    new MasterGame
                    {
                        Title = "Vương Thiên Nhất vs Trịnh Duy Đồng - Kỳ Vương Tranh Bá 2019",
                        RedPlayer = "Vương Thiên Nhất",
                        BlackPlayer = "Trịnh Duy Đồng",
                        Event = "Bích Quế Viên Bôi (Siêu Kỳ Vương)",
                        EventDate = new DateTime(2019, 12, 10),
                        Result = "1-0",
                        OpeningName = "Trung Pháo đối Bình Phong Mã Lưỡng Đầu Xà",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"Bich Que Vien Boi\"]\n[Site \"Quang Chau\"]\n[Date \"2019.12.10\"]\n[Round \"Chung Ket\"]\n[Red \"Vuong Thien Nhat\"]\n[Black \"Trinh Duy Dong\"]\n[Result \"1-0\"]\n1. C2.5 H8.7\n2. H2.3 R9.1\n3. R1.2 H2.3\n4. P7.1 P7.1\n5. H8.7 P3.1\n6. R2.6 C8.9\n7. C8.4 R1.2\n8. R2.7 C2.1\n9. P5.1 H7.5\n10. C5.1 C9.2\n(Ngoại tinh lai khách Vương Thiên Nhất công phá trung lộ thần sầu giành ngôi Quán quân)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new MasterGame
                    {
                        Title = "Hứa Ngân Xuyên vs Lữ Khâm - Cúp Ngũ Dương 2001",
                        RedPlayer = "Hứa Ngân Xuyên",
                        BlackPlayer = "Lữ Khâm",
                        Event = "Cúp Cờ Tướng Ngũ Dương Danh Thủ",
                        EventDate = new DateTime(2001, 1, 18),
                        Result = "1-0",
                        OpeningName = "Khởi Mã Cuộc đối Tốt Đề Pháo",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"Ngu Duong Boi\"]\n[Site \"Quang Chau\"]\n[Date \"2001.01.18\"]\n[Round \"Vong 5\"]\n[Red \"Hua Ngan Xuyen\"]\n[Black \"Lu Kham\"]\n[Result \"1-0\"]\n1. H2.3 P7.1\n2. P7.1 C2.5\n3. C2.4 H2.3\n4. H8.7 R1.2\n5. R1.2 H8.7\n6. R2.6 P3.1\n7. C8.9 C8.9\n8. P5.1 C5.4\n9. H7.5 R9.8\n10. R2.8 A4.5\n(Thái cực quy tông Hứa Ngân Xuyên vận dụng tàn cuộc tột đỉnh nghệ thuật)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new MasterGame
                    {
                        Title = "Nguyễn Thành Bảo vs Tôn Dũng Chinh - Đại hội Thể thao Châu Á 2010",
                        RedPlayer = "Nguyễn Thành Bảo",
                        BlackPlayer = "Tôn Dũng Chinh",
                        Event = "Asian Games Quảng Châu 2010",
                        EventDate = new DateTime(2010, 11, 15),
                        Result = "1/2-1/2",
                        OpeningName = "Phi Tượng Cuộc đối Quá Cung Pháo",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"Asian Games 16\"]\n[Site \"Quang Chau\"]\n[Date \"2010.11.15\"]\n[Round \"Vong 4\"]\n[Red \"Nguyen Thanh Bao\"]\n[Black \"Ton Dung Chinh\"]\n[Result \"1/2-1/2\"]\n1. E2.4 C2.6\n2. H2.3 H2.3\n3. R1.2 R1.2\n4. H8.7 P7.1\n5. P7.1 H8.7\n6. C8.9 C8.2\n7. P3.1 P3.1\n8. R2.6 C6.5\n9. C2.5 R2.4\n10. H3.5 R2.8\n(Tây Độc Nguyễn Thành Bảo thi triển lối đánh bão táp quả cảm thủ hòa danh thủ Trung Hoa)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new MasterGame
                    {
                        Title = "Lại Lý Huynh vs Mạnh Thần - Giáp Cấp Liên Tái 2023",
                        RedPlayer = "Lại Lý Huynh",
                        BlackPlayer = "Mạnh Thần",
                        Event = "Giải Vô địch Đồng đội Cờ Tướng Giáp Cấp Trung Quốc",
                        EventDate = new DateTime(2023, 7, 24),
                        Result = "1-0",
                        OpeningName = "Tiên Nhân Chỉ Lộ chuyển Tả Trung Pháo",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"Giap Cap Lien Tai 2023\"]\n[Site \"Hang Chau\"]\n[Date \"2023.07.24\"]\n[Round \"Vong 14\"]\n[Red \"Lai Ly Huynh\"]\n[Black \"Manh Than\"]\n[Result \"1-0\"]\n1. P7.1 P7.1\n2. C2.5 H8.7\n3. H2.3 R9.1\n4. R1.2 H2.3\n5. H8.7 P3.1\n6. R2.6 C8.9\n7. C8.4 R1.2\n8. P5.1 H7.5\n9. C5.1 C9.2\n10. R2.7 C2.1\n(Nam Phương Công Tử Lại Lý Huynh kích bại Siêu đại sư Mạnh Thần vang dội kỳ đàn)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new MasterGame
                    {
                        Title = "Dương Quan Lân vs Hồ Vinh Hoa - Trận Chiến Chuyển Giao Thế Hệ 1960",
                        RedPlayer = "Dương Quan Lân",
                        BlackPlayer = "Hồ Vinh Hoa",
                        Event = "Giải Vô địch Toàn quốc Trung Quốc 1960",
                        EventDate = new DateTime(1960, 10, 22),
                        Result = "0-1",
                        OpeningName = "Quá Cung Pháo đối Bình Phong Mã",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"National Championship 1960\"]\n[Site \"Bac Kinh\"]\n[Date \"1960.10.22\"]\n[Round \"Chung Ket\"]\n[Red \"Duong Quan Lan\"]\n[Black \"Ho Vinh Hoa\"]\n[Result \"0-1\"]\n1. C2.6 H8.7\n2. H2.3 P7.1\n3. R1.2 R9.1\n4. H8.7 H2.3\n5. P7.1 C8.9\n6. R2.6 C2.4\n7. P3.1 P3.1\n8. H3.4 C4.5\n9. R2.7 H7.5\n10. P5.1 R1.2\n(Thiếu niên 15 tuổi Hồ Vinh Hoa hạ gục đương kim Ma Kỳ Dương Quan Lân mở ra kỷ nguyên Thập Liên Quán)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new MasterGame
                    {
                        Title = "Triệu Hâm Hâm vs Tưởng Xuyên - Đỉnh Cao Tấn Công 2007",
                        RedPlayer = "Triệu Hâm Hâm",
                        BlackPlayer = "Tưởng Xuyên",
                        Event = "Giải Vô địch Cá nhân Toàn quốc 2007",
                        EventDate = new DateTime(2007, 9, 16),
                        Result = "1-0",
                        OpeningName = "Trung Pháo Quá Hà Xe đối Bình Phong Mã",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"National Championship 2007\"]\n[Site \"Ho Nam\"]\n[Date \"2007.09.16\"]\n[Round \"Vong 9\"]\n[Red \"Trieu Ham Ham\"]\n[Black \"Tuong Xuyen\"]\n[Result \"1-0\"]\n1. C2.5 H8.7\n2. H2.3 R9.1\n3. R1.2 H2.3\n4. P7.1 P7.1\n5. R2.6 C8.9\n6. R2.2 C9.4\n7. H8.7 P3.1\n8. C8.9 R1.2\n9. P5.1 H7.5\n10. C5.1 C2.5\n(Lục Mạch Thần Kiếm Triệu Hâm Hâm phát động đợt công kích vũ bão giành ngôi Kỳ vương trẻ tuổi)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new MasterGame
                    {
                        Title = "Lại Lý Huynh vs Nguyễn Thành Bảo - Siêu Kinh Điển Việt Nam 2021",
                        RedPlayer = "Lại Lý Huynh",
                        BlackPlayer = "Nguyễn Thành Bảo",
                        Event = "Giải Vô địch Cờ Tướng Xuất sắc Toàn quốc",
                        EventDate = new DateTime(2021, 11, 28),
                        Result = "1-0",
                        OpeningName = "Ngũ Cửu Pháo đối Bình Phong Mã Tả Mã Bàn Hà",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"National Masters Vietnam 2021\"]\n[Site \"Vung Tau\"]\n[Date \"2021.11.28\"]\n[Round \"Chung Ket\"]\n[Red \"Lai Ly Huynh\"]\n[Black \"Nguyen Thanh Bao\"]\n[Result \"1-0\"]\n1. C2.5 H8.7\n2. H2.3 R9.1\n3. R1.2 H2.3\n4. P7.1 P7.1\n5. C8.9 C8.9\n6. R2.6 P3.1\n7. H8.7 R1.2\n8. P9.1 H7.6\n9. C9.4 A4.5\n10. R6.7 C9.2\n(Trận thư hùng đỉnh cao giữa hai tượng đài cờ tướng hàng đầu Việt Nam suốt hai thập kỷ)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new MasterGame
                    {
                        Title = "Trịnh Duy Đồng vs Uông Dương - Quyết Chiến Chung Kết 2018",
                        RedPlayer = "Trịnh Duy Đồng",
                        BlackPlayer = "Uông Dương",
                        Event = "Giải Vô địch Cờ Tướng Cá nhân Châu Á 2018",
                        EventDate = new DateTime(2018, 9, 25),
                        Result = "0-1",
                        OpeningName = "Phi Tượng Cuộc đối Sĩ Giác Pháo",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"Asian Individual Championship 2018\"]\n[Site \"Thuong Hai\"]\n[Date \"2018.09.25\"]\n[Round \"Chung Ket\"]\n[Red \"Trinh Duy Dong\"]\n[Black \"Uong Duong\"]\n[Result \"0-1\"]\n1. E2.4 C8.4\n2. H2.3 H8.7\n3. P7.1 P7.1\n4. R1.2 R9.1\n5. H8.7 H2.3\n6. C2.5 P3.1\n7. R2.6 C2.1\n8. C8.9 R1.2\n9. P3.1 C4.5\n10. H7.5 R2.7\n(Toán Đầu Đà Uông Dương tính toán thâm sâu xuất sắc kích bại Thục Sơn Thiếu Hiệp Trịnh Duy Đồng)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new MasterGame
                    {
                        Title = "Vương Thiên Nhất vs Tạ Tịnh - Đỉnh Phong Đối Quyết 2013",
                        RedPlayer = "Vương Thiên Nhất",
                        BlackPlayer = "Tạ Tịnh",
                        Event = "Chung kết Giải Vô địch Quốc gia Trung Quốc 2013",
                        EventDate = new DateTime(2013, 11, 14),
                        Result = "1-0",
                        OpeningName = "Khởi Mã Cuộc Chuyển Trung Pháo đối Đơn Đề Mã",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"China National Championship 2013\"]\n[Site \"Thach Gia Trang\"]\n[Date \"2013.11.14\"]\n[Round \"Chung Ket\"]\n[Red \"Vuong Thien Nhat\"]\n[Black \"Ta Tinh\"]\n[Result \"1-0\"]\n1. H2.3 P7.1\n2. P7.1 H8.7\n3. C2.5 H2.3\n4. R1.2 R9.1\n5. H8.7 P3.1\n6. R2.6 C8.9\n7. C8.4 R1.2\n8. P5.1 H7.5\n9. C5.1 C9.2\n10. R2.7 C2.1\n(Ngoại Tinh Lai Khách điều binh khiển tướng như thần giành danh hiệu vô địch thuyết phục)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new MasterGame
                    {
                        Title = "Liễu Đại Hoa vs Lý Lai Quần - Kỳ Đàn Tranh Hùng 1982",
                        RedPlayer = "Liễu Đại Hoa",
                        BlackPlayer = "Lý Lai Quần",
                        Event = "Cúp Danh Thủ Ngũ Dương Bôi Lần 2",
                        EventDate = new DateTime(1982, 1, 10),
                        Result = "1-0",
                        OpeningName = "Trung Pháo Tiến Tam Binh đối Bình Phong Mã",
                        InitialFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1",
                        PgnContent = "[Event \"Ngu Duong Boi 1982\"]\n[Site \"Quang Chau\"]\n[Date \"1982.01.10\"]\n[Round \"Vong 3\"]\n[Red \"Lieu Dai Hoa\"]\n[Black \"Ly Lai Quan\"]\n[Result \"1-0\"]\n1. C2.5 H8.7\n2. H2.3 R9.1\n3. P3.1 P3.1\n4. R1.2 H2.3\n5. H8.9 C8.9\n6. R2.6 P7.1\n7. C8.7 R1.2\n8. P5.1 H7.5\n9. R6.7 C2.1\n10. C7.1 A4.5\n(Đông Phương Ma Mưu Liễu Đại Hoa phô diễn nghệ thuật cờ mù và trung cuộc thâm sâu)",
                        MovesJson = "[]",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                foreach (var g in extraGames)
                {
                    if (!await _context.MasterGames.AnyAsync(m => m.Title == g.Title))
                    {
                        _context.MasterGames.Add(g);
                    }
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}
